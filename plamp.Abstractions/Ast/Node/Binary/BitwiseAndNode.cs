namespace plamp.Abstractions.Ast.Node.Binary;

/// <summary>
/// Узел AST операции побитового И
/// </summary>
/// <param name="left">Левый операнд</param>
/// <param name="right">Правый операнд</param>
public class BitwiseAndNode(NodeBase left, NodeBase right) : BaseBinaryNode(left, right);