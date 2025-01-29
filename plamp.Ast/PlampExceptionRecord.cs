namespace plamp.Ast;

/// <summary>
/// Inlined exception
/// </summary>
public record PlampExceptionRecord()
{
    public required string Message { get; init; }

    public required int Code { get; init; }
    
    public required ExceptionLevel Level { get; init; }
}