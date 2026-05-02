using System;
using System.Reflection;
using System.Reflection.Emit;
using plamp.Abstractions.Symbols.SymTable;

namespace plamp.Abstractions.Symbols.SymTableBuilding;

public interface IFieldBuilderInfo : IFieldInfo
{
    /// <summary>
    /// Информация о поле типа.
    /// После задания этого поля любые попытки по изменению состояния этого класса должны завершаться с <see cref="InvalidOperationException"/>
    /// </summary>
    public FieldInfo? Field { get; set; }
    
    /// <summary>
    /// Билдер поля, служит, для того чтобы хранить недосозданный тип для вывода типов полей. После установки <see cref="Field"/> становится null
    /// А обращение к нему запрещается через <see cref="InvalidOperationException"/>
    /// </summary>
    public FieldBuilder? Builder { get; set; }
}