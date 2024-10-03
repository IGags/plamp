using System.Collections.Generic;
using plamp.Ast.Node;
using plamp.Native.Tokenization;

namespace plamp.Native.Parsing;

public record ParserResult(List<NodeBase> NodeList, List<ParserException> ParserExceptions, List<TokenizeException> TokenizeExceptions);