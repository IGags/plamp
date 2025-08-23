using System.Collections.Generic;
using System.Linq;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.AstManipulation.Modification;

namespace plamp.Alternative.Visitors.ModulePreCreation.ImplicitReturnInVoid;

public class ImplicitReturnInVoidFuncWeaver : BaseWeaver<PreCreationContext, PreCreationContext>
{
    protected override PreCreationContext CreateInnerContext(PreCreationContext context) => context;

    protected override PreCreationContext MapInnerToOuter(PreCreationContext innerContext, PreCreationContext outerContext) => innerContext;

    protected override VisitResult PreVisitFunction(FuncNode node, PreCreationContext context, NodeBase? parent)
    {
        if (node.ReturnType is not null && node.ReturnType.Symbol != typeof(void)) return VisitResult.SkipChildren;
        if(node.Body.ExpressionList.Any(x => x is ReturnNode)) return VisitResult.SkipChildren;

        var returnNode = new ReturnNode(null);
        
        //TODO: special place for virtual nodes
        context.SymbolTable.AddSymbol(returnNode, new FilePosition(-1, -1), new FilePosition(-1, -1));
        var expressions = new List<NodeBase>(node.Body.ExpressionList) { returnNode };
        var body = new BodyNode(expressions);
        Replace(node.Body, body, context);
        return VisitResult.SkipChildren;
    }
}