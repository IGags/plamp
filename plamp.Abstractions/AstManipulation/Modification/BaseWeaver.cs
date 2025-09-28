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
    /// Словарь замен, которые выполнил код посетителя. Все замены происходят после окончания обхода AST.
    /// Это значит, что повстречать удел, на который заменили далее при обходе невозможно. 
    /// </summary>
    protected Dictionary<NodeBase, NodeBase> ReplacementDict { get; } = [];
    
    /// <summary>
    /// Запуск процедуры обхода дерева AST с возможностью замены некоторых узлов по ссылке.
    /// </summary>
    /// <param name="ast">Дерево AST, которое будет обходиться</param>
    /// <param name="context">Объект с пользовательскими данными, которые необходимы для обхода</param>
    /// <returns>Объект с пользовательскими данными. Возможна мутация или возврат другого объекта</returns>
    public virtual TOuterContext WeaveDiffs(NodeBase ast, TOuterContext context)
    {
        var innerContext = CreateInnerContext(context);
        VisitNodeBase(ast, innerContext, null);
        var result = MapInnerToOuter(innerContext, context);
        ProceedNodeReplacement(ast);
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
    protected abstract TOuterContext MapInnerToOuter(TInnerContext innerContext, TOuterContext outerContext);

    /// <summary>
    /// Метод замены узла, который используется внутри классов-наследников
    /// </summary>
    /// <param name="from">Какой узел требуется заменить</param>
    /// <param name="to">На какой узел требуется заменить</param>
    /// <param name="context">Контекст обхода AST</param>
    /// <exception cref="ArgumentException">Происходит, если есть попытка заменить корневой узел(RootNode) или если в таблице символов нет узла из параметра <paramref name="from"/>.</exception>
    protected virtual void Replace(NodeBase from, NodeBase to, TInnerContext context)
    {
        if(from.GetType() == typeof(RootNode)) throw new ArgumentException("Cannot replace root node, check visitor code");
        if (!context.SymbolTable.TryGetSymbol(from, out var pair))
        {
            throw new ArgumentException("Symbol does not exists in table, please check parser and tree construction logic");
        }
        
        //Immediate symbol addition may create a memory leak, possible need create another variation of symbol table with unused nodes cleanup 
        context.SymbolTable.AddSymbol(to, pair.Key, pair.Value);
        ReplacementDict.Add(from, to);
    }

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

    /// <summary>
    /// Логика, происходящая после основного обхода, которая заменяет узлы из вызовов метода <see cref="Replace"/>
    /// </summary>
    /// <param name="ast">Дерево AST, которое использовалось при обходе.</param>
    private void ProceedNodeReplacement(NodeBase ast)
    {
        var nodeChildren = ast.Visit();
        ProceedRecursive(nodeChildren, ast);
        return;

        // Рекурсивный обход в глубину.
        void ProceedRecursive(IEnumerable<NodeBase> children, NodeBase parent)
        {
            foreach (var child in children.ToList())
            {
                var innerChildren = child.Visit();
                ProceedRecursive(innerChildren, child);
                
                //Если нашли узел, который требуется заменить, то меняем его через метод класса NodeBase
                if (ReplacementDict.TryGetValue(child, out var replacement))
                {
                    parent.ReplaceChild(child, replacement);
                }
            }
        }
    }
}