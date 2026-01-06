using System.Reflection;
using System.Reflection.Emit;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Binary;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ComplexTypes;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Abstractions.Ast.Node.Definitions.Variable;
using plamp.Abstractions.Ast.Node.Unary;
using plamp.Abstractions.Symbols.SymTable;

namespace plamp.ILCodeEmitters;

/// <summary>
/// Этот класс используется для создания из AST списка IL инструкций для конкретной функции.
/// Данный класс не производит никакой валидации.
/// При обнаружении несоответствия структуры AST желаемой, класс завершает своё исполнение с ошибкой.
/// Класс статический, это нужно, чтобы не было возможности хранить состояние. Это сделано, чтобы была возможность исполнять код в нескольких потоках.
/// Класс синхронный так как io взаимодействия в нём не существует.
/// На данный момент эмиссия IL кода происходит 1 к 1 без оптимизаций.
/// </summary>
public static class IlCodeEmitter
{
    /// <summary>
    /// Создать IL код для <see cref="T:plamp.Abstractions.Ast.Node.Body.BodyNode"/> с помощью экземпляра <see cref="T:System.Reflection.Emit.MethodBuilder"/>
    /// </summary>
    /// <param name="context">Одноразовый экземпляр класса контекста для создания кода конкретного метода</param>
    public static void EmitMethodBody(CompilerEmissionContext context)
    {
        var varStack = new LocalVarStack();
        var generator = context.MethodBuilder.GetILGenerator();
        var returnLabel = generator.DefineLabel();
        var emissionContext = new EmissionContext(varStack, context.Parameters, generator, [], context.MethodBuilder, returnLabel);
        EmitExpression(context.MethodBody, emissionContext);
        generator.MarkLabel(returnLabel);
        generator.Emit(OpCodes.Ret);
    }

    /// <summary>
    /// Общий метод для генерации IL всего, что может находиться как выражение внутри body. И даже самого body.
    /// </summary>
    /// <param name="expression">Узел ast, чьи инструкции требуется сгенерировать</param>
    /// <param name="context">Основная модель, которая хранит состояние текущей трансляции дерева разбора в il.</param>
    /// <exception cref="InvalidOperationException">Инструкция на уровне body не встречается или неизвестно как её обрабатывать</exception>
    private static void EmitExpression(
        NodeBase expression,
        EmissionContext context)
    {
        switch (expression)
        {
            case BodyNode bodyNode:
                EmitBody(bodyNode, context);
                break;
            case AssignNode assignNode:
                EmitAssign(assignNode, context);
                break;
            case ConditionNode conditionNode:
                EmitCondition(conditionNode, context);
                break;
            case WhileNode whileNode:
                EmitWhileLoop(whileNode, context);
                break;
            case BreakNode:
                EmitBreak(context);
                break;
            case ContinueNode:
                EmitContinue(context);
                break;
            case ReturnNode returnNode:
                EmitReturn(returnNode, context);
                break;
            case CallNode callNode:
                EmitCall(callNode, context, true);
                break;
            case VariableDefinitionNode variableDefinitionNode:
                EmitVariableDefinition(variableDefinitionNode, context);
                break;
            case BaseUnaryNode unary when unary.GetType() != typeof(UnaryMinusNode):
                EmitIncrementOrDecrement(unary, context, false);
                break;
            default:
                throw new InvalidOperationException(
                    "Unknown body level instruction. If you see this, report to programmer");
        }
    }

    /// <summary>
    /// Создание IL кода для узла блока инструкций.
    /// </summary>
    /// <param name="body">Узел AST обозначающий блок инструкций</param>
    /// <param name="context">Основная модель, которая хранит состояние текущей трансляции дерева разбора в il.</param>
    private static void EmitBody(
        BodyNode body, 
        EmissionContext context)
    {
        //Существует некоторое дублирование вызовов так как генератор это достаточно закрытый абстрактный класс.
        //Возможно будет написана собственная реалзация позже.
        context.LocalVarStack.BeginScope();
        context.Generator.BeginScope();
        foreach (var instruction in body.ExpressionList)
        {
            EmitExpression(instruction, context);
        }
        context.Generator.EndScope();
        context.LocalVarStack.EndScope();
    }
    
    /// <summary>
    /// Сгенерировать IL для инструкции возврата из функции
    /// Инструкция возврата одна и находится в конце функции, а код,
    /// который хочет вернуться должен положить значение на стек и переместиться в конец функции
    /// </summary>
    /// <param name="returnNode">Узел AST обозначающий возврат из функции</param>
    /// <param name="context">Основная модель, которая хранит состояние текущей трансляции дерева разбора в il.</param>
    private static void EmitReturn(ReturnNode returnNode, EmissionContext context)
    {
        //Возвращаем всё всегда по значению. Ссылочные типы просто скопируют ссылку. А структуры будут анбокшены и помещены на стек.
        if(returnNode.ReturnValue is MemberNode) EmitGetMember(returnNode.ReturnValue, context, true);
        //ReturnValue может быть null и это легально, значит функция void
        else if(returnNode.ReturnValue != null) EmitSingleLineExpression(returnNode.ReturnValue, context);
        context.Generator.Emit(OpCodes.Br, context.FnReturnLabel);
    }
    
    #region Looping

    /// <summary>
    /// Сгенерировать IL для цикла while
    /// </summary>
    /// <param name="whileNide">Узел AST обозначающий while</param>
    /// <param name="context">Основная модель, которая хранит состояние текущей трансляции дерева разбора в il.</param>
    /// <exception cref="Exception">Если тело цикла не является <see cref="T:plamp.Abstractions.Ast.Node.Body.BodyNode"/></exception>
    private static void EmitWhileLoop(WhileNode whileNide, EmissionContext context)
    {
        if(whileNide.Body is not BodyNode body) 
            throw new Exception("Body of while loop must be a body node. If you see this exception write to a compiler developer.");
        
        /*
         * Метки - обозначения точки, куда происходит переход потока выполнения с условием или без внутри IL. Аналог goto или if-else в C#.
         * Внутри IL генератора создание и привязка метки к номеру инструкции происходит двумя отдельными действиями.
         * Так как по сути IL код это просто набор байт, то метка переносит поток выполнения программы к конкретному байту смещения от начала тела функции.
         * Поэтому если вызвать MarkLabel, то поток выполнения будет переходить на первую инструкцию после MarkLabel.
         */
        
        //Объявление следующей инструкции как начала цикла
        var startLabelName = context.DefineLabel();
        context.Generator.MarkLabel(context.Labels[startLabelName]);

        //Создание метки конца цикла без присваивания метке номера инструкции. 
        var endLabelName = context.DefineLabel();
        
        /*
         * Циклы создаются в лоб, что не совсем оптимально
         *
         * start-label: 
         *     condition
         *     brfalse end-label
         *     body
         *     br start-label
         * end-label:
         */
        
        EmitSingleLineExpression(whileNide.Condition, context);
        context.Generator.Emit(OpCodes.Brfalse, context.Labels[endLabelName]);
        
        context.EnterCycleContext(startLabelName, endLabelName);
        EmitBody(body, context);
        context.ExitCycleContext();
        
        context.Generator.Emit(OpCodes.Br, context.Labels[startLabelName]);
        context.Generator.MarkLabel(context.Labels[endLabelName]);
    }

    /// <summary>
    /// Сгенерировать IL для инструкции прерывания цикада
    /// </summary>
    /// <param name="context">Основная модель, которая хранит состояние текущей трансляции дерева разбора в il.</param>
    /// <exception cref="Exception">Происходит если инструкция находится не в теле цикла.</exception>
    private static void EmitBreak(EmissionContext context)
    {
        var endLabel = context.GetCurrentCycleContext()?.EndLabel;
        if (endLabel == null) throw new Exception("Cannot emit break instruction outside of cycle body. If you see this exception write to a compiler developer.");
        //Переход в конец цикла
        context.Generator.Emit(OpCodes.Br, context.Labels[endLabel]);
    }

    /// <summary>
    /// Сгенерировать IL для инструкции пропуска итерации цикада
    /// </summary>
    /// <param name="context">Основная модель, которая хранит состояние текущей трансляции дерева разбора в il.</param>
    /// <exception cref="Exception">Происходит если инструкция находится не в теле цикла.</exception>
    private static void EmitContinue(EmissionContext context)
    {
        var startLabel = context.GetCurrentCycleContext()?.StartLabel;
        if (startLabel == null) throw new Exception("Cannot emit break instruction outside of cycle body. If you see this exception write to a compiler developer.");
        //Переход к началу цикла
        context.Generator.Emit(OpCodes.Br, context.Labels[startLabel]);
    }

    #endregion

    #region Conditional

    /// <summary>
    /// Создание IL для ветвления.
    /// </summary>
    /// <param name="conditionNode">Условный узел AST</param>
    /// <param name="context">Основная модель, которая хранит состояние текущей трансляции дерева разбора в il.</param>
    /// <exception cref="Exception">Происходит, если if или else не является BodyNode</exception>
    private static void EmitCondition(ConditionNode conditionNode, EmissionContext context)
    {
        if(conditionNode.IfClause is not BodyNode ifBody) throw new Exception("If clause must be a body node. If you see this exception write to a compiler developer.");
        if(conditionNode.ElseClause is not null and not BodyNode) throw new Exception("Else clause must be a body node. If you see this exception write to a compiler developer.");
        
        string? elseClauseEndLab;
        //Если внутри if мы гарантировано выходим из функции, то переход в конец всего условия не требуется.
        if (ifBody.ExpressionList.Any(x => x.GetType() == typeof(ReturnNode)) && conditionNode.ElseClause != null)
        {
            elseClauseEndLab = null;
        }
        else
        {
            elseClauseEndLab = context.DefineLabel();
        }
        
        //Эмиссия кода предиката
        EmitSingleLineExpression(conditionNode.Predicate, context);
        //Условный переход в конец if блока
        var ifClauseEndLab = context.DefineLabel();
        context.Generator.Emit(OpCodes.Brfalse, context.Labels[ifClauseEndLab]);
        //If блок
        EmitBody(ifBody, context);
        
        //Если тела else нет, то создаём метку пропуска if и выходим.
        if (conditionNode.ElseClause is null)
        {
            context.Generator.MarkLabel(context.Labels[ifClauseEndLab]);
            return;
        }
        
        //Если return в if блоке нет и есть else, то создаём безусловный переход в конец условия.
        if (elseClauseEndLab != null)
        {
            context.Generator.Emit(OpCodes.Br, context.Labels[elseClauseEndLab]);
        }
        
        //Помечаем следующую инструкцию как конец if(начало else)
        context.Generator.MarkLabel(context.Labels[ifClauseEndLab]);
        //Else body
        EmitBody((BodyNode)conditionNode.ElseClause, context);

        //Переход в конец else
        if (elseClauseEndLab != null)
        {
            context.Generator.MarkLabel(context.Labels[elseClauseEndLab]);
        }
    }

    #endregion
    
    #region Unary operators

    /// <summary>
    /// Генерация IL унарного оператора.
    /// </summary>
    /// <param name="unaryBase">Базовый класс AST узла унарного оператора</param>
    /// <param name="context">Основная модель, которая хранит состояние текущей трансляции дерева разбора в il.</param>
    /// <exception cref="Exception">Унарный оператор не найден.</exception>
    private static void EmitUnary(BaseUnaryNode unaryBase, EmissionContext context)
    {
        switch (unaryBase)
        {
            case NotNode:
                EmitSingleLineExpression(unaryBase.Inner, context);
                context.Generator.Emit(OpCodes.Ldc_I4_0);
                context.Generator.Emit(OpCodes.Ceq);
                break;
            case UnaryMinusNode:
                EmitSingleLineExpression(unaryBase.Inner, context);
                context.Generator.Emit(OpCodes.Neg);
                break;
            case PrefixIncrementNode:
            case PostfixIncrementNode:
            case PrefixDecrementNode:
            case PostfixDecrementNode:
                EmitIncrementOrDecrement(unaryBase, context, true);
                break;
            default: throw new Exception("Unknown unary operator. Compiler error, please report to developer");
        }
    }

    /// <summary>
    /// Основная модель, которая хранит состояние текущей трансляции дерева разбора в il.
    /// </summary>
    /// <param name="unaryBase">Базовый класс AST узла унарного оператора(должен быть инкрементом или декрементом)</param>
    /// <param name="context">Основная модель, которая хранит состояние текущей трансляции дерева разбора в il.</param>
    /// <param name="withAssign">Если результат оператора присваивается, то будет сгенерирован другой IL</param>
    /// <exception cref="InvalidOperationException">Оператор не является инкрементом или декрементом, операнд в который нельзя присвоить значение, тип операнда не число</exception>
    private static void EmitIncrementOrDecrement(BaseUnaryNode unaryBase, EmissionContext context, bool withAssign)
    {
        const string innerValueExceptionMessage = "Inner value of unary increment must be a variable arg or model property. Compiler error, please report to developer";
        const string invalidOperatorExceptionMessage = "Node must be an increment or an decrement. Compiler error, please report to developer";
        const string exceptionMessage = "Member is not a numeric type. Compiler error, please report to developer";
        
        Type memberType;
        switch (unaryBase.Inner)
        {
            case MemberNode member:
                memberType = EmitGetLocalVarOrArg(member, context, true);
                break;
            case FieldAccessNode fieldAccess:
                memberType = EmitGetField(fieldAccess, context, true);
                break;
            default: throw new InvalidOperationException(innerValueExceptionMessage);
        }
        
        switch (unaryBase)
        {
            case PrefixIncrementNode:
                LoadConstant(memberType);
                context.Generator.Emit(OpCodes.Add);
                if(withAssign) context.Generator.Emit(OpCodes.Dup);
                break;
            case PrefixDecrementNode:
                LoadConstant(memberType);
                context.Generator.Emit(OpCodes.Sub);
                if(withAssign) context.Generator.Emit(OpCodes.Dup);
                break;
            case PostfixIncrementNode:
                if(withAssign) context.Generator.Emit(OpCodes.Dup);
                LoadConstant(memberType);
                context.Generator.Emit(OpCodes.Add);
                break;
            case PostfixDecrementNode:
                if(withAssign) context.Generator.Emit(OpCodes.Dup);
                LoadConstant(memberType);
                context.Generator.Emit(OpCodes.Sub);
                break;
            default: throw new InvalidOperationException(invalidOperatorExceptionMessage);
        }

        switch (unaryBase.Inner)
        {
            case MemberNode member:
                EmitSetLocalVarOrArg(member.MemberName, context);
                break;
            case FieldAccessNode fieldAccess:
                Set(fieldAccess, context, true);
                break;
        }
        
        return;

        //Загрузка операнда инкремента(происходит особым образом для разных типов)
        void LoadConstant(Type constantType)
        {
            if (constantType == typeof(ulong) || constantType == typeof(long))
            {
                context.Generator.Emit(OpCodes.Ldc_I4_1);
                context.Generator.Emit(OpCodes.Conv_I8);
            }
            else if(constantType == typeof(int) 
                    || constantType == typeof(uint) 
                    || constantType == typeof(short) 
                    || constantType == typeof(ushort) 
                    || constantType == typeof(byte)) context.Generator.Emit(OpCodes.Ldc_I4_1);
            else if(constantType == typeof(float)) context.Generator.Emit(OpCodes.Ldc_R4, 1f);
            else if(constantType == typeof(double)) context.Generator.Emit(OpCodes.Ldc_R8, 1d);
            else throw new InvalidOperationException(exceptionMessage);
        }
    }

    #endregion
    
    #region Binary operators

    /// <summary>
    /// Сгенерировать IL для бинарного оператора
    /// </summary>
    /// <param name="binaryNode">Узел бинарного оператора</param>
    /// <param name="context">Основная модель, которая хранит состояние текущей трансляции дерева разбора в il.</param>
    /// <exception cref="Exception">Бинарный оператор не распознан.</exception>
    private static void EmitBaseBinary(BaseBinaryNode binaryNode, EmissionContext context)
    {
        EmitSingleLineExpression(binaryNode.Left, context);
        EmitSingleLineExpression(binaryNode.Right, context);
        switch (binaryNode)
        {
            case AddNode:
                EmitPlus(context);
                break;
            case SubNode:
                EmitMinus(context);
                break;
            case MulNode:
                EmitMultiply(context);
                break;
            case DivNode:
                EmitDivide(context);
                break;
            case BitwiseAndNode:
                EmitBitwiseAnd(context);
                break;
            case BitwiseOrNode:
                EmitBitwiseOr(context);
                break;
            case AndNode:
                EmitAndNode(context);
                break;
            case OrNode:
                EmitOrNode(context);
                break;
            case EqualNode:
                EmitEqual(context);
                break;
            case GreaterNode:
                EmitGreater(context);
                break;
            case GreaterOrEqualNode:
                EmitGreaterOrEqual(context);
                break;
            case LessNode:
                EmitLess(context);
                break;
            case LessOrEqualNode:
                EmitLessOrEqual(context);
                break;
            case NotEqualNode:
                EmitNotEqual(context);
                break;
            case ModuloNode:
                EmitModulo(context);
                break;
            case XorNode:
                EmitXor(context);
                break;
            default:
                throw new Exception(
                    $"Cannot emit this kind of binary operator {binaryNode.GetType().Name}. If you see this exception write to a compiler developer.");
        }
    }

    /// <summary>
    /// Сгенерировать IL для оператора присваивания.
    /// </summary>
    /// <param name="assignNode">Узел AST оператора присваивания.</param>
    /// <param name="context">Основная модель, которая хранит состояние текущей трансляции дерева разбора в il.</param>
    /// <exception cref="Exception">В цель присвоения нельзя присвоить значение.</exception>
    private static void EmitAssign(AssignNode assignNode, EmissionContext context)
    {
        var fldList = new List<FieldInfo?>();
        var referentialTargets = new List<NodeBase>(); 
        foreach (var (target, source) in assignNode.Targets.Zip(assignNode.Sources))
        {
            if(TryEmitAssignTargetForStructureType(target, source, context)) continue;
            PrepareAssignTarget(target, context, out var fld);
            fldList.Add(fld);
            //Генерация кода источника присвоения.
            EmitSingleLineExpression(source, context);
            referentialTargets.Add(target);
        }

        foreach (var (target, fld) in referentialTargets.AsEnumerable().Reverse().Zip(fldList))
        {
            EmitAssignTarget(target, context, fld);
        }
    }
    
    private static bool TryEmitAssignTargetForStructureType(NodeBase target, NodeBase source, EmissionContext context)
    {
        if (source is not InitTypeNode { Type.TypeInfo: { } info }) return false;
        
        var type = info.AsType();
        if (type is not { IsValueType: true, IsPrimitive: false }) return false;
        PrepareAssignTarget(target, context, out var fld);

        switch (target)
        {
            case FieldAccessNode:
                if(fld == null) throw new Exception("Member access must be a field. If you see this exception write to a compiler developer.");
                context.Generator.Emit(OpCodes.Ldflda, fld);
                break;
            case IndexerNode:
                context.Generator.Emit(OpCodes.Ldelema);
                break;
            case MemberNode memberNode:
                EmitGetLocalVarOrArg(memberNode, context, false);
                break;
            case VariableDefinitionNode variableDefinitionNode:
                foreach (var name in variableDefinitionNode.Names)
                {
                    //TODO: костыль, лень второй раз логику писать
                    EmitGetLocalVarOrArg(new MemberNode(name.Value), context, false);
                }
                break;
            default: throw new Exception();
        }

        if (target is VariableDefinitionNode varDef)
        {
            foreach (var _ in varDef.Names)
            {
                EmitSingleLineExpression(source, context);
            }
        }
        else
        {
            EmitSingleLineExpression(source, context);
        }

        return true;
    }

    private static void PrepareAssignTarget(NodeBase target, EmissionContext context, out FieldInfo? emitFld)
    {
        emitFld = null;
        //Подготовка цели присвоения.
        switch (target)
        {
            case FieldAccessNode accessNode:
                if (accessNode.Field is not { FieldInfo: { } info })
                {
                    throw new Exception("Member access must be a field. If you see this exception write to a compiler developer.");
                }
                emitFld = info.AsField();
                
                if(accessNode.From is MemberNode member) EmitGetMember(member, context, false);
                else if (accessNode.From is FieldAccessNode innerAccess) EmitGetField(innerAccess, context, false);
                else throw new Exception();
                break;
            case VariableDefinitionNode varDef:
                EmitVariableDefinition(varDef, context);
                break;
            case MemberNode: break;
            case IndexerNode indexerNode:
                EmitSingleLineExpression(indexerNode.From, context);
                EmitSingleLineExpression(indexerNode.IndexMember, context);
                break;
            default: throw new Exception();
        }
    }

    private static void EmitAssignTarget(NodeBase target, EmissionContext context, FieldInfo? emitFld)
    {
        //Генерация цели присвоения
        if (emitFld is not null)
        {
            context.Generator.Emit(OpCodes.Stfld, emitFld);
        }
        else if (target is VariableDefinitionNode {Names: { } varNames})
        {
            //Делаем дублирование присваивания всем переменным если их больше одной.
            for (var i = 0; i < varNames.Count - 1; i++)
            {
                context.Generator.Emit(OpCodes.Dup);
            }
            foreach (var varName in varNames)
            {
                EmitSetLocalVarOrArg(varName.Value, context);
            }
        }
        else if(target is MemberNode memberNode)
        {
            EmitSetLocalVarOrArg(memberNode.MemberName, context);
        }
        else if (target is IndexerNode indexerNode)
        {
            var itemType = indexerNode.ItemType?.AsType();
            if (itemType == null)
                throw new Exception("Array item type is not set. If you see this exception write to a compiler developer.");
            EmitSetArrayElemOpcode(itemType, context);
        }
        else
        {
            throw new Exception(
                $"Cannot emit assign to target of type {target.GetType().Name}. If you see this exception write to a compiler developer.");
        }
    }

    /// <summary>
    /// Создание IL кода для узла AST, который не является блоком инструкций на уровне языка и помещается в одну строку кода.
    /// </summary>
    /// <param name="expression">AST узел выражения</param>
    /// <param name="context">Основная модель, которая хранит состояние текущей трансляции дерева разбора в il.</param>
    /// <exception cref="Exception">Выражение не распознано</exception>
    private static void EmitSingleLineExpression(NodeBase expression, EmissionContext context)
    {
        switch (expression)
        {
            case MemberNode          memberNode:          EmitGetLocalVarOrArg(memberNode, context, false); break;
            case BaseBinaryNode      binaryNode:          EmitBaseBinary(binaryNode, context);              break;
            case BaseUnaryNode       unaryNode:           EmitUnary(unaryNode, context);                    break;
            case CallNode            callNode:            EmitCall(callNode, context, false);               break;
            case CastNode            castNode:            EmitTypeConversion(castNode, context);            break;
            case LiteralNode         literalNode:         EmitLiteral(literalNode, context);                break;
            case FieldAccessNode     memberAccessNode:    EmitGetField(memberAccessNode, context, true);    break;
            case InitArrayNode       initArrayNode:       EmitArrayInitialization(initArrayNode, context);  break;
            case IndexerNode         indexerNode:         EmitIndexer(indexerNode, context);                break;
            case InitTypeNode        initTypeNode:        EmitTypeInit(initTypeNode, context);              break;
            default:                                                                                        throw new Exception("Cannot emit expression. If you see this exception write to a compiler developer.");
        }
    }

    /// <summary>
    /// Генерация IL получения поля модели
    /// </summary>
    /// <param name="accessNode">AST узел получения поля модели</param>
    /// <param name="context">Основная модель, которая хранит состояние текущей трансляции дерева разбора в il.</param>
    /// <param name="byValue">Нужно ли получать поле по значению</param>
    /// <exception cref="Exception">Узел AST имеет не валидную конфигурацию или узел не имеет информации о поле, которое требуется получить</exception>
    private static Type EmitGetField(FieldAccessNode accessNode, EmissionContext context, bool byValue)
    {
        var fieldInfo = accessNode.Field.FieldInfo?.AsField();
        if (fieldInfo == null)
        {
            throw new Exception("Member access must has .net member representation. If you see this exception write to a compiler developer.");
        }
        
        switch (accessNode.From)
        {
            case MemberNode member:
                EmitGetMember(member, context, byValue);
                break;
            case FieldAccessNode fieldAccess:
                EmitGetField(fieldAccess, context, byValue);
                break;
            default:
                throw new Exception("Invalid member access. If you see this exception write to a compiler developer.");
        }
        
        context.Generator.Emit(OpCodes.Ldfld, fieldInfo);
        return fieldInfo.FieldType;
    }
    
    /// <summary>
    /// Генерация IL оператора больше
    /// </summary>
    /// <param name="context">Основная модель, которая хранит состояние текущей трансляции дерева разбора в il.</param>
    private static void EmitGreater(EmissionContext context)
        => context.Generator.Emit(OpCodes.Cgt);

    /// <summary>
    /// Генерация IL оператора больше или равно
    /// </summary>
    /// <param name="context">Основная модель, которая хранит состояние текущей трансляции дерева разбора в il.</param>
    private static void EmitGreaterOrEqual(EmissionContext context)
    {
        context.Generator.Emit(OpCodes.Clt);
        context.Generator.Emit(OpCodes.Ldc_I4_0);
        context.Generator.Emit(OpCodes.Ceq);
    }

    /// <summary>
    /// Генерация IL оператора меньше
    /// </summary>
    /// <param name="context">Основная модель, которая хранит состояние текущей трансляции дерева разбора в il.</param>
    private static void EmitLess(EmissionContext context) 
        => context.Generator.Emit(OpCodes.Clt);

    /// <summary>
    /// Генерация IL оператора меньше или равно
    /// </summary>
    /// <param name="context">Основная модель, которая хранит состояние текущей трансляции дерева разбора в il.</param>
    private static void EmitLessOrEqual(EmissionContext context)
    {
        context.Generator.Emit(OpCodes.Cgt);
        context.Generator.Emit(OpCodes.Ldc_I4_0);
        context.Generator.Emit(OpCodes.Ceq);
    }

    /// <summary>
    /// Генерация IL не равно
    /// </summary>
    /// <param name="context">Основная модель, которая хранит состояние текущей трансляции дерева разбора в il.</param>
    private static void EmitNotEqual(EmissionContext context)
    {
        context.Generator.Emit(OpCodes.Ceq);
        context.Generator.Emit(OpCodes.Ldc_I4_0);
        context.Generator.Emit(OpCodes.Ceq);
    }

    /// <summary>
    /// Генерация IL оператора получения остатка от деления
    /// </summary>
    /// <param name="context">Основная модель, которая хранит состояние текущей трансляции дерева разбора в il.</param>
    private static void EmitModulo(EmissionContext context) 
        => context.Generator.Emit(OpCodes.Rem);

    /// <summary>
    /// Генерация IL оператора исключающего или
    /// </summary>
    /// <param name="context">Основная модель, которая хранит состояние текущей трансляции дерева разбора в il.</param>
    private static void EmitXor(EmissionContext context)
        => context.Generator.Emit(OpCodes.Xor);

    /// <summary>
    /// Генерация IL оператора равенства
    /// </summary>
    /// <param name="context">Основная модель, которая хранит состояние текущей трансляции дерева разбора в il.</param>
    private static void EmitEqual(EmissionContext context)
        => context.Generator.Emit(OpCodes.Ceq);

    /// <summary>
    /// Генерация IL оператора сложения
    /// </summary>
    /// <param name="context">Основная модель, которая хранит состояние текущей трансляции дерева разбора в il.</param>
    private static void EmitPlus(EmissionContext context)
        => context.Generator.Emit(OpCodes.Add);

    /// <summary>
    /// Генерация IL оператора вычитания
    /// </summary>
    /// <param name="context">Основная модель, которая хранит состояние текущей трансляции дерева разбора в il.</param>
    private static void EmitMinus(EmissionContext context)
        => context.Generator.Emit(OpCodes.Sub);

    /// <summary>
    /// Генерация IL оператора умножения
    /// </summary>
    /// <param name="context">Основная модель, которая хранит состояние текущей трансляции дерева разбора в il.</param>
    private static void EmitMultiply(EmissionContext context)
        => context.Generator.Emit(OpCodes.Mul);

    /// <summary>
    /// Генерация IL оператора деления
    /// </summary>
    /// <param name="context">Основная модель, которая хранит состояние текущей трансляции дерева разбора в il.</param>
    private static void EmitDivide(EmissionContext context)
        => context.Generator.Emit(OpCodes.Div);

    /// <summary>
    /// Генерация IL оператора битового И
    /// </summary>
    /// <param name="context">Основная модель, которая хранит состояние текущей трансляции дерева разбора в il.</param>
    private static void EmitBitwiseAnd(EmissionContext context)
        => context.Generator.Emit(OpCodes.And);

    /// <summary>
    /// Генерация IL оператора битового ИЛИ
    /// </summary>
    /// <param name="context">Основная модель, которая хранит состояние текущей трансляции дерева разбора в il.</param>
    private static void EmitBitwiseOr(EmissionContext context)
        => context.Generator.Emit(OpCodes.Or);

    /// <summary>
    /// Генерация IL оператора логического И
    /// </summary>
    /// <param name="context">Основная модель, которая хранит состояние текущей трансляции дерева разбора в il.</param>
    private static void EmitAndNode(EmissionContext context)
        => context.Generator.Emit(OpCodes.And);

    /// <summary>
    /// Генерация IL оператора логического ИЛИ
    /// </summary>
    /// <param name="context">Основная модель, которая хранит состояние текущей трансляции дерева разбора в il.</param>
    private static void EmitOrNode(EmissionContext context)
        => context.Generator.Emit(OpCodes.Or);

    #endregion

    #region Misc

    /// <summary>
    /// Генерация IL операции смена типа(каста)
    /// </summary>
    /// <param name="node">Узел смены типа</param>
    /// <param name="context">Основная модель, которая хранит состояние текущей трансляции дерева разбора в il.</param>
    /// <exception cref="ArgumentException">Невозможно привести тип.</exception>
    private static void EmitTypeConversion(CastNode node, EmissionContext context)
    {
        switch (node.Inner)
        {
            case FieldAccessNode fieldAccessNode:
                EmitGetField(fieldAccessNode, context, false);
                break;
            case MemberNode member:
                EmitGetLocalVarOrArg(member, context, false);
                break;
            case LiteralNode literal:
                EmitLiteral(literal, context);
                break;
            case CallNode call:
                EmitCall(call, context, false);
                break;
            case BaseBinaryNode binary:
                EmitBaseBinary(binary, context);
                break;
            case BaseUnaryNode unary:
                EmitUnary(unary, context);
                break;
            case CastNode cast:
                EmitTypeConversion(cast, context);
                break;
            case IndexerNode indexer:
                EmitIndexer(indexer, context);
                break;
            default: throw new ArgumentException(nameof(node.Inner));
        }
        
        var toType = GetTypeFromNode(node.ToType)!;
        var fromType = node.FromType?.AsType();

        if (fromType == null) throw new ArgumentException("From type cannot be null semantics exception");
        if(!fromType.IsValueType && !toType.IsValueType) EmitCast(toType, context);
        else if(fromType.IsValueType && !toType.IsValueType) EmitBox(fromType, context);
        else if(!fromType.IsValueType && toType.IsValueType) EmitUnbox(toType, context);
        //We can convert i4 -> i8 or any other
        else EmitNumberTypeConversion(toType, context);
        //A struct cannot be converted to a struct
    }

    /// <summary>
    /// Генерация IL приведения одного ссылочного типа в другой
    /// </summary>
    /// <param name="targetType">Целевой тип</param>
    /// <param name="context">Основная модель, которая хранит состояние текущей трансляции дерева разбора в il.</param>
    private static void EmitCast(Type targetType, EmissionContext context) 
        => context.Generator.Emit(OpCodes.Castclass, targetType);

    /// <summary>
    /// Генерация IL боксинга
    /// </summary>
    /// <param name="sourceType">Тип, из которого происходит боксинг</param>
    /// <param name="context">Основная модель, которая хранит состояние текущей трансляции дерева разбора в il.</param>
    private static void EmitBox(Type sourceType, EmissionContext context) 
        => context.Generator.Emit(OpCodes.Box, sourceType);

    /// <summary>
    /// Генерация IL конверсии одного числового типа в другой
    /// </summary>
    /// <param name="targetType">Целевой числовой тип</param>
    /// <param name="context">Основная модель, которая хранит состояние текущей трансляции дерева разбора в il.</param>
    /// <exception cref="Exception">Невозможно привести тип</exception>
    private static void EmitNumberTypeConversion(Type targetType, EmissionContext context)
    {
        if (targetType == typeof(long)) context.Generator.Emit(OpCodes.Conv_I8);
        else if (targetType == typeof(ulong)) context.Generator.Emit(OpCodes.Conv_U8);
        else if (targetType == typeof(int)) context.Generator.Emit(OpCodes.Conv_Ovf_I4);
        else if (targetType == typeof(uint)) context.Generator.Emit(OpCodes.Conv_Ovf_U4);
        else if (targetType == typeof(short)) context.Generator.Emit(OpCodes.Conv_Ovf_I2);
        else if (targetType == typeof(ushort) || targetType == typeof(char)) context.Generator.Emit(OpCodes.Conv_Ovf_U2);
        else if (targetType == typeof(byte)) context.Generator.Emit(OpCodes.Conv_Ovf_U1);
        else if (targetType == typeof(double)) context.Generator.Emit(OpCodes.Conv_R8);
        else if (targetType == typeof(float)) context.Generator.Emit(OpCodes.Conv_R4);
        else throw new Exception("Cannot emit numeric type conversion. If you see this exception write to a compiler developer.");
    }
    
    /// <summary>
    /// Генерация IL анбоксинга
    /// </summary>
    /// <param name="targetType">Целевой тип структуры</param>
    /// <param name="context">Основная модель, которая хранит состояние текущей трансляции дерева разбора в il.</param>
    private static void EmitUnbox(Type targetType, EmissionContext context) 
        => context.Generator.Emit(OpCodes.Unbox_Any, targetType);

    /// <summary>
    /// Генерация IL помещения на стек константы
    /// </summary>
    /// <param name="literalNode">Узел AST константы</param>
    /// <param name="context">Основная модель, которая хранит состояние текущей трансляции дерева разбора в il.</param>
    /// <exception cref="ArgumentException">AST константы содержит null как значение.</exception>
    /// <exception cref="InvalidOperationException">Неизвестный тип константы.</exception>
    private static void EmitLiteral(LiteralNode literalNode, EmissionContext context)
    {
        const string errorText = "Unknown literal type. Please report to compiler developer.";
        var literalType = literalNode.Type.AsType();
        if (literalType == null) throw new Exception(errorText);
        
        if (literalType == typeof(string))
        {
            if (literalNode.Value == null) context.Generator.Emit(OpCodes.Ldnull);
            else context.Generator.Emit(OpCodes.Ldstr, (string)literalNode.Value);
            return;
        }
        if(literalNode.Value == null) throw new ArgumentException("Cannot emit load null to value type");
        if (literalType == typeof(int)) context.Generator.Emit(OpCodes.Ldc_I4, (int)literalNode.Value);
        else if (literalType == typeof(uint)) context.Generator.Emit(OpCodes.Ldc_I4, BitConverter.ToInt32(BitConverter.GetBytes((uint)literalNode.Value)));
        else if (literalType == typeof(long)) context.Generator.Emit(OpCodes.Ldc_I8, (long)literalNode.Value);
        else if (literalType == typeof(ulong)) context.Generator.Emit(OpCodes.Ldc_I8, BitConverter.ToInt64(BitConverter.GetBytes((ulong)literalNode.Value)));
        else if (literalType == typeof(short)) context.Generator.Emit(OpCodes.Ldc_I4, (int)(short)literalNode.Value);
        else if (literalType == typeof(ushort)) context.Generator.Emit(OpCodes.Ldc_I4, (ushort)literalNode.Value);
        else if (literalType == typeof(byte)) context.Generator.Emit(OpCodes.Ldc_I4, (int)(byte)literalNode.Value);
        else if (literalType == typeof(float)) context.Generator.Emit(OpCodes.Ldc_R4, (float)literalNode.Value);
        else if (literalType == typeof(double)) context.Generator.Emit(OpCodes.Ldc_R8, (double)literalNode.Value);
        else if (literalType == typeof(bool)) context.Generator.Emit((bool)literalNode.Value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
        else if (literalType == typeof(char)) context.Generator.Emit(OpCodes.Ldc_I4, Convert.ToUInt32(literalNode.Value));
        else throw new InvalidOperationException(errorText);
    }

    /// <summary>
    /// Генерация IL объявления переменной, также создание переменной в генераторе по имени и помещении в стек.
    /// </summary>
    /// <param name="variableDefinitionNode">Узел AST объявления переменной</param>
    /// <param name="context">Основная модель, которая хранит состояние текущей трансляции дерева разбора в il.</param>
    /// <exception cref="Exception">Узел переменной заполнен некорректно</exception>
    /// <exception cref="ArgumentException">Тип переменной не имеет репрезентации в .net</exception>
    private static void EmitVariableDefinition(
        VariableDefinitionNode variableDefinitionNode,
        EmissionContext context)
    {
        if(variableDefinitionNode.Type?.TypeInfo?.AsType() is not { } type) throw new Exception();
        if(variableDefinitionNode.Names is not { } members) throw new Exception();
        if (type == null) throw new ArgumentException("Cannot emit variable definition with null type");
        foreach (var member in members)
        {
            var builder = context.Generator.DeclareLocal(type);
            context.LocalVarStack.Add(member.Value, builder);
        }
    }
    
    /// <summary>
    /// Генерация IL вызова метода
    /// </summary>
    /// <param name="callNode">Узел ast вызова метода</param>
    /// <param name="context">Основная модель, которая хранит состояние текущей трансляции дерева разбора в il.</param>
    /// <param name="popResult">Флаг определяющий потребуется ли результат работы метода на стеке далее.</param>
    /// <exception cref="Exception">Не известно как интерпретировать кому принадлежит метод</exception>
    private static void EmitCall(CallNode callNode, EmissionContext context, bool popResult)
    {
        if(callNode.FnInfo == null) throw new Exception();
        
        switch (callNode.From)
        {
            case MemberNode:
                EmitGetMember(callNode.From, context, false);
                break;
            case ThisNode:
                context.Generator.Emit(OpCodes.Ldarg_0);
                break;
            case null: break;
            default: throw new Exception();
        }
        
        foreach (var arg in callNode.Args)
        {
            if(arg is MemberNode) EmitGetMember(arg, context, true);
            else EmitSingleLineExpression(arg, context);
        }
        var returnType = EmitMethodCall(callNode.FnInfo, context);
        
        if (returnType != typeof(void) && popResult) context.Generator.Emit(OpCodes.Pop);
    }

    #endregion

    #region Utility

    /// <summary>
    /// Попытка получения .net типа узла из него
    /// </summary>
    /// <param name="node">Узел AST</param>
    /// <returns>Тип в случае успеха иначе null</returns>
    private static Type? GetTypeFromNode(NodeBase node) => node is not TypeNode typeNode ? null : typeNode.TypeInfo?.AsType();

    /// <summary>
    /// Выбор и генерация инструкции вызова метода по метаинформации о нём
    /// </summary>
    /// <param name="fnRef">Метоинформация о методе</param>
    /// <param name="context">Основная модель, которая хранит состояние текущей трансляции дерева разбора в il.</param>
    /// <returns>Тип, который должен возвращать метод.</returns>
    private static Type EmitMethodCall(IFnInfo fnRef, EmissionContext context)
    {
        var methodInfo = fnRef.AsFunc();
        if (methodInfo == null) throw new Exception();
        var opcode = methodInfo.IsStatic ? OpCodes.Call : OpCodes.Callvirt;
        context.Generator.Emit(opcode, methodInfo);
        return methodInfo.ReturnType;
    }

    /// <summary>
    /// Генерация IL получения локальной переменной или аргумента функции
    /// </summary>
    /// <param name="member">Узел переменной или аргумента</param>
    /// <param name="context">Основная модель, которая хранит состояние текущей трансляции дерева разбора в il.</param>
    /// <param name="byValue">Получать по значению или по ссылке</param>
    /// <returns>Тип переменной или аргумента</returns>
    /// <exception cref="InvalidOperationException">Переменная или аргумент не найдены.</exception>
    private static Type EmitGetLocalVarOrArg(MemberNode member, EmissionContext context, bool byValue)
    {
        OpCode opcode;
        if (context.LocalVarStack.TryGetValue(member.MemberName, out var builder))
        {
            opcode = builder.LocalType is { IsValueType: true, IsPrimitive: false } && !byValue ? OpCodes.Ldloca : OpCodes.Ldloc;
            context.Generator.Emit(opcode, builder);
            return builder.LocalType;
        }

        ParameterInfo? arg;
        if ((arg = context.Arguments.FirstOrDefault(x => x.Name == member.MemberName)) == null)
        {
            throw new InvalidOperationException(
                "Argument does not exists. Invalid compilation. Report to language developer.");
        }
        
        var ix = Array.IndexOf(context.Arguments, arg);
        
        opcode = arg.ParameterType is { IsValueType: true, IsPrimitive: false } && !byValue
            ? OpCodes.Ldarga_S
            : OpCodes.Ldarg_S;
        
        ix = context.CurrentMethod.IsStatic ? ix : ix + 1;
        context.Generator.Emit(opcode, ix);
        return arg.ParameterType;
    }
    
    /// <summary>
    /// Генерация IL сохранения значения в переменную или аргумент
    /// </summary>
    /// <param name="name">Имя переменной или аргумента</param>
    /// <param name="context">Основная модель, которая хранит состояние текущей трансляции дерева разбора в il.</param>
    /// <exception cref="Exception">Переменная или аргумент не найдены.</exception>
    private static void EmitSetLocalVarOrArg(string name, EmissionContext context)
    {
        if (context.LocalVarStack.TryGetValue(name, out var builder))
        {
            context.Generator.Emit(OpCodes.Stloc, builder);
            return;
        }

        ParameterInfo? arg;
        if ((arg = context.Arguments.FirstOrDefault()) == null) throw new Exception();

        var ix = Array.IndexOf(context.Arguments, arg);
        ix = context.CurrentMethod.IsStatic ? ix : ix + 1;
        context.Generator.Emit(OpCodes.Starg_S, ix);
    }

    /// <summary>
    /// Получение локального члена функции с проверкой типа и генерация IL кода помещения его значения на стек.
    /// </summary>
    /// <param name="node">Узел AST локального члена</param>
    /// <param name="context">Основная модель, которая хранит состояние текущей трансляции дерева разбора в il.</param>
    /// <param name="byValue">По значению или ссылке</param>
    /// <exception cref="Exception">Узел AST не имеет тип MemberNode.</exception>
    private static void EmitGetMember(NodeBase node, EmissionContext context, bool byValue)
    {
        if(node is not MemberNode memberNode) throw new Exception();
        EmitGetLocalVarOrArg(memberNode, context, byValue);
    }

    #endregion

    #region Arrays

    private static void EmitArrayInitialization(InitArrayNode initArrayNode, EmissionContext context)
    {
        var type = initArrayNode.ArrayItemType.TypeInfo?.AsType();
        if (type == null) throw new Exception();
        EmitSingleLineExpression(initArrayNode.LengthDefinition, context);
        context.Generator.Emit(OpCodes.Newarr, type);
    }
    
    private static void EmitIndexer(IndexerNode indexerNode, EmissionContext context)
    {
        if (indexerNode.ItemType?.AsType() is not {} fromType) throw new Exception();
        EmitSingleLineExpression(indexerNode.From, context);
        EmitSingleLineExpression(indexerNode.IndexMember, context);
        
        if(fromType == typeof(int))                                    context.Generator.Emit(OpCodes.Ldelem_I4);
        else if(fromType == typeof(uint))                              context.Generator.Emit(OpCodes.Ldelem_U4);
        else if(fromType == typeof(long) || fromType == typeof(ulong)) context.Generator.Emit(OpCodes.Ldelem_I8);
        else if(fromType == typeof(byte) || fromType == typeof(bool))  context.Generator.Emit(OpCodes.Ldelem_U1);
        else if(fromType == typeof(char))                              context.Generator.Emit(OpCodes.Ldelem_U2);
        else if(fromType == typeof(float))                             context.Generator.Emit(OpCodes.Ldelem_R4);
        else if(fromType == typeof(double))                            context.Generator.Emit(OpCodes.Ldelem_R8);
        else if(fromType.IsClass)                                      context.Generator.Emit(OpCodes.Ldelem_Ref);
        else if (fromType is { IsPrimitive: false, IsClass: false })   context.Generator.Emit(OpCodes.Ldelem,     fromType);
        else                                                           throw new Exception();
    }

    private static void EmitSetArrayElemOpcode(Type? elemType, EmissionContext context)
    {
        if (elemType == null) throw new Exception();
        
        if      (elemType == typeof(int))                               context.Generator.Emit(OpCodes.Stelem_I4);
        else if (elemType == typeof(uint))                              context.Generator.Emit(OpCodes.Stelem_I4);
        else if (elemType == typeof(long) || elemType == typeof(ulong)) context.Generator.Emit(OpCodes.Stelem_I8);
        else if (elemType == typeof(byte) || elemType == typeof(bool))  context.Generator.Emit(OpCodes.Stelem_I );
        else if (elemType == typeof(char))                              context.Generator.Emit(OpCodes.Stelem_I2);
        else if (elemType == typeof(float))                             context.Generator.Emit(OpCodes.Stelem_R4);
        else if (elemType == typeof(double))                            context.Generator.Emit(OpCodes.Stelem_R8);
        else if (elemType.IsClass)                                      context.Generator.Emit(OpCodes.Stelem_Ref);
        else if (elemType is { IsPrimitive: false, IsClass: false })    context.Generator.Emit(OpCodes.Stelem,     elemType);
        else                                                            throw new Exception();
    }

    #endregion

    #region User-defined types

    private static void EmitTypeInit(InitTypeNode node, EmissionContext context)
    {
        if (node.Type.TypeInfo?.AsType() is not { } type)
            throw new Exception("Compiler exception, if you see this - write to the compiler developer");
        if (type.IsValueType)
        {
            context.Generator.Emit(OpCodes.Initobj, type);
        }
        else
        {
            var ctor = node.Type.TypeInfo.AsType().GetConstructor(BindingFlags.Public | BindingFlags.Instance, []);
            context.Generator.Emit(OpCodes.Newobj, ctor!);
        }
    }

    #endregion
}