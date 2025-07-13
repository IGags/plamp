using System.Collections.Generic;
using plamp.Abstractions.Ast;

namespace plamp.Alternative.Tokenization;

public record TokenizationResult(TokenSequence Sequence, List<PlampException> Exceptions);