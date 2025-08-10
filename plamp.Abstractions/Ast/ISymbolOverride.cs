namespace plamp.Abstractions.Ast;

/// <summary>
/// Interface that helps add exceptions to symbols more dispersively
/// </summary>
public interface ISymbolOverride
{
    public bool TryOverride(PlampExceptionRecord exceptionRecord, out PlampException exception);
}