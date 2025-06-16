using System;
using System.Collections.Generic;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Native.Parsing.Symbols;
using plamp.Native.Tokenization;
using plamp.Native.Tokenization.Token;

namespace plamp.Native.Parsing.Transactions;

internal class ParsingTransactionSource
{
    private readonly TokenSequence _tokenSequence;
    private readonly List<PlampException> _exceptions;
    private readonly Stack<ParsingTransaction> _transactionStack = [];
    internal readonly Dictionary<NodeBase, PlampNativeSymbolRecord> SymbolDictionary;
    
    public IReadOnlyList<PlampException> Exceptions => _exceptions;
    
    public ParsingTransactionSource(
        TokenSequence tokenSequence, 
        List<PlampException> exceptions, 
        Dictionary<NodeBase, PlampNativeSymbolRecord> symbolDictionary)
    {
        _tokenSequence = tokenSequence;
        _exceptions = exceptions;
        SymbolDictionary = symbolDictionary;
    }

    public IParsingTransaction BeginTransaction()
    {
        return new ParsingTransaction(_exceptions, _tokenSequence, SymbolDictionary, this);
    }


    private ParsingTransaction Pop() => _transactionStack.Pop();
    
    private class ParsingTransaction : IParsingTransaction
    {
        private readonly int _tokenSequencePosition;
        private readonly List<PlampException> _exceptionList;
        private readonly TokenSequence _sequence;
        private readonly ParsingTransactionSource _source;
        private readonly List<PlampException> _temporalList = [];
        private readonly Dictionary<NodeBase, PlampNativeSymbolRecord> _symbolDictionary;
        private readonly Dictionary<NodeBase, PlampNativeSymbolRecord> _temporalDictionary = [];
    
        private bool _isComplete;
        
        public ParsingTransaction(List<PlampException> exceptionList,
            TokenSequence sequence,
            Dictionary<NodeBase, PlampNativeSymbolRecord> symbolDictionary,
            ParsingTransactionSource source)
        {
            _tokenSequencePosition = sequence.Position;
            _exceptionList = exceptionList;
            _sequence = sequence;
            _source = source;
            _source._transactionStack.Push(this);
            _symbolDictionary = symbolDictionary;
        }

        public void Commit()
        {
            if (_isComplete) return;
            _exceptionList.AddRange(_temporalList);
            foreach (var kvp in _temporalDictionary)
            {
                _symbolDictionary.Add(kvp.Key, kvp.Value);
            }
            Pop();
        }

        public void Rollback()
        {
            if(_isComplete) return;
            Pass();
            _sequence.Position = _tokenSequencePosition;
        }

        public void Pass()
        {
            if (_isComplete) return;
            Pop();
        }

        public void AddException(PlampException exception)
        {
            if (_isComplete) throw new Exception("Transaction was completed");
            _temporalList.Add(exception);
        }

        public void AddSymbol(NodeBase symbol, NodeBase[] children, TokenBase[] nodeTokens)
        {
            if (_isComplete) throw new Exception("Transaction was completed");
            _temporalDictionary.Add(symbol, new(children, nodeTokens));
        }

        private void Pop()
        {
            ParsingTransaction res;
            do
            {
                res = _source._transactionStack.Peek();
                if (res == this)
                {
                    res._isComplete = true;
                    _source.Pop();
                    return;
                }

                _source.Pop();
                if (res._isComplete)
                {
                    continue;
                }

                throw new Exception(
                    "Parsing inner parsing transaction is not complete, handle leak parser shutdown");
            } while (res != this);
        }
    }
}