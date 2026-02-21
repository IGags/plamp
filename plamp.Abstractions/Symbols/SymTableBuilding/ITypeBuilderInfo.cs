using System.Collections.Generic;
using System.Reflection.Emit;
using plamp.Abstractions.Ast.Node.ComplexTypes;
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
    /// Ссылка на узел AST объявления типа, который изменяется при построении типа.
    /// </summary>
    public TypedefNode Definition { get; }
    
    /// <summary>
    /// Билдер представления типа внутри .net clr
    /// </summary>
    public TypeBuilder? Type { get; set; }
    
    /// <summary>
    /// Список объектов - строителей полей типа
    /// </summary>
    public IReadOnlyList<IFieldBuilderInfo> FieldBuilders { get; }
    
    /// <summary>
    /// Конструктор текущего типа - статическая функция.
    /// Которая принимает один аргумент - объект этого типа и возвращает его же с проинициализированными полями.
    /// Если поле null, то конструктор создан не будет.
    /// Для всех типов объявленных в пользовательском коде генерируется автоматически при обходе ast.
    /// Функция не видна внутри таблицы символов этого модуля.
    /// </summary>
    public IFnBuilderInfo? Constructor { get; set; }

    /// <summary>
    /// Метод добавления поля в тип.
    /// </summary>
    /// <param name="defNode">Узел AST объявления поля типа</param>
    /// <exception cref="System.InvalidOperationException">Если такое поле уже объявлено в этом типе</exception>
    public void AddField(FieldDefNode defNode);

    /// <summary>
    /// Объявление дженерик параметра у типа.
    /// Тип не может иметь коллизии имён не с одним из типов текущего модуля.
    /// Тип дженерик параметра не отображается в таблице символов модуля, поэтому если он там требуется, то его следует добавить отдельно.
    /// </summary>
    /// <param name="genericParameter">Узел ast дженерик параметра</param>
    /// <exception cref="System.InvalidOperationException">Если такой дженерик аргумент уже объявлен в типе</exception>
    /// <returns>Тип дженерик аргумента.</returns>
    public ITypeInfo AddGenericParameter(GenericDefinitionNode genericParameter);
}