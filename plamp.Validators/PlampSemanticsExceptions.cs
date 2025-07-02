using System.Collections.Generic;
using plamp.Abstractions.Ast;

namespace plamp.Validators;

public static class PlampSemanticsExceptions
{
    #region Type inference

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
    
    public static PlampExceptionRecord TypeNameMustBeMember() =>
        new()
        {
            Message = "Type name must be a member or member access",
            Code = "SEM1344",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord GenericMustBeType() =>
        new()
        {
            Message = "Generics must be a type",
            Code = "SEM1345",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord ParameterMustHaveName() =>
        new()
        {
            Message = "Parameter must have a name",
            Code = "SEM1346",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord ParameterMustHaveType() =>
        new()
        {
            Message = "Parameter must have a type",
            Code = "SEM1347",
            Level = ExceptionLevel.Error
        };
    
    
    public static PlampExceptionRecord AmbigulousTypeName(string typeName, IEnumerable<string> modules) =>
        new()
        {
            Message = $"Type with name {typeName} is defined in several modules: {string.Join(", ", modules)}",
            Code = "SEM1348",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord TypeNotFound(string typeName) =>
        new()
        {
            Message = $"Type with name {typeName} not found",
            Code = "SEM1349",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord CannotInferenceMethod(string typeAlias, string methodName) =>
        new()
        {
            Message = $"Method with name {methodName} from type {typeAlias} cannot be inference by its signature",
            Code = "SEM1350",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord MethodNotFound(string typeAlias, string methodName) =>
        new()
        {
            Message = $"Method with name {methodName} from type {typeAlias} not found",
            Code = "SEM1351",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord CannotInferenceConstructor(string typeAlias) =>
        new()
        {
            Message = $"Constructor of type {typeAlias} cannot be inferred by its signature",
            Code = "SEM1352",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord ConstructorNotFound(string typeAlias) =>
        new()
        {
            Message = $"Constructor of type {typeAlias} not found",
            Code = "SEM1353",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord CannotApplyBinaryOperator(string operatorName, string leftType, string rightType) =>
        new()
        {
            Message = $"Cannot apply binary operator {operatorName} to {leftType} and {rightType}",
            Code = "SEM1354",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord CannotApplyUnaryOperator(string operatorName, string innerType) =>
        new()
        {
            Message = $"Cannot apply unary operator {operatorName} to {innerType}",
            Code = "SEM1355",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord InvalidAssignmentTarget() =>
        new()
        {
            Message = "Assignment is allowed for properties or fields and variables or args",
            Code = "SEM1356",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord VariableIsNotDefinedYet(string varName) =>
        new()
        {
            Message = $"Variable {varName} is not defined or encounters later",
            Code = "SEM1357",
            Level = ExceptionLevel.Error
        };

    public static PlampExceptionRecord AssignmentTypeMismatch(string leftType, string rightType) =>
        new()
        {
            Message = $"Cannot assign expression of type {rightType} to {rightType} target",
            Code = "SEM1358",
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