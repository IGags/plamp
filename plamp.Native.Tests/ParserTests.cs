using System;
using System.Linq;
using plamp.Native.Parsing;
using plamp.Native.Tokenization.Token;
using Xunit;

namespace plamp.Native.Tests;

public class ParserTests
{
    [Fact]
    public void TestAddUnexpectedToken()
    {
        var parser = new PlampNativeParser("");
        var shouldErrorText = $"{ParserErrorConstants.UnexpectedTokenPrefix} {nameof(Word)}";
        parser.AddUnexpectedToken<Word>();
        Assert.Single(parser.Exceptions);
        Assert.Equal(shouldErrorText, parser.Exceptions.First().Message);
    }

    [Theory]
    [InlineData("w", -1, 0)]
    [InlineData("w\n", -1, 1)]
    [InlineData("", -1, -1)]
    public void TestAdvanceToEndOfLineAndAddException(string code, int startPosition, int endPosition)
    {
        var parser = new PlampNativeParser(code);
        parser.AdvanceToEndOfLineAndAddException();
        Assert.Single(parser.Exceptions);
        Assert.Equal(ParserErrorConstants.ExpectedEndOfLine, parser.Exceptions.First().Message);
        Assert.Equal(startPosition, parser.Exceptions.First().StartPosition);
        Assert.Equal(endPosition, parser.Exceptions.First().EndPosition);
    }

    [Fact]
    public void TestAdvanceWithExceptionFromNotStartOfLine()
    {
        var parser = new PlampNativeParser("w a");
        parser.TokenSequence.GetNextToken();
        parser.AdvanceToEndOfLineAndAddException();
        Assert.Single(parser.Exceptions);
        Assert.Equal(ParserErrorConstants.ExpectedEndOfLine, parser.Exceptions.First().Message);
        Assert.Equal(1, parser.Exceptions.First().StartPosition);
        Assert.Equal(2, parser.Exceptions.First().EndPosition);
    }

    [Fact]
    public void TestAdvanceWithExceptionFromEndOfLine()
    {
        var parser = new PlampNativeParser("\n");
        parser.TokenSequence.GetNextToken();
        parser.AdvanceToEndOfLineAndAddException();
        Assert.Empty(parser.Exceptions);
    }

    [Fact]
    public void TestAddKeywordException()
    {
        var parser = new PlampNativeParser("");
        parser.AddKeywordException();
        Assert.Single(parser.Exceptions);
        Assert.Equal(ParserErrorConstants.CannotUseKeyword, parser.Exceptions.First().Message);
    }

    [Theory]
    [InlineData("", 0, 0, new[]{typeof(EndOfLine)}, null)]
    [InlineData("w", 1, 0, new[]{typeof(object)}, null)]
    [InlineData("w", 0, 0, new[]{typeof(Word)}, typeof(Word))]
    [InlineData("w\n", 1, 1, new[]{typeof(EndOfLine)}, typeof(EndOfLine))]
    [InlineData("w\n\t", 1, 1, new[]{typeof(EndOfLine), typeof(Scope)}, typeof(EndOfLine))]
    [InlineData("\nw\n", 2, 2, new[]{typeof(EndOfLine)}, typeof(EndOfLine))]
    public void TestAdvanceFirstOfTokens(string code, int position, int shift, Type[] typesToShift, Type targetToken)
    {
        var parser = new PlampNativeParser(code);
        for (int i = 0; i < shift; i++)
        {
            parser.TokenSequence.GetNextToken();
        }
        parser.AdvanceToFirstOfTokens(typesToShift.ToList());
        Assert.Equal(position, parser.TokenSequence.Position);
        Assert.Equal(targetToken, parser.TokenSequence.Current()?.GetType());
    }

    [Fact]
    public void TestAdvanceToRequestedWithEmptySequence()
    {
        var parser = new PlampNativeParser("");
        parser.AdvanceToRequestedToken<EndOfLine>();
        Assert.Equal(0, parser.TokenSequence.Position);
        Assert.Null(parser.TokenSequence.Current()?.GetType());
    }
    
    [Fact]
    public void TestAdvanceToRequestedToSelectedToken()
    {
        var parser = new PlampNativeParser("w");
        parser.AdvanceToRequestedToken<Word>();
        Assert.Equal(0, parser.TokenSequence.Position);
        Assert.Equal(typeof(Word), parser.TokenSequence.Current()?.GetType());
    }
    
    [Fact]
    public void TestAdvanceFromRequestedToSelectedToken()
    {
        var parser = new PlampNativeParser("w");
        parser.TokenSequence.GetNextToken();
        parser.AdvanceToRequestedToken<Word>();
        Assert.Equal(0, parser.TokenSequence.Position);
        Assert.Equal(typeof(Word), parser.TokenSequence.Current()?.GetType());
    }
    
    [Fact]
    public void TestAdvanceAfterRequestedToSelectedToken()
    {
        var parser = new PlampNativeParser("a w");
        parser.TokenSequence.GetNextToken();
        parser.TokenSequence.GetNextToken();
        parser.AdvanceToRequestedToken<Word>();
        Assert.Equal(2, parser.TokenSequence.Position);
        Assert.Equal(typeof(Word), parser.TokenSequence.Current()?.GetType());
    }

    [Theory]
    [InlineData("", 0, -1)]
    [InlineData("w", 0, -1)]
    [InlineData("w", 1, 0)]
    [InlineData("w ", 2, 0)]
    [InlineData("w", 2, 0)]
    [InlineData(" w", 1, -1)]
    public void TestRollBackToRequestedNonWhiteSpaceToken(string code, int shift, int position)
    {
        var parser = new PlampNativeParser(code);
        for (int i = 0; i < shift; i++)
        {
            parser.TokenSequence.GetNextToken();
        }
        parser.RollBackToRequestedNonWhiteSpaceToken<Word>();
        Assert.Equal(position, parser.TokenSequence.Position);
        if (parser.TokenSequence.Current() != null)
        {
            Assert.IsType<Word>(parser.TokenSequence.Current());
        }
    }
    
}