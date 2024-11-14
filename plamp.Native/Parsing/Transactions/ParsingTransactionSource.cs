using System;
using System.Collections.Generic;
using plamp.Native.Tokenization;

namespace plamp.Native.Parsing.Transactions;

public class ParsingTransactionSource
{
    private readonly TokenSequence _tokenSequence;
    private readonly List<PlampException> _exceptions;
    private readonly Stack<ParsingTransaction> _transactionStack = [];

    public ParsingTransactionSource(TokenSequence tokenSequence, List<PlampException> exceptions)
    {
        _tokenSequence = tokenSequence;
        _exceptions = exceptions;
    }

    public IParsingTransaction BeginTransaction()
    {
        return new ParsingTransaction(_exceptions, _tokenSequence, this);
    }


    private ParsingTransaction Pop() => _transactionStack.Pop();
    
    private class ParsingTransaction : IParsingTransaction
    {
        private readonly int _tokenSequencePosition;
        private readonly List<PlampException> _exceptionList;
        private readonly TokenSequence _sequence;
        private readonly ParsingTransactionSource _source;
        private readonly List<PlampException> _temporalList = [];
    
        private bool _isComplete;
        
        public ParsingTransaction(List<PlampException> exceptionList,
            TokenSequence sequence, ParsingTransactionSource source)
        {
            _tokenSequencePosition = sequence.Position;
            _exceptionList = exceptionList;
            _sequence = sequence;
            _source = source;
        }

        public void Commit()
        {
            if (_isComplete) return;
            _exceptionList.AddRange(_temporalList);
            _isComplete = true;
            Pop();
        }

        public void Rollback()
        {
            Pass();
            _sequence.Position = _tokenSequencePosition;
        }

        public void Pass()
        {
            if (_isComplete) return;
            _isComplete = true;
            Pop();
        }

        public void AddException(PlampException exception)
        {
            if (_isComplete) throw new Exception("Transction was completed");
            _temporalList.Add(exception);
        }

        private void Pop()
        {
            ParsingTransaction res;
            do
            {
                res = _source.Pop();
                res.Rollback();
            } while (res != this);
        }
    }
}