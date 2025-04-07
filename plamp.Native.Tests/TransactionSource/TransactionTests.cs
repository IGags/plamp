using System;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Native.Parsing.Symbols;
using plamp.Native.Parsing.Transactions;
using plamp.Native.Tokenization;
using plamp.Native.Tokenization.Token;
using Xunit;

namespace plamp.Native.Tests.TransactionSource;

public class TransactionTests
{
    #region Commit

    [Fact]
    public void CommitWithoutExceptions()
    {
        var result = ParserTestHelper.GetSourceCode("+ -").Tokenize(ParserTestHelper.AssemblyName);
        var source = new ParsingTransactionSource(result.Sequence, [], []);
        var transaction = source.BeginTransaction();
        result.Sequence.Position = 2;
        transaction.Commit();
        
        Assert.Equal(2, result.Sequence.Position);
        Assert.Empty(source.Exceptions);
    }

    [Fact]
    public void CommitWithExceptions()
    {
        var result = ParserTestHelper.GetSourceCode(string.Empty).Tokenize(ParserTestHelper.AssemblyName);
        var source = new ParsingTransactionSource(result.Sequence, [], []);

        var ex = new PlampException(
            PlampNativeExceptionInfo.InvalidCastOperator(),
            default,
            default,
            ParserTestHelper.FileName,
            ParserTestHelper.AssemblyName);
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
        var result = ParserTestHelper.GetSourceCode("(((").Tokenize(ParserTestHelper.AssemblyName);
        var source = new ParsingTransactionSource(result.Sequence, [], []);
        
        result.Sequence.Position = 2;
        var transaction = source.BeginTransaction();
        _ = source.BeginTransaction();
        result.Sequence.Position = 1;
        Assert.Throws<Exception>(() => transaction.Commit());
    }

    [Fact]
    public void CommitWithInnerTransactionHasException()
    {
        var result = ParserTestHelper.GetSourceCode(string.Empty).Tokenize(ParserTestHelper.AssemblyName);
        var source = new ParsingTransactionSource(result.Sequence, [], []);
        var transaction = source.BeginTransaction();
        var transaction2 = source.BeginTransaction();
        var ex = new PlampException(
            PlampNativeExceptionInfo.InvalidTypeName(),
            default,
            default,
            ParserTestHelper.FileName,
            ParserTestHelper.AssemblyName);
        transaction2.AddException(ex);
        Assert.Throws<Exception>(() => transaction.Commit());
    }

    [Fact]
    public void CommitTwice()
    {
        var result = ParserTestHelper.GetSourceCode("()").Tokenize(ParserTestHelper.AssemblyName);
        var source = new ParsingTransactionSource(result.Sequence, [], []);
        var transaction = source.BeginTransaction();
        result.Sequence.Position = 1;
        transaction.Commit();
        transaction.Commit();
        Assert.Equal(1, result.Sequence.Position);
        Assert.Empty(source.Exceptions);
    }

    [Fact]
    public void CommitWithSymbol()
    {
        var result = ParserTestHelper.GetSourceCode("1").Tokenize(ParserTestHelper.AssemblyName);
        var source = new ParsingTransactionSource(result.Sequence, [], []);
        var transaction = source.BeginTransaction();
        result.Sequence.Position = 1;
        var literal = new NumberLiteral("1", new(0, 0), new(0, 0), 1, typeof(int));
        var node = new LiteralNode(1, typeof(int));
        transaction.AddSymbol(node, [], [literal]);
        transaction.Commit();
        Assert.Equal(1, result.Sequence.Position);
        Assert.Empty(source.Exceptions);
        Assert.Single(source.SymbolDictionary);
        Assert.True(source.SymbolDictionary.ContainsKey(node));
        var symbol = source.SymbolDictionary[node];
        var symbolEntry = new PlampNativeSymbolRecord([], [literal]);
        Assert.Equal(symbolEntry, symbol);
    }

    #endregion

    #region Rollback

    [Fact]
    public void RollbackWithoutExceptions()
    {
        var result = ParserTestHelper.GetSourceCode("0 0").Tokenize(ParserTestHelper.AssemblyName);
        var source = new ParsingTransactionSource(result.Sequence, [], []);
        var transaction = source.BeginTransaction();
        result.Sequence.Position = 2;
        transaction.Rollback();
        Assert.Equal(-1, result.Sequence.Position);
        Assert.Empty(source.Exceptions);
    }

    [Fact]
    public void RollbackWithExceptions()
    {
        var result = ParserTestHelper.GetSourceCode("0 0").Tokenize(ParserTestHelper.AssemblyName);
        var source = new ParsingTransactionSource(result.Sequence, [], []);
        var transaction = source.BeginTransaction();
        transaction.AddException(
            new PlampException(
                PlampNativeExceptionInfo.InvalidTypeName(),
                new(0, 0),
                new(0, 1),
                ParserTestHelper.FileName,
                ParserTestHelper.AssemblyName));
        result.Sequence.Position = 2;
        transaction.Rollback();
        Assert.Equal(-1, result.Sequence.Position);
        Assert.Empty(source.Exceptions);
    }

    [Fact]
    public void RollbackWithInnerTransactionHasNoException()
    {
        var result = ParserTestHelper.GetSourceCode("0 0").Tokenize(ParserTestHelper.AssemblyName);
        var source = new ParsingTransactionSource(result.Sequence, [], []);
        var transaction = source.BeginTransaction();
        _ = source.BeginTransaction();
        result.Sequence.Position = 2;
        Assert.Throws<Exception>(() => transaction.Rollback());
    }

    [Fact]
    public void RollbackWithInnerTransactionHasException()
    {
        var result = ParserTestHelper.GetSourceCode("0 0").Tokenize(ParserTestHelper.AssemblyName);
        var source = new ParsingTransactionSource(result.Sequence, [], []);
        var transaction = source.BeginTransaction();
        var transaction2 = source.BeginTransaction();
        transaction2.AddException(
            new PlampException(PlampNativeExceptionInfo.InvalidTypeName(),
            new(0, 0),
            new(0, 1),
            ParserTestHelper.FileName,
            ParserTestHelper.AssemblyName));
        Assert.Throws<Exception>(() => transaction.Rollback());
    }

    [Fact]
    public void RollbackTwice()
    {
        var result = ParserTestHelper.GetSourceCode("0 0").Tokenize(ParserTestHelper.AssemblyName);
        var source = new ParsingTransactionSource(result.Sequence, [], []);
        var transaction = source.BeginTransaction();
        result.Sequence.Position = 1;
        transaction.Rollback();
        transaction.Rollback();
        Assert.Equal(-1, result.Sequence.Position);
        Assert.Empty(source.Exceptions);
    }
    
    [Fact]
    public void RollbackWithSymbol()
    {
        var result = ParserTestHelper.GetSourceCode("1").Tokenize(ParserTestHelper.AssemblyName);
        var source = new ParsingTransactionSource(result.Sequence, [], []);
        var transaction = source.BeginTransaction();
        result.Sequence.Position = 1;
        var literal = new NumberLiteral("1", new(0, 0), new(0, 0), 1, typeof(int));
        var node = new LiteralNode(1, typeof(int));
        transaction.AddSymbol(node, [], [literal]);
        transaction.Rollback();
        Assert.Equal(-1, result.Sequence.Position);
        Assert.Empty(source.Exceptions);
        Assert.Empty(source.SymbolDictionary);
    }
    
    #endregion

    #region Pass

    [Fact]
    public void PassWithoutExceptions()
    {
        var result = ParserTestHelper.GetSourceCode("0 0").Tokenize(ParserTestHelper.AssemblyName);
        var source = new ParsingTransactionSource(result.Sequence, [], []);
        var transaction = source.BeginTransaction();
        result.Sequence.Position = 1;
        transaction.Pass();
        Assert.Equal(1, result.Sequence.Position);
        Assert.Empty(source.Exceptions);
    }

    [Fact]
    public void PassWithExceptions()
    {
        var result = ParserTestHelper.GetSourceCode("0 0").Tokenize(ParserTestHelper.AssemblyName);
        var source = new ParsingTransactionSource(result.Sequence, [], []);
        var transaction = source.BeginTransaction();
        result.Sequence.Position = 1;
        transaction.AddException(new PlampException(PlampNativeExceptionInfo.InvalidTypeName(),
            new(0, 0),
            new(0, 1),
            ParserTestHelper.FileName,
            ParserTestHelper.AssemblyName));
        transaction.Pass();
        Assert.Equal(1, result.Sequence.Position);
        Assert.Empty(source.Exceptions);
    }

    [Fact]
    public void PassWithInnerTransactionHasNoExceptionButChangePosition()
    {
        var result = ParserTestHelper.GetSourceCode("0 0").Tokenize(ParserTestHelper.AssemblyName);
        var source = new ParsingTransactionSource(result.Sequence, [], []);
        var transaction = source.BeginTransaction();
        _ = source.BeginTransaction();
        result.Sequence.Position = 1;
        Assert.Throws<Exception>(() => transaction.Pass());
    }
    
    [Fact]
    public void PassWithInnerTransactionHasNoException()
    {
        var result = ParserTestHelper.GetSourceCode("0 0").Tokenize(ParserTestHelper.AssemblyName);
        var source = new ParsingTransactionSource(result.Sequence, [], []);
        var transaction = source.BeginTransaction();
        result.Sequence.Position = 1;
        _ = source.BeginTransaction();
        Assert.Throws<Exception>(() => transaction.Pass());
    }

    [Fact]
    public void PassWithInnerTransactionHasException()
    {
        var result = ParserTestHelper.GetSourceCode("0 0").Tokenize(ParserTestHelper.AssemblyName);
        var source = new ParsingTransactionSource(result.Sequence, [], []);
        var transaction = source.BeginTransaction();
        result.Sequence.Position = 1;
        var transaction2 = source.BeginTransaction();
        transaction2.AddException(
            new PlampException(PlampNativeExceptionInfo.InvalidTypeName(),
            new(0, 0),
            new(0, 1),
            ParserTestHelper.FileName,
            ParserTestHelper.AssemblyName));
        Assert.Throws<Exception>(() => transaction.Pass());
    }

    [Fact]
    public void PassTwice()
    {
        var result = ParserTestHelper.GetSourceCode("0 0").Tokenize(ParserTestHelper.AssemblyName);
        var source = new ParsingTransactionSource(result.Sequence, [], []);
        var transaction = source.BeginTransaction();
        result.Sequence.Position = 1;
        transaction.Pass();
        transaction.Pass();
        Assert.Equal(1, result.Sequence.Position);
        Assert.Empty(source.Exceptions);
    }
    
    [Fact]
    public void PassWithSymbol()
    {
        var result = ParserTestHelper.GetSourceCode("1").Tokenize(ParserTestHelper.AssemblyName);
        var source = new ParsingTransactionSource(result.Sequence, [], []);
        var transaction = source.BeginTransaction();
        result.Sequence.Position = 1;
        var literal = new NumberLiteral("1", new(0, 0), new(0, 0), 1, typeof(int));
        var node = new LiteralNode(1, typeof(int));
        transaction.AddSymbol(node, [], [literal]);
        transaction.Pass();
        Assert.Equal(1, result.Sequence.Position);
        Assert.Empty(source.Exceptions);
        Assert.Empty(source.SymbolDictionary);
    }

    #endregion

    #region AddException

    [Fact]
    public void AddExceptionToUncompletedTransaction()
    {
        var result = ParserTestHelper.GetSourceCode("0 0").Tokenize(ParserTestHelper.AssemblyName);
        var source = new ParsingTransactionSource(result.Sequence, [], []);
        var transaction = source.BeginTransaction();
        //Yep, just call this method and believe that it won't fail
        transaction.AddException(
            new PlampException(PlampNativeExceptionInfo.InvalidTypeName(),
            new(0, 0),
            new(0, 1),
            ParserTestHelper.FileName,
            ParserTestHelper.AssemblyName));
    }

    [Fact]
    public void AddExceptionToCompletedTransaction()
    {
        var result = ParserTestHelper.GetSourceCode("0 0").Tokenize(ParserTestHelper.AssemblyName);
        var source = new ParsingTransactionSource(result.Sequence, [], []);
        var transaction = source.BeginTransaction();
        transaction.Commit();
        Assert.Throws<Exception>(() => transaction.AddException(
            new PlampException(
                PlampNativeExceptionInfo.InvalidTypeName(),
                new(0, 0),
                new(0, 1),
                ParserTestHelper.FileName,
                ParserTestHelper.AssemblyName)));
    }

    #endregion

    #region AddSymbol

    [Fact]
    public void AddSymbolToUncompletedTransaction()
    {
        var result = ParserTestHelper.GetSourceCode("0 0").Tokenize(ParserTestHelper.AssemblyName);
        var source = new ParsingTransactionSource(result.Sequence, [], []);
        var transaction = source.BeginTransaction();
        //Yep, just call this method and believe that it won't fail
        transaction.AddSymbol(new LiteralNode(1, typeof(int)), [], []);
    }

    [Fact]
    public void AddSymbolToCompletedTransaction()
    {
        var result = ParserTestHelper.GetSourceCode("0 0").Tokenize(ParserTestHelper.AssemblyName);
        var source = new ParsingTransactionSource(result.Sequence, [], []);
        var transaction = source.BeginTransaction();
        transaction.Commit();
        Assert.Throws<Exception>(() => transaction.AddSymbol(new LiteralNode(1, typeof(int)), [], []));
    }

    #endregion
}