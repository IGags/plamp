namespace plamp.Abstractions.Ast.Node.Binary;

/// <summary>
/// Узел AST арифметической операции умножения
/// </summary>
/// <param name="left">Левый операнд</param>
/// <param name="right">Правый операнд</param>
public class MulNode(NodeBase left, NodeBase right) : BaseBinaryNode(left, right);