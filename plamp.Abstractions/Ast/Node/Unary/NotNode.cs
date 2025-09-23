namespace plamp.Abstractions.Ast.Node.Unary;

/// <summary>
/// Узел AST операции логического отрицания
/// </summary>
/// <param name="inner">Операнд</param>
public class NotNode(NodeBase inner) : BaseUnaryNode(inner);