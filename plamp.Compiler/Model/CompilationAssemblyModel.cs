using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using plamp.Abstractions.Compilation;

namespace plamp.Compiler.Model;

public record CompilationAssemblyModel(
    AssemblyName Name,
    HashSet<SourceFile> SourceFiles,
    HashSet<AssemblyReference> References);