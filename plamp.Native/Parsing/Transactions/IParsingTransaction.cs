using plamp.Ast;

namespace plamp.Native.Parsing.Transactions;

public interface IParsingTransaction
{
    public void Commit();

    public void Rollback();

    public void Pass();

    public void AddException(PlampException exception);
}