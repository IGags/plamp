using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using plamp.Abstractions.Assemblies;
using plamp.Abstractions.Compilation;
using plamp.Abstractions.CompilerEmission;
using plamp.Abstractions.Parsing;
using plamp.Abstractions.Validation;
using plamp.Compiler.Model;

namespace plamp.Compiler;

public class PlampCompilation : ICompilation
{
    private readonly ResourceScheduler<IParser> _parser;
    private readonly IReadOnlyList<ResourceScheduler<IValidator>> _validators;
    private readonly ResourceScheduler<ICompiledEmitter> _emitter;
    private readonly IStaticAssemblyContainer _container;

    private readonly Dictionary<AssemblyName, CompilationAssemblyModel> _compilationAssemblyModels = [];

    public PlampCompilation(
        ResourceScheduler<IParser> parser,
        IReadOnlyList<ResourceScheduler<IValidator>> validators,
        ResourceScheduler<ICompiledEmitter> emitter, 
        IStaticAssemblyContainer container)
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
        throw new NotImplementedException();
    }

    public Task<CompilationResult> TryCompileAsync()
    {
        throw new NotImplementedException();
    }
}