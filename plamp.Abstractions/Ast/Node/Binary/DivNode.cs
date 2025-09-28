namespace plamp.Abstractions.Ast.Node.Binary;

/// <summary>
/// Узел AST арифметической операции деления
/// </summary>
/// <param name="left">Левый операнд</param>
/// <param name="right">Правый операнд</param>
public class DivNode(NodeBase left, NodeBase right) : BaseBinaryNode(left, right);