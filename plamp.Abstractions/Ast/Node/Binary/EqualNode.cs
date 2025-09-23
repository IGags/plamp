namespace plamp.Abstractions.Ast.Node.Binary;

/// <summary>
/// Узел AST операции сравнения
/// </summary>
/// <param name="left">Левый операнд</param>
/// <param name="right">Правый операнд</param>
public class EqualNode(NodeBase left, NodeBase right) : BaseBinaryNode(left, right);