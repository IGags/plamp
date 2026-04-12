using System;
using System.Collections.Generic;
using System.Linq;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Definitions;

namespace plamp.Abstractions.AstManipulation.Modification;

/// <summary>
/// Базовый класс модифицирующего посетителя, который уже включает в себя логику обхода дерева разбора и его модификации.<br/>
/// Для реализации своих модифицирующих посетителей рекомендуется наследоваться от него.
/// </summary>
/// <typeparam name="TOuterContext">Пользовательский тип, который подаётся на вход. Возвращается из посетителя на выходе.</typeparam>
/// <typeparam name="TInnerContext">Внутренний тип контекста, который пробрасывается между переопределениями базовых методов посетителя.</typeparam>
public abstract class BaseWeaver<TOuterContext, TInnerContext> 
    : BaseVisitor<TInnerContext>, IWeaver<TOuterContext> 
    where TOuterContext : BaseVisitorContext 
    where TInnerContext : BaseVisitorContext
{
    /// <summary>
    /// Запуск процедуры обхода дерева AST с возможностью замены некоторых узлов по ссылке.
    /// </summary>
    /// <param name="ast">Дерево AST, которое будет обходиться</param>
    /// <param name="context">Объект с пользовательскими данными, которые необходимы для обхода</param>
    /// <returns>Объект с пользовательскими данными. Возможна мутация или возврат другого объекта</returns>
    public virtual TOuterContext WeaveDiffs(NodeBase ast, TOuterContext context)
    {
        //Перестраховка от того, что кто-то не очистил replacements
        context.WeaveReplacementDict.Clear();
        var innerContext = CreateInnerContext(context);
        VisitNodeBase(ast, innerContext, null);
        var result = MapInnerToOuter(innerContext, context);
        ProceedNodeReplacement(ast, innerContext);
        return result;
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
    protected abstract TOuterContext MapInnerToOuter(TInnerContext innerContext, TOuterContext outerContext);

    /// <summary>
    /// Метод, заменяющий узел AST, на новый в соответствии с функцией, которая создаёт этот узел
    /// </summary>
    /// <param name="from">Какой узел требуется заменить</param>
    /// <param name="toFactory">Функция, которая генерирует новый узел</param>
    /// <param name="context">Контекст обхода AST</param>
    /// <typeparam name="T">Тип исходного узла, обязан быть наследником <see cref="NodeBase"/></typeparam>
    /// <exception cref="ArgumentException">Происходит, если есть попытка заменить корневой узел(RootNode) или если в таблице символов нет узла из параметра <paramref name="from"/>.</exception>
    protected virtual void Replace<T>(T from, Func<T, NodeBase> toFactory, TInnerContext context) where T : NodeBase
    {
        if(from.GetType() == typeof(RootNode)) throw new ArgumentException("Cannot replace root node, check visitor code");
        if (!context.TranslationTable.TryGetSymbol(from, out _))
        {
            throw new ArgumentException("Symbol does not exist in table, please check parser and tree construction logic");
        }
        context.WeaveReplacementDict.Add(from, () => toFactory(from));
    }

    /// <summary>
    /// Логика создания ошибки и записи её в контекст обхода для упрощения кода наследников.
    /// </summary>
    /// <param name="node">Узел AST, на который надо добавить ошибку</param>
    /// <param name="record">Шаблон ошибки.</param>
    /// <param name="context">Контекст обхода.</param>
    protected void SetExceptionToSymbol(NodeBase node, PlampExceptionRecord record, TInnerContext context)
    {
        var exception = context.TranslationTable.SetExceptionToNode(node, record);
        context.Exceptions.Add(exception);
    }
    
    /// <summary>
    /// Логика, происходящая после основного обхода, которая заменяет узлы из вызовов метода <see cref="Replace"/>
    /// </summary>
    /// <param name="ast">Дерево AST, которое использовалось при обходе.</param>
    /// <param name="context">Контекст обхода, помогает находить позиции узлов AST в исходном файле</param>
    private void ProceedNodeReplacement(NodeBase ast, TInnerContext context)
    {
        var nodeChildren = ast.Visit();
        ProceedRecursive(nodeChildren, ast);
        context.WeaveReplacementDict.Clear();
        return;

        // Рекурсивный обход в глубину.
        void ProceedRecursive(IEnumerable<NodeBase> children, NodeBase parent)
        {
            foreach (var child in children.ToList())
            {
                var innerChildren = child.Visit();
                ProceedRecursive(innerChildren, child);
                
                if (!context.WeaveReplacementDict.TryGetValue(child, out var replacement)) continue;
                if (!context.TranslationTable.TryGetSymbol(child, out var position))
                {
                    throw new Exception("Compiler error: symbol position not found");
                }

                //Если нашли узел, который требуется заменить, то меняем его через метод класса NodeBase
                var newChild = replacement();
                parent.ReplaceChild(child, newChild);
                context.TranslationTable.RemoveSymbol(child);
                context.TranslationTable.AddSymbol(newChild, position);
            }
        }
    }
}