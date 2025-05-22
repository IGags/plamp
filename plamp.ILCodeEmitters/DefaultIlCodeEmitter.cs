using System.Reflection;
using System.Reflection.Emit;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Binary;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.Ast.Node.Unary;
using plamp.Abstractions.CompilerEmission;

namespace plamp.ILCodeEmitters;

public class DefaultIlCodeEmitter : IIlCodeEmitter
{
    public Task EmitMethodBodyAsync(CompilerEmissionContext context, CancellationToken cancellationToken = default)
    {
        var argInfoList = context.MethodBuilder.GetParameters();
        var varStack = new LocalVarStack();
        var generator = context.MethodBuilder.GetILGenerator();
        var emissionContext = new EmissionContext(varStack, argInfoList, generator, []);
        varStack.BeginScope();
        EmitExpression(context.MethodBody, emissionContext);
        varStack.EndScope();
        return Task.CompletedTask;
    }

    private void EmitExpression(
        NodeBase expression,
        EmissionContext context)
    {
        switch (expression)
        {
            case BodyNode bodyNode:
                EmitBody(bodyNode, context);
                break;
            case BaseBinaryNode binaryNode:
                EmitBaseBinary(binaryNode, context);
                break;
            case BaseUnaryNode unaryNode:
                EmitUnary(unaryNode, context);
                break;
            case CastNode castNode:
                EmitCast(castNode, context);
                break;
            case ConditionNode conditionNode:
                EmitCondition(conditionNode, context);
                break;
            case WhileNode whileNode:
                EmitWhileLoop(whileNode, context);
                break;
            case BreakNode:
                EmitBreak(context);
                break;
            case ContinueNode:
                EmitContinue(context);
                break;
            case ReturnNode returnNode:
                EmitReturn(returnNode, context);
                break;
            case CallNode callNode:
                EmitCall(callNode, context);
                break;
            case ConstructorCallNode constructorNode:
                EmitCallCtor(constructorNode, context);
                break;
            case VariableDefinitionNode variableDefinitionNode:
                EmitVariableDefinition(variableDefinitionNode, context);
                break;
            case LiteralNode literalNode:
                EmitLiteral(literalNode, context);
                break;
            case MemberAccessNode memberAccessNode:
                EmitMemberAccess(memberAccessNode, context);
                break;
        }
    }

    private void EmitBody(
        BodyNode body, 
        EmissionContext context)
    {
        context.LocalVarStack.BeginScope();
        context.Generator.BeginScope();
        foreach (var instruction in body.InstructionList)
        {
            if(instruction == null) throw new InvalidOperationException("Cannot emit null instruction");
            EmitExpression(instruction, context);
        }
        context.Generator.EndScope();
        context.LocalVarStack.EndScope();
    }
    
    private void EmitReturn(ReturnNode returnNode, EmissionContext context)
    {
        if(returnNode.ReturnValue != null) EmitGetMember(returnNode.ReturnValue, context);
        context.Generator.Emit(OpCodes.Ret);
    }
    
    #region Looping

    private void EmitWhileLoop(WhileNode whileNide, EmissionContext context)
    {
        if(whileNide.Body is not BodyNode body) return;
        var startLabelName = CreateLabel(context);
        context.Generator.MarkLabel(context.Labels[startLabelName]);

        var endLabelName = CreateLabel(context);
        
        EmitExpression(whileNide.Condition, context);
        context.Generator.Emit(OpCodes.Brfalse, context.Labels[endLabelName]);
        
        context.EnterCycleContext(startLabelName, endLabelName);
        EmitBody(body, context);
        context.ExitCycleContext();
        
        context.Generator.Emit(OpCodes.Br, context.Labels[startLabelName]);
        context.Generator.MarkLabel(context.Labels[endLabelName]);
    }

    private void EmitBreak(EmissionContext context)
    {
        var endLabel = context.GetCurrentCycleContext()?.EndLabel;
        if (endLabel == null) return;
        context.Generator.Emit(OpCodes.Br, context.Labels[endLabel]);
    }

    private void EmitContinue(EmissionContext context)
    {
        var startLabel = context.GetCurrentCycleContext()?.StartLabel;
        if (startLabel == null) return;
        context.Generator.Emit(OpCodes.Br, context.Labels[startLabel]);
    }

    #endregion

    #region Conditional

    private void EmitCondition(ConditionNode conditionNode, EmissionContext context)
    {
        var endLab = context.NextLabel();
        EmitClause(conditionNode.IfClause, context, endLab);
        
        foreach (var elifClause in conditionNode.ElifClauseList)
        {
            EmitClause(elifClause, context, endLab);
        }

        if (conditionNode.ElseClause != null)
        {
            EmitBody(conditionNode.ElseClause, context);
        }
    }

    private void EmitClause(ClauseNode node, EmissionContext context, string conditionEndLab)
    {
        EmitExpression(node.Predicate, context);
        var clauseEndLabel = CreateLabel(context);
        context.Generator.Emit(OpCodes.Brfalse, context.Labels[clauseEndLabel]);
        if(node.Body is not BodyNode body) return;
        EmitBody(body, context);
        context.Generator.Emit(OpCodes.Br, context.Labels[conditionEndLab]);
        context.Generator.MarkLabel(context.Labels[clauseEndLabel]);
    }

    #endregion
    
    #region Unary operators

    private void EmitUnary(BaseUnaryNode unaryBase, EmissionContext context)
    {
        switch (unaryBase)
        {
            case PrefixIncrementNode:
                EmitGetMember(unaryBase.Inner, context);
                context.Generator.Emit(OpCodes.Ldc_I4_1);
                context.Generator.Emit(OpCodes.Add_Ovf);
                break;
            case PrefixDecrementNode:
                EmitGetMember(unaryBase.Inner, context);
                context.Generator.Emit(OpCodes.Ldc_I4_1);
                context.Generator.Emit(OpCodes.Sub_Ovf);
                break;
            case PostfixDecrementNode:
                EmitGetMember(unaryBase.Inner, context);
                context.Generator.Emit(OpCodes.Dup);
                context.Generator.Emit(OpCodes.Ldc_I4_1);
                context.Generator.Emit(OpCodes.Sub_Ovf);
                EmitSetMember(unaryBase.Inner, context);
                break;
            case PostfixIncrementNode:
                EmitGetMember(unaryBase.Inner, context);
                context.Generator.Emit(OpCodes.Dup);
                context.Generator.Emit(OpCodes.Ldc_I4_1);
                context.Generator.Emit(OpCodes.Add_Ovf);
                EmitSetMember(unaryBase.Inner, context);
                break;
            case NotNode:
                EmitGetMember(unaryBase.Inner, context);
                context.Generator.Emit(OpCodes.Ldc_I4_0);
                context.Generator.Emit(OpCodes.Ceq);
                break;
            case UnaryMinusNode:
                EmitGetMember(unaryBase.Inner, context);
                context.Generator.Emit(OpCodes.Neg);
                break;
        }
    }

    #endregion
    
    #region Binary operators

    private void EmitBaseBinary(BaseBinaryNode binaryNode, EmissionContext context)
    {
        EmitGetMember(binaryNode.Left, context);
        EmitGetMember(binaryNode.Right, context);
        switch (binaryNode)
        {
            case PlusNode:
                EmitPlus(context);
                break;
            case MinusNode:
                EmitMinus(context);
                break;
            case MultiplyNode:
                EmitMultiply(context);
                break;
            case DivideNode:
                EmitDivide(context);
                break;
            case BitwiseAndNode:
                EmitBitwiseAnd(context);
                break;
            case BitwiseOrNode:
                EmitBitwiseOr(context);
                break;
            case AndNode:
                EmitAndNode(context);
                break;
            case OrNode:
                EmitOrNode(context);
                break;
            case EqualNode:
                EmitEqual(context);
                break;
            case GreaterNode:
                EmitGreater(context);
                break;
            case GreaterOrEqualNode:
                EmitGreaterOrEqual(context);
                break;
            case LessNode:
                EmitLess(context);
                break;
            case LessOrEqualNode:
                EmitLessOrEqual(context);
                break;
            case NotEqualNode:
                EmitNotEqual(context);
                break;
            case ModuloNode:
                EmitModulo(context);
                break;
            case XorNode:
                EmitXor(context);
                break;
            case BaseAssignNode assignNode:
                EmitAssign(assignNode, context);
                break;
        }
    }

    private void EmitAssign(BaseAssignNode assignNode, EmissionContext context)
    {
        EmitExpression(assignNode.Right, context);
        switch (assignNode)
        {
            case AssignNode:
                EmitSetMember(assignNode.Left, context);
                break;
        }
    }
    
    private void EmitGreater(EmissionContext context)
        => context.Generator.Emit(OpCodes.Cgt);

    private void EmitGreaterOrEqual(EmissionContext context)
    {
        context.Generator.Emit(OpCodes.Clt);
        context.Generator.Emit(OpCodes.Ldc_I4_0);
    }

    private void EmitLess(EmissionContext context) 
        => context.Generator.Emit(OpCodes.Clt);

    private void EmitLessOrEqual(EmissionContext context)
    {
        context.Generator.Emit(OpCodes.Cgt);
        context.Generator.Emit(OpCodes.Ldc_I4_0);
        context.Generator.Emit(OpCodes.Ceq);
    }

    private void EmitNotEqual(EmissionContext context)
    {
        context.Generator.Emit(OpCodes.Ceq);
        context.Generator.Emit(OpCodes.Ldc_I4_0);
        context.Generator.Emit(OpCodes.Ceq);
    }

    private void EmitModulo(EmissionContext context) 
        => context.Generator.Emit(OpCodes.Rem);

    private void EmitXor(EmissionContext context)
        => context.Generator.Emit(OpCodes.Xor);

    private void EmitEqual(EmissionContext context)
        => context.Generator.Emit(OpCodes.Ceq);

    private void EmitPlus(EmissionContext context)
        => context.Generator.Emit(OpCodes.Add_Ovf);

    private void EmitMinus(EmissionContext context)
        => context.Generator.Emit(OpCodes.Sub_Ovf);

    private void EmitMultiply(EmissionContext context)
        => context.Generator.Emit(OpCodes.Mul_Ovf);

    private void EmitDivide(EmissionContext context)
        => context.Generator.Emit(OpCodes.Div);

    private void EmitBitwiseAnd(EmissionContext context)
        => context.Generator.Emit(OpCodes.And);

    private void EmitBitwiseOr(EmissionContext context)
        => context.Generator.Emit(OpCodes.Or);

    private void EmitAndNode(EmissionContext context)
        => context.Generator.Emit(OpCodes.Add);

    private void EmitOrNode(EmissionContext context)
        => context.Generator.Emit(OpCodes.Or);

    #endregion

    #region Misc

    private void EmitCast(CastNode node, EmissionContext context)
    {
        EmitExpression(node.Inner, context);
        var typ = GetTypeFromNode(node);
        if (typ == typeof(long)) context.Generator.Emit(OpCodes.Conv_Ovf_I8);
        else if (typ == typeof(ulong)) context.Generator.Emit(OpCodes.Conv_Ovf_U8);
        else if (typ == typeof(int)) context.Generator.Emit(OpCodes.Conv_Ovf_I4);
        else if (typ == typeof(uint)) context.Generator.Emit(OpCodes.Conv_Ovf_U4);
        else if (typ == typeof(short)) context.Generator.Emit(OpCodes.Conv_Ovf_I2);
        else if (typ == typeof(ushort)) context.Generator.Emit(OpCodes.Conv_Ovf_U2);
        else if (typ == typeof(byte)) context.Generator.Emit(OpCodes.Conv_Ovf_U1);
        else if (typ == typeof(double)) context.Generator.Emit(OpCodes.Conv_R8);
        else if (typ == typeof(float)) context.Generator.Emit(OpCodes.Conv_R4);
        else context.Generator.Emit(OpCodes.Castclass, typ!);
    }
    
    private void EmitLiteral(LiteralNode literalNode, EmissionContext context)
    {
        if (literalNode.Type == typeof(string)) context.Generator.Emit(OpCodes.Ldstr, (string)literalNode.Value);
        else if (literalNode.Type == typeof(int)) context.Generator.Emit(OpCodes.Ldc_I4, (int)literalNode.Value);
        else if (literalNode.Type == typeof(uint)) context.Generator.Emit(OpCodes.Ldc_I4, Convert.ToInt32((uint)literalNode.Value));
        else if (literalNode.Type == typeof(long)) context.Generator.Emit(OpCodes.Ldc_I8, (long)literalNode.Value);
        else if (literalNode.Type == typeof(ulong)) context.Generator.Emit(OpCodes.Ldc_I8, Convert.ToInt64((ulong)literalNode.Value));
        else if (literalNode.Type == typeof(short)) context.Generator.Emit(OpCodes.Ldc_I4, (int)literalNode.Value);
        else if (literalNode.Type == typeof(ushort)) context.Generator.Emit(OpCodes.Ldc_I4, (int)literalNode.Value);
        else if (literalNode.Type == typeof(byte)) context.Generator.Emit(OpCodes.Ldc_I4, (int)literalNode.Value);
        else if (literalNode.Type == typeof(float)) context.Generator.Emit(OpCodes.Ldc_R4, (float)literalNode.Value);
        else if (literalNode.Type == typeof(double)) context.Generator.Emit(OpCodes.Ldc_R8, (double)literalNode.Value);
    }

    private void EmitVariableDefinition(
        VariableDefinitionNode variableDefinitionNode,
        EmissionContext context)
    {
        if(variableDefinitionNode.Type is not TypeNode type) return;
        if(variableDefinitionNode.Member is not MemberNode member) return;
        var builder = context.Generator.DeclareLocal(type.Symbol);
        context.LocalVarStack.Add(member.MemberName, builder);
    }

    private void EmitCallCtor(ConstructorCallNode constructorCallNode, EmissionContext context)
    {
        if(constructorCallNode.Symbol == null) return;
        foreach (var arg in constructorCallNode.Args)
        {
            EmitGetMember(arg, context);
        }
        
        context.Generator.Emit(OpCodes.Newobj, constructorCallNode.Symbol);
    }
    
    private void EmitCall(CallNode callNode, EmissionContext context)
    {
        if(callNode.Symbol == null) return;
        context.Generator.Emit(OpCodes.Ldarg_0);
        foreach (var arg in callNode.Args)
        {
            EmitGetMember(arg, context);
        }
        EmitMethodCall(callNode.Symbol, context);
    }

    #endregion

    #region Utility

    private Type? GetTypeFromNode(NodeBase node) => node is not TypeNode typeNode ? null : typeNode.Symbol;

    private void EmitMethodCall(MethodInfo methodInfo, EmissionContext context)
    {
        var opcode = methodInfo.IsVirtual ? OpCodes.Callvirt : OpCodes.Call;
        context.Generator.Emit(opcode, methodInfo);
    }

    private bool TryEmitGetLocalVarOrArg(MemberNode member, EmissionContext context)
    {
        if (context.LocalVarStack.TryGetValue(member.MemberName, out var builder))
        {
            context.Generator.Emit(OpCodes.Ldloc, builder);
            return true;
        }

        ParameterInfo? arg;
        if ((arg = context.Arguments.FirstOrDefault()) == null) return false;
        
        var ix = Array.IndexOf(context.Arguments, arg);
        context.Generator.Emit(OpCodes.Ldarg_S, ix + 1);
        return true;
    }

    private bool TryEmitSetLocalVarOrArg(MemberNode member, EmissionContext context)
    {
        if (context.LocalVarStack.TryGetValue(member.MemberName, out var builder))
        {
            context.Generator.Emit(OpCodes.Stloc, builder);
            return true;
        }

        ParameterInfo? arg;
        if ((arg = context.Arguments.FirstOrDefault()) == null) return false;
        
        var ix = Array.IndexOf(context.Arguments, arg);
        context.Generator.Emit(OpCodes.Starg_S, ix + 1);
        return true;
    }

    private void EmitGetMember(NodeBase node, EmissionContext context)
    {
        if (node is LiteralNode literalNode)
        {
            EmitLiteral(literalNode, context);
            return;
        }
        
        if(node is not MemberNode memberNode) return;
        if (TryEmitGetLocalVarOrArg(memberNode, context)) return;
        if (memberNode.Symbol == null) return;
        switch (memberNode.Symbol)
        {
            case PropertyInfo pi:
                var getter = pi.GetGetMethod();
                if(getter == null) return;
                EmitMethodCall(getter, context);
                break;
            case FieldInfo fi:
                context.Generator.Emit(OpCodes.Ldfld, fi);
                break;
        }
    }

    private void EmitSetMember(NodeBase node, EmissionContext context)
    {
        if(node is not MemberNode memberNode) return;
        if (TryEmitSetLocalVarOrArg(memberNode, context)) return;
        if (memberNode.Symbol == null) return;
        switch (memberNode.Symbol)
        {
            case PropertyInfo pi:
                var setter = pi.GetSetMethod();
                if(setter == null) return;
                EmitMethodCall(setter, context);
                break;
            case FieldInfo fi:
                context.Generator.Emit(OpCodes.Stfld, fi);
                break;
        }
    }
    
    private void EmitMemberAccess(
        MemberAccessNode memberAccessNode, 
        EmissionContext context)
    {
        if(memberAccessNode.From is not MemberNode memberFrom) return;
        if(memberAccessNode.Member is not MemberNode member) return;
        EmitGetMember(memberFrom, context);
        EmitGetMember(member, context);
    }

    #endregion
    
    //TODO: жидко
    private string CreateLabel(EmissionContext context)
    {
        var label = context.Generator.DefineLabel();
        var name = Guid.NewGuid().ToString();
        context.Labels[name] = label;
        return name;
    }
}