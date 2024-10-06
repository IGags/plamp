using System;
using System.Linq;
using plamp.Native.Tokenization;
using plamp.Native.Tokenization.Token;
using Xunit;

namespace plamp.Native.Tests;

//TODO: проверить позиции токенов
public class TokenizerTests
{
    [Fact]
    public void TestEmptyString()
    {
        var result = "".Tokenize();
        Assert.Empty(result.Sequence.TokenList);
        Assert.Empty(result.Exceptions);
    }

    [Fact]
    public void TestNullSequence()
    {
        var result = ((string)null).Tokenize();
        Assert.Empty(result.Sequence.TokenList);
        Assert.Empty(result.Exceptions);
    }

    [Theory]
    [InlineData(" ", typeof(WhiteSpace), " ")]
    [InlineData("\r", typeof(WhiteSpace), "\r")]
    [InlineData("    ", typeof(Scope), "    ")]
    [InlineData("\t", typeof(Scope), "\t")]
    [InlineData("abc", typeof(Word), "abc")]
    [InlineData("123", typeof(Word), "123")]
    [InlineData("a1", typeof(Word), "a1")]
    [InlineData("1a", typeof(Word), "1a")]
    [InlineData("\"\"", typeof(StringLiteral), "")]
    [InlineData("\"abc\"", typeof(StringLiteral), "abc")]
    [InlineData("\"123\"", typeof(StringLiteral), "123")]
    [InlineData("\"a1\"", typeof(StringLiteral), "a1")]
    [InlineData("\"1a\"", typeof(StringLiteral), "1a")]
    [InlineData("\"!@#№;$%:^?&*)(_-+={}[]/,.'~<>\"", typeof(StringLiteral), "!@#№;$%:^?&*)(_-+={}[]/,.'~<>")]
    [InlineData("\"\\n\"", typeof(StringLiteral), "\n")]
    [InlineData("\"\\\\\"", typeof(StringLiteral), "\\")]
    [InlineData("\"\\r\"", typeof(StringLiteral), "\r")]
    [InlineData("\"\\t\"", typeof(StringLiteral), "\t")]
    [InlineData("\"\\\"\"", typeof(StringLiteral), "\"")]
    [InlineData("[", typeof(OpenSquareBracket))]
    [InlineData("]", typeof(CloseSquareBracket))]
    [InlineData("(", typeof(OpenParen))]
    [InlineData(")", typeof(CloseParen))]
    [InlineData(",", typeof(Comma))]
    [InlineData("\r", typeof(WhiteSpace))]
    [InlineData("\n", typeof(EndOfLine), "\n")]
    [InlineData("\r\n", typeof(EndOfLine), PlampNativeTokenizer.EndOfLineCrlf)]
    [InlineData("+=", typeof(Operator))]
    [InlineData("-=", typeof(Operator))]
    [InlineData("++", typeof(Operator))]
    [InlineData("--", typeof(Operator))]
    [InlineData("*=", typeof(Operator))]
    [InlineData("/=", typeof(Operator))]
    [InlineData("==", typeof(Operator))]
    [InlineData("!=", typeof(Operator))]
    [InlineData("<=", typeof(Operator))]
    [InlineData(">=", typeof(Operator))]
    [InlineData("&&", typeof(Operator))]
    [InlineData("||", typeof(Operator))]
    [InlineData("%=", typeof(Operator))]
    [InlineData("+", typeof(Operator))]
    [InlineData("-", typeof(Operator))]
    [InlineData(".", typeof(Operator))]
    [InlineData("/", typeof(Operator))]
    [InlineData("*", typeof(Operator))]
    [InlineData("<", typeof(OpenAngleBracket))]
    [InlineData(">", typeof(CloseAngleBracket))]
    [InlineData("!", typeof(Operator))]
    [InlineData("%", typeof(Operator))]
    public void TestSingleToken(string code, Type resultToken, string inline = null)
    {
        var result = code.Tokenize();
        Assert.Single(result.Sequence);
        Assert.IsType(resultToken, result.Sequence.First());
        Assert.Empty(result.Exceptions);
        if (inline != null)
        {
            Assert.Equal(inline, result.Sequence.First().GetString());
        }
        Assert.Equal(0, result.Sequence.First().StartPosition);
        Assert.Equal(code.Length - 1, result.Sequence.First().EndPosition);
    }

    [Theory]
    [InlineData("\"", TokenizerErrorConstants.StringIsNotClosed, 0, 0, 1)]
    [InlineData("\"\n", TokenizerErrorConstants.StringIsNotClosed, 0, 0, 2)]
    [InlineData("\"\r\n", TokenizerErrorConstants.StringIsNotClosed, 0, 0, 2)]
    [InlineData("\"\\x\"", TokenizerErrorConstants.InvalidEscapeSequence, 1, 2, 1)]
    [InlineData("@", TokenizerErrorConstants.UnexpectedToken, 0, 0)]
    public void TestParserErrors(string code, string exceptionText, int startPos, int endPos, int count = 0)
    {
        var result = code.Tokenize();
        Assert.Equal(count, result.Sequence.Count());
        Assert.Single(result.Exceptions);
        Assert.Equal(exceptionText, result.Exceptions.First().Message);
        Assert.Equal(startPos, result.Exceptions.First().StartPosition);
        Assert.Equal(endPos, result.Exceptions.First().EndPosition);
    }
    
    [Theory]
    [InlineData("\"\\x", new[]{TokenizerErrorConstants.InvalidEscapeSequence, TokenizerErrorConstants.StringIsNotClosed}, new[]{1, 2, 0, 2})]
    [InlineData("@\"", new[]{TokenizerErrorConstants.UnexpectedToken, TokenizerErrorConstants.StringIsNotClosed}, new[]{0, 0, 1, 1})]
    [InlineData("@\"\\x", new[]
    {
        TokenizerErrorConstants.UnexpectedToken, TokenizerErrorConstants.InvalidEscapeSequence, TokenizerErrorConstants.StringIsNotClosed
    }, new[]{0, 0, 2, 3, 1, 3})]
    public void TestMultipleErrors(string code, string[] textList, int[] errorPosList)
    {
        var result = code.Tokenize();
        Assert.Equal(textList.Length, result.Exceptions.Count);
        for (int i = 0; i < textList.Length; i++)
        {
            Assert.Equal(textList[i], result.Exceptions[i].Message);
            Assert.Equal(errorPosList[i * 2], result.Exceptions[i].StartPosition);
            Assert.Equal(errorPosList[i * 2 + 1], result.Exceptions[i].EndPosition);
        }
    }

    /// <summary>
    /// Edge case
    /// </summary>
    [Fact]
    public void TestWhiteSpace()
    {
        var result = "w\t".Tokenize();
        Assert.Equal(2, result.Sequence.Count());
        Assert.Empty(result.Exceptions);
        Assert.IsType<WhiteSpace>(result.Sequence.TokenList[1]);
        Assert.Equal("\t", result.Sequence.TokenList[1].GetString());
    }
    
    [Theory]
    [InlineData("\n    ", new[]{typeof(EndOfLine), typeof(Scope)})]
    [InlineData("        ", new[]{typeof(Scope), typeof(Scope)})]
    [InlineData("     ", new[]{typeof(Scope), typeof(WhiteSpace)})]
    [InlineData("\t\t", new[]{typeof(Scope), typeof(Scope)})]
    [InlineData("    \t", new[]{typeof(Scope), typeof(Scope)})]
    [InlineData("\t    ", new[]{typeof(Scope), typeof(Scope)})]
    [InlineData("    a    ", new[]{typeof(Scope), typeof(Word), 
        typeof(WhiteSpace), typeof(WhiteSpace), typeof(WhiteSpace), typeof(WhiteSpace)})]
    [InlineData("    a\t", new[]{typeof(Scope), typeof(Word), typeof(WhiteSpace)})]
    [InlineData("\ta    ", new[]{typeof(Scope), typeof(Word), 
        typeof(WhiteSpace), typeof(WhiteSpace), typeof(WhiteSpace), typeof(WhiteSpace)})]
    public void TestScope(string code, Type[] sequenceShould)
    {
        var result = code.Tokenize();
        Assert.Empty(result.Exceptions);
        Assert.Equal(result.Sequence.Count(), sequenceShould.Length);
        for (var i = 0; i < sequenceShould.Length; i++)
        {
            Assert.IsType(sequenceShould[i], result.Sequence.TokenList[i]);
        }
    }

    [Fact]
    public void TestParseSequenceAfterEndOfLineSeparator()
    {
        var result = "\"l\nw".Tokenize();
        Assert.Single(result.Exceptions);
        Assert.Equal(TokenizerErrorConstants.StringIsNotClosed, result.Exceptions.First().Message);
        Assert.Equal(0, result.Exceptions.First().StartPosition);
        Assert.Equal(1, result.Exceptions.First().EndPosition);
        Assert.Equal(3, result.Sequence.Count());
        Assert.IsType<StringLiteral>(result.Sequence.TokenList[0]);
        Assert.Equal(0, result.Sequence.TokenList[0].StartPosition);
        Assert.Equal(1, result.Sequence.TokenList[0].EndPosition);
        Assert.IsType<EndOfLine>(result.Sequence.TokenList[1]);
        Assert.Equal(2, result.Sequence.TokenList[1].StartPosition);
        Assert.Equal(2, result.Sequence.TokenList[1].EndPosition);
        Assert.IsType<Word>(result.Sequence.TokenList[2]);
        Assert.Equal(3, result.Sequence.TokenList[2].StartPosition);
        Assert.Equal(3, result.Sequence.TokenList[2].EndPosition);
    }

    [Fact] 
    public void TestParseSequenceAfterEndOfLineCrlfSeparator()
    {
        var result = "\"l\r\nw".Tokenize();
        Assert.Single(result.Exceptions);
        Assert.Equal(TokenizerErrorConstants.StringIsNotClosed, result.Exceptions.First().Message);
        Assert.Equal(0, result.Exceptions.First().StartPosition);
        Assert.Equal(1, result.Exceptions.First().EndPosition);
        Assert.Equal(3, result.Sequence.Count());
        Assert.IsType<StringLiteral>(result.Sequence.TokenList[0]);
        Assert.Equal(0, result.Sequence.TokenList[0].StartPosition);
        Assert.Equal(1, result.Sequence.TokenList[0].EndPosition);
        Assert.IsType<EndOfLine>(result.Sequence.TokenList[1]);
        Assert.Equal(2, result.Sequence.TokenList[1].StartPosition);
        Assert.Equal(3, result.Sequence.TokenList[1].EndPosition);
        Assert.IsType<Word>(result.Sequence.TokenList[2]);
        Assert.Equal(4, result.Sequence.TokenList[2].StartPosition);
        Assert.Equal(4, result.Sequence.TokenList[2].EndPosition);
    }

    [Theory]
    [InlineData("")]
    [InlineData("@  ")]
    [InlineData("\"\n ")]
    [InlineData("     1 + 2 + 3")]
    [InlineData("     1 @ 2 @ 3")]
    [InlineData("   \"321\"  1 @ 2 @ 3")]
    [InlineData("\"\" 222")]
    public void TestOverallConsistency(string code)
    {
        var tokenSequence = code.Tokenize();
        var position = 0;
        for (; position < code.Length;)
        {
            var token = tokenSequence.Sequence.FirstOrDefault(x => x.StartPosition == position);
            var error = tokenSequence.Exceptions.FirstOrDefault(x => x.StartPosition == position);
            Assert.False(token == null && error == null);
            position = (token?.EndPosition ?? error.EndPosition) + 1;
        }
        Assert.Equal(position, code.Length);
    }
}