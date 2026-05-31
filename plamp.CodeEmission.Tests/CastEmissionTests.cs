using System.Reflection;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Binary;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ComplexTypes;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Abstractions.Ast.Node.Definitions.Variable;
using plamp.Abstractions.Ast.Node.Unary;
using plamp.Abstractions.Symbols.SymTable;
using plamp.Alternative;
using plamp.CodeEmission.Tests.Infrastructure;

namespace plamp.CodeEmission.Tests;

public class CastEmissionTests
{
    public class ExampleParent;
    
    
    public class ExampleChild : ExampleParent, IExampleInterface;
    
    public interface IExampleInterface;
    
    public struct ExampleStruct : IExampleInterface;
    
    /// <summary>
    /// Emit cast object to its parent
    /// </summary>
    [Theory]
    [MemberData(nameof(CastEmissionDataProvider))]
    public void EmitCastAsync(Type from, Type to, object? instance = null)
    {
        var inParam = new TestParameter(from, "toCast");
        const string castResName = "castRes";
        var fromRef = EmissionSetupHelper.MakeTypeRef(from);
        var toRef = EmissionSetupHelper.MakeTypeRef(to);
        
        var methodBody = new BodyNode(
        [
            new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(toRef), new VariableNameNode(castResName)),
            new AssignNode([new MemberNode(castResName)], [EmissionSetupHelper.CreateCastNode(fromRef, toRef, new MemberNode(inParam.Name))]),
            new ReturnNode(new MemberNode(castResName))
        ]);

        var (typeInstance, methodInfo) =
            EmissionSetupHelper.CreateInstanceWithMethod([inParam], methodBody, toRef.AsType());
        instance ??= Activator.CreateInstance(fromRef.AsType());
        var res = methodInfo!.Invoke(typeInstance, [instance]);
        Assert.NotNull(res);
        Assert.Equal(toRef.AsType(), res.GetType());
    }

    //I can't store object of type in object typed variable
    [Fact]
    public void EmitShortIntConversion()
    {
        var to = Builtins.Int;
        var from = Builtins.Short;
        const short instance = 31;
        var (methodInfo, typeInstance) = CreateConversionDelegate(from, to);
        var res = methodInfo.Invoke(typeInstance, [instance]);
        Assert.NotNull(res);
        Assert.Equal(to.AsType(), res.GetType());
    }
    
    [Fact]
    public void EmitByteIntConversion()
    {
        var to = Builtins.Int;
        var from = Builtins.Byte;
        const byte instance = 31;
        var (methodInfo, typeInstance) = CreateConversionDelegate(from, to);
        var res = methodInfo.Invoke(typeInstance, [instance]);
        Assert.NotNull(res);
        Assert.Equal(to.AsType(), res.GetType());
    }

    private (MethodInfo, object) CreateConversionDelegate(ITypeInfo from, ITypeInfo to)
    {
        var inParam = new TestParameter(from.AsType(), "toCast");

        const string castResName = "castRes";
        /*
         * T castRes
         * castRes = (T)arg
         * return castRes
         */
        var methodBody = new BodyNode(
        [
            new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(to), new VariableNameNode(castResName)),
            new AssignNode([new MemberNode(castResName)], [EmissionSetupHelper.CreateCastNode(from, to, new MemberNode(inParam.Name))]),
            new ReturnNode(new MemberNode(castResName))
        ]);
        var (instance, methodInfo) = EmissionSetupHelper.CreateInstanceWithMethod([inParam], methodBody, to.AsType());
        return (methodInfo, instance)!;
    }

    public static IEnumerable<object[]> CastEmissionDataProvider()
    {
        yield return [typeof(IExampleInterface), typeof(ExampleChild), new ExampleChild()];
        yield return [typeof(IExampleInterface), typeof(ExampleStruct), new ExampleStruct()];
        yield return [typeof(object), typeof(int), 322];
        yield return [typeof(object), typeof(ExampleChild), new ExampleChild()];
        yield return [typeof(object), typeof(ExampleParent), new ExampleParent()];
        yield return [typeof(ExampleParent), typeof(ExampleChild), new ExampleChild()];
        yield return [typeof(long), typeof(int), 13];
        yield return [typeof(int), typeof(long), 91];
        yield return [typeof(object), typeof(string), "cab"];
        yield return [typeof(float), typeof(double), 1.32f];
        yield return [typeof(double), typeof(float), 1.32D];
        yield return [typeof(double), typeof(int), 1.32D];
        yield return [typeof(int), typeof(double), 1];
        yield return [typeof(int), typeof(short), 222];
        yield return [typeof(int), typeof(byte), 112];
        yield return [typeof(int), typeof(int), 22];
        yield return [typeof(long), typeof(ulong), 41412];
        yield return [typeof(int), typeof(char), 64];
        yield return [typeof(char), typeof(int), 's'];

        //In runtime there is no difference between child instance and parent type
        // yield return [typeof(int), typeof(object), 233];
        // yield return [typeof(ExampleChild), typeof(ExampleParent), new ExampleChild()];
        // yield return [typeof(ExampleChild), typeof(object), new ExampleChild()];
        // yield return [typeof(ExampleChild), typeof(IExampleInterface), new ExampleChild()];
        // yield return [typeof(ExampleParent), typeof(object), new ExampleParent()];
        // yield return [typeof(ExampleStruct), typeof(IExampleInterface), new ExampleStruct()];
        // yield return [typeof(string), typeof(object), "abc"];
    }

    [Fact]
    public void LiteralCast_Correct()
    {
        /*
         * return (float)43;
         */
        var to = new TypeNode(new TypeNameNode("float"))
        {
            TypeInfo = Builtins.Float
        };
        
        const int literal = 43;
        var cast = new CastNode(to, new LiteralNode(literal, Builtins.Int))
        {
            FromType = Builtins.Int
        };

        var methodBody = new BodyNode(
        [
            new ReturnNode(cast)
        ]);
        var (instance, methodInfo) = EmissionSetupHelper.CreateInstanceWithMethod([], methodBody, typeof(float));
        var res = methodInfo!.Invoke(instance, []);
        Assert.NotNull(res);
        Assert.Equal(typeof(float), res.GetType());
        Assert.Equal((float)literal, res);
    }

    public static int Example() => 14;

    [Fact]
    public void MethodCallCast_Correct()
    {
        /*
         * return (double)Example();
         */
        var to = new TypeNode(new TypeNameNode("double"))
        {
            TypeInfo = Builtins.Double
        };
        var call = new CallNode(null, new FuncCallNameNode("Example"), [], []);
        var info = typeof(CastEmissionTests).GetMethod(nameof(Example), BindingFlags.Static | BindingFlags.Public)!;
        call.FnInfo = EmissionSetupHelper.MakeFuncRef(info);
        var cast = new CastNode(to, call)
        {
            FromType = Builtins.Int
        };

        var methodBody = new BodyNode(
        [
            new ReturnNode(cast)
        ]);
        var (instance, methodInfo) = EmissionSetupHelper.CreateInstanceWithMethod([], methodBody, typeof(double));
        var res = methodInfo!.Invoke(instance, []);
        Assert.NotNull(res);
        Assert.Equal(typeof(double), res.GetType());
        Assert.Equal((double)14, res);
    }

    [Fact]
    public void BinaryCast_Correct()
    {
        /*
         * return (short)(32768 - 1);
         */
        var to = new TypeNode(new TypeNameNode("short"))
        {
            TypeInfo = Builtins.Short
        };
        var cast = new CastNode(to, new SubNode(new LiteralNode(32768, Builtins.Int), new LiteralNode(1, Builtins.Int)))
        {
            FromType = Builtins.Int
        };

        var methodBody = new BodyNode(
        [
            new ReturnNode(cast)
        ]);
        var (instance, methodInfo) = EmissionSetupHelper.CreateInstanceWithMethod([], methodBody, typeof(short));
        var res = methodInfo!.Invoke(instance, []);
        Assert.NotNull(res);
        Assert.Equal(typeof(short), res.GetType());
        Assert.Equal((short)(32768 - 1), res);
    }

    [Fact]
    public void UnaryCast_Correct()
    {
        /*
         * return (int)-1.5;
         */
        var to = new TypeNode(new TypeNameNode("int"))
        {
            TypeInfo = Builtins.Int
        };
        var cast = new CastNode(to, new UnaryMinusNode(new LiteralNode(1.5f, Builtins.Float)))
        {
            FromType = Builtins.Float
        };

        var methodBody = new BodyNode(
        [
            new ReturnNode(cast)
        ]);
        var (instance, methodInfo) = EmissionSetupHelper.CreateInstanceWithMethod([], methodBody, typeof(int));
        var res = methodInfo!.Invoke(instance, []);
        Assert.NotNull(res);
        Assert.Equal(typeof(int), res.GetType());
        Assert.Equal((int)-1.5f, res);
    }

    [Fact]
    public void CastCast_Correct()
    {
        /*
         * return (int)1.5;
         */
        var innerType = new TypeNode(new TypeNameNode("double"))
        {
            TypeInfo = Builtins.Double
        };
        var innerCast = new CastNode(innerType, new LiteralNode(1.5f, Builtins.Float))
        {
            FromType = Builtins.Float
        };
        var to = new TypeNode(new TypeNameNode("int"))
        {
            TypeInfo = Builtins.Int
        };
        var cast = new CastNode(to, innerCast)
        {
            FromType = Builtins.Double
        };

        var methodBody = new BodyNode(
        [
            new ReturnNode(cast)
        ]);
        var (instance, methodInfo) = EmissionSetupHelper.CreateInstanceWithMethod([], methodBody, typeof(int));
        var res = methodInfo!.Invoke(instance, []);
        Assert.NotNull(res);
        Assert.Equal(typeof(int), res.GetType());
        Assert.Equal((int)(double)1.5f, res);
    }

    [Fact]
    public void CastArrayGetter_Correct()
    {
        /*
         * a := [5]double;
         * return int(a[1]);
         */
        var variableType = new TypeNode(new TypeNameNode("double[]"))
        {
            TypeInfo = Builtins.Double.MakeArrayType()
        };
        var arrayItemType = new TypeNode(new TypeNameNode("double"))
        {
            TypeInfo = Builtins.Double
        };

        var castToType = new TypeNode(new TypeNameNode("int"))
        {
            TypeInfo = Builtins.Int
        };

        var elemGetter = new IndexerNode(new MemberNode("a"), new LiteralNode(1, Builtins.Int))
        {
            ItemType = Builtins.Double
        };

        var castNode = new CastNode(castToType, elemGetter)
        {
            FromType = Builtins.Double
        };

        var body = new BodyNode(
        [
            new AssignNode(
                [new VariableDefinitionNode(variableType, new VariableNameNode("a"))], 
                [new InitArrayNode(arrayItemType, new LiteralNode(5, Builtins.Int))]
            ),
            new ReturnNode(castNode)
        ]);
        
        var (instance, methodInfo) = EmissionSetupHelper.CreateInstanceWithMethod([], body, typeof(int));
        var res = methodInfo!.Invoke(instance, []);
        Assert.NotNull(res);
        Assert.Equal(typeof(int), res.GetType());
        Assert.Equal(0, res);
    }
}