using System.Collections.Generic;
using System.Reflection.Emit;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;

namespace plamp.Abstractions.CompilerEmission;

public interface ICompiledEmitter
{
    bool TryEmitType(
        NodeBase typeNode,
        ModuleBuilder builder,
        out TypeBuilder type,
        out List<PlampException> exceptions);

    bool TryEmitEnum(
        NodeBase enumNode, 
        ModuleBuilder builder, 
        out EnumBuilder enumBuilder,
        out List<PlampException> exceptions);
}