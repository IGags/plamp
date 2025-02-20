using plamp.Ast;
using plamp.Ast.Node;
using plamp.Native.Parsing.Symbols;
using plamp.Native.Tokenization.Token;

namespace plamp.Native.Parsing.Transactions;

public interface IParsingTransaction
{
    public void Commit();

    public void Rollback();

    public void Pass();

    public void AddException(PlampException exception);

    public void AddSymbol(NodeBase symbol, NodeBase[] children, TokenBase[] nodeTokens);
}