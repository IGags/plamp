using System;

namespace plamp.FluentGrammaticGenerator.Tokenization.Token;

/// <summary>
/// Token position in source code text
/// </summary>
public readonly record struct TokenPosition(int Row, int Column)
{
    public override int GetHashCode() => HashCode.Combine(Row.GetHashCode(), Column.GetHashCode());
}