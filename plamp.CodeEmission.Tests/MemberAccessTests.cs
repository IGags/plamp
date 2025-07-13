using System.Reflection;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.CodeEmission.Tests.Infrastructure;

namespace plamp.CodeEmission.Tests;

/// <summary>
/// Member access в c# лишь иллюзия
/// a.b = c => a.set_b(c)
/// c = a.b => c = a.get_b()
/// Поэтому компиллятор требует, чтобы ему передавали истинную форму
/// </summary>
public class MemberAccessTests
{
    public struct ExampleFldStruct(int x)
    {
        public int X = x;
    };

    public class ExampleFldClass(int x)
    {
        public int X = x;
    };

    public struct ExamplePropStruct(int x)
    {
        public int X { get; set; } = x;
    }

    public class ExamplePropClass(int x)
    {
        public int X { get; set; } = x;
    }
    
    /// <summary>
    /// Если аргумент - структура, то с ней следует работать по-особому.
    /// Значение со стека следует грузить по ссылке, так как обычная загрузка может сломать кадр стека(clr считает размер кадра)
    /// и положить рантайм
    /// </summary>
    [Theory]
    [MemberData(nameof(GetMemberDataProvider))]
    public async Task GetMember(object arg, MemberInfo member, Type returnType, object expectedVal)
    {
        var objParam = new TestParameter(arg.GetType(), "obj");
        const string tempVarName = "prop";

        NodeBase memberNode;
        switch (member)
        {
            case FieldInfo field:
                memberNode = new MemberAccessNode(
                    new MemberNode(objParam.Name),
                    EmissionSetupHelper.CreateMemberNode(field));
                break;
            case PropertyInfo property:
                var getter = property.GetGetMethod();
                memberNode = EmissionSetupHelper.CreateCallNode(new MemberNode(objParam.Name), getter!, []);
                break;
            default: throw new ArgumentException(nameof(member));
        }
        
        /*
         * string prop
         * prop = obj.Value
         * return prop
         */
        var body = new BodyNode(
        [
            new VariableDefinitionNode(
                EmissionSetupHelper.CreateTypeNode(objParam.ParameterType),
                new MemberNode(tempVarName)
                ),
            new AssignNode(
                new MemberNode(tempVarName),
                memberNode),
            new ReturnNode(new MemberNode(tempVarName))
        ]);
        var (instance, method) = await EmissionSetupHelper.CreateInstanceWithMethodAsync([objParam], body, returnType);
        var res = method!.Invoke(instance, [arg])!;
        Assert.Equal(expectedVal, res);
        Assert.Equal(returnType, res.GetType());
    }
    
    public static IEnumerable<object[]> GetMemberDataProvider()
    {
        var valueExpected = 13;
        yield return [new ExampleFldClass(valueExpected), typeof(ExampleFldClass).GetField(nameof(ExampleFldClass.X))!, 
            typeof(int), valueExpected];
        yield return [new ExampleFldStruct(valueExpected), typeof(ExampleFldStruct).GetField(nameof(ExampleFldStruct.X))!, 
            typeof(int), valueExpected];
        yield return [new ExamplePropClass(valueExpected), typeof(ExamplePropClass).GetProperty(nameof(ExamplePropClass.X))!, 
            typeof(int), valueExpected];
        yield return [new ExamplePropStruct(valueExpected), typeof(ExamplePropStruct).GetProperty(nameof(ExamplePropStruct.X))!, 
            typeof(int), valueExpected];
    }

    [Theory]
    [MemberData(nameof(SetMemberDataProvider))]
    public async Task SetMember(object arg, MemberInfo member, object memberVal)
    {
        var objParam = new TestParameter(arg.GetType(), "obj");
        var valParam = new TestParameter(memberVal.GetType(), "val");
        
        /*
         * obj.Member = val
         * return obj
         */
        NodeBase memberNode;
        switch (member)
        {
            case FieldInfo fld:
                memberNode = new AssignNode(
                    new MemberAccessNode(
                        new MemberNode(objParam.Name),
                        EmissionSetupHelper.CreateMemberNode(fld)
                        ),
                    new MemberNode(valParam.Name)
                    );
                break;
            case PropertyInfo prop:
                memberNode = EmissionSetupHelper.CreateCallNode(
                    new MemberNode(objParam.Name), prop.GetSetMethod()!, [new MemberNode(valParam.Name)]);
                break;
            default: throw new ArgumentException(nameof(member));
        }
        
        var body = new BodyNode([
            memberNode,
            new ReturnNode(new MemberNode(objParam.Name))
        ]);

        var (instance, method) =
            await EmissionSetupHelper.CreateInstanceWithMethodAsync([objParam, valParam], body, arg.GetType());
        
        var res = method!.Invoke(instance, [arg, memberVal])!;
        Assert.Equal(arg.GetType(), res.GetType());

        object val;
        switch (member)
        {
            case PropertyInfo prop:
                var getter = prop.GetGetMethod()!;
                val = getter.Invoke(res, [])!;
                break;
            case FieldInfo fld:
                val = fld.GetValue(res)!;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(member));
        }
        Assert.Equal(memberVal, val);
    }

    public static IEnumerable<object[]> SetMemberDataProvider()
    {
        yield return [new ExampleFldClass(0), typeof(ExampleFldClass).GetField(nameof(ExampleFldClass.X))!, 13];
        yield return [new ExampleFldStruct(0), typeof(ExampleFldStruct).GetField(nameof(ExampleFldStruct.X))!, 23];
        yield return [new ExamplePropClass(0), typeof(ExamplePropClass).GetProperty(nameof(ExamplePropClass.X))!, 33];
        yield return [new ExamplePropStruct(0), typeof(ExamplePropStruct).GetProperty(nameof(ExamplePropStruct.X))!, 43];
    }
    
   
    public class ExampleIndexerClass
    {
        public readonly Dictionary<int, int> Inner = [];
        
        public int this[int ix]
        {
            get => Inner[ix];
            set => Inner[ix] = value;
        }
    }
    
    public struct ExampleIndexerStruct
    {
        public readonly Dictionary<int, int> Inner = [];

        public ExampleIndexerStruct() { }
        
        public int this[int ix]
        {
            get => Inner[ix];
            set => Inner[ix] = value;
        }
    }

    [Theory]
    [MemberData(nameof(GetIndexerDataProvider))]
    public async Task GetByIndexer(object arg, MethodInfo indexerGetter, int index, int resShould)
    {
        var objParam = new TestParameter(arg.GetType(), "obj");
        var valParam = new TestParameter(index.GetType(), "ix");
        const string tempVarName = "temp";
        
        /*
         * var temp = obj[ix]
         * return tmp
         */
        var body = new BodyNode(
        [
            new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(resShould.GetType()), new MemberNode(tempVarName)),
            new AssignNode(
                new MemberNode(tempVarName), 
                EmissionSetupHelper.CreateCallNode(
                    new MemberNode(objParam.Name),
                    indexerGetter,
                    [new MemberNode(valParam.Name)]
                    )
                ),
            new ReturnNode(new MemberNode(tempVarName))
        ]);

        var (instance, method) =
            await EmissionSetupHelper.CreateInstanceWithMethodAsync([objParam, valParam], body, resShould.GetType());
        
        var res = method!.Invoke(instance, [arg, index])!;
        Assert.Equal(resShould, res);
        Assert.Equal(resShould.GetType(), res.GetType());
    }

    public static IEnumerable<object[]> GetIndexerDataProvider()
    {
        var structWithValue = new ExampleIndexerStruct();
        structWithValue.Inner.Add(2, 42);
        var structGetter = typeof(ExampleIndexerStruct).GetProperties()
            .First(x => x.GetIndexParameters().Any()).GetGetMethod()!;

        yield return [structWithValue, structGetter, 2, 42];
        
        var classWithValue = new ExampleIndexerClass();
        classWithValue.Inner.Add(2, 11);
        var classGetter = typeof(ExampleIndexerClass).GetProperties()
            .First(x => x.GetIndexParameters().Any()).GetGetMethod()!;
        
        yield return [classWithValue, classGetter, 2, 11];
    }

    [Theory]
    [MemberData(nameof(SetByIndexerDataProvider))]
    public async Task EmitSetByIndexer(object arg, MethodInfo indexerSetter, int index, int value, FieldInfo innerDictGetter)
    {
        var objParam = new TestParameter(arg.GetType(), "obj");
        var indexerParam = new TestParameter(index.GetType(), "ix");
        var valueParam = new TestParameter(value.GetType(), "val");
        
        /*
         * obj.set_indexer(ix, value)
         * return obj
         */
        var body = new BodyNode(
        [
            EmissionSetupHelper.CreateCallNode(
                new MemberNode(objParam.Name),
                indexerSetter,
                [
                    new MemberNode(indexerParam.Name),
                    new MemberNode(valueParam.Name),
                ]
                ),
            new ReturnNode(new MemberNode(objParam.Name))
        ]);

        var (instance, method) =
            await EmissionSetupHelper.CreateInstanceWithMethodAsync(
                [objParam, indexerParam, valueParam],
                body,
                arg.GetType());
        
        var res = method!.Invoke(instance, [arg, index, value])!;
        Assert.Equal(arg.GetType(), res.GetType());
        var innerDict = (Dictionary<int, int>)innerDictGetter.GetValue(res)!;
        Assert.Contains(index, innerDict);
        Assert.Equal(value, innerDict[index]);
    }

    public static IEnumerable<object[]> SetByIndexerDataProvider()
    {
        var indexableStruct = new ExampleIndexerStruct();
        var structIndexerSetter = typeof(ExampleIndexerStruct).GetProperties()
            .First(x => x.GetIndexParameters().Any()).GetSetMethod()!;
        var structDictGetter = typeof(ExampleIndexerStruct).GetField(nameof(ExampleIndexerStruct.Inner))!;
        
        yield return [indexableStruct, structIndexerSetter, 2, 32, structDictGetter];
        
        var indexableClass = new ExampleIndexerClass();
        var classIndexerSetter = typeof(ExampleIndexerClass).GetProperties()
            .First(x => x.GetIndexParameters().Any()).GetSetMethod()!;
        var classDictGetter = typeof(ExampleIndexerClass).GetField(nameof(ExampleIndexerClass.Inner))!;
        
        yield return [indexableClass, classIndexerSetter, 4, 33, classDictGetter];
    }
}