using System.Collections.Generic;
using System.Reflection;
using plamp.Abstractions.Compilation;
using plamp.Abstractions.Compilation.Models;

namespace plamp.Compiler.Model;

internal record ReadOnlyAssemblyTreeNode
{
    public virtual AssemblyName Name { get; init; }

    public virtual IReadOnlySet<SourceFile> SourceFiles { get; } = new HashSet<SourceFile>();

    public virtual IReadOnlyList<ReadOnlyAssemblyTreeNode> DynamicReferences { get; } = [];
    
    public virtual IReadOnlyList<Assembly> StaticReferences { get; } = [];
}