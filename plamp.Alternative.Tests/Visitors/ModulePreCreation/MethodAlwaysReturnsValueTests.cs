using System;
using System.Collections.Generic;
using System.Linq;
using plamp.Abstractions;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Alternative.Visitors.ModulePreCreation;
using plamp.Alternative.Visitors.ModulePreCreation.MustReturn;
using plamp.Intrinsics;
using Xunit;

namespace plamp.Alternative.Tests.Visitors.ModulePreCreation;

public class MethodAlwaysReturnsValueTests
{
    [Theory]
    [MemberData(nameof(AlwaysReturnsValidatorDataProvider))]
    public void VoidMethod(BodyNode body, string methodName, ICompileTimeType returnType, bool shouldExcept)
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
        var returnNode = new ReturnNode(new LiteralNode(1, RuntimeSymbols.SymbolTable.Int)); 
        yield return [new BodyNode([]), "VoidMethod", RuntimeSymbols.SymbolTable.Void, false];
        yield return [new BodyNode([returnNode]), "SimpleReturn", RuntimeSymbols.SymbolTable.Int, false];
        yield return [new BodyNode([returnNode, returnNode]), "ReturnTwice", RuntimeSymbols.SymbolTable.Int, false];
        yield return [new BodyNode([]), "SimpleDoesNotReturn", RuntimeSymbols.SymbolTable.Int, true];
        yield return [new BodyNode([new ReturnNode(null)]), "VoidButReturn", RuntimeSymbols.SymbolTable.Void, false];

        var whileBody = new BodyNode(
        [
            new WhileNode(
                new LiteralNode(true, RuntimeSymbols.SymbolTable.Bool),
                new BodyNode([returnNode]))
        ]);

        yield return [whileBody, "WhileDoesNotGuaranteeReturn", RuntimeSymbols.SymbolTable.Int, true];

        var ifBody = new BodyNode(
        [
            new ConditionNode(new LiteralNode(false, RuntimeSymbols.SymbolTable.Bool),
                new BodyNode([returnNode]),
                null)
        ]);

        yield return [ifBody, "IfDoesNotGuaranteeReturn", RuntimeSymbols.SymbolTable.Int, true];

        var ifElseBody = new BodyNode(
        [
            new ConditionNode(new LiteralNode(false, RuntimeSymbols.SymbolTable.Bool),
                new BodyNode([returnNode]),
                new BodyNode([returnNode]))
        ]);

        yield return [ifElseBody, "IfElseGuaranteeReturn", RuntimeSymbols.SymbolTable.Int, false];

        var ifElseWithoutIfReturn = new BodyNode(
        [
            new ConditionNode(new LiteralNode(false, RuntimeSymbols.SymbolTable.Bool),
                new BodyNode([]),
                new BodyNode([returnNode]))
        ]);
        
        yield return [ifElseWithoutIfReturn, "IfElseWithoutReturnInIfBranch", RuntimeSymbols.SymbolTable.Int, true];
        
        var ifElseWithoutElseReturn = new BodyNode(
        [
            new ConditionNode(new LiteralNode(false, RuntimeSymbols.SymbolTable.Bool),
                new BodyNode([returnNode]),
                new BodyNode([]))
        ]);
        
        yield return [ifElseWithoutElseReturn, "IfElseWithoutReturnInElseBranch", RuntimeSymbols.SymbolTable.Int, true];
        
        var notFullIfElseWithBaseReturn = new BodyNode(
        [
            new ConditionNode(new LiteralNode(false, RuntimeSymbols.SymbolTable.Bool),
                new BodyNode([returnNode]),
                new BodyNode([])),
            returnNode
        ]);
        
        yield return [notFullIfElseWithBaseReturn, "IfElseWithBaseReturn", RuntimeSymbols.SymbolTable.Int, false];
        
        var ifElseWithCompleteReturnAndBase = new BodyNode(
        [
            new ConditionNode(new LiteralNode(false, RuntimeSymbols.SymbolTable.Bool),
                new BodyNode([returnNode]),
                new BodyNode([returnNode])),
            returnNode
        ]);
        
        yield return [ifElseWithCompleteReturnAndBase, "FullIfElseWithBaseReturn", RuntimeSymbols.SymbolTable.Int, false];

        var nestedFullIfElse = new BodyNode(
        [
            new ConditionNode(new LiteralNode(false, RuntimeSymbols.SymbolTable.Bool),
                new ConditionNode(
                    new LiteralNode(false, RuntimeSymbols.SymbolTable.Bool),
                    new BodyNode([returnNode]),
                    new BodyNode([returnNode])),
                new BodyNode([returnNode]))
        ]);
        
        yield return [nestedFullIfElse, "NestedConditionFull", RuntimeSymbols.SymbolTable.Int, false];
        
        var nestedNotFull = new BodyNode(
        [
            new ConditionNode(new LiteralNode(false, RuntimeSymbols.SymbolTable.Bool),
                new ConditionNode(
                    new LiteralNode(false, RuntimeSymbols.SymbolTable.Bool),
                    new BodyNode([returnNode]),
                    new BodyNode([])),
                new BodyNode([returnNode]))
        ]);
        
        yield return [nestedNotFull, "NestedNotFull", RuntimeSymbols.SymbolTable.Int, false];
    }

    [Fact]
    public void VisitMultipleInvalidMethods()
    {
        var node = new RootNode([], null,
            [
                CreateMethod(RuntimeSymbols.SymbolTable.Int, "1", [], new BodyNode([])),
                CreateMethod(RuntimeSymbols.SymbolTable.Int, "1", [], new BodyNode([]))
            ],
            []);
        
        var validator = new FuncMustReturnValueValidator();
        
        var context = new PreCreationContext(new MockTranslationTable(), SymbolTableInitHelper.CreateDefaultTables());
        
        var res = validator.Validate(node, context);
        Assert.Equal(2, res.Exceptions.Count);
        Assert.All(res.Exceptions, Assert.NotNull);
    }

    private static FuncNode CreateMethod(ICompileTimeType returnType, string name, List<NodeBase> parameters, BodyNode body)
    {
        return new FuncNode(
            CreateTypeNode(returnType),
            new FuncNameNode(name),
            parameters.Cast<ParameterNode>().ToList(),
            body
        );
    }
    
    private static TypeNode CreateTypeNode(ICompileTimeType fromType)
    {
        var type = new TypeNode(new TypeNameNode(""));
        type.SetTypeRef(fromType);
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
            throw new NotImplementedException();
        }

        public void AddSymbol(NodeBase symbol, FilePosition position)
        {
            throw new NotImplementedException();
        }

        public ITranslationTable Fork()
        {
            throw new NotImplementedException();
        }

        public void Merge(ITranslationTable child)
        {
            throw new NotImplementedException();
        }
    }
}