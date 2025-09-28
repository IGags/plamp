namespace plamp.Abstractions.Ast.Node.Binary;

/// <summary>
/// Узел AST операции не равно
/// </summary>
/// <param name="left">Левый операнд</param>
/// <param name="right">Правый операнд</param>
public class NotEqualNode(NodeBase left, NodeBase right) : BaseBinaryNode(left, right);