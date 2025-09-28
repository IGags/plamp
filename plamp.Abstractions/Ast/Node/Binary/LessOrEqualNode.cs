namespace plamp.Abstractions.Ast.Node.Binary;

/// <summary>
/// Узел AST операции меньше или равно
/// </summary>
/// <param name="left">Левый операнд</param>
/// <param name="right">Правый операнд</param>
public class LessOrEqualNode(NodeBase left, NodeBase right) : BaseBinaryNode(left, right);