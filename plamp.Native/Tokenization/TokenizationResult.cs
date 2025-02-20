using System.Collections.Generic;
using plamp.Ast;

namespace plamp.Native.Tokenization;

public record TokenizationResult(TokenSequence Sequence, List<PlampException> Exceptions);