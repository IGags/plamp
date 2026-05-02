using AutoFixture;
using plamp.Alternative.Parsing;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Parsing;

/// <summary>
/// Проверяет комментарии.
/// Здесь тестируется только то, что комментарии не ломают разбор файла
/// и сохраняются в таблице трансляции
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
        context.TranslationTable.Comments.Count.ShouldBe(4);
    }

    /// <summary>
    /// Парсер должен сохранять комментарии в таблице трансляции вместе с исходным текстом
    /// </summary>
    [Fact]
    public void ParseFile_StoresCommentsInTranslationTable()
    {
        const string code = """
                            // заголовок
                            module math;
                            /* тело */
                            """;
        var fixture = new Fixture { Customizations = { new ParserContextCustomization(code) } };
        var context = fixture.Create<ParsingContext>();

        Parser.ParseFile(context);

        context.Exceptions.ShouldBeEmpty();
        context.TranslationTable.Comments.Count.ShouldBe(2);
        context.TranslationTable.Comments[0].Text.ShouldBe("// заголовок");
        context.TranslationTable.Comments[1].Text.ShouldBe("/* тело */");
        context.TranslationTable.Comments[0].Position.ByteOffset.ShouldBe(0);
        context.TranslationTable.Comments[1].Position.ByteOffset.ShouldBeGreaterThan(context.TranslationTable.Comments[0].Position.ByteOffset);
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
        context.TranslationTable.Comments.Count.ShouldBe(2);
    }

    /// <summary>
    /// Незакрытый многострочный комментарий не должен ломать уже разобранный код
    /// и должен скрывать всё, что идёт после него, до конца файла
    /// </summary>
    [Fact]
    public void ParseFile_UnclosedMultiLineComment_DoesNotBreakCodeBeforeComment()
    {
        const string code = """
                            module math;

                            fn first() {}
                            /* сломанный комментарий
                            fn second() {}
                            """;
        var fixture = new Fixture { Customizations = { new ParserContextCustomization(code) } };
        var context = fixture.Create<ParsingContext>();

        var result = Parser.ParseFile(context);

        result.Functions.Count.ShouldBe(1);
        result.Functions[0].FuncName.Value.ShouldBe("first");
        context.Exceptions.ShouldContain(x => x.Code == PlampExceptionInfo.CommentIsNotClosed().Code);
        context.TranslationTable.Comments.Count.ShouldBe(1);
        context.TranslationTable.Comments[0].Text.ShouldContain("fn second()");
    }
}