using System;
using System.Collections.Generic;
using System.Linq;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Binary;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.Ast.Node.Unary;
using plamp.Alternative.AstExtensions;
using plamp.Alternative.Tokenization.Enums;
using plamp.Alternative.Tokenization.Token;
using RootNode = plamp.Alternative.AstExtensions.RootNode;

namespace plamp.Alternative.Parsing;

public static class Parser
{
    //TODO: убрать
    enum State
    {
        Imports,
        ModuleDef
    }
    
    public static RootNode ParseFile(ParsingContext context)
    {
        var topLevelList = new List<NodeBase>();
        while (context.Sequence.Current() is not EndOfFile)
        {
            if (TryParseTopLevel(context, out var topLevel) && topLevel != null) topLevelList.Add(topLevel);
        }

        var state = State.Imports;
        var imports = new List<ImportNode>();
        var module = default(ModuleDefinitionNode);
        var funcs = new List<DefNode>();
        foreach (var statement in topLevelList)
        {
            if (state == State.Imports && statement is ImportNode import)
            {
                imports.Add(import);
            }

            if (state == State.Imports && statement is ModuleDefinitionNode def)
            {
                module = def;
                state = State.ModuleDef;
            }

            if (state == State.ModuleDef && statement is DefNode defNode)
            {
                funcs.Add(defNode);
            }
        }

        return new RootNode(imports, module, funcs);
    }

    public static bool TryParseTopLevel(ParsingContext context, out NodeBase? topLevel)
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
                return false;
        }
    }

    #region Parsing use

    public static bool TryParseUse(ParsingContext context, out ImportNode? importNode)
    {
        importNode = null;
        var importKeyword = context.Sequence.Current();
        if (importKeyword is not KeywordToken { Keyword: Keywords.Use }) return false;
        context.Sequence.MoveNextNonWhiteSpace();
        
        var moduleName = GetModuleName(context);

        if (context.Sequence.Current() is EndOfStatement)
        {
            context.Sequence.MoveNextNonWhiteSpace();
            importNode = new ImportNode(moduleName, null);
            context.SymbolTable.AddSymbol(importNode, importKeyword.Start, importKeyword.End);
            return true;
        }
        if (context.Sequence.Current() is OpenCurlyBracket)
        {
            var importStart = context.Sequence.CurrentStart;
            if(!GetImportItems(context, out var list)) return false;
            var importEnd = context.Sequence.CurrentStart;
            importNode = new ImportNode(moduleName, list);
            context.SymbolTable.AddSymbol(importNode, importStart, importEnd);
            return true;
        }

        var record = PlampNativeExceptionInfo.ExpectedEndOfStatement();
        context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentStart, context.Sequence.CurrentEnd,
            context.FileName, null));
        return false;
    }

    private static string GetModuleName(ParsingContext context)
    {
        var name = string.Empty;
        if (context.Sequence.Current() is not Word modName) return name;
        name += modName.GetStringRepresentation();
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
                var record = PlampNativeExceptionInfo.ExpectedSubmoduleName();
                context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentStart,
                    context.Sequence.CurrentStart, context.FileName, null));
                break;
            }
        }

        return name;
    }

    private static bool TryParseImportItem(ParsingContext context, out ImportItemNode? importItem)
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
                var record = PlampNativeExceptionInfo.AliasExpected();
                context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentStart,
                    context.Sequence.CurrentEnd, context.FileName, null));
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
                if (TryParseImportItem(context, out var item) && item != null)
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

        var record = PlampNativeExceptionInfo.ExpectedClosingCurlyBracket();
        context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentStart, context.Sequence.CurrentEnd,
            context.FileName, null));

        return true;
    }

    #endregion

    #region Parsing module

    public static bool TryParseModuleDef(ParsingContext context, out ModuleDefinitionNode? module)
    {
        module = null;
        if(context.Sequence.Current() is not KeywordToken { Keyword: Keywords.Module }) return false;
        var defStart = context.Sequence.CurrentStart;
        context.Sequence.MoveNextNonWhiteSpace();
        var moduleName = GetModuleName(context);
        module = new ModuleDefinitionNode(moduleName);

        FilePosition defEnd;
        if (context.Sequence.Current() is not EndOfStatement)
        {
            var record = PlampNativeExceptionInfo.ExpectedEndOfStatement();
            context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentStart, context.Sequence.CurrentEnd, context.FileName, null));
            defEnd = context.Sequence.CurrentStart;
            context.SymbolTable.AddSymbol(module, defStart, defEnd);
            return true;
        }
        defEnd = context.Sequence.CurrentEnd;
        context.SymbolTable.AddSymbol(module, defStart, defEnd);
        context.Sequence.MoveNextNonWhiteSpace();
        return true;
    }

    #endregion

    #region Parsing func

    public static bool TryParseFunc(ParsingContext context, out DefNode? func)
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
            var record = PlampNativeExceptionInfo.ExpectedFuncName();
            context.Exceptions.Add(new PlampException(record, current.Start, current.End, context.FileName, null));
            return false;
        }

        if (!TryParseArgSequence(context, out var list) || list is null) return false;
        TypeNode? type = null;
        if (context.Sequence.Current() is Word) TryParseType(context, out type);

        if (!TryParseBody(context, out var body) || body == null) return false;
        func = new DefNode(type, new MemberNode(name), list, body);
        context.SymbolTable.AddSymbol(func, fnToken.Start, fnToken.End);
        return true;
    }

    public static bool TryParseArgSequence(ParsingContext context, out List<ParameterNode>? parameterList)
    {
        parameterList = null;
        if (context.Sequence.Current() is not OpenParen)
        {
            var current = context.Sequence.Current();
            var record = PlampNativeExceptionInfo.ExpectedOpenParen();
            context.Exceptions.Add(new PlampException(record, current.Start, current.End, context.FileName, null));
            return false;
        }
        context.Sequence.MoveNextNonWhiteSpace();

        if (context.Sequence.Current() is CloseParen)
        {
            parameterList = [];
            context.Sequence.MoveNextNonWhiteSpace();
            return true;
        }
        
        parameterList = new List<ParameterNode>();
        if (!TryParseArg(context, out var arg) || arg == null) return false;
        parameterList.Add(arg);
        
        while (context.Sequence.Current() is Comma)
        {
            context.Sequence.MoveNextNonWhiteSpace();
            var fork = context.Fork();
            if (TryParseArg(fork, out arg) && arg != null)
            {
                parameterList.Add(arg);
            }
            else
            {
                var record = PlampNativeExceptionInfo.ExpectedArgDefinition();
                context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentStart,
                    context.Sequence.CurrentEnd, context.FileName, null));
                return false;
            }
        }

        if (context.Sequence.Current() is not CloseParen)
        {
            var record = PlampNativeExceptionInfo.ExpectedCloseParen();
            context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentStart,
                context.Sequence.CurrentEnd, context.FileName, null));
        }
        else
        {
            context.Sequence.MoveNextNonWhiteSpace();
        }

        return true;
    }

    public static bool TryParseArg(ParsingContext context, out ParameterNode? arg)
    {
        arg = null;
        if(context.Sequence.Current() is not Word) return false;
        var start = context.Sequence.CurrentStart;
        if(!TryParseType(context, out var type) || type == null) return false;
        
        if (context.Sequence.Current() is not Word argName)
        {
            var record = PlampNativeExceptionInfo.ExpectedArgName();
            context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentStart,
                context.Sequence.CurrentEnd, context.FileName, null));
            arg = null;
            return false;
        }

        var end = context.Sequence.CurrentEnd;
        arg = new ParameterNode(type, new MemberNode(argName.GetStringRepresentation()));
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
            PlampNativeExceptionInfo.UnexpectedToken(token.GetStringRepresentation());
        context.Exceptions.Add(new PlampException(record, token.Start, token.End, context.FileName, null));
    }

    public static bool TryParseBody(ParsingContext context, out BodyNode? body)
    {
        body = null;
        var expressions = new List<NodeBase>();
        var start = context.Sequence.CurrentStart;
        FilePosition end;
        if (context.Sequence.Current() is not OpenCurlyBracket)
        {
            if (TryParseStatement(context, out var expression) && expression != null) expressions.Add(expression);
            end = context.Sequence.CurrentStart;
            body = new BodyNode(expressions);
            context.SymbolTable.AddSymbol(body, start, end);
            return true;
        }

        context.Sequence.MoveNextNonWhiteSpace();
        while (context.Sequence.Current() is not EndOfFile and not CloseCurlyBracket)
        {
            if (!TryParseStatement(context, out var expression) || expression == null) continue;
            expressions.Add(expression);
        }
        if (context.Sequence.Current() is EndOfFile)
        {
            var record = PlampNativeExceptionInfo.ExpectedClosingCurlyBracket();
            context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentStart,
                context.Sequence.CurrentEnd, context.FileName, null));
            body = new BodyNode(expressions);
            end = context.Sequence.CurrentStart;
            context.SymbolTable.AddSymbol(body, start, end);
            return true;
        }

        end = context.Sequence.CurrentEnd;
        context.Sequence.MoveNextNonWhiteSpace();
        body = new BodyNode(expressions);
        context.SymbolTable.AddSymbol(body, start, end);
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
                break;
            case KeywordToken {Keyword: Keywords.Continue}:
                expression = new ContinueNode();
                break;
            case KeywordToken {Keyword: Keywords.Return}:
                if(!TryParseReturn(context, out var node)) return false;
                expression = node;
                break;
            default:
                var precedenceFork = context.Fork();
                if (TryParsePrecedence(precedenceFork, out var precedence) && precedence != null)
                {
                    expression = precedence;
                    context.Merge(precedenceFork);
                    ConsumeEndOfStatement(context);
                    break;
                }
                AddUnexpectedTokenException(context);
                context.Sequence.MoveNextNonWhiteSpace();
                return false;
        }
        
        return true;
    }

    public static bool TryParseCondition(ParsingContext context, out ConditionNode? condition)
    {
        condition = null;
        if(context.Sequence.Current() is not KeywordToken{Keyword: Keywords.If}) return false;
        var conditionToken = context.Sequence.Current();
        context.Sequence.MoveNextNonWhiteSpace();

        if (!TryParseConditionPredicate(context, out var expression) || expression == null) return false;
        if (!TryParseBody(context, out var body) || body == null) return false;

        if (context.Sequence.Current() is not KeywordToken { Keyword: Keywords.Else })
        {
            condition = new ConditionNode(expression, body, null);
            return true;
        }

        context.Sequence.MoveNextNonWhiteSpace();
        if (!TryParseBody(context, out var elseBody) || elseBody == null) return false;
        condition = new ConditionNode(expression, body, elseBody);
        context.SymbolTable.AddSymbol(condition, conditionToken.Start, conditionToken.End);
        return true;
    }

    public static bool TryParseWhileLoop(ParsingContext context, out WhileNode? loop)
    {
        loop = null;
        if (context.Sequence.Current() is not KeywordToken{Keyword:Keywords.While}) return false;
        var loopToken = context.Sequence.Current();
        context.Sequence.MoveNextNonWhiteSpace();
        if (!TryParseConditionPredicate(context, out var predicate) || predicate == null) return false;
        if (!TryParseBody(context, out var body) || body == null) return false;
        loop = new WhileNode(predicate, body);
        context.SymbolTable.AddSymbol(loop, loopToken.Start, loopToken.End);
        return true;
    }

    public static bool TryParseConditionPredicate(ParsingContext context, out NodeBase? conditionPredicate)
    {
        conditionPredicate = null;
        if (context.Sequence.Current() is not OpenParen)
        {
            var record = PlampNativeExceptionInfo.ExpectedOpenParen();
            context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentStart, context.Sequence.CurrentEnd, context.FileName, null));
            return false;
        }

        context.Sequence.MoveNextNonWhiteSpace();

        var conditionParsed = TryParsePrecedence(context, out conditionPredicate);

        if (context.Sequence.Current() is not CloseParen)
        {
            var record = PlampNativeExceptionInfo.ExpectedCloseParen();
            context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentStart, context.Sequence.CurrentEnd, context.FileName, null));
        }
        else
        {
            context.Sequence.MoveNextNonWhiteSpace();
        }

        return conditionParsed;
    }

    public static bool TryParseReturn(ParsingContext context, out ReturnNode? node)
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

        if (!TryParseExpression(context, out var expr) || expr == null) return false;
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
        
        var precedenceContext = context.Fork();
        var unaryParsed = TryParsePrecedence(precedenceContext, out var precedence);
        if (unaryParsed)
        {
            context.Merge(precedenceContext);
            expression = precedence;
            return true;
        }
        
        var variableDefContext = context.Fork();
        var definitionParsed = TryParseVariableDefinition(variableDefContext, out var definition);
        if (definitionParsed)
        {
            context.Merge(variableDefContext);
            expression = definition;
            return true;
        }
        
        context.Sequence.MoveNextNonWhiteSpace();
        var record = PlampNativeExceptionInfo.ExpectedExpression();
        context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentStart, context.Sequence.CurrentEnd, context.FileName, null));
        return false;
    }

    public static bool TryParseAssignment(ParsingContext context, out NodeBase? assignment)
    {
        assignment = null;
        var definitionFork = context.Fork();
        if (TryParseVariableDefinition(definitionFork, out var definition))
        {
            context.Merge(definitionFork);
            if (context.Sequence.Current() is not OperatorToken { Operator: OperatorEnum.Assign })
            {
                var record = PlampNativeExceptionInfo.ExpectedAssignment();
                context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentStart,
                    context.Sequence.CurrentEnd, context.FileName, null));
                return false;
            }

            var assign = context.Sequence.Current();
            context.Sequence.MoveNextNonWhiteSpace();
            
            if (TryParsePrecedence(context, out var right))
            {
                assignment = new AssignNode(definition as VariableDefinitionNode, right);
                context.SymbolTable.AddSymbol(assignment, assign.Start, assign.End);
                return true;
            }

            var exceptionRecord = PlampNativeExceptionInfo.ExpectedAssignmentSource();
            context.Exceptions.Add(new PlampException(exceptionRecord, context.Sequence.CurrentStart,
                context.Sequence.CurrentEnd, context.FileName, null));
            return false;
        }

        var memberFork = context.Fork();
        if (memberFork.Sequence.Current() is Word member)
        {
            context.Merge(memberFork);
            context.Sequence.MoveNextNonWhiteSpace();
            if (context.Sequence.Current() is not OperatorToken { Operator: OperatorEnum.Assign })
            {
                var record = PlampNativeExceptionInfo.ExpectedAssignment();
                context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentStart,
                    context.Sequence.CurrentEnd, context.FileName, null));
                return false;
            }

            var assign = context.Sequence.Current();
            context.Sequence.MoveNextNonWhiteSpace();
            
            if (TryParsePrecedence(context, out var right))
            {
                definition = new MemberNode(member.GetStringRepresentation());
                context.SymbolTable.AddSymbol(definition, member.Start, member.End);
                assignment = new AssignNode(definition as VariableDefinitionNode, right);
                context.SymbolTable.AddSymbol(assignment, assign.Start, assign.End);
                return true;
            }

            var exceptionRecord = PlampNativeExceptionInfo.ExpectedAssignmentSource();
            context.Exceptions.Add(new PlampException(exceptionRecord, context.Sequence.CurrentStart,
                context.Sequence.CurrentEnd, context.FileName, null));
        }

        return false;
    }

    public static bool TryParseFuncCall(ParsingContext context, out NodeBase? call)
    {
        call = null;
        if (context.Sequence.Current() is not Word funcName) return false;
        var start = context.Sequence.CurrentStart;
        context.Sequence.MoveNextNonWhiteSpace();
        
        if (context.Sequence.Current() is not OpenParen)
        {
            var record = PlampNativeExceptionInfo.ExpectedOpenParen();
            context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentStart,
                context.Sequence.CurrentEnd, context.FileName, null));
            return false;
        }

        context.Sequence.MoveNextNonWhiteSpace();

        var argExpressions = new List<NodeBase>();
        FilePosition end;
        if (context.Sequence.Current() is CloseParen)
        {
            end = context.Sequence.CurrentEnd;
            call = new CallNode(new ThisNode(), new MemberNode(funcName.GetStringRepresentation()), argExpressions);
            context.Sequence.MoveNextNonWhiteSpace();
            context.SymbolTable.AddSymbol(call, start, end);
            return true;
        }

        if (!TryParsePrecedence(context, out var arg) || arg == null) return false;
        argExpressions.Add(arg);
        
        while (context.Sequence.Current() is Comma)
        {
            context.Sequence.MoveNextNonWhiteSpace();
            if (TryParsePrecedence(context, out arg) && arg != null)
            {
                argExpressions.Add(arg);
            }
            else
            {
                var record = PlampNativeExceptionInfo.ExpectedExpression();
                context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentStart,
                    context.Sequence.CurrentEnd, context.FileName, null));
                return false;
            }
        }
        
        if (context.Sequence.Current() is not CloseParen)
        {
            var record = PlampNativeExceptionInfo.ExpectedCloseParen();
            context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentStart,
                context.Sequence.CurrentEnd, context.FileName, null));
            end = context.Sequence.CurrentStart;
        }
        else
        {
            end = context.Sequence.CurrentEnd;
            context.Sequence.MoveNextNonWhiteSpace();
        }

        call = new CallNode(new ThisNode(), new MemberNode(funcName.GetStringRepresentation()), argExpressions);
        context.SymbolTable.AddSymbol(call, start, end);
        return true;
    }

    public static bool TryParseVariableDefinition(ParsingContext context, out NodeBase? variableDefinition)
    {
        variableDefinition = null;
        if (!TryParseType(context, out var type)) return false;
        var start = context.Sequence.CurrentStart;
        context.Sequence.MoveNextNonWhiteSpace();
        if (context.Sequence.Current() is not Word variableName)
        {
            var record = PlampNativeExceptionInfo.ExpectedVarName();
            context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentStart,
                context.Sequence.CurrentEnd, context.FileName, null));
            return false;
        }

        var end = context.Sequence.CurrentEnd;
        var name = new MemberNode(variableName.GetStringRepresentation());
        context.Sequence.MoveNextNonWhiteSpace();
        
        variableDefinition = new VariableDefinitionNode(type, name);
        context.SymbolTable.AddSymbol(variableDefinition, start, end);
        return true;
    }

    public static bool TryParseType(ParsingContext context, out TypeNode? type)
    {
        type = null;
        if (context.Sequence.Current() is not Word typeName)
        {
            var record = PlampNativeExceptionInfo.ExpectedTypeName();
            context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentStart, context.Sequence.CurrentEnd, context.FileName, null));
            return false;
        }
        
        context.Sequence.MoveNextNonWhiteSpace();
        type = new TypeNode(new MemberNode(typeName.GetStringRepresentation()), []);
        context.SymbolTable.AddSymbol(type, typeName.Start, typeName.End);
        return true;
    }

    public static bool TryParsePrecedence(ParsingContext context, out NodeBase? expression, int rbp = 0)
    {
        if (!TryParseNud(context, out expression) || expression == null) return false;
        while (TryParseLed(rbp, expression, out expression, context)) { }
        return true;
    }

    public static bool TryParseNud(
        ParsingContext context,
        out NodeBase? node)
    {
        var start = context.Sequence.CurrentStart;
        node = null;
        var parenFork = context.Fork();
        if (parenFork.Sequence.Current() is OpenParen
            && parenFork.Sequence.MoveNextNonWhiteSpace()
            && TryParsePrecedence(parenFork, out var inner)
            && inner != null)
        {
            if (parenFork.Sequence.Current() is not CloseParen)
            {
                var record = PlampNativeExceptionInfo.ExpectedCloseParen();
                parenFork.Exceptions.Add(new PlampException(record, parenFork.Sequence.CurrentStart,
                    context.Sequence.CurrentEnd, context.FileName, null));
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
        if (TryParseFuncCall(funcFork, out var call)
            && call != null)
        {
            context.Merge(funcFork);
            node = call;
            return true;
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
            { Operator: OperatorEnum.Increment or OperatorEnum.Decrement or OperatorEnum.Not or OperatorEnum.Sub })
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
            }
            context.Merge(prefixFork);
            context.SymbolTable.AddSymbol(node!, op.Start, op.End);
            return true;
        }

        return false;
    }

    public static bool TryParseLed(
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
        if (!res) return false;
        switch (token.Operator)
        {
            case OperatorEnum.Mul:
                output = new MultiplyNode(left, right);
                break;
            case OperatorEnum.Div:
                output = new DivideNode(left, right);
                break;
            case OperatorEnum.Add:
                output = new PlusNode(left, right);
                break;
            case OperatorEnum.Sub:
                output = new MinusNode(left, right);
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
                output = new AssignNode(left as VariableDefinitionNode, right);
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