namespace plamp.Abstractions;

/// <summary>
/// Определение поля во время компиляции. Является ссылкой на поле внутри таблицы символов определённого модуля.
/// </summary>
/// <param name="ContainingType">Ссылка на тип, в котором это поле содержится</param>
/// <param name="Name">Имя поля.</param>
public class CompileTimeField(CompileTimeType ContainingType, string Name);