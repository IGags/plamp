using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;

namespace plamp.Abstractions.AstManipulation.Validation;

/// <summary>
/// Базовый класс не модифицирующего посетителя, который уже включает в себя логику обхода дерева разбора.<br/>
/// Для реализации своих не модифицирующих посетителей рекомендуется наследоваться от него.
/// </summary>
/// <typeparam name="TOuterContext">Пользовательский тип, который подаётся на вход. Возвращается из посетителя на выходе.</typeparam>
/// <typeparam name="TInnerContext">Внутренний тип контекста, который пробрасывается между переопределениями базовых методов посетителя.</typeparam>
public abstract class BaseValidator<TOuterContext, TInnerContext> 
    : BaseVisitor<TInnerContext>, IValidator<TOuterContext>
    where TOuterContext : BaseVisitorContext
    where TInnerContext : BaseVisitorContext
{
    
    /// <summary>
    /// Запуск процедуры обхода дерева AST с без возможности замены узлов.
    /// </summary>
    /// <param name="ast">Дерево AST, которое будет обходиться</param>
    /// <param name="context">Объект с пользовательскими данными, которые необходимы для обхода</param>
    /// <returns>Объект с пользовательскими данными. Возможна мутация или возврат другого объекта</returns>
    public virtual TOuterContext Validate(NodeBase ast, TOuterContext context)
    {
        var innerContext = CreateInnerContext(context);
        VisitNodeBase(ast, innerContext, null);
        var result = MapInnerToOuter(context, innerContext);
        return result;
    }

    /// <inheritdoc cref="BaseVisitor{TContext}.VisitNodeBase"/>
    protected sealed override VisitResult VisitNodeBase(NodeBase node, TInnerContext context, NodeBase? parent)
    {
        return base.VisitNodeBase(node, context, parent);
    }

    /// <summary>
    /// Создание внутреннего контекста, с которым будет работать посетитель при обходе.
    /// </summary>
    /// <param name="context">Объект контекста, который был передан снаружи.</param>
    /// <returns>Объект внутреннего контекста посетителя.</returns>
    protected abstract TInnerContext CreateInnerContext(TOuterContext context);
    
    /// <summary>
    /// Создание объекта выходного контекста, который будет возвращён после завершения обхода AST
    /// </summary>
    /// <param name="innerContext">Объект внутреннего контекста, который использовался при обходе.</param>
    /// <param name="outerContext">Объект внешнего контекста, который был получен при старте обхода</param>
    /// <returns>Новый объект контекста, который будет возвращён после работы посетителя</returns>
    protected abstract TOuterContext MapInnerToOuter(TOuterContext outerContext, TInnerContext innerContext);

    /// <summary>
    /// Логика создания ошибки и записи её в контекст обхода для упрощения кода наследников.
    /// </summary>
    /// <param name="node">Узел AST, на который надо добавить ошибку</param>
    /// <param name="record">Шаблон ошибки.</param>
    /// <param name="context">Контекст обхода.</param>
    protected void SetExceptionToSymbol(NodeBase node, PlampExceptionRecord record, TInnerContext context)
    {
        var exception = context.SymbolTable.SetExceptionToNode(node, record, context.FileName);
        context.Exceptions.Add(exception);
    }
}