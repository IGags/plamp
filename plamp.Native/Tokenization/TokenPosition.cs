using System;

namespace plamp.Native.Tokenization;

/// <summary>
/// Позиция в файле
/// </summary>
public record struct TokenPosition(int Row, int Column) : IComparable<TokenPosition>
{
    public int CompareTo(TokenPosition other)
    {
        if (Row == other.Row && Column == other.Column)
        {
            return 0;
        }

        if (Row < other.Row || (Row == other.Row && Column < other.Column))
        {
            return -1;
        }

        return 1;
    }
}