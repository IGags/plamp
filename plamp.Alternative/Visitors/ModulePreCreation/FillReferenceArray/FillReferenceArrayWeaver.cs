using System;
using System.Linq;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Binary;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ComplexTypes;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Abstractions.Ast.Node.Definitions.Variable;
using plamp.Abstractions.Ast.Node.Unary;
using plamp.Abstractions.AstManipulation.Modification;
using plamp.Abstractions.Symbols.SymTable;

namespace plamp.Alternative.Visitors.ModulePreCreation.FillReferenceArray;

public class FillReferenceArrayWeaver : BaseWeaver<PreCreationContext, FillReferenceArrayContext>
{
    protected override FillReferenceArrayContext CreateInnerContext(PreCreationContext context) => new(context);

    protected override PreCreationContext MapInnerToOuter(FillReferenceArrayContext innerContext, PreCreationContext outerContext) => innerContext;

    protected override VisitResult PreVisitAssign(AssignNode node, FillReferenceArrayContext context, NodeBase? parent)
    {
        if (node.Sources.Count != node.Targets.Count) return VisitResult.SkipChildren;
        for (var i = 0; i < node.Targets.Count; i++)
        {
            //Если не массив или если он пуст, то не имеет смысла его заполнять
            if (node.Sources[i] is not InitArrayNode initArray || initArray.LengthDefinition is LiteralNode {Value: 0}) continue;
            var info = initArray.ArrayItemType.TypeInfo;
            //Такое может быть, когда мы не поняли, что за тип перед нами ранее при выводе типов
            if(info == null) continue;
            if(SymbolSearchUtility.IsNumeric(info) || SymbolSearchUtility.IsLogical(info)) continue;
            context.ToFill.Add(new(node.Targets[i], info));
        }

        return VisitResult.SkipChildren;
    }

    protected override VisitResult PostVisitInstruction(NodeBase node, FillReferenceArrayContext context, NodeBase? parent)
    {
        var instructions = context.NewInstructions.Peek();
        instructions.Add(node);
        foreach (var toFill in context.ToFill)
        { 
            var target = toFill.FillTarget is VariableDefinitionNode varDef ? new MemberNode(varDef.Name.Value) : toFill.FillTarget;
            var item = CreateItemNodeToFill(toFill.ItemType);
            
            var intType = new TypeNode(new TypeNameNode("int")) { TypeInfo = Builtins.Int };
            
            var iteratorName = new VariableNameNode(context.GetVariableName());
            var iteratorDef = new VariableDefinitionNode(intType, iteratorName);

            var lengthName = new VariableNameNode(context.GetVariableName());
            var lengthDef = new VariableDefinitionNode(intType, lengthName);

            var funcSymbol = Builtins.SymTable.FindFuncs("length").Single(x => x.Arguments[0].Type.AsType() == typeof(Array));
            var lengthCall = new CallNode(null, new FuncCallNameNode("length"), [target]){FnInfo = funcSymbol};
            var assign = new AssignNode([iteratorDef, lengthDef], [new LiteralNode(0, Builtins.Int), lengthCall]);


            var indexer = new IndexerNode(target, new MemberNode(iteratorName.Value)) { ItemType = toFill.ItemType };
            var loop = new WhileNode(
                new LessNode(new MemberNode(iteratorName.Value), new MemberNode(lengthName.Value)),
                new BodyNode(
                [
                    new AssignNode([indexer], [item]),
                    new PostfixIncrementNode(new MemberNode(iteratorName.Value))
                ])
            );
            
            instructions.Add(assign);
            instructions.Add(loop);
        }
        
        context.ToFill.Clear();
        return VisitResult.Continue;
    }

    private NodeBase CreateItemNodeToFill(ITypeInfo itemType)
    {
        if (itemType.IsArrayType)
        {
            var element = itemType.ElementType()!;
            var type = new TypeNode(new TypeNameNode(element.Name)){TypeInfo = element};
            return new InitArrayNode(type, new LiteralNode(0, Builtins.Int));
        }

        if (SymbolSearchUtility.IsString(itemType))
        {
            return new LiteralNode("", Builtins.String);
        }
        //Иначе думаем, что это пользователький тип.
        var typeNode = new TypeNode(new TypeNameNode(itemType.Name)) { TypeInfo = itemType };
        return new InitTypeNode(typeNode, []);
    }

    protected override VisitResult PreVisitBody(BodyNode node, FillReferenceArrayContext context, NodeBase? parent)
    {
        context.NewInstructions.Push([]);
        return VisitResult.Continue;
    }

    protected override VisitResult PostVisitBody(BodyNode node, FillReferenceArrayContext context, NodeBase? parent)
    {
        var instructions = context.NewInstructions.Pop();
        if (node.ExpressionList.Count != instructions.Count)
        {
            var newBody = new BodyNode(instructions);
            Replace(node, _ => newBody, context);
        }
        return VisitResult.Continue;
    }
}