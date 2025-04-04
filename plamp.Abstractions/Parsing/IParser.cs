using System.Reflection;
using plamp.Abstractions.Compilation;

namespace plamp.Abstractions.Parsing;

public interface IParser
{
    public ParserResult Parse(SourceFile sourceFile, AssemblyName assemblyName);
}