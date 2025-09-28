namespace plamp.Abstractions.Ast.Node.Binary;

/// <summary>
/// Узел AST операции больше
/// </summary>
/// <param name="left">Левый операнд</param>
/// <param name="right">Правый операнд</param>
public class GreaterNode(NodeBase left, NodeBase right) : BaseBinaryNode(left, right);