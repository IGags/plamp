using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using plamp.Alternative.Tokenization;
using plamp.Alternative.Tokenization.Enums;
using plamp.Alternative.Tokenization.Token;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Tokenization;

/// <summary>
/// Проверяет токенизацию комментариев
/// </summary>
public class CommentTokenizationTests
{
    private const string FileName = "test.plp";

    /// <summary>
    /// Однострочный комментарий должен завершаться переводом строки и не скрывать следующую строку
    /// </summary>
    [Fact]
    public async Task Tokenization_SingleLineComment_DoesNotSwallowNextLine()
    {
        const string code = """
                            a // comment
                            b
                            """;

        var result = await TokenizeAsync(code);

        result.Exceptions.ShouldBeEmpty();
        result.Sequence.Select(x => x.GetStringRepresentation()).ShouldNotContain("// comment");
        result.Sequence.Count(x => x is WhiteSpace { Kind: WhiteSpaceKind.LineBreak }).ShouldBeGreaterThanOrEqualTo(2);
        result.Sequence.OfType<Word>().Select(x => x.GetStringRepresentation()).ShouldBe(["a", "b"]);
    }

    /// <summary>
    /// Многострочный комментарий внутри выражения не должен поломать выражение
    /// </summary>
    [Fact]
    public async Task Tokenization_MultiLineComment_Inline_DoesNotBreakNextToken()
    {
        const string code = "a/*comment*/:=11";

        var result = await TokenizeAsync(code);

        result.Exceptions.ShouldBeEmpty();
        result.Sequence.Select(x => x.GetStringRepresentation()).ShouldNotContain("/*comment*/");
        result.Sequence.OfType<OperatorToken>().ShouldContain(x => x.Operator == OperatorEnum.Assign);
        result.Sequence.OfType<Literal>().ShouldContain(x => Equals(x.ActualValue, 11));
    }

    /// <summary>
    /// Маркер многострочного комментария внутри однострочного не должен запускать многострочный комментарий и оставаться частью однострочного
    /// </summary>
    [Fact]
    public async Task Tokenization_SingleLineComment_CanContainMultiLineCommentMarker()
    {
        const string code = "// comment with /* marker";

        var result = await TokenizeAsync(code);

        result.Exceptions.ShouldBeEmpty();
        result.Sequence.Select(x => x.GetStringRepresentation()).ShouldNotContain(code);
        result.Sequence.OfType<WhiteSpace>().ShouldNotContain(x => x.GetStringRepresentation().Contains("comment"));
    }

    /// <summary>
    /// Маркер однострочного комментария внутри многострочного должен оставаться текстом многострочного комментария
    /// </summary>
    [Fact]
    public async Task Tokenization_MultiLineComment_CanContainSingleLineCommentMarker()
    {
        const string code = "/* comment with // marker */";

        var result = await TokenizeAsync(code);

        result.Exceptions.ShouldBeEmpty();
        result.Sequence.Select(x => x.GetStringRepresentation()).ShouldNotContain(code);
        result.Sequence.OfType<WhiteSpace>().ShouldNotContain(x => x.GetStringRepresentation().Contains("comment"));
    }

    /// <summary>
    /// Повторный открывающий маркер многострочного комментария должен оставаться текстом первого комментария
    /// </summary>
    [Fact]
    public async Task Tokenization_MultiLineComment_CanContainMultiLineCommentMarker()
    {
        const string code = "/* outer /* inner */";

        var result = await TokenizeAsync(code);

        result.Exceptions.ShouldBeEmpty();
        result.Sequence.Select(x => x.GetStringRepresentation()).ShouldNotContain(code);
        result.Sequence.OfType<WhiteSpace>().ShouldNotContain(x => x.GetStringRepresentation().Contains("outer"));
    }

    /// <summary>
    /// Незакрытый многострочный комментарий должен возвращать исключение и скрывать текст до конца файла
    /// </summary>
    [Fact]
    public async Task Tokenization_UnclosedMultiLineComment_ReturnsCommentIsNotClosed()
    {
        const string code = """
                            a
                            /* comment
                            @"
                            """;

        var result = await TokenizeAsync(code);

        result.Exceptions.Count.ShouldBe(1);
        result.Exceptions.Single().Code.ShouldBe(PlampExceptionInfo.CommentIsNotClosed().Code);
        result.Sequence.OfType<Word>().Single().GetStringRepresentation().ShouldBe("a");
        result.Sequence.Select(x => x.GetStringRepresentation()).ShouldNotContain("@");
    }

    /// <summary>
    /// Закрывающий маркер без открытого многострочного комментария должен создавать исключение
    /// </summary>
    [Fact]
    public async Task Tokenization_ClosingMultiLineCommentWithoutOpening_ReturnsUnexpectedToken()
    {
        const string code = "/* comment */ */";

        var result = await TokenizeAsync(code);

        result.Exceptions.Count.ShouldBe(1);
        result.Exceptions.Single().Code.ShouldBe(PlampExceptionInfo.UnexpectedToken("*/").Code);
        result.Exceptions.Single().Message.ShouldBe(PlampExceptionInfo.UnexpectedToken("*/").Message);
        result.Sequence.OfType<OperatorToken>().ShouldBeEmpty();
    }

    /// <summary>
    /// Токенизирует код
    /// </summary>
    private static async Task<TokenizationResult> TokenizeAsync(string code)
    {
        using var stream = new MemoryStream(Encoding.Unicode.GetBytes(code));
        using var reader = new StreamReader(stream, Encoding.Unicode);

        return await Tokenizer.TokenizeAsync(reader, FileName);
    }
}
