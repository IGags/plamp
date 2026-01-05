using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ComplexTypes;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Abstractions.Ast.Node.Definitions.Variable;
using plamp.Alternative;
using plamp.Alternative.SymbolsImpl;
using plamp.CodeEmission.Tests.Infrastructure;
using plamp.ILCodeEmitters;
using Shouldly;

namespace plamp.CodeEmission.Tests;

public class FieldAssignEmissionTests
{
    public class Point
    {
        [PlampFieldGenerated]
        public int X = 933;
        [PlampFieldGenerated]
        public int Y = 934;
    }
    
    public class Line
    {
        public Point First = new();
        public Point Second = new();
    }
    
    [Fact]
    //В ноде нет мембера - ошибка
    public void NodeHasNoMember_Incorrect()
    {
        var type = new TypeNode(new TypeNameNode("Point"))
        {
            TypeInfo = EmissionSetupHelper.MakeTypeRef(typeof(Point))
        };

        var fieldType = new TypeNode(new TypeNameNode("int"))
        {
            TypeInfo = Builtins.Int
        };
        var initType = new InitTypeNode(type, []);
        var fieldNode = new FieldNode("X");
        var fieldAccess = new FieldAccessNode(null!, fieldNode);
        var ast = new BodyNode(
        [
            new AssignNode([new VariableDefinitionNode(type, new VariableNameNode("a"))], [initType]),
            new AssignNode([new VariableDefinitionNode(fieldType, new VariableNameNode("b"))], [fieldAccess]),
            new ReturnNode(new MemberNode("b"))
        ]);
        var (_, methodInfo, _) = EmissionSetupHelper.CreateMethodBuilder("mth", typeof(int), []);
        Should.Throw<Exception>(() => IlCodeEmitter.EmitMethodBody(new CompilerEmissionContext(ast, methodInfo, [], null)));
    }
    
    [Fact]
    //Присвоение из поля
    public void EmitAssignFromField_Correct()
    {
        var type = new TypeNode(new TypeNameNode("Point"))
        {
            TypeInfo = EmissionSetupHelper.MakeTypeRef(typeof(Point))
        };

        var fieldType = new TypeNode(new TypeNameNode("int"))
        {
            TypeInfo = Builtins.Int
        };
        var fld = typeof(Point).GetField(nameof(Point.X))!;
        var initType = new InitTypeNode(type, []);
        var fieldNode = new FieldNode("X"){ FieldInfo = EmissionSetupHelper.MakeFieldRef(fld)};
        var fieldAccess = new FieldAccessNode(new MemberNode("a"), fieldNode);
        var ast = new BodyNode(
        [
            new AssignNode([new VariableDefinitionNode(type, new VariableNameNode("a"))], [initType]),
            new AssignNode([new VariableDefinitionNode(fieldType, new VariableNameNode("b"))], [fieldAccess]),
            new ReturnNode(new MemberNode("b"))
        ]);
        var (instance, methodInfo) = EmissionSetupHelper.CreateInstanceWithMethod([], ast, typeof(int));
        var res = methodInfo!.Invoke(instance, []).ShouldBeOfType<int>();
        res.ShouldBe(933);
    }
    
    [Fact]
    //Присвоение в поле
    public void EmitAssignToField_Correct()
    {
        var type = new TypeNode(new TypeNameNode("Point"))
        {
            TypeInfo = EmissionSetupHelper.MakeTypeRef(typeof(Point))
        };
        var fld = typeof(Point).GetField(nameof(Point.X))!;
        var initType = new InitTypeNode(type, []);
        var fieldNode = new FieldNode("X"){ FieldInfo = EmissionSetupHelper.MakeFieldRef(fld)};
        var fieldAccess = new FieldAccessNode(new MemberNode("a"), fieldNode);
        var ast = new BodyNode(
        [
            new AssignNode([new VariableDefinitionNode(type, new VariableNameNode("a"))], [initType]),
            new AssignNode([fieldAccess], [new LiteralNode(1, Builtins.Int)]),
            new ReturnNode(new MemberNode("a"))
        ]);
        var (instance, methodInfo) = EmissionSetupHelper.CreateInstanceWithMethod([], ast, typeof(Point));
        var res = methodInfo!.Invoke(instance, []).ShouldBeOfType<Point>();
        res.X.ShouldBe(1);
    }
    
    [Fact]
    //Пустое представление из .net - ошибка
    public void AssignFromFieldEmptyNetRepresentation_Throws()
    {
        var type = new TypeNode(new TypeNameNode("Point"))
        {
            TypeInfo = EmissionSetupHelper.MakeTypeRef(typeof(Point))
        };

        var fieldType = new TypeNode(new TypeNameNode("int"))
        {
            TypeInfo = Builtins.Int
        };
        var initType = new InitTypeNode(type, []);
        var fieldNode = new FieldNode("X");
        var fieldAccess = new FieldAccessNode(new MemberNode("a"), fieldNode);
        var ast = new BodyNode(
        [
            new AssignNode([new VariableDefinitionNode(type, new VariableNameNode("a"))], [initType]),
            new AssignNode([new VariableDefinitionNode(fieldType, new VariableNameNode("b"))], [fieldAccess]),
            new ReturnNode(new MemberNode("b"))
        ]);
        var (_, methodInfo, _) = EmissionSetupHelper.CreateMethodBuilder("mth", typeof(int), []);
        Should.Throw<Exception>(() => IlCodeEmitter.EmitMethodBody(new CompilerEmissionContext(ast, methodInfo, [], null)));
    }
    
    [Fact]
    //Присвоение из цепочки полей
    public void AssignFromFieldAccessSequence_Correct()
    {
        var type = new TypeNode(new TypeNameNode("Point"))
        {
            TypeInfo = EmissionSetupHelper.MakeTypeRef(typeof(Point))
        };

        var fieldType = new TypeNode(new TypeNameNode("int"))
        {
            TypeInfo = Builtins.Int
        };
        var fld = typeof(Point).GetField(nameof(Point.X))!;
        var initType = new InitTypeNode(type, []);
        var fieldNode = new FieldNode("X"){ FieldInfo = EmissionSetupHelper.MakeFieldRef(fld)};
        var fieldAccess = new FieldAccessNode(new MemberNode("a"), fieldNode);
        var ast = new BodyNode(
        [
            new AssignNode([new VariableDefinitionNode(type, new VariableNameNode("a"))], [initType]),
            new AssignNode([new VariableDefinitionNode(fieldType, new VariableNameNode("b"))], [fieldAccess]),
            new ReturnNode(new MemberNode("b"))
        ]);
        var (instance, methodInfo) = EmissionSetupHelper.CreateInstanceWithMethod([], ast, typeof(int));
        var res = methodInfo!.Invoke(instance, []).ShouldBeOfType<int>();
        res.ShouldBe(933);
    }
    
    [Fact]
    //Присвоение в цепочку полей
    public void AssignToFieldSequence_Correct()
    {
        var lineType = new TypeNode(new TypeNameNode("Line"))
        {
            TypeInfo = EmissionSetupHelper.MakeTypeRef(typeof(Line))
        };
        var initType = new InitTypeNode(lineType, []);

        var lineField = typeof(Line).GetField(nameof(Line.First))!;
        var pointField = typeof(Point).GetField(nameof(Point.X))!;
        
        var lineFieldNode = new FieldNode("First"){FieldInfo = EmissionSetupHelper.MakeFieldRef(lineField)};
        var pointFieldNode = new FieldNode("X") { FieldInfo = EmissionSetupHelper.MakeFieldRef(pointField) };
        var fieldAccess = new FieldAccessNode(new FieldAccessNode(new MemberNode("a"), lineFieldNode), pointFieldNode);
        var ast = new BodyNode(
        [
            new AssignNode([new VariableDefinitionNode(lineType, new VariableNameNode("a"))], [initType]),
            new AssignNode([fieldAccess], [new LiteralNode(1, Builtins.Int)]),
            new ReturnNode(new MemberNode("a"))
        ]);
        var (instance, methodInfo) = EmissionSetupHelper.CreateInstanceWithMethod([], ast, typeof(Line));
        var res = methodInfo!.Invoke(instance, []).ShouldBeOfType<Line>();
        res.First.X.ShouldBe(1);
    }
}