using System.Collections;
using System.Collections.Generic;
using plamp.Abstractions.Ast;
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

    public FilePosition CurrentStart
    {
        get
        {
            if (_position < 0 || _tokenList.Count == 0)
            {
                return new(-1, -1);
            }
            return _position >= _tokenList.Count ? new(-1, -1) : Current().Start;
        }
    }

    public FilePosition CurrentEnd
    {
        get
        {
            if (_tokenList.Count == 0)
            {
                return new(-1, -1);
            }
            if (_position >= _tokenList.Count)
            {
                return new(-1, -1);
            }
            return _position < 0 ? new(-1, -1) : Current().End;
        }
    }
    
    public TokenSequence(List<TokenBase> tokenList)
    {
        _tokenList = tokenList;
    }
    
    public TokenBase GetNextToken()
    {
        _position++;
        return Current();
    }

    public TokenBase GetNextNonWhiteSpace()
    {
        do
        {
            _position++;
            var current = Current();
            if (current == null)
            {
                break;
            }

            if (current.GetType() != typeof(WhiteSpace))
            {
                return current;
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
            var current = Current();
            if (current == null)
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