namespace plamp.Abstractions.Ast.Node.Binary;

/// <summary>
/// Узел AST операции логического И
/// </summary>
/// <param name="left">Левый операнд</param>
/// <param name="right">Правый операнд</param>
public class AndNode(NodeBase left, NodeBase right) : BaseBinaryNode(left, right);