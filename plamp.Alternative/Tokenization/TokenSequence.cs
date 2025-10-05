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
    
    public FilePosition CurrentPosition => Current().Position;
    
    public FilePosition MakeRangeFromPrevNonWhitespace(TokenBase to)
    {
        if (Current().Position.FileName != to.Position.FileName) throw new InvalidOperationException("File of target position is differ");
        if (to is WhiteSpace) throw new InvalidOperationException("Cannot make range to whitespace");
        if (_position == 0) throw new InvalidOperationException("Cannot make range from first token");
        
        var prev = default(FilePosition);
        var i = _position - 1;
        for (; i >= 0; i--)
        {
            if (_tokenList[i] is WhiteSpace) continue;
            prev = _tokenList[i].Position;
            break;
        }

        if (prev == default) throw new InvalidOperationException("Token not found in sequence");

        if (prev.CompareTo(to.Position) == 0)
        {
            if (_tokenList[i] != to) throw new InvalidOperationException("Token not found in sequence");
            return prev;
        }
        var characterLen = 0;
        if (prev.CompareTo(to.Position) != 1) throw new InvalidOperationException("Cannot make range to token that after current");
        
        for (; i >= 0; i--)
        {
            var token = _tokenList[i];
            characterLen += token.Position.CharacterLength;
            if (token == to) return token.Position with { CharacterLength = characterLen };
        }
        throw new InvalidOperationException("Token not found in sequence");
    }

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