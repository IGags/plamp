using System.Reflection;
using plamp.Abstractions.Compilation;
using plamp.Abstractions.Compilation.Models;
using plamp.Abstractions.Parsing;

namespace plamp.Compiler.Model;

internal record WrappedParsedResult(ParserResult ParserResult, SourceFile FromSourceFile, AssemblyName AssemblyName);