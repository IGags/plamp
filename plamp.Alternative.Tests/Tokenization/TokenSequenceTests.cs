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
    private const int Utf16CharacterByteCount = 2;
    
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
        var sequence = new TokenSequence([new EndOfFile(new(0, 0, ""))]);
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
            new OperatorToken(">", new(0, 1, ""), OperatorEnum.Greater),
            new WhiteSpace(" ", new FilePosition(Utf16CharacterByteCount, 1, ""), WhiteSpaceKind.WhiteSpace),
            new EndOfFile(new FilePosition(Utf16CharacterByteCount * 2, 0, ""))
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
        var sequence = new TokenSequence([new EndOfFile(new(0, 0, ""))]);
        var moved = sequence.MoveNext();
        moved.ShouldBe(false);
    }

    [Fact]
    public void MoveNextInSequenceWithMultipleTokens_AdvancesPosition()
    {
        var sequence = new TokenSequence([
            new WhiteSpace(" ", new FilePosition(0, 1, ""), WhiteSpaceKind.WhiteSpace),
            new EndOfFile(new(Utf16CharacterByteCount, 0, ""))
        ]);

        var moved = sequence.MoveNext();
        moved.ShouldBe(true);
        sequence.Position.ShouldBe(1);
    }

    [Fact]
    public void MoveNextNonWhiteSpace_SkipsWhiteSpaces()
    {
        var sequence = new TokenSequence([
            new WhiteSpace(" ", new FilePosition(Utf16CharacterByteCount * 0, 1, ""), WhiteSpaceKind.WhiteSpace),
            new WhiteSpace(" ", new FilePosition(Utf16CharacterByteCount * 1, 1, ""), WhiteSpaceKind.WhiteSpace),
            new WhiteSpace(" ", new FilePosition(Utf16CharacterByteCount * 2, 1, ""), WhiteSpaceKind.WhiteSpace),
            new EndOfFile(new(Utf16CharacterByteCount * 3, 0, ""))
        ]);

        var moved = sequence.MoveNextNonWhiteSpace();
        moved.ShouldBe(true);
        sequence.Position.ShouldBe(3);
    }

    [Fact]
    public void MoveNextNonWhiteSpace_DoesNotSkipNonWhiteSpace()
    {
        var sequence = new TokenSequence([
            new OperatorToken(">", new(0, 1, ""), OperatorEnum.Greater),
            new EndOfFile(new(Utf16CharacterByteCount, 0, ""))
        ]);
        
        var moved = sequence.MoveNext();
        moved.ShouldBe(true);
        sequence.Position.ShouldBe(1);
    }

    [Fact]
    public void MoveNextNonWhiteSpaceAtTheEnd_ReturnsFalse()
    {
        var sequence = new TokenSequence([
            new EndOfFile(new(0, 0, ""))
        ]);
        var moved = sequence.MoveNext();
        moved.ShouldBe(false);
        sequence.Position.ShouldBe(0);
    }

    [Fact]
    public void Fork_CreatesPointerCopy()
    {
        var tokenList = new List<TokenBase> { new EndOfFile(new(0, 0, "")) };
        var sequence = new TokenSequence(tokenList);
        var forked = sequence.Fork();
        forked.Position.ShouldBe(sequence.Position);
        forked.Current().ShouldBe(sequence.Current());
    }

    [Fact]
    public void MakeRangeBasedOnPrevious_Correct()
    {
        var first = new Comma(new FilePosition(0, 1, ""));
        var tokenList = new List<TokenBase>
        {
            first,
            new WhiteSpace(" ", new FilePosition(Utf16CharacterByteCount, 1, ""), WhiteSpaceKind.WhiteSpace),
            new Comma(new FilePosition(Utf16CharacterByteCount * 2, 1, "")),
            new EndOfFile(new FilePosition(Utf16CharacterByteCount * 3, 0, ""))
        };
        var sequence = new TokenSequence(tokenList);
        sequence.MoveNextNonWhiteSpace();
        sequence.MoveNextNonWhiteSpace();
        sequence.MoveNext();
        var range = sequence.MakeRangeFromPrevNonWhitespace(first);
        var resultShould = new FilePosition(0, 3, "");
        Assert.Equal(resultShould, range);
    }

    [Fact]
    public void MakeRangeToPreviousDifferentFile_Throws()
    {
        var pos = new FilePosition(0, 1, "f");
        var first = new Comma(pos);
        var incorrectComma = new Comma(pos with { FileName = "" });
        var tokenList = new List<TokenBase>()
        {
            first, new EndOfFile(new FilePosition(Utf16CharacterByteCount, 0, "f"))
        };
        var sequence = new TokenSequence(tokenList);
        sequence.MoveNextNonWhiteSpace();
        sequence.MoveNext();
        Assert.Throws<InvalidOperationException>(() => sequence.MakeRangeFromPrevNonWhitespace(incorrectComma));
    }

    [Fact]
    public void MakeRangeToWhitespace_Throws()
    {
        var tokenList = new List<TokenBase>()
        {
            new Comma(new FilePosition(0, 1, "f")), new EndOfFile(new FilePosition(Utf16CharacterByteCount, 0, "f"))
        };
        var sequence = new TokenSequence(tokenList);
        sequence.MoveNextNonWhiteSpace();
        sequence.MoveNext();
        var whitespace = new WhiteSpace(" ", new FilePosition(0, 1, "nf"), WhiteSpaceKind.WhiteSpace);
        Assert.Throws<InvalidOperationException>(() => sequence.MakeRangeFromPrevNonWhitespace(whitespace));
    }

    [Fact]
    public void MakeRangeFromFirstToken_Throws()
    {
        var to = new Comma(new FilePosition(0, 1, "f"));
        var tokenList = new List<TokenBase>()
        {
            to, new EndOfFile(new FilePosition(Utf16CharacterByteCount, 0, "f"))
        };
        var sequence = new TokenSequence(tokenList);
        Assert.Throws<InvalidOperationException>(() => sequence.MakeRangeFromPrevNonWhitespace(to));
    }

    [Fact]
    public void MakeRangeWhiteSpacesBeforeToken_Throws()
    {
        var to = new Comma(new FilePosition(0, 1, "f"));
        var tokenList = new List<TokenBase>()
        {
            new WhiteSpace(" ", new FilePosition(0, 1, "f"), WhiteSpaceKind.WhiteSpace),
            new OpenParen(new FilePosition(Utf16CharacterByteCount, 1, "f")),
            new EndOfFile(new FilePosition(Utf16CharacterByteCount, 0, "f"))
        };
        var sequence = new TokenSequence(tokenList);
        sequence.MoveNextNonWhiteSpace();
        Assert.Throws<InvalidOperationException>(() => sequence.MakeRangeFromPrevNonWhitespace(to));
    }

    [Fact]
    public void MakeRangeTokenAfterOrCurrent_Throws()
    {
        var tokenList = new List<TokenBase>()
        {
            new Comma(new FilePosition(0, 1, "")),
            new WhiteSpace(" ", new FilePosition(Utf16CharacterByteCount, 1, ""), WhiteSpaceKind.WhiteSpace),
            new Comma(new FilePosition(Utf16CharacterByteCount * 2, 1, "")),
            new EndOfFile(new FilePosition(Utf16CharacterByteCount * 3, 0, ""))
        };
        var sequence = new TokenSequence(tokenList);
        sequence.MoveNextNonWhiteSpace();
        Assert.Throws<InvalidOperationException>(() => sequence.MakeRangeFromPrevNonWhitespace(sequence.Current()));
    }

    [Fact]
    public void MakeRangeTokenNotFound_Throws()
    {
        var tokenList = new List<TokenBase>()
        {
            new Comma(new FilePosition(0, 1, "")),
            new WhiteSpace(" ", new FilePosition(Utf16CharacterByteCount, 1, ""), WhiteSpaceKind.WhiteSpace),
            new Comma(new FilePosition(Utf16CharacterByteCount * 2, 1, "")),
            new EndOfFile(new FilePosition(Utf16CharacterByteCount * 3, 0, ""))
        };
        var sequence = new TokenSequence(tokenList);
        sequence.MoveNextNonWhiteSpace();
        var madeUpToken = new OpenParen(new FilePosition(0, 1, ""));
        Assert.Throws<InvalidOperationException>(() => sequence.MakeRangeFromPrevNonWhitespace(madeUpToken));
    }
}