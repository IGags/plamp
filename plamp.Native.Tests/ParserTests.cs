using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using plamp.Native.Parsing;
using plamp.Native.Tokenization;
using plamp.Native.Tokenization.Token;
using Xunit;

namespace plamp.Native.Tests;
#pragma warning disable CS0618
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
    [InlineData("w", -1, 1)]
    [InlineData("w\n", -1, 1)]
    [InlineData("", -1, 0)]
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
        Assert.Equal(3, parser.Exceptions.First().EndPosition);
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
    [InlineData("w \n", 3, 0)]
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
        else
        {
            Assert.Null(parser.TokenSequence.Current());
        }
    }

    [Theory]
    [InlineData("", 0, 0, 
        false, false, true, 
        null, typeof(Word), false)]
    [InlineData("w", 0, 0, true, true, false, 
        typeof(Word), typeof(Word), false)]
    [InlineData("w", 0, 0, false, false, false, 
        typeof(Word), typeof(Word), true)]
    [InlineData("w", 0, 0, false, false, true, 
        typeof(Word), typeof(EndOfLine), false)]
    [InlineData(" w", 0, 1, true, true, false, 
        typeof(Word), typeof(Word), false)]
    [InlineData(" w", 0, 1, false, false, false, 
        typeof(Word), typeof(Word), true)]
    [InlineData(" w", 0, 1, false, false, true, 
        typeof(Word), typeof(EndOfLine), false)]
    
    [InlineData(" w", 1, 1, true, true, false, 
        typeof(Word), typeof(Word), false)]
    [InlineData(" w", 1, 1, false, false, false, 
        typeof(Word), typeof(Word), true)]
    [InlineData(" w", 1, 1, false, false, true, 
        typeof(Word), typeof(EndOfLine), false)]
    [InlineData("  w", 1, 2, true, true, false, 
        typeof(Word), typeof(Word), false)]
    [InlineData("  w", 1, 2, false, false, false, 
        typeof(Word), typeof(Word), true)]
    [InlineData("  w", 1, 2, false, false, true, 
        typeof(Word), typeof(EndOfLine), false)]
    
    [InlineData("<w", 1, 1, true, true, false, 
        typeof(Word), typeof(Word), false)]
    [InlineData("<w", 1, 1, false, false, false, 
        typeof(Word), typeof(Word), true)]
    [InlineData("<w", 1, 1, false, false, true, 
        typeof(Word), typeof(EndOfLine), false)]
    [InlineData("< w", 1, 2, true, true, false, 
        typeof(Word), typeof(Word), false)]
    [InlineData("< w", 1, 2, false, false, false, 
        typeof(Word), typeof(Word), true)]
    [InlineData("< w", 1, 2, false, false, true, 
        typeof(Word), typeof(EndOfLine), false)]
    
    [InlineData("<<", 1, 1, true, true, false, 
        typeof(OpenAngleBracket), typeof(OpenAngleBracket), false)]
    [InlineData("<<", 1, 1, false, false, false, 
        typeof(OpenAngleBracket), typeof(OpenAngleBracket), true)]
    [InlineData("<<", 1, 1, false, false, true, 
        typeof(OpenAngleBracket), typeof(EndOfLine), false)]
    [InlineData("w w", 1, 2, true, true, false, 
        typeof(Word), typeof(Word), false)]
    [InlineData("w w", 1, 2, false, false, false, 
        typeof(Word), typeof(Word), true)]
    [InlineData("w w", 1, 2, false, false, true, 
        typeof(Word), typeof(EndOfLine), false)]
    [InlineData("w", 1, 1, false, false, true, 
        null, typeof(Word), false)]
    [InlineData(" ", 1, 1, false, false, true, 
        null, typeof(Word), false)]
    
    [InlineData(" ", 0, -1, false, false, false, 
        null, typeof(WhiteSpace), false)]
    [InlineData("  ", 1, 0, false, false, false, 
        typeof(WhiteSpace), typeof(WhiteSpace), false)]
    [InlineData("w ", 1, 0, false, false, false, 
        typeof(Word), typeof(WhiteSpace), false)]
    [InlineData("w\n", 1, 0, false, false, false, 
        typeof(Word), typeof(WhiteSpace), false)]
    [InlineData(" ", 1, 0, false, false, false, 
        typeof(WhiteSpace), typeof(WhiteSpace), false)]
    [InlineData("w", 1, 0, false, false, false, 
        typeof(Word), typeof(WhiteSpace), false)]
    public void TestConsumeNextNonWhiteSpaceWithoutRollback(string code, int shift, 
        int position, bool makeTokenError, bool isPredicateMismatchExpected, 
        bool isTokenTypeMismatchedExpected, Type currentTokenType, Type typeToConsume, bool isSuccess)
    {
        var parser = new PlampNativeParser(code);
        for (int i = 0; i < shift; i++)
        {
            parser.TokenSequence.GetNextToken();
        }
        
        //So complicated logic because of method signature
        var isPredicateMismatchActual = false;
        var isTokenTypeMismatchedActual = false;
        var method = parser.GetType().GetMethod(nameof(PlampNativeParser.TryConsumeNextNonWhiteSpaceWithoutRollback), BindingFlags.Instance | BindingFlags.NonPublic);
        var methodWithType = method!.MakeGenericMethod(typeToConsume);
        var @delegate = Expression.Lambda(Expression.Constant(!makeTokenError, typeof(bool)), Expression.Parameter(typeToConsume)).Compile();
        var funcType = typeof(Func<,>).MakeGenericType(typeToConsume, typeof(bool));
        var param = Expression.Parameter(typeof(Delegate));
        var cast = Expression.Lambda(Expression.Convert(param, funcType), param);
        var func = cast.Compile().DynamicInvoke(@delegate);
        var outToken = GetDefaultValue(typeToConsume);
        var res = (bool)methodWithType.Invoke(parser,
            [func, () => { isPredicateMismatchActual = true; }, () => { isTokenTypeMismatchedActual = true; }, outToken])!;
        Assert.Equal(position, parser.TokenSequence.Position);
        Assert.Equal(currentTokenType, parser.TokenSequence.Current()?.GetType());
        Assert.Equal(isSuccess, res);
        Assert.Equal(isPredicateMismatchExpected, isPredicateMismatchActual);
        Assert.Equal(isTokenTypeMismatchedExpected, isTokenTypeMismatchedActual);
    }

    [Theory]
    [InlineData("", 0, -1, 
        false, true,
        null, typeof(Word), false)]
    [InlineData("w", 0, -1, true, true, 
        null, typeof(Word), false)]
    [InlineData("w", 0, 0, false, false,
        typeof(Word), typeof(Word), true)]
    [InlineData("w", 0, -1, false, true,
        null, typeof(EndOfLine), false)]
    [InlineData(" w", 0, -1, true, true,
        null, typeof(Word), false)]
    [InlineData(" w", 0, 1, false, false, 
        typeof(Word), typeof(Word), true)]
    [InlineData(" w", 0, -1, false, true, 
        null, typeof(EndOfLine), false)]
    
    [InlineData(" w", 1, 0, true, true, 
        typeof(WhiteSpace), typeof(Word), false)]
    [InlineData(" w", 1, 1, false, false,
        typeof(Word), typeof(Word), true)]
    [InlineData(" w", 1, 0, false, true,
        typeof(WhiteSpace), typeof(EndOfLine), false)]
    [InlineData("  w", 1, 0, true, true,
        typeof(WhiteSpace), typeof(Word), false)]
    [InlineData("  w", 1, 2, false, false,
        typeof(Word), typeof(Word), true)]
    [InlineData("  w", 1, 0, false, true,
        typeof(WhiteSpace), typeof(EndOfLine), false)]
    
    [InlineData("<w", 1, 0, true, true,
        typeof(OpenAngleBracket), typeof(Word), false)]
    [InlineData("<w", 1, 1, false, false,
        typeof(Word), typeof(Word), true)]
    [InlineData("<w", 1, 0, false, true,
        typeof(OpenAngleBracket), typeof(EndOfLine), false)]
    [InlineData("< w", 1, 0, true, true,
        typeof(OpenAngleBracket), typeof(Word), false)]
    [InlineData("< w", 1, 2, false, false,
        typeof(Word), typeof(Word), true)]
    [InlineData("< w", 1, 0, false, true,
        typeof(OpenAngleBracket), typeof(EndOfLine), false)]
    
    [InlineData("<<", 1, 0, true, true,
        typeof(OpenAngleBracket), typeof(OpenAngleBracket), false)]
    [InlineData("<<", 1, 1, false, false,
        typeof(OpenAngleBracket), typeof(OpenAngleBracket), true)]
    [InlineData("<<", 1, 0, false, true,
        typeof(OpenAngleBracket), typeof(EndOfLine), false)]
    [InlineData("w w", 1, 0, true, true,
        typeof(Word), typeof(Word), false)]
    [InlineData("w w", 1, 2, false, false,
        typeof(Word), typeof(Word), true)]
    [InlineData("w w", 1, 0, false, true,
        typeof(Word), typeof(EndOfLine), false)]
    [InlineData("w", 1, 0, false, true,
        typeof(Word), typeof(Word), false)]
    [InlineData(" ", 1, 0, false, true,
        typeof(WhiteSpace), typeof(Word), false)]
    
    [InlineData(" ", 0, -1, false, false,
        null, typeof(WhiteSpace), false)]
    [InlineData("  ", 1, 0, false, false,
        typeof(WhiteSpace), typeof(WhiteSpace), false)]
    [InlineData("w ", 1, 0, false, false,
        typeof(Word), typeof(WhiteSpace), false)]
    [InlineData("w\n", 1, 0, false, false,
        typeof(Word), typeof(WhiteSpace), false)]
    [InlineData(" ", 1, 0, false, false,
        typeof(WhiteSpace), typeof(WhiteSpace), false)]
    [InlineData("w", 1, 0, false, false,
        typeof(Word), typeof(WhiteSpace), false)]
    public void TestTryConsumeNextNonWhiteSpaceToken(string code, int shift, 
        int position, bool makeTokenError, bool isPredicateMismatchExpected, 
        Type currentTokenType, Type typeToConsume, bool isSuccess)
    {
        var parser = new PlampNativeParser(code);
        for (int i = 0; i < shift; i++)
        {
            parser.TokenSequence.GetNextToken();
        }
        
        //So complicated logic because of method signature
        var isPredicateMismatchActual = false;
        var method = parser.GetType().GetMethod(nameof(PlampNativeParser.TryConsumeNextNonWhiteSpace), BindingFlags.Instance | BindingFlags.NonPublic);
        var methodWithType = method!.MakeGenericMethod(typeToConsume);
        var @delegate = Expression.Lambda(Expression.Constant(!makeTokenError, typeof(bool)), Expression.Parameter(typeToConsume)).Compile();
        var funcType = typeof(Func<,>).MakeGenericType(typeToConsume, typeof(bool));
        var param = Expression.Parameter(typeof(Delegate));
        var cast = Expression.Lambda(Expression.Convert(param, funcType), param);
        var func = cast.Compile().DynamicInvoke(@delegate);
        var outToken = GetDefaultValue(typeToConsume);
        var res = (bool)methodWithType.Invoke(parser,
            [func, () => { isPredicateMismatchActual = true; }, outToken])!;
        Assert.Equal(position, parser.TokenSequence.Position);
        Assert.Equal(currentTokenType, parser.TokenSequence.Current()?.GetType());
        Assert.Equal(isSuccess, res);
        Assert.Equal(isPredicateMismatchExpected, isPredicateMismatchActual);
    }
    
    [Theory]
    [InlineData("", false, 1, -1)]
    [InlineData(" ", false, 1, -1)]
    [InlineData("w", true, 1, 0)]
    [InlineData(",", false, 2, 0)]
    [InlineData("w,w", true, 2, 2)]
    [InlineData("w,", false, 2, 1)]
    [InlineData(",w", false, 2, 1)]
    [InlineData("w,,w", false, 3, 3)]
    [InlineData(",w,w", false, 3, 3)]
    [InlineData("w,w,", false, 3, 3)]
    [InlineData("w,w,w", true, 3, 4)]
    [InlineData("w,w\n", true, 2, 2)]
    public void TestTryParseCommaSeparated(string code, bool methodResult, int resultElementCount, int resultPosition)
    {
        var parser = new PlampNativeParser(code);
        var result = parser.TryParseCommaSeparated<Word>(Wrapper, out var list);
        Assert.Equal(methodResult, result);
        Assert.Equal(resultElementCount, list.Count);
        Assert.Equal(resultPosition, parser.TokenSequence.Position);

        bool Wrapper(out Word token) => parser.TryConsumeNextNonWhiteSpace(_ => true, () => { }, out token);
    }

    [Theory]
    [InlineData("", false, -1, false, true,
        true, ParserErrorConstants.UnexpectedTokenPrefix + " " + nameof(OpenParen), -1, 0)]
    [InlineData("(", false, 1, false, true, 
        true, ParserErrorConstants.ExpectedCloseParen, 1, 1)]
    [InlineData(")", false, -1, false, true,
        true, ParserErrorConstants.UnexpectedTokenPrefix + " " + nameof(OpenParen), -1, -1)]
    [InlineData("()", true, 1, true, false, false)]
    [InlineData("(w", false, 2, false, false, 
        true, ParserErrorConstants.ExpectedCloseParen, 2, 2)]
    [InlineData("w)", false, -1, false, true,
        true, ParserErrorConstants.UnexpectedTokenPrefix + " " + nameof(OpenParen), -1, -1)]
    [InlineData("(w)", true, 2, false, false, false)]
    [InlineData("(x)", false, 2, false, true, false)]
    [InlineData("(w x)", false, 4, false, false, 
        true, ParserErrorConstants.ExpectedCloseParen, 2, 3)]
    [InlineData("(\n", false, 1, false, true, 
        true, ParserErrorConstants.ExpectedCloseParen, 1, 1)]
    [InlineData("(\n)", false, 1, false, true, 
        true, ParserErrorConstants.ExpectedCloseParen, 1, 1)]
    [InlineData("\n)", false, -1, false, true,
        true, ParserErrorConstants.UnexpectedTokenPrefix + " " + nameof(OpenParen), -1, -1)]
    public void TestTryParseInParen(string code, bool methodResult, int resultPosition, bool isEmptyCaseInvokedExpected, 
        bool resultTokenIsNull, bool isError, string errorText = null, int errorPositionStart = 0, int errorPositionEnd = 0)
    {
        var parser = new PlampNativeParser(code);
        var isEmptyCaseInvokedActual = false;
        var result = parser.TryParseInParen<Word, OpenParen, CloseParen>(Wrapper,
            () => { isEmptyCaseInvokedActual = true;
                return new Word("", 0);
            }, out var resultToken);
        Assert.Equal(methodResult, result);
        Assert.Equal(resultPosition, parser.TokenSequence.Position);
        Assert.Equal(isEmptyCaseInvokedExpected, isEmptyCaseInvokedActual);
        if (resultTokenIsNull)
        {
            Assert.Null(resultToken);
        }
        else
        {
            Assert.NotNull(resultToken);
        }

        if (isError)
        {
            Assert.Single(parser.Exceptions);
            Assert.Equal(errorText, parser.Exceptions.First().Message);
            Assert.Equal(errorPositionStart, parser.Exceptions.First().StartPosition);
            Assert.Equal(errorPositionEnd, parser.Exceptions.First().EndPosition);
        }
        
        bool Wrapper(out Word token) => parser.TryConsumeNextNonWhiteSpace(x => x.GetString() == "w", () => { }, out token);
    }
    
    private object GetDefaultValue(Type t) => t.IsValueType ? Activator.CreateInstance(t) : null;
    
    
}