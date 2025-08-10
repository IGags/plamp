using System;
using System.Collections;
using System.Collections.Generic;
using plamp.Abstractions.Ast;
using plamp.Alternative.Tokenization.Token;

namespace plamp.Alternative.Tokenization;

public class TokenSequence : IEnumerable<TokenBase>
{
    private int _position;
    private readonly List<TokenBase> _tokenList;

    public TokenSequence(List<TokenBase> tokenList)
    {
        if (tokenList.Count == 0)
        {
            throw new ArgumentException("Token sequence cannot be empty");
        }
        _tokenList = tokenList;
    }

    public int Position
    {
        get => _position;
        set
        {
            if (value < 0 || value >= _tokenList.Count)
            {
                throw new ArgumentException("Invalid position");
            }
            _position = value;
        }
    }
    
    public FilePosition CurrentStart => Current().Start;

    public FilePosition CurrentEnd => Current().End;

    public TokenSequence Fork() => new(_tokenList) { _position = _position };

    public bool MoveNext()
    {
        if (_position + 1 >= _tokenList.Count) return false;
        _position++;
        return true;
    }

    public bool MoveNextNonWhiteSpace()
    {
        while(true)
        {
            if(!MoveNext()) return false;
            var current = Current();

            if (current.GetType() != typeof(WhiteSpace))
            {
                return true;
            }
        }
    }

    public TokenBase Current() => _tokenList[_position];

    /// <summary>
    /// For testing purposes
    /// </summary>
    public IEnumerator<TokenBase> GetEnumerator()
    {
        return _tokenList.GetEnumerator();
    }

    /// <summary>
    /// For testing purposes
    /// </summary>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}