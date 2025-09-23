namespace plamp.Abstractions.Ast.Node.Binary;

/// <summary>
/// Узел AST арифметической операции вычитания
/// </summary>
/// <param name="left">Левый операнд</param>
/// <param name="right">Правый операнд</param>
public class SubNode(NodeBase left, NodeBase right) : BaseBinaryNode(left, right);