using System.Linq;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Alternative;
using plamp.Alternative.Visitors.ModulePreCreation;
using plamp.Alternative.Visitors.ModulePreCreation.SignatureInference;
using Xunit;

namespace plamp.Validators.Tests.ModulePreCreation;

public class FuncSignatureInferenceVisitorTests
{
    private const string FileName = "program.plp";
    
    [Fact]
    public void EmptyRoot_NoExceptionNoSignatures()
    {
        var table = new SymbolTable();
        var root = new RootNode([], null, []);
        table.AddSymbol(root, new FilePosition(-1, -1), new FilePosition(-1, -1));
        var context = new PreCreationContext(FileName, table);
        var visitor = new SignatureTypeInferenceValidator();
        var result = visitor.WeaveDiffs(root, context);
        Assert.Empty(result.Functions);
        Assert.Empty(result.Exceptions);
    }

    [Fact]
    public void VoidDefinitionEmptyArgs_SingleSignature()
    {
        var funcName = "fn1";
        var table = new SymbolTable();
        var func1Name = new MemberNode(funcName);
        var func1Body = new BodyNode([]);
        var func1 = new DefNode(null, func1Name, [], func1Body);
        var root = new RootNode([], null, [func1]);
        table.AddSymbol(root, new FilePosition(-1, -1), new FilePosition(-1, -1));
        table.AddSymbol(func1, new FilePosition(0, 0), new FilePosition(0, 0));
        table.AddSymbol(func1Name, new FilePosition(0, 1), new FilePosition(0, 2));
        table.AddSymbol(func1Body, new FilePosition(1, 0), new FilePosition(1, 1));

        var context = new PreCreationContext(FileName, table);
        var visitor = new SignatureTypeInferenceValidator();
        var result = visitor.WeaveDiffs(root, context);
        Assert.Empty(result.Exceptions);
        var func = Assert.Single(result.Functions);
        Assert.Equal(funcName, func.Key);
        Assert.Equal(root.Funcs[0], func.Value);
        Assert.Equal(root.Funcs[0].ReturnType!.Symbol, typeof(void));
    }

    [Fact]
    public void VoidDefinitionSimpleReturnTypeEmptyArgs_SingleSignature()
    {
        var funcName = "fn1";
        var table = new SymbolTable();
        var typeName = new MemberNode("int");
        var returnType = new TypeNode(typeName);
        var func1Name = new MemberNode(funcName);
        var func1Body = new BodyNode([]);
        var func1 = new DefNode(returnType, func1Name, [], func1Body);
        var root = new RootNode([], null, [func1]);
        table.AddSymbol(root, new FilePosition(-1, -1), new FilePosition(-1, -1));
        table.AddSymbol(func1, new FilePosition(0, 0), new FilePosition(0, 0));
        table.AddSymbol(func1Name, new FilePosition(0, 1), new FilePosition(0, 2));
        table.AddSymbol(func1Body, new FilePosition(1, 0), new FilePosition(1, 1));
        table.AddSymbol(returnType, new FilePosition(1, 0), new FilePosition(1, 1));
        table.AddSymbol(typeName, new FilePosition(1, 0), new FilePosition(1, 1));
        
        var context = new PreCreationContext(FileName, table);
        var visitor = new SignatureTypeInferenceValidator();
        var result = visitor.WeaveDiffs(root, context);
        Assert.Empty(result.Exceptions);
        var func = Assert.Single(result.Functions);
        Assert.Equal(funcName, func.Key);
        Assert.Equal(func1, func.Value);
        Assert.Equal(func1.ReturnType!.Symbol, typeof(int));
    }
}