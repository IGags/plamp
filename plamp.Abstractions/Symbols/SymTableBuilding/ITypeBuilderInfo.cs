using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Emit;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;
using plamp.Abstractions.Symbols.SymTable;

namespace plamp.Abstractions.Symbols.SymTableBuilding;

/// <summary>
/// Расширение интерфейса для динамического построения типа во время компиляции.
/// Каждый объект этого типа ссылается на единое объявление в таблице символов для того, чтобы при компиляции типа все места, где он используется в модуле сразу получили тип из .net
/// Такой объект семантически связан с узлами AST, которые его объявляют
/// Реализации, создающие тип с явно переданным именем модуля, должны бросать <see cref="InvalidOperationException"/>,
/// если имя модуля пустое или состоит только из пробельных символов.
/// </summary>
public interface ITypeBuilderInfo : ITypeInfo
{
    /// <summary>
    /// Представление типа внутри .net clr
    /// После того, как здесь появляется значение, тип становится неизменяемым.
    /// Любые попытки вызвать методы, которые изменяют его состояние должны завершаться с <see cref="InvalidOperationException"/>
    /// </summary>
    public Type? Type { get; set; }
    
    /// <summary>
    /// Билдер типа, служит для вывода типов полей. После установки <see cref="Type"/> становится null
    /// А обращение к нему запрещается через <see cref="InvalidOperationException"/>
    /// </summary>
    public TypeBuilder? Builder { get; set; }
    
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
    
    /// <summary>
    /// Попытаться получить узел ast объявления поля внутри компилируемого типа.
    /// </summary>
    /// <param name="info">Информация о поле во время компиляции</param>
    /// <param name="defNode">В случае, если тип действительно содержит такое поле - будет получено его объявление</param>
    /// <returns>Флаг успеха поиска</returns>
    public bool TryGetDefinition(IFieldBuilderInfo info, [NotNullWhen(true)]out FieldDefNode? defNode);
}
