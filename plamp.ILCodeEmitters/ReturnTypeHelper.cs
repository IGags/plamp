using plamp.Abstractions.Symbols.SymTable;

namespace plamp.ILCodeEmitters;

/// <summary>
/// Вспомогательные методы для отображения списка возвращаемых типов языка в CLR
/// </summary>
internal static class ReturnTypeHelper
{
    /// <summary>
    /// Возвращает CLR тип результата функции
    /// </summary>
    /// <param name="returnTypes">Типы возвращаемых значений на уровне языка.</param>
    /// <returns>void для функции без результата или тип последнего возвращаемого значения.</returns>
    public static Type GetClrReturnType(IReadOnlyList<ITypeInfo> returnTypes) =>
        returnTypes.Count == 0 ? typeof(void) : returnTypes[^1].AsType();

    /// <summary>
    /// Возвращает типы out параметров результата функции
    /// </summary>
    /// <param name="returnTypes">Типы возвращаемых значений на уровне языка.</param>
    /// <returns>Типы всех возвращаемых значений кроме последнего, преобразованные в by-ref типы.</returns>
    public static IReadOnlyList<Type> GetOutReturnParameterTypes(IReadOnlyList<ITypeInfo> returnTypes) =>
        returnTypes.Take(Math.Max(0, returnTypes.Count - 1))
            .Select(x => x.AsType().MakeByRefType())
            .ToList();
}
