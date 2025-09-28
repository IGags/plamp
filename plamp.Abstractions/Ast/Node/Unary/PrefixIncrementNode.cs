namespace plamp.Abstractions.Ast.Node.Unary;

/// <summary>
/// Узел AST обозначающий префиксный инкремент
/// </summary>
/// <param name="inner">Операнд</param>
public class PrefixIncrementNode(NodeBase inner) : BaseUnaryNode(inner);