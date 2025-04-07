using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace plamp.Abstractions.Compilation;

public interface ICompilation
{
    void AddDynamicAssembly(AssemblyName assemblyName, HashSet<SourceFile> sourceFile, HashSet<AssemblyName> referencedAssemblies = null);

    Task<CompilationResult> TryCompileAsync();
}