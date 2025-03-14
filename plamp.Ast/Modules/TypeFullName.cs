namespace plamp.Ast.Modules;

/// <summary>
/// Module + Type name
/// </summary>
public readonly record struct TypeFullName(string ModuleName, string TypeName);