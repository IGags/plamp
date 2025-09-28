namespace plamp.Abstractions.Ast.Node.Unary;

/// <summary>
/// Узел AST обозначающий арифметическую операцию смены знака
/// </summary>
/// <param name="inner">Операнд</param>
public class UnaryMinusNode(NodeBase inner) : BaseUnaryNode(inner);