using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Binary;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Abstractions.Ast.Node.Definitions.Variable;
using plamp.Abstractions.Ast.Node.Unary;
using plamp.Alternative.Tokenization.Enums;
using plamp.Alternative.Tokenization.Token;

namespace plamp.Alternative.Parsing;

public static class Parser
{
    public static RootNode ParseFile(ParsingContext context)
    {
        var topLevelList = new List<NodeBase>();
        while (context.Sequence.Current() is not EndOfFile)
        {
            if (TryParseTopLevel(context, out var topLevel)) topLevelList.Add(topLevel);
        }

        var imports = new List<ImportNode>();
        var modules = new List<ModuleDefinitionNode>();
        var functions = new List<FuncNode>();
        foreach (var statement in topLevelList)
        {
            if (statement is ImportNode import) imports.Add(import);
            if (statement is ModuleDefinitionNode def) modules.Add(def);
            if (statement is FuncNode defNode) functions.Add(defNode);
        }

        if (modules.Count > 1)
        {
            var record = PlampExceptionInfo.DuplicateModuleDefinition();
            foreach (var module in modules)
            {
                context.Exceptions.Add(context.SymbolTable.SetExceptionToNode(module, record, context.FileName));
            }

            modules = null;
        }
        var moduleDef = modules?.FirstOrDefault();
        var node = new RootNode(imports, moduleDef, functions);
        context.SymbolTable.AddSymbol(node, default, default);
        return node;
    }

    public static bool TryParseTopLevel(
        ParsingContext context, 
        [NotNullWhen(true)] out NodeBase? topLevel)
    {
        topLevel = null;
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
                context.Sequence.MoveNextNonWhiteSpace();
                return false;
        }
    }

    #region Parsing use

    public static bool TryParseUse(
        ParsingContext context, 
        [NotNullWhen(true)] out ImportNode? importNode)
    {
        importNode = null;
        var importKeyword = context.Sequence.Current();
        if (importKeyword is not KeywordToken { Keyword: Keywords.Use }) return false;
        context.Sequence.MoveNextNonWhiteSpace();
        
        var moduleName = GetModuleNameOrDefault(context);
        if (moduleName == null) return false;
        
        if (context.Sequence.Current() is OpenCurlyBracket)
        {
            var importStart = context.Sequence.CurrentStart;
            if(!GetImportItems(context, out var list)) return false;
            var importEnd = context.Sequence.CurrentStart;
            importNode = new ImportNode(moduleName, list);
            context.SymbolTable.AddSymbol(importNode, importStart, importEnd);
            return true;
        }
        
        importNode = new ImportNode(moduleName, null);
        context.SymbolTable.AddSymbol(importNode, importKeyword.Start, importKeyword.End);
        ConsumeEndOfStatement(context);
        return true;
    }

    private static string? GetModuleNameOrDefault(ParsingContext context)
    {
        if (context.Sequence.Current() is not Word modName)
        {
            var record = PlampExceptionInfo.ExpectedModuleName();
            var current = context.Sequence.Current();
            context.Exceptions.Add(new PlampException(record, current.Start, current.End, context.FileName));
            return null;
        }
        var name = modName.GetStringRepresentation();
        context.Sequence.MoveNextNonWhiteSpace();
        
        while (context.Sequence.Current() is OperatorToken { Operator: OperatorEnum.Access })
        {
            context.Sequence.MoveNextNonWhiteSpace();
            if (context.Sequence.Current() is Word subName)
            {
                name += "." + subName.GetStringRepresentation();
                context.Sequence.MoveNextNonWhiteSpace();
            }
            else
            {
                var record = PlampExceptionInfo.ExpectedSubmoduleName();
                context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentStart,
                    context.Sequence.CurrentStart, context.FileName));
                break;
            }
        }

        return name;
    }

    private static bool TryParseImportItem(
        ParsingContext context, 
        [NotNullWhen(true)] out ImportItemNode? importItem)
    {
        importItem = null;
        if (context.Sequence.Current() is not Word word) return false;
        var importStart = context.Sequence.CurrentStart;
        var importEnd = context.Sequence.CurrentEnd;
        var itemName = word.GetStringRepresentation();
        context.Sequence.MoveNextNonWhiteSpace();
        if (context.Sequence.Current() is KeywordToken { Keyword: Keywords.As })
        {
            context.Sequence.MoveNextNonWhiteSpace();
            if (context.Sequence.Current() is not Word alias)
            {
                var record = PlampExceptionInfo.AliasExpected();
                context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentStart,
                    context.Sequence.CurrentEnd, context.FileName));
                return false;
            }
            importEnd = context.Sequence.CurrentEnd;
            context.Sequence.MoveNextNonWhiteSpace();
            importItem = new ImportItemNode(itemName, alias.GetStringRepresentation());
        }
        else
        {
            importItem = new ImportItemNode(itemName, itemName);
        }
        
        context.SymbolTable.AddSymbol(importItem, importStart, importEnd);
        return true;
    }

    private static bool GetImportItems(ParsingContext context, out List<ImportItemNode> imports)
    {
        imports = [];
        if (context.Sequence.Current() is not OpenCurlyBracket) return false;
        context.Sequence.MoveNextNonWhiteSpace();
        bool? importedItem = false;
        while (true)
        {
            if (context.Sequence.Current() is CloseCurlyBracket)
            {
                context.Sequence.MoveNextNonWhiteSpace();
                return true;
            }
            if(context.Sequence.Current() is EndOfFile) break;
            
            if ((!importedItem.HasValue || !importedItem.Value) && context.Sequence.Current() is Word)
            {
                if (TryParseImportItem(context, out var item))
                {
                    imports.Add(item);
                }
                importedItem = true;
                continue;
            }

            if (context.Sequence.Current() is Comma)
            {
                importedItem = false;
                context.Sequence.MoveNextNonWhiteSpace();
                continue;
            }
            
            AddUnexpectedTokenException(context);
            importedItem = null;
        }

        var record = PlampExceptionInfo.ExpectedClosingCurlyBracket();
        context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentStart, context.Sequence.CurrentEnd,
            context.FileName));

        return true;
    }

    #endregion

    #region Parsing module

    public static bool TryParseModuleDef(
        ParsingContext context,
        [NotNullWhen(true)] out ModuleDefinitionNode? module)
    {
        module = null;
        if(context.Sequence.Current() is not KeywordToken { Keyword: Keywords.Module }) return false;
        var defStart = context.Sequence.CurrentStart;
        context.Sequence.MoveNextNonWhiteSpace();
        var moduleName = GetModuleNameOrDefault(context);
        if (moduleName == null) return false;
        module = new ModuleDefinitionNode(moduleName);
        
        var defEnd = context.Sequence.CurrentEnd;
        ConsumeEndOfStatement(context);
        context.SymbolTable.AddSymbol(module, defStart, defEnd);
        return true;
    }

    #endregion

    #region Parsing func

    public static bool TryParseFunc(
        ParsingContext context,
        [NotNullWhen(true)] out FuncNode? func)
    {
        func = null;
        if (context.Sequence.Current() is not KeywordToken { Keyword: Keywords.Fn }) return false;
        var fnToken = context.Sequence.Current();
        context.Sequence.MoveNextNonWhiteSpace();
        string name;
        
        if (context.Sequence.Current() is Word funcName)
        {
            name = funcName.GetStringRepresentation();
            context.Sequence.MoveNextNonWhiteSpace();
        }
        else
        {
            var current = context.Sequence.Current();
            var record = PlampExceptionInfo.ExpectedFuncName();
            context.Exceptions.Add(new PlampException(record, current.Start, current.End, context.FileName));
            return false;
        }

        if (!TryParseArgSequence(context, out var list)) return false;
        TypeNode? type = null;
        if (context.Sequence.Current() is Word) TryParseType(context, out type);

        if (!TryParseBody(context, out var body)) return false;
        var funcNameNode = new FuncNameNode(name);
        context.SymbolTable.AddSymbol(funcNameNode, funcName.Start, funcName.End);
        func = new FuncNode(type, funcNameNode, list, body);
        context.SymbolTable.AddSymbol(func, fnToken.Start, fnToken.End);
        return true;
    }

    public static bool TryParseArgSequence(
        ParsingContext context,
        [NotNullWhen(true)]out List<ParameterNode>? parameterList)
    {
        parameterList = null;
        if (context.Sequence.Current() is not OpenParen)
        {
            var current = context.Sequence.Current();
            var record = PlampExceptionInfo.ExpectedOpenParen();
            context.Exceptions.Add(new PlampException(record, current.Start, current.End, context.FileName));
            return false;
        }
        context.Sequence.MoveNextNonWhiteSpace();

        if (context.Sequence.Current() is CloseParen)
        {
            parameterList = [];
            context.Sequence.MoveNextNonWhiteSpace();
            return true;
        }


        if (!TryParseArg(context, out var arg))
        {
            var record = PlampExceptionInfo.ExpectedArgDefinition();
            context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentStart,
                context.Sequence.CurrentEnd, context.FileName));
            return false;
        }
        parameterList = [arg];

        while (context.Sequence.Current() is Comma)
        {
            context.Sequence.MoveNextNonWhiteSpace();
            var fork = context.Fork();
            if (TryParseArg(fork, out arg))
            {
                parameterList.Add(arg);
                context.Merge(fork);
            }
            else
            {
                var record = PlampExceptionInfo.ExpectedArgDefinition();
                context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentStart,
                    context.Sequence.CurrentEnd, context.FileName));
                return false;
            }
        }

        if (context.Sequence.Current() is not CloseParen)
        {
            var record = PlampExceptionInfo.ExpectedCloseParen();
            context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentStart,
                context.Sequence.CurrentEnd, context.FileName));
        }
        else
        {
            context.Sequence.MoveNextNonWhiteSpace();
        }

        return true;
    }

    public static bool TryParseArg(
        ParsingContext context, 
        [NotNullWhen(true)]out ParameterNode? arg)
    {
        arg = null;
        if(context.Sequence.Current() is not Word) return false;
        var start = context.Sequence.CurrentStart;
        if(!TryParseType(context, out var type)) return false;
        
        if (context.Sequence.Current() is not Word argName)
        {
            var record = PlampExceptionInfo.ExpectedArgName();
            context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentStart,
                context.Sequence.CurrentEnd, context.FileName));
            arg = null;
            return false;
        }

        var end = context.Sequence.CurrentEnd;
        var argNameNode = new ParameterNameNode(argName.GetStringRepresentation());
        context.SymbolTable.AddSymbol(argNameNode, argName.Start, argName.End);
        arg = new ParameterNode(type, argNameNode);
        context.SymbolTable.AddSymbol(arg, start, end);
        context.Sequence.MoveNextNonWhiteSpace();
        return true;
    }

    #endregion

    #region Util

    private static void AddUnexpectedTokenException(ParsingContext context)
    {
        var token = context.Sequence.Current();
        var record =
            PlampExceptionInfo.UnexpectedToken(token.GetStringRepresentation());
        context.Exceptions.Add(new PlampException(record, token.Start, token.End, context.FileName));
    }

    public static bool TryParseBody(
        ParsingContext context, 
        [NotNullWhen(true)]out BodyNode? body)
    {
        body = null;
        var expressions = new List<NodeBase>();
        var start = context.Sequence.CurrentStart;
        FilePosition end;
        if (context.Sequence.Current() is not OpenCurlyBracket)
        {
            if (TryParseStatement(context, out var expression)) expressions.Add(expression);
            end = context.Sequence.CurrentStart;
            body = new BodyNode(expressions);
            context.SymbolTable.AddSymbol(body, start, end);
            return true;
        }

        context.Sequence.MoveNextNonWhiteSpace();
        while (context.Sequence.Current() is not EndOfFile and not CloseCurlyBracket)
        {
            if (!TryParseStatement(context, out var expression)) continue;
            expressions.Add(expression);
        }
        if (context.Sequence.Current() is EndOfFile)
        {
            var record = PlampExceptionInfo.ExpectedClosingCurlyBracket();
            context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentStart,
                context.Sequence.CurrentEnd, context.FileName));
            end = context.Sequence.CurrentStart;
            body = new BodyNode(expressions);
            context.SymbolTable.AddSymbol(body, start, end);
            return true;
        }

        end = context.Sequence.CurrentEnd;
        context.Sequence.MoveNextNonWhiteSpace();
        body = new BodyNode(expressions);
        context.SymbolTable.AddSymbol(body, start, end);
        return true;
    }

    public static bool TryParseStatement(
        ParsingContext context, 
        [NotNullWhen(true)]out NodeBase? expression)
    {
        expression = null;
        switch (context.Sequence.Current())
        {
            case KeywordToken {Keyword: Keywords.If}:
                if(!TryParseCondition(context, out var condition)) return false;
                expression = condition;
                return true;
            case KeywordToken {Keyword: Keywords.While}:
                if(!TryParseWhileLoop(context, out var loop)) return false;
                expression = loop;
                return true;
            //TODO: To separate method.
            case KeywordToken {Keyword: Keywords.Break}:
                expression = new BreakNode();
                var current = context.Sequence.Current();
                context.SymbolTable.AddSymbol(expression, current.Start, current.End);
                context.Sequence.MoveNextNonWhiteSpace();
                ConsumeEndOfStatement(context);
                return true;
            case KeywordToken {Keyword: Keywords.Continue}:
                expression = new ContinueNode();
                current = context.Sequence.Current();
                context.SymbolTable.AddSymbol(expression, current.Start, current.End);
                context.Sequence.MoveNextNonWhiteSpace();
                ConsumeEndOfStatement(context);
                return true;
            case KeywordToken {Keyword: Keywords.Return}:
                if(!TryParseReturn(context, out var node)) return false;
                expression = node;
                return true;
            case EndOfStatement:
                ConsumeEndOfStatement(context);
                break;
            default:
                var precedenceFork = context.Fork();
                if (TryParseExpression(precedenceFork, out var precedence))
                {
                    expression = precedence;
                    context.Merge(precedenceFork);
                    ConsumeEndOfStatement(context);
                    return true;
                }
                AddUnexpectedTokenException(context);
                context.Sequence.MoveNextNonWhiteSpace();
                break;
        }

        return false;
    }

    public static bool TryParseCondition(
        ParsingContext context, 
        [NotNullWhen(true)]out ConditionNode? condition)
    {
        condition = null;
        if(context.Sequence.Current() is not KeywordToken{Keyword: Keywords.If}) return false;
        var conditionToken = context.Sequence.Current();
        context.Sequence.MoveNextNonWhiteSpace();

        if (!TryParseConditionPredicate(context, out var expression)) return false;
        if (!TryParseBody(context, out var body)) return false;

        if (context.Sequence.Current() is not KeywordToken { Keyword: Keywords.Else })
        {
            condition = new ConditionNode(expression, body, null);
            context.SymbolTable.AddSymbol(condition, conditionToken.Start, conditionToken.End);
            return true;
        }

        context.Sequence.MoveNextNonWhiteSpace();
        if (!TryParseBody(context, out var elseBody)) return false;
        condition = new ConditionNode(expression, body, elseBody);
        context.SymbolTable.AddSymbol(condition, conditionToken.Start, conditionToken.End);
        return true;
    }

    public static bool TryParseWhileLoop(
        ParsingContext context, 
        [NotNullWhen(true)]out WhileNode? loop)
    {
        loop = null;
        if (context.Sequence.Current() is not KeywordToken{Keyword:Keywords.While}) return false;
        var loopToken = context.Sequence.Current();
        context.Sequence.MoveNextNonWhiteSpace();
        if (!TryParseConditionPredicate(context, out var predicate)) return false;
        if (!TryParseBody(context, out var body)) return false;
        loop = new WhileNode(predicate, body);
        context.SymbolTable.AddSymbol(loop, loopToken.Start, loopToken.End);
        return true;
    }

    public static bool TryParseConditionPredicate(
        ParsingContext context, 
        [NotNullWhen(true)]out NodeBase? conditionPredicate)
    {
        conditionPredicate = null;
        if (context.Sequence.Current() is not OpenParen)
        {
            var record = PlampExceptionInfo.ExpectedOpenParen();
            context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentStart, context.Sequence.CurrentEnd, context.FileName));
            return false;
        }

        context.Sequence.MoveNextNonWhiteSpace();
        if (!TryParsePrecedence(context, out conditionPredicate))
        {
            var record = PlampExceptionInfo.ExpectedExpression();
            var current = context.Sequence.Current();
            context.Exceptions.Add(new PlampException(record, current.Start, current.End, context.FileName));
            return false;
        }

        if (context.Sequence.Current() is not CloseParen)
        {
            var record = PlampExceptionInfo.ExpectedCloseParen();
            context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentStart, context.Sequence.CurrentEnd, context.FileName));
        }
        else
        {
            context.Sequence.MoveNextNonWhiteSpace();
        }

        return true;
    }

    public static bool TryParseReturn(
        ParsingContext context, 
        [NotNullWhen(true)]out ReturnNode? node)
    {
        node = null;
        if (context.Sequence.Current() is not KeywordToken{Keyword: Keywords.Return}) return false;
        var returnToken = context.Sequence.Current();
        context.Sequence.MoveNextNonWhiteSpace();
        if (context.Sequence.Current() is EndOfStatement)
        {
            ConsumeEndOfStatement(context);
            node = new ReturnNode(null);
            context.SymbolTable.AddSymbol(node, returnToken.Start, returnToken.End);
            return true;
        }

        if (!TryParsePrecedence(context, out var expr))
        {
            var record = PlampExceptionInfo.ExpectedExpression();
            var current = context.Sequence.Current();
            context.Exceptions.Add(new PlampException(record, current.Start, current.End, context.FileName));
            return false;
        }
        ConsumeEndOfStatement(context);
        node = new ReturnNode(expr);
        context.SymbolTable.AddSymbol(node, returnToken.Start, returnToken.End);
        return true;
    }

    private static void ConsumeEndOfStatement(ParsingContext context)
    {
        if (context.Sequence.Current() is EndOfStatement)
        {
            context.Sequence.MoveNextNonWhiteSpace();
            return;
        }
        var record = PlampExceptionInfo.ExpectedEndOfStatement();
        context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentStart, context.Sequence.CurrentEnd, context.FileName));
    }
    
    public static bool TryParseExpression(
        ParsingContext context, 
        [NotNullWhen(true)]out NodeBase? expression)
    {
        expression = null;
        var assignContext = context.Fork();
        if (TryParseAssignment(assignContext, out var assignment))
        {
            context.Merge(assignContext);
            expression = assignment;
            return true;
        }
        
        var variableDefContext = context.Fork();
        if (TryParseVariableDefinition(variableDefContext, out var definition))
        {
            context.Merge(variableDefContext);
            expression = definition;
            return true;
        }
        
        var precedenceContext = context.Fork();
        if (TryParsePrecedence(precedenceContext, out var precedence))
        {
            context.Merge(precedenceContext);
            expression = precedence;
            return true;
        }
        
        context.Sequence.MoveNextNonWhiteSpace();
        var record = PlampExceptionInfo.ExpectedExpression();
        context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentStart, context.Sequence.CurrentEnd, context.FileName));
        return false;
    }

    //TODO: Split to separate methods
    public static bool TryParseAssignment(
        ParsingContext context, 
        [NotNullWhen(true)] out NodeBase? assignment)
    {
        assignment = null;
        var definitionFork = context.Fork();
        if (TryParseVariableDefinition(definitionFork, out var definition))
        {
            context.Merge(definitionFork);
            if (context.Sequence.Current() is not OperatorToken { Operator: OperatorEnum.Assign })
            {
                var record = PlampExceptionInfo.ExpectedAssignment();
                context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentStart,
                    context.Sequence.CurrentEnd, context.FileName));
                return false;
            }

            var assign = context.Sequence.Current();
            context.Sequence.MoveNextNonWhiteSpace();
            
            if (TryParsePrecedence(context, out var right))
            {
                assignment = new AssignNode(definition, right);
                context.SymbolTable.AddSymbol(assignment, assign.Start, assign.End);
                return true;
            }

            var exceptionRecord = PlampExceptionInfo.ExpectedAssignmentSource();
            context.Exceptions.Add(new PlampException(exceptionRecord, context.Sequence.CurrentStart,
                context.Sequence.CurrentEnd, context.FileName));
            return false;
        }

        var memberFork = context.Fork();
        if (memberFork.Sequence.Current() is Word member)
        {
            context.Merge(memberFork);
            context.Sequence.MoveNextNonWhiteSpace();
            if (context.Sequence.Current() is not OperatorToken { Operator: OperatorEnum.Assign })
            {
                var record = PlampExceptionInfo.ExpectedAssignment();
                context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentStart,
                    context.Sequence.CurrentEnd, context.FileName));
                return false;
            }

            var assign = context.Sequence.Current();
            context.Sequence.MoveNextNonWhiteSpace();
            
            if (TryParsePrecedence(context, out var right))
            {
                definition = new MemberNode(member.GetStringRepresentation());
                context.SymbolTable.AddSymbol(definition, member.Start, member.End);
                assignment = new AssignNode(definition, right);
                context.SymbolTable.AddSymbol(assignment, assign.Start, assign.End);
                return true;
            }

            var exceptionRecord = PlampExceptionInfo.ExpectedAssignmentSource();
            context.Exceptions.Add(new PlampException(exceptionRecord, context.Sequence.CurrentStart,
                context.Sequence.CurrentEnd, context.FileName));
        }

        return false;
    }

    public static bool TryParseFuncCall(
        ParsingContext context, 
        [NotNullWhen(true)]out NodeBase? call)
    {
        call = null;
        if (context.Sequence.Current() is not Word funcName) return false;
        var start = context.Sequence.CurrentStart;
        context.Sequence.MoveNextNonWhiteSpace();
        
        if (context.Sequence.Current() is not OpenParen)
        {
            var record = PlampExceptionInfo.ExpectedOpenParen();
            context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentStart,
                context.Sequence.CurrentEnd, context.FileName));
            return false;
        }

        context.Sequence.MoveNextNonWhiteSpace();

        var argExpressions = new List<NodeBase>();
        FilePosition end;
        if (context.Sequence.Current() is CloseParen)
        {
            end = context.Sequence.CurrentEnd;
            call = new CallNode(null, new MemberNode(funcName.GetStringRepresentation()), argExpressions);
            context.Sequence.MoveNextNonWhiteSpace();
            context.SymbolTable.AddSymbol(call, start, end);
            return true;
        }

        if (!TryParsePrecedence(context, out var arg))
        {
            var record = PlampExceptionInfo.ExpectedExpression();
            context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentStart,
                context.Sequence.CurrentEnd, context.FileName));
            return false;
        }
        argExpressions.Add(arg);
        
        while (context.Sequence.Current() is Comma)
        {
            context.Sequence.MoveNextNonWhiteSpace();
            if (TryParsePrecedence(context, out arg))
            {
                argExpressions.Add(arg);
            }
            else
            {
                var record = PlampExceptionInfo.ExpectedExpression();
                context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentStart,
                    context.Sequence.CurrentEnd, context.FileName));
                call = null;
                return false;
            }
        }
        
        if (context.Sequence.Current() is not CloseParen)
        {
            var record = PlampExceptionInfo.ExpectedCloseParen();
            context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentStart,
                context.Sequence.CurrentEnd, context.FileName));
            end = context.Sequence.CurrentStart;
        }
        else
        {
            end = context.Sequence.CurrentEnd;
            context.Sequence.MoveNextNonWhiteSpace();
        }

        call = new CallNode(null, new MemberNode(funcName.GetStringRepresentation()), argExpressions);
        context.SymbolTable.AddSymbol(call, start, end);
        return true;
    }

    public static bool TryParseVariableDefinition(
        ParsingContext context, 
        [NotNullWhen(true)]out NodeBase? variableDefinition)
    {
        variableDefinition = null;
        if (!TryParseType(context, out var type)) return false;
        var start = context.Sequence.CurrentStart;
        if (context.Sequence.Current() is not Word variableName)
        {
            var record = PlampExceptionInfo.ExpectedVarName();
            context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentStart,
                context.Sequence.CurrentEnd, context.FileName));
            return false;
        }

        var end = context.Sequence.CurrentEnd;
        var name = new VariableNameNode(variableName.GetStringRepresentation());
        context.Sequence.MoveNextNonWhiteSpace();
        
        variableDefinition = new VariableDefinitionNode(type, name);
        context.SymbolTable.AddSymbol(variableDefinition, start, end);
        return true;
    }

    public static bool TryParseType(
        ParsingContext context, 
        [NotNullWhen(true)]out TypeNode? type)
    {
        type = null;
        if (context.Sequence.Current() is not Word typeName)
        {
            var record = PlampExceptionInfo.ExpectedTypeName();
            context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentStart, context.Sequence.CurrentEnd, context.FileName));
            return false;
        }
        
        context.Sequence.MoveNextNonWhiteSpace();
        var typeNameNode = new TypeNameNode(typeName.GetStringRepresentation());
        type = new TypeNode(typeNameNode, []);
        context.SymbolTable.AddSymbol(type, typeName.Start, typeName.End);
        context.SymbolTable.AddSymbol(typeNameNode, typeName.Start, typeName.End);
        return true;
    }

    public static bool TryParsePrecedence(
        ParsingContext context,
        [NotNullWhen(true)] out NodeBase? expression,
        int rbp = 0)
    {
        if (!TryParseNud(context, out expression)) return false;
        if (TryParsePostfix(context, expression, out var withPostfix))
        {
            expression = withPostfix;
        }
        
        while (TryParseLed(rbp, expression, out expression, context)) { }
        return true;
    }

    public static bool TryParseNud(
        ParsingContext context,
        [NotNullWhen(true)]out NodeBase? node)
    {
        var start = context.Sequence.CurrentStart;
        node = null;
        var parenFork = context.Fork();
        if (parenFork.Sequence.Current() is OpenParen
            && parenFork.Sequence.MoveNextNonWhiteSpace()
            && TryParsePrecedence(parenFork, out var inner))
        {
            if (parenFork.Sequence.Current() is not CloseParen)
            {
                var record = PlampExceptionInfo.ExpectedCloseParen();
                parenFork.Exceptions.Add(new PlampException(record, parenFork.Sequence.CurrentStart,
                    parenFork.Sequence.CurrentEnd, context.FileName));
            }
            else
            {
                parenFork.Sequence.MoveNextNonWhiteSpace();
            }
            
            context.Merge(parenFork);
            node = inner;
            return true;
        }

        var funcFork = context.Fork();
        if (TryParseFuncCall(funcFork, out var call))
        {
            context.Merge(funcFork);
            node = call;
            return true;
        }

        if (context.Sequence.Current() is KeywordToken keywordToken)
        {
            switch (keywordToken.Keyword)
            {
                case Keywords.Null:
                    node = new LiteralNode(null, typeof(object));
                    context.Sequence.MoveNextNonWhiteSpace();
                    return true;
                case Keywords.True:
                    node = new LiteralNode(true, typeof(bool));
                    context.Sequence.MoveNextNonWhiteSpace();
                    return true;
                case Keywords.False:
                    node = new LiteralNode(false, typeof(bool));
                    context.Sequence.MoveNextNonWhiteSpace();
                    return true;
            }
        }
        
        if (context.Sequence.Current() is Word member)
        {
            node = new MemberNode(member.GetStringRepresentation());
            context.Sequence.MoveNextNonWhiteSpace();
            context.SymbolTable.AddSymbol(node, start, context.Sequence.CurrentEnd);
            return true;
        }

        if (context.Sequence.Current() is Literal literal)
        {
            node = new LiteralNode(literal.ActualValue, literal.ActualType);
            context.Sequence.MoveNextNonWhiteSpace();
            context.SymbolTable.AddSymbol(node, start, context.Sequence.CurrentEnd);
            return true;
        }
        
        var prefixFork = context.Fork();
        if (prefixFork.Sequence.Current() is OperatorToken
            { Operator: OperatorEnum.Increment 
                or OperatorEnum.Decrement 
                or OperatorEnum.Not 
                or OperatorEnum.Sub
                or OperatorEnum.Add
            })
        {
            var op = (OperatorToken)prefixFork.Sequence.Current();
            prefixFork.Sequence.MoveNextNonWhiteSpace();
            if (!TryParseNud(prefixFork, out var innerNode)) return false;
            
            switch (op.Operator)
            {
                case OperatorEnum.Increment:
                    node = new PrefixIncrementNode(innerNode);
                    break;
                case OperatorEnum.Decrement:
                    node = new PrefixDecrementNode(innerNode);
                    break;
                case OperatorEnum.Not:
                    node = new NotNode(innerNode);
                    break;
                case OperatorEnum.Sub:
                    node = new UnaryMinusNode(innerNode);
                    break;
                //Unary minus does not exist.
                case OperatorEnum.Add:
                    node = innerNode;
                    context.Merge(prefixFork);
                    return true;
                default: throw new InvalidOperationException();
            }
            context.Merge(prefixFork);
            context.SymbolTable.AddSymbol(node, op.Start, op.End);
            return true;
        }

        return false;
    }

    private static bool TryParsePostfix(
        ParsingContext context,
        NodeBase inner,
        [NotNullWhen(true)]out NodeBase? output)
    {
        output = null;
        if (context.Sequence.Current() is not OperatorToken operatorToken) return false;
        
        switch (operatorToken.Operator)
        {
            case OperatorEnum.Increment:
                output = new PostfixIncrementNode(inner);
                context.Sequence.MoveNextNonWhiteSpace();
                return true;
            case OperatorEnum.Decrement:
                output = new PostfixDecrementNode(inner);
                context.Sequence.MoveNextNonWhiteSpace();
                return true;
        }

        return false;
    }

    private static bool TryParseLed(
        int rbp, 
        NodeBase left, 
        out NodeBase output, 
        ParsingContext context)
    {
        output = left;
        if (context.Sequence.Current() is not OperatorToken token) return false;
        var opFork = context.Fork();
        var precedence = token.GetPrecedence(false);
        if (precedence <= rbp) return false;
        opFork.Sequence.MoveNextNonWhiteSpace();

        var res = TryParsePrecedence(opFork, out var right, precedence);
        //Access is not binary operator yet
        if (!res || right == null || token.Operator == OperatorEnum.Access) return false;
        switch (token.Operator)
        {
            case OperatorEnum.Mul:
                output = new MulNode(left, right);
                break;
            case OperatorEnum.Div:
                output = new DivNode(left, right);
                break;
            case OperatorEnum.Add:
                output = new AddNode(left, right);
                break;
            case OperatorEnum.Sub:
                output = new SubNode(left, right);
                break;
            case OperatorEnum.Lesser:
                output = new LessNode(left, right);
                break;
            case OperatorEnum.Greater:
                output = new GreaterNode(left, right);
                break;
            case OperatorEnum.LesserOrEquals:
                output = new LessOrEqualNode(left, right);
                break;
            case OperatorEnum.GreaterOrEquals:
                output = new GreaterOrEqualNode(left, right);
                break;
            case OperatorEnum.Equals:
                output = new EqualNode(left, right);
                break;
            case OperatorEnum.NotEquals:
                output = new NotEqualNode(left, right);
                break;
            case OperatorEnum.And:
                output = new AndNode(left, right);
                break;
            case OperatorEnum.Or:
                output = new OrNode(left, right);
                break;
            case OperatorEnum.Modulo:
                output = new ModuloNode(left, right);
                break;
            case OperatorEnum.Assign:
                output = new AssignNode(left, right);
                break;
            default:
                throw new Exception();
        }
        context.Merge(opFork);
        context.SymbolTable.AddSymbol(output, token.Start, token.End);
        return true;
    }

    #endregion
}