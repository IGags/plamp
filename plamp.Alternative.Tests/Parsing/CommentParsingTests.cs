using AutoFixture;
using plamp.Alternative.Parsing;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Parsing;

/// <summary>
/// Проверяет корректную обработку комментариев парсером
/// </summary>
public class CommentParsingTests
{
    /// <summary>
    /// Комментарии между значимыми токенами не должны ломать разбор файла
    /// </summary>
    [Fact]
    public void ParseFile_WithCommentsBetweenTokens_Correct()
    {
        const string code = """
                            module math; // модуль

                            /* описание */
                            fn sum(a: int) int {
                                /* перед возвратом */
                                return /* значение */ a;
                            }
                            """;
        var fixture = new Fixture { Customizations = { new ParserContextCustomization(code) } };
        var context = fixture.Create<ParsingContext>();

        var result = Parser.ParseFile(context);

        context.Exceptions.ShouldBeEmpty();
        result.ModuleName.ShouldNotBeNull();
        result.Functions.Count.ShouldBe(1);
        result.Comments.Count.ShouldBe(4);
    }

    /// <summary>
    /// Парсер должен сохранять комментарии в корневом узле AST вместе с исходным текстом
    /// </summary>
    [Fact]
    public void ParseFile_StoresCommentsInRootNode()
    {
        const string code = """
                            // заголовок
                            module math;
                            /* тело */
                            """;
        var fixture = new Fixture { Customizations = { new ParserContextCustomization(code) } };
        var context = fixture.Create<ParsingContext>();

        var result = Parser.ParseFile(context);

        context.Exceptions.ShouldBeEmpty();
        result.Comments.Count.ShouldBe(2);
        result.Comments[0].Text.ShouldBe("// заголовок");
        result.Comments[1].Text.ShouldBe("/* тело */");
        result.Comments[0].Position.ByteOffset.ShouldBe(0);
        result.Comments[1].Position.ByteOffset.ShouldBeGreaterThan(result.Comments[0].Position.ByteOffset);
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
        var fixture = new Fixture { Customizations = { new ParserContextCustomization(code) } };
        var context = fixture.Create<ParsingContext>();

        var result = Parser.ParseFile(context);

        context.Exceptions.ShouldBeEmpty();
        result.Imports.ShouldBeEmpty();
        result.Functions.ShouldBeEmpty();
        result.Types.ShouldBeEmpty();
        result.ModuleName.ShouldBeNull();
        result.Comments.Count.ShouldBe(2);
    }
}