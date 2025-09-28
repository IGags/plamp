using plamp.Abstractions.Ast.Node;

namespace plamp.Abstractions.AstManipulation.Modification;

/// <summary>
/// Интерфейс, который определяет модифицирующий тип посетителя - ткача.
/// Этот тип модифицирует существующее дерево разбора.
/// Поэтому компилятор должен запускать такие преобразования всегда в 1 потоке.
/// </summary>
/// <typeparam name="TContext">Тип пользовательского объекта, который будет использоваться при обходе</typeparam>
public interface IWeaver<TContext> where TContext : BaseVisitorContext
{
    /// <summary>
    /// Изменить дерево AST.
    /// </summary>
    /// <param name="ast">Дерево разбора программы(AST)</param>
    /// <param name="context">Пользовательский объект, который будет использоваться при обходе AST</param>
    /// <returns>Модифицированный объект контекста. Изменения происходят с AST по ссылке.</returns>
    public TContext WeaveDiffs(NodeBase ast, TContext context);
}