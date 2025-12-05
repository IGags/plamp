using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using plamp.Abstractions.Ast;
using plamp.Alternative;
using plamp.Alternative.Parsing;
using plamp.Alternative.Tokenization;
using plamp.Alternative.Visitors.ModuleCreation;
using plamp.Alternative.Visitors.ModulePreCreation;
using plamp.Alternative.Visitors.ModulePreCreation.FieldDefInference;
using plamp.Alternative.Visitors.ModulePreCreation.FuncDefInference;
using plamp.Alternative.Visitors.ModulePreCreation.ModuleName;
using plamp.Alternative.Visitors.ModulePreCreation.MustReturn;
using plamp.Alternative.Visitors.ModulePreCreation.TypedefInference;
using plamp.Alternative.Visitors.ModulePreCreation.TypeInference;
using plamp.Cli.Diagnostics;
using plamp.Intrinsics;

namespace plamp.Cli;

public static class CompilationDriver
{
    public static async Task<CompilationRes> CompileModuleAsync(string fileName, string programText, bool printAst)
    {
        using var stream = new MemoryStream(Encoding.Unicode.GetBytes(programText));
        using var reader = new StreamReader(stream, Encoding.Unicode);
        var tokenizationResult = await Tokenizer.TokenizeAsync(reader, fileName);
        var translationTable = new TranslationTable();
        var parsingContext = new ParsingContext(tokenizationResult.Sequence, tokenizationResult.Exceptions, translationTable);
        var ast = Parser.ParseFile(parsingContext);
        
        var context = new PreCreationContext(translationTable, new SymbolTable("%UNDEFINED%", []));
        context.Dependencies.Add(RuntimeSymbols.GetSymbolTable);
        context.Exceptions.AddRange(parsingContext.Exceptions);

        if (printAst)
        {
            var printVisitor = new PrintAstVisitor();
            printVisitor.Validate(ast, context);
            return new CompilationRes(context.Exceptions, null);
        }
        
        //Валидация того, что имя модуля не совпадает с именем члена, объявленного в нём.
        var moduleNameVisitor = new ModuleNameValidator();
        context = moduleNameVisitor.Validate(ast, context);

        //Валидация и добавление типов в таблицу символов
        var typeDefInferenceWeaver = new TypedefInferenceWeaver();
        context = typeDefInferenceWeaver.WeaveDiffs(ast, context);

        //Валидация, типизация и добавление полей в таблицу символов.
        var fieldDefInferenceWeaver = new FieldDefInferenceWeaver();
        context = fieldDefInferenceWeaver.WeaveDiffs(ast, context);

        //Вывод сигнатур функции, вывод типов и возвращаемых значение, запись в таблицу символов.
        //Валидация дублирующихся объявлений.
        var funcDefInferenceWeaver = new FuncDefInferenceWeaver();
        context = funcDefInferenceWeaver.WeaveDiffs(ast, context);

        //Проверка того, что функция всегда возвращает значеие.
        var funcReturnVisitor = new FuncMustReturnValueValidator();
        context = funcReturnVisitor.Validate(ast, context);
        
        var typeInferenceVisitor = new TypeInferenceWeaver();
        context = typeInferenceVisitor.WeaveDiffs(ast, context);

        if (context.Exceptions.Count != 0)
        {
            return new(context.Exceptions, null);
        }
        
        var assemblyName = new AssemblyName(DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule(context.SymbolTable.ModuleName);

        var compilationContext = new CreationContext(assemblyBuilder, moduleBuilder, context.SymbolTable, context);
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