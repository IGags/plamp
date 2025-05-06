using System.Reflection;
using System.Threading;
using plamp.Abstractions.Compilation;
using plamp.Abstractions.Compilation.Models;

namespace plamp.Abstractions.Parsing;

public interface IParser
{
    public ParserResult Parse(SourceFile sourceFile, AssemblyName assemblyName, CancellationToken cancellationToken);
}