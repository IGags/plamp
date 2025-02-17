using System;
using System.Collections.Generic;
using System.Linq;
using plamp.Ast.Node;
using plamp.Native.Tokenization.Token;

namespace plamp.Native.Parsing.Symbols;

/// <summary>
/// Record that represent symbol own tokens(without children) and child nodes
/// </summary>
internal record PlampNativeSymbolRecord(IReadOnlyList<NodeBase> Children, IReadOnlyList<TokenBase> Tokens)
{
    public virtual bool Equals(PlampNativeSymbolRecord other)
    {
        if(other == null) return false;
        return Children.SequenceEqual(other.Children) &&
               Tokens.SequenceEqual(other.Tokens);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var child in Children)
        {
            hash.Add(child);
        }

        foreach (var token in Tokens)
        {
            hash.Add(token);
        }
        return hash.ToHashCode();
    }
}