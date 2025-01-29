using System;
using plamp.Ast;
using plamp.Native.Parsing.Transactions;
using plamp.Native.Tokenization;
using Xunit;

namespace plamp.Native.Tests.TransactionSource;

public class TransactionTests
{
    #region Commit

    [Fact]
    public void CommitWithoutExceptions()
    {
        var result = "+ -".Tokenize();
        var source = new ParsingTransactionSource(result.Sequence, []);
        var transaction = source.BeginTransaction();
        result.Sequence.Position = 2;
        transaction.Commit();
        
        Assert.Equal(2, result.Sequence.Position);
        Assert.Empty(source.Exceptions);
    }

    [Fact]
    public void CommitWithExceptions()
    {
        var result = string.Empty.Tokenize();
        var source = new ParsingTransactionSource(result.Sequence, []);

        var ex = new PlampException(PlampNativeExceptionInfo.InvalidCastOperator(), default, default);
        var transaction = source.BeginTransaction();
        transaction.AddException(ex);
        transaction.Commit();
        Assert.Equal(-1, result.Sequence.Position);
        Assert.Single(source.Exceptions);
        Assert.Equal(ex, source.Exceptions[0]);
    }

    [Fact]
    public void CommitWithInnerTransactionHasNotException()
    {
        var result = "(((".Tokenize();
        var source = new ParsingTransactionSource(result.Sequence, []);
        
        result.Sequence.Position = 2;
        var transaction = source.BeginTransaction();
        _ = source.BeginTransaction();
        result.Sequence.Position = 1;
        Assert.Throws<Exception>(() => transaction.Commit());
    }

    [Fact]
    public void CommitWithInnerTransactionHasException()
    {
        var result = string.Empty.Tokenize();
        var source = new ParsingTransactionSource(result.Sequence, []);
        var transaction = source.BeginTransaction();
        var transaction2 = source.BeginTransaction();
        var ex = new PlampException(PlampNativeExceptionInfo.InvalidTypeName(), default, default);
        transaction2.AddException(ex);
        Assert.Throws<Exception>(() => transaction.Commit());
    }

    [Fact]
    public void CommitTwice()
    {
        var result = "()".Tokenize();
        var source = new ParsingTransactionSource(result.Sequence, []);
        var transaction = source.BeginTransaction();
        result.Sequence.Position = 1;
        transaction.Commit();
        transaction.Commit();
        Assert.Equal(1, result.Sequence.Position);
        Assert.Empty(source.Exceptions);
    }

    #endregion

    #region Rollback

    [Fact]
    public void RollbackWithoutExceptions()
    {
        var result = "0 0".Tokenize();
        var source = new ParsingTransactionSource(result.Sequence, []);
        var transaction = source.BeginTransaction();
        result.Sequence.Position = 2;
        transaction.Rollback();
        Assert.Equal(-1, result.Sequence.Position);
        Assert.Empty(source.Exceptions);
    }

    [Fact]
    public void RollbackWithExceptions()
    {
        var result = "0 0".Tokenize();
        var source = new ParsingTransactionSource(result.Sequence, []);
        var transaction = source.BeginTransaction();
        transaction.AddException(new PlampException(PlampNativeExceptionInfo.InvalidTypeName(), new (0, 0), new (0, 1)));
        result.Sequence.Position = 2;
        transaction.Rollback();
        Assert.Equal(-1, result.Sequence.Position);
        Assert.Empty(source.Exceptions);
    }

    [Fact]
    public void RollbackWithInnerTransactionHasNoException()
    {
        var result = "0 0".Tokenize();
        var source = new ParsingTransactionSource(result.Sequence, []);
        var transaction = source.BeginTransaction();
        _ = source.BeginTransaction();
        result.Sequence.Position = 2;
        Assert.Throws<Exception>(() => transaction.Rollback());
    }

    [Fact]
    public void RollbackWithInnerTransactionHasException()
    {
        var result = "0 0".Tokenize();
        var source = new ParsingTransactionSource(result.Sequence, []);
        var transaction = source.BeginTransaction();
        var transaction2 = source.BeginTransaction();
        transaction2.AddException(new PlampException(PlampNativeExceptionInfo.InvalidTypeName(), new(0, 0), new(0, 1)));
        Assert.Throws<Exception>(() => transaction.Rollback());
    }

    [Fact]
    public void RollbackTwice()
    {
        var result = "0 0".Tokenize();
        var source = new ParsingTransactionSource(result.Sequence, []);
        var transaction = source.BeginTransaction();
        result.Sequence.Position = 1;
        transaction.Rollback();
        transaction.Rollback();
        Assert.Equal(-1, result.Sequence.Position);
        Assert.Empty(source.Exceptions);
    }
    
    #endregion

    #region Pass

    [Fact]
    public void PassWithoutExceptions()
    {
        var result = "0 0".Tokenize();
        var source = new ParsingTransactionSource(result.Sequence, []);
        var transaction = source.BeginTransaction();
        result.Sequence.Position = 1;
        transaction.Pass();
        Assert.Equal(1, result.Sequence.Position);
        Assert.Empty(source.Exceptions);
    }

    [Fact]
    public void PassWithExceptions()
    {
        var result = "0 0".Tokenize();
        var source = new ParsingTransactionSource(result.Sequence, []);
        var transaction = source.BeginTransaction();
        result.Sequence.Position = 1;
        transaction.AddException(new PlampException(PlampNativeExceptionInfo.InvalidTypeName(), new(0, 0), new(0, 1)));
        transaction.Pass();
        Assert.Equal(1, result.Sequence.Position);
        Assert.Empty(source.Exceptions);
    }

    [Fact]
    public void PassWithInnerTransactionHasNoExceptionButChangePosition()
    {
        var result = "0 0".Tokenize();
        var source = new ParsingTransactionSource(result.Sequence, []);
        var transaction = source.BeginTransaction();
        _ = source.BeginTransaction();
        result.Sequence.Position = 1;
        Assert.Throws<Exception>(() => transaction.Pass());
    }
    
    [Fact]
    public void PassWithInnerTransactionHasNoException()
    {
        var result = "0 0".Tokenize();
        var source = new ParsingTransactionSource(result.Sequence, []);
        var transaction = source.BeginTransaction();
        result.Sequence.Position = 1;
        _ = source.BeginTransaction();
        Assert.Throws<Exception>(() => transaction.Pass());
    }

    [Fact]
    public void PassWithInnerTransactionHasException()
    {
        var result = "0 0".Tokenize();
        var source = new ParsingTransactionSource(result.Sequence, []);
        var transaction = source.BeginTransaction();
        result.Sequence.Position = 1;
        var transaction2 = source.BeginTransaction();
        transaction2.AddException(new PlampException(PlampNativeExceptionInfo.InvalidTypeName(), new(0, 0), new(0, 1)));
        Assert.Throws<Exception>(() => transaction.Pass());
    }

    [Fact]
    public void PassTwice()
    {
        var result = "0 0".Tokenize();
        var source = new ParsingTransactionSource(result.Sequence, []);
        var transaction = source.BeginTransaction();
        result.Sequence.Position = 1;
        transaction.Pass();
        transaction.Pass();
        Assert.Equal(1, result.Sequence.Position);
        Assert.Empty(source.Exceptions);
    }

    #endregion

    #region AddException

    [Fact]
    public void AddExceptionToUncompletedTransaction()
    {
        var result = "0 0".Tokenize();
        var source = new ParsingTransactionSource(result.Sequence, []);
        var transaction = source.BeginTransaction();
        //Yep, just call this method and believe that it won't fail
        transaction.AddException(new PlampException(PlampNativeExceptionInfo.InvalidTypeName(), new(0, 0), new(0, 1)));
    }

    [Fact]
    public void AddExceptionToCompletedTransaction()
    {
        var result = "0 0".Tokenize();
        var source = new ParsingTransactionSource(result.Sequence, []);
        var transaction = source.BeginTransaction();
        transaction.Commit();
        Assert.Throws<Exception>(() => transaction.AddException(new PlampException(PlampNativeExceptionInfo.InvalidTypeName(), new(0, 0), new(0, 1))));
    }

    #endregion
}