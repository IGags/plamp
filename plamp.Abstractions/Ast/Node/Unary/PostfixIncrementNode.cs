namespace plamp.Abstractions.Ast.Node.Unary;

/// <summary>
/// Узел AST определяющий операцию постфиксного инкремента
/// </summary>
/// <param name="inner">Операнд</param>
public class PostfixIncrementNode(NodeBase inner) : BaseUnaryNode(inner);