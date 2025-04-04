using System.Collections.Generic;

namespace plamp.Ast.FileLoading;

public interface ICompilation
{
    IReadOnlyList<string> SourceFiles { get; }
}