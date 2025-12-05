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
        var context = new PreCreationContext(new MockTranslationTable(), new SymbolTable("mod", []));
        
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
        var returnNode = new ReturnNode(new LiteralNode(1, RuntimeSymbols.GetSymbolTable.MakeInt())); 
        yield return [new BodyNode([]), "VoidMethod", RuntimeSymbols.GetSymbolTable.MakeVoid(), false];
        yield return [new BodyNode([returnNode]), "SimpleReturn", RuntimeSymbols.GetSymbolTable.MakeInt(), false];
        yield return [new BodyNode([returnNode, returnNode]), "ReturnTwice", RuntimeSymbols.GetSymbolTable.MakeInt(), false];
        yield return [new BodyNode([]), "SimpleDoesNotReturn", RuntimeSymbols.GetSymbolTable.MakeInt(), true];
        yield return [new BodyNode([new ReturnNode(null)]), "VoidButReturn", RuntimeSymbols.GetSymbolTable.MakeVoid(), false];

        var whileBody = new BodyNode(
        [
            new WhileNode(
                new LiteralNode(true, RuntimeSymbols.GetSymbolTable.MakeLogical()),
                new BodyNode([returnNode]))
        ]);

        yield return [whileBody, "WhileDoesNotGuaranteeReturn", RuntimeSymbols.GetSymbolTable.MakeInt(), true];

        var ifBody = new BodyNode(
        [
            new ConditionNode(new LiteralNode(false, RuntimeSymbols.GetSymbolTable.MakeLogical()),
                new BodyNode([returnNode]),
                null)
        ]);

        yield return [ifBody, "IfDoesNotGuaranteeReturn", RuntimeSymbols.GetSymbolTable.MakeInt(), true];

        var ifElseBody = new BodyNode(
        [
            new ConditionNode(new LiteralNode(false, RuntimeSymbols.GetSymbolTable.MakeLogical()),
                new BodyNode([returnNode]),
                new BodyNode([returnNode]))
        ]);

        yield return [ifElseBody, "IfElseGuaranteeReturn", RuntimeSymbols.GetSymbolTable.MakeInt(), false];

        var ifElseWithoutIfReturn = new BodyNode(
        [
            new ConditionNode(new LiteralNode(false, RuntimeSymbols.GetSymbolTable.MakeLogical()),
                new BodyNode([]),
                new BodyNode([returnNode]))
        ]);
        
        yield return [ifElseWithoutIfReturn, "IfElseWithoutReturnInIfBranch", RuntimeSymbols.GetSymbolTable.MakeInt(), true];
        
        var ifElseWithoutElseReturn = new BodyNode(
        [
            new ConditionNode(new LiteralNode(false, RuntimeSymbols.GetSymbolTable.MakeLogical()),
                new BodyNode([returnNode]),
                new BodyNode([]))
        ]);
        
        yield return [ifElseWithoutElseReturn, "IfElseWithoutReturnInElseBranch", RuntimeSymbols.GetSymbolTable.MakeInt(), true];
        
        var notFullIfElseWithBaseReturn = new BodyNode(
        [
            new ConditionNode(new LiteralNode(false, RuntimeSymbols.GetSymbolTable.MakeLogical()),
                new BodyNode([returnNode]),
                new BodyNode([])),
            returnNode
        ]);
        
        yield return [notFullIfElseWithBaseReturn, "IfElseWithBaseReturn", RuntimeSymbols.GetSymbolTable.MakeInt(), false];
        
        var ifElseWithCompleteReturnAndBase = new BodyNode(
        [
            new ConditionNode(new LiteralNode(false, RuntimeSymbols.GetSymbolTable.MakeLogical()),
                new BodyNode([returnNode]),
                new BodyNode([returnNode])),
            returnNode
        ]);
        
        yield return [ifElseWithCompleteReturnAndBase, "FullIfElseWithBaseReturn", RuntimeSymbols.GetSymbolTable.MakeInt(), false];

        var nestedFullIfElse = new BodyNode(
        [
            new ConditionNode(new LiteralNode(false, RuntimeSymbols.GetSymbolTable.MakeLogical()),
                new ConditionNode(
                    new LiteralNode(false, RuntimeSymbols.GetSymbolTable.MakeLogical()),
                    new BodyNode([returnNode]),
                    new BodyNode([returnNode])),
                new BodyNode([returnNode]))
        ]);
        
        yield return [nestedFullIfElse, "NestedConditionFull", RuntimeSymbols.GetSymbolTable.MakeInt(), false];
        
        var nestedNotFull = new BodyNode(
        [
            new ConditionNode(new LiteralNode(false, RuntimeSymbols.GetSymbolTable.MakeLogical()),
                new ConditionNode(
                    new LiteralNode(false, RuntimeSymbols.GetSymbolTable.MakeLogical()),
                    new BodyNode([returnNode]),
                    new BodyNode([])),
                new BodyNode([returnNode]))
        ]);
        
        yield return [nestedNotFull, "NestedNotFull", RuntimeSymbols.GetSymbolTable.MakeInt(), false];
    }

    [Fact]
    public void VisitMultipleInvalidMethods()
    {
        var node = new RootNode([], null,
            [
                CreateMethod(RuntimeSymbols.GetSymbolTable.MakeInt(), "1", [], new BodyNode([])),
                CreateMethod(RuntimeSymbols.GetSymbolTable.MakeInt(), "1", [], new BodyNode([]))
            ],
            []);
        
        var validator = new FuncMustReturnValueValidator();
        var context = new PreCreationContext(new MockTranslationTable(), new SymbolTable("mod", []));
        
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