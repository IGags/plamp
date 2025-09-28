namespace plamp.Abstractions.Ast.Node.Binary;

/// <summary>
/// Узел AST операции получения остатка от деления
/// </summary>
/// <param name="left">Левый операнд</param>
/// <param name="right">Правый операнд</param>
public class ModuloNode(NodeBase left, NodeBase right) : BaseBinaryNode(left, right);