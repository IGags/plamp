using System.Collections.Generic;
using System.Reflection;
using plamp.Abstractions.Compilation;
using plamp.Abstractions.Compilation.Models;

namespace plamp.Compiler.Model;

internal record AssemblyTreeNode : ReadOnlyAssemblyTreeNode
{
    public override AssemblyName Name { get; init; }

    public override HashSet<SourceFile> SourceFiles { get; } = new();

    public override List<AssemblyTreeNode> DynamicReferences { get; } = [];
    
    public override List<Assembly> StaticReferences { get; } = [];
    
    public HashSet<AssemblyName> RemainReferences { get; init; } = [];
}