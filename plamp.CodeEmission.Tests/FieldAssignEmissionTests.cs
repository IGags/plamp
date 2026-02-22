using System.Reflection;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ComplexTypes;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Abstractions.Ast.Node.Definitions.Variable;
using plamp.Abstractions.Symbols;
using plamp.Alternative;
using plamp.CodeEmission.Tests.Infrastructure;
using plamp.ILCodeEmitters;
using Shouldly;

namespace plamp.CodeEmission.Tests;

public class FieldAssignEmissionTests
{
    public class Point
    {
        [PlampVisible]
        public int X = 933;
        [PlampVisible]
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
        Should.Throw<Exception>(() => IlCodeEmitter.EmitMethodBody(ast, methodInfo, []));
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
        Should.Throw<Exception>(() => IlCodeEmitter.EmitMethodBody(ast, methodInfo, []));
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

    public class FldArrayTest
    {
        public int[] Fld = new int[3];
    }
    
    [Fact]
    public void AssignToFieldFromArrayElement_Correct()
    {
        /*
         * a.Fld[2] := 32;
         */
        var fieldInfo = typeof(FldArrayTest).GetField(nameof(FldArrayTest.Fld))!;
        var field = new FieldNode(nameof(FldArrayTest.Fld)) { FieldInfo = EmissionSetupHelper.MakeFieldRef(fieldInfo) };
        var fieldAccess = new FieldAccessNode(new MemberNode("a"), field);
        var indexer = new IndexerNode(fieldAccess, new LiteralNode(2, Builtins.Int)){ItemType = Builtins.Int};
        var valueShould = Random.Shared.Next();
        var assign = new AssignNode([indexer], [new LiteralNode(valueShould, Builtins.Int)]);
        var body = new BodyNode(
        [
            assign
        ]);

        var parameter = new TestParameter(typeof(FldArrayTest), "a");
        var (instance, methodInfo) = EmissionSetupHelper.CreateInstanceWithMethod([parameter], body, typeof(void));
        var arg = new FldArrayTest();
        methodInfo!.Invoke(instance, [arg]);
        arg.Fld[2].ShouldBe(valueShould);
    }

    [Fact]
    public void GetFromFieldFromArrayElement_Correct()
    {
        /*
         * return a.Fld[1];
         */
        var fieldInfo = typeof(FldArrayTest).GetField(nameof(FldArrayTest.Fld))!;
        var field = new FieldNode(nameof(FldArrayTest.Fld)) { FieldInfo = EmissionSetupHelper.MakeFieldRef(fieldInfo) };
        var fieldAccess = new FieldAccessNode(new MemberNode("a"), field);
        var indexer = new IndexerNode(fieldAccess, new LiteralNode(1, Builtins.Int)){ItemType = Builtins.Int};
        var ret = new ReturnNode(indexer);
        var body = new BodyNode([ret]);
        
        var parameter = new TestParameter(typeof(FldArrayTest), "a");
        var (instance, methodInfo) = EmissionSetupHelper.CreateInstanceWithMethod([parameter], body, typeof(int));
        var valueShould = Random.Shared.Next();
        var arg = new FldArrayTest(){Fld = {[1] = valueShould}};

        var res = methodInfo!.Invoke(instance, [arg]);
        res.ShouldBeOfType<int>().ShouldBe(valueShould);
    }

    public static Point GetFixedPoint()
    {
        return new Point { X = 12 };
    }
    
    [Fact]
    public void GetFromFieldFromFuncCall_Correct()
    {
        /*
         * return call().Fld;
         */
        var fnInfo = GetType().GetMethod(nameof(GetFixedPoint), BindingFlags.Public | BindingFlags.Static, [])!;
        var call = EmissionSetupHelper.CreateCallNode(null, EmissionSetupHelper.MakeFuncRef(fnInfo), []);
        var fieldInfo = typeof(Point).GetField(nameof(Point.X))!;
        var field = new FieldNode(nameof(Point.X)) { FieldInfo = EmissionSetupHelper.MakeFieldRef(fieldInfo) };
        var fieldAccess = new FieldAccessNode(call, field);
        var body = new BodyNode([new ReturnNode(fieldAccess)]);
        
        var (instance, methodInfo) = EmissionSetupHelper.CreateInstanceWithMethod([], body, typeof(int));
        var res = methodInfo!.Invoke(instance, []);
        res.ShouldBeOfType<int>().ShouldBe(12);
    }
}