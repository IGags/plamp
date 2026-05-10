using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;
using plamp.Abstractions.Symbols;
using plamp.Alternative.SymbolsBuildingImpl;
using Shouldly;
using Xunit;
using TypeBuilder = plamp.Alternative.SymbolsBuildingImpl.TypeBuilder;
using TypeInfo = plamp.Alternative.SymbolsImpl.TypeInfo;

namespace plamp.Alternative.Tests.SymbolsBuildingImpl;

public class GenericImplFieldInfoTests
{
    private class GenericFieldHost<T>
    {
        [PlampVisible]
        public T Value = default!;
    }
    
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
        var genericDef = TypeInfo.FromType(typeof(GenericFieldHost<>), "test");
        var intType = TypeInfo.FromType(typeof(int), "test");
        var fieldInfo = new BlankFieldInfo(intType, nameof(GenericFieldHost<object>.Value), genericDef);
        var impl = genericDef.MakeGenericType([Builtins.String]);
        var fieldImpl = new GenericImplFieldInfo(impl!, fieldInfo, Builtins.Int);
        var fldInfoActual = fieldImpl.AsField();
        var fldInfoExpected = typeof(GenericFieldHost<string>).GetField(nameof(GenericFieldHost<object>.Value))!;
        fldInfoActual.ShouldBe(fldInfoExpected);
    }

    /// <summary>
    /// Возвращает корректно поле из недостроенного дженерик типа
    /// </summary>
    [Fact]
    public void AsFieldFromTypeBuilder_Correct()
    {
        var asm = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("afkkf"), AssemblyBuilderAccess.RunAndCollect);
        var mod = asm.DefineDynamicModule("1512155125");
        var type = mod.DefineType("TestGeneric", TypeAttributes.Public | TypeAttributes.AutoLayout);
        var genericParam = type.DefineGenericParameters("T").First(); 
        var fld = type.DefineField("Fld", genericParam, FieldAttributes.Public);
        
        var param = new GenericParameterBuilder("T", "test") { GenericParameterType = genericParam };
        var genericDef = new TypeBuilder("TestType", [param], "test");
        var fldDef = new FieldDefNode(new TypeNode(new TypeNameNode("T")), new FieldNameNode("Fld"));
        fldDef.FieldType.TypeInfo = param;
        
        genericDef.AddField(fldDef);
        var info = genericDef.FieldBuilders.First();
        info.Builder = fld;
        genericDef.Builder = type;

        var intType = Builtins.Int;
        var impl = genericDef.MakeGenericType([intType]);
        var implFld = impl!.Fields.ShouldHaveSingleItem();

        var implInfo = implFld.AsField();
        implInfo.Name.ShouldBe("Fld");
        Should.Throw<Exception>(() => implInfo.FieldType);
    }

    /// <summary>
    /// Корректно возвращает поле из созданного типа
    /// </summary>
    [Fact]
    public void AsFieldFromTypeWithParamBuilder_Correct()
    {
        var asm = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("afkkf"), AssemblyBuilderAccess.RunAndCollect);
        var mod = asm.DefineDynamicModule("1512155125");
        
        var type = mod.DefineType("TestGeneric", TypeAttributes.Public | TypeAttributes.AutoLayout);
        var genericParam = type.DefineGenericParameters("T").First();
        var ctorInfo = typeof(PlampVisibleAttribute).GetConstructor(BindingFlags.Public | BindingFlags.Instance, [])!;
        var attrBuilder = new CustomAttributeBuilder(ctorInfo, []);
        
        var fld = type.DefineField("Fld", genericParam, FieldAttributes.Public);
        fld.SetCustomAttribute(attrBuilder);
        var runtimeType = type.CreateType();

        var info = TypeInfo.FromType(runtimeType, "test");
        var impl = info.MakeGenericType([Builtins.Int]).ShouldNotBeNull();

        var fldInfo = impl.Fields.ShouldHaveSingleItem();
        var sharpField = fldInfo.AsField();
        sharpField.Name.ShouldBe("Fld");
        sharpField.FieldType.ShouldBe(typeof(int));
    }
}
