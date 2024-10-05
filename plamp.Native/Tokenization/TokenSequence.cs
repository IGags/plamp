using System;
using System.Collections;
using System.Collections.Generic;
using plamp.Native.Tokenization.Token;

namespace plamp.Native.Tokenization;

public class TokenSequence : IEnumerable<TokenBase>
{
    private readonly List<TokenBase> _tokenList;
    private int _position = -1;

    public IReadOnlyList<TokenBase> TokenList => _tokenList;
    
    public int Position
    {
        get => _position;
        set
        {
            if (value < 0)
            {
                _position = -1;
            }
            else if(value > _tokenList.Count)
            {
                _position = _tokenList.Count;
            }
            else
            {
                _position = value;
            }
        }
    }

    public TokenPosition CurrentStart => new(Current()?.StartPosition ?? -1);
    public TokenPosition CurrentEnd => new(Current()?.EndPosition ?? -1);
    
    public TokenSequence(List<TokenBase> tokenList)
    {
        _tokenList = tokenList;
    }
    
    public TokenBase GetNextToken()
    {
        _position = _position == _tokenList.Count ? _position : ++_position;
        return _tokenList.Count <= _position || _position < 0 ? null : _tokenList[_position];
    }

    public TokenBase GetNextNonWhiteSpace()
    {
        do
        {
            _position = _position == _tokenList.Count ? _position : ++_position;
            if (_tokenList.Count <= _position)
            {
                return null;
            }

            if (_tokenList[_position].GetType() != typeof(WhiteSpace))
            {
                return _tokenList[_position];
            }
        } while (_tokenList.Count > _position);

        return null;
    }

    public TokenBase PeekNextNonWhiteSpace()
    {
        var pos = _position;
        do
        {
            pos = pos == _tokenList.Count ? pos : ++pos;
            if (_tokenList.Count <= pos)
            {
                return null;
            }

            if (_tokenList[pos].GetType() == typeof(WhiteSpace))
            {
                continue;
            }

            return _tokenList[pos];
        } while (_tokenList.Count > pos);

        return null;
    }

    public TokenBase PeekNext()
    {
        var pos = _position;
        pos = pos == _tokenList.Count ? pos : ++pos;
        return _tokenList.Count <= pos ? null : _tokenList[pos];
    }

    public TokenBase Current()
    {
        return _tokenList.Count <= _position || _position < 0 ? null : _tokenList[_position];
    }

    public TokenBase RollBackToNonWhiteSpace()
    {
        do
        {
            _position = _position == -1 ? -1 : --_position;
            if (_position < 0)
            {
                return null;
            }

            if (_tokenList[_position].GetType() == typeof(WhiteSpace))
            {
                continue;
                
            }

            return _tokenList[_position];
        } while (_position != -1);

        return null;
    }

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