using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Emit;

namespace plamp.ILCodeEmitters;

/// <summary>
/// Внутренняя модель области видимости переменных внутри текущей функции.
/// </summary>
internal class LocalVarStack
{
    /// <summary>
    /// "Стопка" областей видимостей переменных от самых внешней до самых вложенных
    /// </summary>
    private readonly Stack<List<KeyValuePair<string, LocalBuilder>>> _localVars = new();

    /// <summary>
    /// Общий пул переменных видимых в текущей области.
    /// </summary>
    private readonly Dictionary<string, LocalBuilder> _vars = [];

    /// <summary>
    /// Список переменных, которые объявлены в ближайшей области видимости.
    /// </summary>
    private List<KeyValuePair<string, LocalBuilder>> _currentScope = [];

    /// <summary>
    /// Создать "стопку" областей видимости. Создаётся сразу с корневой области.
    /// </summary>
    public LocalVarStack() => _localVars.Push([]);

    /// <summary>
    /// Войти во вложенную область видимости.
    /// </summary>
    public void BeginScope()
    {
        _currentScope = [];
        _localVars.Push(_currentScope);
    }

    /// <summary>
    /// Выйти из области видимости.
    /// </summary>
    public void EndScope()
    {
        Debug.Assert(_localVars.Count > 1);
        var values = _localVars.Pop();
        foreach (var varName in values)
        {
            _vars.Remove(varName.Key);
        }

        _currentScope = _localVars.Peek();
    }
    
    /// <summary>
    /// Проверить, что переменная есть в текущей области видимости.
    /// (Это достаточно бессмысленно, так как это должен делать семантический анализатор)
    /// </summary>
    /// <param name="name">Название переменной.</param>
    /// <returns>True - переменная есть в текущей области иначе false.</returns>
    public bool Contains(string name) => _vars.ContainsKey(name);

    /// <summary>
    /// Добавить переменную в область видимости. Может бросить ошибку, если переменная уже существует.
    /// </summary>
    /// <param name="name">Имя переменной</param>
    /// <param name="type">Тип переменной</param>
    public void Add(string name, LocalBuilder type)
    {
        _vars.Add(name, type);
        _currentScope.Add(new (name, type));
    }
    
    /// <summary>
    /// Получить указатель на переменную <see cref="System.Reflection.Emit.LocalBuilder"/>
    /// </summary>
    /// <param name="name">Имя переменной</param>
    /// <param name="builder">Указатель на переменную в контексте создания инструкций текущей функции</param>
    /// <returns>True - если переменная найдена было, иначе false.</returns>
    public bool TryGetValue(string name, [MaybeNullWhen(false)]out LocalBuilder builder) => _vars.TryGetValue(name, out builder);
}