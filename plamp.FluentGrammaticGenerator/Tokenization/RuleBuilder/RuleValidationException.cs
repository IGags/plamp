using System;

namespace plamp.FluentGrammaticGenerator.Tokenization.RuleBuilder;

public class RuleValidationException(string message) : Exception(message);