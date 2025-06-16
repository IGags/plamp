using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.CompilerEmission;
using plamp.CodeEmission.Tests.Infrastructure;
using plamp.ILCodeEmitters;

namespace plamp.CodeEmission.Tests;

public class CtorEmissionTests
{
    public interface ITestData
    {
        public int IntProp { get; }
        
        public string StringProp { get; }
        
        public KeyValuePair<int, int> PairProp { get; }
    }
    
    public class CtorClass : ITestData
    {
        public int IntProp { get; }
        
        public string StringProp { get; }
        
        public KeyValuePair<int, int> PairProp { get; }
        
        public CtorClass() { }

        public CtorClass(int intProp, string stringProp, KeyValuePair<int, int> pairProp)
        {
            IntProp = intProp;
            StringProp = stringProp;
            PairProp = pairProp;
        }
    }
    
    public struct CtorStruct : ITestData
    {
        public int IntProp { get; }
        
        public string StringProp { get; }
        
        public KeyValuePair<int, int> PairProp { get; }
        
        public CtorStruct() { }

        public CtorStruct(int intProp, string stringProp, KeyValuePair<int, int> pairProp)
        {
            IntProp = intProp;
            StringProp = stringProp;
            PairProp = pairProp;
        }
    }
    
    [Theory]
    [MemberData(nameof(EmptyCtorDataProvider))]
    public async Task EmitCallClassCtor(Type objectType)
    {
        const string methodName = "Test";
        var (_, typeBuilder, methodBuilder, _) = EmissionSetupHelper.CreateMethodBuilder(methodName, objectType, []);
        var ctorInfo = objectType.GetConstructor([])!;
        
        /*
         * var tempVar
         * tempVar = new CtorClass()
         * return tempVar
         */
        var tempVarName = "tempVar";
        var body = new BodyNode(
        [
            new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(objectType), new MemberNode(tempVarName)),
            new AssignNode(
                new MemberNode(tempVarName), 
                EmissionSetupHelper.CreateConstructorNode(
                    EmissionSetupHelper.CreateTypeNode(objectType), [], ctorInfo)),
            new ReturnNode(new MemberNode(tempVarName))
        ]);

        var emissionContext = new CompilerEmissionContext(body, methodBuilder, [], null, null);
        var emitter = new DefaultIlCodeEmitter();
        await emitter.EmitMethodBodyAsync(emissionContext, CancellationToken.None);
        var type = typeBuilder.CreateType();
        var (instance, method) = EmissionSetupHelper.CreateObject(type, methodName);

        var res = method!.Invoke(instance, []);
        Assert.IsType(objectType, res);
    }
    
    public static IEnumerable<object[]> EmptyCtorDataProvider()
    {
        yield return [typeof(CtorClass)];
        yield return [typeof(CtorStruct)];
    }

    [Theory]
    [MemberData(nameof(CtorWithArgDataProvider))]
    public async Task EmitCallClassArgCtor(Type objectType)
    {
        const string methodName = "Test";
        var argType1 = typeof(int);
        var argType2 = typeof(string);
        var argType3 = typeof(KeyValuePair<int, int>);
        
        var (_, typeBuilder, methodBuilder, _) = EmissionSetupHelper.CreateMethodBuilder(methodName, objectType, [argType1, argType2, argType3]);
        var ctorInfo = objectType.GetConstructor([argType1, argType2, argType3])!;
        var arg1 = new TestParameter(argType1, "arg1");
        var arg2 = new TestParameter(argType2, "arg2");
        var arg3 = new TestParameter(argType3, "arg3");
        
        var tempVarName = "tempVar";
        /*
         * var tempVar
         * tempVar = new CtorClass(arg1, arg2, arg3)
         * return tempVar
         */
        var body = new BodyNode(
        [
            new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(objectType), new MemberNode(tempVarName)),
            new AssignNode(
                new MemberNode(tempVarName), 
                EmissionSetupHelper.CreateConstructorNode(
                    EmissionSetupHelper.CreateTypeNode(objectType), 
                    [
                        new MemberNode(arg1.Name),
                        new MemberNode(arg2.Name),
                        new MemberNode(arg3.Name)
                    ],
                    ctorInfo)),
            new ReturnNode(new MemberNode(tempVarName))
        ]);

        var context = new CompilerEmissionContext(body, methodBuilder, [arg1, arg2, arg3], null, null);
        var emitter = new DefaultIlCodeEmitter();
        await emitter.EmitMethodBodyAsync(context, CancellationToken.None);
        var type = typeBuilder.CreateType();
        var (instance, method) = EmissionSetupHelper.CreateObject(type, methodName);

        var arg1Val = 321;
        var arg2Val = "hiiii";
        var arg3Val = new KeyValuePair<int, int>(96, 69);
        
        var res = method!.Invoke(instance, [arg1Val, arg2Val, arg3Val])!;
        Assert.IsType(objectType, res);
        var cls = (ITestData)res;
        Assert.Equal(arg1Val, cls.IntProp);
        Assert.Equal(arg2Val, cls.StringProp);
        Assert.Equal(arg3Val, cls.PairProp);
    }

    public static IEnumerable<object[]> CtorWithArgDataProvider()
    {
        yield return [typeof(CtorClass)];
        yield return [typeof(CtorStruct)];
    }
}