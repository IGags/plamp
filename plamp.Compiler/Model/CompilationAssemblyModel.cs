using System.Collections.Generic;
using System.Reflection;
using plamp.Abstractions.Compilation;
using plamp.Abstractions.Compilation.Models;

namespace plamp.Compiler.Model;

public record CompilationAssemblyModel(
    AssemblyName Name,
    HashSet<SourceFile> SourceFiles,
    HashSet<AssemblyName> References);