using System;
using System.Collections.Generic;
using System.Linq;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Abstractions.Symbols.SymTable;
using plamp.Alternative.Visitors.ModulePreCreation;
using plamp.Alternative.Visitors.ModulePreCreation.MustReturn;
using Xunit;

namespace plamp.Alternative.Tests.Visitors.ModulePreCreation;

public class MethodAlwaysReturnsValueTests
{
    [Theory]
    [MemberData(nameof(AlwaysReturnsValidatorDataProvider))]
    public void VoidMethod(BodyNode body, string methodName, ITypeInfo returnType, bool shouldExcept)
    {
        var defNode = CreateMethod(returnType, methodName, [], body);
        var validator = new FuncMustReturnValueValidator();
        var context = new PreCreationContext(new MockTranslationTable(), SymbolTableInitHelper.CreateDefaultTables());
        
        var res = validator.Validate(defNode, context);
        
        if (shouldExcept)
        {
            Assert.Single(res.Exceptions);
            Assert.NotNull(res.Exceptions[0]);
        }
        else
        {
            Assert.Empty(res.Exceptions);
        }
    }

    public static IEnumerable<object[]> AlwaysReturnsValidatorDataProvider()
    {
        var returnNode = new ReturnNode(new LiteralNode(1, Builtins.Int)); 
        yield return [new BodyNode([]), "VoidMethod", Builtins.Void, false];
        yield return [new BodyNode([returnNode]), "SimpleReturn", Builtins.Int, false];
        yield return [new BodyNode([returnNode, returnNode]), "ReturnTwice", Builtins.Int, false];
        yield return [new BodyNode([]), "SimpleDoesNotReturn", Builtins.Int, true];
        yield return [new BodyNode([new ReturnNode(null)]), "VoidButReturn", Builtins.Void, false];

        var whileBody = new BodyNode(
        [
            new WhileNode(
                new LiteralNode(true, Builtins.Bool),
                new BodyNode([returnNode]))
        ]);

        yield return [whileBody, "WhileDoesNotGuaranteeReturn", Builtins.Int, true];

        var ifBody = new BodyNode(
        [
            new ConditionNode(new LiteralNode(false, Builtins.Bool),
                new BodyNode([returnNode]),
                null)
        ]);

        yield return [ifBody, "IfDoesNotGuaranteeReturn", Builtins.Int, true];

        var ifElseBody = new BodyNode(
        [
            new ConditionNode(new LiteralNode(false, Builtins.Bool),
                new BodyNode([returnNode]),
                new BodyNode([returnNode]))
        ]);

        yield return [ifElseBody, "IfElseGuaranteeReturn", Builtins.Int, false];

        var ifElseWithoutIfReturn = new BodyNode(
        [
            new ConditionNode(new LiteralNode(false, Builtins.Bool),
                new BodyNode([]),
                new BodyNode([returnNode]))
        ]);
        
        yield return [ifElseWithoutIfReturn, "IfElseWithoutReturnInIfBranch", Builtins.Int, true];
        
        var ifElseWithoutElseReturn = new BodyNode(
        [
            new ConditionNode(new LiteralNode(false, Builtins.Bool),
                new BodyNode([returnNode]),
                new BodyNode([]))
        ]);
        
        yield return [ifElseWithoutElseReturn, "IfElseWithoutReturnInElseBranch", Builtins.Int, true];
        
        var notFullIfElseWithBaseReturn = new BodyNode(
        [
            new ConditionNode(new LiteralNode(false, Builtins.Bool),
                new BodyNode([returnNode]),
                new BodyNode([])),
            returnNode
        ]);
        
        yield return [notFullIfElseWithBaseReturn, "IfElseWithBaseReturn", Builtins.Int, false];
        
        var ifElseWithCompleteReturnAndBase = new BodyNode(
        [
            new ConditionNode(new LiteralNode(false, Builtins.Bool),
                new BodyNode([returnNode]),
                new BodyNode([returnNode])),
            returnNode
        ]);
        
        yield return [ifElseWithCompleteReturnAndBase, "FullIfElseWithBaseReturn", Builtins.Int, false];

        var nestedFullIfElse = new BodyNode(
        [
            new ConditionNode(new LiteralNode(false, Builtins.Bool),
                new ConditionNode(
                    new LiteralNode(false, Builtins.Bool),
                    new BodyNode([returnNode]),
                    new BodyNode([returnNode])),
                new BodyNode([returnNode]))
        ]);
        
        yield return [nestedFullIfElse, "NestedConditionFull", Builtins.Int, false];
        
        var nestedNotFull = new BodyNode(
        [
            new ConditionNode(new LiteralNode(false, Builtins.Bool),
                new ConditionNode(
                    new LiteralNode(false, Builtins.Bool),
                    new BodyNode([returnNode]),
                    new BodyNode([])),
                new BodyNode([returnNode]))
        ]);
        
        yield return [nestedNotFull, "NestedNotFull", Builtins.Int, false];
    }

    [Fact]
    public void VisitMultipleInvalidMethods()
    {
        var node = new RootNode([], null,
            [
                CreateMethod(Builtins.Int, "1", [], new BodyNode([])),
                CreateMethod(Builtins.Int, "1", [], new BodyNode([]))
            ],
            []);
        
        var validator = new FuncMustReturnValueValidator();
        
        var context = new PreCreationContext(new MockTranslationTable(), SymbolTableInitHelper.CreateDefaultTables());
        
        var res = validator.Validate(node, context);
        Assert.Equal(2, res.Exceptions.Count);
        Assert.All(res.Exceptions, Assert.NotNull);
    }

    private static FuncNode CreateMethod(ITypeInfo returnType, string name, List<NodeBase> parameters, BodyNode body)
    {
        return new FuncNode(
            CreateTypeNode(returnType),
            new FuncNameNode(name),
            [],
            parameters.Cast<ParameterNode>().ToList(),
            body
        );
    }
    
    private static TypeNode CreateTypeNode(ITypeInfo fromType)
    {
        var type = new TypeNode(new TypeNameNode(""))
        {
            TypeInfo = fromType
        };
        return type;
    }

    private class MockTranslationTable : ITranslationTable
    {
        public PlampException SetExceptionToNode(NodeBase node, PlampExceptionRecord exceptionRecord)
        {
            return node is FuncNode ? new PlampException(exceptionRecord, new(1, 1, "aaa")) : throw new ArgumentException();
        }

        public bool TryGetSymbol(NodeBase symbol, out FilePosition position)
        {
            throw new NotSupportedException();
        }

        public void AddSymbol(NodeBase symbol, FilePosition position)
        {
            throw new NotSupportedException();
        }

        public bool RemoveSymbol(NodeBase symbol)
        {
            throw new NotSupportedException();
        }

        public ITranslationTable Fork()
        {
            throw new NotSupportedException();
        }

        public void Merge(ITranslationTable child)
        {
            throw new NotSupportedException();
        }
    }
}