using System;
using plamp.Abstractions.Ast.Node.Definitions;

namespace plamp.Abstractions;

/// <summary>
/// Интерфейс-ссылка, определяющий тип конкретной сущности во время компиляции.
/// Нужен так как не всегда во время компиляции можно ассоциировать тип объекта с типом из clr.
/// Для каждого типа существует в единственном экземпляре в таблице символов.
/// </summary>
public interface ICompileTimeType : IEquatable<ICompileTimeType>
{
    /// <summary>
    /// Имя типа из модуля(может отличаться от <see cref="TypeNameNode"/>)
    /// </summary>
    public string TypeName { get; }
    
    /// <summary>
    /// Таблица описания модуля, в котором объявлен тип
    /// </summary>
    public ISymbolTable DeclaringTable { get; }

    /// <summary>
    /// Возвращает полную информацию о типе.
    /// Внимание, объект находится в единственном экземпляре во время компиляции.
    /// </summary>
    /// <returns>Информация о типе</returns>
    public TypeDefinitionInfo GetDefinitionInfo();

    /// <summary>
    /// Создаёт тип массива из текущего и автоматически добавляет его в таблицу символов.
    /// Если у оригинального типа объявлен ClrType, то у массива это поле тоже будет заполнено
    /// </summary>
    /// <returns>Ссылка на тип-массив</returns>
    public ICompileTimeType MakeArrayType();

    /// <summary>
    /// Пытается создать поле для типа.
    /// </summary>
    /// <returns>
    /// Объект-указатель на поле. Если такое поле уже есть, то вернётся дубликат.
    /// Если объявить существующее поле другого типа, то будет возвращён null.
    /// </returns>
    public ICompileTimeField? DefineField(string name, ICompileTimeType type);
}