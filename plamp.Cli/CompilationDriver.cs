using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using plamp.Abstractions;
using plamp.Abstractions.Ast;
using plamp.Alternative;
using plamp.Alternative.Parsing;
using plamp.Alternative.Tokenization;
using plamp.Alternative.Visitors.ModuleCreation;
using plamp.Alternative.Visitors.ModulePreCreation;
using plamp.Alternative.Visitors.ModulePreCreation.MustReturn;
using plamp.Alternative.Visitors.ModulePreCreation.TypeInference;
using plamp.Alternative.Visitors.SymbolTableBuilding;
using plamp.Alternative.Visitors.SymbolTableBuilding.FieldDefInference;
using plamp.Alternative.Visitors.SymbolTableBuilding.FuncDefInference;
using plamp.Alternative.Visitors.SymbolTableBuilding.MemberNameUniqueness;
using plamp.Alternative.Visitors.SymbolTableBuilding.ModuleName;
using plamp.Alternative.Visitors.SymbolTableBuilding.TypedefInference;
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

        var dependencies = new List<ISymbolTable> { RuntimeSymbols.SymbolTable };
        var currentModuleTable = new SymbolTable("%UNDEFINED%", dependencies);
        dependencies.Add(currentModuleTable);
        var symTableBuildingContext = new SymbolTableBuildingContext(
            translationTable, 
            dependencies,
            currentModuleTable);
        symTableBuildingContext.Exceptions.AddRange(parsingContext.Exceptions);

        if (printAst)
        {
            var printVisitor = new PrintAstVisitor();
            printVisitor.Validate(ast, symTableBuildingContext);
            return new CompilationRes(symTableBuildingContext.Exceptions, null);
        }
        
        //Валидация того, что имя модуля не совпадает с именем члена, объявленного в нём.
        var moduleNameVisitor = new ModuleNameValidator();
        symTableBuildingContext = moduleNameVisitor.Validate(ast, symTableBuildingContext);

        //Имена типов не должны совпадать с именами функции.
        var memberNameUniquenessValidator = new MemberNameUniquenessValidator();
        symTableBuildingContext = memberNameUniquenessValidator.Validate(ast, symTableBuildingContext);

        //Валидация и добавление типов в таблицу символов
        var typeDefInferenceWeaver = new TypedefInferenceWeaver();
        symTableBuildingContext = typeDefInferenceWeaver.WeaveDiffs(ast, symTableBuildingContext);

        //Валидация, типизация и добавление полей в таблицу символов.
        var fieldDefInferenceWeaver = new FieldDefInferenceWeaver();
        symTableBuildingContext = fieldDefInferenceWeaver.WeaveDiffs(ast, symTableBuildingContext);

        //Вывод сигнатур функции, вывод типов и возвращаемых значение, запись в таблицу символов.
        //Валидация дублирующихся объявлений.
        var funcDefInferenceWeaver = new FuncDefInferenceWeaver();
        symTableBuildingContext = funcDefInferenceWeaver.WeaveDiffs(ast, symTableBuildingContext);

        var preCreateContext = new PreCreationContext(symTableBuildingContext.TranslationTable, dependencies);
        
        //Проверка того, что функция всегда возвращает значеие.
        var funcReturnVisitor = new FuncMustReturnValueValidator();
        preCreateContext = funcReturnVisitor.Validate(ast, preCreateContext);
        
        var typeInferenceVisitor = new TypeInferenceWeaver();
        preCreateContext = typeInferenceVisitor.WeaveDiffs(ast, preCreateContext);

        if (preCreateContext.Exceptions.Count != 0)
        {
            return new(preCreateContext.Exceptions, null);
        }
        
        var assemblyName = new AssemblyName(DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule(currentModuleTable.ModuleName);

        var compilationContext = new CreationContext(assemblyBuilder, moduleBuilder, currentModuleTable, symTableBuildingContext);
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