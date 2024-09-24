using System.Collections.Generic;
using Parser.Token;

namespace Parser;

public class TokenSequence
{
    private readonly List<TokenBase> _tokenList;
    private int _position = -1;

    public TokenSequence(List<TokenBase> tokenList)
    {
        _tokenList = tokenList;
    }
    
    public TokenBase GetNextToken()
    {
        _position = _position == _tokenList.Count ? _position : _position++;
        return _tokenList.Count <= _position || _position < 0 ? null : _tokenList[_position];
    }

    public TokenBase GetNextNonWhiteSpace()
    {
        do
        {
            _position = _position == _tokenList.Count ? _position : _position++;
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
            pos = pos == _tokenList.Count ? pos : pos++;
            if (_tokenList.Count <= pos)
            {
                return null;
            }

            if (_tokenList[pos].GetType() != typeof(WhiteSpace))
            {
                return _tokenList[pos];
            }
        } while (_tokenList.Count > pos);

        return null;
    }

    public TokenBase Current()
    {
        return _tokenList.Count <= _position || _position < 0 ? null : _tokenList[_position];
    }

    public TokenBase RollBack()
    {
        _position = _position == 0 ? 0 : _position--;
        return _tokenList.Count <= _position || _position < 0 ? null : _tokenList[_position];
    }

    public TokenBase RollBackToNonWhiteSpace()
    {
        do
        {
            _position = _position == -1 ? -1 : _position--;
            if (_position < 0)
            {
                return null;
            }

            if (_tokenList[_position].GetType() != typeof(WhiteSpace))
            {
                return _tokenList[_position];
            }
        } while (_position != -1);

        return null;
    }

    public void ToBegin()
    {
        _position = -1;
    }
}