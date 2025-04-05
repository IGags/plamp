using System.Reflection;
using plamp.Abstractions.Compilation;
using plamp.Native.Parsing;

namespace plamp.Native.Tests;

public static class ParserTestHelper
{
    public const string FileName = "script.plp";
    
    public static readonly AssemblyName AssemblyName = typeof(ParserTestHelper).Assembly.GetName();

    public static SourceFile GetSourceCode(string sourceCode) => new(FileName, sourceCode);
    
    internal static ParsingContext GetContext(string sourceCode) 
        => PlampNativeParser.BuildContext(GetSourceCode(sourceCode), AssemblyName);
}