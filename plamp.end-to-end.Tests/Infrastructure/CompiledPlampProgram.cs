using System.Reflection;
using System.Reflection.Emit;

namespace plamp.EndToEnd.Tests.Infrastructure;

/// <summary>
/// Обёртка над скомпилированным .plp файлом, которая даёт тестам безопасную точку доступа к методам
/// </summary>
/// <param name="filePath">Полный путь к исходному .plp файлу, из которого получен модуль</param>
/// <param name="module">Reflection.Emit модуль с финализированными методами</param>
/// <param name="emittedIl">IL дамп, собранный во время эмиссии</param>
public sealed class CompiledPlampProgram(string filePath, ModuleBuilder module, string emittedIl)
{
    /// <summary>
    /// Полный путь к исходному .plp файлу
    /// </summary>
    public string FilePath { get; } = filePath;

    /// <summary>
    /// Модуль, в который <see cref="plamp.ILCodeEmitters.SymTableEmitter"/> сгенерировал методы и типы
    /// </summary>
    public ModuleBuilder Module { get; } = module;

    /// <summary>
    /// Текстовое представление IL, которое сгенерировал plamp при сборке текущего файла
    /// </summary>
    public string EmittedIl { get; } = emittedIl;

    /// <summary>
    /// Вызывает методы из скомпилированного .plp файла и возвращает сырой результат Reflection.Invoke
    /// </summary>
    /// <param name="methodName">Имя функции из .plp файла</param>
    /// <param name="arguments">Аргументы функции в порядке объявления</param>
    /// <returns>Значение, возвращённое функцией, либо null для void-методов</returns>
    /// <exception cref="MissingMethodException">Метод с указанным именем не найден в модуле</exception>
    public object? Invoke(string methodName, params object?[] arguments)
    {
        var method = GetMethod(methodName);
        try
        {
            return method.Invoke(null, arguments);
        }
        catch (Exception ex)
        {
            throw new E2ERuntimeInvocationException(FilePath, methodName, EmittedIl, ex);
        }
    }

    /// <summary>
    /// Вызывает метод и проверяет, что результат имеет ожидаемый CLR тип
    /// </summary>
    /// <typeparam name="T">Ожидаемый CLR тип результата</typeparam>
    /// <param name="methodName">Имя функции из .plp файла</param>
    /// <param name="arguments">Аргументы функции в порядке объявления</param>
    /// <returns>Типизированный результат выполнения функции</returns>
    /// <exception cref="InvalidOperationException">Метод вернул значение другого</exception>
    /// <exception cref="MissingMethodException">Метод с указанным именем не найден в модуле</exception>
    public T Invoke<T>(string methodName, params object?[] arguments)
    {
        var result = Invoke(methodName, arguments);
        return result is T typed
            ? typed
            : throw new InvalidOperationException(
                $"Метод {methodName} вернул {result?.GetType().FullName ?? "null"}, ожидался {typeof(T).FullName}.");
    }

    /// <summary>
    /// Ищет метод, который был создан после вызова <see cref="ModuleBuilder.CreateGlobalFunctions"/>.
    /// </summary>
    /// <param name="methodName">Имя функции из .plp файла</param>
    /// <exception cref="MissingMethodException">Метод с указанным именем не найден в модуле</exception>
    public MethodInfo GetMethod(string methodName)
    {
        return Module.GetMethod(methodName)
            ?? throw new MissingMethodException($"В скомпилированном файле {FilePath} не найден метод {methodName}.");
    }
}
