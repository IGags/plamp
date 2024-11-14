using System.Collections.Generic;

namespace plamp.Native.Tokenization;

public record TokenizationResult(TokenSequence Sequence, List<PlampException> Exceptions);