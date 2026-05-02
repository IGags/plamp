using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Symbols.SymTable;
using plamp.Alternative.Parsing;
using plamp.Alternative.SymbolsBuildingImpl;
using plamp.Alternative.Tokenization;
using plamp.Alternative.Visitors;
using plamp.Alternative.Visitors.ModuleCreation;
using plamp.Alternative.Visitors.ModulePreCreation;
using plamp.Alternative.Visitors.ModulePreCreation.FillReferenceArray;
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
using plamp.Alternative.Tokenization.Enums;
using plamp.Alternative.Tokenization.Token;

namespace plamp.Alternative;

public static class CompilationPipeline
{
    public static async Task<ParsingResult> RunParsingAsync(StreamReader fileStream, string fileName)
    {
        var tokenizationResult = await Tokenizer.TokenizeAsync(fileStream, fileName);
        var translationTable = new TranslationTable();
        AddCommentsToTranslationTable(tokenizationResult.Sequence, translationTable);
        var parsingContext = new ParsingContext(tokenizationResult.Sequence, tokenizationResult.Exceptions, translationTable);
        SkipLeadingTrivia(parsingContext.Sequence);
        var ast = Parser.ParseFile(parsingContext);
        return new ParsingResult(ast, parsingContext);
    }

    /// <summary>
    /// Переносит комментарии из потока токенов в таблицу трансляции
    /// </summary>
    /// <param name="sequence">Последовательность токенов исходного файла</param>
    /// <param name="translationTable">Таблица трансляции текущего файла</param>
    private static void AddCommentsToTranslationTable(TokenSequence sequence, ITranslationTable translationTable)
    {
        foreach (var token in sequence)
        {
            if (token is not WhiteSpace whiteSpace)
            {
                continue;
            }

            var kind = whiteSpace.Kind switch
            {
                WhiteSpaceKind.SingleLineComment => CommentKind.SingleLine,
                WhiteSpaceKind.MultiLineComment => CommentKind.MultiLine,
                _ => (CommentKind?)null
            };

            if (kind.HasValue)
            {
                translationTable.AddComment(new SourceComment(whiteSpace.GetStringRepresentation(), whiteSpace.Position, kind.Value));
            }
        }
    }

    /// <summary>
    /// Перед началом парсинга переводит текущую позицию последовательности на первый значимый токен
    /// </summary>
    /// <param name="sequence">Последовательность токенов файла</param>
    private static void SkipLeadingTrivia(TokenSequence sequence)
    {
        if (sequence.Current() is WhiteSpace)
        {
            sequence.MoveNextNonWhiteSpace();
        }
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

        //Заполнение массивов ссылочных типов.
        var fillRefArrayVisitor = new FillReferenceArrayWeaver();
        fillRefArrayVisitor.WeaveDiffs(ast, context);
        
        return new(ast);
    }

    public static CreationResult RunModuleCreation(NodeBase ast, CreationContext context)
    {
        var symbolCreator = new ModuleSymbolsCreationValidator();
        context = symbolCreator.Validate(ast, context);
        
        var fnSignatureVisitor = new FuncCreatorValidator();
        context = fnSignatureVisitor.Validate(ast, context);

        var compilationVisitor = new CompilationValidator();
        compilationVisitor.Validate(ast, context);
        context.ModuleBuilder.CreateGlobalFunctions();
        return new CreationResult(context.AssemblyBuilder);
    }

    public static async Task<CompilationRes> RunEntirePipelineAsync(StreamReader fileStream, string fileName)
    {
        var (ast, context) = await RunParsingAsync(fileStream, fileName);
        
        var dependencies = new List<ISymTable> { Builtins.SymTable };
        var symTableBuilder = new SymTableBuilder() { ModuleName = "<undefined>" };
        var symTableCtx = new SymbolTableBuildingContext(context.TranslationTable, dependencies, symTableBuilder);
        ast = RunSymTableBuilding(ast, symTableCtx).Ast;

        dependencies.Add(symTableBuilder);
        var preCreationContext = new PreCreationContext(context.TranslationTable, dependencies);
        ast = RunModulePreCreate(ast, preCreationContext).Ast;
        
        if (context.Exceptions.Count != 0 
            || symTableCtx.Exceptions.Count != 0 
            || preCreationContext.Exceptions.Count != 0)
        {
            var unitedExceptions = context.Exceptions
                .Concat(symTableCtx.Exceptions)
                .Concat(preCreationContext.Exceptions)
                .ToList();
            
            return new(unitedExceptions, null);
        }
        
        var assemblyName = new AssemblyName(DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule(symTableBuilder.ModuleName);

        var compilationContext = new CreationContext(assemblyBuilder, moduleBuilder, symTableBuilder, preCreationContext);
        var assembly = RunModuleCreation(ast, compilationContext).Compiled;
        return new CompilationRes([], assembly);
    }
    
    public record struct ParsingResult(NodeBase Ast, ParsingContext Context);

    public record struct SymTableBuildingResult(NodeBase Ast);

    public record struct PreCreationResult(NodeBase Ast);

    public record struct CreationResult(Assembly Compiled);
    
    public record struct CompilationRes(List<PlampException> Exceptions, Assembly? Compiled);
}