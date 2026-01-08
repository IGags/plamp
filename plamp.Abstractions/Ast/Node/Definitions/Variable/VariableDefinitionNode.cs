using System.Collections.Generic;
using plamp.Abstractions.Ast.Node.Definitions.Type;

namespace plamp.Abstractions.Ast.Node.Definitions.Variable;

/// <summary>
/// Узел AST обозначающий объявление переменной или их списка.
/// </summary>
public class VariableDefinitionNode : NodeBase
{
    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="type">Ссылка на тип, значение которого сохраняется в переменной.</param>
    /// <param name="name">Имя переменной</param>
    public VariableDefinitionNode(TypeNode? type, VariableNameNode name)
    {
        Type = type;
        Name = name;
    }
    
    /// <summary>
    /// Ссылка на тип, значение которого сохраняется в переменной.<br/>
    /// Может быть null, когда вывод типов столкнулся с семантикой вида a := b, но при этом a не была объявлена ни разу до этого.<br/>
    /// В случае невозможности вывести тип null останется и компиляция прекратится.<br/>
    /// На этапе эмиссии кода в IL значение должно быть.
    /// </summary>
    public TypeNode? Type { get; private set; }
    
    /// <summary>
    /// Имя переменной
    /// </summary>
    public VariableNameNode Name { get; private set; }

    /// <inheritdoc cref="NodeBase"/>
    public override IEnumerable<NodeBase> Visit()
    {
        if(Type != null) yield return Type;
        yield return Name;
    }

    /// <inheritdoc cref="NodeBase"/>
    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        if (Type == child && newChild is TypeNode newType) Type = newType;
        if (Name == child && newChild is VariableNameNode varNameChild) Name = varNameChild;
    }
}