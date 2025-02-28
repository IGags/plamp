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

    #endregion
}