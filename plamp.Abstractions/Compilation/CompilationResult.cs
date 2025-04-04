using System.Reflection;

namespace plamp.Abstractions.Compilation;

public record CompilationResult(object Entrypoint, bool Success, Assembly CompiledAssembly);