namespace plamp.Abstractions.Ast.Node.Unary;

/// <summary>
/// Узел AST операции постфиксного декремента
/// </summary>
/// <param name="inner">Операнд</param>
public class PostfixDecrementNode(NodeBase inner) : BaseUnaryNode(inner);