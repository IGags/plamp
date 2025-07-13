namespace plamp.Abstractions.Ast;

/// <summary>
/// Inlined exception
/// </summary>
public record PlampExceptionRecord
{
    public required string Message { get; init; }

    public required string Code { get; init; }
    
    public required ExceptionLevel Level { get; init; }
}