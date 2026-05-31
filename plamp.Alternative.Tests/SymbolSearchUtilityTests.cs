using System;
using System.Collections.Generic;
using plamp.Abstractions.Ast.Node.ComplexTypes;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;
using plamp.Abstractions.Symbols.SymTable;
using plamp.Alternative.SymbolsBuildingImpl;
using plamp.Alternative.SymbolsImpl;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests;

public class SymbolSearchUtilityTests
{
    #region TryGetTypeOrGetErrorRecord

    /// <summary>
    /// Получение типа внутри модуля - корректно
    /// </summary>
    [Fact]
    public void GetTypeOneMatches_Correct()
    {
        const string typeName = "test";
        var builder = new SymTableBuilder() { ModuleName = "test" };
        var info = builder.DefineType(new TypedefNode(new TypedefNameNode(typeName), [], []));
        var reference = new TypeNode(new TypeNameNode(typeName));

        var record =
            SymbolSearchUtility.TryGetTypeOrErrorRecord(reference, [builder, Builtins.SymTable], out var actualInfo);
        record.ShouldBeNull();
        actualInfo.ShouldBe(info);
    }

    /// <summary>
    /// Получение типа, тип не найден - ошибка
    /// </summary>
    [Fact]
    public void TypeNotFound_ReturnsError()
    {
        var builder = new SymTableBuilder() { ModuleName = "test" };
        var reference = new  TypeNode(new TypeNameNode("i'm not exist"));
        var record = SymbolSearchUtility.TryGetTypeOrErrorRecord(reference, [builder, Builtins.SymTable], out var actual);
        record.ShouldNotBeNull().Code.ShouldBe(PlampExceptionInfo.TypeIsNotFound("").Code);
        actual.ShouldBeNull();
    }

    /// <summary>
    /// Получение типа, тип имеется в нескольких модулях - ошибка
    /// </summary>
    [Fact]
    public void TypeFoundIntTwoModulesSimultaneously_ReturnsError()
    {
        const string typeName = "T";
        
        var builder1 = new SymTableBuilder() { ModuleName = "test" };
        var builder2 = new SymTableBuilder() { ModuleName = "test2" };
        
        builder1.DefineType(new TypedefNode(new TypedefNameNode(typeName), [], []));
        builder2.DefineType(new TypedefNode(new TypedefNameNode(typeName), [], []));
        
        var reference = new TypeNode(new TypeNameNode(typeName));
        
        var record = SymbolSearchUtility.TryGetTypeOrErrorRecord(reference, [builder1, builder2], out var actual);
        actual.ShouldBeNull();
        var code = record.ShouldNotBeNull().Code;
        code.ShouldBe(PlampExceptionInfo.AmbiguousTypeName("", []).Code);
    }

    /// <summary>
    /// Получение типа, тип имеет другое число дженерик параметров - ошибка, но тип будет найден
    /// </summary>
    [Fact]
    public void GenericTypeHasDifferentParameterCount_ReturnsErrorWithType()
    {
        const string typeName = "T";
        
        var builder = new SymTableBuilder() { ModuleName = "test" };
        var param = builder.CreateGenericParameter(new GenericDefinitionNode(new GenericParameterNameNode("A")));
        var info = builder.DefineType(new TypedefNode(new TypedefNameNode(typeName), [],
            [new GenericDefinitionNode(new GenericParameterNameNode("A"))]), [param]);
        
        var reference = new TypeNode(new TypeNameNode(typeName));
        
        var record = SymbolSearchUtility.TryGetTypeOrErrorRecord(reference, [builder, Builtins.SymTable], out var actual);
        actual.ShouldBe(info);
        record.ShouldNotBeNull().Code.ShouldBe(PlampExceptionInfo.GenericTypeDefinitionHasDifferentParameterCount(1, 0).Code);
    }

    #endregion

    #region IsNumeric

    /// <summary>
    /// Тип относится ко множеству - корректно
    /// </summary>
    [Fact]
    public void IsNumeric_ReturnsTrue()
    {
        var type = Builtins.Double;
        SymbolSearchUtility.IsNumeric(type).ShouldBeTrue();
    }

    /// <summary>
    /// Тип не относится ко множеству
    /// </summary>
    [Fact]
    public void IsNumeric_ReturnsFalse()
    {
        var type = Builtins.Char;
        SymbolSearchUtility.IsNumeric(type).ShouldBeFalse();
    }

    #endregion

    #region IsLogical

    /// <summary>
    /// Тип является логическим
    /// </summary>
    [Fact]
    public void TypeIsLogical_ReturnsTrue()
    {
        var type = Builtins.Bool;
        SymbolSearchUtility.IsLogical(type).ShouldBeTrue();
    }

    /// <summary>
    /// Тип не является логическим
    /// </summary>
    [Fact]
    public void TypeIsNotLogical_ReturnsFalse()
    {
        var type = Builtins.Bool.MakeArrayType();
        SymbolSearchUtility.IsLogical(type).ShouldBeFalse();
    }

    #endregion

    #region IsVoid

    /// <summary>
    /// Тип является void
    /// </summary>
    [Fact]
    public void TypeIsVoid_ReturnsTrue()
    {
        var type = Builtins.Void;
        SymbolSearchUtility.IsVoid(type).ShouldBeTrue();
    }

    /// <summary>
    /// Тип не является void
    /// </summary>
    [Fact]
    public void TypeIsNotVoid_ReturnsFalse()
    {
        var type = Builtins.Any;
        SymbolSearchUtility.IsVoid(type).ShouldBeFalse();
    }

    #endregion

    #region IsString

    /// <summary>
    /// Тип является строкой
    /// </summary>
    [Fact]
    public void TypeIsString_ReturnsTrue()
    {
        var type = Builtins.String;
        SymbolSearchUtility.IsString(type).ShouldBeTrue();
    }

    /// <summary>
    /// Тип не является строкой
    /// </summary>
    [Fact]
    public void TypeIsNotString_ReturnsFalse()
    {
        var type = Builtins.Char.MakeArrayType();
        SymbolSearchUtility.IsString(type).ShouldBeFalse();
    }

    #endregion

    #region TryGetFuncOrErrorRecord

    /// <summary>
    /// Получение одной подходящей функции
    /// </summary>
    [Fact]
    public void FuncExists_Correct()
    {
        var fnName = Builtins.StrConcat.DefinitionName;
        var record = SymbolSearchUtility.TryGetFuncOrErrorRecord(fnName, [Builtins.SymTable], out var actual);
        record.ShouldBeNull();
        actual.ShouldBe(Builtins.StrConcat);
    }

    /// <summary>
    /// Подходящей функции не нашлось
    /// </summary>
    [Fact]
    public void FuncNotFound_ReturnsError()
    {
        var fnName = Builtins.StrConcat.DefinitionName;
        var record = SymbolSearchUtility.TryGetFuncOrErrorRecord(fnName, [], out var actual);
        actual.ShouldBeNull();
        record.ShouldNotBeNull().Code.ShouldBe(PlampExceptionInfo.FunctionIsNotFound("").Code);
    }
    
    /// <summary>
    /// Подходящих функицй нашлось несколько
    /// </summary>
    [Fact]
    public void FuncFoundAcrossMultipleModules_ReturnsError()
    {
        var fnName = Builtins.StrConcat.DefinitionName;
        var record = SymbolSearchUtility.TryGetFuncOrErrorRecord(fnName, [Builtins.SymTable, Builtins.SymTable], out var actual);
        actual.ShouldBeNull();
        record.ShouldNotBeNull().Code.ShouldBe(PlampExceptionInfo.AmbiguousFunctionReference("", []).Code);
    }
    
    #endregion

    #region FillGenericMapping

    /// <summary>
    /// Объявление функции не может иметь объявление дженерик типа как параметр
    /// </summary>
    [Fact]
    public void ParameterTypeGenericDef_ThrowsError()
    {
        var symTable = new SymTableBuilder();
        var genericParamNode = new GenericDefinitionNode(new GenericParameterNameNode("A"));
        var genericParam = symTable.CreateGenericParameter(genericParamNode);

        var paramType = symTable.DefineType(new TypedefNode(new TypedefNameNode("T"), [], [genericParamNode]),
            [genericParam]);

        var argType = Builtins.Any;

        Should.Throw<InvalidOperationException>(() =>
            SymbolSearchUtility.FillGenericMapping(paramType, argType, []));
    }

    /// <summary>
    /// Аргумент функции не может иметь тип дженерик объявления.
    /// </summary>
    [Fact]
    public void ArgumentTypeGenericDef_ThrowsError()
    {
        var argType = TypeInfo.FromType(typeof(List<>), "test");
        var paramType = Builtins.Any;
        
        Should.Throw<InvalidOperationException>(() => SymbolSearchUtility.FillGenericMapping(paramType, argType, []));
    }

    /// <summary>
    /// Тип параметра функции дженерик параметр
    /// </summary>
    [Fact]
    public void FuncParameterHasGenericParameterArgHasNo_ReturnsMapping()
    {
        var argType = Builtins.Byte.MakeArrayType();
        var paramType = new GenericParameterBuilder("A", "test");
        var mapping = new List<KeyValuePair<ITypeInfo, ITypeInfo>>();
        SymbolSearchUtility.FillGenericMapping(paramType, argType, mapping);
        var pair = mapping.ShouldHaveSingleItem();
        pair.Key.ShouldBe(paramType);
        pair.Value.ShouldBe(argType);
    }

    /// <summary>
    /// Тип параметра массив, тип аргумента нет
    /// </summary>
    [Fact]
    public void ParameterIsArrayArgumentIsNot_ReturnsNothing()
    {
        var argType = Builtins.Char;
        var paramElemType = new GenericParameterBuilder("A", "test");
        var paramType = paramElemType.MakeArrayType();
        
        var mapping = new List<KeyValuePair<ITypeInfo, ITypeInfo>>();
        SymbolSearchUtility.FillGenericMapping(paramType, argType, mapping);
        mapping.ShouldBeEmpty();
    }

    /// <summary>
    /// И параметр и аргумент типы массивов
    /// </summary>
    [Fact]
    public void ParameterHasArrayTypeArgumentHasEither()
    {
        var argElemType = Builtins.Ushort;
        var argType = argElemType.MakeArrayType();
        var paramElemType = new GenericParameterBuilder("A", "test");
        var paramType = paramElemType.MakeArrayType();
        var mapping = new List<KeyValuePair<ITypeInfo, ITypeInfo>>();
        SymbolSearchUtility.FillGenericMapping(paramType, argType, mapping);
        var pair = mapping.ShouldHaveSingleItem();
        pair.Key.ShouldBe(paramElemType);
        pair.Value.ShouldBe(argElemType);
    }

    /// <summary>
    /// Параметр дженерик реализация, а аргумент нет
    /// </summary>
    [Fact]
    public void ParameterIsGenericImplementationArgumentIsNot_ReturnsNothing()
    {
        var genericDef = TypeInfo.FromType(typeof(List<>), "test");
        var paramType = genericDef.MakeGenericType([new GenericParameterBuilder("T", "test")]).ShouldNotBeNull();
        var argType = Builtins.Any;
        
        var mapping = new List<KeyValuePair<ITypeInfo, ITypeInfo>>();
        SymbolSearchUtility.FillGenericMapping(paramType, argType, mapping);
        mapping.ShouldBeEmpty();
    }

    /// <summary>
    /// И параметр и тип дженерики, но с разным типом объявления 
    /// </summary>
    [Fact]
    public void ParameterAndArgumentHasDifferentGenericTypeDefinitions_ReturnsNothing()
    {
        var firstDef = TypeInfo.FromType(typeof(List<>), "test");
        var secondDef = TypeInfo.FromType(typeof(Dictionary<,>), "test");
        
        var paramType = firstDef.MakeGenericType([new GenericParameterBuilder("T", "test")]).ShouldNotBeNull();
        var argType = secondDef.MakeGenericType([Builtins.String, Builtins.Short]).ShouldNotBeNull();
        
        var mapping = new List<KeyValuePair<ITypeInfo, ITypeInfo>>();
        SymbolSearchUtility.FillGenericMapping(paramType, argType, mapping);
        mapping.ShouldBeEmpty();
    }

    /// <summary>
    /// Корректное число аргументов в маппинг списке при обходе дженерик реализаций с общим базовым типом
    /// </summary>
    [Fact]
    public void ParameterAndArgumentHasSameGenericDef_Correct()
    {
        var genericDef = TypeInfo.FromType(typeof(Dictionary<,>), "test");
        
        var firstBuilder = new GenericParameterBuilder("T", "test");
        var secondBuilder = new GenericParameterBuilder("T2", "test");
        
        var paramType = genericDef.MakeGenericType([firstBuilder.MakeArrayType(), secondBuilder]).ShouldNotBeNull();

        var firstArgElem = Builtins.Char;
        var firstArg = firstArgElem.MakeArrayType();
        var argType = genericDef.MakeGenericType([firstArg, firstBuilder]).ShouldNotBeNull();
        
        var mapping = new List<KeyValuePair<ITypeInfo, ITypeInfo>>();
        SymbolSearchUtility.FillGenericMapping(paramType, argType, mapping);
        mapping.Count.ShouldBe(2);
        mapping[0].Key.ShouldBe(firstBuilder);
        mapping[0].Value.ShouldBe(firstArgElem);
        mapping[1].Key.ShouldBe(secondBuilder);
        mapping[1].Value.ShouldBe(firstBuilder);
    }

    /// <summary>
    /// Данный метод не производит валидации на то, что один и тот же параметр имеет две разные реализации
    /// </summary>
    [Fact]
    public void MappingCanHasDuplicateKeys_Correct()
    {
        var genericDef = TypeInfo.FromType(typeof(Dictionary<,>), "test");
        
        var firstBuilder = new GenericParameterBuilder("T", "test");
        var paramType = genericDef.MakeGenericType([firstBuilder, firstBuilder]).ShouldNotBeNull();

        var firstArg = Builtins.Int;
        var secondArg = Builtins.String;
        var argType = genericDef.MakeGenericType([firstArg, secondArg]).ShouldNotBeNull();
        
        var mapping = new List<KeyValuePair<ITypeInfo, ITypeInfo>>();
        SymbolSearchUtility.FillGenericMapping(paramType, argType, mapping);
        mapping.Count.ShouldBe(2);
        mapping[0].Key.ShouldBe(firstBuilder);
        mapping[0].Value.ShouldBe(firstArg);
        mapping[1].Key.ShouldBe(firstBuilder);
        mapping[1].Value.ShouldBe(secondArg);
    }

    /// <summary>
    /// Функция не проводит валидации на соответствие или конверсии типов
    /// </summary>
    [Fact]
    public void IgnoresDifferentNonGenericTypes_Correct()
    {
        var paramType = Builtins.Char;
        var argType = Builtins.Float;
        var mapping = new List<KeyValuePair<ITypeInfo, ITypeInfo>>();
        SymbolSearchUtility.FillGenericMapping(paramType, argType, mapping);
        mapping.ShouldBeEmpty();
    }

    #endregion

    #region ImplicitlyConvertable

    public static IEnumerable<object[]> ImplicitNumericConversion_ReturnsTrue_Suit()
    {
        yield return [Builtins.Byte, Builtins.Int];
        yield return [Builtins.Int, Builtins.Long];
        yield return [Builtins.Long, Builtins.Double];
        yield return [Builtins.Int, Builtins.Int];
        yield return [Builtins.Int, Builtins.Ulong];
        yield return [Builtins.Short, Builtins.Int];
        yield return [Builtins.Float, Builtins.Double];
    } 
    
    /// <summary>
    /// Случаи, когда один числовой тип может быть превращён в другой
    /// </summary>
    [Theory]
    [MemberData(nameof(ImplicitNumericConversion_ReturnsTrue_Suit))]
    public void ImplicitNumericConversion_ReturnsTrue(ITypeInfo from, ITypeInfo to)
    {
        SymbolSearchUtility.ImplicitlyConvertable(from, to).ShouldBeTrue();
    }

    public static IEnumerable<object[]> ImplicitNumericConversion_ReturnsFalse_Suit()
    {
        yield return [Builtins.Long, Builtins.Ulong];
        yield return [Builtins.Int, Builtins.Uint];
        yield return [Builtins.Int, Builtins.Short];
        yield return [Builtins.Float, Builtins.Long];
    }
    
    /// <summary>
    /// Случаи, когда один числовой тип неприводим к другому
    /// </summary>
    [Theory]
    [MemberData(nameof(ImplicitNumericConversion_ReturnsFalse_Suit))]
    public void ImplicitNumericConversion_ReturnsFalse(ITypeInfo from, ITypeInfo to)
    {
        SymbolSearchUtility.ImplicitlyConvertable(from, to).ShouldBeFalse();
    }

    public static IEnumerable<object[]> ImplicitArrayConversion_ReturnsTrue_Suit()
    {
        yield return [Builtins.Int.MakeArrayType(), Builtins.Array];
        var param = new GenericParameterBuilder("T", "t");
        yield return [param.MakeArrayType(), Builtins.Array];
        yield return [Builtins.Array, Builtins.Array];
        yield return [Builtins.Int.MakeArrayType(), Builtins.Int.MakeArrayType()];
    }
    
    /// <summary>
    /// Случаи, когда типы массивов приводимы друг к другу.
    /// </summary>
    [Theory]
    [MemberData(nameof(ImplicitArrayConversion_ReturnsTrue_Suit))]
    public void ImplicitArrayConversion_ReturnsTrue(ITypeInfo from, ITypeInfo to)
    {
        SymbolSearchUtility.ImplicitlyConvertable(from, to).ShouldBeTrue();
    }

    public static IEnumerable<object[]> ImplicitArrayConversion_ReturnsFalse_Suit()
    {
        var param = new GenericParameterBuilder("T", "t");
        yield return [param, Builtins.Array];
        yield return [Builtins.Any, Builtins.Array];
        yield return [Builtins.Void, Builtins.Array];
        var type = TypeInfo.FromType(typeof(List<>), "m");
        yield return [type, Builtins.Array];
        var impl = type.MakeGenericType([Builtins.Int]).ShouldNotBeNull();
        yield return [impl, Builtins.Array];
        yield return [Builtins.Int.MakeArrayType(), Builtins.Float.MakeArrayType()];
        yield return [Builtins.Int.MakeArrayType(), Builtins.Any.MakeArrayType()];
    }
    
    /// <summary>
    /// Случаи, когда типы массивов не приводимы друг к другу
    /// </summary>
    [Theory]
    [MemberData(nameof(ImplicitArrayConversion_ReturnsFalse_Suit))]
    public void ImplicitArrayConversion_ReturnsFalse(ITypeInfo from, ITypeInfo to)
    {
        SymbolSearchUtility.ImplicitlyConvertable(from, to).ShouldBeFalse();
    }
    
    public static IEnumerable<object[]> AnyConversion_ReturnsTrue_Suit()
    {
        yield return [Builtins.Any, Builtins.Any];
        yield return [Builtins.Int, Builtins.Any];
        yield return [Builtins.String, Builtins.Any];
        var param = new GenericParameterBuilder("T", "t");
        yield return [param.MakeArrayType(), Builtins.Any];
        var generic = TypeInfo.FromType(typeof(List<>), "a");
        var impl = generic.MakeGenericType([Builtins.Int]).ShouldNotBeNull();
        yield return [impl, Builtins.Any];
        yield return [param, Builtins.Any];
        yield return [Builtins.Array, Builtins.Any];
    }
    
    /// <summary>
    /// Корректная конверсия с типом any
    /// </summary>
    [Theory]
    [MemberData(nameof(AnyConversion_ReturnsTrue_Suit))]
    public void AnyConversion_ReturnsTrue(ITypeInfo from, ITypeInfo to)
    {
        SymbolSearchUtility.ImplicitlyConvertable(from, to).ShouldBeTrue();
    }

    public static IEnumerable<object[]> AnyConversion_ReturnsFalse_Suit()
    {
        yield return [Builtins.Void, Builtins.Any];
        yield return [Builtins.Any, Builtins.Void];
        yield return [Builtins.Any, Builtins.Int];
    }
    
    /// <summary>
    /// Конверсия с типом any невозможна
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    [Theory]
    [MemberData(nameof(AnyConversion_ReturnsFalse_Suit))]
    public void AnyConversion_ReturnsFalse(ITypeInfo from, TypeInfo to)
    {
        SymbolSearchUtility.ImplicitlyConvertable(from, to).ShouldBeFalse();
    }

    public static IEnumerable<object[]> GenericConversion_ReturnsFalse_Suit()
    {
        var baseType = TypeInfo.FromType(typeof(List<>), "m");
        var param = new GenericParameterBuilder("T", "m");
        var impl = baseType.MakeGenericType([param]).ShouldNotBeNull();
        var impl2 = baseType.MakeGenericType([Builtins.Int]).ShouldNotBeNull();
        var impl3 = baseType.MakeGenericType([Builtins.Any]).ShouldNotBeNull();
        yield return [impl2, impl];
        yield return [impl2, impl3];
    }
    
    [Theory]
    [MemberData(nameof(GenericConversion_ReturnsFalse_Suit))]
    public void GenericConversion_ReturnsFalse(ITypeInfo from, ITypeInfo to)
    {
        SymbolSearchUtility.ImplicitlyConvertable(from, to).ShouldBeFalse();
    }

    #endregion

    #region NeedToCast

    public static IEnumerable<object[]> NeedsToCreateExplicitCast_True_Suit()
    {
        yield return [Builtins.Int, Builtins.Float];
        var param = new GenericParameterBuilder("T", "t");
        yield return [param, Builtins.Any];
        var time = TypeInfo.FromType(typeof(DateTime), "m");
        yield return [time, Builtins.Any];
        var generic = TypeInfo.FromType(typeof(List<>), "m");
        var impl = generic.MakeGenericType([Builtins.Int]).ShouldNotBeNull();
        yield return [impl, Builtins.Any];
    } 
    
    /// <summary>
    /// Случаи, когда требуется создание явной конверсии из типа в тип
    /// </summary>
    [Theory]
    [MemberData(nameof(NeedsToCreateExplicitCast_True_Suit))]
    public void NeedsToCreateExplicitCast_True(ITypeInfo from, ITypeInfo to)
    {
        SymbolSearchUtility.NeedToCast(from, to).ShouldBeTrue();
    }

    public static IEnumerable<object[]> NeedsToCreateExplicitCast_False_Suit()
    {
        yield return [Builtins.Int, Builtins.String];
        yield return [Builtins.Any, Builtins.Void];
        yield return [Builtins.Float.MakeArrayType(), Builtins.Int.MakeArrayType()];
        yield return [Builtins.Array, Builtins.Any];
        yield return [Builtins.Int.MakeArrayType(), Builtins.Any];
        yield return [Builtins.Int.MakeArrayType(), Builtins.Array];
        var param = new GenericParameterBuilder("T", "t");
        yield return [param.MakeArrayType(), Builtins.Array];
        yield return [Builtins.Int, Builtins.Int];
        yield return [Builtins.Any, Builtins.Any];
        var type = TypeInfo.FromType(typeof(List<>), "m");
        var impl = type.MakeGenericType([Builtins.Int]).ShouldNotBeNull();
        yield return [impl, Builtins.Array];
    }

    /// <summary>
    /// Случаи, когда не требуется создания явной конверсии или никакая конверсия невозможна в принципе
    /// </summary>
    [Theory]
    [MemberData(nameof(NeedsToCreateExplicitCast_False_Suit))]
    public void NeedToExplicitCast_False(ITypeInfo from, ITypeInfo to)
    {
        SymbolSearchUtility.NeedToCast(from, to).ShouldBeFalse();
    }

    #endregion
}