using System;
using System.Reflection;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;
using plamp.Abstractions.AstManipulation.Validation;

namespace plamp.Alternative.Visitors.ModuleCreation;

public class TypeCreatorValidator : BaseValidator<CreationContext, CreationContext>
{
    protected override CreationContext CreateInnerContext(CreationContext context) => context;

    protected override CreationContext MapInnerToOuter(CreationContext outerContext, CreationContext innerContext) => innerContext;

    protected override VisitResult PreVisitTypedef(TypedefNode node, CreationContext context, NodeBase? parent)
    {
        var typeBuilder = context.ModuleBuilder.DefineType(
            node.Name.Value,
            TypeAttributes.SequentialLayout | TypeAttributes.Public | TypeAttributes.Sealed, 
            typeof(ValueType));
        
        node.Type = typeBuilder;
        return VisitResult.SkipChildren;
    }
}