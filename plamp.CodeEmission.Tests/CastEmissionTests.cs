using System.Reflection;
using plamp.Abstractions;
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
using plamp.CodeEmission.Tests.Infrastructure;
using plamp.Intrinsics;

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
            EmissionSetupHelper.CreateInstanceWithMethod([inParam], methodBody, toRef.GetDefinitionInfo().ClrType!);
        instance ??= Activator.CreateInstance(fromRef.GetDefinitionInfo().ClrType!);
        var res = methodInfo!.Invoke(typeInstance, [instance]);
        Assert.NotNull(res);
        Assert.Equal(toRef.GetDefinitionInfo().ClrType!, res.GetType());
    }

    //I can't store object of type in object typed variable
    [Fact]
    public void EmitShortIntConversion()
    {
        var to = RuntimeSymbols.SymbolTable.MakeInt();
        var from = RuntimeSymbols.SymbolTable.MakeShort();
        const short instance = 31;
        var (methodInfo, typeInstance) = CreateConversionDelegate(from, to);
        var res = methodInfo.Invoke(typeInstance, [instance]);
        Assert.NotNull(res);
        Assert.Equal(to.GetDefinitionInfo().ClrType!, res.GetType());
    }
    
    [Fact]
    public void EmitByteIntConversion()
    {
        var to = RuntimeSymbols.SymbolTable.MakeInt();
        var from = RuntimeSymbols.SymbolTable.MakeByte();
        const byte instance = 31;
        var (methodInfo, typeInstance) = CreateConversionDelegate(from, to);
        var res = methodInfo.Invoke(typeInstance, [instance]);
        Assert.NotNull(res);
        Assert.Equal(to.GetDefinitionInfo().ClrType!, res.GetType());
    }

    private (MethodInfo, object) CreateConversionDelegate(ICompileTimeType from, ICompileTimeType to)
    {
        var inParam = new TestParameter(from.GetDefinitionInfo().ClrType!, "toCast");

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
        var (instance, methodInfo) = EmissionSetupHelper.CreateInstanceWithMethod([inParam], methodBody, to.GetDefinitionInfo().ClrType!);
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
        var to = new TypeNode(new TypeNameNode("float"));
        to.SetTypeRef(RuntimeSymbols.SymbolTable.MakeFloat());
        const int literal = 43;
        var cast = new CastNode(to, new LiteralNode(literal, RuntimeSymbols.SymbolTable.MakeInt()));
        cast.SetFromType(RuntimeSymbols.SymbolTable.MakeInt());
        
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
        var to = new TypeNode(new TypeNameNode("double"));
        to.SetTypeRef(RuntimeSymbols.SymbolTable.MakeDouble());
        var call = new CallNode(null, new FuncCallNameNode("Example"), []);
        var info = typeof(CastEmissionTests).GetMethod(nameof(Example), BindingFlags.Static | BindingFlags.Public)!;
        call.SetInfo(EmissionSetupHelper.MakeFuncRef(info));
        var cast = new CastNode(to, call);
        cast.SetFromType(RuntimeSymbols.SymbolTable.MakeInt());
        
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
        var to = new TypeNode(new TypeNameNode("short"));
        to.SetTypeRef(RuntimeSymbols.SymbolTable.MakeShort());
        var cast = new CastNode(to, new SubNode(new LiteralNode(32768, RuntimeSymbols.SymbolTable.MakeInt()), new LiteralNode(1, RuntimeSymbols.SymbolTable.MakeInt())));
        cast.SetFromType(RuntimeSymbols.SymbolTable.MakeInt());
        
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
        var to = new TypeNode(new TypeNameNode("int"));
        to.SetTypeRef(RuntimeSymbols.SymbolTable.MakeInt());
        var cast = new CastNode(to, new UnaryMinusNode(new LiteralNode(1.5f, RuntimeSymbols.SymbolTable.MakeFloat())));
        cast.SetFromType(RuntimeSymbols.SymbolTable.MakeFloat());

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
        var innerType = new TypeNode(new TypeNameNode("double"));
        innerType.SetTypeRef(RuntimeSymbols.SymbolTable.MakeDouble());
        var innerCast = new CastNode(innerType, new LiteralNode(1.5f, RuntimeSymbols.SymbolTable.MakeFloat()));
        innerCast.SetFromType(RuntimeSymbols.SymbolTable.MakeFloat());
        var to = new TypeNode(new TypeNameNode("int"));
        to.SetTypeRef(RuntimeSymbols.SymbolTable.MakeInt());
        var cast = new CastNode(to, innerCast);
        cast.SetFromType(RuntimeSymbols.SymbolTable.MakeDouble());
        
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
        var variableType = new TypeNode(new TypeNameNode("double[]"));
        variableType.SetTypeRef(RuntimeSymbols.SymbolTable.MakeDouble().MakeArrayType());
        var arrayItemType = new TypeNode(new TypeNameNode("double"));
        arrayItemType.SetTypeRef(RuntimeSymbols.SymbolTable.MakeDouble());

        var castToType = new TypeNode(new TypeNameNode("int"));
        castToType.SetTypeRef(RuntimeSymbols.SymbolTable.MakeInt());
        
        var elemGetter = new IndexerNode(new MemberNode("a"), new LiteralNode(1, RuntimeSymbols.SymbolTable.MakeInt()));
        elemGetter.SetItemType(RuntimeSymbols.SymbolTable.MakeDouble());
        
        var castNode = new CastNode(castToType, elemGetter);
        castNode.SetFromType(RuntimeSymbols.SymbolTable.MakeDouble());
        
        var body = new BodyNode(
        [
            new AssignNode(
                [new VariableDefinitionNode(variableType, new VariableNameNode("a"))], 
                [new InitArrayNode(arrayItemType, new LiteralNode(5, RuntimeSymbols.SymbolTable.MakeInt()))]
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