using System;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Binary;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ComplexTypes;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Abstractions.Ast.Node.Definitions.Variable;
using plamp.Abstractions.Ast.Node.Unary;

namespace plamp.Abstractions.Ast;

/// <summary>
/// Базовый класс для всех посетителей абстрактных деревьев кода в компилляторе.<br/>
/// Совершает обход AST в глубину.<br/>
/// Для каждого типа синтаксического узла есть метод, который вызывается перед посещением потомков.<br/>
/// Также для каждого типа узла есть метод, который вызывает после посещения его потомков.
/// </summary>
/// <typeparam name="TContext">
/// Тип объекта, который пробрасывается переопределённые методы у классов-наследников.<br/>
/// Нужен для переноса сотояния между вызовами и хранения глобального состояния обхода AST.<br/>
/// </typeparam>
//TODO: Possible do with stack if will StackOverflow occurs
public abstract class BaseVisitor<TContext>
{
    /// <summary>
    /// Перечисление, которое определяет поведение обхода после выхода из переопределённого метода класса-наследника
    /// </summary>
    protected enum VisitResult
    {
        /// <summary>
        /// Продолжать обход в глубину(дефолтное поведение)
        /// </summary>
        Continue,
        /// <summary>
        /// Полностью прервать обход
        /// </summary>
        Break,
        /// <summary>
        /// Пропустить потомков этого синтаксического узла
        /// </summary>
        SkipChildren
    }

    /// <summary>
    /// Метод, способный посетить всех потомков конкретного узла.<br/>
    /// Иногда бывает полезно вызвать его, потом вернуть <see cref="VisitResult.SkipChildren"/> результат.
    /// </summary>
    /// <param name="node">Узел, потомков которого следует посетить</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    protected virtual VisitResult VisitChildren(NodeBase node, TContext context)
    {
        foreach (var child in node.Visit())
        {
            var preVisitRes = VisitResult.Continue;
            /*
             * Для узла типа Body также есть логика посещения каждой отдельной инструкции помимо логики посещения самого узла.
             * Например:
             * {
             *      a := 1;
             *      b := 2;
             * }
             * 
             * Pre visit body
             *      Pre visit instruction
             *      Pre visit assign
             *      ...
             *      Post visit assign
             *      Post visit instruction
             * 
             *      Pre visit instruction
             *      Pre visit assign
             *      ...
             *      Post visit assign
             *      Post visit instruction
             * Post visit body
             *
             * Далее в коде применяется такой же подход для унарных и бинарных операторов
             */
            if (node is BodyNode)
            {
                preVisitRes = PreVisitInstruction(child, context, node);
                if (preVisitRes == VisitResult.Break) return VisitResult.Break;
            }
            
            //Если на каком-то этапе происходит прерывание, то полностью заканчиваем.
            var visitRes = VisitResult.Continue;
            if(preVisitRes is not VisitResult.SkipChildren) visitRes = VisitNodeBase(child, context, node);
            if (visitRes is VisitResult.Break) return VisitResult.Break;
            
            
            if (node is BodyNode) visitRes = PostVisitInstruction(child, context, node);
            if (visitRes == VisitResult.Break) return VisitResult.Break;
        }
        return VisitResult.Continue;
    }
    
    /// <summary>
    /// Выбор метода для обработки посещения узла AST конкретного типа
    /// </summary>
    /// <param name="node">Узел, который требуется посетить вместе с потомками</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел. Иногда требуется так как узел одного типа может иметь родительский узел разных типов.</param>
    protected virtual VisitResult VisitNodeBase(NodeBase node, TContext context, NodeBase? parent)
    {
        switch (node)
        {
            case RootNode rootNode:
                return VisitCore(rootNode, context, parent, PreVisitRoot, PostVisitRoot);
            case ModuleDefinitionNode moduleDefinition:
                return VisitCore(moduleDefinition, context, parent, PreVisitModuleDefinition, PostVisitModuleDefinition);
            case ImportNode importNode:
                return VisitCore(importNode, context, parent, PreVisitImport, PostVisitImport);
            case ImportItemNode importItem:
                return VisitCore(importItem, context, parent, PreVisitImportItem, PostVisitImportItem);
            case BodyNode bodyNode:
                return VisitCore(bodyNode, context, parent, PreVisitBody, PostVisitBody);
            case ConditionNode conditionNode:
                return VisitCore(conditionNode, context, parent, PreVisitCondition, PostVisitCondition);
            case FuncNode defNode:
                return VisitCore(defNode, context, parent, PreVisitFunction, PostVisitFunction);
            case WhileNode whileNode:
                return VisitCore(whileNode, context, parent, PreVisitWhile, PostVisitWhile);
            case BreakNode breakNode:
                return VisitCore(breakNode, context, parent, PreVisitBreak, PostVisitBreak);
            case ContinueNode continueNode:
                return VisitCore(continueNode, context, parent, PreVisitContinue, PostVisitContinue);
            case ReturnNode returnNode:
                return VisitCore(returnNode, context, parent, PreVisitReturn, PostVisitReturn);
            case CallNode callNode:
                return VisitCore(callNode, context, parent, PreVisitCall, PostVisitCall);
            case CastNode castNode:
                return VisitCore(castNode, context, parent, PreVisitCast, PostVisitCast);
            case ConstructorCallNode constructorNode:
                return VisitCore(constructorNode, context, parent, PreVisitConstructor, PostVisitConstructor);
            case EmptyNode emptyNode:
                return VisitCore(emptyNode, context, parent, PreVisitEmpty, PostVisitEmpty);
            case MemberNode memberNode:
                return VisitCore(memberNode, context, parent, PreVisitMember, PostVisitMember);
            case ParameterNode parameterNode:
                return VisitCore(parameterNode, context, parent, PreVisitParameter, PostVisitParameter);
            case TypeNode typeNode:
                return VisitCore(typeNode, context, parent, PreVisitType, PostVisitType);
            case VariableDefinitionNode variableDefinitionNode:
                return VisitCore(variableDefinitionNode, context, parent, PreVisitVariableDefinition, PostVisitVariableDefinition);
            case MemberAccessNode memberAccessNode:
                return VisitCore(memberAccessNode, context, parent, PreVisitMemberAccess, PostVisitMemberAccess);
            case ThisNode thisNode:
                return VisitCore(thisNode, context, parent, PreVisitThis, PostVisitThis);
            case LiteralNode constNode:
                return VisitCore(constNode, context, parent, PreVisitLiteral, PostVisitLiteral);
            case TypeNameNode typeName:
                return VisitCore(typeName, context, parent, PreVisitTypeName, PostVisitTypeName);
            case FuncNameNode funcName:
                return VisitCore(funcName, context, parent, PreVisitFuncName, PostVisitFuncName);
            case ParameterNameNode parameterName:
                return VisitCore(parameterName, context, parent, PreVisitParameterName, PostVisitParameterName);
            case VariableNameNode variableName:
                return VisitCore(variableName, context, parent, PreVisitVariableName, PostVisitVariableName);
            case FuncCallNameNode funcCallName:
                return VisitCore(funcCallName, context, parent, PreVisitFuncCallName, PostVisitFuncCallName);
            case InitArrayNode initArray:
                return VisitCore(initArray, context, parent, PreVisitInitArray, PostVisitInitArray);
            case ArrayTypeSpecificationNode arrayRank:
                return VisitCore(arrayRank, context, parent, PreVisitArrayTypeSpecification, PostVisitArrayTypeSpecification);
            case ElemGetterNode elemGetter:
                return VisitCore(elemGetter, context, parent, PreVisitElemGetter, PostVisitElemGetter);
            case ElemSetterNode elemSetter:
                return VisitCore(elemSetter, context, parent, PreVisitElemSetter, PostVisitElemSetter);
            case ArrayIndexerNode arrayIndexerNode:
                return VisitCore(arrayIndexerNode, context, parent, PreVisitArrayIndexer, PostVisitArrayIndexer);
            case BaseBinaryNode binaryNode:
                return VisitBinaryExpression(binaryNode, context, parent);
            case BaseUnaryNode unaryNode:
                return VisitUnaryNode(unaryNode, context, parent);
        }
        
        return VisitCore(node, context, parent, PreVisitDefault, PostVisitDefault);
    }

    /// <summary>
    /// Вызывается перед посещением каждого унарного оператора. Может прервать вызов конкретного обработчика в случае <see cref="VisitResult.Break"/>
    /// </summary>
    /// <param name="unaryNode">Базовый класс всех унарных операторов.</param>
    /// <param name="context">Контекст конкретного посетителя.</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitUnary(BaseUnaryNode unaryNode, TContext context, NodeBase? parent) => VisitResult.Continue;

    /// <summary>
    /// Вызывается после посещения каждого унарного оператора, если до этого никакой из внутренних методов не вернул <see cref="VisitResult.Break"/>
    /// </summary>
    /// <param name="unaryNode">Базовый класс всех унарных операторов.</param>
    /// <param name="context">Контекст конкретного посетителя.</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitUnary(BaseUnaryNode unaryNode, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Выбор метода для обработки посещения узла-наследника базового унарного оператора конкретного типа.
    /// </summary>
    /// <param name="unaryNode">Узел унарного оператора, который требуется посетить вместе с потомками</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult VisitUnaryNode(BaseUnaryNode unaryNode, TContext context, NodeBase? parent)
    {
        var preVisitRes = PreVisitUnary(unaryNode, context, parent);
        if (preVisitRes is VisitResult.Break) return VisitResult.Break;
        var visitRes = VisitResult.Continue;
        if (preVisitRes is not VisitResult.SkipChildren)
        {
            visitRes = unaryNode switch
            {
                NotNode notNode => VisitCore(notNode, context, parent, PreVisitNot, PostVisitNot),
                PostfixDecrementNode postfixDecrement => VisitCore(postfixDecrement, context, parent,
                    PreVisitPostfixDecrement, PostVisitPostfixDecrement),
                PostfixIncrementNode postfixIncrement => VisitCore(postfixIncrement, context, parent,
                    PreVisitPostfixIncrement, PostVisitPostfixIncrement),
                PrefixDecrementNode prefixDecrementNode => VisitCore(prefixDecrementNode, context, parent,
                    PreVisitPrefixDecrement, PostVisitPrefixDecrement),
                PrefixIncrementNode prefixIncrementNode => VisitCore(prefixIncrementNode, context, parent,
                    PreVisitPrefixIncrement, PostVisitPrefixIncrement),
                UnaryMinusNode unaryMinusNode => VisitCore(unaryMinusNode, context, parent, PreVisitUnaryMinus,
                    PostVisitUnaryMinus),
                _ => VisitCore(unaryNode, context, parent, PreVisitDefault, PostVisitDefault)
            };
        }

        return visitRes is VisitResult.Break ? VisitResult.Break : PostVisitUnary(unaryNode, context, parent);
    }
    
    /// <summary>
    /// Вызывается перед посещением каждого бинарного оператора. Может прервать вызов конкретного обработчика в случае <see cref="VisitResult.Break"/>
    /// </summary>
    /// <param name="node">Базовый класс всех бинарных операторов.</param>
    /// <param name="context">Контекст конкретного посетителя.</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitBinary(BaseBinaryNode node, TContext context, NodeBase? parent) => VisitResult.Continue;

    /// <summary>
    /// Вызывается после посещения каждого бинарного оператора, если до этого никакой из внутренних методов не вернул <see cref="VisitResult.Break"/>
    /// </summary>
    /// <param name="node">Базовый класс всех бинарных операторов.</param>
    /// <param name="context">Контекст конкретного посетителя.</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitBinary(BaseBinaryNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Выбор метода для обработки посещения узла-наследника базового бинарного оператора конкретного типа.
    /// </summary>
    /// <param name="node">Узел бинарного оператора, который требуется посетить вместе с потомками</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult VisitBinaryExpression(BaseBinaryNode node, TContext context, NodeBase? parent)
    {
        var preVisitRes = PreVisitBinary(node, context, parent);
        if (preVisitRes is VisitResult.Break) return VisitResult.Break;
        var visitRes = VisitResult.Continue;
        if (preVisitRes is not VisitResult.SkipChildren)
        {
            visitRes = node switch
            {
                BaseAssignNode baseAssignNode => VisitBaseAssign(baseAssignNode, context, parent),
                BitwiseAndNode bitwiseAndNode => VisitCore(bitwiseAndNode, context, parent, PreVisitBitwiseAnd,
                    PostVisitBitwiseAnd),
                BitwiseOrNode bitwiseOrNode => VisitCore(bitwiseOrNode, context, parent, PreVisitBitwiseOr,
                    PostVisitBitwiseOr),
                XorNode xorNode => VisitCore(xorNode, context, parent, PreVisitXor, PostVisitXor),
                AndNode andNode => VisitCore(andNode, context, parent, PreVisitAnd, PostVisitAnd),
                DivNode divideNode => VisitCore(divideNode, context, parent, PreVisitDiv, PostVisitDiv),
                EqualNode equalNode => VisitCore(equalNode, context, parent, PreVisitEqual, PostVisitEqual),
                GreaterNode greaterNode => VisitCore(greaterNode, context, parent, PreVisitGreater, PostVisitGreater),
                GreaterOrEqualNode greaterOrEqualsNode => VisitCore(greaterOrEqualsNode, context, parent,
                    PreVisitGreaterOrEquals, PostVisitGreaterOrEquals),
                LessNode lessNode => VisitCore(lessNode, context, parent, PreVisitLess, PostVisitLess),
                LessOrEqualNode lessOrEqualNode => VisitCore(lessOrEqualNode, context, parent, PreVisitLessOrEqual,
                    PostVisitLessOrEqual),
                SubNode minusNode => VisitCore(minusNode, context, parent, PreVisitSub, PostVisitSub),
                ModuloNode moduloNode => VisitCore(moduloNode, context, parent, PreVisitModulo, PostVisitModulo),
                MulNode multiplyNode => VisitCore(multiplyNode, context, parent, PreVisitMul, PostVisitMul),
                NotEqualNode notEqualNode => VisitCore(notEqualNode, context, parent, PreVisitNotEqual,
                    PostVisitNotEqual),
                OrNode orNode => VisitCore(orNode, context, parent, PreVisitOr, PostVisitOr),
                AddNode plusNode => VisitCore(plusNode, context, parent, PreVisitAdd, PostVisitAdd),
                _ => VisitCore(node, context, parent, PreVisitDefault, PostVisitDefault)
            };
        }

        return visitRes is VisitResult.Break ? VisitResult.Break : PostVisitBinary(node, context, parent);
    }

    /// <summary>
    /// Выбор метода для обработки посещения узла-наследника базового оператора присваивания.
    /// </summary>
    /// <param name="node">Базовый узел присваивания</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult VisitBaseAssign(BaseAssignNode node, TContext context, NodeBase? parent)
    {
        return node switch
        {
            AssignNode assignNode => VisitCore(assignNode, context, parent, PreVisitAssign, PostVisitAssign),
            _ => VisitCore(node, context, parent, PreVisitDefault, PostVisitDefault)
        };
    }

    /// <summary>
    /// Вызов перед посещением узла-указателя на источник вызова функции
    /// </summary>
    /// <param name="thisNode">Узел указатель</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitThis(ThisNode thisNode, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов после посещения узла-указателя на источник вызова функции
    /// </summary>
    /// <param name="thisNode">Узел указатель</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitThis(ThisNode thisNode, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов перед посещением узла присваивания
    /// </summary>
    /// <param name="node">Узел присваивания</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitAssign(AssignNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов после посещения узла присваивания
    /// </summary>
    /// <param name="node">Узел присваивания</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitAssign(AssignNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов перед посещением узла логического И
    /// </summary>
    /// <param name="node">Логическое И</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitAnd(AndNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов после посещения узла логического И
    /// </summary>
    /// <param name="node">Логическое И</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitAnd(AndNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов перед посещением узла деления
    /// </summary>
    /// <param name="node">Узел деления</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitDiv(DivNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов после посещения узла деления
    /// </summary>
    /// <param name="node">Узел деления</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitDiv(DivNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов перед посещением узла сравнения
    /// </summary>
    /// <param name="node">Узел сравнения</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitEqual(EqualNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов после посещения узла сравнения
    /// </summary>
    /// <param name="node">Узел сравнения</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitEqual(EqualNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов перед посещением узла больше
    /// </summary>
    /// <param name="node">Узел больше</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitGreater(GreaterNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов после посещения узла больше
    /// </summary>
    /// <param name="node">Узел больше</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitGreater(GreaterNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов перед посещением узла больше или равно
    /// </summary>
    /// <param name="node">Узел больше или равно</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitGreaterOrEquals(GreaterOrEqualNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов после посещения узла больше или равно
    /// </summary>
    /// <param name="node">Узел больше или равно</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitGreaterOrEquals(GreaterOrEqualNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов перед посещением узла меньше
    /// </summary>
    /// <param name="node">Узел меньше</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitLess(LessNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов после посещения узла меньше
    /// </summary>
    /// <param name="node">Узел меньше</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitLess(LessNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов перед посещением узла меньше или равно
    /// </summary>
    /// <param name="node">Узел меньше или равно</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitLessOrEqual(LessOrEqualNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов после посещения узла меньше или равно
    /// </summary>
    /// <param name="node">Узел меньше или равно</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitLessOrEqual(LessOrEqualNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов перед посещением узла вычитания
    /// </summary>
    /// <param name="node">Узел вычитания</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitSub(SubNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов после посещения узла вычитания
    /// </summary>
    /// <param name="node">Узел вычитания</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitSub(SubNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов перед посещением узла получения остатка от деления
    /// </summary>
    /// <param name="node">Узел получения остатка от деления</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitModulo(ModuloNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов после посещения узла получения остатка от деления
    /// </summary>
    /// <param name="node">Узел получения остатка от деления</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitModulo(ModuloNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов перед посещением узла умножения.
    /// </summary>
    /// <param name="node">Узел умножения</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitMul(MulNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов после посещения узла умножения
    /// </summary>
    /// <param name="node">Узел умножения</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitMul(MulNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов перед посещением узла не равно
    /// </summary>
    /// <param name="node">Узел не равно</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitNotEqual(NotEqualNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов после посещения узла не равно
    /// </summary>
    /// <param name="node">Узел не равно</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitNotEqual(NotEqualNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов перед посещением узла логического ИЛИ
    /// </summary>
    /// <param name="node">Узел логического ИЛИ</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitOr(OrNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов после посещения узла логического ИЛИ
    /// </summary>
    /// <param name="node">Узел логического ИЛИ</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitOr(OrNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов перед посещением узла суммирования
    /// </summary>
    /// <param name="node">Узел суммирования</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitAdd(AddNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов после посещения узла суммирования
    /// </summary>
    /// <param name="node">Узел суммирования</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitAdd(AddNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов перед посещением узла тела(блок инструкций)
    /// </summary>
    /// <param name="node">Узел блока инструкций</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitBody(BodyNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов после посещения узла тела(блок инструкций)
    /// </summary>
    /// <param name="node">Узел блока инструкций</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitBody(BodyNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов перед посещением объявления условия
    /// </summary>
    /// <param name="node">Узел условия</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitCondition(ConditionNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов после посещения объявления условия
    /// </summary>
    /// <param name="node">Узел условия</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitCondition(ConditionNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов перед посещением объявления функции
    /// </summary>
    /// <param name="node">Узел объявления функции</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitFunction(FuncNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов после посещения объявления функции
    /// </summary>
    /// <param name="node">Узел объявления функции</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitFunction(FuncNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов перед объявлением цикла с предусловием
    /// </summary>
    /// <param name="node">Узел объявления цикла с предусловием</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitWhile(WhileNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов после объявления цикла с предусловием
    /// </summary>
    /// <param name="node">Узел объявления цикла с предусловием</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitWhile(WhileNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов перед посещением узла выхода из цикла
    /// </summary>
    /// <param name="node">Узел выхода из цикла</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitBreak(BreakNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов после посещения узла выхода из цикла
    /// </summary>
    /// <param name="node">Узел выхода из цикла</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitBreak(BreakNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов перед посещением узла пропуска текущей итерации
    /// </summary>
    /// <param name="node">Узел пропуска текущей итерации</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitContinue(ContinueNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов после посещения узла пропуска текущей итерации
    /// </summary>
    /// <param name="node">Узел пропуска текущей итерации</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitContinue(ContinueNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов перед посещением узла возврата из функции.
    /// </summary>
    /// <param name="node">Узел возврата из функции</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitReturn(ReturnNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов после посещения узла возврата из функции.
    /// </summary>
    /// <param name="node">Узел возврата из функции</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitReturn(ReturnNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов перед посещением узла отрицания значения
    /// </summary>
    /// <param name="node">Узел отрицания значения</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitNot(NotNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов после посещения узла отрицания значения
    /// </summary>
    /// <param name="node">Узел отрицания значения</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitNot(NotNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов перед посещением узла постфиксного декремента
    /// </summary>
    /// <param name="node">Узел постфиксного декремента</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitPostfixDecrement(PostfixDecrementNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов после посещения узла постфиксного декремента
    /// </summary>
    /// <param name="node">Узел постфиксного декремента</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitPostfixDecrement(PostfixDecrementNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов перед посещением узла постфиксного инкремента
    /// </summary>
    /// <param name="node">Узел постфиксного инкремента</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitPostfixIncrement(PostfixIncrementNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов после посещения узла постфиксного инкремента
    /// </summary>
    /// <param name="node">Узел постфиксного инкремента</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitPostfixIncrement(PostfixIncrementNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов перед посещением узла префиксного декремента
    /// </summary>
    /// <param name="node">Узел префиксного декремента</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitPrefixDecrement(PrefixDecrementNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов после посещения узла префиксного декремента
    /// </summary>
    /// <param name="node">Узел префиксного декремента</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitPrefixDecrement(PrefixDecrementNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов перед посещением узла префиксного инкремента
    /// </summary>
    /// <param name="node">Узел префиксного инкремента</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitPrefixIncrement(PrefixIncrementNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов после посещения узла префиксного инкремента
    /// </summary>
    /// <param name="node">Узел префиксного инкремента</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitPrefixIncrement(PrefixIncrementNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов перед посещением узла арифметического унарного отрицания
    /// </summary>
    /// <param name="node">Узел арифметического унарного</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitUnaryMinus(UnaryMinusNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов после посещения узла арифметического унарного отрицания
    /// </summary>
    /// <param name="node">Узел арифметического унарного</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitUnaryMinus(UnaryMinusNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов перед посещением узла вызова функции
    /// </summary>
    /// <param name="node">Узел вызова функции</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitCall(CallNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов после посещения узла вызова функции
    /// </summary>
    /// <param name="node">Узел вызова функции</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitCall(CallNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов перед посещением узла приведения типа
    /// </summary>
    /// <param name="node">Узел приведения типа</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitCast(CastNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов после посещения узла приведения типа
    /// </summary>
    /// <param name="node">Узел приведения типа</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitCast(CastNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов перед посещением узла создания объекта
    /// </summary>
    /// <param name="node">Узел создания объекта</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitConstructor(ConstructorCallNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов после посещения узла создания объекта
    /// </summary>
    /// <param name="node">Узел создания объекта</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitConstructor(ConstructorCallNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов перед посещением пустого узла 
    /// </summary>
    /// <param name="node">Пустой узел</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitEmpty(EmptyNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов после посещения пустого узла
    /// </summary>
    /// <param name="node">Пустой узел</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitEmpty(EmptyNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов перед посещением узла члена функции(имя переменной или аргумента)
    /// </summary>
    /// <param name="node">Узел локального члена функции(имя переменной или аргумента)</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitMember(MemberNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов после посещения узла члена функции(имя переменной или аргумента)
    /// </summary>
    /// <param name="node">Узел локального члена функции(имя переменной или аргумента)</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitMember(MemberNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов перед посещением объявления аргумента функции
    /// </summary>
    /// <param name="node">Объявление узла аргумента функции</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitParameter(ParameterNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов после посещения объявления аргумента функции
    /// </summary>
    /// <param name="node">Объявление узла аргумента функции</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitParameter(ParameterNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов перед посещением узла использования типа
    /// </summary>
    /// <param name="node">Узел использования типа</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitType(TypeNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов после посещения узла использования типа
    /// </summary>
    /// <param name="node">Узел использования типа</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitType(TypeNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов перед посещением объявления переменной
    /// </summary>
    /// <param name="node">Узел объявления переменной</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitVariableDefinition(VariableDefinitionNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов после посещения объявления переменной
    /// </summary>
    /// <param name="node">Узел объявления переменной</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitVariableDefinition(VariableDefinitionNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов перед посещением узла доступа к члену типа
    /// </summary>
    /// <param name="accessNode">Узел доступа к члену типа</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitMemberAccess(MemberAccessNode accessNode, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов после посещения узла доступа к члену типа
    /// </summary>
    /// <param name="accessNode">Узел доступа к члену типа</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitMemberAccess(MemberAccessNode accessNode, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов перед посещением литерала(строковый, числовой или булев)
    /// </summary>
    /// <param name="literalNode">Узел литерала(строковый, числовой или булев)</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitLiteral(LiteralNode literalNode, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов после посещения литерала(строковый, числовой или булев)
    /// </summary>
    /// <param name="literalNode">Узел литерала(строковый, числовой или булев)</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitLiteral(LiteralNode literalNode, TContext context, NodeBase? parent) => VisitResult.Continue;

    /// <summary>
    /// Вызов перед посещением узла битового И
    /// </summary>
    /// <param name="node">Узел битового И</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitBitwiseAnd(BitwiseAndNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов после посещения узла битового И
    /// </summary>
    /// <param name="node">Узел битового И</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitBitwiseAnd(BitwiseAndNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов перед посещением узла битового ИЛИ
    /// </summary>
    /// <param name="node">Узел битового ИЛИ</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitBitwiseOr(BitwiseOrNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов после посещения узла битового ИЛИ
    /// </summary>
    /// <param name="node">Узел битового ИЛИ</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitBitwiseOr(BitwiseOrNode node, TContext context, NodeBase? parent) => VisitResult.Continue;

    /// <summary>
    /// Вызов перед посещением узла исключающего или
    /// </summary>
    /// <param name="node">Узел исключающего или</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitXor(XorNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов после посещения узла исключающего или
    /// </summary>
    /// <param name="node">Узел исключающего или</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitXor(XorNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов перед посещением корневого узла единицы компиляции
    /// </summary>
    /// <param name="node">Корневой узел единицы компилляции</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitRoot(RootNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов после посещения корневого узла единицы компиляции
    /// </summary>
    /// <param name="node">Корневой узел единицы компилляции</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitRoot(RootNode node, TContext context, NodeBase? parent) => VisitResult.Continue;

    /// <summary>
    /// Вызов перед посещением директивы импорта
    /// </summary>
    /// <param name="node">Узел директивы импорта</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitImport(ImportNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов после посещения директивы импорта
    /// </summary>
    /// <param name="node">Узел директивы импорта</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitImport(ImportNode node, TContext context, NodeBase? parent) => VisitResult.Continue;

    /// <summary>
    /// Вызов перед посещением узла элемента импорта
    /// </summary>
    /// <param name="node">Узел элемента импорта(функция, тип)</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitImportItem(ImportItemNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов после посещения узла элемента импорта
    /// </summary>
    /// <param name="node">Узел элемента импорта(функция, тип)</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitImportItem(ImportItemNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов перед посещением объявления модуля
    /// </summary>
    /// <param name="definition">Узел объявления модуля</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitModuleDefinition(ModuleDefinitionNode definition, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов после посещения объявления модуля
    /// </summary>
    /// <param name="definition">Узел объявления модуля</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitModuleDefinition(ModuleDefinitionNode definition, TContext context, NodeBase? parent) => VisitResult.Continue;

    /// <summary>
    /// Метод, который позволяет добавлять расширения для кастомных типов узлов AST
    /// </summary>
    /// <param name="node">Узел кастомного типа</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitDefault(NodeBase node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Метод, который позволяет добавлять расширения для кастомных типов узлов AST
    /// </summary>
    /// <param name="node">Узел кастомного типа</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitDefault(NodeBase node, TContext context, NodeBase? parent) => VisitResult.Continue;

    /// <summary>
    /// Вызов перед посещением каждой инструкции в блоке <see cref="BodyNode"/>
    /// </summary>
    /// <param name="node">Инструкция внутри <see cref="BodyNode"/></param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitInstruction(NodeBase node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов после посещения каждой инструкции в блоке <see cref="BodyNode"/>
    /// </summary>
    /// <param name="node">Инструкция внутри <see cref="BodyNode"/></param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitInstruction(NodeBase node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов перед посещением объявления имени функции
    /// </summary>
    /// <param name="node">Объявление имени функции</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitFuncName(FuncNameNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов после посещения объявления имени функции
    /// </summary>
    /// <param name="node">Объявление имени функции</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitFuncName(FuncNameNode node, TContext context, NodeBase? parent) => VisitResult.Continue;

    /// <summary>
    /// Вызов перед посещением узла имени типа
    /// </summary>
    /// <param name="node">Узел имени типа</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitTypeName(TypeNameNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов после посещения узла имени типа
    /// </summary>
    /// <param name="node">Узел имени типа</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitTypeName(TypeNameNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов перед посещением узла имени параметра функции
    /// </summary>
    /// <param name="node">Узел имени параметра функции</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitParameterName(ParameterNameNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов после посещения узла имени параметра функции
    /// </summary>
    /// <param name="node">Узел имени параметра функции</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitParameterName(ParameterNameNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов перед посещением узла объявления имени переменной
    /// </summary>
    /// <param name="node">Узел имени переменной</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitVariableName(VariableNameNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов после посещения узла объявления имени переменной
    /// </summary>
    /// <param name="node">Узел имени переменной</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitVariableName(VariableNameNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов перед посещением узла имени вызываемой функции
    /// </summary>
    /// <param name="node">Узел имени вызываемой функции</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitFuncCallName(FuncCallNameNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов после посещения узла имени вызываемой функции
    /// </summary>
    /// <param name="node">Узел имени вызываемой функции</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitFuncCallName(FuncCallNameNode node, TContext context, NodeBase? parent) => VisitResult.Continue;

    /// <summary>
    /// Вызов перед посещением узла инициализации массива
    /// </summary>
    /// <param name="node">Узел создания массива</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitInitArray(InitArrayNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов после посещения узла инициализации массива
    /// </summary>
    /// <param name="node">Узел создания массива</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitInitArray(InitArrayNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов перед посещением узла определения длины массива
    /// </summary>
    /// <param name="node">Узел определения длины массива</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitArrayTypeSpecification(ArrayTypeSpecificationNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов после посещения узла определения длины массива
    /// </summary>
    /// <param name="node">Узел определения длины массива</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitArrayTypeSpecification(ArrayTypeSpecificationNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов перед посещением узла получения элемента сложного типа
    /// </summary>
    /// <param name="node">Узел получения элемента сложного типа</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitElemGetter(ElemGetterNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов после посещения узла получения элемента сложного типа
    /// </summary>
    /// <param name="node">Узел получения элемента сложного типа</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitElemGetter(ElemGetterNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов перед посещением узла установки значения сложного типа
    /// </summary>
    /// <param name="node">Узел установки значения сложного типа</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitElemSetter(ElemSetterNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов после посещения узла установки значения сложного типа
    /// </summary>
    /// <param name="node">Узел установки значения сложного типа</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitElemSetter(ElemSetterNode node, TContext context, NodeBase? parent) => VisitResult.Continue;

    /// <summary>
    /// Вызов перед посещением узла индексации массива
    /// </summary>
    /// <param name="node">Узел индексации массива</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PreVisitArrayIndexer(ArrayIndexerNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Вызов после посещения узла индексации массива
    /// </summary>
    /// <param name="node">Узел индексации массива</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    protected virtual VisitResult PostVisitArrayIndexer(ArrayIndexerNode node, TContext context, NodeBase? parent) => VisitResult.Continue;
    
    /// <summary>
    /// Базовый метод, определяющий логику обхода конкретного узла AST
    /// </summary>
    /// <param name="node">Узел, который требуется обойти</param>
    /// <param name="context">Контекст конкретного посетителя</param>
    /// <param name="parent">Родительский узел.</param>
    /// <param name="preVisit">Метод, который вызывается перед обходом потомков</param>
    /// <param name="postVisit">Метод, который вызывается после обхода потомков</param>
    /// <typeparam name="T">Тип узла, который сейчас обходится</typeparam>
    protected virtual VisitResult VisitCore<T>(
        T node, 
        TContext context, 
        NodeBase? parent,
        Func<T, TContext, NodeBase?, VisitResult> preVisit,
        Func<T, TContext, NodeBase?, VisitResult> postVisit) where T : NodeBase
    {
        var preResult = preVisit(node, context, parent);
        if (preResult is VisitResult.Break) return preResult;
        
        var visitResult = VisitResult.Continue;
        if(preResult is not VisitResult.SkipChildren) visitResult = VisitChildren(node, context);
        if (visitResult == VisitResult.Break) return visitResult;
        
        var postResult = postVisit(node, context, parent);
        return postResult;
    }
}