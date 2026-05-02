using System;
using System.Collections.Generic;
using plamp.Abstractions.Symbols.SymTable;

namespace plamp.Alternative;

/// <summary>
/// Общая логика, которая используется при реализации дженерик типа
/// </summary>
public class GenericImplementationHelper
{
    /// <summary>
    /// Замещает дженерик параметры содержащего типа/функции в других дженерик на их реализации.
    /// Обходит тип рекурсивно и замещает совпавшие типы.
    /// </summary>
    /// <param name="openType">Тип, который требует реализации</param>
    /// <param name="typeMapping">Список соответствий тип параметра - тип реализации</param>
    /// <returns>Готовая реализация типа с замещёнными параметрами</returns>
    /// <exception cref="InvalidOperationException">Тип, которые требуется реализовать - дженерик объявление. Или не все параметры есть в таблице маппинга.</exception>
    /// <code>
    /// type A[T]{
    ///     b: T;
    /// }
    /// Превращается в
    /// type A[int]{
    ///     b: int; //За подмену типа отвечает этот метод.
    /// }
    /// </code>
    public static ITypeInfo ImplementType(
        ITypeInfo openType,
        IReadOnlyDictionary<ITypeInfo, ITypeInfo> typeMapping)
    {
        if (openType.IsGenericTypeDefinition)
            throw new InvalidOperationException("Нельзя сделать имплементацию для дженерик объявления");

        if (openType.IsGenericTypeParameter)
        {
            return typeMapping.GetValueOrDefault(openType) ??
                   throw new InvalidOperationException("Неполный маппинг типов для имплементации дженериков");
        }

        if (openType.IsArrayType)
        {
            var elemType = openType.ElementType();
            ArgumentNullException.ThrowIfNull(elemType);
            var elemImpl = ImplementType(elemType, typeMapping);
            return elemImpl.MakeArrayType();
        }

        if (openType.IsGenericType)
        {
            var openTypeDef = openType.GetGenericTypeDefinition();
            ArgumentNullException.ThrowIfNull(openTypeDef);
            var openTypeArgs = openType.GetGenericArguments();
            var implArgs = new List<ITypeInfo>();
            foreach (var argType in openTypeArgs)
            {
                implArgs.Add(ImplementType(argType, typeMapping));
            }

            var implType = openTypeDef.MakeGenericType(implArgs);
            ArgumentNullException.ThrowIfNull(implType);
            return implType;
        }

        return openType;
    }
}