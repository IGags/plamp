using System.Reflection;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Binary;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ComplexTypes;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.Ast.Node.Definitions.Variable;
using plamp.Abstractions.CompilerEmission;
using plamp.CodeEmission.Tests.Infrastructure;
using plamp.ILCodeEmitters;
using Shouldly;

// ReSharper disable EntityNameCapturedOnly.Local

namespace plamp.CodeEmission.Tests;

/// <summary>
/// Emit call and same stuff
/// </summary>
public class MethodCallTests
{
    public class CallbackClass
    {
        private bool _callbackResult;

        public bool CallbackResult => _callbackResult;
        
        public void TriggerCallback() => _callbackResult = true;
    }
    
    [Fact]
    public async Task EmitActionCall()
    {
        var method = typeof(CallbackClass).GetMethod(nameof(CallbackClass.TriggerCallback))!;
        var callbackArg = new TestParameter(typeof(CallbackClass), "callbackInstance");
        /*
         * callbackInstance.TriggerCallback()
         * return
         */
        var body = new BodyNode(
        [
            EmissionSetupHelper.CreateCallNode(new MemberNode(callbackArg.Name), method, []),
            new ReturnNode(null)
        ]);
        var (instance, methodInfo) =
            await EmissionSetupHelper.CreateInstanceWithMethodAsync([callbackArg], body, typeof(void));
        
        var callbackInstance = new CallbackClass();
        methodInfo!.Invoke(instance, [callbackInstance]);
        Assert.True(callbackInstance.CallbackResult);
    }
    
    public static class StaticCallbackClass
    {
        private static bool _callbackResult;
        
        public static bool CallbackResult => _callbackResult;
        
        public static void TriggerCallback() => _callbackResult = true;
        
        public static void Reset() => _callbackResult = false;
    }

    [Fact]
    public async Task EmitStaticActionCall()
    {
        var method = typeof(StaticCallbackClass).GetMethod(nameof(StaticCallbackClass.TriggerCallback))!;
        /*
         * StaticCallbackClass.TriggerCallback()
         * return
         */
        var body = new BodyNode(
        [
            EmissionSetupHelper.CreateCallNode(null, method, []),
            new ReturnNode(null)
        ]);

        var (instance, methodInfo) = await EmissionSetupHelper.CreateInstanceWithMethodAsync([], body, typeof(void));
        StaticCallbackClass.Reset();
        methodInfo!.Invoke(instance, []);
        Assert.True(StaticCallbackClass.CallbackResult);
    }

    public class CallbackClassWithArg
    {
        private string _arg1Value = "";
        private int _arg2Value;
        private KeyValuePair<int, int> _arg3Value;
        
        public string Arg1Value => _arg1Value;
        public int Arg2Value => _arg2Value;
        public KeyValuePair<int, int>  Arg3Value => _arg3Value;
        
        public void TriggerCallback(string arg1Value, int arg2Value, KeyValuePair<int, int> arg3Value)
        {
            _arg1Value = arg1Value;
            _arg2Value = arg2Value;
            _arg3Value = arg3Value;
        }
    }
    
    [Fact]
    public async Task EmitActionCallWithArgs()
    {
        var instanceArg = new TestParameter(typeof(CallbackClassWithArg), "callbackInstance");
        var callback1Arg = new TestParameter(typeof(string), "callbackArg1");
        var callback2Arg = new TestParameter(typeof(int), "callbackArg2");
        var callback3Arg = new TestParameter(typeof(KeyValuePair<int, int>), "callbackArg3");
        var info = typeof(CallbackClassWithArg).GetMethod(nameof(CallbackClassWithArg.TriggerCallback))!;
        
        /*
         * callbackInstance.TriggerCallback(callbackArg)
         * return
         */
        var body = new BodyNode(
        [
            EmissionSetupHelper.CreateCallNode(
                new MemberNode(instanceArg.Name), 
                info, 
                [
                    new MemberNode(callback1Arg.Name),
                    new MemberNode(callback2Arg.Name),
                    new MemberNode(callback3Arg.Name)
                ]),
            new ReturnNode(null)
        ]);
        var (instance, methodInfo) =
            await EmissionSetupHelper.CreateInstanceWithMethodAsync(
                [instanceArg, callback1Arg, callback2Arg, callback3Arg], body, typeof(void));

        var callbackInstance = new CallbackClassWithArg();
        var arg1 = "413";
        var arg2 = 233;
        var arg3 = new KeyValuePair<int, int>(42, 43);
        
        methodInfo!.Invoke(instance, [callbackInstance, arg1, arg2, arg3]);
        Assert.Equal(arg1, callbackInstance.Arg1Value);
        Assert.Equal(arg2, callbackInstance.Arg2Value);
        Assert.Equal(arg3, callbackInstance.Arg3Value);
    }
    
    public static class StaticCallbackClassWithArg
    {
        private static string? _arg1Value;
        private static int _arg2Value;
        private static KeyValuePair<int, int> _arg3Value;
        
        public static string? Arg1Value => _arg1Value;
        public static int Arg2Value => _arg2Value;
        public static KeyValuePair<int, int>  Arg3Value => _arg3Value;
        
        public static void TriggerCallback(string arg1Value, int arg2Value, KeyValuePair<int, int> arg3Value)
        {
            _arg1Value = arg1Value;
            _arg2Value = arg2Value;
            _arg3Value = arg3Value;
        }
        
        public static void Reset()
        {
            _arg1Value = null;
            _arg2Value = 0;
            _arg3Value = default;
        }
    }
    
    [Fact]
    public async Task EmitStaticActionCallWithArgs()
    {
        var callback1Arg = new TestParameter(typeof(string), "callbackArg1");
        var callback2Arg = new TestParameter(typeof(int), "callbackArg2");
        var callback3Arg = new TestParameter(typeof(KeyValuePair<int, int>), "callbackArg3");
        var info = typeof(StaticCallbackClassWithArg).GetMethod(nameof(StaticCallbackClassWithArg.TriggerCallback))!;
        
        /*
         * StaticCallbackClassWithArg.TriggerCallback(callbackArg)
         * return
         */
        var body = new BodyNode(
        [
            EmissionSetupHelper.CreateCallNode(
                null,
                info, 
                [
                    new MemberNode(callback1Arg.Name),
                    new MemberNode(callback2Arg.Name),
                    new MemberNode(callback3Arg.Name)
                ]),
            new ReturnNode(null)
        ]);
        var (instance, methodInfo) =
            await EmissionSetupHelper.CreateInstanceWithMethodAsync(
                [callback1Arg, callback2Arg, callback3Arg], 
                body,
                typeof(void));

        var arg1 = "413";
        var arg2 = 233;
        var arg3 = new KeyValuePair<int, int>(42, 43);
        StaticCallbackClassWithArg.Reset();
        
        methodInfo!.Invoke(instance, [arg1, arg2, arg3]);
        Assert.Equal(arg1, StaticCallbackClassWithArg.Arg1Value);
        Assert.Equal(arg2, StaticCallbackClassWithArg.Arg2Value);
        Assert.Equal(arg3, StaticCallbackClassWithArg.Arg3Value);
    }
    
    public class CallbackFuncClass
    {
        public object? ReturnValue { private get; init; }

        public object? TriggerCallback() => ReturnValue;
    }
    
    [Fact]
    public async Task EmitCallFunc()
    {
        var callbackArg = new TestParameter(typeof(CallbackFuncClass), "callbackInstance");
        var info = typeof(CallbackFuncClass).GetMethod(nameof(CallbackFuncClass.TriggerCallback))!;
        
        /*
         * var temp
         * temp = callbackInstance.TriggerCallback()
         * return temp
         */
        var tempVarName = "temp";
        var returnType = typeof(object);
        var body = new BodyNode(
        [
            new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(returnType), new VariableNameNode(tempVarName)),
            new AssignNode(new MemberNode(tempVarName), EmissionSetupHelper.CreateCallNode(new MemberNode(callbackArg.Name), info, [])),
            new ReturnNode(new MemberNode(tempVarName))
        ]);
        var (instance, methodInfo) =
            await EmissionSetupHelper.CreateInstanceWithMethodAsync([callbackArg], body, returnType);
        
        var expectedValue = new object();
        var callbackInstance = new CallbackFuncClass
        {
            ReturnValue  = expectedValue
        };
        
        var res = methodInfo!.Invoke(instance, [callbackInstance])!;
        Assert.Equal(expectedValue, res);
    }
    
    public static class StaticCallbackFuncClass
    {
        public static object? ReturnValue { private get; set; }

        public static object? TriggerCallback() => ReturnValue;
    }
    
    [Fact]
    public async Task EmitCallStaticFunc()
    {
        var info = typeof(StaticCallbackFuncClass).GetMethod(nameof(StaticCallbackFuncClass.TriggerCallback))!;
        /*
         * var temp
         * temp = StaticCallbackFuncClass.TriggerCallback()
         * return temp
         */
        var tempVarName = "temp";
        var retType = typeof(object);
        var body = new BodyNode(
        [
            new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(retType), new VariableNameNode(tempVarName)),
            new AssignNode(new MemberNode(tempVarName), EmissionSetupHelper.CreateCallNode(null, info, [])),
            new ReturnNode(new MemberNode(tempVarName))
        ]);
        var (instance, methodInfo) = await EmissionSetupHelper.CreateInstanceWithMethodAsync([], body, retType);
        StaticCallbackFuncArgClass.Reset();
        var expectedValue = new object();
        StaticCallbackFuncClass.ReturnValue = expectedValue;
        
        var res = methodInfo!.Invoke(instance, [])!;
        Assert.Equal(expectedValue, res);
    }
    
    public class CallbackFuncArgClass
    {
        public KeyValuePair<char, char> ReturnValue { private get; set; }

        public string? StringValue { get; private set; }
        
        public char CharValue { get; private set; }
        
        public ValueTask TaskValue { get; private set; }
        
        public KeyValuePair<char, char> TriggerCallback(string str, char ch, ValueTask tsk)
        {
            StringValue = str;
            CharValue = ch;
            TaskValue = tsk;
            return ReturnValue;
        }
    }
    
    [Fact]
    public async Task EmitCallFuncWithArgs()
    {
        var instanceArg = new TestParameter(typeof(CallbackFuncArgClass), "callbackInstance");
        var callback1Arg = new TestParameter(typeof(string), "callbackArg1");
        var callback2Arg = new TestParameter(typeof(char), "callbackArg2");
        var callback3Arg = new TestParameter(typeof(ValueTask), "callbackArg3");
        var info = typeof(CallbackFuncArgClass).GetMethod(nameof(CallbackFuncArgClass.TriggerCallback))!;
        var retType = typeof(KeyValuePair<char, char>);
        
        /*
         * var temp
         * temp = callbackInstance.TriggerCallback(callbackArg1, callbackArg2, callbackArg3)
         * return temp
         */
        var tempVarName = "temp";
        var body = new BodyNode(
        [
            new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(retType), new VariableNameNode(tempVarName)),
            new AssignNode(
                new MemberNode(tempVarName),
                EmissionSetupHelper.CreateCallNode(
                    new MemberNode(instanceArg.Name), 
                    info, 
                    [
                        new MemberNode(callback1Arg.Name),
                        new MemberNode(callback2Arg.Name),
                        new MemberNode(callback3Arg.Name)
                    ])
            ),
            new ReturnNode(new MemberNode(tempVarName))
        ]);
        var (instance, methodInfo) =
            await EmissionSetupHelper.CreateInstanceWithMethodAsync(
                [instanceArg, callback1Arg, callback2Arg, callback3Arg], body, retType);

        var callbackInstance = new CallbackFuncArgClass();
        var arg1 = "413";
        var arg2 = 's';
        var arg3 = ValueTask.CompletedTask;
        var expectedReturn = new KeyValuePair<char, char>('a', 'z');
        callbackInstance.ReturnValue = expectedReturn;
        
        var res = methodInfo!.Invoke(instance, [callbackInstance, arg1, arg2, arg3])!;
        Assert.Equal(arg1, callbackInstance.StringValue);
        Assert.Equal(arg2, callbackInstance.CharValue);
        Assert.Equal(arg3, callbackInstance.TaskValue);
        Assert.Equal(expectedReturn, res);
    }
    
    public static class StaticCallbackFuncArgClass
    {
        public static KeyValuePair<char, char> ReturnValue { private get; set; }

        public static string? StringValue { get; private set; }
        
        public static char CharValue { get; private set; }
        
        public static ValueTask TaskValue { get; private set; }
        
        public static KeyValuePair<char, char> TriggerCallback(string str, char ch, ValueTask tsk)
        {
            StringValue = str;
            CharValue = ch;
            TaskValue = tsk;
            return ReturnValue;
        }

        public static void Reset()
        {
            StringValue = null;
            CharValue = 'a';
            TaskValue = default;
            ReturnValue = default;
        }
    }
    
    [Fact]
    public async Task EmitCallStaticFuncWithArgs()
    {
        var retType = typeof(KeyValuePair<char, char>);
        var callback1Arg = new TestParameter(typeof(string), "callbackArg1");
        var callback2Arg = new TestParameter(typeof(char), "callbackArg2");
        var callback3Arg = new TestParameter(typeof(ValueTask), "callbackArg3");
        var info = typeof(StaticCallbackFuncArgClass).GetMethod(nameof(StaticCallbackFuncArgClass.TriggerCallback))!;
        
        /*
         * var temp
         * temp = callbackInstance.TriggerCallback(callbackArg1, callbackArg2, callbackArg3)
         * return temp
         */
        var tempVarName = "temp";
        var body = new BodyNode(
        [
            new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(retType), new VariableNameNode(tempVarName)),
            new AssignNode(
                new MemberNode(tempVarName),
                EmissionSetupHelper.CreateCallNode(
                    null, 
                    info, 
                    [
                        new MemberNode(callback1Arg.Name),
                        new MemberNode(callback2Arg.Name),
                        new MemberNode(callback3Arg.Name)
                    ])
            ),
            new ReturnNode(new MemberNode(tempVarName))
        ]);
        var (instance, methodInfo) =
            await EmissionSetupHelper.CreateInstanceWithMethodAsync(
                [callback1Arg, callback2Arg, callback3Arg], 
                body,
                retType);

        StaticCallbackFuncArgClass.Reset();
        var arg1 = "413";
        var arg2 = 's';
        var arg3 = ValueTask.CompletedTask;
        var expectedReturn = new KeyValuePair<char, char>('a', 'z');
        StaticCallbackFuncArgClass.ReturnValue = expectedReturn;
        
        var res = methodInfo!.Invoke(instance, [arg1, arg2, arg3])!;
        Assert.Equal(arg1, StaticCallbackFuncArgClass.StringValue);
        Assert.Equal(arg2, StaticCallbackFuncArgClass.CharValue);
        Assert.Equal(arg3, StaticCallbackFuncArgClass.TaskValue);
        Assert.Equal(expectedReturn, res);
    }
    
    public struct CallbackStruct
    {
        private bool _callbackResult;
        
        public bool CallbackResult => _callbackResult;
        
        public void TriggerCallback()
        {
            _callbackResult = true;
        }
    }
    
    [Fact]
    public async Task EmitStructActionCall()
    {
        var retType = typeof(CallbackStruct);
        var method = typeof(CallbackStruct).GetMethod(nameof(CallbackStruct.TriggerCallback))!;
        var callbackArg = new TestParameter(typeof(CallbackStruct), "callbackInstance");
        
        
        /*
         * callbackInstance.TriggerCallback()
         * return callbackInstance
         */
        var body = new BodyNode(
        [
            EmissionSetupHelper.CreateCallNode(new MemberNode(callbackArg.Name), method, []),
            new ReturnNode(new MemberNode(callbackArg.Name))
        ]);
        var (instance, methodInfo) =
            await EmissionSetupHelper.CreateInstanceWithMethodAsync([callbackArg], body, retType);
        
        var callbackInstance = new CallbackStruct();
        var res = (CallbackStruct)methodInfo!.Invoke(instance, [callbackInstance])!;
        Assert.True(res.CallbackResult);
    }
    
    //fib :^)
    [Fact]
    public async Task EmitRecursiveCall()
    {
        const string methodName = "Fibonacci";
        var retType = typeof(int);
        var argType = typeof(int);
        var(_, typeBuilder, methodBuilder, _) = EmissionSetupHelper.CreateMethodBuilder(methodName, retType, [argType]);
        
        /*
         * int zero
         * zero = 0
         * int one
         * one = 1
         * int two
         * two = 2
         * 
         * bool eq0
         * eq0 = n == zero
         * bool eq1
         * eq1 = n == one
         * if(eq0)
         *     return zero
         * if(eq1)
         *     return one
         *
         * int nm1
         * nm1 = n - one
         * nm1 = Fibonacci(nm1) 
         * int nm2
         * nm2 = n - two
         * nm2 = Fibonacci(nm2)
         *
         * int sum
         * sum = nm1 + nm2
         * return sum
         */
        bool zero, one, two, eq0, eq1, nm1, nm2, sum;
        var arg = new TestParameter(argType, "n");
        var body = new BodyNode(
        [
            new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(typeof(int)), new VariableNameNode(nameof(zero))),
            new AssignNode(new MemberNode(nameof(zero)), new LiteralNode(0, typeof(int))),
            
            new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(typeof(int)), new VariableNameNode(nameof(one))),
            new AssignNode(new MemberNode(nameof(one)), new LiteralNode(1, typeof(int))),
            
            new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(typeof(int)), new VariableNameNode(nameof(two))),
            new AssignNode(new MemberNode(nameof(two)), new LiteralNode(2, typeof(int))),
            
            new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(typeof(int)), new VariableNameNode(nameof(eq0))),
            new AssignNode(new MemberNode(nameof(eq0)), new EqualNode(new MemberNode(arg.Name), new MemberNode(nameof(zero)))),
            
            new ConditionNode(
                    new MemberNode(nameof(eq0)), 
                    new BodyNode(
                    [
                        new ReturnNode(new MemberNode(nameof(zero)))
                    ]), null),
            
            new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(typeof(int)), new VariableNameNode(nameof(eq1))),
            new AssignNode(new MemberNode(nameof(eq1)), new EqualNode(new MemberNode(arg.Name), new MemberNode(nameof(one)))),
            
            new ConditionNode(
                    new MemberNode(nameof(eq1)), 
                    new BodyNode(
                    [
                        new ReturnNode(new MemberNode(nameof(one)))
                    ]), null),
            
            new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(typeof(int)), new VariableNameNode(nameof(nm1))),
            new AssignNode(new MemberNode(nameof(nm1)), new SubNode(new MemberNode(arg.Name), new MemberNode(nameof(one)))),
            new AssignNode(new MemberNode(nameof(nm1)), EmissionSetupHelper.CreateCallNode(new ThisNode(), methodBuilder.GetInner(), [new MemberNode(nameof(nm1))])),
            
            new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(typeof(int)), new VariableNameNode(nameof(nm2))),
            new AssignNode(new MemberNode(nameof(nm2)), new SubNode(new MemberNode(arg.Name), new MemberNode(nameof(two)))),
            new AssignNode(new MemberNode(nameof(nm2)), EmissionSetupHelper.CreateCallNode(new ThisNode(), methodBuilder.GetInner(), [new MemberNode(nameof(nm2))])),
            
            new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(typeof(int)), new VariableNameNode(nameof(sum))),
            new AssignNode(new MemberNode(nameof(sum)), new AddNode(new MemberNode(nameof(nm1)), new MemberNode (nameof(nm2)))),
            new ReturnNode(new MemberNode(nameof(sum)))
        ]);
        
        var context = new CompilerEmissionContext(body, methodBuilder, [arg], null);
        var emitter = new DefaultIlCodeEmitter();
        await emitter.EmitMethodBodyAsync(context, CancellationToken.None);
        var type = typeBuilder.CreateType();
        var (instance, methodInfo) = EmissionSetupHelper.CreateObject(type, methodName);
        
        for (var i = 0; i < 10; i++)
        {
            var should = Fibonacci(i);
            var actual = (int)methodInfo!.Invoke(instance, [i])!;
            Assert.Equal(should, actual);
        }
        
        //Break when n < 0
        static int Fibonacci(int n)
        {
            if (n == 0) return 0;
            if (n == 1) return 1;
            return Fibonacci(n - 1) + Fibonacci(n - 2);
        }
    }

    [Fact]
    public async Task EmitCallOtherDynamicInSameType()
    {
        const string methodName = "Greet";
        var returnType = typeof(string);
        var (_, typeBuilder, greetBuilder, _) = EmissionSetupHelper.CreateMethodBuilder(methodName, returnType, []);
        const string otherName = "GreeterDep";

        var otherArgType = typeof(string);

        var otherBuilder = typeBuilder.DefineMethod(
            otherName,
            MethodAttributes.Final | MethodAttributes.Private,
            returnType,
            [otherArgType]
            );

        var otherDebugBuilder = new DebugMethodBuilder(otherBuilder);
            
        
        var otherArg = new TestParameter(otherArgType, "arg");
        bool temp1, temp2;
        /*
         * string temp1
         * string temp2
         * temp1 = "Hi"
         * temp2 = "Bye"
         * arg = arg.Replace(temp1, temp2)
         * return arg
         */
        var mth = typeof(string).GetMethod(
            nameof(string.Replace), 
            BindingFlags.Instance | BindingFlags.Public, 
            [typeof(string), typeof(string)])!;
        
        var otherBody = new BodyNode(
        [
            new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(typeof(string)),
                new VariableNameNode(nameof(temp1))),
            new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(typeof(string)),
                new VariableNameNode(nameof(temp2))),

            new AssignNode(new MemberNode(nameof(temp1)), new LiteralNode("Hi", typeof(string))),
            new AssignNode(new MemberNode(nameof(temp2)), new LiteralNode("Bye", typeof(string))),

            new AssignNode(new MemberNode(otherArg.Name),
                EmissionSetupHelper.CreateCallNode(new MemberNode(otherArg.Name), mth, [new MemberNode(nameof(temp1)), new MemberNode(nameof(temp2))])),
            new ReturnNode(new MemberNode(otherArg.Name))
        ]);

        /*
         * string temp1
         * temp1 = "Hi you're cool"
         * temp1 = this.GreetDep(temp1)
         * return temp1
         */
        const string literal = "Hi you're cool";
        var body = new BodyNode(
        [
            new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(typeof(string)), new VariableNameNode(nameof(temp1))),
            new AssignNode(new MemberNode(nameof(temp1)), new LiteralNode(literal, literal.GetType())),
            new AssignNode(new MemberNode(nameof(temp1)), EmissionSetupHelper.CreateCallNode(new ThisNode(), otherBuilder, [new MemberNode(nameof(temp1))])),
            new ReturnNode(new MemberNode(nameof(temp1)))
        ]);

        var otherContext = new CompilerEmissionContext(otherBody, otherDebugBuilder, [otherArg], null);
        var context = new CompilerEmissionContext(body, greetBuilder, [], null);

        var emitter = new DefaultIlCodeEmitter();
        await emitter.EmitMethodBodyAsync(otherContext, CancellationToken.None);
        await emitter.EmitMethodBodyAsync(context, CancellationToken.None);
        var type = typeBuilder.CreateType();

        var (instance, method) = EmissionSetupHelper.CreateObject(type, methodName);
        var res = method!.Invoke(instance!, []);
        var resShould = literal.Replace("Hi", "Bye");
        Assert.Equal(resShould, res);
    }

    public interface ILateBound
    {
        public int Count();
    }
    
    public class LateBoundClass : ILateBound
    {
        public int Count() => 1888;
    }
    
    public struct LateBoundStruct : ILateBound
    {
        public int Count() => 1889;
    }
    
    [Theory]
    [InlineData(typeof(LateBoundClass))]
    [InlineData(typeof(LateBoundStruct))]
    public async Task EmitLateBoundCall(Type instanceType)
    {
        var instanceArg = new TestParameter(typeof(ILateBound), "instance");

        bool temp1;
        /*
         * int temp1
         * temp1 = instance.Count()
         * return temp1
         */
        var mth = typeof(ILateBound).GetMethod(nameof(ILateBound.Count), BindingFlags.Instance | BindingFlags.Public)!;
        var body = new BodyNode(
        [
            new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(typeof(int)), new VariableNameNode(nameof(temp1))),
            new AssignNode(new MemberNode(nameof(temp1)), EmissionSetupHelper.CreateCallNode(new MemberNode(instanceArg.Name), mth, [])),
            new ReturnNode(new MemberNode(nameof(temp1)))
        ]);

        var (instance, method) =
            await EmissionSetupHelper.CreateInstanceWithMethodAsync([instanceArg], body, typeof(int));
        var argTypeInstance = Activator.CreateInstance(instanceType)!;
        var res = method!.Invoke(instance, [argTypeInstance]);
        var resShould = mth.Invoke(argTypeInstance, []);
        Assert.Equal(resShould, res);
    }

    [Fact]
    public async Task EmitCyclicCallBetween2DynamicTypes()
    {
        const string firstName = "M1";
        var argType = typeof(string);
        var (_, typeBuilder, methodBuilder, module) =
            EmissionSetupHelper.CreateMethodBuilder(
                firstName, typeof(string), [argType], MethodAttributes.Final | MethodAttributes.Static | MethodAttributes.Public);

        var arg = new TestParameter(argType, "from");

        var secondTypeName = $"{Guid.NewGuid()}_secondType";
        var typ2 = module.DefineType(secondTypeName, TypeAttributes.Class | TypeAttributes.Sealed);
        const string secondName = "M2";
        var method2Builder = typ2.DefineMethod(secondName,
            MethodAttributes.Final | MethodAttributes.Static | MethodAttributes.Public, CallingConventions.Standard,
            typeof(string), [argType]);
        var method2Dbg = new DebugMethodBuilder(method2Builder);

        var equality = typeof(string).GetMethod(
            "op_Equality", 
            BindingFlags.Public | BindingFlags.Static,
            [typeof(string), typeof(string)])!;
        
        bool temp1, temp2, temp3;
        /*
         * string temp1
         * bool temp2
         * temp1 = "M2"
         * temp2 = string.op_Equality(temp1, from)
         *
         * if(temp2)
         *     string temp3
         *     temp3 = "hi from M1, bro"
         *     return temp3
         *
         * temp1 = "M1"
         * temp1 = second_type.M2(temp1)
         * return temp1
         */
        const string m1Literal = "hi from M1, bro";
        var body = new BodyNode(
        [
            new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(typeof(string)), new VariableNameNode(nameof(temp1))),
            new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(typeof(bool)), new VariableNameNode(nameof(temp2))),
            new AssignNode(new MemberNode(nameof(temp1)), new LiteralNode(secondName, secondName.GetType())),
            new AssignNode(new MemberNode(nameof(temp2)), EmissionSetupHelper.CreateCallNode(null, equality, [new MemberNode(nameof(temp1)), new MemberNode(arg.Name)])),
            
            new ConditionNode(
                new MemberNode(nameof(temp2)),
                new BodyNode(
                    [
                        new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(typeof(string)), new VariableNameNode(nameof(temp3))),
                        new AssignNode(new MemberNode(nameof(temp3)), new LiteralNode(m1Literal, m1Literal.GetType())),
                        new ReturnNode(new MemberNode(nameof(temp3)))
                    ]),
                null),
            new AssignNode(new MemberNode(nameof(temp1)), new LiteralNode(firstName, firstName.GetType())),
            new AssignNode(new MemberNode(nameof(temp1)), EmissionSetupHelper.CreateCallNode(null, method2Builder, [new MemberNode(nameof(temp1))])),
            new ReturnNode(new MemberNode(nameof(temp1)))
        ]);
        
        //Opposite case
        const string m2Literal = "hi from M2, bro";
        var body2 = new BodyNode(
        [
            new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(typeof(string)), new VariableNameNode(nameof(temp1))),
            new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(typeof(bool)), new VariableNameNode(nameof(temp2))),
            new AssignNode(new MemberNode(nameof(temp1)), new LiteralNode(firstName, firstName.GetType())),
            new AssignNode(new MemberNode(nameof(temp2)), EmissionSetupHelper.CreateCallNode(null, equality, [new MemberNode(nameof(temp1)), new MemberNode(arg.Name)])),
            
            new ConditionNode(
                new MemberNode(nameof(temp2)),
                new BodyNode(
                [
                    new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(typeof(string)), new VariableNameNode(nameof(temp3))),
                    new AssignNode(new MemberNode(nameof(temp3)), new LiteralNode(m2Literal, m2Literal.GetType())),
                    new ReturnNode(new MemberNode(nameof(temp3)))
                ]),
                null),
            new AssignNode(new MemberNode(nameof(temp1)), new LiteralNode(secondName, secondName.GetType())),
            new AssignNode(new MemberNode(nameof(temp1)), EmissionSetupHelper.CreateCallNode(null, methodBuilder.GetInner(), [new MemberNode(nameof(temp1))])),
            new ReturnNode(new MemberNode(nameof(temp1)))
        ]);

        var context1 = new CompilerEmissionContext(body, methodBuilder, [arg], null);
        var context2 = new CompilerEmissionContext(body2, method2Dbg, [arg], null);
        var emitter = new DefaultIlCodeEmitter();
        await emitter.EmitMethodBodyAsync(context1, CancellationToken.None);
        await emitter.EmitMethodBodyAsync(context2, CancellationToken.None);

        var type1 = typeBuilder.CreateType();
        var type2 = typ2.CreateType();

        var (_, method1) = EmissionSetupHelper.CreateObject(type1, firstName);
        var (_, method2) = EmissionSetupHelper.CreateObject(type2, secondName);

        var emptyRes1 = method1!.Invoke(null, [null]);
        Assert.Equal(m2Literal, emptyRes1);
        var m2Res1 = method1.Invoke(null, [secondName]);
        Assert.Equal(m1Literal, m2Res1);
        
        var emptyRes2 = method2!.Invoke(null, [null]);
        Assert.Equal(m1Literal, emptyRes2);
        var m1Res2 = method2.Invoke(null, [firstName]);
        Assert.Equal(m2Literal, m1Res2);
    }

    [Fact]
    public async Task CallWithArrayElementGetter_ReturnsCorrect()
    {
        /*
         * fn call_getter([]int a, int ix) int { return callback(a[ix]); }
         */
        var arrayParam = new TestParameter(typeof(int[]), "a");
        var indexerParam = new TestParameter(typeof(int), "ix");

        var getter = new ElemGetterNode(new MemberNode("a"), new ArrayIndexerNode(new MemberNode("ix")));
        getter.SetItemType(typeof(int));

        var callNode = new CallNode(null, new FuncCallNameNode("square"), [getter]);
        callNode.SetInfo(typeof(MethodCallTests).GetMethod(nameof(Square), [typeof(int)])!);
        
        var body = new BodyNode([new ReturnNode(callNode)]);
        var (instance, method) =
            await EmissionSetupHelper.CreateInstanceWithMethodAsync(
                [arrayParam, indexerParam], 
                body, 
                typeof(int));

        var arr = new[] { 10 };
        var result = method!.Invoke(instance, [arr, 0]);
        result.ShouldBeOfType<int>().ShouldBe(Square(10));
    }

    public static int Square(int value) => value * value;
    
    
    public class NonVoidCallback
    {
        public bool Called { get; private set; }

        public int CallbackWithResult()
        {
            Called = true;
            return 1;
        }
    }
    
    [Fact]
    public async Task NonVoidCallWithoutAssign_ReturnsSuccess()
    {
        var call = new CallNode(new MemberNode("a"), new FuncCallNameNode(nameof(NonVoidCallback.CallbackWithResult)), []);
        call.SetInfo(typeof(NonVoidCallback).GetMethod(nameof(NonVoidCallback.CallbackWithResult))!);
        var ast = new BodyNode(
        [
           call,
           new ReturnNode(null)
        ]);
        var arg = new TestParameter(typeof(NonVoidCallback), "a");
        var (typeBuilder, methodBuilder) = await EmissionSetupHelper.CreateInstanceWithMethodAsync([arg], ast, typeof(void));
        var argInst = new NonVoidCallback();
        Should.NotThrow(() => methodBuilder!.Invoke(typeBuilder, [argInst]));
        argInst.Called.ShouldBe(true);
    }
}