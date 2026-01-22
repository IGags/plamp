using System.Collections.Generic;
using plamp.Abstractions.Ast;
using plamp.Alternative.Tokenization;

namespace plamp.Alternative.Parsing;

public record ParsingContext(
    TokenSequence Sequence,
    List<PlampException> Exceptions,
    ITranslationTable TranslationTable)
{
    public ParsingContext Fork() => new(Sequence.Fork(), [], TranslationTable.Fork());

    public void Merge(ParsingContext other)
    {
        Sequence.Position = other.Sequence.Position;
        Exceptions.AddRange(other.Exceptions);
        TranslationTable.Merge(other.TranslationTable);
    }
}