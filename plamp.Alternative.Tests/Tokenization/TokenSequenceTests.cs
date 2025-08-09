using System;
using System.Collections.Generic;
using plamp.Abstractions.Ast;
using plamp.Alternative.Tokenization;
using plamp.Alternative.Tokenization.Enums;
using plamp.Alternative.Tokenization.Token;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Tokenization;

public class TokenSequenceTests
{
    [Fact]
    public void EmptySequenceCreate_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => new TokenSequence([]));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void SetPositionOutOfSequence_ThrowsArgumentException(int position)
    {
        var sequence = new TokenSequence([new EndOfFile(new(0, 0), new(0, 0))]);
        Should.Throw<ArgumentException>(() => sequence.Position = position);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void SetPositionInsideSequence_ReturnsCorrect(int position)
    {
        var tokenList = new List<TokenBase>
        {
            new OperatorToken(">", new(0, 0), new (0, 0), OperatorEnum.Greater),
            new WhiteSpace(" ", new FilePosition(0, 1), new FilePosition(0, 1), WhiteSpaceKind.WhiteSpace),
            new EndOfFile(new FilePosition(1, 1), new FilePosition(1, 1))
        };
        
        var sequence = new TokenSequence(tokenList)
        {
            Position = position
        };
        
        sequence.Current().ShouldBe(tokenList[position]);
    }

    [Fact]
    public void MoveNextInSequenceWithSingleToken_ReturnFalse()
    {
        var sequence = new TokenSequence([new EndOfFile(new(0, 0), new(0, 0))]);
        var moved = sequence.MoveNext();
        moved.ShouldBe(false);
    }

    [Fact]
    public void MoveNextInSequenceWithMultipleTokens_AdvancesPosition()
    {
        var sequence = new TokenSequence([
            new WhiteSpace(" ", new FilePosition(0, 0), new FilePosition(0, 0), WhiteSpaceKind.WhiteSpace),
            new EndOfFile(new(1, 0), new(1, 0))
        ]);

        var moved = sequence.MoveNext();
        moved.ShouldBe(true);
        sequence.Position.ShouldBe(1);
    }

    [Fact]
    public void MoveNextNonWhiteSpace_SkipsWhiteSpaces()
    {
        var sequence = new TokenSequence([
            new WhiteSpace(" ", new FilePosition(0, 0), new FilePosition(0, 0), WhiteSpaceKind.WhiteSpace),
            new WhiteSpace(" ", new FilePosition(1, 0), new FilePosition(1, 0), WhiteSpaceKind.WhiteSpace),
            new WhiteSpace(" ", new FilePosition(2, 0), new FilePosition(2, 0), WhiteSpaceKind.WhiteSpace),
            new EndOfFile(new(1, 0), new(1, 0))
        ]);

        var moved = sequence.MoveNextNonWhiteSpace();
        moved.ShouldBe(true);
        sequence.Position.ShouldBe(3);
    }

    [Fact]
    public void MoveNextNonWhiteSpace_DoesNotSkipNonWhiteSpace()
    {
        var sequence = new TokenSequence([
            new OperatorToken(">", new(0, 0), new (0, 0), OperatorEnum.Greater),
            new EndOfFile(new(1, 0), new(1, 0))
        ]);
        
        var moved = sequence.MoveNext();
        moved.ShouldBe(true);
        sequence.Position.ShouldBe(1);
    }

    [Fact]
    public void MoveNextNonWhiteSpaceAtTheEnd_ReturnsFalse()
    {
        var sequence = new TokenSequence([
            new EndOfFile(new(1, 0), new(1, 0))
        ]);
        var moved = sequence.MoveNext();
        moved.ShouldBe(false);
        sequence.Position.ShouldBe(0);
    }

    [Fact]
    public void Fork_CreatesPointerCopy()
    {
        var tokeList = new List<TokenBase> { new EndOfFile(new(1, 0), new(1, 0)) };
        var sequence = new TokenSequence(tokeList);
        var forked = sequence.Fork();
        forked.Position.ShouldBe(sequence.Position);
        forked.Current().ShouldBe(sequence.Current());
    }
}