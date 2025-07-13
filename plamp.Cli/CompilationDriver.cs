using System.Reflection;
using System.Reflection.Emit;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Compilation.Models;
using plamp.Alternative;
using plamp.Alternative.Parsing;
using plamp.Alternative.Tokenization;
using plamp.Alternative.Visitors;
using plamp.Validators.BasicSemanticsValidators.MustReturn;

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
        
        var moduleNameVisitor = new ModuleNameWeaver();
        var moduleNameRes = moduleNameVisitor.WeaveDiffs(ast,
            new ModuleNameWeaverContext(tokenizationResult.Exceptions, symbolTable, fileName));
        
        var memberNameVisitor = new MemberNameUniquenessValidator();
        var memberNameVisitorRes = memberNameVisitor.Validate(ast,
            new MemberNameUniquenessValidatorContext(moduleNameRes.Exceptions, symbolTable, fileName));

        var memberSignatureVisitor = new SignatureTypeInferenceWeaver();
        var memberSignatureRes = memberSignatureVisitor.WeaveDiffs(ast, new SignatureInferenceContext(memberNameVisitorRes.Exceptions, symbolTable, fileName));

        var defSignatures = memberSignatureRes.Signatures
            .GroupBy(x => x.Name).Where(x => x.Count() == 1)
            .SelectMany(x => x).ToDictionary(x => x.Name.MemberName, x => x);

        var funcReturnVisitor = new MethodMustReturnValueValidator();
        var funcReturnRes = funcReturnVisitor.Validate(ast,
            new MustReturnValueContext() { Exceptions = memberSignatureRes.Exceptions, SymbolTable = symbolTable });
        
        var typeInferenceVisitor = new TypeInferenceWeaver();
        var typeInferenceRes = typeInferenceVisitor.WeaveDiffs(ast,
            new TypeInferenceContext(symbolTable, fileName, defSignatures, funcReturnRes.Exceptions));

        if (typeInferenceRes.Exceptions.Count != 0)
        {
            return new(typeInferenceRes.Exceptions, null);
        }
        
        var assemblyName = new AssemblyName(DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule(moduleNameRes.ModuleName!);
        var signatureVisitor = new DefSignatureCreationWeaver();
        var methods = signatureVisitor.WeaveDiffs(ast, new DefSignatureCreationContext(moduleBuilder)).Methods;
        var callVisitor = new MethodCallInferenceWeaver();
        callVisitor.WeaveDiffs(ast, new MethodCallInferenceContext(methods, symbolTable));

        var compilationVisitor = new CompilationWeaver();
        var argDict = defSignatures.ToDictionary(
            x => x.Key,
            x => x.Value
                .ParameterList.Select(y => new ParamImpl(y.Type.Symbol, y.Name.MemberName))
                .Cast<ParameterInfo>().ToArray());
        compilationVisitor.WeaveDiffs(ast, new CompilationContext(methods, argDict));
        moduleBuilder.CreateGlobalFunctions();
        return new CompilationRes([], assemblyBuilder);
    }
    
    private class ParamImpl(Type type, string name) : ParameterInfo
    {
        public override Type ParameterType => type;
        public override string Name => name;
    }

    public record CompilationRes(List<PlampException> Exceptions, Assembly? Compiled);
}