using System;
using System.Collections.Generic;
using plamp.Native.Tokenization.Token;

namespace plamp.Native.Tokenization;

public class TokenSequence
{
    private readonly List<TokenBase> _tokenList;
    private int _position = -1;

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

    public TokenBase PeekNextNonWhiteSpace(int shift = 0)
    {
        if (shift < 0)
        {
            throw new ArgumentException();
        }
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
            if (shift <= 0)
            {
                return _tokenList[pos];
            }

            shift--;
        } while (_tokenList.Count > pos);

        return null;
    }

    public TokenBase PeekNext(int shift = 0)
    {
        var pos = _position + shift;
        pos = pos == _tokenList.Count ? pos : ++pos;
        return _tokenList.Count <= pos ? null : _tokenList[pos];
    }

    public TokenBase Current()
    {
        return _tokenList.Count <= _position || _position < 0 ? null : _tokenList[_position];
    }

    public TokenBase RollBack(int count = 1)
    {
        for (int i = 0; i < count && _position > 0; i++)
        {
            _position = _position == 0 ? 0 : --_position;
            
        }
        return _tokenList.Count <= _position || _position < 0 ? null : _tokenList[_position];
    }

    public TokenBase RollBackToNonWhiteSpace(int shift = 0)
    {
        if (shift < 0)
        {
            throw new ArgumentException();
        }
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

            if (shift <= 0)
            {
                return _tokenList[_position];
            }

            shift--;
        } while (_position != -1);

        return null;
    }

    public void ToBegin()
    {
        _position = -1;
    }
}