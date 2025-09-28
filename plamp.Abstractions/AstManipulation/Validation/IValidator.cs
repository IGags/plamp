using plamp.Abstractions.Ast.Node;

namespace plamp.Abstractions.AstManipulation.Validation;

/// <summary>
/// Интерфейс обобщающий валидаторы - тип посетителей, которые не имеют права менять структуру AST. <br/>
/// Данный тип посетителя служит для валидации структуры AST. И сбора информации об AST(напр. Сигнатуры методов или объявления типов)<br/>
/// Так как такие типы посетителей не меняют структуру AST, то они могут быть запущены параллельно при компиляции.
/// </summary>
/// <typeparam name="TContext">Тип пользовательского объекта, который передаётся валидатору перед обходом.</typeparam>
public interface IValidator<TContext> where TContext : BaseVisitorContext
{
    /// <summary>
    /// Выполнение логики обхода AST без модификации его структуры.
    /// </summary>
    /// <param name="ast">Синтаксическое дерево, которое требуется обойти</param>
    /// <param name="context">Объект контекста, который передаёт необходимую для работы валидатора информацию</param>
    /// <returns>Модифицированный объект контекста. Изменения с AST не происходят.</returns>
    public TContext Validate(NodeBase ast, TContext context);
}