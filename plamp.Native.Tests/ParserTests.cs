using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using plamp.Ast.Node;
using plamp.Ast.Node.Assign;
using plamp.Ast.Node.Binary;
using plamp.Ast.Node.Body;
using plamp.Ast.Node.Unary;
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
    [InlineData("", -1, -1, false)]
    public void TestAdvanceToEndOfLineAndAddException(string code, int startPosition, int endPosition, bool isError = true)
    {
        var parser = new PlampNativeParser(code);
        parser.AdvanceToEndOfLineAndAddException();
        if (isError)
        {
            Assert.Single(parser.Exceptions);
            Assert.Equal(ParserErrorConstants.ExpectedEndOfLine, parser.Exceptions.First().Message);
            Assert.Equal(startPosition, parser.Exceptions.First().StartPosition);
            Assert.Equal(endPosition, parser.Exceptions.First().EndPosition);
        }
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
        true, ParserErrorConstants.UnexpectedTokenPrefix + " " + nameof(OpenParen), 0, 0)]
    [InlineData("()", true, 1, true, false, false)]
    [InlineData("(w", false, 2, false, false, 
        true, ParserErrorConstants.ExpectedCloseParen, 2, 2)]
    [InlineData("w)", false, -1, false, true,
        true, ParserErrorConstants.UnexpectedTokenPrefix + " " + nameof(OpenParen), 0, 0)]
    [InlineData("(w)", true, 2, false, false, false)]
    [InlineData("(x)", false, 2, false, true, false)]
    [InlineData("(w x)", false, 4, false, false, 
        true, ParserErrorConstants.ExpectedCloseParen, 2, 3)]
    [InlineData("(\n", false, 1, false, true, 
        true, ParserErrorConstants.ExpectedCloseParen, 1, 1)]
    [InlineData("(\n)", false, 1, false, true, 
        true, ParserErrorConstants.ExpectedCloseParen, 1, 1)]
    [InlineData("\n)", false, -1, false, true,
        true, ParserErrorConstants.UnexpectedTokenPrefix + " " + nameof(OpenParen), 0, 0)]
    [InlineData("", false, -1, false, true,
        false, null, 0, 0, false)]
    [InlineData("(w", false, 1, false, false,
        false, null, 0, 0, false)]
    public void TestTryParseInParen(string code, bool methodResult, int resultPosition, bool isEmptyCaseInvokedExpected, 
        bool resultTokenIsNull, bool isError, string errorText = null, int errorPositionStart = 0, int errorPositionEnd = 0, bool isStrict = true)
    {
        var parser = new PlampNativeParser(code);
        var isEmptyCaseInvokedActual = false;
        var result = parser.TryParseInParen<Word, OpenParen, CloseParen>(Wrapper,
            () => { isEmptyCaseInvokedActual = true;
                return new Word("", 0);
            }, out var resultToken, isStrict);
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

    [Fact]
    private void TestTryParseInParenEdgeCase()
    {
        var parser = new PlampNativeParser("(1");
        var result = parser.TryParseInParen<Word, OpenParen, CloseParen>(Wrapper, () => null, out _);
        Assert.False(result);
        Assert.Equal(2, parser.TokenSequence.Position);
        Assert.Single(parser.Exceptions);
        Assert.Equal(ParserErrorConstants.ExpectedCloseParen, parser.Exceptions.First().Message);
        Assert.Equal(2, parser.Exceptions.First().StartPosition);
        Assert.Equal(2, parser.Exceptions.First().EndPosition);
        
        bool Wrapper(out Word token)
        {
            token = null;
            parser.TokenSequence.GetNextToken();
            return false;
        }
    }
    
    private object GetDefaultValue(Type t) => t.IsValueType ? Activator.CreateInstance(t) : null;

    [Theory]
    [InlineData("", 0, false, new[]{typeof(MemberNode)}, new[]{"2"}, -1)]
    [InlineData("+1", 0, true, new[]{typeof(PlusNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"2", "1"}, 1)]
    [InlineData("*1", 0, true, new[]{typeof(MultiplyNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"2", "1"}, 1)]
    [InlineData("-1", 0, true, new[]{typeof(MinusNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"2", "1"}, 1)]
    [InlineData("/1", 0, true, new[]{typeof(DivideNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"2", "1"}, 1)]
    [InlineData("<1", 0, true, new[]{typeof(LessNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"2", "1"}, 1)]
    [InlineData(">1", 0, true, new[]{typeof(GreaterNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"2", "1"}, 1)]
    [InlineData("<=1", 0, true, new[]{typeof(LessOrEqualNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"2", "1"}, 1)]
    [InlineData(">=1", 0, true, new[]{typeof(GreaterOrEqualsNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"2", "1"}, 1)]
    [InlineData("==1", 0, true, new[]{typeof(EqualNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"2", "1"}, 1)]
    [InlineData("!=1", 0, true, new[]{typeof(NotEqualNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"2", "1"}, 1)]
    [InlineData("&&1", 0, true, new[]{typeof(AndNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"2", "1"}, 1)]
    [InlineData("||1", 0, true, new[]{typeof(OrNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"2", "1"}, 1)]
    [InlineData("%1", 0, true, new[]{typeof(ModuloNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"2", "1"}, 1)]
    [InlineData("=1", 0, true, new[]{typeof(AssignNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"2", "1"}, 1)]
    [InlineData("+=1", 0, true, new[]{typeof(AddAndAssignNode), typeof(MemberNode),typeof(MemberNode)}, new[]{"2", "1"}, 1)]
    [InlineData("-=1", 0, true, new[]{typeof(SubAndAssignNode), typeof(MemberNode),typeof(MemberNode)}, new[]{"2", "1"}, 1)]
    [InlineData("*=1", 0, true, new[]{typeof(MulAndAssignNode), typeof(MemberNode),typeof(MemberNode)}, new[]{"2", "1"}, 1)]
    [InlineData("/=1", 0, true, new[]{typeof(DivAndAssignNode), typeof(MemberNode),typeof(MemberNode)}, new[]{"2", "1"}, 1)]
    [InlineData("%=1", 0, true, new[]{typeof(ModuloAndAssignNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"2", "1"}, 1)]
    [InlineData("&=1", 0, true, new[]{typeof(AndAndAssignNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"2", "1"}, 1)]
    [InlineData("|=1", 0, true, new[]{typeof(OrAndAssignNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"2", "1"}, 1)]
    [InlineData("^=1", 0, true, new[]{typeof(XorAndAssignNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"2", "1"}, 1)]
    [InlineData("&1", 0, true, new[]{typeof(BitwiseAndNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"2", "1"}, 1)]
    [InlineData("|1", 0, true, new[]{typeof(BitwiseOrNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"2", "1"}, 1)]
    [InlineData("^1", 0, true, new[]{typeof(XorNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"2", "1"}, 1)]
    [InlineData("^1", int.MaxValue, false, new[]{typeof(MemberNode)}, new[]{"2"}, -1)]
    [InlineData("!1", 0, false, new[]{typeof(MemberNode)}, new[]{"2"}, -1)]
    public void TestTryParseLedCorrect(string code, int rbp, bool isParsedExpected, Type[] treeTypeIterator, string[] memberIterator, int? tokenSequencePos = null)
    {
        var startNode = new MemberNode("2");
        var parser = new PlampNativeParser(code);
        var isParsedActual = parser.TryParseLed(rbp, startNode, out var res);
        Assert.Equal(isParsedExpected, isParsedActual);
        var visitor = new TypeTreeVisitor(treeTypeIterator, memberIterator.ToList());
        visitor.Visit(res);
        visitor.Validate();
        if (tokenSequencePos != null)
        {
            Assert.Equal(tokenSequencePos.Value, parser.TokenSequence.Position);
        }
    }

    [Theory]
    [InlineData("", new[]{typeof(MemberNode)}, new[]{"1"}, -1)]
    [InlineData("[]", new[]{typeof(IndexerNode), typeof(MemberNode)}, new[]{"1"}, 1, 1, 
        new[]{ParserErrorConstants.EmptyIndexerDefinition}, new[]{0}, new[]{1})]
    [InlineData("[2]", new[]{typeof(IndexerNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1", "2"}, 2)]
    [InlineData("[2,3]", new[]{typeof(IndexerNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1", "2", "3"}, 4)]
    [InlineData("[", new[]{typeof(MemberNode)}, new []{"1"}, 1, 2, new[]{ParserErrorConstants.InvalidExpression, ParserErrorConstants.ExpectedCloseParen}, new[]{1,1}, new[]{1,1})]
    [InlineData("[1", new[]{typeof(MemberNode)}, new []{"1"}, 2, 1, new[]{ParserErrorConstants.ExpectedCloseParen}, new[]{2}, new[]{2})]
    [InlineData("]", new[]{typeof(MemberNode)}, new []{"1"}, -1)]
    [InlineData("[2,]", new[]{typeof(IndexerNode),typeof(MemberNode),typeof(MemberNode)}, new []{"1", "2"}, 3, 1, new[]{ParserErrorConstants.InvalidExpression}, new []{3},new []{3})]
    [InlineData("[+]", new[]{typeof(MemberNode)}, new []{"1"}, 2, 2, new[]{ParserErrorConstants.InvalidExpression,ParserErrorConstants.ExpectedCloseParen}, new[]{1,1}, new[]{1,1})]
    [InlineData("[,3]", new[]{typeof(IndexerNode),typeof(MemberNode),typeof(MemberNode)}, new []{"1", "3"}, 3, 1, new[]{ParserErrorConstants.InvalidExpression}, new []{1},new []{1})]
    [InlineData("[2,,3]", new[]{typeof(IndexerNode),typeof(MemberNode),typeof(MemberNode),typeof(MemberNode)}, new []{"1", "2", "3"}, 5, 1, new[]{ParserErrorConstants.InvalidExpression}, new []{3},new []{3})]
    public void TestParseIndexerOrDefault(string code, Type[] treeTypeIterator, string[] memberIterator, 
        int? tokenSequencePos = null, int errorCount = 0, 
        string[] errorTextList = null, int[] errorStartPosList = null, int[] errorEndPosList = null)
    {
        var startNode = new MemberNode("1");
        var parser = new PlampNativeParser(code);
        parser.TryParseIndexer(startNode, out var res);
        Assert.Equal(errorCount, parser.Exceptions.Count);
        if (errorCount != 0)
        {
            for (int i = 0; i < parser.Exceptions.Count; i++)
            {
                Assert.Equal(errorTextList[i], parser.Exceptions[i].Message);
                Assert.Equal(errorStartPosList[i], parser.Exceptions[i].StartPosition);
                Assert.Equal(errorEndPosList[i], parser.Exceptions[i].EndPosition);
            }
        }

        if (tokenSequencePos != null)
        {
            Assert.Equal(tokenSequencePos, parser.TokenSequence.Position);
        }

        var visitor = new TypeTreeVisitor(treeTypeIterator, memberIterator.ToList());
        visitor.Visit(res);
        visitor.Validate();
    }

    //TODO: копирование
    [Theory]
    [InlineData("", new[]{typeof(MemberNode)}, new[]{"1"}, -1)]
    [InlineData(".d", new[]{typeof(MemberAccessNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1","d"}, 1)]
    [InlineData(".d()", new[]{typeof(CallNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1","d"}, 3)]
    [InlineData(".d(a)", new[]{typeof(CallNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1","d", "a"}, 4)]
    [InlineData(".d(a,b)", new[]{typeof(CallNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1","d", "a", "b"}, 6)]
    [InlineData(".", new[]{typeof(MemberNode)}, new[]{"1"}, 0, 1, new[]{ParserErrorConstants.UnexpectedTokenPrefix + " " + nameof(Word)}, new []{1},new []{1})]
    [InlineData(".+", new[]{typeof(MemberNode)}, new[]{"1"}, 0, 1, new[]{ParserErrorConstants.UnexpectedTokenPrefix + " " + nameof(Word)}, new []{1},new []{1})]
    [InlineData(".var", new[]{typeof(MemberNode)}, new[]{"1"}, 0, 1, new[]{ParserErrorConstants.CannotUseKeyword}, new []{1},new []{3})]
    [InlineData(".d(a,)", new[]{typeof(CallNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1","d", "a"}, 5, 1, new[]{ParserErrorConstants.InvalidExpression}, new []{5},new []{5})]
    [InlineData(".d(,a)", new[]{typeof(CallNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1","d", "a"}, 5, 1, new[]{ParserErrorConstants.InvalidExpression}, new []{3},new []{3})]
    [InlineData(".d(,)", new[]{typeof(CallNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1","d"}, 4, 2, new[]{ParserErrorConstants.InvalidExpression,ParserErrorConstants.InvalidExpression}, new []{3,4},new []{3,4})]
    [InlineData(".d(a", new[]{typeof(CallNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1","d","a"}, 4, 1, new[]{ParserErrorConstants.ExpectedCloseParen}, new []{4},new []{4})]
    [InlineData(".d(", new[]{typeof(CallNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1","d"}, 3, 2, new[]{ParserErrorConstants.InvalidExpression, ParserErrorConstants.ExpectedCloseParen}, new[]{3,3}, new[]{3,3})]
    public void TestTryParseCall(string code, Type[] treeTypeIterator, string[] memberIterator, 
        int? tokenSequencePos = null, int errorCount = 0, 
        string[] errorTextList = null, int[] errorStartPosList = null, int[] errorEndPosList = null)
    {
        var startNode = new MemberNode("1");
        var parser = new PlampNativeParser(code);
        parser.TryParseCall(startNode, out var res);
        Assert.Equal(errorCount, parser.Exceptions.Count);
        if (errorCount != 0)
        {
            for (int i = 0; i < parser.Exceptions.Count; i++)
            {
                Assert.Equal(errorTextList[i], parser.Exceptions[i].Message);
                Assert.Equal(errorStartPosList[i], parser.Exceptions[i].StartPosition);
                Assert.Equal(errorEndPosList[i], parser.Exceptions[i].EndPosition);
            }
        }

        if (tokenSequencePos != null)
        {
            Assert.Equal(tokenSequencePos, parser.TokenSequence.Position);
        }

        var visitor = new TypeTreeVisitor(treeTypeIterator, memberIterator.ToList());
        visitor.Visit(res);
        visitor.Validate();
    }

    [Theory]
    [InlineData("", new[]{typeof(MemberNode)}, new[]{"1"}, -1)]
    [InlineData(".c().d()", new[]{typeof(CallNode), typeof(CallNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1", "c", "d"}, 7)]
    [InlineData(".c.d()", new[]{typeof(CallNode), typeof(MemberAccessNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1", "c", "d"}, 5)]
    [InlineData(".c().d", new[]{typeof(MemberAccessNode), typeof(CallNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1", "c", "d"}, 5)]
    [InlineData(".c()[2]", new[]{typeof(IndexerNode), typeof(CallNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1", "c", "2"}, 6)]
    [InlineData("[3][2]", new[]{typeof(IndexerNode), typeof(IndexerNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1", "3", "2"}, 5)]
    [InlineData(".d[2]", new[]{typeof(IndexerNode), typeof(MemberAccessNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1", "d", "2"}, 4)]
    [InlineData("[2].d", new[]{typeof(MemberAccessNode), typeof(IndexerNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1", "2", "d"}, 4)]
    [InlineData("++", new[]{typeof(PostfixIncrementNode), typeof(MemberNode)}, new[]{"1"}, 0)]
    [InlineData("--", new[]{typeof(PostfixDecrementNode), typeof(MemberNode)}, new[]{"1"}, 0)]
    [InlineData("++ddd", new[]{typeof(PostfixIncrementNode), typeof(MemberNode)}, new[]{"1"}, 0)]
    [InlineData("--ddd", new[]{typeof(PostfixDecrementNode), typeof(MemberNode)}, new[]{"1"}, 0)]
    [InlineData("+++", new[]{typeof(PostfixIncrementNode), typeof(MemberNode)}, new[]{"1"}, 0)]
    [InlineData("---", new[]{typeof(PostfixDecrementNode), typeof(MemberNode)}, new[]{"1"}, 0)]
    [InlineData("-", new[]{typeof(MemberNode)}, new[]{"1"}, -1)]
    [InlineData("[2]++", new[]{typeof(PostfixIncrementNode), typeof(IndexerNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1", "2"}, 3)]
    [InlineData("++[2]", new[]{typeof(IndexerNode), typeof(PostfixIncrementNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1", "2"}, 3)]
    [InlineData(".d++", new[]{typeof(PostfixIncrementNode), typeof(MemberAccessNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1", "d"}, 2)]
    [InlineData("++.d", new[]{typeof(MemberAccessNode), typeof(PostfixIncrementNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1", "d"}, 2)]
    [InlineData(".d()++", new[]{typeof(PostfixIncrementNode), typeof(CallNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1", "d"}, 4)]
    [InlineData("++.d()", new[]{typeof(CallNode), typeof(PostfixIncrementNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1", "d"}, 4)]
    [InlineData(".d(a).d(a,c)", new[]{typeof(CallNode), typeof(CallNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1", "d", "a", "d", "a", "c"}, 11)]
    [InlineData(".d(a,b).x", new[]{typeof(MemberAccessNode), typeof(CallNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1", "d", "a", "b", "x"}, 8)]
    [InlineData(".x.d(c)", new[]{typeof(CallNode), typeof(MemberAccessNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1", "x", "d", "c"}, 6)]
    [InlineData(".d(a,b,c)", new[]{typeof(CallNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1", "d", "a", "b", "c"}, 8)]
    [InlineData(".d(a)++", new[]{typeof(PostfixIncrementNode), typeof(CallNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1", "d", "a"}, 5)]
    [InlineData("++.d(a)", new[]{typeof(CallNode), typeof(PostfixIncrementNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1", "d", "a"}, 5)]
    [InlineData("[2].d(a)", new[]{typeof(CallNode), typeof(IndexerNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1", "2", "d", "a"}, 7)]
    [InlineData(".d(a)[2]", new[]{typeof(IndexerNode), typeof(CallNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1", "d", "a", "2"}, 7)]
    [InlineData("[2.c()", new[]{typeof(MemberNode)}, new[]{"1"}, 6, 1, new[]{ParserErrorConstants.ExpectedCloseParen}, new[]{6}, new[]{6})]
    [InlineData("[2\"r\"", new[]{typeof(MemberNode)}, new[]{"1"}, 3, 1, new[]{ParserErrorConstants.ExpectedCloseParen}, new[]{2}, new[]{5})]
    [InlineData("+[2]", new[]{typeof(MemberNode)}, new[]{"1"}, -1)]
    [InlineData(".c(2\"r\"", new[]{typeof(CallNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"1","c","2"}, 5, 1, new[]{ParserErrorConstants.ExpectedCloseParen}, new[]{4}, new[]{7})]
    public void TestParsePostfixIfExist(string code, Type[] treeTypeIterator, string[] memberIterator,
        int? tokenSequencePos = null, int errorCount = 0,
        string[] errorTextList = null, int[] errorStartPosList = null, int[] errorEndPosList = null)
    {
        var startNode = new MemberNode("1");
        var parser = new PlampNativeParser(code);
        var res = parser.ParsePostfixIfExist(startNode);
        Assert.Equal(errorCount, parser.Exceptions.Count);
        if (errorCount != 0)
        {
            for (int i = 0; i < parser.Exceptions.Count; i++)
            {
                Assert.Equal(errorTextList[i], parser.Exceptions[i].Message);
                Assert.Equal(errorStartPosList[i], parser.Exceptions[i].StartPosition);
                Assert.Equal(errorEndPosList[i], parser.Exceptions[i].EndPosition);
            }
        }

        if (tokenSequencePos != null)
        {
            Assert.Equal(tokenSequencePos, parser.TokenSequence.Position);
        }

        var visitor = new TypeTreeVisitor(treeTypeIterator, memberIterator.ToList());
        visitor.Visit(res);
        visitor.Validate();
    }
    
    [Theory]
    [InlineData("", new[]{typeof(MemberNode)}, new[]{"1"}, -1)]
    [InlineData("1", new[]{typeof(MemberNode)}, new[]{"1"}, -1)]
    [InlineData("++", new[]{typeof(PostfixIncrementNode), typeof(MemberNode)}, new[]{"1"}, 0)]
    [InlineData("--", new[]{typeof(PostfixDecrementNode), typeof(MemberNode)}, new[]{"1"}, 0)]
    [InlineData("++++", new[]{typeof(PostfixIncrementNode), typeof(MemberNode)}, new[]{"1"}, 0)]
    [InlineData("----", new[]{typeof(PostfixDecrementNode), typeof(MemberNode)}, new[]{"1"}, 0)]
    [InlineData("++--", new[]{typeof(PostfixIncrementNode), typeof(MemberNode)}, new[]{"1"}, 0)]
    [InlineData("--++", new[]{typeof(PostfixDecrementNode), typeof(MemberNode)}, new[]{"1"}, 0)]
    [InlineData("++-", new[]{typeof(PostfixIncrementNode), typeof(MemberNode)}, new[]{"1"}, 0)]
    [InlineData("--+", new[]{typeof(PostfixDecrementNode), typeof(MemberNode)}, new[]{"1"}, 0)]
    [InlineData("++2", new[]{typeof(PostfixIncrementNode), typeof(MemberNode)}, new[]{"1"}, 0)]
    [InlineData("--2", new[]{typeof(PostfixDecrementNode), typeof(MemberNode)}, new[]{"1"}, 0)]
    public void TestParsePostfixOperator(string code, Type[] treeTypeIterator, string[] memberIterator,
        int? tokenSequencePos = null, int errorCount = 0,
        string[] errorTextList = null, int[] errorStartPosList = null, int[] errorEndPosList = null)
    {
        var startNode = new MemberNode("1");
        var parser = new PlampNativeParser(code);
        var res = parser.TryParsePostfixOperator(startNode);
        Assert.Equal(errorCount, parser.Exceptions.Count);
        if (errorCount != 0)
        {
            for (int i = 0; i < parser.Exceptions.Count; i++)
            {
                Assert.Equal(errorTextList[i], parser.Exceptions[i].Message);
                Assert.Equal(errorStartPosList[i], parser.Exceptions[i].StartPosition);
                Assert.Equal(errorEndPosList[i], parser.Exceptions[i].EndPosition);
            }
        }

        if (tokenSequencePos != null)
        {
            Assert.Equal(tokenSequencePos, parser.TokenSequence.Position);
        }

        var visitor = new TypeTreeVisitor(treeTypeIterator, memberIterator.ToList());
        visitor.Visit(res);
        visitor.Validate();
    }
    
    [Theory]
    [InlineData("", new Type[]{}, new string[]{}, -1)]
    [InlineData("\n", new Type[]{}, new string[]{}, -1)]
    //TD
    [InlineData("\"123\"", new[]{typeof(ConstNode)}, new string[]{}, 0)]
    [InlineData("aaa", new[]{typeof(MemberNode)}, new[]{"aaa"}, 0)]
    [InlineData("321", new[]{typeof(MemberNode)}, new[]{"321"}, 0)]
    [InlineData("-aaa", new[]{typeof(UnaryMinusNode), typeof(MemberNode)}, new[]{"aaa"}, 1)]
    [InlineData("-321", new[]{typeof(UnaryMinusNode), typeof(MemberNode)}, new[]{"321"}, 1)]
    [InlineData("!aaa", new[]{typeof(NotNode), typeof(MemberNode)}, new[]{"aaa"}, 1)]
    [InlineData("!321", new[]{typeof(NotNode), typeof(MemberNode)}, new[]{"321"}, 1)]
    [InlineData("++aaa", new[]{typeof(PrefixIncrementNode), typeof(MemberNode)}, new[]{"aaa"}, 1)]
    [InlineData("++321", new[]{typeof(PrefixIncrementNode), typeof(MemberNode)}, new[]{"321"}, 1)]
    [InlineData("--aaa", new[]{typeof(PrefixDecrementNode), typeof(MemberNode)}, new[]{"aaa"}, 1)]
    [InlineData("--321", new[]{typeof(PrefixDecrementNode), typeof(MemberNode)}, new[]{"321"}, 1)]
    [InlineData("=321", new Type[]{}, new string[]{}, -1)]
    [InlineData("!!!321", new[]{typeof(NotNode), typeof(NotNode), typeof(NotNode), typeof(MemberNode)}, new[]{"321"}, 3)]
    [InlineData("(int)x", new[]{typeof(CastNode), typeof(TypeNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"int", "x"}, 3)]
    [InlineData("(int)(int)x", new[]{typeof(CastNode), typeof(TypeNode), typeof(MemberNode), typeof(CastNode), typeof(TypeNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"int", "int", "x"}, 6)]
    [InlineData("!(int)!(int)x", new[]{typeof(NotNode), typeof(CastNode), typeof(TypeNode), typeof(MemberNode), typeof(NotNode), typeof(CastNode), typeof(TypeNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"int", "int", "x"}, 8)]
    [InlineData("(int)", new Type[0], new string[0], -1)]
    [InlineData("--", new Type[0], new string[0], -1)]
    [InlineData("(int)(1 + 1)", new []{typeof(CastNode), typeof(TypeNode), typeof(MemberNode), typeof(PlusNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"int", "1", "1"})]
    [InlineData("new", new Type[0], new string[0], -1)]
    [InlineData("new int", new Type[0], new string[0], -1)]
    [InlineData("new int()", new[]{typeof(ConstructorNode), typeof(TypeNode), typeof(MemberNode)}, new []{"int"}, 4)]
    [InlineData("new int(a, b)", new[]{typeof(ConstructorNode), typeof(TypeNode), typeof(MemberNode), typeof(MemberNode), typeof(MemberNode)}, new []{"int", "a", "b"}, 8)]
    [InlineData("\"a\"++", new[]{typeof(PostfixIncrementNode), typeof(ConstNode)}, new string[0], 1)]
    [InlineData("var", new Type[0], new string[0], -1)]
    [InlineData("var x", new[]{typeof(VariableDefinitionNode), typeof(MemberNode)}, new[]{"x"}, 2)]
    [InlineData("var var", new Type[0], new string[0], -1, 1, new []{ParserErrorConstants.CannotUseKeyword}, new []{4}, new []{6})]
    [InlineData("int d", new[]{typeof(VariableDefinitionNode), typeof(TypeNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"int", "d"}, 2)]
    [InlineData("int", new[]{typeof(MemberNode)}, new[]{"int"}, 0)]
    [InlineData("int x = 1 + 1", new[]{typeof(AssignNode), typeof(VariableDefinitionNode), typeof(TypeNode), typeof(MemberNode), typeof(MemberNode), typeof(PlusNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"int", "x", "1", "1"}, 10)]
    [InlineData("var x = 1 + 1", new[]{typeof(AssignNode), typeof(VariableDefinitionNode), typeof(MemberNode), typeof(PlusNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"x", "1", "1"}, 10)]
    [InlineData("var x=1+", new []{typeof(AssignNode), typeof(VariableDefinitionNode), typeof(MemberNode), typeof(MemberNode)}, new[]{"x", "1"}, 4)]
    public void TestTryParseNud(string code, Type[] treeTypeIterator, string[] memberIterator,
        int? tokenSequencePos = null, int errorCount = 0,
        string[] errorTextList = null, int[] errorStartPosList = null, int[] errorEndPosList = null)
    {
        var parser = new PlampNativeParser(code);
        parser.TryParseNud(out var nud);
        Assert.Equal(errorCount, parser.Exceptions.Count);
        if (errorCount != 0)
        {
            for (int i = 0; i < parser.Exceptions.Count; i++)
            {
                Assert.Equal(errorTextList[i], parser.Exceptions[i].Message);
                Assert.Equal(errorStartPosList[i], parser.Exceptions[i].StartPosition);
                Assert.Equal(errorEndPosList[i], parser.Exceptions[i].EndPosition);
            }
        }

        if (tokenSequencePos != null)
        {
            Assert.Equal(tokenSequencePos, parser.TokenSequence.Position);
        }

        if (nud == null)
        {
            Assert.Empty(treeTypeIterator);
        }
        else
        {
            var visitor = new TypeTreeVisitor(treeTypeIterator, memberIterator.ToList());
            visitor.Visit(nud);
            visitor.Validate();
        }
    }
    
    [Theory]
    [InlineData("", new Type[0], new string[0], false, -1)]
    [InlineData("1", new []{typeof(MemberNode)}, new []{"1"}, true, 0)]
    [InlineData("1+1", new []{typeof(PlusNode), typeof(MemberNode), typeof(MemberNode)}, new []{"1", "1"}, true, 2)]
    [InlineData("1+1+", new []{typeof(PlusNode), typeof(MemberNode), typeof(MemberNode)}, new []{"1", "1"}, true, 2)]
    [InlineData("+", new Type[0], new string[0], false, -1)]
    public void TestTryParseWithPrecedence(
        string code, Type[] treeTypeIterator, string[] memberIterator, bool expectedResult,
        int? tokenSequencePos = null, int errorCount = 0, int startPrecedence = 0,
        string[] errorTextList = null, int[] errorStartPosList = null, int[] errorEndPosList = null)
    {
        var parser = new PlampNativeParser(code);
        var actualResult = parser.TryParseWithPrecedence(out var nud, startPrecedence);
        Assert.Equal(expectedResult, actualResult);
        Assert.Equal(errorCount, parser.Exceptions.Count);
        if (errorCount != 0)
        {
            for (int i = 0; i < parser.Exceptions.Count; i++)
            {
                Assert.Equal(errorTextList[i], parser.Exceptions[i].Message);
                Assert.Equal(errorStartPosList[i], parser.Exceptions[i].StartPosition);
                Assert.Equal(errorEndPosList[i], parser.Exceptions[i].EndPosition);
            }
        }

        if (tokenSequencePos != null)
        {
            Assert.Equal(tokenSequencePos, parser.TokenSequence.Position);
        }

        if (nud == null)
        {
            Assert.Empty(treeTypeIterator);
        }
        else
        {
            var visitor = new TypeTreeVisitor(treeTypeIterator, memberIterator.ToList());
            visitor.Visit(nud);
            visitor.Validate();
        }
    }

    [Theory]
    [InlineData("", new Type[0], new string[0], false, -1)]
    [InlineData("while", new []{typeof(WhileNode), typeof(BodyNode)}, new string[0], true, 1, 1, new []{$"{ParserErrorConstants.UnexpectedTokenPrefix} {nameof(OpenParen)}"}, new []{5}, new []{5})]
    [InlineData("while()", new []{typeof(WhileNode), typeof(BodyNode)}, new string[0], true, 3, 1, new[]{ParserErrorConstants.ExpectedConditionExpression}, new[]{5}, new[]{6})]
    [InlineData("while(a==1)", new []{typeof(WhileNode), typeof(EqualNode), typeof(MemberNode), typeof(MemberNode), typeof(BodyNode)}, new []{"a", "1"}, true, 6)]
    [InlineData("while(a==1)\n", new []{typeof(WhileNode), typeof(EqualNode), typeof(MemberNode), typeof(MemberNode), typeof(BodyNode)}, new []{"a", "1"}, true, 6)]
    [InlineData("while(a==1)\n    var x=0", new []{typeof(WhileNode), typeof(EqualNode), typeof(MemberNode), typeof(MemberNode), typeof(BodyNode), typeof(AssignNode), typeof(VariableDefinitionNode), typeof(MemberNode), typeof(MemberNode)}, new []{"a", "1", "x", "0"}, true, 13)]
    [InlineData("while(a==1,5+11)\n    var x=0", new []{typeof(WhileNode), typeof(EqualNode), typeof(MemberNode), typeof(MemberNode), typeof(BodyNode), typeof(AssignNode), typeof(VariableDefinitionNode), typeof(MemberNode), typeof(MemberNode)}, new []{"a", "1", "x", "0"},true, 17, 1, new[]{ParserErrorConstants.ExpectedCloseParen}, new[]{10}, new[]{14})]
    [InlineData("while()\n    var x=0", new []{typeof(WhileNode), typeof(BodyNode), typeof(AssignNode), typeof(VariableDefinitionNode), typeof(MemberNode), typeof(MemberNode)}, new []{"x", "0"}, true, 10, 1, new[]{ParserErrorConstants.ExpectedConditionExpression}, new[]{5}, new[]{6})]
    [InlineData("while\n    var x=0", new []{typeof(WhileNode), typeof(BodyNode), typeof(AssignNode), typeof(VariableDefinitionNode), typeof(MemberNode), typeof(MemberNode)}, new []{"x", "0"}, true, 8, 1, new []{$"{ParserErrorConstants.UnexpectedTokenPrefix} {nameof(OpenParen)}"}, new []{5}, new []{5})]
    [InlineData("while(a==1)555\n    var x=0", new []{typeof(WhileNode), typeof(EqualNode), typeof(MemberNode), typeof(MemberNode), typeof(BodyNode), typeof(AssignNode), typeof(VariableDefinitionNode), typeof(MemberNode), typeof(MemberNode)}, new []{"a", "1", "x", "0"}, true, 14, 1, new[]{ParserErrorConstants.ExpectedEndOfLine}, new[]{11}, new[]{14})]
    public void TestTryParseWhile(
        string code, Type[] treeTypeIterator, string[] memberIterator, bool expectedResult,
        int? tokenSequencePos = null, int errorCount = 0,
        string[] errorTextList = null, int[] errorStartPosList = null, int[] errorEndPosList = null)
    {
        var parser = new PlampNativeParser(code);
        var actualResult = parser.TryParseWhileLoop(out var whileNode);
        Assert.Equal(expectedResult, actualResult);
        Assert.Equal(errorCount, parser.Exceptions.Count);
        if (errorCount != 0)
        {
            for (int i = 0; i < parser.Exceptions.Count; i++)
            {
                Assert.Equal(errorTextList[i], parser.Exceptions[i].Message);
                Assert.Equal(errorStartPosList[i], parser.Exceptions[i].StartPosition);
                Assert.Equal(errorEndPosList[i], parser.Exceptions[i].EndPosition);
            }
        }

        if (tokenSequencePos != null)
        {
            Assert.Equal(tokenSequencePos, parser.TokenSequence.Position);
        }

        if (whileNode == null)
        {
            Assert.Empty(treeTypeIterator);
        }
        else
        {
            var visitor = new TypeTreeVisitor(treeTypeIterator, memberIterator.ToList());
            visitor.Visit(whileNode);
            visitor.Validate();
        }
    }

    [Theory]
    [InlineData("", true, true, -1, 3, new []{ParserErrorConstants.InvalidExpression, ParserErrorConstants.ExpectedInKeyword, ParserErrorConstants.InvalidExpression}, new[]{-1, -1, -1}, new []{0,0,0})]
    [InlineData("var t", true, false, 2, 1, new []{ParserErrorConstants.ExpectedInKeyword}, new[]{5}, new []{5})]
    [InlineData("var t in", true, false, 4)]
    [InlineData("var t in d", false, false, 6)]
    [InlineData("in d", false, true, 2, 1, new []{ParserErrorConstants.InvalidExpression}, new[]{-1}, new []{-1})]
    [InlineData("in", true, true, 0, 2, new []{ParserErrorConstants.InvalidExpression, ParserErrorConstants.InvalidExpression}, new[]{-1, 1}, new []{-1, 1})]
    [InlineData("var t d", false, false, 4, 1, new []{ParserErrorConstants.ExpectedInKeyword}, new[]{6}, new []{6})]
    public void TestTryParseForHeader(string code, bool isIterableNull, bool isIteratorNull, int resultPosition, int errorCount = 0,
        string[] errorTextList = null, int[] errorStartPosList = null, int[] errorEndPosList = null)
    {
        var parser = new PlampNativeParser(code);
        parser.TryParseForHeader(out var holder);
        if (isIterableNull)
        {
            Assert.Null(holder.Iterable);
        }
        else
        {
            Assert.NotNull(holder.Iterable);
        }
        
        if (isIteratorNull)
        {
            Assert.Null(holder.IteratorVar);
        }
        else
        {
            Assert.NotNull(holder.IteratorVar);
        }
        
        Assert.Equal(resultPosition, parser.TokenSequence.Position);
        Assert.Equal(errorCount, parser.Exceptions.Count);
        if (errorCount != 0)
        {
            for (int i = 0; i < parser.Exceptions.Count; i++)
            {
                Assert.Equal(errorTextList[i], parser.Exceptions[i].Message);
                Assert.Equal(errorStartPosList[i], parser.Exceptions[i].StartPosition);
                Assert.Equal(errorEndPosList[i], parser.Exceptions[i].EndPosition);
            }
        }
    }
}