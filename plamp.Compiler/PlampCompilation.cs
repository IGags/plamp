using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using plamp.Abstractions.Assemblies;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Compilation;
using plamp.Abstractions.Compilation.Models;
using plamp.Abstractions.CompilerEmission;
using plamp.Abstractions.Parsing;
using plamp.Abstractions.Validation;
using plamp.Abstractions.Validation.Models;
using plamp.Compiler.Model;
using plamp.Compiler.Util;

namespace plamp.Compiler;

public class PlampCompilation : ICompilation
{
    private readonly ResourceScheduler<IParser> _parser;
    private readonly IReadOnlyList<ResourceScheduler<IValidator>> _validators;
    private readonly ResourceScheduler<ICompiledEmitter> _emitter;
    private ICompiledAssemblyContainer _container;
    private bool _complete;
    private readonly SemaphoreSlim _sem = new(1, 1);

    private readonly Dictionary<AssemblyName, CompilationAssemblyModel> _compilationAssemblyModels 
        = new(new AssemblyNameEqualityComparer());

    public PlampCompilation(
        ResourceScheduler<IParser> parser,
        IReadOnlyList<ResourceScheduler<IValidator>> validators,
        ResourceScheduler<ICompiledEmitter> emitter, 
        ICompiledAssemblyContainer container)
    {
        _parser = parser;
        _validators = validators;
        _emitter = emitter;
        _container = container;
    }

    public void AddDynamicAssembly(
        AssemblyName assemblyName,
        HashSet<SourceFile> sourceFile,
        HashSet<AssemblyName> referencedAssemblies = null)
    {
        if(_complete) ThrowIncorrectCompilation();
        _sem.Wait();
        try
        {
            if(_complete) ThrowIncorrectCompilation();
            _compilationAssemblyModels
                .Add(assemblyName, new (assemblyName, sourceFile, referencedAssemblies));
        }
        finally
        {
            _sem.Release();
        }
    }

    public async Task<CompilationResult> TryCompileAsync(CancellationToken cancellationToken = default)
    {
        await _sem.WaitAsync(cancellationToken);
        _complete = true;
        _sem.Release();
        var treeBuilder = new AssemblyTreeBuilder();
        if (!treeBuilder.TryBuildAssemblyTree(
                _compilationAssemblyModels.Values.ToList(), 
                _container, 
                out var compilationTaskQueue, 
                out var errors))
        {
            return new CompilationResult(false, [], errors);
        }

        var compilations = new List<AssemblyCompilation>();

        while (compilationTaskQueue.Any())
        {
            var job = compilationTaskQueue.Dequeue();
            var compilationResult = await ExecutePipelineAsync(job, cancellationToken);
            if (compilationResult.Success)
            {
                compilations.Add(compilationResult);
                _container = _container.AddCompiledDynamicAssembly(compilationResult.CompiledAssembly);
            }
            else
            {
                compilations.Add(compilationResult);
                return new CompilationResult(false, compilations, []);
            }
        }
        
        return new CompilationResult(true, compilations, []);
    }

    private async Task<AssemblyCompilation> ExecutePipelineAsync(ReadOnlyAssemblyTreeNode node, CancellationToken cancellationToken)
    {
        var parserTasks = new List<Task<WrappedParsedResult>>();
        foreach (var sourceFile in node.SourceFiles)
        {
            parserTasks.Add(ParseAsync(sourceFile, node.Name, cancellationToken));
        }
        
        var parserResults = await Task.WhenAll(parserTasks);
        var currentCompilationSymbols = parserResults
            .SelectMany(x => x.ParserResult.NodeList).ToList();

        var contexts = parserResults.Select(
            x => x.ParserResult.NodeList
                .Select(y => CreateValidationContext(x, y, currentCompilationSymbols)))
            .SelectMany(x => x).ToList();

        var validationTasks = new List<Task<ValidationResult>>();
        foreach (var context in contexts)
        {
            validationTasks.Add(ExecuteValidationPipelineAsync(context, cancellationToken));
        }
        var validationResults = await Task.WhenAll(validationTasks);
        
        var exceptions
            = parserResults.SelectMany(x => x.ParserResult.Exceptions)
                .Concat(validationResults.SelectMany(x => x.Exceptions))
                .ToList();
        
        if (exceptions.Any(x => x.Level == ExceptionLevel.Error))
        {
            return new AssemblyCompilation(null, false, null, exceptions);
        }

        var emitter = _emitter.GetResource();
        var symbolTables = parserResults.Select(x => x.ParserResult.SymbolTable).ToList();
        var table = new CompositeSymbolTable(symbolTables);
        var assembly = emitter.EmitAssembly(currentCompilationSymbols, _container, table, node.Name);

        return new AssemblyCompilation(null, true, assembly, exceptions);
    }

    private ValidationContext CreateValidationContext(
        WrappedParsedResult parsedResult,
        NodeBase currentSymbol, 
        IReadOnlyList<NodeBase> currentCompilationSymbols)
    {
        return new ValidationContext()
        {
            AssemblyContainer = _container,
            AssemblyName = parsedResult.AssemblyName,
            Ast = currentSymbol,
            CurrentCompilationSymbols = currentCompilationSymbols,
            Exceptions = [],
            FileName = parsedResult.FromSourceFile.FileName,
            Table = parsedResult.ParserResult.SymbolTable
        };
    }

    private Task<WrappedParsedResult> ParseAsync(
        SourceFile sourceFile, 
        AssemblyName assemblyName, 
        CancellationToken cancellationToken)
    {
        var parser = _parser.GetResource();
        var result = parser.Parse(sourceFile, assemblyName, cancellationToken);
        _parser.Return(parser);
        return Task.FromResult(new WrappedParsedResult(result, sourceFile, assemblyName));
    }

    private Task<ValidationResult> ExecuteValidationPipelineAsync(
        ValidationContext context,
        CancellationToken cancellationToken)
    {
        var exceptions = new List<PlampException>();
        foreach (var resourceScheduler in _validators)
        {
            var validator = resourceScheduler.GetResource();
            var result = validator.Validate(context, cancellationToken);
            exceptions.AddRange(result.Exceptions);
            resourceScheduler.Return(validator);
        }

        var validationResult = new ValidationResult() { Exceptions = exceptions };
        return Task.FromResult(validationResult);
    }

    private void ThrowIncorrectCompilation() 
        => throw new InvalidOperationException("Compilation has already been started.");
}