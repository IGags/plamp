using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.Xunit2;
using plamp.Abstractions.Ast;
using plamp.Alternative.Tokenization;
using plamp.Alternative.Tokenization.Enums;
using plamp.Alternative.Tokenization.Token;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Tokenization;

public class TokenizerTests
{
    private const int Utf16ByteCharacterByteCount = 2;
    
    [Theory, AutoData]
    public async Task TestEmptyString(string fileName)
    {
        using var stream = new MemoryStream([]);
        using var reader = new StreamReader(stream, Encoding.Unicode);
        var result = await Tokenizer.TokenizeAsync(reader, fileName);
        Assert.Single(result.Sequence);
        Assert.Equal(typeof(EndOfFile), result.Sequence.First().GetType());
    }

    public static IEnumerable<object[]> SingleToken_Correct_DataProvider()
    {
        yield return [";", typeof(EndOfStatement)];
        yield return [" ", typeof(WhiteSpace), new Predicate<TokenBase>(t => ((WhiteSpace)t).Kind == WhiteSpaceKind.WhiteSpace)];
        yield return ["// comment", typeof(WhiteSpace), new Predicate<TokenBase>(t =>
        {
            var whiteSpace = (WhiteSpace)t;
            return whiteSpace.Kind == WhiteSpaceKind.SingleLineComment
                   && whiteSpace.GetStringRepresentation() == "// comment";
        })];
        yield return ["/*comment*/", typeof(WhiteSpace), new Predicate<TokenBase>(t =>
        {
            var whiteSpace = (WhiteSpace)t;
            return whiteSpace.Kind == WhiteSpaceKind.MultiLineComment
                   && whiteSpace.GetStringRepresentation() == "/*comment*/";
        })];
        yield return ["/*line1\nline2*/", typeof(WhiteSpace), new Predicate<TokenBase>(t =>
        {
            var whiteSpace = (WhiteSpace)t;
            return whiteSpace.Kind == WhiteSpaceKind.MultiLineComment
                   && whiteSpace.GetStringRepresentation() == "/*line1\nline2*/";
        })];
        yield return ["abc", typeof(Word)];
        yield return ["a1", typeof(Word)];
        yield return ["A", typeof(Word)];
        yield return ["\"\"", typeof(Literal), new Predicate<TokenBase>(t =>
        {
            var literal = (Literal)t;
            return literal.GetStringRepresentation() == "\"\""
                   && literal.ActualType.Equals(Builtins.String)
                   && (string)literal.ActualValue == "";
        })];
        yield return ["\"abc\"", typeof(Literal), new Predicate<TokenBase>(t =>
        {
            var literal = (Literal)t;
            return literal.GetStringRepresentation() == "\"abc\""
                   && literal.ActualType.Equals(Builtins.String)
                   && (string)literal.ActualValue == "abc";
        })];
        yield return ["\"123\"", typeof(Literal), new Predicate<TokenBase>(t =>
        {
            var literal = (Literal)t;
            return literal.GetStringRepresentation() == "\"123\""
                   && literal.ActualType.Equals(Builtins.String)
                   && (string)literal.ActualValue == "123";
        })];
        yield return ["\"a1\"", typeof(Literal), new Predicate<TokenBase>(t =>
        {
            var literal = (Literal)t;
            return literal.GetStringRepresentation() == "\"a1\""
                   && literal.ActualType.Equals(Builtins.String)
                   && (string)literal.ActualValue == "a1";
        })];
        yield return ["\"1a\"", typeof(Literal), new Predicate<TokenBase>(t =>
        {
            var literal = (Literal)t;
            return literal.GetStringRepresentation() == "\"1a\""
                   && literal.ActualType.Equals(Builtins.String)
                   && (string)literal.ActualValue == "1a";
        })];
        yield return ["\"!@#№;$%:^?&*)(_-+={}[]/,.'~<>\"", typeof(Literal), new Predicate<TokenBase>(t =>
        {
            var literal = (Literal)t;
            return literal.GetStringRepresentation() == "\"!@#№;$%:^?&*)(_-+={}[]/,.'~<>\""
                   && literal.ActualType.Equals(Builtins.String)
                   && (string)literal.ActualValue == "!@#№;$%:^?&*)(_-+={}[]/,.'~<>";
        })];
        yield return ["\"\\n\"", typeof(Literal), new Predicate<TokenBase>(t =>
        {
            var literal = (Literal)t;
            return literal.GetStringRepresentation() == "\"\n\""
                   && literal.ActualType.Equals(Builtins.String)
                   && (string)literal.ActualValue == "\n";
        })];
        yield return ["\"\\\\\"", typeof(Literal), new Predicate<TokenBase>(t =>
        {
            var literal = (Literal)t;
            return literal.GetStringRepresentation() == "\"\\\""
                   && literal.ActualType.Equals(Builtins.String)
                   && (string)literal.ActualValue == "\\";
        })];
        yield return ["\"\\r\"", typeof(Literal), new Predicate<TokenBase>(t =>
        {
            var literal = (Literal)t;
            return literal.GetStringRepresentation() == "\"\r\""
                   && literal.ActualType.Equals(Builtins.String)
                   && (string)literal.ActualValue == "\r";
        })];
        yield return ["\"\\t\"", typeof(Literal), new Predicate<TokenBase>(t =>
        {
            var literal = (Literal)t;
            return literal.GetStringRepresentation() == "\"\t\""
                   && literal.ActualType.Equals(Builtins.String)
                   && (string)literal.ActualValue == "\t";
        })];
        yield return ["\"\\\"\"", typeof(Literal), new Predicate<TokenBase>(t =>
        {
            var literal = (Literal)t;
            return literal.GetStringRepresentation() == "\"\"\""
                   && literal.ActualType.Equals(Builtins.String)
                   && (string)literal.ActualValue == "\"";
        })];
        yield return ["[", typeof(OpenSquareBracket)];
        yield return ["]", typeof(CloseSquareBracket)];
        yield return ["(", typeof(OpenParen)];
        yield return [")", typeof(CloseParen)];
        yield return [",", typeof(Comma)];
        yield return [":", typeof(Colon)];
        yield return [":=", typeof(OperatorToken), new Predicate<TokenBase>(t => ((OperatorToken)t).Operator == OperatorEnum.Assign)];
        yield return ["++", typeof(OperatorToken), new Predicate<TokenBase>(t => ((OperatorToken)t).Operator == OperatorEnum.Increment)];
        yield return ["--", typeof(OperatorToken), new Predicate<TokenBase>(t => ((OperatorToken)t).Operator == OperatorEnum.Decrement)];
        yield return ["=", typeof(OperatorToken), new Predicate<TokenBase>(t => ((OperatorToken)t).Operator == OperatorEnum.Equals)];
        yield return ["!=", typeof(OperatorToken), new Predicate<TokenBase>(t => ((OperatorToken)t).Operator == OperatorEnum.NotEquals)];
        yield return ["<=", typeof(OperatorToken), new Predicate<TokenBase>(t => ((OperatorToken)t).Operator == OperatorEnum.LesserOrEquals)];
        yield return [">=", typeof(OperatorToken), new Predicate<TokenBase>(t => ((OperatorToken)t).Operator == OperatorEnum.GreaterOrEquals)];
        yield return ["&&", typeof(OperatorToken), new Predicate<TokenBase>(t => ((OperatorToken)t).Operator == OperatorEnum.And)];
        yield return ["||", typeof(OperatorToken), new Predicate<TokenBase>(t => ((OperatorToken)t).Operator == OperatorEnum.Or)];
        yield return ["+", typeof(OperatorToken), new Predicate<TokenBase>(t => ((OperatorToken)t).Operator == OperatorEnum.Add)];
        yield return ["-", typeof(OperatorToken), new Predicate<TokenBase>(t => ((OperatorToken)t).Operator == OperatorEnum.Sub)];
        yield return ["/", typeof(OperatorToken), new Predicate<TokenBase>(t => ((OperatorToken)t).Operator == OperatorEnum.Div)];
        yield return ["*", typeof(OperatorToken), new Predicate<TokenBase>(t => ((OperatorToken)t).Operator == OperatorEnum.Mul)];
        yield return ["<", typeof(OperatorToken), new Predicate<TokenBase>(t => ((OperatorToken)t).Operator == OperatorEnum.Lesser)];
        yield return [">", typeof(OperatorToken), new Predicate<TokenBase>(t => ((OperatorToken)t).Operator == OperatorEnum.Greater)];
        yield return [".", typeof(OperatorToken), new Predicate<TokenBase>(t => ((OperatorToken)t).Operator == OperatorEnum.Access)];
        yield return ["!", typeof(OperatorToken), new Predicate<TokenBase>(t => ((OperatorToken)t).Operator == OperatorEnum.Not)];
        yield return ["use", typeof(KeywordToken), new Predicate<TokenBase>(t => ((KeywordToken)t).Keyword == Keywords.Use)];
        yield return ["fn", typeof(KeywordToken), new Predicate<TokenBase>(t => ((KeywordToken)t).Keyword == Keywords.Fn)];
        yield return ["false", typeof(KeywordToken), new Predicate<TokenBase>(t => ((KeywordToken)t).Keyword == Keywords.False)];
        yield return ["true", typeof(KeywordToken), new Predicate<TokenBase>(t => ((KeywordToken)t).Keyword == Keywords.True)];
        yield return ["while", typeof(KeywordToken), new Predicate<TokenBase>(t => ((KeywordToken)t).Keyword == Keywords.While)];
        yield return ["if", typeof(KeywordToken), new Predicate<TokenBase>(t => ((KeywordToken)t).Keyword == Keywords.If)];
        yield return ["as", typeof(KeywordToken), new Predicate<TokenBase>(t => ((KeywordToken)t).Keyword == Keywords.As)];
        yield return ["module", typeof(KeywordToken), new Predicate<TokenBase>(t => ((KeywordToken)t).Keyword == Keywords.Module)];
        yield return ["type", typeof(KeywordToken), new Predicate<TokenBase>(t => ((KeywordToken)t).Keyword == Keywords.Type)];
        yield return ["else", typeof(KeywordToken), new Predicate<TokenBase>(t => ((KeywordToken)t).Keyword == Keywords.Else)];
        yield return ["null", typeof(KeywordToken), new Predicate<TokenBase>(t => ((KeywordToken)t).Keyword == Keywords.Null)];
        yield return ["return", typeof(KeywordToken), new Predicate<TokenBase>(t => ((KeywordToken)t).Keyword == Keywords.Return)];
        yield return ["break", typeof(KeywordToken), new Predicate<TokenBase>(t => ((KeywordToken)t).Keyword == Keywords.Break)];
        yield return ["continue", typeof(KeywordToken), new Predicate<TokenBase>(t => ((KeywordToken)t).Keyword == Keywords.Continue)];
        yield return ["type", typeof(KeywordToken), new Predicate<TokenBase>(t => ((KeywordToken)t).Keyword == Keywords.Type)];
        yield return ["1", typeof(Literal), new Predicate<TokenBase>(t => ((Literal)t).ActualType.Equals(Builtins.Int) && (int)((Literal)t).ActualValue == 1)];
        yield return ["0", typeof(Literal), new Predicate<TokenBase>(t => ((Literal)t).ActualType.Equals(Builtins.Int) && (int)((Literal)t).ActualValue == 0)];
        yield return ["1i", typeof(Literal), new Predicate<TokenBase>(t => ((Literal)t).ActualType.Equals(Builtins.Int) && (int)((Literal)t).ActualValue == 1)];
        yield return ["1ui", typeof(Literal), new Predicate<TokenBase>(t => ((Literal)t).ActualType.Equals(Builtins.Uint) && (uint)((Literal)t).ActualValue == 1)];
        yield return ["5000000000", typeof(Literal), new Predicate<TokenBase>(t => ((Literal)t).ActualType.Equals(Builtins.Long) && (long)((Literal)t).ActualValue == 5000000000)];
        yield return ["1l", typeof(Literal), new Predicate<TokenBase>(t => ((Literal)t).ActualType.Equals(Builtins.Long) && (long)((Literal)t).ActualValue == 1)];
        yield return ["1ul", typeof(Literal), new Predicate<TokenBase>(t => ((Literal)t).ActualType.Equals(Builtins.Ulong) && (ulong)((Literal)t).ActualValue == 1)];
        yield return ["1.0", typeof(Literal), new Predicate<TokenBase>(t => ((Literal)t).ActualType.Equals(Builtins.Double) && Math.Abs((double)((Literal)t).ActualValue - 1) < 1e-9)];
        yield return ["1d", typeof(Literal), new Predicate<TokenBase>(t => ((Literal)t).ActualType.Equals(Builtins.Double) && Math.Abs((double)((Literal)t).ActualValue - 1) < 1e-9)];
        yield return ["1f", typeof(Literal), new Predicate<TokenBase>(t => ((Literal)t).ActualType.Equals(Builtins.Float) && Math.Abs((float)((Literal)t).ActualValue - 1) < 1e-5)];
        yield return ["1b", typeof(Literal), new Predicate<TokenBase>(t => ((Literal)t).ActualType.Equals(Builtins.Byte) && (byte)((Literal)t).ActualValue == 1)];
    }
    
    [Theory]
    [MemberData(nameof(SingleToken_Correct_DataProvider))]
    public async Task SingleToken_Correct<T>(string code, Type tokenType, Predicate<TokenBase>? condition = null)
    {
        var fileName = new Fixture().Create<string>();
        using var stream = new MemoryStream(Encoding.Unicode.GetBytes(code));
        using var reader = new StreamReader(stream, Encoding.Unicode);
        var result = await Tokenizer.TokenizeAsync(reader, fileName);
        Assert.Equal(3, result.Sequence.Count());
        Assert.IsType(tokenType, result.Sequence.First());
        Assert.Empty(result.Exceptions);
        if (condition != null)
        {
            Assert.True(condition(result.Sequence.First()));
        }
        Assert.Equal(0, result.Sequence.First().Position.ByteOffset);
        Assert.Equal(code.Length, result.Sequence.First().Position.CharacterLength);
    }

    private const string FileName = "example.plp";
    
    public static IEnumerable<object[]> Tokenization_ReturnsError_DataProvider()
    {
        yield return ["\"", new List<PlampException>{new(PlampExceptionInfo.StringIsNotClosed(), new FilePosition(0, 1, FileName))}];
        yield return ["\"\n", new List<PlampException>{new(PlampExceptionInfo.StringIsNotClosed(), new FilePosition(0, 1, FileName))}];
        yield return ["\"\r", new List<PlampException>{new(PlampExceptionInfo.StringIsNotClosed(), new FilePosition(0, 1, FileName))}];
        yield return ["\"\\x\"", new List<PlampException>{new(PlampExceptionInfo.InvalidEscapeSequence("\\x"), new FilePosition(Utf16ByteCharacterByteCount, 2, FileName))}];
        yield return ["@", new List<PlampException>{new(PlampExceptionInfo.UnexpectedToken("@"), new FilePosition(0, 1, FileName))}];
        yield return ["1.0i", new List<PlampException>{new (PlampExceptionInfo.UnknownNumberFormat(), new FilePosition(0, 4, FileName))}];
        yield return ["1fic", new List<PlampException>{new (PlampExceptionInfo.UnknownNumberFormat(), new FilePosition(0, 4, FileName))}];
        yield return ["/* comment", new List<PlampException>{new(PlampExceptionInfo.CommentIsNotClosed(), new FilePosition(0, 10, FileName))}];
        yield return ["\"\\x", new List<PlampException>
        {
            new (PlampExceptionInfo.InvalidEscapeSequence("\\x"), new FilePosition(Utf16ByteCharacterByteCount, 2, FileName)),
            new (PlampExceptionInfo.StringIsNotClosed(), new FilePosition(0, 3, FileName))
        }];
        yield return ["@\"", new List<PlampException>
        {
            new (PlampExceptionInfo.UnexpectedToken("@"), new FilePosition(0, 1, FileName)),
            new (PlampExceptionInfo.StringIsNotClosed(), new FilePosition(Utf16ByteCharacterByteCount, 1, FileName))
        }];
        yield return ["@\"\\x", new List<PlampException>
        {
            new (PlampExceptionInfo.UnexpectedToken("@"), new FilePosition(0, 1, FileName)),
            new (PlampExceptionInfo.InvalidEscapeSequence("\\x"), new FilePosition(Utf16ByteCharacterByteCount * 2, 2, FileName)),
            new (PlampExceptionInfo.StringIsNotClosed(), new FilePosition(Utf16ByteCharacterByteCount, 3, FileName))
        }];
    }
    
    [Theory]
    [MemberData(nameof(Tokenization_ReturnsError_DataProvider))]
    public async Task Tokenization_ReturnsError(string code, List<PlampException> expectedExceptions)
    {
        using var stream = new MemoryStream(Encoding.Unicode.GetBytes(code));
        using var reader = new StreamReader(stream, Encoding.Unicode);
        var result = await Tokenizer.TokenizeAsync(reader, FileName);
        Assert.Equal(expectedExceptions.Count, result.Exceptions.Count);
        foreach (var exception in expectedExceptions.Zip(result.Exceptions))
        {
            Assert.Equal(exception.First.FilePosition, exception.Second.FilePosition);
            Assert.Equal(exception.First.Message, exception.Second.Message);
            Assert.Equal(exception.First.Code, exception.Second.Code);
            Assert.Equal(exception.First.Level, exception.Second.Level);
        }
    }

    [Theory]
    [InlineData(": =")]
    [InlineData(": :=")]
    public async Task ParseColonVariations_Correct(string sequence)
    {
        using var stream = new MemoryStream(Encoding.Unicode.GetBytes(sequence));
        using var reader = new StreamReader(stream, Encoding.Unicode);
        var result = await Tokenizer.TokenizeAsync(reader, FileName);
        result.Exceptions.ShouldBeEmpty();
        result.Sequence.Current().ShouldBeOfType<Colon>();
    }

    /// <summary>
    /// Проверяет, что однострочный комментарий заканчивается на переводе строки
    /// и не хавает токены следующей строки
    /// </summary>
    [Fact]
    public async Task Tokenization_SingleLineComment_DoesNotSwallowNextLine()
    {
        const string code = """
                            a // comment
                            b
                            """;
        using var stream = new MemoryStream(Encoding.Unicode.GetBytes(code));
        using var reader = new StreamReader(stream, Encoding.Unicode);

        var result = await Tokenizer.TokenizeAsync(reader, FileName);

        result.Exceptions.ShouldBeEmpty();
        result.Sequence.Select(x => x.GetStringRepresentation()).ShouldContain("// comment");
        result.Sequence.Count(x => x is WhiteSpace { Kind: WhiteSpaceKind.LineBreak }).ShouldBeGreaterThanOrEqualTo(2);
        result.Sequence.OfType<Word>().Select(x => x.GetStringRepresentation()).ShouldBe(["a", "b"]);
    }

    /// <summary>
    /// Проверяет, что многострочный комментарий внутри выражения не ломает разбор соседних токенов
    /// </summary>
    [Fact]
    public async Task Tokenization_MultiLineComment_Inline_DoesNotBreakNextToken()
    {
        const string code = "a/*comment*/:=11";
        using var stream = new MemoryStream(Encoding.Unicode.GetBytes(code));
        using var reader = new StreamReader(stream, Encoding.Unicode);

        var result = await Tokenizer.TokenizeAsync(reader, FileName);

        result.Exceptions.ShouldBeEmpty();
        result.Sequence.OfType<WhiteSpace>().ShouldContain(x =>
            x.Kind == WhiteSpaceKind.MultiLineComment && x.GetStringRepresentation() == "/*comment*/");
        result.Sequence.OfType<OperatorToken>().ShouldContain(x => x.Operator == OperatorEnum.Assign);
        result.Sequence.OfType<Literal>().ShouldContain(x => Equals(x.ActualValue, 11));
    }
}