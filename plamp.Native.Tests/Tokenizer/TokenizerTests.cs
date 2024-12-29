using System;
using System.Collections.Generic;
using System.Linq;
using plamp.Native.Tokenization;
using plamp.Native.Tokenization.Token;
using Xunit;

namespace plamp.Native.Tests.Tokenizer;

public class TokenizerTests
{
    [Fact]
    public void TestEmptyString()
    {
        var result = "".Tokenize();
        Assert.Single(result.Sequence);
        Assert.Equal(typeof(EndOfLine), result.Sequence.First().GetType());
    }

    [Fact]
    public void TestNullSequence()
    {
        var result = ((string)null).Tokenize();
        Assert.Single(result.Sequence);
        Assert.Equal(typeof(EndOfLine), result.Sequence.First().GetType());
    }

    [Theory]
    [ClassData(typeof(TestSingleTokenProvider))]
    public void TestSingleToken<T>(string code, Type tokenType, Predicate<TokenBase> condition = null)
    {
        var result = code.Tokenize();
        Assert.Equal(2, result.Sequence.TokenList.Count);
        Assert.IsType(tokenType, result.Sequence.First());
        Assert.Empty(result.Exceptions);
        if (condition != null)
        {
            Assert.True(condition(result.Sequence.First()));
        }
        Assert.Equal(0, result.Sequence.First().Start.Column);
        Assert.Equal(code.Length - 1, result.Sequence.First().End.Column);
    }

    // \n => \r\n implicit
    [Fact]
    public void TestEndOfLineTokenization()
    {
        var result = "\n".Tokenize();
        Assert.Equal(2, result.Sequence.TokenList.Count);
        Assert.IsType<EndOfLine>(result.Sequence.First());
        var token = result.Sequence.First() as EndOfLine;
        Assert.Equal(1, token.End.Column);
        Assert.Equal("\r\n", token.GetStringRepresentation());
    }

    [Theory]
    [ClassData(typeof(TestParserErrorProvider))]
    public void TestParserErrors(string code, List<PlampException> expectedExceptions)
    {
        var result = code.Tokenize();
        Assert.Equal(expectedExceptions.Count, result.Exceptions.Count);
        foreach (var exception in expectedExceptions.Zip(result.Exceptions))
        {
            Assert.Equal(exception.First.StartPosition, exception.Second.StartPosition);
            Assert.Equal(exception.First.EndPosition, exception.Second.EndPosition);
            Assert.Equal(exception.First.Message, exception.Second.Message);
            Assert.Equal(exception.First.Code, exception.Second.Code);
            Assert.Equal(exception.First.Level, exception.Second.Level);
        }
    }

    /// <summary>
    /// Edge case
    /// </summary>
    [Fact]
    public void TestWhiteSpace()
    {
        var result = "w\t".Tokenize();
        Assert.Equal(3, result.Sequence.Count());
        Assert.Empty(result.Exceptions);
        Assert.IsType<WhiteSpace>(result.Sequence.TokenList[1]);
        Assert.Equal("\t", result.Sequence.TokenList[1].GetStringRepresentation());
    }
    
    [Theory]
    [InlineData("\n    ", new[]{typeof(EndOfLine), typeof(WhiteSpace), typeof(EndOfLine)})]
    [InlineData("        ", new[]{typeof(WhiteSpace), typeof(WhiteSpace), typeof(EndOfLine)})]
    [InlineData("     ", new[]{typeof(WhiteSpace), typeof(WhiteSpace) , typeof(EndOfLine)})]
    [InlineData("\t\t", new[]{typeof(WhiteSpace), typeof(WhiteSpace), typeof(EndOfLine)})]
    [InlineData("    \t", new[]{typeof(WhiteSpace), typeof(WhiteSpace), typeof(EndOfLine)})]
    [InlineData("\t    ", new[]{typeof(WhiteSpace), typeof(WhiteSpace), typeof(EndOfLine)})]
    [InlineData("    a    ", new[]{typeof(WhiteSpace), typeof(Word), typeof(WhiteSpace), typeof(EndOfLine)})]
    [InlineData("    a\t", new[]{typeof(WhiteSpace), typeof(Word), typeof(WhiteSpace), typeof(EndOfLine)})]
    [InlineData("\ta    ", new[]{typeof(WhiteSpace), typeof(Word), typeof(WhiteSpace), typeof(EndOfLine)})]
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
    
    [Theory]
    [InlineData("")]
    [InlineData("     1 + 2 + 3")]
    [InlineData("\"\" 222")]
    public void TestOverallConsistency(string code)
    {
        var codeLength = code.Length + 2; //Plus to because of implicit \r\n addition
        var tokenSequence = code.Tokenize();
        Assert.Empty(tokenSequence.Exceptions);
        var last = new TokenPosition(0, -1);
        foreach (var token in tokenSequence.Sequence)
        {
            Assert.True(
                (token.Start.Column == last.Column + 1 && token.Start.Row == last.Row) 
                || (token.Start.Row == last.Row + 1 && token.Start.Column == 0));
            last = token.End;
        }
    }
}