using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using plamp.Abstractions.Symbols.SymTable;
using plamp.Alternative.SymbolsBuildingImpl;
using Shouldly;
using Xunit;
using TypeInfo = plamp.Alternative.SymbolsImpl.TypeInfo;

namespace plamp.Alternative.Tests.SymbolsBuildingImpl;

public class BlankFieldInfoTests
{
    private ITypeInfo CreateTypeInfo() => TypeInfo.FromType(typeof(int), "test");

    /// <summary>
    /// До завершения поля можно менять builder
    /// </summary>
    [Fact]
    public void IncompleteFieldModifyBuilder_Correct()
    {
        var fld = new BlankFieldInfo(CreateTypeInfo(), "field", CreateTypeInfo());
        var asm = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("123"), AssemblyBuilderAccess.RunAndCollect);
        var mod = asm.DefineDynamicModule("123");
        var type = mod.DefineType("4321");
        var fldBuilder = type.DefineField("sfaaf", typeof(int), FieldAttributes.Static);
        fld.Builder = fldBuilder;
        fld.Builder.ShouldBeSameAs(fldBuilder);
    }

    /// <summary>
    /// После завершения поле нельзя модифицировать
    /// </summary>
    [Fact]
    public void CompleteField_BuilderAccessThrowsInvalidOperation()
    {
        var fld = new BlankFieldInfo(CreateTypeInfo(), "field", CreateTypeInfo())
        {
            Field = typeof(DateTime).GetFields().First()
        };
        Should.Throw<InvalidOperationException>(() => fld.Builder);
        Should.Throw<InvalidOperationException>(() => fld.Builder = null);
        Should.Throw<InvalidOperationException>(() => fld.Field = null);
    }

    /// <summary>
    /// Завершённость поля не влияет на равенство
    /// </summary>
    [Fact]
    public void CompleteFieldEqualsToNotComplete_Correct()
    {
        var fld1 = new BlankFieldInfo(CreateTypeInfo(), "field", CreateTypeInfo());
        var fld2 = new BlankFieldInfo(CreateTypeInfo(), "field", CreateTypeInfo())
        {
            Field = typeof(DateTime).GetFields().First()
        };
        fld2.ShouldBe(fld1);
    }
    
    /// <summary>
    /// Завершённое поле возвращает FieldInfo вместо builder
    /// </summary>
    [Fact]
    public void CompleteFieldReturnsFieldInfoInsteadOfBuilder_Correct()
    {
        var fld = new BlankFieldInfo(CreateTypeInfo(), "field", CreateTypeInfo());
        var asm = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("123"), AssemblyBuilderAccess.RunAndCollect);
        var mod = asm.DefineDynamicModule("123");
        var type = mod.DefineType("4321");
        var fldBuilder = type.DefineField("sfaaf", typeof(int), FieldAttributes.Static);
        fld.Builder = fldBuilder;

        var dateField = typeof(DateTime).GetFields().First(); 
        fld.Field = dateField;
        
        fld.AsField().ShouldBe(dateField);
    }
  
    /// <summary>
    /// Нельзя создать поле без имени
    /// </summary>
    [Fact]
    public void CannotCreateFieldWithEmptyName_ThrowsInvalidOperationException()
    {
        Should.Throw<InvalidOperationException>(() => new BlankFieldInfo(CreateTypeInfo(), "", CreateTypeInfo()));
    }

    /// <summary>
    /// Имя поля не может состоять из пробелов
    /// </summary>
    [Fact]
    public void CannotCreateFieldWithWhiteSpaceName_ThrowsInvalidOperationException()
    {
        Should.Throw<InvalidOperationException>(() => new BlankFieldInfo(CreateTypeInfo(), " ", CreateTypeInfo()));
    }
    
    /// <summary>
    /// Поле не может иметь тип void
    /// </summary>
    [Fact]
    public void CannotCreateFieldWithVoidType_ThrowsInvalidOperationException()
    {
        Should.Throw<InvalidOperationException>(() => new BlankFieldInfo(Builtins.Void, "field", CreateTypeInfo()));
    }
}
