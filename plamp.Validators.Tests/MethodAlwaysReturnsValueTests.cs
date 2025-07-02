using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Validators.BasicSemanticsValidators.MustReturn;
using Xunit;

namespace plamp.Validators.Tests;

public class MethodAlwaysReturnsValueTests
{
    [Theory]
    [MemberData(nameof(AlwaysReturnsValidatorDataProvider))]
    public void VoidMethod(BodyNode body, string methodName, Type returnType, bool shouldExcept)
    {
        var defNode = CreateMethod(returnType, methodName, [], body);
        var validator = new MethodMustReturnValueValidator();
        var context = new MustReturnValueContext()
        {
            Exceptions = [],
            SymbolTable = new MockSymbolTable()
        };
        
        var res = validator.Validate(defNode, context)!;
        
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
        var node = new TypeDefinitionNode(
            new MemberNode("123"),
            [
                CreateMethod(typeof(int), "1", [], new BodyNode([])),
                CreateMethod(typeof(int), "1", [], new BodyNode([]))
            ]);
        
        var validator = new MethodMustReturnValueValidator();
        var context = new MustReturnValueContext()
        {
            Exceptions = [],
            SymbolTable = new MockSymbolTable()
        };
        
        var res = validator.Validate(node, context)!;
        Assert.Equal(2, res.Exceptions.Count);
        Assert.All(res.Exceptions, Assert.NotNull);
    }

    private static DefNode CreateMethod(Type returnType, string name, List<NodeBase> parameters, BodyNode body)
    {
        return new DefNode(
            CreateTypeNode(returnType),
            new MemberNode(name),
            parameters,
            body
        );
    }
    
    private static TypeNode CreateTypeNode(Type fromType) => new(null, null) { Symbol = fromType };

    private class MockSymbolTable : ISymbolTable
    {
        public PlampException? SetExceptionToNodeAndChildren(PlampExceptionRecord exceptionRecord, NodeBase node, string fileName,
            AssemblyName assemblyName)
        {
            return node is DefNode ? new PlampException(exceptionRecord, new(1, 1), new(1, 1), fileName, assemblyName) : null;
        }

        public PlampException? SetExceptionToNodeWithoutChildren(PlampExceptionRecord exceptionRecord, NodeBase node, string fileName,
            AssemblyName assemblyName)
        {
            return node is DefNode ? new PlampException(exceptionRecord, new(1, 1), new(1, 1), fileName, assemblyName) : null;
        }

        public List<PlampException?> SetExceptionToChildren(PlampExceptionRecord exceptionRecord, NodeBase node, string fileName,
            AssemblyName assemblyName)
        {
            return node
                .Visit()
                .Select(x => SetExceptionToNodeWithoutChildren(exceptionRecord, x, fileName, assemblyName))
                .ToList();
        }

        public bool Contains(NodeBase node) => false;

        public bool TryGetChildren(NodeBase node, out IReadOnlyList<NodeBase> children)
        {
            children = [];
            return false;
        }
    }
}