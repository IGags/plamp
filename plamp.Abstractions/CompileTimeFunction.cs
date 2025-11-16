using System.Collections.Generic;

namespace plamp.Abstractions;

/// <summary>
/// Класс, определяющий ссылку на объявление функции.
/// Нужен так как не всегда во время компиляции можно ассоциировать функцию с её скомпилированным значением из clr.
/// Для каждой функции существует в единственном экземпляре в таблице символов.
/// Не потокобезопасен.
/// </summary>
/// <param name="ModuleName">Имя модуля, в котором объявлена функция.</param>
/// <param name="Name">Имя функции.</param>
/// <param name="ArgumentTypes">Список типов аргументов функции.</param>
public class CompileTimeFunction(string ModuleName, string Name, List<CompileTimeType> ArgumentTypes);