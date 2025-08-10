using plamp.Abstractions.Ast;

namespace plamp.Alternative;

public static class PlampExceptionInfo
{
    #region Tokenization(1000-1099)

    public static PlampExceptionRecord UnexpectedToken(string stringToken) =>
        new()
        {
            Message = $"Unexpected token \"{stringToken}\"",
            Code = "TOK1000",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord StringIsNotClosed() => 
        new()
        {
            Message = "String is not closed",
            Code = "TOK1030",
            Level = ExceptionLevel.Error
        };


    public static PlampExceptionRecord InvalidEscapeSequence(string sequence) =>
        new()
        {
            Message = $"Invalid escape sequence \"{sequence}\"",
            Code = "TOK1031",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord UnknownNumberFormat() =>
        new()
        {
            Message = "Unknown number format",
            Code = "TOK1032",
            Level = ExceptionLevel.Error
        };

    #endregion

    #region PRSing(1100-1299)

    public static PlampExceptionRecord ExpectedExpression() =>
        new()
        {
            Message = "Expected expression",
            Code = "PRS1103",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord ExpectedArgDefinition() =>
        new()
        {
            Message = "Expected arg definition",
            Code = "PRS1109",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord AliasExpected() =>
        new()
        {
            Code = "PRS1117",
            Level = ExceptionLevel.Error,
            Message = "After as keyword expected alias"
        };

    public static PlampExceptionRecord ExpectedClosingCurlyBracket() =>
        new()
        {
            Code = "PRS1118",
            Level = ExceptionLevel.Error,
            Message = "Expected closing curly bracket"
        };

    public static PlampExceptionRecord ExpectedEndOfStatement() =>
        new()
        {
            Code = "PRS1119",
            Level = ExceptionLevel.Error,
            Message = "Expected end of statement"
        };

    public static PlampExceptionRecord ExpectedFuncName()
    {
        return new()
        {
            Code = "PRS1120",
            Level = ExceptionLevel.Error,
            Message = "Expected function name"
        };
    }

    public static PlampExceptionRecord ExpectedOpenParen()
    {
        return new()
        {
            Code = "PRS1121",
            Level = ExceptionLevel.Error,
            Message = "Expected open paren"
        };
    }

    public static PlampExceptionRecord ExpectedCloseParen()
    {
        return new()
        {
            Code = "PRS1122",
            Level = ExceptionLevel.Error,
            Message = "Expected close paren"
        };
    }

    public static PlampExceptionRecord ExpectedTypeName()
    {
        return new()
        {
            Code = "PRS1123",
            Level = ExceptionLevel.Error,
            Message = "Expected type name"
        };
    }

    public static PlampExceptionRecord ExpectedArgName()
    {
        return new()
        {
            Code = "PRS1124",
            Level = ExceptionLevel.Error,
            Message = "Expected argument name"
        };
    }

    public static PlampExceptionRecord ExpectedAssignment()
    {
        return new()
        {
            Code = "PRS1125",
            Level = ExceptionLevel.Error,
            Message = "Expected assignment"
        };
    }

    public static PlampExceptionRecord ExpectedAssignmentSource()
    {
        return new()
        {
            Code = "PRS1126",
            Level = ExceptionLevel.Error,
            Message = "Expected assignment source"
        };
    }

    public static PlampExceptionRecord ExpectedVarName()
    {
        return new()
        {
            Code = "PRS1127",
            Level = ExceptionLevel.Error,
            Message = "Expected var name"
        };
    }

    public static PlampExceptionRecord ExpectedSubmoduleName()
    {
        return new()
        {
            Code = "PRS1128",
            Level = ExceptionLevel.Error,
            Message = "Expected submodule name"
        };
    }

    public static PlampExceptionRecord TypesIsNotSupported()
    {
        return new()
        {
            Code = "PRS1129",
            Level = ExceptionLevel.Error,
            Message = "Types is not supported in current version of language",
        };
    }

    public static PlampExceptionRecord ExpectedModuleName()
    {
        return new()
        {
            Code = "PRS1130",
            Level = ExceptionLevel.Error,
            Message = "Expected module name"
        };
    }
    
    #endregion

    #region Semantics

    public static PlampExceptionRecord DuplicateVariableDefinition()
    {
        return new()
        {
            Code = "SEM1300",
            Level = ExceptionLevel.Error,
            Message = "Variable already defined"
        };
    }

    public static PlampExceptionRecord CannotAssign()
    {
        return new()
        {
            Code = "SEM1301",
            Level = ExceptionLevel.Error,
            Message = "Assignment target and source types are differs"
        };
    }

    public static PlampExceptionRecord CannotFindMember()
    {
        return new()
        {
            Code = "SEM1302",
            Level = ExceptionLevel.Error,
            Message = "Unknown variable or arg"
        };
    }

    public static PlampExceptionRecord UnknownFunction()
    {
        return new PlampExceptionRecord()
        {
            Code = "SEM1303",
            Level = ExceptionLevel.Error,
            Message = "Cannot find function within module with this signature"
        };
    }

    public static PlampExceptionRecord PredicateMustBeBooleanType()
    {
        return new PlampExceptionRecord()
        {
            Code = "SEM1304",
            Level = ExceptionLevel.Error,
            Message = "Predicate must be a boolean type"
        };
    }

    public static PlampExceptionRecord CannotApplyOperator()
    {
        return new PlampExceptionRecord()
        {
            Code = "SEM1305",
            Level = ExceptionLevel.Error,
            Message = "Cannot apply operator, types differ"
        };
    }

    public static PlampExceptionRecord ArgumentAlreadyDefined()
    {
        return new PlampExceptionRecord()
        {
            Code = "SEM1306",
            Level = ExceptionLevel.Error,
            Message = "Argument with same name already defined"
        };
    }

    public static PlampExceptionRecord ReturnTypeMismatch()
    {
        return new PlampExceptionRecord()
        {
            Code = "SEM1307",
            Level = ExceptionLevel.Error,
            Message = "Return expression type should match with return type of func"
        };
    }

    public static PlampExceptionRecord DuplicateMemberNameInModule()
    {
        return new PlampExceptionRecord()
        {
            Code = "SEM1308",
            Level = ExceptionLevel.Error,
            Message = "Member with same name already declared in this module"
        };
    }

    public static PlampExceptionRecord DuplicateModuleDefinition()
    {
        return new PlampExceptionRecord()
        {
            Code = "SEM1309",
            Level = ExceptionLevel.Error,
            Message = "Module name already declared in this module"
        };
    }

    public static PlampExceptionRecord MemberCannotHaveSameNameAsDeclaringModule()
    {
        return new PlampExceptionRecord()
        {
            Code = "SEM1310",
            Level = ExceptionLevel.Error,
            Message = "Member cannot have same name as declaring module"
        };
    }

    public static PlampExceptionRecord ModuleMustHaveName()
    {
        return new PlampExceptionRecord()
        {
            Code = "SEM1311",
            Level = ExceptionLevel.Error,
            Message = "The code file must have a module name that belongs to. Add \"module MODULE_NAME;\""
        };
    }
    
    public static PlampExceptionRecord FuncMustReturnValue() =>
        new()
        {
            Message = "Function must return a value",
            Code = "SEM1312",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord ReturnValueIsMissing() =>
        new()
        {
            Message = "Return value is missing",
            Code = "SEM1313",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord CannotReturnValue() =>
        new()
        {
            Message = "Cannot return value from a void function",
            Code = "SEM1314",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord CannotAssignNone() =>
        new()
        {
            Message = "Cannot assign type of none",
            Code = "SEM1315",
            Level = ExceptionLevel.Error
        };

    #endregion
}