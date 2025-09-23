namespace plamp.Abstractions.Ast.Node.Binary;

/// <summary>
/// Узел AST битовой и логической операции исключающего или
/// </summary>
/// <param name="left">Левый операнд</param>
/// <param name="right">Правый операнд</param>
public class XorNode(NodeBase left, NodeBase right) : BaseBinaryNode(left, right);