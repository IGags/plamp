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

    #endregion
}