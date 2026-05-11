using System;
using System.Collections.Generic;
using System.Linq;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Abstractions.Symbols.SymTable;

namespace plamp.Alternative;

/// <summary>
/// Всякая статика, которая может быть полезна сразу многим
/// </summary>
public static class SymbolSearchUtility
{
    /// <summary>
    /// Получение информации о типе по узлу AST ссылки на него.
    /// </summary>
    /// <param name="typeNode">Узел-ссылка на тип в AST</param>
    /// <param name="symbolTables">Список модулей, в которых необходимо найти тип</param>
    /// <param name="typeInfo">Возвращаемая информация о типе, не null, если метод вернул null, в случае несовпадения числа дженериков у типа возвращает тот тип</param>
    /// <returns>В случае если значений типов нет или их несколько, или если число дженерик параметров типа не совпадает - возвращает запись об ошибке</returns>
    public static PlampExceptionRecord? TryGetTypeOrErrorRecord(
        TypeNode typeNode,
        IEnumerable<ISymTable> symbolTables,
        out ITypeInfo? typeInfo)
    {
        typeInfo = null;
        var name = typeNode.TypeName.Name;
        var genericCt = typeNode.GenericParameters.Count;
        var error = TryGetTypeCore(name, genericCt, symbolTables, out typeInfo);
        return error;       
    }
    
    private static PlampExceptionRecord? TryGetTypeCore(
        string name, 
        int genericCount, 
        IEnumerable<ISymTable> symbolTables,
        out ITypeInfo? typeInfo)
    {
        typeInfo = null;
        var types = new List<(ITypeInfo typ, ISymTable table)>();
        foreach (var table in symbolTables)
        {
            var type = table.FindType(name);
            if(type != null) types.Add((type, table));
        }

        if (types.Count > 1)
        {
            return PlampExceptionInfo.AmbiguousTypeName(name, types.Select(x => x.table.ModuleName));
        }
        if (types.Count == 0) return PlampExceptionInfo.TypeIsNotFound(name);

        var defParamCount = types[0].typ.GetGenericParameters().Count; 
        if (defParamCount != genericCount)
        {
            typeInfo = types[0].typ;
            return PlampExceptionInfo.GenericTypeDefinitionHasDifferentParameterCount(defParamCount, genericCount);
        }
        
        typeInfo = types[0].typ;
        return null;
    }

    /// <summary>
    /// Проверяет - является ли данный тип числовым типом
    /// </summary>
    /// <param name="type">Тип которой надо проверить</param>
    /// <returns>Результат проверки</returns>
    public static bool IsNumeric(ITypeInfo type)
    {
        return type.Equals(Builtins.Int)
               || type.Equals(Builtins.Uint)
               || type.Equals(Builtins.Long)
               || type.Equals(Builtins.Ulong)
               || type.Equals(Builtins.Short)
               || type.Equals(Builtins.Ushort)
               || type.Equals(Builtins.Byte)
               || type.Equals(Builtins.Sbyte)
               || type.Equals(Builtins.Double)
               || type.Equals(Builtins.Float);
    }
    
    /// <summary>
    /// Проверяет - является ли данный тип логическим типом
    /// </summary>
    /// <param name="type">Тип которой надо проверить</param>
    /// <returns>Результат проверки</returns>
    public static bool IsLogical(ITypeInfo type) => type.Equals(Builtins.Bool);

    /// <summary>
    /// Проверяет - является ли данный тип void типом
    /// </summary>
    /// <param name="type">Тип которой надо проверить</param>
    /// <returns>Результат проверки</returns>
    public static bool IsVoid(ITypeInfo type) => type.Equals(Builtins.Void);

    /// <summary>
    /// Проверяет - является ли данный тип строковым типом
    /// </summary>
    /// <param name="type">Тип которой надо проверить</param>
    /// <returns>Результат проверки</returns>
    public static bool IsString(ITypeInfo type) => type.Equals(Builtins.String);

    /// <summary>
    /// Ищет функцию в указанном списке модулей.
    /// </summary>
    /// <param name="name">Имя функции, по которому следует искать, просто имя без указания дженерик параметров и типов аргументов</param>
    /// <param name="symbolTables">Список символьных таблиц по которым производить поиск</param>
    /// <param name="fnInfo">Информация о найденной функции, не null, если возвращаемое значение null</param>
    /// <returns>Информация об ошибке возникшей при поиске(подходящих функций не нашлось или их несколько)</returns>
    public static PlampExceptionRecord? TryGetFuncOrErrorRecord(
        string name,
        IEnumerable<ISymTable> symbolTables, 
        out IFnInfo? fnInfo)
    {
        fnInfo = null;
        var funcs = new List<(string modName, IFnInfo fnInfo)>();
        foreach (var symbolTable in symbolTables)
        {
            var found = symbolTable.FindFunc(name);
            if(found != null) funcs.Add((symbolTable.ModuleName, found));
        }

        if (funcs.Count == 1) fnInfo = funcs[0].fnInfo;

        return funcs.Count switch
        {
            0 => PlampExceptionInfo.FunctionIsNotFound(name),
            > 1 => PlampExceptionInfo.AmbiguousFunctionReference(name, funcs.Select(x => x.modName)),
            _ => null
        };
    }

    /// <summary>
    /// Составляет соответствие типам параметрам дженерик функции внутри её аргументов и типам из реализации. Не проверяет дубликаты.
    /// Не проверяет возможность конверсии одного типа во второй, в случае несовпадения типа и невозможности дальнейшего обхода прекращает выполнение без какой-либо ошибки
    /// </summary>
    /// <param name="fnParameterType">Тип параметра из функции</param>
    /// <param name="fnArgType">Тип аргумента, с которым функцию вызвали</param>
    /// <param name="genericMapping">Список соответствий дженерик аргументов функции и типов из вызова функции.</param>
    /// <exception cref="InvalidOperationException">Ошибка случается если в качестве аргумента или параметра передано объявление дженерик типа</exception>
    /// <exception cref="ArgumentNullException">Ошибка случается при некорректной реализации интерфейса <see cref="ITypeInfo"/></exception>
    public static void FillGenericMapping(
        ITypeInfo fnParameterType,
        ITypeInfo fnArgType,
        List<KeyValuePair<ITypeInfo, ITypeInfo>> genericMapping)
    {
        if (fnParameterType.IsGenericTypeDefinition)
        {
            throw new InvalidOperationException("В аргументе объявления функции не может быть объявления дженерик типа");
        }

        if (fnArgType.IsGenericTypeDefinition)
        {
            throw new InvalidOperationException("В аргументе функции не может быть объявления дженерик типа");
        }
        
        if (fnParameterType.IsGenericTypeParameter)
        {
            genericMapping.Add(new(fnParameterType, fnArgType));
            return;
        }

        if (fnParameterType.IsArrayType)
        {
            if (!fnArgType.IsArrayType) return;
            
            var fnParamElem = fnParameterType.ElementType();
            ArgumentNullException.ThrowIfNull(fnParamElem);
            var fnArgElem = fnArgType.ElementType();
            ArgumentNullException.ThrowIfNull(fnArgElem);

            FillGenericMapping(fnParamElem, fnArgElem, genericMapping);
            return;
        }

        if (!fnParameterType.IsGenericType) return;
        if (!fnArgType.IsGenericType) return;
            
        var fnParamDef = fnParameterType.GetGenericTypeDefinition();
        ArgumentNullException.ThrowIfNull(fnParamDef);
        var fnArgDef = fnArgType.GetGenericTypeDefinition();
        ArgumentNullException.ThrowIfNull(fnArgDef);
            
        if (!fnArgDef.Equals(fnParamDef)) return;
            
        var fnParamArgs = fnParameterType.GetGenericArguments();
        var fnArgArgs = fnArgType.GetGenericArguments();

        if (fnParamArgs.Count != fnArgArgs.Count) return;

        foreach (var (paramArg, argArg) in fnParamArgs.Zip(fnArgArgs))
        {
            FillGenericMapping(paramArg, argArg, genericMapping);
        }
    }

    /// <summary>
    /// Метод проверяющий возможно ли неявно сконвертировать исходный тип в целевой
    /// </summary>
    /// <param name="from">Тип из которого происходит конверсия</param>
    /// <param name="to">Тип в которой идёт конверсия</param>
    /// <returns>Флаг возможности операции</returns>
    public static bool ImplicitlyConvertable(ITypeInfo from, ITypeInfo to)
    {
        if (from.Equals(to)) return true;
        return ImplicitlyNumericConvertable(from, to)
               || ArrayImplicitlyConvertable(from, to)
               || AnyImplicitlyConvertable(from, to);
    }

    /// <summary>
    /// Говорит - требуется ли создания явного оператора приведения внутри Ast
    /// </summary>
    /// <param name="from">Тип из которого происходит конверсия</param>
    /// <param name="to">Тип в которой идёт конверсия</param>
    /// <returns>True - создание оператора требуется, False - создание оператора не требуется или конверсия невозможна</returns>
    public static bool NeedToCast(ITypeInfo from, ITypeInfo to)
    {
        if (!ImplicitlyConvertable(from, to)) return false;
        if (from.Equals(to)) return false;
        if ((from.IsArrayType || from.Equals(Builtins.Array)) 
            && (to.Equals(Builtins.Any) || to.Equals(Builtins.Array))) return false;
        return true;
    }

    /// <summary>
    /// Проверка на возможность конверсии для числовых типов
    /// </summary>
    private static bool ImplicitlyNumericConvertable(ITypeInfo from, ITypeInfo to)
    {
        if (!IsNumeric(from) || !IsNumeric(to)) return false;
        var fromPower = GetNumericTypeConversionPower(from);
        var toPower = GetNumericTypeConversionPower(to);
        var difference = fromPower - toPower;
        return difference > 0;
    }

    /// <summary>
    /// Проверка на возможность конверсии в обобщённый тип массива
    /// </summary>
    private static bool ArrayImplicitlyConvertable(ITypeInfo from, ITypeInfo to)
    {
        return to.Equals(Builtins.Array) && from.IsArrayType;
    }

    /// <summary>
    /// Проверка на возможность конверсии в обобщённый тип
    /// </summary>
    private static bool AnyImplicitlyConvertable(ITypeInfo from, ITypeInfo to)
    {
        return to.Equals(Builtins.Any) && !from.Equals(Builtins.Void);
    }

    /// <summary>
    /// Кусок логики, который отвечает за потенциал конверсии одного числового типа в другой
    /// </summary>
    /// <param name="type">Числовой тип</param>
    /// <returns>Потенциал конверсии, чем он ниже, тем в меньшее число типов может быть сконвертирован текущий</returns>
    private static int GetNumericTypeConversionPower(ITypeInfo type)
    {
        if (type.Equals(Builtins.Double)) return 0;
        if (type.Equals(Builtins.Float))  return 1;
        if (type.Equals(Builtins.Long) || type.Equals(Builtins.Ulong)) return 2;
        if (type.Equals(Builtins.Int) || type.Equals(Builtins.Uint)) return 3;
        if (type.Equals(Builtins.Short) || type.Equals(Builtins.Ushort)) return 4;
        if (type.Equals(Builtins.Byte) || type.Equals(Builtins.Sbyte)) return 5;
        throw new ArgumentException("Для получения потенциала конверсии числового типа передан не числовой тип.");
    }
}