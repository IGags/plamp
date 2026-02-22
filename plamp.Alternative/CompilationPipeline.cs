using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Symbols.SymTable;
using plamp.Abstractions.Symbols.SymTableBuilding;
using plamp.Alternative.Parsing;
using plamp.Alternative.SymbolsBuildingImpl;
using plamp.Alternative.Tokenization;
using plamp.Alternative.Visitors;
using plamp.Alternative.Visitors.ModulePreCreation;
using plamp.Alternative.Visitors.ModulePreCreation.FlowControlInsideLoop;
using plamp.Alternative.Visitors.ModulePreCreation.MustReturn;
using plamp.Alternative.Visitors.ModulePreCreation.TypeInference;
using plamp.Alternative.Visitors.SymbolTableBuilding;
using plamp.Alternative.Visitors.SymbolTableBuilding.CircularDependency;
using plamp.Alternative.Visitors.SymbolTableBuilding.FieldDefInference;
using plamp.Alternative.Visitors.SymbolTableBuilding.FuncDefInference;
using plamp.Alternative.Visitors.SymbolTableBuilding.MemberNameUniqueness;
using plamp.Alternative.Visitors.SymbolTableBuilding.ModuleName;
using plamp.Alternative.Visitors.SymbolTableBuilding.TypedefInference;

namespace plamp.Alternative;

public static class CompilationPipeline
{
    public static async Task<ParsingResult> RunParsingAsync(StreamReader fileStream, string fileName)
    {
        var tokenizationResult = await Tokenizer.TokenizeAsync(fileStream, fileName);
        var translationTable = new TranslationTable();
        var parsingContext = new ParsingContext(tokenizationResult.Sequence, tokenizationResult.Exceptions, translationTable);
        var ast = Parser.ParseFile(parsingContext);
        return new ParsingResult(ast, parsingContext);
    }

    public static SymTableBuildingResult RunSymTableBuilding(
        NodeBase ast,
        SymbolTableBuildingContext context)
    {
        //Валидация того, что имя модуля не совпадает с именем члена, объявленного в нём.
        var moduleNameVisitor = new ModuleNameValidator();
        context = moduleNameVisitor.Validate(ast, context);

        //Имена типов не должны совпадать с именами функции.
        var memberNameUniquenessValidator = new MemberNameUniquenessValidator();
        context = memberNameUniquenessValidator.Validate(ast, context);

        //Валидация и добавление типов в таблицу символов
        var typeDefInferenceWeaver = new TypedefInferenceWeaver();
        context = typeDefInferenceWeaver.WeaveDiffs(ast, context);

        //Валидация, типизация и добавление полей в таблицу символов.
        var fieldDefInferenceWeaver = new FieldDefInferenceWeaver();
        context = fieldDefInferenceWeaver.WeaveDiffs(ast, context);

        var circularDependencyValidator = new TypeHasCircularDependencyValidator();
        context = circularDependencyValidator.Validate(ast, context);

        //Вывод сигнатур функции, вывод типов и возвращаемых значений, запись в таблицу символов.
        //Валидация дублирующихся объявлений.
        var funcDefInferenceWeaver = new FuncDefInferenceWeaver();
        funcDefInferenceWeaver.WeaveDiffs(ast, context);

        return new (ast);
    }

    public static PreCreationResult RunModulePreCreate(NodeBase ast, PreCreationContext context)
    {
        //Проверка того, что функция всегда возвращает значение.
        var funcReturnVisitor = new FuncMustReturnValueValidator();
        context = funcReturnVisitor.Validate(ast, context);
        
        //Вывод типов.
        var typeInferenceVisitor = new TypeInferenceWeaver();
        typeInferenceVisitor.WeaveDiffs(ast, context);

        //Проверка выражений на уровне body(нельзя запихать что попало)
        var bodyLevelValidator = new BodyLevelExpressionValidator();
        bodyLevelValidator.Validate(ast, context);

        //Break и continue только в цикле
        var flowControlInsideLoopValidator = new FlowControlInsideLoopValidator();
        flowControlInsideLoopValidator.Validate(ast, context);
        
        return new(ast);
    }

    public static async Task<AstParsingRes> RunAstParsing(StreamReader fileStream, string fileName)
    {
        var (ast, context) = await RunParsingAsync(fileStream, fileName);
        
        var dependencies = new List<ISymTable> { Builtins.SymTable };
        var symTableBuilder = new SymTableBuilder() { ModuleName = "<undefined>" };
        var symTableCtx = new SymbolTableBuildingContext(context.TranslationTable, dependencies, symTableBuilder);
        ast = RunSymTableBuilding(ast, symTableCtx).Ast;

        dependencies.Add(symTableBuilder);
        var preCreationContext = new PreCreationContext(context.TranslationTable, dependencies);
        _ = RunModulePreCreate(ast, preCreationContext).Ast;
        
        var totalExceptions = context.Exceptions
            .Concat(symTableCtx.Exceptions)
            .Concat(preCreationContext.Exceptions)
            .ToList();
        
        return new AstParsingRes(totalExceptions, symTableCtx.SymTableBuilder);
    }
    
    public record struct ParsingResult(NodeBase Ast, ParsingContext Context);

    public record struct SymTableBuildingResult(NodeBase Ast);

    public record struct PreCreationResult(NodeBase Ast);
    
    public record struct AstParsingRes(List<PlampException> Exceptions, ISymTableBuilder CurrentModuleBuilder);
}