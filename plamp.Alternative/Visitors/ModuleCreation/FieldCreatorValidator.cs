using System.Reflection;
using System.Reflection.Emit;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;
using plamp.Abstractions.AstManipulation.Validation;
using plamp.Alternative.SymbolsImpl;

namespace plamp.Alternative.Visitors.ModuleCreation;

public class FieldCreatorValidator : BaseValidator<CreationContext, CreationContext>
{
    protected override CreationContext CreateInnerContext(CreationContext context) => context;

    protected override CreationContext MapInnerToOuter(CreationContext outerContext, CreationContext innerContext) => innerContext;

    protected override VisitResult PreVisitFieldDef(FieldDefNode defNode, CreationContext context, NodeBase? parent)
    {
        if (parent is not TypedefNode {Type: {} typeBuilder } || defNode.FieldType.TypeInfo is not {} fieldTypeInfo) return VisitResult.SkipChildren;
        var fldBuilder = typeBuilder.DefineField(defNode.Name.Value, fieldTypeInfo.AsType(), FieldAttributes.Public);
        var builder = new CustomAttributeBuilder(typeof(PlampFieldGeneratedAttribute).GetConstructor([])!, []);
        fldBuilder.SetCustomAttribute(builder);
        defNode.Field = fldBuilder;
        return VisitResult.SkipChildren;
    }
}