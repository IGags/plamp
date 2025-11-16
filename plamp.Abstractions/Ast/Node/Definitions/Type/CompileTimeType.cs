namespace plamp.Abstractions.Ast.Node.Definitions.Type;

/// <summary>
/// Структура, определяющая тип конкретной сущности во время компиляции.
/// Нужна так как не всегда во время компиляции можно ассоциировать тип объекта с типом из clr
/// </summary>
/// <param name="ModuleName">Имя модуля, которому принадлежит тип</param>
/// <param name="TypeName">Имя типа из модуля(может отличаться от <see cref="TypeNameNode"/>)</param>
/// <param name="ClrType">Тип из clr .net, который ассоциирован с этим. null - значение до привязки, либо после привязки с ошибкой.</param>
public record struct CompileTimeType(string ModuleName, string TypeName, System.Type? ClrType);