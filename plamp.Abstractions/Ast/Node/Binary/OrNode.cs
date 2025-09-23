namespace plamp.Abstractions.Ast.Node.Binary;

/// <summary>
/// Узел AST операции логического или
/// </summary>
/// <param name="left">Левый операнд</param>
/// <param name="right">Правый операнд</param>
public class OrNode(NodeBase left, NodeBase right) : BaseBinaryNode(left, right);