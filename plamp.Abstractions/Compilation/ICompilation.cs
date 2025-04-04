using System;
using System.Collections.Generic;
using System.Reflection;

namespace plamp.Abstractions.Compilation;

public interface ICompilation
{
    void AddDynamicAssembly(AssemblyName assemblyName, HashSet<SourceFile> sourceFile, Type entrypointType = null);

    bool TryCompile(out List<CompilationResult> results);
}