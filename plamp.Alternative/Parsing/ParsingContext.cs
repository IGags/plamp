using System.Collections.Generic;
using plamp.Abstractions.Ast;
using plamp.Alternative.Tokenization;

namespace plamp.Alternative.Parsing;

public record ParsingContext(
    TokenSequence Sequence,
    string FileName,
    List<PlampException> Exceptions,
    SymbolTable SymbolTable)
{
    public ParsingContext Fork() => this with
    {
        Sequence = Sequence.Fork(),
        Exceptions = []
    };

    public void Merge(ParsingContext other)
    {
        Sequence.Position = other.Sequence.Position;
        Exceptions.AddRange(other.Exceptions);
    }
}