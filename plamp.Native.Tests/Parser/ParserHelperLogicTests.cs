using System;
using System.Linq;
using plamp.Abstractions.Ast;
using plamp.Native.Parsing;
using plamp.Native.Tokenization.Enumerations;
using plamp.Native.Tokenization.Token;
using Xunit;

#pragma warning disable CS0618 // Type or member is obsolete
namespace plamp.Native.Tests.Parser;

public class ParserHelperLogicTests
{
    #region AddExceptionToTheTokenRange

    
    /// <summary>
    /// Must add exception in ascending order
    /// </summary>
    [Theory]
    [InlineData("aaa", 0, 0)]
    [InlineData("aaa aaa", 0, 2)]
    [InlineData("a\na", 0, 2)]
    [InlineData("a->\na", 0, 3)]
    //Tokenizer implicitly add \n to the end of code
    [InlineData("", 0, 0)]
    public void TestAddExceptionToTokenRange(string code, int startToken, int endToken)
    {
        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        context.TokenSequence.Position = startToken;
        var start = context.TokenSequence.Current();
        context.TokenSequence.Position = endToken;
        var end = context.TokenSequence.Current();
        PlampNativeParser.AddExceptionToTheTokenRange(start, end, 
            PlampNativeExceptionInfo.UnexpectedToken(null), transaction, context);
        transaction.Commit();

        Assert.Single(context.TransactionSource.Exceptions);
        Assert.Equal(start.Start, context.TransactionSource.Exceptions.First().StartPosition);
        Assert.Equal(end.End, context.TransactionSource.Exceptions.First().EndPosition);
    }

    /// <summary>
    /// Exception can be caused by inverted order
    /// </summary>
    [Fact]
    public void TestAddExceptionToNegativeOrder()
    {
        const string code = "1 ";
        var parser = new PlampNativeParser();
        var context = ParserTestHelper.GetContext(code);
        context.TokenSequence.Position = 0;
        var transaction = context.TransactionSource.BeginTransaction();
        var start = context.TokenSequence.Current();
        context.TokenSequence.Position = 1;
        var end = context.TokenSequence.Current();
        Assert.Throws<ArgumentException>(() =>
            PlampNativeParser.AddExceptionToTheTokenRange(end, start, 
                PlampNativeExceptionInfo.UnexpectedToken(null),
                transaction, context));
    }
    
    #endregion

    #region SkipLineBreak
    
    /// <summary>
    /// If parser encounters line break it should ignore next end of line
    /// and merge rest part of token sequence
    /// </summary>
    [Theory]
    [InlineData("", -1)]
    [InlineData("->\n", 1)]
    [InlineData(" ->\n", 2)]
    [InlineData("-> \n", 2)]
    [InlineData("->1", 0)]
    [InlineData("1->", -1)]
    public void SkipLineBreakTests(string code, int resultPos)
    {
        var context = ParserTestHelper.GetContext(code);
        PlampNativeParser.SkipLineBreak(context);
        Assert.Equal(resultPos, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    #endregion

    #region AdvanceToEndOfLineOrRequested
    
    /// <summary>
    /// Parser should advance to requested token type
    /// or to end of line if it appears earlier
    /// </summary>
    [Theory]
    [InlineData("", 0)]
    [InlineData("  ", 2)]
    [InlineData("priv \n (", 2)]
    [InlineData("priv ->\n (", 5)]
    public void AdvanceToEndOfLineOrRequestedTests(string code, int resultPos)
    {
        var context = ParserTestHelper.GetContext(code);
        PlampNativeParser.AdvanceToEndOfLineOrRequested<OpenParen>(context);
        Assert.Equal(resultPos, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    /// <summary>
    /// Parser start from zero(not -1) position and should not move
    /// </summary>
    [Fact]
    public void AdvanceToEndOfLineOrRequestedIfOnRequested()
    {
        const string code = "((";
        var context = ParserTestHelper.GetContext(code);
        context.TokenSequence.Position = 0;
        PlampNativeParser.AdvanceToEndOfLineOrRequested<OpenParen>(context);
        Assert.Equal(0, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    /// <summary>
    /// Parser starts from 1 pos, but requested token has 0 position
    /// </summary>
    [Fact]
    public void AdvanceToEndOfLineOrRequestedIdTokenBefore()
    {
        const string code = "( \n";
        var context = ParserTestHelper.GetContext(code);
        context.TokenSequence.Position = 1;
        PlampNativeParser.AdvanceToEndOfLineOrRequested<OpenParen>(context);
        Assert.Equal(2, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    /// <summary>
    /// Corner case. Parser shouldn't advance to whitespace
    /// </summary>
    [Fact]
    public void ExceptAdvanceToWhiteSpaceOrEndOfLine()
    {
        const string code = " ";
        var context = ParserTestHelper.GetContext(code);
        Assert.Throws<Exception>(() => PlampNativeParser.AdvanceToEndOfLineOrRequested<WhiteSpace>(context));
    }
    
    #endregion

    #region TryConsumeNextNonWhiteSpace
    
    /// <summary>
    /// Parser should return false
    /// </summary>
    [Fact]
    public void TryConsumeNextNonWhiteSpaceOnEmptyString()
    {
        const string code = "";
        var context = ParserTestHelper.GetContext(code);
        var falsePredicateExecuted = false;
        var result = PlampNativeParser.TryConsumeNextNonWhiteSpace<OpenParen>(
            _ => true, _ => falsePredicateExecuted = true, out var token, context);
        Assert.False(result);
        Assert.Null(token);
        Assert.Empty(context.TransactionSource.Exceptions);
        Assert.Equal(-1, context.TokenSequence.Position);
        Assert.True(falsePredicateExecuted);
    }

    /// <summary>
    /// Returns true and don't call false action
    /// </summary>
    [Fact]
    public void TryConsumeNextNonWhiteSpaceForMatchToken()
    {
        const string code = "(";
        var context = ParserTestHelper.GetContext(code);
        var falsePredicateExecuted = false;
        var result = PlampNativeParser.TryConsumeNextNonWhiteSpace<OpenParen>(
            _ => true, _ => falsePredicateExecuted = true, out var token, context);
        Assert.True(result);
        Assert.Equal(typeof(OpenParen), token.GetType());
        Assert.Empty(context.TransactionSource.Exceptions);
        Assert.Equal(0, context.TokenSequence.Position);
        Assert.False(falsePredicateExecuted);
    }

    /// <summary>
    /// Next non-whitespace has different type
    /// </summary>
    [Fact]
    public void TryConsumeNextNonWhiteSpaceForTypeMismatch()
    {
        const string code = ")";
        var context = ParserTestHelper.GetContext(code);
        var falsePredicateExecuted = false;
        var result = PlampNativeParser.TryConsumeNextNonWhiteSpace<OpenParen>(
            _ => true, _ => falsePredicateExecuted = true, out var token, context);
        Assert.False(result);
        Assert.Null(token);
        Assert.Empty(context.TransactionSource.Exceptions);
        Assert.Equal(-1, context.TokenSequence.Position);
        Assert.True(falsePredicateExecuted);
    }

    /// <summary>
    /// Next non-whitespace has same type but suddenly predicate returns false
    /// </summary>
    [Fact]
    public void TryConsumeNextNonWhiteSpaceForPredicateMismatch()
    {
        const string code = "(";
        var context = ParserTestHelper.GetContext(code);
        var falsePredicateExecuted = false;
        var result = PlampNativeParser.TryConsumeNextNonWhiteSpace<OpenParen>(
            _ => false, _ => falsePredicateExecuted = true, out var token, context);
        Assert.False(result);
        Assert.Null(token);
        Assert.Empty(context.TransactionSource.Exceptions);
        Assert.Equal(-1, context.TokenSequence.Position);
        Assert.True(falsePredicateExecuted);
    }

    /// <summary>
    /// From parser position to non-whitespace token only whitespaces
    /// </summary>
    [Fact]
    public void TryConsumeNextNonWhiteSpaceWithWhiteSpaceBetween()
    {
        const string code = " (";
        var context = ParserTestHelper.GetContext(code);
        var falsePredicateExecuted = false;
        var result = PlampNativeParser.TryConsumeNextNonWhiteSpace<OpenParen>(
            _ => true, _ => falsePredicateExecuted = true, out var token, context);
        Assert.True(result);
        Assert.Equal(typeof(OpenParen), token.GetType());
        Assert.Empty(context.TransactionSource.Exceptions);
        Assert.Equal(1, context.TokenSequence.Position);
        Assert.False(falsePredicateExecuted);
    }

    /// <summary>
    /// Code has whitespace only
    /// </summary>
    [Fact]
    public void TryConsumeNextNonWhiteSpaceWithOnlyWhiteSpace()
    {
        const string code = "    ";
        var context = ParserTestHelper.GetContext(code);
        var falsePredicateExecuted = false;
        var result = PlampNativeParser.TryConsumeNextNonWhiteSpace<OpenParen>(
            _ => true, _ => falsePredicateExecuted = true, out var token, context);
        Assert.False(result);
        Assert.Null(token);
        Assert.Empty(context.TransactionSource.Exceptions);
        Assert.Equal(-1, context.TokenSequence.Position);
        Assert.True(falsePredicateExecuted);
    }

    /// <summary>
    /// Method calls with whitespace as generic
    /// </summary>
    [Fact]
    public void TryConsumeNextNonWhiteSpaceForWhiteSpace()
    {
        const string code = "";
        var context = ParserTestHelper.GetContext(code);
        Assert.Throws<Exception>(() => PlampNativeParser.TryConsumeNextNonWhiteSpace<WhiteSpace>(
            _ => true, _ => { }, out _, context));
    }
    
    #endregion

    #region TryParseCommaSeparated

    /// <summary>
    /// Parsing of empty sequence returns fail
    /// </summary>
    [Fact]
    public void TryParseCommaSeparatedEmpty()
    {
        const string code = "";
        var context = ParserTestHelper.GetContext(code);
        var res = PlampNativeParser.TryParseCommaSeparated(
            TryConsumeOpenParen(context), out var result, 
            PlampNativeParser.ExpressionParsingResult.FailedNeedRollback, context);
        
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.FailedNeedRollback, res);
        Assert.Single(result);
        Assert.Null(result[0]);
        Assert.Equal(-1, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    /// <summary>
    /// Should complete with one element and success
    /// </summary>
    [Fact]
    public void TryParseCommaSeparatedOneMatchCase()
    {
        const string code = "(";
        var context = ParserTestHelper.GetContext(code);
        var res = PlampNativeParser.TryParseCommaSeparated(
            TryConsumeOpenParen(context), out var result, 
            PlampNativeParser.ExpressionParsingResult.FailedNeedRollback, context);
        
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, res);
        Assert.Single(result);
        Assert.Equal(typeof(OpenParen), result[0].GetType());
        Assert.Equal(0, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    [Fact]
    public void TryParseCommaSeparatedOneMismatchCase()
    {
        const string code = ")";
        var context = ParserTestHelper.GetContext(code);
        var res = PlampNativeParser.TryParseCommaSeparated(
            TryConsumeOpenParen(context), out var result, 
            PlampNativeParser.ExpressionParsingResult.FailedNeedRollback, context);
        
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.FailedNeedRollback, res);
        Assert.Single(result);
        Assert.Null(result[0]);
        Assert.Equal(-1, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    [Fact]
    public void TryParseCommaSeparatedTwoMatchCases()
    {
        const string code = "(,(";
        var context = ParserTestHelper.GetContext(code);
        var res = PlampNativeParser.TryParseCommaSeparated(
            TryConsumeOpenParen(context), out var result, 
            PlampNativeParser.ExpressionParsingResult.FailedNeedRollback, context);
        
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, res);
        Assert.Equal(2, result.Count);
        Assert.Equal(typeof(OpenParen), result[0].GetType());
        Assert.Equal(typeof(OpenParen), result[1].GetType());
        Assert.Equal(2, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    [Fact]
    public void TryParseCommaSeparatedMatchAndMismatch()
    {
        const string code = "(,)";
        var context = ParserTestHelper.GetContext(code);
        var res = PlampNativeParser.TryParseCommaSeparated(
            TryConsumeOpenParen(context), out var result, 
            PlampNativeParser.ExpressionParsingResult.FailedNeedRollback, context);
        
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.FailedNeedRollback, res);
        Assert.Equal(2, result.Count);
        Assert.Equal(typeof(OpenParen), result[0].GetType());
        Assert.Null(result[1]);
        Assert.Equal(1, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    [Fact]
    public void TryParseCommaSeparatedMismatchAndMatch()
    {
        const string code = "),(";
        var context = ParserTestHelper.GetContext(code);
        var res = PlampNativeParser.TryParseCommaSeparated(
            TryConsumeOpenParen(context), out var result, 
            PlampNativeParser.ExpressionParsingResult.FailedNeedRollback, context);
        
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.FailedNeedRollback, res);
        Assert.Single(result);
        Assert.Null(result[0]);
        Assert.Equal(-1, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    [Fact]
    public void TryParseCommaSeparatedWithWrongSplitter()
    {
        const string code = "(-(";
        var context = ParserTestHelper.GetContext(code);
        var res = PlampNativeParser.TryParseCommaSeparated(
            TryConsumeOpenParen(context), out var result, 
            PlampNativeParser.ExpressionParsingResult.FailedNeedRollback, context);
        
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, res);
        Assert.Single(result);
        Assert.Equal(typeof(OpenParen), result[0].GetType());
        Assert.Equal(0, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }
    
    /// <summary>
    /// Internal function that consumes only open parens
    /// </summary>
    private PlampNativeParser.TryParseInternal<OpenParen> TryConsumeOpenParen(ParsingContext context)
    {
        PlampNativeParser.ExpressionParsingResult Internal(out OpenParen paren, ParsingContext _)
        {
            var res = PlampNativeParser.TryConsumeNextNonWhiteSpace(_ => true, _ => { }, out paren, context);
            return res
                ? PlampNativeParser.ExpressionParsingResult.Success
                : PlampNativeParser.ExpressionParsingResult.FailedNeedRollback;
        }

        return Internal;
    }
    #endregion

    #region TryParseInParen

    [Fact]
    public void TryParseInParenEmpty()
    {
        const string code = "";
        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        var emptyCaseInvoked = false;
        var res = PlampNativeParser.TryParseInParen<OperatorToken, OpenParen, CloseParen>(
            transaction, 
            ConsumeOperator(context), (_, _) =>
            {
                emptyCaseInvoked = true;
                return default;
            }, out var @operator, 
            PlampNativeParser.ExpressionParsingResult.FailedNeedPass,
            PlampNativeParser.ExpressionParsingResult.FailedNeedCommit,
            context);
        transaction.Commit();
        
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.FailedNeedPass, res);
        Assert.False(emptyCaseInvoked);
        Assert.Null(@operator);
        Assert.Equal(-1, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    [Fact]
    public void TryParseInParenWithoutOpenParen()
    {
        const string code = "-)";
        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        var emptyCaseInvoked = false;
        var res = PlampNativeParser.TryParseInParen<OperatorToken, OpenParen, CloseParen>(
            transaction, 
            ConsumeOperator(context), (_, _) =>
            {
                emptyCaseInvoked = true;
                return default;
            }, out var @operator, 
            PlampNativeParser.ExpressionParsingResult.FailedNeedPass,
            PlampNativeParser.ExpressionParsingResult.FailedNeedCommit,
            context);
        transaction.Commit();
        
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.FailedNeedPass, res);
        Assert.False(emptyCaseInvoked);
        Assert.Null(@operator);
        Assert.Equal(-1, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    [Fact]
    public void TryParseInParenWithEmptyParens()
    {
        const string code = "()";
        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        var emptyCaseInvoked = false;
        var res = PlampNativeParser.TryParseInParen<OperatorToken, OpenParen, CloseParen>(
            transaction, 
            ConsumeOperator(context), (_, _) =>
            {
                emptyCaseInvoked = true;
                return default;
            }, out var @operator, 
            PlampNativeParser.ExpressionParsingResult.FailedNeedPass,
            PlampNativeParser.ExpressionParsingResult.FailedNeedCommit, context);
        transaction.Commit();
        
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.FailedNeedCommit, res);
        Assert.True(emptyCaseInvoked);
        Assert.Null(@operator);
        Assert.Equal(1, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    [Fact]
    public void TryParseInParenWithoutCloseParen()
    {
        const string code = "(-";
        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        var emptyCaseInvoked = false;
        var res = PlampNativeParser.TryParseInParen<OperatorToken, OpenParen, CloseParen>(
            transaction, 
            ConsumeOperator(context), (_, _) =>
            {
                emptyCaseInvoked = true;
                return null;
            }, out var @operator, 
            PlampNativeParser.ExpressionParsingResult.FailedNeedPass,
            PlampNativeParser.ExpressionParsingResult.FailedNeedCommit,
            context);
        transaction.Commit();
        
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, res);
        Assert.False(emptyCaseInvoked);
        Assert.Equal(OperatorEnum.Minus, @operator.Operator);
        Assert.Equal(2, context.TokenSequence.Position);
        Assert.Single(context.TransactionSource.Exceptions);
        var exceptionShould = new PlampException(
            PlampNativeExceptionInfo.ParenExpressionIsNotClosed(),
            new FilePosition(0, 0), new FilePosition(0, 3),
            ParserTestHelper.FileName, ParserTestHelper.AssemblyName);
        //CRLF end of line has two characters
        Assert.Equal(exceptionShould, context.TransactionSource.Exceptions[0]);
    }

    [Fact]
    public void TryParseInParenValid()
    {
        const string code = "(-)";
        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        var emptyCaseInvoked = false;
        var res = PlampNativeParser.TryParseInParen<OperatorToken, OpenParen, CloseParen>(
            transaction, 
            ConsumeOperator(context), (_, _) =>
            {
                emptyCaseInvoked = true;
                return default;
            }, out var @operator, 
            PlampNativeParser.ExpressionParsingResult.FailedNeedPass,
            PlampNativeParser.ExpressionParsingResult.FailedNeedCommit,
            context);
        transaction.Commit();
        
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.Success, res);
        Assert.False(emptyCaseInvoked);
        Assert.Equal(OperatorEnum.Minus, @operator.Operator);
        Assert.Equal(2, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    [Fact]
    public void TryParseInParenWithInvalidInternalExpression()
    {
        const string code = "(word)";
        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        var emptyCaseInvoked = false;
        var res = PlampNativeParser.TryParseInParen<OperatorToken, OpenParen, CloseParen>(
            transaction, 
            ConsumeOperator(context), (_, _) =>
            {
                emptyCaseInvoked = true;
                return null;
            }, out var @operator, 
            PlampNativeParser.ExpressionParsingResult.FailedNeedPass,
            PlampNativeParser.ExpressionParsingResult.FailedNeedCommit,
            context);
        transaction.Commit();
        
        Assert.Equal(PlampNativeParser.ExpressionParsingResult.FailedNeedRollback, res);
        Assert.False(emptyCaseInvoked);
        Assert.Null(@operator);
        Assert.Equal(2, context.TokenSequence.Position);
        Assert.Single(context.TransactionSource.Exceptions);
        var exceptionShould = new PlampException(
            PlampNativeExceptionInfo.Expected(nameof(CloseParen)),
            new FilePosition(0, 0), new FilePosition(0, 5),
            ParserTestHelper.FileName, ParserTestHelper.AssemblyName);
        Assert.Equal(exceptionShould, context.TransactionSource.Exceptions[0]);
    }
    
    /// <summary>
    /// Internal function that returns operators.
    /// I know that I have same function above, but I don't want to split functions between regions
    /// </summary>
    private PlampNativeParser.TryParseInternal<OperatorToken> ConsumeOperator(ParsingContext context)
    {
        PlampNativeParser.ExpressionParsingResult Internal(out OperatorToken @operator, ParsingContext _)
        {
            var res = PlampNativeParser.TryConsumeNextNonWhiteSpace(_ => true, _ => { }, out @operator, context);
            return res
                ? PlampNativeParser.ExpressionParsingResult.Success
                : PlampNativeParser.ExpressionParsingResult.FailedNeedRollback;
        }

        return Internal;
    }

    #endregion

    //TODO: Duplicated logic
    #region AdvanceToRequestedTokenWithException

    [Fact]
    public void AdvanceToRequestedTokenWithExceptionOnEndOfLine()
    {
        const string code = "\n(";
        var context = ParserTestHelper.GetContext(code);
        context.TokenSequence.Position = 0;
        var transaction = context.TransactionSource.BeginTransaction();
        PlampNativeParser.AdvanceToRequestedTokenWithException<OpenParen>(transaction, context);
        transaction.Commit();
        
        Assert.Equal(0, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    /// <summary>
    /// Has strange behaviour because of
    /// (means first we call tryConsume then call this method.
    /// I made this because I don't want to create complicated signature)
    /// </summary>
    [Fact]
    public void AdvanceToRequestedTokenWithExceptionIfNextIsRequested()
    {
        const string code = "(";
        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        PlampNativeParser.AdvanceToRequestedTokenWithException<OpenParen>(transaction, context);
        transaction.Commit();
        
        Assert.Equal(0, context.TokenSequence.Position);
        Assert.Single(context.TransactionSource.Exceptions);
        var expectedException = new PlampException(PlampNativeExceptionInfo.Expected(nameof(OpenParen)),
            new FilePosition(0, 0), new FilePosition(0, 0),
            ParserTestHelper.FileName, ParserTestHelper.AssemblyName);
        
        Assert.Equal(expectedException, context.TransactionSource.Exceptions[0]);
    }

    [Fact]
    public void AdvanceToRequestedTokenWithExceptionIfNextIsNotRequested()
    {
        const string code = "-(";
        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        PlampNativeParser.AdvanceToRequestedTokenWithException<OpenParen>(transaction, context);
        transaction.Commit();
        
        Assert.Equal(1, context.TokenSequence.Position);
        Assert.Single(context.TransactionSource.Exceptions);
        var expectedException = new PlampException(
            PlampNativeExceptionInfo.Expected(nameof(OpenParen)),
            new FilePosition(0, 0), new FilePosition(0, 1),
            ParserTestHelper.FileName, ParserTestHelper.AssemblyName);
        
        Assert.Equal(expectedException, context.TransactionSource.Exceptions[0]);
    }

    [Fact]
    public void AdvanceToRequestedTokenWithExceptionIfNextIsEndOfLine()
    {
        const string code = "\n(";
        var context = ParserTestHelper.GetContext(code);
        var transaction = context.TransactionSource.BeginTransaction();
        PlampNativeParser.AdvanceToRequestedTokenWithException<OpenParen>(transaction, context);
        transaction.Commit();
        
        Assert.Equal(0, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }
    
    #endregion

    #region TryConsumeNext

    [Fact]
    public void TryConsumeNextOnEmpty()
    {
        const string code = "";
        var context = ParserTestHelper.GetContext(code);
        var falsePredicateCalled = false;
        var res = PlampNativeParser.TryConsumeNext<OpenParen>(
            _ => true, _ => falsePredicateCalled = true, out var result, context);
        Assert.False(res);
        Assert.Null(result);
        Assert.True(falsePredicateCalled);
        Assert.Equal(-1, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    [Fact]
    public void TryConsumeNextWhiteSpace()
    {
        const string code = " ";
        var context = ParserTestHelper.GetContext(code);
        var falsePredicateCalled = false;
        var res = PlampNativeParser.TryConsumeNext<WhiteSpace>(
            _ => true, _ => falsePredicateCalled = true, out var result, context);
        Assert.True(res);
        Assert.Equal(typeof(WhiteSpace), result.GetType());
        Assert.False(falsePredicateCalled);
        Assert.Equal(0, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    [Fact]
    public void TryConsumeNextNonWhiteSpace()
    {
        const string code = "(";
        var context = ParserTestHelper.GetContext(code);
        var falsePredicateCalled = false;
        var res = PlampNativeParser.TryConsumeNext<OpenParen>(
            _ => true, _ => falsePredicateCalled = true, out var result, context);
        Assert.True(res);
        Assert.Equal(typeof(OpenParen), result.GetType());
        Assert.False(falsePredicateCalled);
        Assert.Equal(0, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    [Fact]
    public void TryConsumeNextWhiteSpaceFail()
    {
        const string code = "(";
        var context = ParserTestHelper.GetContext(code);
        var falsePredicateCalled = false;
        var res = PlampNativeParser.TryConsumeNext<WhiteSpace>(
            _ => true, _ => falsePredicateCalled = true, out var result, context);
        Assert.False(res);
        Assert.Null(result);
        Assert.True(falsePredicateCalled);
        Assert.Equal(-1, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }
    
    [Fact]
    public void TryConsumeNextNonWhiteSpaceFail()
    {
        const string code = " ";
        var context = ParserTestHelper.GetContext(code);
        var falsePredicateCalled = false;
        var res = PlampNativeParser.TryConsumeNext<WhiteSpace>(
            _ => false, _ => falsePredicateCalled = true, out var result, context);
        Assert.False(res);
        Assert.Null(result);
        Assert.True(falsePredicateCalled);
        Assert.Equal(-1, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    [Fact]
    public void TryConsumeWithFalsePredicate()
    {
        const string code = "(";
        var context = ParserTestHelper.GetContext(code);
        var falsePredicateCalled = false;
        var res = PlampNativeParser.TryConsumeNext<WhiteSpace>(
            _ => false, _ => falsePredicateCalled = true, out var result, context);
        Assert.False(res);
        Assert.Null(result);
        Assert.True(falsePredicateCalled);
        Assert.Equal(-1, context.TokenSequence.Position);
        Assert.Empty(context.TransactionSource.Exceptions);
    }

    #endregion
}