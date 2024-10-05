using System;
using plamp.Native.Tokenization;
using plamp.Native.Tokenization.Token;
using Xunit;

namespace plamp.Native.Tests;

public class TokenSequenceTests
{
    [Theory]
    [InlineData("", int.MinValue, -1)]
    [InlineData("", 0, 0)]
    [InlineData("", int.MaxValue, 0)]
    [InlineData(" ", int.MinValue, -1)]
    [InlineData(" ", 0, 0)]
    [InlineData(" ", int.MaxValue, 1)]
    [InlineData("   ", 1, 1)]
    [InlineData("   ", 0, 0)]
    [InlineData("   ", 2, 2)]
    [InlineData("   ", int.MaxValue, 3)]
    [InlineData("   ", int.MinValue, -1)]
    public void TestSetPosition(string code, int setPos, int targetPos)
    {
        var sequence = code.Tokenize().Sequence;
        sequence.Position = setPos;
        Assert.Equal(targetPos, sequence.Position);
    }

    [Theory]
    [InlineData("", 1, 0, null)]
    [InlineData(" ", 1, 0, typeof(WhiteSpace))]
    [InlineData(" ", 2, 1, null)]
    [InlineData("\n ", 2, 1, typeof(WhiteSpace))]
    public void TestGetNextToken(string code, int shift, int position, Type resultType)
    {
        var sequence = code.Tokenize().Sequence;
        for (int i = 0; i < shift; i++)
        {
            sequence.GetNextToken();
        }
        Assert.Equal(position, sequence.Position);
        if (resultType == null)
        {
            Assert.Null(sequence.Current());
        }
        else
        {
            Assert.Equal(resultType, sequence.Current().GetType());
        }
    }

    [Theory]
    [InlineData("", 1, 0, null)]
    [InlineData(" ", 1, 1, null)]
    [InlineData("w", 1, 0, typeof(Word))]
    [InlineData("w", 2, 1, null)]
    [InlineData("w w", 2, 2, typeof(Word))]
    [InlineData(" w", 1, 1, typeof(Word))]
    public void TestGetNextNonWhiteSpace(string code, int shift, int position, Type resultType)
    {
        var sequence = code.Tokenize().Sequence;
        for (int i = 0; i < shift; i++)
        {
            sequence.GetNextNonWhiteSpace();
        }
        Assert.Equal(position, sequence.Position);
        if (resultType == null)
        {
            Assert.Null(sequence.Current());
        }
        else
        {
            Assert.Equal(resultType, sequence.Current().GetType());
        }
    }

    [Theory]
    [InlineData("", 0, -1, null)]
    [InlineData("", 1, 0, null)]
    [InlineData(" ", 1, 0, null)]
    [InlineData(" ", 0, -1, null)]
    [InlineData("w", 0, -1, typeof(Word))]
    [InlineData("w", 1, 0, null)]
    [InlineData(" w", 0, -1, typeof(Word))]
    [InlineData(" w", 1, 0, typeof(Word))]
    [InlineData("w \n", 1, 0, typeof(EndOfLine))]
    public void TestPeekNextNonWhiteSpace(string code, int shift, int position, Type resultType)
    {
        var sequence = code.Tokenize().Sequence;
        for (int i = 0; i < shift; i++)
        {
            sequence.GetNextToken();
        }

        var res = sequence.PeekNextNonWhiteSpace();
        Assert.Equal(position, sequence.Position);
        if (resultType == null)
        {
            Assert.Null(res);
        }
        else
        {
            Assert.Equal(resultType, res.GetType());
        }
    }

    [Theory]
    [InlineData("", 0, -1, null)]
    [InlineData("", 1, 0, null)]
    [InlineData(" ", 1, 0, null)]
    [InlineData(" ", 0, -1, typeof(WhiteSpace))]
    [InlineData("w", 0, -1, typeof(Word))]
    [InlineData("w", 1, 0, null)]
    [InlineData(" w", 0, -1, typeof(WhiteSpace))]
    [InlineData(" w", 1, 0, typeof(Word))]
    [InlineData("w \n", 1, 0, typeof(WhiteSpace))]
    public void TestPeekNext(string code, int shift, int position, Type resultType)
    {
        var sequence = code.Tokenize().Sequence;
        for (int i = 0; i < shift; i++)
        {
            sequence.GetNextToken();
        }

        var res = sequence.PeekNext();
        Assert.Equal(position, sequence.Position);
        if (resultType == null)
        {
            Assert.Null(res);
        }
        else
        {
            Assert.Equal(resultType, res.GetType());
        }
    }

    [Theory]
    [InlineData("", 0, null)]
    [InlineData("", 1, null)]
    [InlineData(" ", 0, null)]
    [InlineData(" ", 1, typeof(WhiteSpace))]
    [InlineData(" ", 2, null)]
    public void TestCurrent(string code, int shift, Type resultType)
    {
        var sequence = code.Tokenize().Sequence;
        for (int i = 0; i < shift; i++)
        {
            sequence.GetNextToken();
        }

        var res = sequence.Current();
        if (resultType == null)
        {
            Assert.Null(res);
        }
        else
        {
            Assert.Equal(resultType, res.GetType());
        }
    }

    [Theory]
    [InlineData("", 0, -1, null)]
    [InlineData("", 1, -1, null)]
    [InlineData("w", 1, -1, null)]
    [InlineData("w", 2, 0, typeof(Word))]
    [InlineData("w ", 2, 0, typeof(Word))]
    [InlineData("w ", 3, 0, typeof(Word))]
    [InlineData("w \n", 3, 0, typeof(Word))]
    public void TestRollBackToNonWhiteSpace(string code, int shift, int position, Type resultType)
    {
        var sequence = code.Tokenize().Sequence;
        for (int i = 0; i < shift; i++)
        {
            sequence.GetNextToken();
        }

        sequence.RollBackToNonWhiteSpace();
        Assert.Equal(position, sequence.Position);
        if (resultType == null)
        {
            Assert.Null(sequence.Current());
        }
        else
        {
            Assert.Equal(resultType, sequence.Current().GetType());
        }
    }
}