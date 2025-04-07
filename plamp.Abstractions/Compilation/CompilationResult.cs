using System.Collections.Generic;

namespace plamp.Abstractions.Compilation;

public record CompilationResult(bool Success, List<AssemblyCompilation> Compilations);