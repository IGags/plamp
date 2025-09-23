namespace plamp.Abstractions.Ast.Node.Binary;

/// <summary>
/// Узел AST операции больше или равно
/// </summary>
/// <param name="left">Левый операнд</param>
/// <param name="right">Правый операнд</param>
public class GreaterOrEqualNode(NodeBase left, NodeBase right) : BaseBinaryNode(left, right);