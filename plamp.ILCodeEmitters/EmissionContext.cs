using System.Reflection;
using System.Reflection.Emit;

namespace plamp.ILCodeEmitters;

/// <summary>
/// Основная модель, которая хранит состояние текущей трансляции дерева разбора в il.
/// </summary>
/// <param name="LocalVarStack">Стек видимых переменных в локальной области видимости. (видно выше - не видно глубже)</param>
/// <param name="Arguments">Список аргументов текущей функции</param>
/// <param name="Generator">Генератор il кода.</param>
/// <param name="Labels">Словарь меток перехода к il инструкциям(необходимо для циклов и условий)</param>
/// <param name="CurrentMethod">Информация о текущем создающемся методе</param>
internal record EmissionContext(
    LocalVarStack LocalVarStack,
    ParameterInfo[] Arguments,
    ILGenerator Generator,
    Dictionary<string, Label> Labels,
    MethodInfo CurrentMethod)
{
    /// <summary>
    /// Стек циклов нужен в условиях, когда мы встречаем циклы в цикле.
    /// </summary>
    private readonly Stack<CycleContext> _currentCycles = [];

    /// <summary>
    /// Вход в текущий цикл. Позволяет ассоциировать цикл с метками начала и конца.
    /// </summary>
    /// <param name="startLabel">Метка перехода к условию в цикле.</param>
    /// <param name="endLabel">Метка выхода за цикл.</param>
    public void EnterCycleContext(string startLabel, string endLabel) => _currentCycles.Push(new(startLabel, endLabel));        
    
    /// <summary>
    /// Выйти из контекста цикла. Сменит метки, с которыми ассоциирован цикл.
    /// </summary>
    public void ExitCycleContext() => _currentCycles.Pop();
    
    /// <summary>
    /// Получить метки текущего цикла.
    /// </summary>
    /// <returns>Метки текущего цикла, null - если меток нет.</returns>
    public CycleContext? GetCurrentCycleContext() => _currentCycles.Count == 0 ? null : _currentCycles.Peek();

    /// <summary>
    /// Объявить метку перехода(но не присваивать её конкретной инструкции)
    /// </summary>
    /// <returns>Человеко читаемое название метки</returns>
    public string DefineLabel()
    {
        var label = Generator.DefineLabel();
        var name = Guid.NewGuid().ToString();
        Labels[name] = label;
        return name;
    }
}