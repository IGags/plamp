using System.Reflection;

namespace plamp.Abstractions.Compilation;

public record AssemblyCompilation(object Entrypoint, bool Success, Assembly CompiledAssembly);