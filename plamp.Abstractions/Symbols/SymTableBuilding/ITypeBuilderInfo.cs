using System.Collections.Generic;
using System.Reflection.Emit;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;
using plamp.Abstractions.Symbols.SymTable;

namespace plamp.Abstractions.Symbols.SymTableBuilding;

/// <summary>
/// Расширение интерфейса для динамического построения типа во время компиляции.
/// Каждый объект этого типа ссылается на единое объявление в таблице символов для того, чтобы при компиляции типа все места, где он используется в модуле сразу получили тип из .net
/// Такой объект семантически связан с узлами AST, которые его объявляют
/// </summary>
public interface ITypeBuilderInfo : ITypeInfo
{
    /// <summary>
    /// Билдер представления типа внутри .net clr
    /// </summary>
    public TypeBuilder? Type { get; set; }
    
    /// <summary>
    /// Список объектов - строителей полей типа
    /// </summary>
    public IReadOnlyList<IFieldBuilderInfo> FieldBuilders { get; }

    /// <summary>
    /// Список объектов - описывающих дженерик параметры типа
    /// </summary>
    public IReadOnlyList<IGenericParameterBuilder> GenericParameterBuilders { get; }
    
    /// <summary>
    /// Метод добавления поля в тип.
    /// </summary>
    /// <param name="defNode">Узел AST объявления поля типа</param>
    /// <exception cref="System.InvalidOperationException">Если такое поле уже объявлено в этом типе</exception>
    public void AddField(FieldDefNode defNode);
}