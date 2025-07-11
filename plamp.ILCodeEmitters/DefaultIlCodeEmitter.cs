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
        var varStack = new LocalVarStack();
        var generator = context.MethodBuilder.GetILGenerator();
        var emissionContext = new EmissionContext(varStack, context.Parameters, generator, [], context.MethodBuilder);
        EmitExpression(context.MethodBody, emissionContext);
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
            case AssignNode assignNode:
                EmitAssign(assignNode, context);
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
            case VariableDefinitionNode variableDefinitionNode:
                EmitVariableDefinition(variableDefinitionNode, context);
                break;
        }
    }

    private void EmitBody(
        BodyNode body, 
        EmissionContext context)
    {
        context.LocalVarStack.BeginScope();
        context.Generator.BeginScope();
        foreach (var instruction in body.ExpressionList)
        {
            if(instruction == null) continue;
            EmitExpression(instruction, context);
        }
        context.Generator.EndScope();
        context.LocalVarStack.EndScope();
    }
    
    private void EmitReturn(ReturnNode returnNode, EmissionContext context)
    {
        //For struct cases. Struct must be returned by value
        if(returnNode.ReturnValue is MemberNode) EmitGetMember(returnNode.ReturnValue, context, true);
        else if(returnNode.ReturnValue != null) EmitSingleLineExpression(returnNode.ReturnValue, context);
        context.Generator.Emit(OpCodes.Ret);
    }
    
    #region Looping

    private void EmitWhileLoop(WhileNode whileNide, EmissionContext context)
    {
        if(whileNide.Body is not BodyNode body) return;
        var startLabelName = CreateLabel(context);
        context.Generator.MarkLabel(context.Labels[startLabelName]);

        var endLabelName = CreateLabel(context);
        
        EmitSingleLineExpression(whileNide.Condition, context);
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
        if(conditionNode.IfClause is not BodyNode ifBody) return;
        if(conditionNode.ElseClause is not null and not BodyNode) return;
        
        string? elseClauseEndLab;
        if (ifBody.ExpressionList.Any(x => x.GetType() == typeof(ReturnNode))
            && conditionNode.ElseClause != null
            && ((BodyNode)conditionNode.ElseClause).ExpressionList.Any(x => x.GetType() == typeof(ReturnNode)))
        {
            elseClauseEndLab = null;
        }
        else
        {
            elseClauseEndLab = CreateLabel(context);
        }
        
        EmitSingleLineExpression(conditionNode.Predicate, context);
        var ifClauseEndLab = CreateLabel(context);
        context.Generator.Emit(OpCodes.Brfalse, context.Labels[ifClauseEndLab]);
        EmitBody(ifBody, context);
        
        if (conditionNode.ElseClause is null)
        {
            context.Generator.MarkLabel(context.Labels[ifClauseEndLab]);
            return;
        }

        if (elseClauseEndLab != null)
        {
            context.Generator.Emit(OpCodes.Br, context.Labels[elseClauseEndLab]);
        }
        
        context.Generator.MarkLabel(context.Labels[ifClauseEndLab]);
        EmitBody((BodyNode)conditionNode.ElseClause, context);

        if (elseClauseEndLab != null)
        {
            context.Generator.MarkLabel(context.Labels[elseClauseEndLab]);
        }
    }

    #endregion
    
    #region Unary operators

    private void EmitUnary(BaseUnaryNode unaryBase, EmissionContext context)
    {
        switch (unaryBase)
        {
            case NotNode:
                EmitSingleLineExpression(unaryBase.Inner, context);
                context.Generator.Emit(OpCodes.Ldc_I4_0);
                context.Generator.Emit(OpCodes.Ceq);
                break;
            case UnaryMinusNode:
                EmitSingleLineExpression(unaryBase.Inner, context);
                context.Generator.Emit(OpCodes.Neg);
                break;
        }
    }

    #endregion
    
    #region Binary operators

    private void EmitBaseBinary(BaseBinaryNode binaryNode, EmissionContext context)
    {
        EmitSingleLineExpression(binaryNode.Left, context);
        EmitSingleLineExpression(binaryNode.Right, context);
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
        }
    }

    private void EmitAssign(AssignNode assignNode, EmissionContext context)
    {
        FieldInfo? emitFld = null;
        switch (assignNode.Left)
        {
            case MemberAccessNode accessNode:
                emitFld = EmitAccessField(accessNode, context);
                break;
            case VariableDefinitionNode varDef:
                EmitVariableDefinition(varDef, context);
                break;
        }

        EmitSingleLineExpression(assignNode.Right, context);

        if (emitFld is not null)
        {
            context.Generator.Emit(OpCodes.Stfld, emitFld);
        }
        else if (assignNode.Left is VariableDefinitionNode {Member: MemberNode varMember})
        {
            EmitSetLocalVarOrArg(varMember, context);
        }
        else if(assignNode.Left is MemberNode memberNode)
        {
            EmitSetLocalVarOrArg(memberNode, context);
        }
    }

    private void EmitSingleLineExpression(NodeBase source, EmissionContext context)
    {
        switch (source)
        {
            case MemberNode memberNode: 
                EmitGetLocalVarOrArg(memberNode, context, false);
                break;
            case BaseBinaryNode binaryNode:
                EmitBaseBinary(binaryNode, context);
                break;
            case BaseUnaryNode unaryNode:
                EmitUnary(unaryNode, context);
                break;
            case CallNode callNode:
                EmitCall(callNode, context);
                break;
            case ConstructorCallNode constructorCallNode:
                EmitCallCtor(constructorCallNode, context);
                break;
            case CastNode castNode:
                EmitTypeConversion(castNode, context);
                break;
            case LiteralNode literalNode:
                EmitLiteral(literalNode, context);
                break;
            case MemberAccessNode memberAccessNode:
                EmitGetField(memberAccessNode, context);
                break;
        }
    }

    private void EmitGetField(MemberAccessNode accessNode, EmissionContext context)
    {
        if (accessNode.From is not MemberNode from
            || accessNode.Member is not MemberNode memberNode)
        {
            return;
        }
        
        if (memberNode.Symbol == null) return;
        
        EmitGetMember(from, context, true);
        switch (memberNode.Symbol)
        {
            case FieldInfo fi:
                context.Generator.Emit(OpCodes.Ldfld, fi);
                break;
        }
    }

    private FieldInfo? EmitAccessField(MemberAccessNode accessNode, EmissionContext context)
    {
        if (accessNode.From is not MemberNode from
            || accessNode.Member is not MemberNode memberNode)
        {
            return null;
        }
        
        if (memberNode.Symbol == null) return null;
        
        EmitGetMember(from, context, false);
        return memberNode.Symbol as FieldInfo;
    }
    
    private void EmitGreater(EmissionContext context)
        => context.Generator.Emit(OpCodes.Cgt);

    private void EmitGreaterOrEqual(EmissionContext context)
    {
        context.Generator.Emit(OpCodes.Clt);
        context.Generator.Emit(OpCodes.Ldc_I4_0);
        context.Generator.Emit(OpCodes.Ceq);
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
        => context.Generator.Emit(OpCodes.And);

    private void EmitOrNode(EmissionContext context)
        => context.Generator.Emit(OpCodes.Or);

    #endregion

    #region Misc

    private void EmitTypeConversion(CastNode node, EmissionContext context)
    {
        EmitGetLocalVarOrArg((MemberNode)node.Inner, context, false);
        var toType = GetTypeFromNode(node.ToType)!;
        var fromType = node.FromType;
        
        if(!fromType.IsValueType && !toType.IsValueType) EmitCast(toType, context);
        else if(fromType.IsValueType && !toType.IsValueType) EmitBox(fromType, context);
        else if(!fromType.IsValueType && toType.IsValueType) EmitUnbox(toType, context);
        //We can convert i4 -> i8 or any other
        else EmitNumberTypeConversion(toType, context);
        //A struct cannot be converted to a struct
    }

    private void EmitCast(Type targetType, EmissionContext context) 
        => context.Generator.Emit(OpCodes.Castclass, targetType);

    private void EmitBox(Type sourceType, EmissionContext context) 
        => context.Generator.Emit(OpCodes.Box, sourceType);

    private void EmitNumberTypeConversion(Type targetType, EmissionContext context)
    {
        if (targetType == typeof(long)) context.Generator.Emit(OpCodes.Conv_I8);
        else if (targetType == typeof(ulong)) context.Generator.Emit(OpCodes.Conv_U8);
        else if (targetType == typeof(int)) context.Generator.Emit(OpCodes.Conv_Ovf_I4);
        else if (targetType == typeof(uint)) context.Generator.Emit(OpCodes.Conv_Ovf_U4);
        else if (targetType == typeof(short)) context.Generator.Emit(OpCodes.Conv_Ovf_I2);
        else if (targetType == typeof(ushort) || targetType == typeof(char)) context.Generator.Emit(OpCodes.Conv_Ovf_U2);
        else if (targetType == typeof(byte)) context.Generator.Emit(OpCodes.Conv_Ovf_U1);
        else if (targetType == typeof(double)) context.Generator.Emit(OpCodes.Conv_R8);
        else if (targetType == typeof(float)) context.Generator.Emit(OpCodes.Conv_R4);
    }
    
    private void EmitUnbox(Type targetType, EmissionContext context) 
        => context.Generator.Emit(OpCodes.Unbox_Any, targetType);

    private void EmitLiteral(LiteralNode literalNode, EmissionContext context)
    {
        if (literalNode.Type == typeof(string))
        {
            if (literalNode.Value == null) context.Generator.Emit(OpCodes.Ldnull);
            else context.Generator.Emit(OpCodes.Ldstr, (string)literalNode.Value);
        }
        else if (literalNode.Type == typeof(int)) context.Generator.Emit(OpCodes.Ldc_I4, (int)literalNode.Value);
        else if (literalNode.Type == typeof(uint)) context.Generator.Emit(OpCodes.Ldc_I4, BitConverter.ToInt32(BitConverter.GetBytes((uint)literalNode.Value)));
        else if (literalNode.Type == typeof(long)) context.Generator.Emit(OpCodes.Ldc_I8, (long)literalNode.Value);
        else if (literalNode.Type == typeof(ulong)) context.Generator.Emit(OpCodes.Ldc_I8, BitConverter.ToInt64(BitConverter.GetBytes((ulong)literalNode.Value)));
        else if (literalNode.Type == typeof(short)) context.Generator.Emit(OpCodes.Ldc_I4, (int)(short)literalNode.Value);
        else if (literalNode.Type == typeof(ushort)) context.Generator.Emit(OpCodes.Ldc_I4, (ushort)literalNode.Value);
        else if (literalNode.Type == typeof(byte)) context.Generator.Emit(OpCodes.Ldc_I4, (int)(byte)literalNode.Value);
        else if (literalNode.Type == typeof(float)) context.Generator.Emit(OpCodes.Ldc_R4, (float)literalNode.Value);
        else if (literalNode.Type == typeof(double)) context.Generator.Emit(OpCodes.Ldc_R8, (double)literalNode.Value);
        else if (literalNode.Type == typeof(bool)) context.Generator.Emit((bool)literalNode.Value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
        else if (literalNode.Type == typeof(char)) context.Generator.Emit(OpCodes.Ldc_I4, Convert.ToUInt32(literalNode.Value));
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
            EmitGetMember(arg, context, true);
        }
        
        context.Generator.Emit(OpCodes.Newobj, constructorCallNode.Symbol);
    }
    
    private void EmitCall(CallNode callNode, EmissionContext context)
    {
        if(callNode.Symbol == null) return;
        
        switch (callNode.From)
        {
            case MemberNode:
                EmitGetMember(callNode.From, context, false);
                break;
            case ThisNode:
                context.Generator.Emit(OpCodes.Ldarg_0);
                break;
        }
        
        foreach (var arg in callNode.Args)
        {
            if(arg is MemberNode) EmitGetMember(arg, context, true);
            else EmitSingleLineExpression(arg, context);
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

    private void EmitGetLocalVarOrArg(MemberNode member, EmissionContext context, bool byValue)
    {
        if (context.LocalVarStack.TryGetValue(member.MemberName, out var builder))
        {
            context.Generator.Emit(OpCodes.Ldloc, builder);
            return;
        }

        ParameterInfo? arg;
        if ((arg = context.Arguments.FirstOrDefault(x => x.Name == member.MemberName)) == null)
        {
            return;
        }
        
        var ix = Array.IndexOf(context.Arguments, arg);
        
        var opcode = arg.ParameterType is { IsValueType: true, IsPrimitive: false } && !byValue
            ? OpCodes.Ldarga_S
            : OpCodes.Ldarg_S;
        
        ix = context.CurrentMethod.IsStatic ? ix : ix + 1;
        context.Generator.Emit(opcode, ix);
    }

    private void EmitSetLocalVarOrArg(MemberNode member, EmissionContext context)
    {
        if (context.LocalVarStack.TryGetValue(member.MemberName, out var builder))
        {
            context.Generator.Emit(OpCodes.Stloc, builder);
            return;
        }

        ParameterInfo? arg;
        if ((arg = context.Arguments.FirstOrDefault()) == null) return;

        var ix = Array.IndexOf(context.Arguments, arg);
        context.Generator.Emit(OpCodes.Starg_S, ix + 1);
    }

    private void EmitGetMember(NodeBase node, EmissionContext context, bool byValue)
    {
        if(node is not MemberNode memberNode) return;
        EmitGetLocalVarOrArg(memberNode, context, byValue);
    }

    #endregion
    
    //TODO: жидко дублирование хранения лейблов
    private string CreateLabel(EmissionContext context)
    {
        var label = context.Generator.DefineLabel();
        var name = Guid.NewGuid().ToString();
        context.Labels[name] = label;
        return name;
    }
}