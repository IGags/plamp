namespace plamp.Abstractions;

/// <summary>
/// Определение поля во время компиляции. Является ссылкой на поле внутри таблицы символов определённого модуля.
/// </summary>
public interface ICompileTimeField
{
    /// <summary>
    /// Ссылка на тип, в котором это поле содержится
    /// </summary>
    public ICompileTimeType ContainingType { get; }
    
    /// <summary>
    /// Имя поля.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Получить информацию об объявлении поля.
    /// </summary>
    /// <returns>Информация об объявлении поля.</returns>
    public FieldDefinitionInfo GetDefinitionInfo();
}