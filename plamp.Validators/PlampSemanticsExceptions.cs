using plamp.Abstractions.Ast;

namespace plamp.Validators;

public static class PlampSemanticsExceptions
{
    #region Impossible tree

    public static PlampExceptionRecord InvalidChildExpression() =>
        new()
        {
            Message = "Invalid child expression",
            Code = "SEM1300",
            Level = ExceptionLevel.Error,
        };

    public static PlampExceptionRecord InvalidAssignmentTarget() =>
        new()
        {
            Message = "Assignment is valid only for variables, variable definitions or properties",
            Code = "SEM1301",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord InvalidChangeAndReAssignmentTarget() =>
        new()
        {
            Message = "Change and re-assignment is valid only for variables, or properties",
            Code = "SEM1302",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord MissingRightExpression() =>
        new()
        {
            Message = "Binary operator requires a right side expression",
            Code = "SEM1303",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord MissingLeftExpression() =>
        new()
        {
            Message = "Binary operator requires a left side expression",
            Code = "SEM1304",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord MissingForBody() =>
        new()
        {
            Message = "For loop requires a body",
            Code = "SEM1305",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord InvalidForLoopCounter() =>
        new()
        {
            Message = "For loop counter can only be method call, " +
                      "indexer, expression.",
            Code = "SEM1306",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord InvalidForIterator() =>
        new()
        {
            Message = "For loop iterator must be an assignment expression",
            Code = "SEM1307",
            Level = ExceptionLevel.Error
        };
    
    public static PlampExceptionRecord InvalidForLoopCondition() =>
        new()
        {
            Message = "For loop condition can only be method call, " +
                      "indexer, expression.",
            Code = "SEM1308",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord MissingForeachBody() =>
        new()
        {
            Message = "Foreach loop requires a body",
            Code = "SEM1309",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord ForeachIteratorVarMustExist() =>
        new()
        {
            Message = "Foreach loop iterator must exist",
            Code = "SEM1310",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord ForeachIteratorMustBeVariableDeclaration() =>
        new()
        {
            Message = "Foreach iterator must be a variable declaration",
            Code = "SEM1311",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord ForeachIterableMustExist() =>
        new()
        {
            Message = "Foreach iterable must exist",
            Code = "SEM1312",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord MismatchForeachIterableNodeType() =>
        new()
        {
            Message = "Foreach iterable must be a variable, method call, constructor, indexer or property",
            Code = "SEM1313",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord MissingWhileBody() =>
        new()
        {
            Message = "While loop requires a body",
            Code = "SEM1314",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord MissingWhileCondition() =>
        new()
        {
            Message = "While loop requires a condition",
            Code = "SEM1315",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord MismatchWhileConditionNodeType() =>
        new()
        {
            Message = "While predicate must be a variable, method call, constructor, indexer or property",
            Code = "SEM1316",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord ConditionMustHaveBaseClause() =>
        new()
        {
            Message = "Condition requires a base clause",
            Code = "SEM1317",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord ConditionClauseMustHaveBody() =>
        new()
        {
            Message = "Condition requires a body",
            Code = "SEM1318",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord ConditionClausePredicateNodeTypeMismatch() =>
        new()
        {
            Message = "Condition clause predicate must be a variable, method call, property or indexer",
            Code = "SEM1319",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord ClausePredicateMustExist() =>
        new()
        {
            Message = "Condition clause predicate must exist",
            Code = "SEM1320",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord UnaryNodeRequiresUnderlyingNode() =>
        new()
        {
            Message = "Unary node require inner node",
            Code = "SEM1321",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord UnaryNodeInnerNodeTypeMismatch() =>
        new()
        {
            Message = "Inner node must be member, property, constant, method call, literal, indexer",
            Code = "SEM1322",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord CallNodeRequireCallerNode() =>
        new()
        {
            Message = "Call node requires a caller node",
            Code = "SEM1323",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord CallerNodeTypeMismatch() =>
        new()
        {
            Message = "Caller must be a member, property or indexer, cast, unary, binary node",
            Code = "SEM1324",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord ArgNodeMustNotBeNull() =>
        new()
        {
            Message = "Arg node in args must not be null",
            Code = "SEM1325",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord ArgMustBeValueReturningNode() =>
        new()
        {
            Message = "Call argument must return a value",
            Code = "SEM1326",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord IndexNodeRequireIndexableNode() =>
        new()
        {
            Message = "Index node requires an indexable node",
            Code = "SEM1327",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord IndexableNodeTypeMismatch() =>
        new()
        {
            Message = "Indexable must be a member, property, indexer, binary, cast, unary",
            Code = "SEM1328",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord IndexerArgMustBeReturningNode() =>
        new()
        {
            Message = "Indexer node argument must return a value",
            Code = "SEM1329",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord CannotAccessMemberFromNothing() =>
        new()
        {
            Message = "Cannot access member from nothing",
            Code = "SEM1330",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord CannotAccessNothing() =>
        new()
        {
            Message = "Cannot access nothing",
            Code = "SEM1331",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord AccessibleNodeTypeMismatch() =>
        new()
        {
            Message = "Accessible node must return a typed value",
            Code = "SEM1332",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord MemberAccessTargetMustBeMember() =>
        new()
        {
            Message = "Member access target must be a member",
            Code = "SEM1333",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord ConstructorMustHaveCreatingType() =>
        new()
        {
            Message = "Constructor must define type that creates",
            Code = "SEM1334",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord ConstructorTargetMustBeType() =>
        new()
        {
            Message = "Constructor target must be a type",
            Code = "SEM1335",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord ConstructorArgTypeMismatch() =>
        new()
        {
            Message = "Constructor arg must be expression that returns value",
            Code = "SEM1336",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord CastMustHaveType() =>
        new()
        {
            Message = "Cannot use cast node without type",
            Code = "SEM1337",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord CastTargetTypeMustBeTypeNode() =>
        new()
        {
            Message = "Cast target type must be a type node",
            Code = "SEM1338",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord NullCastTarget() =>
        new()
        {
            Message = "A cast target expression is required",
            Code = "SEM1339",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord CastTargetMustReturnValue() =>
        new()
        {
            Message = "Cast target expression must return a value",
            Code = "SEM1340",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord DefNodeMustHaveReturnType() =>
        new()
        {
            Message = "Def node must have return type",
            Code = "SEM1341",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord DefNodeReturnTypeMustBeTypeNode() =>
        new()
        {
            Message = "Def node return type must be type node",
            Code = "SEM1342",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord MethodMustHaveName() =>
        new()
        {
            Message = "Method must have a name",
            Code = "SEM1343",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord MethodNameMustBeMemberName() =>
        new()
        {
            Message = "Method name must be a name node",
            Code = "SEM1344",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord MethodArgNodeMustBeParameter() =>
        new()
        {
            Message = "Method arg must be a parameter",
            Code = "SEM1345",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord MethodMustHaveBody() =>
        new()
        {
            Message = "Method must have a body",
            Code = "SEM1346",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord UseMustHasTargetModule() =>
        new()
        {
            Message = "Use statement must have a target module",
            Code = "SEM1347",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord UseTargetMustBeMember() =>
        new()
        {
            Message = "Use statement target must be member or member access node",
            Code = "SEM1348",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord TypeNodeMustHaveName() =>
        new()
        {
            Message = "Type node must have a name",
            Code = "SEM1349",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord TypeNameMustBeMember() =>
        new()
        {
            Message = "Type name must be a member or member access",
            Code = "SEM1350",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord GenericsMustNotBeNull() =>
        new()
        {
            Message = "Generics must not be null",
            Code = "SEM1351",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord GenericMustBeType() =>
        new()
        {
            Message = "Generics must be a type",
            Code = "SEM1352",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord ParameterMustHaveName() =>
        new()
        {
            Message = "Parameter must have a name",
            Code = "SEM1353",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord ParameterNameMustBeMember() =>
        new()
        {
            Message = "Parameter name must be a member",
            Code = "SEM1354",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord ParameterMustHaveType() =>
        new()
        {
            Message = "Parameter must have a type",
            Code = "SEM1355",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord ParameterTypeMustBeTypeNode() =>
        new()
        {
            Message = "ParameterType must be a type node",
            Code = "SEM1356",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord VariableMustHaveName() =>
        new()
        {
            Message = "Variable must have a type",
            Code = "SEM1357",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord VariableNameMustBeMember() =>
        new()
        {
            Message = "Variable name must be a member",
            Code = "SEM1358",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord VariableTypeMustBeTypeNode() =>
        new()
        {
            Message = "Variable type must be a type node",
            Code = "SEM1359",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord MemberNameIsNull() =>
        new()
        {
            Message = "Member name must not be null",
            Code = "SEM1360",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord LiteralIsNotValueType() =>
        new()
        {
            Message = "Literal with value type must has value type value",
            Code = "SEM1361",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord LiteralTypeMismatch() =>
        new()
        {
            Message = "Literal type and literal value type mismatch",
            Code = "SEM1362",
            Level = ExceptionLevel.Error
        };

    #endregion

    #region DefMustReturnValue

    public static PlampExceptionRecord DefMustReturnValue() =>
        new()
        {
            Message = "Function must return a value",
            Code = "SEM1400",
            Level = ExceptionLevel.Error
        };

    #endregion
}