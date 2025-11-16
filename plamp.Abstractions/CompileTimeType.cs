using plamp.Abstractions.Ast.Node.Definitions;

namespace plamp.Abstractions;

/// <summary>
/// Класс, определяющий тип конкретной сущности во время компиляции.
/// Нужен так как не всегда во время компиляции можно ассоциировать тип объекта с типом из clr.
/// Для каждого типа существует в единственном экземпляре в таблице символов.
/// Не потокобезопасен.
/// </summary>
/// <param name="ModuleName">Имя модуля, которому принадлежит тип</param>
/// <param name="TypeName">Имя типа из модуля(может отличаться от <see cref="TypeNameNode"/>)</param>
public record CompileTimeType(string ModuleName, string TypeName);