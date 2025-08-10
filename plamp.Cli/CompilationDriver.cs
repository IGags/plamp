using System.Reflection;
using System.Reflection.Emit;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Compilation.Models;
using plamp.Alternative;
using plamp.Alternative.Parsing;
using plamp.Alternative.Tokenization;
using plamp.Alternative.Visitors.ModuleCreation;
using plamp.Alternative.Visitors.ModulePreCreation;
using plamp.Alternative.Visitors.ModulePreCreation.MemberNameUniqueness;
using plamp.Alternative.Visitors.ModulePreCreation.ModuleName;
using plamp.Alternative.Visitors.ModulePreCreation.MustReturn;
using plamp.Alternative.Visitors.ModulePreCreation.SignatureInference;
using plamp.Alternative.Visitors.ModulePreCreation.TypeInference;

namespace plamp.Cli;

public class CompilationDriver
{
    public static CompilationRes CompileModule(string fileName, string programText)
    {
        var source = new SourceFile(fileName, programText);
        var tokenizationResult = Tokenizer.Tokenize(source);
        var symbolTable = new SymbolTable();
        var parsingContext = new ParsingContext(tokenizationResult.Sequence, fileName, tokenizationResult.Exceptions,
            symbolTable);
        var ast = Parser.ParseFile(parsingContext);
        var context = new PreCreationContext(fileName, symbolTable);
        context.Exceptions.AddRange(parsingContext.Exceptions);
        var moduleNameVisitor = new ModuleNameValidator();
        context = moduleNameVisitor.Validate(ast, context);
        
        var memberNameVisitor = new MemberNameUniquenessValidator();
        context = memberNameVisitor.Validate(ast, context);

        var memberSignatureVisitor = new SignatureTypeInferenceWeaver();
        context = memberSignatureVisitor.WeaveDiffs(ast, context);

        var funcReturnVisitor = new MethodMustReturnValueValidator();
        context = funcReturnVisitor.Validate(ast, context);
        
        var typeInferenceVisitor = new TypeInferenceWeaver();
        context = typeInferenceVisitor.WeaveDiffs(ast, context);

        if (context.Exceptions.Count != 0)
        {
            return new(context.Exceptions, null);
        }
        
        var assemblyName = new AssemblyName(DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule(context.ModuleName!);

        var compilationContext = new CreationContext(assemblyBuilder, moduleBuilder, context);
        var signatureVisitor = new DefSignatureCreationValidator();
        compilationContext = signatureVisitor.Validate(ast, compilationContext);
        var callVisitor = new MethodCallInferenceValidator();
        compilationContext = callVisitor.Validate(ast, compilationContext);

        var compilationVisitor = new CompilationValidator();
        compilationVisitor.Validate(ast, compilationContext);
        moduleBuilder.CreateGlobalFunctions();
        return new CompilationRes([], assemblyBuilder);
    }
    
    public record CompilationRes(List<PlampException> Exceptions, Assembly? Compiled);
}

public class ParamImpl(Type type, string name) : ParameterInfo
{
    public override Type ParameterType => type;
    public override string Name => name;
}