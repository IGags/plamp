using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using plamp.Alternative.SymbolsBuildingImpl;
using Shouldly;
using Xunit;
using TypeBuilder = plamp.Alternative.SymbolsBuildingImpl.TypeBuilder;
using TypeInfo = plamp.Alternative.SymbolsImpl.TypeInfo;

namespace plamp.Alternative.Tests.SymbolsBuildingImpl;

public class GenericImplFieldInfoTests
{
    /// <summary>
    /// Создать объект, тип, которому принадлежит поле не дженерик - ошибка
    /// </summary>
    [Fact]
    public void CreateGenericImplFieldInfoDefTypeIsNotGeneric_Throws()
    {
        var genericDef = TypeInfo.FromType(typeof(int), "test");
        var intType = TypeInfo.FromType(typeof(int), "test");
        var fieldInfo = new BlankFieldInfo(genericDef, "213", genericDef);
        Should.Throw<InvalidOperationException>(() => new GenericImplFieldInfo(genericDef, fieldInfo, intType));
    }

    /// <summary>
    /// Создать объект, тип оверрайда поля которого дженерик объявление - ошибка
    /// </summary>
    [Fact]
    public void CreateGenericImplFieldTypeOverrideIsGenericDef_Throws()
    {
        var genericDef = TypeInfo.FromType(typeof(List<object>).GetGenericTypeDefinition(), "test");
        var intType = TypeInfo.FromType(typeof(int), "test");
        var fieldInfo = new BlankFieldInfo(intType, "len", genericDef);
        var impl = genericDef.MakeGenericType([Builtins.String]);
        Should.Throw<InvalidOperationException>(() => new GenericImplFieldInfo(impl!, fieldInfo, genericDef));
    }

    /// <summary>
    /// Создать объект тип оверрайда поля void - ошибка
    /// </summary>
    [Fact]
    public void CreateGenericImplOverrideTypeIsVoid_Trows()
    {
        var genericDef = TypeInfo.FromType(typeof(List<object>).GetGenericTypeDefinition(), "test");
        var intType = TypeInfo.FromType(typeof(int), "test");
        var fieldInfo = new BlankFieldInfo(intType, "len", genericDef);
        var impl = genericDef.MakeGenericType([Builtins.String]);
        Should.Throw<InvalidOperationException>(() => new GenericImplFieldInfo(impl!, fieldInfo, Builtins.Void));
    }

    /// <summary>
    /// Возвращает поле у имплементации базового дженерика
    /// </summary>
    [Fact]
    public void AsFieldReturnsImplField_Correct()
    {
        var genericDef = TypeInfo.FromType(typeof(List<object>).GetGenericTypeDefinition(), "test");
        var intType = TypeInfo.FromType(typeof(int), "test");
        var fieldInfo = new BlankFieldInfo(intType, "Count", genericDef);
        var impl = genericDef.MakeGenericType([Builtins.String]);
        var fieldImpl = new GenericImplFieldInfo(impl!, fieldInfo, Builtins.Int);
        var fldInfoActual = fieldImpl.AsField();
        var fldInfoExpected = typeof(List<string>).GetField(nameof(List<object>.Count))!;
        fldInfoActual.ShouldBe(fldInfoExpected);
    }

    /// <summary>
    /// Возвращает корректно поле из незавершённого типа
    /// </summary>
    [Fact]
    public void AsFieldFromTypeBuilder_Correct()
    {
        var asm = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("afkkf"), AssemblyBuilderAccess.RunAndCollect);
        var mod = asm.DefineDynamicModule("1512155125");
        var type = mod.DefineType("TestType", TypeAttributes.Public | TypeAttributes.AutoLayout, typeof(ValueTuple));
        var genericParam = type.DefineGenericParameters("T").First(); 
        var fld = type.DefineField("Fld", genericParam, FieldAttributes.Public);
        
        var param = new GenericParameterBuilder("T", "test") { GenericParameterType = genericParam };
        var genericDef = new TypeBuilder("TestType", [param], "test");
        var baseFld = new BlankFieldInfo(param, "Fld", genericDef);
        
        var impl = genericDef.MakeGenericType([param]);
    }
}