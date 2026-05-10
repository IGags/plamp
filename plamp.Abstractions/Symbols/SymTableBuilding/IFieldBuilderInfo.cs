using System;
using System.Reflection;
using System.Reflection.Emit;
using plamp.Abstractions.Symbols.SymTable;

namespace plamp.Abstractions.Symbols.SymTableBuilding;

/// <summary>
/// Объект представляющий информацию о свойствах поля типа во время компиляции модуля в котором он объявлен.
/// Реализации, создающие поле с явно переданным именем, должны бросать <see cref="InvalidOperationException"/>,
/// если имя поля пустое или состоит только из пробельных символов.
/// Тип поля не может быть void; такие реализации должны бросать <see cref="InvalidOperationException"/>.
/// Реализации, создающие поле с явно переданным именем модуля, должны бросать <see cref="InvalidOperationException"/>,
/// если имя модуля пустое или состоит только из пробельных символов.
/// </summary>
public interface IFieldBuilderInfo : IFieldInfo
{
    /// <summary>
    /// Информация о поле типа.
    /// После задания этого поля любые попытки по изменению состояния этого класса должны завершаться с <see cref="InvalidOperationException"/>
    /// </summary>
    public FieldInfo? Field { get; set; }
    
    /// <summary>
    /// Билдер поля, служит, для того чтобы хранить не созданный тип для вывода типов полей. После установки <see cref="Field"/> становится null
    /// А обращение к нему запрещается через <see cref="InvalidOperationException"/>
    /// </summary>
    public FieldBuilder? Builder { get; set; }
}
