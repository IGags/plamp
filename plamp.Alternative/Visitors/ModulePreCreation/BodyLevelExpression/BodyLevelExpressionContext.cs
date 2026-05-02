namespace plamp.Alternative.Visitors.ModulePreCreation.BodyLevelExpression;

/// <summary>
/// Контекст обхода для валидации выражений на уровне тела чего-либо.
/// </summary>
public class BodyLevelExpressionContext(PreCreationContext other) : PreCreationContext(other);