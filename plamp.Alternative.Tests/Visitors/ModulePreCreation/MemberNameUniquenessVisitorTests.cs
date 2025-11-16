using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Alternative.Visitors.ModulePreCreation;
using plamp.Alternative.Visitors.ModulePreCreation.MemberNameUniqueness;
using Xunit;

namespace plamp.Alternative.Tests.Visitors.ModulePreCreation;

public class MemberNameUniquenessVisitorTests
{
    private const int Utf16CharacterByteCount = 2;
    
    [Fact]
    public void SingleMember_DoesNotReturnExceptions()
    {
        var table = new TranslationTable();
        var func1Name = new FuncNameNode("fn1");
        var funcBody = new BodyNode([]);
        var func1 = new FuncNode(null, func1Name, [], funcBody);
        var root = new RootNode([], null, [func1], []);
        table.AddSymbol(func1Name, new FilePosition(0, 2, ""));
        table.AddSymbol(funcBody, new FilePosition(2 * Utf16CharacterByteCount, 2, ""));
        table.AddSymbol(func1, new FilePosition(0, 0, ""));
        table.AddSymbol(root, new FilePosition(-1, 0, ""));
        var context = new PreCreationContext(table);
        var visitor = new MemberNameUniquenessValidator();
        var resultContext = visitor.Validate(root, context);
        Assert.Empty(resultContext.Exceptions);
    }

    [Fact]
    public void MultipleMembersWithDifferNames_DoesNotReturnExceptions()
    {
        var table = new TranslationTable();
        var func1Name = new FuncNameNode("fn1");
        var funcBody = new BodyNode([]);
        var func1 = new FuncNode(null, func1Name, [], funcBody);
        
        var func2Name = new FuncNameNode("fn2");
        var func2Body = new BodyNode([]);
        var func2 = new FuncNode(null, func2Name, [], func2Body);
        
        var root = new RootNode([], null, [func1, func2], []);
        
        table.AddSymbol(func1Name, new FilePosition(Utf16CharacterByteCount * 3, 3, ""));
        table.AddSymbol(funcBody, new FilePosition(Utf16CharacterByteCount * 9, 2, ""));
        table.AddSymbol(func1, new FilePosition(0, 2, ""));
        
        table.AddSymbol(func2Name, new FilePosition(Utf16CharacterByteCount * 14, 3, ""));
        table.AddSymbol(func2Body, new FilePosition(Utf16CharacterByteCount * 20, 2, ""));
        table.AddSymbol(func2, new FilePosition(Utf16CharacterByteCount * 11, 2, ""));
        
        table.AddSymbol(root, new FilePosition(-1, 0, ""));
        
        var context = new PreCreationContext(table);
        var visitor = new MemberNameUniquenessValidator();
        var resultContext = visitor.Validate(root, context);
        Assert.Empty(resultContext.Exceptions);
    }

    [Fact]
    public void MultipleMembersWithSameName_ReturnExceptionsToAllMembers()
    {
        var table = new TranslationTable();
        var func1Name = new FuncNameNode("fn1");
        var funcBody = new BodyNode([]);
        var func1 = new FuncNode(null, func1Name, [], funcBody);
        
        var func2Name = new FuncNameNode("fn1");
        var func2Body = new BodyNode([]);
        var func2 = new FuncNode(null, func2Name, [], func2Body);
        
        var root = new RootNode([], null, [func1, func2], []);
        
        table.AddSymbol(func1Name, new FilePosition(Utf16CharacterByteCount * 3, 3, ""));
        table.AddSymbol(funcBody, new FilePosition(Utf16CharacterByteCount * 9, 2, ""));
        table.AddSymbol(func1, new FilePosition(0, 2, ""));
        
        table.AddSymbol(func2Name, new FilePosition(Utf16CharacterByteCount * 14, 3, ""));
        table.AddSymbol(func2Body, new FilePosition(Utf16CharacterByteCount * 20, 2, ""));
        table.AddSymbol(func2, new FilePosition(Utf16CharacterByteCount * 11, 2, ""));
        
        table.AddSymbol(root, new FilePosition(-1, 0, ""));
        
        var context = new PreCreationContext(table);
        var visitor = new MemberNameUniquenessValidator();
        var resultContext = visitor.Validate(root, context);
        Assert.Equal(2, resultContext.Exceptions.Count);
    }
}