using System.Reflection;
using System.Reflection.Emit;
using plamp.Abstractions.Assemblies;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Binary;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.Ast.Node.Unary;
using plamp.Abstractions.Compilation.Models.ApiGeneration;
using plamp.Abstractions.CompilerEmission;

namespace plamp.ILCodeEmitters;

public class DefaultIlCodeEmitter : IIlCodeEmitter
{
    public Task<GeneratorPair> EmitMethodBodyAsync(
        MethodEmitterPair currentEmitterPair, 
        ICompiledAssemblyContainer compiledAssemblyContainer,
        ISymbolTable symbolTable,
        CancellationToken cancellationToken = default)
    {
        var varStack = new LocalVarStack();
        var context = new EmissionContext(varStack, currentEmitterPair.MethodDefinition.Args);
        varStack.BeginScope();
        var actor = new SequentialActor();
        EmitExpression(actor, currentEmitterPair.MethodDefinition.Body, context);
        varStack.EndScope();
        
        var generator = currentEmitterPair.MethodBuilder.GetILGenerator();
        return Task.FromResult(new GeneratorPair(generator, actor));
    }

    private void EmitExpression(
        SequentialActor actor,
        NodeBase expression,
        EmissionContext context)
    {
        switch (expression)
        {
            case BodyNode bodyNode:
                EmitBody(actor, bodyNode, context);
                break;
            case BaseBinaryNode binaryNode:
                EmitBaseBinary(actor, binaryNode, context);
                break;
            case BaseUnaryNode unaryNode:
                EmitUnary(actor, unaryNode, context);
                break;
            case CastNode castNode:
                EmitCast(actor, castNode, context);
                break;
            case ConditionNode conditionNode:
                EmitCondition(actor, conditionNode, context);
                break;
            case WhileNode whileNode:
                EmitWhileLoop(actor, whileNode, context);
                break;
            case BreakNode:
                EmitBreak(actor, context);
                break;
            case ContinueNode:
                EmitContinue(actor, context);
                break;
            case ReturnNode returnNode:
                EmitReturn(actor, returnNode, context);
                break;
            case CallNode callNode:
                EmitCall(actor, callNode, context);
                break;
            case ConstructorCallNode constructorNode:
                EmitCallCtor(actor, constructorNode, context);
                break;
            case VariableDefinitionNode variableDefinitionNode:
                EmitVariableDefinition(actor, variableDefinitionNode, context);
                break;
            case LiteralNode literalNode:
                EmitLiteral(actor, literalNode);
                break;
            case MemberAccessNode memberAccessNode:
                EmitMemberAccess(actor, memberAccessNode, context);
                break;
        }
    }

    private void EmitBody(
        SequentialActor actor,
        BodyNode body, 
        EmissionContext context)
    {
        context.LocalVarStack.BeginScope();
        actor.Add(static g => g.BeginScope());
        foreach (var instruction in body.InstructionList)
        {
            if(instruction == null) throw new InvalidOperationException("Cannot emit null instruction");
            EmitExpression(actor, instruction, context);
        }
        actor.Add(static g => g.EndScope());
        context.LocalVarStack.EndScope();
    }
    
    private void EmitReturn(SequentialActor actor, ReturnNode returnNode, EmissionContext context)
    {
        if(returnNode.ReturnValue != null) EmitGetMember(actor, returnNode.ReturnValue, context);
        actor.Add(static g => g.Emit(OpCodes.Ret));
    }
    
    #region Looping

    private void EmitWhileLoop(SequentialActor actor, WhileNode whileNide, EmissionContext context)
    {
        if(whileNide.Body is not BodyNode body) return;
        var startLabel = context.NextLabel();
        actor.DefineLabel(startLabel);
        actor.Add(g => g.MarkLabel(actor.Labels[startLabel]));

        var endLabel = context.NextLabel();
        actor.DefineLabel(endLabel);
        
        EmitExpression(actor, whileNide.Condition, context);
        actor.Add(g => g.Emit(OpCodes.Brfalse, actor.Labels[endLabel]));
        
        context.EnterCycleContext(startLabel, endLabel);
        EmitBody(actor, body, context);
        context.ExitCycleContext();
        
        actor.Add(g => g.Emit(OpCodes.Br, actor.Labels[startLabel]));
        actor.Add(g => g.MarkLabel(actor.Labels[endLabel]));
    }

    private void EmitBreak(SequentialActor actor, EmissionContext context)
    {
        var endLabel = context.GetCurrentCycleContext()?.EndLabel;
        if (endLabel == null) return;
        actor.Add(g => g.Emit(OpCodes.Br, actor.Labels[endLabel]));
    }

    private void EmitContinue(SequentialActor actor, EmissionContext context)
    {
        var startLabel = context.GetCurrentCycleContext()?.StartLabel;
        if (startLabel == null) return;
        actor.Add(g => g.Emit(OpCodes.Br, actor.Labels[startLabel]));
    }

    #endregion

    #region Conditional

    private void EmitCondition(SequentialActor actor, ConditionNode conditionNode, EmissionContext context)
    {
        var endLab = context.NextLabel();
        EmitClause(actor, conditionNode.IfClause, context, endLab);
        
        foreach (var elifClause in conditionNode.ElifClauseList)
        {
            EmitClause(actor, elifClause, context, endLab);
        }

        if (conditionNode.ElseClause != null)
        {
            EmitBody(actor, conditionNode.ElseClause, context);
        }
    }

    private void EmitClause(SequentialActor actor, ClauseNode node, EmissionContext context, string endLab)
    {
        EmitExpression(actor, node.Predicate, context);
        var currentClauseEnd = context.NextLabel();
        actor.DefineLabel(currentClauseEnd);
        actor.Add(g => g.Emit(OpCodes.Brfalse, actor.Labels[currentClauseEnd]));
        if(node.Body is not BodyNode body) return;
        EmitBody(actor, body, context);
        actor.Add(g => g.Emit(OpCodes.Br, actor.Labels[endLab]));
        actor.Add(g => g.MarkLabel(actor.Labels[currentClauseEnd]));
    }

    #endregion
    
    #region Unary operators

    private void EmitUnary(SequentialActor actor, BaseUnaryNode unaryBase, EmissionContext context)
    {
        switch (unaryBase)
        {
            case PrefixIncrementNode:
                EmitGetMember(actor, unaryBase.Inner, context);
                actor.Add(static g => g.Emit(OpCodes.Ldc_I4_1));
                actor.Add(static g => g.Emit(OpCodes.Add_Ovf));
                break;
            case PrefixDecrementNode:
                EmitGetMember(actor, unaryBase.Inner, context);
                actor.Add(static g => g.Emit(OpCodes.Ldc_I4_1));
                actor.Add(static g => g.Emit(OpCodes.Sub_Ovf));
                break;
            case PostfixDecrementNode:
                EmitGetMember(actor, unaryBase.Inner, context);
                actor.Add(static g => g.Emit(OpCodes.Dup));
                actor.Add(static g => g.Emit(OpCodes.Ldc_I4_1));
                actor.Add(static g => g.Emit(OpCodes.Sub_Ovf));
                EmitSetMember(actor, unaryBase.Inner, context);
                break;
            case PostfixIncrementNode:
                EmitGetMember(actor, unaryBase.Inner, context);
                actor.Add(static g => g.Emit(OpCodes.Dup));
                actor.Add(static g => g.Emit(OpCodes.Ldc_I4_1));
                actor.Add(static g => g.Emit(OpCodes.Add_Ovf));
                EmitSetMember(actor, unaryBase.Inner, context);
                break;
            case NotNode:
                EmitGetMember(actor, unaryBase.Inner, context);
                actor.Add(static g => g.Emit(OpCodes.Ldc_I4_0));
                actor.Add(static g => g.Emit(OpCodes.Ceq));
                break;
            case UnaryMinusNode:
                EmitGetMember(actor, unaryBase.Inner, context);
                actor.Add(static g => g.Emit(OpCodes.Neg));
                break;
        }
    }

    #endregion
    
    #region Binary operators

    private void EmitBaseBinary(SequentialActor actor, BaseBinaryNode binaryNode, EmissionContext context)
    {
        EmitGetMember(actor, binaryNode.Left, context);
        EmitGetMember(actor, binaryNode.Right, context);
        switch (binaryNode)
        {
            case PlusNode:
                EmitPlus(actor);
                break;
            case MinusNode:
                EmitMinus(actor);
                break;
            case MultiplyNode:
                EmitMultiply(actor);
                break;
            case DivideNode:
                EmitDivide(actor);
                break;
            case BitwiseAndNode:
                EmitBitwiseAnd(actor);
                break;
            case BitwiseOrNode:
                EmitBitwiseOr(actor);
                break;
            case AndNode:
                EmitAndNode(actor);
                break;
            case OrNode:
                EmitOrNode(actor);
                break;
            case EqualNode:
                EmitEqual(actor);
                break;
            case GreaterNode:
                EmitGreater(actor);
                break;
            case GreaterOrEqualNode:
                EmitGreaterOrEqual(actor);
                break;
            case LessNode:
                EmitLess(actor);
                break;
            case LessOrEqualNode:
                EmitLessOrEqual(actor);
                break;
            case NotEqualNode:
                EmitNotEqual(actor);
                break;
            case ModuloNode:
                EmitModulo(actor);
                break;
            case XorNode:
                EmitXor(actor);
                break;
            case BaseAssignNode assignNode:
                EmitAssign(actor, assignNode, context);
                break;
        }
    }

    private void EmitAssign(SequentialActor actor, BaseAssignNode assignNode, EmissionContext context)
    {
        EmitExpression(actor, assignNode.Right, context);
        switch (assignNode)
        {
            case AssignNode:
                EmitSetMember(actor, assignNode.Left, context);
                break;
        }
    }
    
    private void EmitGreater(SequentialActor actor)
        => actor.Add(static g => g.Emit(OpCodes.Cgt));

    private void EmitGreaterOrEqual(SequentialActor actor)
    {
        actor.Add(static g => g.Emit(OpCodes.Clt));
        actor.Add(static g => g.Emit(OpCodes.Ldc_I4_0));
    }

    private void EmitLess(SequentialActor actor) 
        => actor.Add(static g => g.Emit(OpCodes.Clt));

    private void EmitLessOrEqual(SequentialActor actor)
    {
        actor.Add(static g => g.Emit(OpCodes.Cgt));
        actor.Add(static g => g.Emit(OpCodes.Ldc_I4_0));
        actor.Add(static g => g.Emit(OpCodes.Ceq));
    }

    private void EmitNotEqual(SequentialActor actor)
    {
        actor.Add(static g => g.Emit(OpCodes.Ceq));
        actor.Add(static g => g.Emit(OpCodes.Ldc_I4_0));
        actor.Add(static g => g.Emit(OpCodes.Ceq));
    }

    private void EmitModulo(SequentialActor actor) 
        => actor.Add(static g => g.Emit(OpCodes.Rem));

    private void EmitXor(SequentialActor actor)
        => actor.Add(static g => g.Emit(OpCodes.Xor));

    private void EmitEqual(SequentialActor actor)
        => actor.Add(static g => g.Emit(OpCodes.Ceq));

    private void EmitPlus(SequentialActor actor)
        => actor.Add(static g => g.Emit(OpCodes.Add_Ovf));

    private void EmitMinus(SequentialActor actor)
        => actor.Add(static g => g.Emit(OpCodes.Sub_Ovf));

    private void EmitMultiply(SequentialActor actor)
        => actor.Add(static g => g.Emit(OpCodes.Mul_Ovf));

    private void EmitDivide(SequentialActor actor)
        => actor.Add(static g => g.Emit(OpCodes.Div));

    private void EmitBitwiseAnd(SequentialActor actor)
        => actor.Add(static g => g.Emit(OpCodes.And));

    private void EmitBitwiseOr(SequentialActor actor)
        => actor.Add(static g => g.Emit(OpCodes.Or));

    private void EmitAndNode(SequentialActor actor)
        => actor.Add(static g => g.Emit(OpCodes.Add));

    private void EmitOrNode(SequentialActor actor)
        => actor.Add(static g => g.Emit(OpCodes.Or));

    #endregion

    #region Misc

    private void EmitCast(SequentialActor actor, CastNode node, EmissionContext context)
    {
        EmitExpression(actor, node.Inner, context);
        var typ = GetTypeFromNode(node);
        if (typ == typeof(long)) actor.Add(static g => g.Emit(OpCodes.Conv_Ovf_I8));
        else if (typ == typeof(ulong)) actor.Add(static g => g.Emit(OpCodes.Conv_Ovf_U8));
        else if (typ == typeof(int)) actor.Add(static g => g.Emit(OpCodes.Conv_Ovf_I4));
        else if (typ == typeof(uint)) actor.Add(static g => g.Emit(OpCodes.Conv_Ovf_U4));
        else if (typ == typeof(short)) actor.Add(static g => g.Emit(OpCodes.Conv_Ovf_I2));
        else if (typ == typeof(ushort)) actor.Add(static g => g.Emit(OpCodes.Conv_Ovf_U2));
        else if (typ == typeof(byte)) actor.Add(static g => g.Emit(OpCodes.Conv_Ovf_U1));
        else if (typ == typeof(double)) actor.Add(static g => g.Emit(OpCodes.Conv_R8));
        else if (typ == typeof(float)) actor.Add(static g => g.Emit(OpCodes.Conv_R4));
        else actor.Add(g => g.Emit(OpCodes.Castclass, typ!));
    }
    
    private void EmitLiteral(
        SequentialActor actor,
        LiteralNode literalNode)
    {
        if (literalNode.Type == typeof(string)) actor.Add(g => g.Emit(OpCodes.Ldstr, (string)literalNode.Value));
        else if (literalNode.Type == typeof(int)) actor.Add(g => g.Emit(OpCodes.Ldc_I4, (int)literalNode.Value));
        else if (literalNode.Type == typeof(uint)) actor.Add(g => g.Emit(OpCodes.Ldc_I4, Convert.ToInt32((uint)literalNode.Value)));
        else if (literalNode.Type == typeof(long)) actor.Add(g => g.Emit(OpCodes.Ldc_I8, (long)literalNode.Value));
        else if (literalNode.Type == typeof(ulong)) actor.Add(g => g.Emit(OpCodes.Ldc_I8, Convert.ToInt64((ulong)literalNode.Value)));
        else if (literalNode.Type == typeof(short)) actor.Add(g => g.Emit(OpCodes.Ldc_I4, (int)literalNode.Value));
        else if (literalNode.Type == typeof(ushort)) actor.Add(g => g.Emit(OpCodes.Ldc_I4, (int)literalNode.Value));
        else if (literalNode.Type == typeof(byte)) actor.Add(g => g.Emit(OpCodes.Ldc_I4, (int)literalNode.Value));
        else if (literalNode.Type == typeof(float)) actor.Add(g => g.Emit(OpCodes.Ldc_R4, (float)literalNode.Value));
        else if (literalNode.Type == typeof(double)) actor.Add(g => g.Emit(OpCodes.Ldc_R8, (double)literalNode.Value));
    }

    private void EmitVariableDefinition(
        SequentialActor actor, 
        VariableDefinitionNode variableDefinitionNode,
        EmissionContext context)
    {
        if(variableDefinitionNode.Type is not TypeNode type) return;
        if(variableDefinitionNode.Member is not MemberNode member) return;
        context.LocalVarStack.Add(member.MemberName, type.Symbol);
        actor.DeclareLocal(member.MemberName, type.Symbol);
    }

    private void EmitCallCtor(SequentialActor actor, ConstructorCallNode constructorCallNode, EmissionContext context)
    {
        if(constructorCallNode.Symbol == null) return;
        foreach (var arg in constructorCallNode.Args)
        {
            EmitGetMember(actor, arg, context);
        }
        
        actor.Add(g => g.Emit(OpCodes.Newobj, constructorCallNode.Symbol));
    }
    
    private void EmitCall(SequentialActor actor, CallNode callNode, EmissionContext context)
    {
        if(callNode.Symbol == null) return;
        actor.Add(g => g.Emit(OpCodes.Ldarg_0));
        foreach (var arg in callNode.Args)
        {
            EmitGetMember(actor, arg, context);
        }
        EmitMethodCall(actor, callNode.Symbol);
    }

    #endregion

    #region Utility

    private Type? GetTypeFromNode(NodeBase node) => node is not TypeNode typeNode ? null : typeNode.Symbol;

    private void EmitMethodCall(SequentialActor actor, MethodInfo methodInfo)
    {
        if(methodInfo.IsVirtual) actor.Add(g => g.Emit(OpCodes.Callvirt, methodInfo));
        else actor.Add(g => g.Emit(OpCodes.Call, methodInfo));
    }

    private bool TryEmitGetLocalVarOrArg(SequentialActor actor, MemberNode member, EmissionContext context)
    {
        if (context.LocalVarStack.TryGetValue(member.MemberName, out _))
        {
            actor.Add(g => g.Emit(OpCodes.Ldloc, actor.Locals[member.MemberName]));
            return true;
        }

        ArgDefinition arg;
        if ((arg = context.Arguments.FirstOrDefault()) == default) return false;
        
        var ix = context.Arguments.IndexOf(arg);
        actor.Add(g => g.Emit(OpCodes.Ldarg_S, ix + 1));
        return true;
    }

    private bool TryEmitSetLocalVarOrArg(SequentialActor actor, MemberNode member, EmissionContext context)
    {
        if (context.LocalVarStack.TryGetValue(member.MemberName, out _))
        {
            actor.Add(g => g.Emit(OpCodes.Stloc, actor.Locals[member.MemberName]));
            return true;
        }

        ArgDefinition arg;
        if ((arg = context.Arguments.FirstOrDefault()) == default) return false;
        
        var ix = context.Arguments.IndexOf(arg);
        actor.Add(g => g.Emit(OpCodes.Starg_S, ix + 1));
        return true;
    }

    private void EmitGetMember(SequentialActor actor, NodeBase node, EmissionContext context)
    {
        if (node is LiteralNode literalNode)
        {
            EmitLiteral(actor, literalNode);
            return;
        }
        
        if(node is not MemberNode memberNode) return;
        if (TryEmitGetLocalVarOrArg(actor, memberNode, context)) return;
        if (memberNode.Symbol == null) return;
        switch (memberNode.Symbol)
        {
            case PropertyInfo pi:
                var getter = pi.GetGetMethod();
                if(getter == null) return;
                EmitMethodCall(actor, getter);
                break;
            case FieldInfo fi:
                actor.Add(g => g.Emit(OpCodes.Ldfld, fi));
                break;
        }
    }

    private void EmitSetMember(SequentialActor actor, NodeBase node, EmissionContext context)
    {
        if(node is not MemberNode memberNode) return;
        if (TryEmitSetLocalVarOrArg(actor, memberNode, context)) return;
        if (memberNode.Symbol == null) return;
        switch (memberNode.Symbol)
        {
            case PropertyInfo pi:
                var setter = pi.GetSetMethod();
                if(setter == null) return;
                EmitMethodCall(actor, setter);
                break;
            case FieldInfo fi:
                actor.Add(g => g.Emit(OpCodes.Stfld, fi));
                break;
        }
    }
    
    private void EmitMemberAccess(
        SequentialActor actor, 
        MemberAccessNode memberAccessNode, 
        EmissionContext context)
    {
        if(memberAccessNode.From is not MemberNode memberFrom) return;
        if(memberAccessNode.Member is not MemberNode member) return;
        EmitGetMember(actor, memberFrom, context);
        EmitGetMember(actor, member, context);
    }

    #endregion
}