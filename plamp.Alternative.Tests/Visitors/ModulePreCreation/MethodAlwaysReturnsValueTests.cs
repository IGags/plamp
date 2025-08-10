using System;
using System.Collections.Generic;
using System.Linq;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Alternative.Visitors.ModulePreCreation;
using plamp.Alternative.Visitors.ModulePreCreation.MustReturn;
using Xunit;

namespace plamp.Alternative.Tests.Visitors.ModulePreCreation;

public class MethodAlwaysReturnsValueTests
{
    [Theory]
    [MemberData(nameof(AlwaysReturnsValidatorDataProvider))]
    public void VoidMethod(BodyNode body, string methodName, Type returnType, bool shouldExcept)
    {
        var defNode = CreateMethod(returnType, methodName, [], body);
        var validator = new MethodMustReturnValueValidator();
        var context = new PreCreationContext("aaa", new MockSymbolTable());
        
        var res = validator.Validate(defNode, context);
        
        if (shouldExcept)
        {
            Assert.Single(res.Exceptions);
            Assert.NotEqual(null, res.Exceptions[0]);
        }
        else
        {
            Assert.Empty(res.Exceptions);
        }
    }

    public static IEnumerable<object[]> AlwaysReturnsValidatorDataProvider()
    {
        var returnNode = new ReturnNode(new LiteralNode(1, typeof(int))); 
        yield return [new BodyNode([]), "VoidMethod", typeof(void), false];
        yield return [new BodyNode([returnNode]), "SimpleReturn", typeof(int), false];
        yield return [new BodyNode([returnNode, returnNode]), "ReturnTwice", typeof(int), false];
        yield return [new BodyNode([]), "SimpleDoesNotReturn", typeof(int), true];
        yield return [new BodyNode([new ReturnNode(null)]), "VoidButReturn", typeof(void), false];

        var whileBody = new BodyNode(
        [
            new WhileNode(
                new LiteralNode(true, typeof(bool)),
                new BodyNode([returnNode]))
        ]);

        yield return [whileBody, "WhileDoesNotGuaranteeReturn", typeof(int), true];

        var ifBody = new BodyNode(
        [
            new ConditionNode(new LiteralNode(false, typeof(bool)),
                new BodyNode([returnNode]),
                null)
        ]);

        yield return [ifBody, "IfDoesNotGuaranteeReturn", typeof(int), true];

        var ifElseBody = new BodyNode(
        [
            new ConditionNode(new LiteralNode(false, typeof(bool)),
                new BodyNode([returnNode]),
                new BodyNode([returnNode]))
        ]);

        yield return [ifElseBody, "IfElseGuaranteeReturn", typeof(int), false];

        var ifElseWithoutIfReturn = new BodyNode(
        [
            new ConditionNode(new LiteralNode(false, typeof(bool)),
                new BodyNode([]),
                new BodyNode([returnNode]))
        ]);
        
        yield return [ifElseWithoutIfReturn, "IfElseWithoutReturnInIfBranch", typeof(int), true];
        
        var ifElseWithoutElseReturn = new BodyNode(
        [
            new ConditionNode(new LiteralNode(false, typeof(bool)),
                new BodyNode([returnNode]),
                new BodyNode([]))
        ]);
        
        yield return [ifElseWithoutElseReturn, "IfElseWithoutReturnInElseBranch", typeof(int), true];
        
        var notFullIfElseWithBaseReturn = new BodyNode(
        [
            new ConditionNode(new LiteralNode(false, typeof(bool)),
                new BodyNode([returnNode]),
                new BodyNode([])),
            returnNode
        ]);
        
        yield return [notFullIfElseWithBaseReturn, "IfElseWithBaseReturn", typeof(int), false];
        
        var ifElseWithCompleteReturnAndBase = new BodyNode(
        [
            new ConditionNode(new LiteralNode(false, typeof(bool)),
                new BodyNode([returnNode]),
                new BodyNode([returnNode])),
            returnNode
        ]);
        
        yield return [ifElseWithCompleteReturnAndBase, "FullIfElseWithBaseReturn", typeof(int), false];

        var nestedFullIfElse = new BodyNode(
        [
            new ConditionNode(new LiteralNode(false, typeof(bool)),
                new ConditionNode(
                    new LiteralNode(false, typeof(bool)),
                    new BodyNode([returnNode]),
                    new BodyNode([returnNode])),
                new BodyNode([returnNode]))
        ]);
        
        yield return [nestedFullIfElse, "NestedConditionFull", typeof(int), false];
        
        var nestedNotFull = new BodyNode(
        [
            new ConditionNode(new LiteralNode(false, typeof(bool)),
                new ConditionNode(
                    new LiteralNode(false, typeof(bool)),
                    new BodyNode([returnNode]),
                    new BodyNode([])),
                new BodyNode([returnNode]))
        ]);
        
        yield return [nestedNotFull, "NestedNotFull", typeof(int), false];
    }

    [Fact]
    public void VisitMultipleInvalidMethods()
    {
        var node = new RootNode([], null,
            [
                CreateMethod(typeof(int), "1", [], new BodyNode([])),
                CreateMethod(typeof(int), "1", [], new BodyNode([]))
            ]);
        
        var validator = new MethodMustReturnValueValidator();
        var context = new PreCreationContext("aaa", new MockSymbolTable());
        
        var res = validator.Validate(node, context);
        Assert.Equal(2, res.Exceptions.Count);
        Assert.All(res.Exceptions, Assert.NotNull);
    }

    private static FuncNode CreateMethod(Type returnType, string name, List<NodeBase> parameters, BodyNode body)
    {
        return new FuncNode(
            CreateTypeNode(returnType),
            new MemberNode(name),
            parameters.Cast<ParameterNode>().ToList(),
            body
        );
    }
    
    private static TypeNode CreateTypeNode(Type fromType)
    {
        var type = new TypeNode(new MemberNode(""));
        type.SetType(fromType);
        return type;
    }

    private class MockSymbolTable : ISymbolTable
    {
        public PlampException SetExceptionToNode(NodeBase node, PlampExceptionRecord exceptionRecord, string fileName)
        {
            return node is FuncNode ? new PlampException(exceptionRecord, new(1, 1), new(1, 1), fileName) : throw new ArgumentException();
        }

        public PlampException SetExceptionToNodeRange(List<NodeBase> nodes, PlampExceptionRecord exceptionRecord, string fileName)
        {
            throw new NotImplementedException();
        }

        public bool TryGetSymbol(NodeBase symbol, out KeyValuePair<FilePosition, FilePosition> pair)
        {
            throw new NotImplementedException();
        }

        public void AddSymbol(NodeBase symbol, FilePosition start, FilePosition end)
        {
            throw new NotImplementedException();
        }
    }
}