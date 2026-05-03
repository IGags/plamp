using System.Linq;
using plamp.Alternative.Parsing;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Parsing;

/// <summary>
/// Проверяет работу комментариев на уровне парсера
/// </summary>
public class CommentParsingTests
{
    /// <summary>
    /// Однострочный комментарий перед конструкцией верхнего уровня должен пропускаться
    /// </summary>
    [Fact]
    public void ParseFile_WithSingleLineCommentBeforeTopLevel_SkipsComment()
    {
        const string code = """
                            // комментарий к модулю
                            module math;
                            """;
        var context = CompilationPipelineBuilder.CreateParsingContext(code);

        var result = Parser.ParseFile(context);

        context.Exceptions.ShouldBeEmpty();
        result.ModuleName.ShouldNotBeNull();
        result.ModuleName.ModuleName.ShouldBe("math");
    }

    /// <summary>
    /// Однострочный комментарий после конструкции должен скрывать только остаток текущей строки
    /// </summary>
    [Fact]
    public void ParseFile_WithSingleLineCommentAfterStatement_SkipsLineTail()
    {
        const string code = """
                            module math; // fn foo() {}
                            fn bar() {}
                            """;
        var context = CompilationPipelineBuilder.CreateParsingContext(code);

        var result = Parser.ParseFile(context);

        context.Exceptions.ShouldBeEmpty();
        result.Functions.Count.ShouldBe(1);
        result.Functions[0].FuncName.Value.ShouldBe("bar");
    }

    /// <summary>
    /// Многострочный комментарий перед конструкцией верхнего уровня должен пропускаться
    /// </summary>
    [Fact]
    public void ParseFile_WithMultiLineCommentBeforeTopLevel_SkipsComment()
    {
        const string code = """
                            /*
                            module ignored;
                            */
                            module math;
                            """;
        var context = CompilationPipelineBuilder.CreateParsingContext(code);

        var result = Parser.ParseFile(context);

        context.Exceptions.ShouldBeEmpty();
        result.ModuleName.ShouldNotBeNull();
        result.ModuleName.ModuleName.ShouldBe("math");
    }

    /// <summary>
    /// Многострочный комментарий между значимыми токенами должен пропускаться
    /// </summary>
    [Fact]
    public void ParseFile_WithMultiLineCommentBetweenTokens_SkipsComment()
    {
        const string code = """
                            fn foo(value: int) int {
                                return /* комментарий внутри выражения */ value;
                            }
                            """;
        var context = CompilationPipelineBuilder.CreateParsingContext(code);

        var result = Parser.ParseFile(context);

        context.Exceptions.ShouldBeEmpty();
        result.Functions.Count.ShouldBe(1);
        result.Functions[0].FuncName.Value.ShouldBe("foo");
    }

    /// <summary>
    /// Файл, состоящий только из комментариев, должен разбираться без ошибок
    /// </summary>
    [Fact]
    public void ParseFile_WithOnlyComments_Correct()
    {
        const string code = """
                            // первый комментарий
                            /* второй комментарий */
                            """;
        var context = CompilationPipelineBuilder.CreateParsingContext(code);

        var result = Parser.ParseFile(context);

        context.Exceptions.ShouldBeEmpty();
        result.Imports.ShouldBeEmpty();
        result.Functions.ShouldBeEmpty();
        result.Types.ShouldBeEmpty();
        result.ModuleName.ShouldBeNull();
    }

    /// <summary>
    /// Если многострочный комментарий не закрыт, в контексте разбора должна остаться ошибка токенизации
    /// </summary>
    [Fact]
    public void ParseFile_WithUnclosedMultiLineComment_ReturnsCommentIsNotClosed()
    {
        const string code = """
                            module math;
                            /*
                            """;
        var context = CompilationPipelineBuilder.CreateParsingContext(code);

        Parser.ParseFile(context);

        context.Exceptions.Select(x => x.Code).ShouldContain(PlampExceptionInfo.CommentIsNotClosed().Code);
    }
}