namespace plamp.Abstractions.Ast.Node.Binary;

/// <summary>
/// Узел AST обозначающий операцию суммирования
/// </summary>
/// <param name="left">Левый операнд</param>
/// <param name="right">Правый операнд</param>
public class AddNode(NodeBase left, NodeBase right) : BaseBinaryNode(left, right);