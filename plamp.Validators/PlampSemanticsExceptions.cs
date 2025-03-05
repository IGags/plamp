using plamp.Ast;

namespace plamp.Validators;

public static class PlampSemanticsExceptions
{
    #region Semantics exceptions(1300-2999) !RESERVED!

    public static PlampExceptionRecord InvalidChildExpression() =>
        new()
        {
            Message = "Invalid child expression",
            Code = 1300,
            Level = ExceptionLevel.Error,
        };

    public static PlampExceptionRecord InvalidAssignmentTarget() =>
        new()
        {
            Message = "Assignment is valid only for variables, variable definitions or properties",
            Code = 1301,
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord InvalidChangeAndReAssignmentTarget() =>
        new()
        {
            Message = "Change and re-assignment is valid only for variables, or properties",
            Code = 1302,
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord MissingRightExpression() =>
        new()
        {
            Message = "Binary operator requires a right side expression",
            Code = 1303,
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord MissingLeftExpression() =>
        new()
        {
            Message = "Binary operator requires a left side expression",
            Code = 1304,
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord MissingForBody() =>
        new()
        {
            Message = "For loop requires a body",
            Code = 1305,
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord InvalidForLoopCounter() =>
        new()
        {
            Message = "For loop counter can only be method call, " +
                      "indexer, expression.",
            Code = 1306,
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord InvalidForIterator() =>
        new()
        {
            Message = "For loop iterator must be an assignment expression",
            Code = 1307,
            Level = ExceptionLevel.Error
        };
    
    public static PlampExceptionRecord InvalidForLoopCondition() =>
        new()
        {
            Message = "For loop condition can only be method call, " +
                      "indexer, expression.",
            Code = 1308,
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord MissingForeachBody() =>
        new()
        {
            Message = "Foreach loop requires a body",
            Code = 1309,
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord ForeachIteratorVarMustExist() =>
        new()
        {
            Message = "Foreach loop iterator must exist",
            Code = 1310,
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord ForeachIteratorMustBeVariableDeclaration() =>
        new()
        {
            Message = "Foreach iterator must be a variable declaration",
            Code = 1311,
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord ForeachIterableMustExist() =>
        new()
        {
            Message = "Foreach iterable must exist",
            Code = 1312,
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord MismatchForeachIterableNodeType() =>
        new()
        {
            Message = "Foreach iterable must be a variable, method call, constructor, indexer or property",
            Code = 1313,
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord MissingWhileBody() =>
        new()
        {
            Message = "While loop requires a body",
            Code = 1314,
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord MissingWhileCondition() =>
        new()
        {
            Message = "While loop requires a condition",
            Code = 1315,
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord MismatchWhileConditionNodeType() =>
        new()
        {
            Message = "While predicate must be a variable, method call, constructor, indexer or property",
            Code = 1316,
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord ConditionMustHaveBaseClause() =>
        new()
        {
            Message = "Condition requires a base clause",
            Code = 1317,
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord ConditionClauseMustHaveBody() =>
        new()
        {
            Message = "Condition requires a body",
            Code = 1318,
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord ConditionClausePredicateNodeTypeMismatch() =>
        new()
        {
            Message = "Condition clause predicate must be a variable, method call, property or indexer",
            Code = 1319,
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord ClausePredicateMustExist() =>
        new()
        {
            Message = "Condition clause predicate must exist",
            Code = 1320,
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord UnaryNodeRequiresUnderlyingNode() =>
        new()
        {
            Message = "Unary node require inner node",
            Code = 1321,
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord UnaryNodeInnerNodeTypeMismatch() =>
        new()
        {
            Message = "Inner node must be member, property, constant, method call, literal, indexer",
            Code = 1322,
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord CallNodeRequireCallerNode() =>
        new()
        {
            Message = "Call node requires a caller node",
            Code = 1323,
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord CallerNodeTypeMismatch() =>
        new()
        {
            Message = "Caller must be a member, property or indexer, cast, unary, binary node",
            Code = 1324,
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord ArgNodeMustNotBeNull() =>
        new()
        {
            Message = "Arg node in call args must not be null",
            Code = 1325,
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord ArgMustBeValueReturningNode() =>
        new()
        {
            Message = "Call argument must return a value",
            Code = 1326,
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord IndexNodeRequireIndexableNode() =>
        new()
        {
            Message = "Index node requires an indexable node",
            Code = 1327,
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord IndexableNodeTypeMismatch() =>
        new()
        {
            Message = "Indexable must be a member, property, indexer, binary, cast, unary",
            Code = 1328,
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord IndexerArgMustBeReturningNode() =>
        new()
        {
            Message = "Indexer node argument must return a value",
            Code = 1329,
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord CannotAccessMemberFromNothing() =>
        new()
        {
            Message = "Cannot access member from nothing",
            Code = 1330,
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord CannotAccessNothing() =>
        new()
        {
            Message = "Cannot access nothing",
            Code = 1331,
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord AccessibleNodeTypeMismatch() =>
        new()
        {
            Message = "Accessible node must return a typed value",
            Code = 1332,
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord MemberAccessTargetMustBeMember() =>
        new()
        {
            Message = "Member access target must be a member",
            Code = 1333,
            Level = ExceptionLevel.Error
        };

    #endregion
}