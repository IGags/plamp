namespace plamp.Abstractions.Ast.Node.Binary;

/// <summary>
/// Узел AST операции меньше
/// </summary>
/// <param name="left">Левый операнд</param>
/// <param name="right">Правый операнд</param>
public class LessNode(NodeBase left, NodeBase right) : BaseBinaryNode(left, right);