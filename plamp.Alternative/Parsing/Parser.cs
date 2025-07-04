using System.Collections.Generic;
using System.Linq;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Alternative.AstExtensions;
using plamp.Alternative.Tokenization.Enums;
using plamp.Alternative.Tokenization.Token;
using RootNode = plamp.Alternative.AstExtensions.RootNode;

namespace plamp.Alternative.Parsing;

public static class Parser
{
    public static RootNode ParseFile(ParsingContext context)
    {
        var topLevelList = new List<NodeBase>();
        while (context.Sequence.Current().GetType() != typeof(EndOfFile))
        {
            if (TryParseTopLevel(context, out var topLevel) && topLevel != null) topLevelList.Add(topLevel);
        }

        return new RootNode();
    }

    public static bool TryParseTopLevel(ParsingContext context, out NodeBase? topLevel)
    {
        topLevel = null;
        if(!context.Sequence.MoveNextNonWhiteSpace()) return false;
        switch (context.Sequence.Current())
        {
            case KeywordToken { Keyword: Keywords.Use }:
                if(!TryParseUse(context, out var import)) return false;
                topLevel = import;
                return true;
            case KeywordToken {Keyword: Keywords.Module}:
                if(!TryParseModuleDef(context, out var module)) return false;
                topLevel = module;
                return true;
            case KeywordToken {Keyword: Keywords.Fn}:
                if(!TryParseFunc(context, out var fn)) return false;
                topLevel = fn;
                return true;
            default:
                AddUnexpectedTokenException(context);
                return false;
        }
    }

    #region Parsing use

    public static bool TryParseUse(ParsingContext context, out ImportNode? importNode)
    {
        importNode = null;
        if (context.Sequence.Current() is not KeywordToken { Keyword: Keywords.Use }) return false;
        var moduleName = GetModuleName(context);

        while (context.Sequence.MoveNextNonWhiteSpace())
        {
            context.Sequence.MoveNextNonWhiteSpace();
            if (context.Sequence.Current() is EndOfFile) return false;
            if (context.Sequence.Current() is EndOfStatement or OpenCurlyBracket) break;
            AddUnexpectedTokenException(context);
        }
        
        if (context.Sequence.Current() is EndOfStatement)
        {
            context.Sequence.MoveNextNonWhiteSpace();
            importNode = new ImportNode(moduleName, null);
            return true;
        }
        
        var importedItems = GetImportItems(context);
        if (context.Sequence.Current() is EndOfFile eof)
        {
            var record = PlampNativeExceptionInfo.ExpectedClosingCurlyBracket();
            context.Exceptions.Add(new PlampException(record, eof.Start, eof.End, context.FileName, null));
        }

        if (context.Sequence.Current() is CloseCurlyBracket)
        {
            context.Sequence.MoveNext();
        }
        
        if (!importedItems.Any()) return false;
        importNode = new ImportNode(moduleName, importedItems);
        return true;
    }

    private static string GetModuleName(ParsingContext context)
    {
        var name = string.Empty;
        bool? prevWord = false;
        while (context.Sequence.PeekNext() is Word or OperatorToken)
        {
            context.Sequence.MoveNext();
            if ((!prevWord.HasValue || !prevWord.Value) && context.Sequence.Current() is Word word)
            {
                name += word.GetStringRepresentation();
                prevWord = true;
            }
            else if ((!prevWord.HasValue || prevWord.Value) && context.Sequence.Current() is OperatorToken { Operator: OperatorEnum.Access } op)
            {
                name += op.GetStringRepresentation();
                prevWord = false;
            }
            else
            {
                AddUnexpectedTokenException(context);
                prevWord = null;
            }
        }

        return name;
    }

    private static bool TryParseImportItem(ParsingContext context, out ImportItemNode? importItem)
    {
        importItem = null;
        if (context.Sequence.PeekNextNonWhiteSpace() is not Word word) return false;
        context.Sequence.MoveNextNonWhiteSpace();
        var itemName = word.GetStringRepresentation();
        if (context.Sequence.PeekNextNonWhiteSpace() is KeywordToken { Keyword: Keywords.As })
        {
            context.Sequence.MoveNextNonWhiteSpace();
            if (context.Sequence.PeekNextNonWhiteSpace() is not Word alias)
            {
                var record = PlampNativeExceptionInfo.AliasExpected();
                context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentStart,
                    context.Sequence.CurrentEnd, context.FileName, null));
                return false;
            }

            importItem = new ImportItemNode(itemName, alias.GetStringRepresentation());
        }
        else
        {
            importItem = new ImportItemNode(itemName, itemName);
        }

        return true;
    }

    private static List<ImportItemNode> GetImportItems(ParsingContext context)
    {
        var importedItems = new List<ImportItemNode>();
        bool? importedItem = false;
        while (context.Sequence.MoveNextNonWhiteSpace())
        {
            if(context.Sequence.Current() is EndOfFile or OpenCurlyBracket) break;
            
            if ((!importedItem.HasValue || !importedItem.Value) && context.Sequence.Current() is Word)
            {
                context.Sequence.RollBackToNonWhiteSpace();
                if (TryParseImportItem(context, out var item) && item != null)
                {
                    importedItems.Add(item);
                }
                importedItem = true;
                continue;
            }

            if (context.Sequence.Current() is Comma)
            {
                importedItem = false;
                continue;
            }
            
            AddUnexpectedTokenException(context);
            importedItem = null;
        }

        return importedItems;
    }

    #endregion

    #region Parsing module

    public static bool TryParseModuleDef(ParsingContext context, out ModuleDefinitionNode? module)
    {
        module = null;
        if(context.Sequence.Current() is not KeywordToken { Keyword: Keywords.Module }) return false;
        var moduleName = GetModuleName(context);
        module = new ModuleDefinitionNode(moduleName);
        
        if (context.Sequence.MoveNext() && context.Sequence.Current() is not EndOfStatement)
        {
            var record = PlampNativeExceptionInfo.ExpectedEndOfStatement();
            context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentStart, context.Sequence.CurrentEnd, context.FileName, null));
        }
        return true;
    }

    #endregion

    #region Parsing func

    public static bool TryParseFunc(ParsingContext context, out DefNode? func)
    {
        func = null;
        if (context.Sequence.Current() is not KeywordToken { Keyword: Keywords.Fn }) return false;
        if(!context.Sequence.MoveNextNonWhiteSpace()) return false;

        var name = "";
        
        if (context.Sequence.Current() is Word funcName)
        {
            name = funcName.GetStringRepresentation();
            if(!context.Sequence.MoveNextNonWhiteSpace()) return false;
        }
        else
        {
            var current = context.Sequence.Current();
            var record = PlampNativeExceptionInfo.ExpectedFuncName();
            context.Exceptions.Add(new PlampException(record, current.Start, current.End, context.FileName, null));
        }

        var args = ParseArgSequence(context).Cast<NodeBase>().ToList();
        
        if (!context.Sequence.MoveNextNonWhiteSpace()) return false;
        if(!TryParseType(context, out var type) || type == null) return false;
        if (!context.Sequence.MoveNextNonWhiteSpace()) return false;
        if (!TryParseBody(context, out var body) || body == null) return false;
        if (name == "") return false;
        func = new DefNode(type, new MemberNode(name), args, body);
        return true;
    }

    public static List<ParameterNode> ParseArgSequence(ParsingContext context)
    {
        if (context.Sequence.Current() is not OpenParen)
        {
            var current = context.Sequence.Current();
            var record = PlampNativeExceptionInfo.ExpectedOpenParen();
            context.Exceptions.Add(new PlampException(record, current.Start, current.End, context.FileName, null));
        }
        
        var args = new List<ParameterNode>();
        bool? comma = false;
        while (context.Sequence.Current() is not CloseParen or EndOfFile)
        {
            if ((!comma.HasValue || comma.Value) && context.Sequence.Current() is Word)
            {
                if(TryParseArg(context, out var arg) && arg != null) args.Add(arg);
                comma = false;
            }
            else if ((!comma.HasValue || !comma.Value) && context.Sequence.Current() is Comma)
            {
                comma = true;
                if(!context.Sequence.MoveNextNonWhiteSpace()) break;
            }
            else
            {
                comma = null;
                AddUnexpectedTokenException(context);
            }
        }
        if (context.Sequence.Current() is EndOfFile)
        {
            var record = PlampNativeExceptionInfo.ExpectedCloseParen();
            context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentStart, context.Sequence.CurrentEnd, context.FileName, null));
            
        }
        
        return args;
    }

    public static bool TryParseArg(ParsingContext context, out ParameterNode? arg)
    {
        arg = null;
        if(context.Sequence.Current() is not Word) return false;
        if(!TryParseType(context, out var type) || type == null) return false;
        
        if (!context.Sequence.MoveNextNonWhiteSpace() || context.Sequence.Current() is not Word argName)
        {
            var record = PlampNativeExceptionInfo.ExpectedArgName();
            context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentStart,
                context.Sequence.CurrentEnd, context.FileName, null));
            return false;
        }
        arg = new ParameterNode(type, new MemberNode(argName.GetStringRepresentation()));
        return true;
    }

    #endregion

    #region Util

    private static void AddUnexpectedTokenException(ParsingContext context)
    {
        var token = context.Sequence.Current();
        var record =
            PlampNativeExceptionInfo.UnexpectedToken(token.GetStringRepresentation());
        context.Exceptions.Add(new PlampException(record, token.Start, token.End, context.FileName, null));
    }

    public static bool TryParseBody(ParsingContext context, out BodyNode? body)
    {
        body = null;
        var expressions = new List<NodeBase>();
        if (context.Sequence.Current() is not OpenCurlyBracket)
        {
            if (TryParseStatement(context, out var expression) && expression != null) expressions.Add(expression);
            ConsumeEndOfStatement(context);
            body = new BodyNode(expressions);
            return true;
        }

        while (context.Sequence.MoveNextNonWhiteSpace())
        {
            if (context.Sequence.Current() is EndOfFile)
            {
                var record = PlampNativeExceptionInfo.ExpectedClosingCurlyBracket();
                context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentStart,
                    context.Sequence.CurrentEnd, context.FileName, null));
                break;
            }
            if(context.Sequence.Current() is CloseCurlyBracket) break;
            
            if (TryParseStatement(context, out var expression) && expression != null)
            {
                expressions.Add(expression);
                ConsumeEndOfStatement(context);
            }
        }
        
        body = new BodyNode(expressions);
        return true;
    }

    public static bool TryParseStatement(ParsingContext context, out NodeBase? expression)
    {
        expression = null;
        switch (context.Sequence.Current())
        {
            case KeywordToken {Keyword: Keywords.If}:
                if(!TryParseCondition(context, out var condition)) return false;
                expression = condition;
                break;
            case KeywordToken {Keyword: Keywords.While}:
                if(!TryParseWhileLoop(context, out var loop)) return false;
                expression = loop;
                break;
            case KeywordToken {Keyword: Keywords.Break}:
                expression = new BreakNode();
                ConsumeEndOfStatement(context);
                break;
            case KeywordToken {Keyword: Keywords.Continue}:
                expression = new ContinueNode();
                ConsumeEndOfStatement(context);
                break;
            case KeywordToken {Keyword: Keywords.Return}:
                if(!TryParseReturn(context, out var node)) return false;
                expression = node;
                break;
            default:
                AddUnexpectedTokenException(context);
                context.Sequence.MoveNextNonWhiteSpace();
                break;
        }
        
        context.Sequence.MoveNextNonWhiteSpace();
        return true;
    }

    public static bool TryParseCondition(ParsingContext context, out ConditionNode? condition)
    {
        condition = null;
        if(context.Sequence.Current() is not KeywordToken{Keyword: Keywords.If}) return false;
        if (!context.Sequence.MoveNextNonWhiteSpace()) return false;

        var conditionParsed = TryParseConditionPredicate(context, out var expression);
        var bodyParsed = TryParseBody(context, out var body);

        if (context.Sequence.Current() is not KeywordToken { Keyword: Keywords.Else })
        {
            condition = new ConditionNode(expression, body, null);
            return conditionParsed && bodyParsed;
        }
        
        var elseParsed = TryParseBody(context, out var elseBody);
        condition = new ConditionNode(expression, body, elseBody);
        return conditionParsed && bodyParsed && elseParsed;
    }

    public static bool TryParseWhileLoop(ParsingContext context, out WhileNode? loop)
    {
        loop = null;
        if (context.Sequence.Current() is not KeywordToken{Keyword:Keywords.While}) return false;
        if (!context.Sequence.MoveNextNonWhiteSpace()) return false;
        var predicateParsed = TryParseConditionPredicate(context, out var predicate);
        var bodyParsed = TryParseBody(context, out var body);
        loop = new WhileNode(predicate, body);
        return predicateParsed && bodyParsed;
    }

    public static bool TryParseConditionPredicate(ParsingContext context, out NodeBase? conditionPredicate)
    {
        if (context.Sequence.Current() is not OpenParen)
        {
            var record = PlampNativeExceptionInfo.ExpectedOpenParen();
            context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentStart, context.Sequence.CurrentEnd, context.FileName, null));
        }
        var conditionParsed = TryParseExpression(context, out conditionPredicate);

        if (context.Sequence.Current() is not CloseParen)
        {
            var record = PlampNativeExceptionInfo.ExpectedCloseParen();
            context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentStart, context.Sequence.CurrentEnd, context.FileName, null));
        }

        return conditionParsed;
    }

    public static bool TryParseReturn(ParsingContext context, out ReturnNode? node)
    {
        node = null;
        if (context.Sequence.Current() is not KeywordToken{Keyword: Keywords.Return}) return false;
        if (!context.Sequence.MoveNextNonWhiteSpace()) return false;
        TryParseExpression(context, out var expr);
        ConsumeEndOfStatement(context);
        node = new ReturnNode(expr);
        return true;
    }

    private static void ConsumeEndOfStatement(ParsingContext context)
    {
        if (context.Sequence.Current() is EndOfStatement) return;
        var record = PlampNativeExceptionInfo.ExpectedEndOfStatement();
        context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentStart, context.Sequence.CurrentEnd, context.FileName, null));
    }
    
    public static bool TryParseExpression(ParsingContext context, out NodeBase? expression)
    {
        expression = null;
        var assignContext = context.Fork();
        var assignmentParsed = TryParseAssignment(assignContext, out var assignment);
        if (assignmentParsed)
        {
            context.Merge(assignContext);
            expression = assignment;
            return true;
        }
        
        var callContext = context.Fork();
        var callParsed = TryParseMethodCall(callContext, out var call);
        if (callParsed)
        {
            context.Merge(callContext);
            expression = call;
            return true;
        }
        
        var unaryContext = context.Fork();
        var unaryParsed = TryParseIncOrDec(unaryContext, out var unary);
        if (unaryParsed)
        {
            context.Merge(unaryContext);
            expression = unary;
            return true;
        }
        
        var variableDefContext = context.Fork();
        var definitionParsed =  TryParseVariableDefinition(variableDefContext, out var definition);
        if (definitionParsed)
        {
            context.Merge(variableDefContext);
            expression = definition;
            return true;
        }
        
        if(!context.Sequence.MoveNextNonWhiteSpace()) return false;
        var record = PlampNativeExceptionInfo.ExpectedExpression();
        context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentStart, context.Sequence.CurrentEnd, context.FileName, null));
        return false;
    }

    public static bool TryParseAssignment(ParsingContext context, out NodeBase? assignment)
    {
        
    }

    public static bool TryParseMethodCall(ParsingContext context, out NodeBase? call)
    {
        
    }

    public static bool TryParseIncOrDec(ParsingContext context, out NodeBase? incOrDec)
    {
        
    }

    public static bool TryParseVariableDefinition(ParsingContext context, out NodeBase? variableDefinition)
    {
        
    }

    public static bool TryParseType(ParsingContext context, out TypeNode? returnType)
    {
        returnType = null;
        if (context.Sequence.Current() is not Word typeName)
        {
            var record = PlampNativeExceptionInfo.ExpectedTypeName();
            context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentStart, context.Sequence.CurrentEnd, context.FileName, null));
            return false;
        }

        returnType = new TypeNode(new MemberNode(typeName.GetStringRepresentation()), []);
        return true;
    }

    #endregion
}