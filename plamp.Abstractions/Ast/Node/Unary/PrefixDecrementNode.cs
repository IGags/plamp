namespace plamp.Abstractions.Ast.Node.Unary;

/// <summary>
/// Узел AST обозначающий префиксный декремент
/// </summary>
/// <param name="inner">Операнд</param>
public class PrefixDecrementNode(NodeBase inner) : BaseUnaryNode(inner);