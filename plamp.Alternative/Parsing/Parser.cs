using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Binary;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ComplexTypes;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;
using plamp.Abstractions.Ast.Node.Definitions.Variable;
using plamp.Abstractions.Ast.Node.Unary;
using plamp.Alternative.Tokenization.Enums;
using plamp.Alternative.Tokenization.Token;
using plamp.Intrinsics;

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
        var types = new List<TypedefNode>();
        foreach (var statement in topLevelList)
        {
            if (statement is ImportNode import) imports.Add(import);
            if (statement is ModuleDefinitionNode def) modules.Add(def);
            if (statement is FuncNode defNode) functions.Add(defNode);
            if (statement is TypedefNode typedef) types.Add(typedef);
        }

        if (modules.Count > 1)
        {
            var record = PlampExceptionInfo.DuplicateModuleDefinition();
            foreach (var module in modules)
            {
                context.Exceptions.Add(context.TranslationTable.SetExceptionToNode(module, record));
            }

            modules = null;
        }

        var moduleDef = modules?.FirstOrDefault();
        var node = new RootNode(imports, moduleDef, functions, types);
        context.TranslationTable.AddSymbol(node, default);
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
                if (!TryParseUse(context, out var import)) return false;
                topLevel = import;
                return true;
            case KeywordToken { Keyword: Keywords.Module }:
                if (!TryParseModuleDef(context, out var module)) return false;
                topLevel = module;
                return true;
            case KeywordToken { Keyword: Keywords.Fn }:
                if (!TryParseFunc(context, out var fn)) return false;
                topLevel = fn;
                return true;
            case KeywordToken { Keyword: Keywords.Type }:
                if (!TryParseTypedef(context, out var typ)) return false;
                topLevel = typ;
                return true;
            default:
                AddUnexpectedTokenException(context);
                context.Sequence.MoveNextNonWhiteSpace();
                return false;
        }
    }

    #region Typedef

    public static bool TryParseTypedef(ParsingContext context, [NotNullWhen(true)] out TypedefNode? typedef)
    {
        typedef = null;
        if (context.Sequence.Current() is not KeywordToken { Keyword: Keywords.Type } typeKeyword) return false;
        context.Sequence.MoveNextNonWhiteSpace();

        if (context.Sequence.Current() is not Word typeName)
        {
            var record = PlampExceptionInfo.ExpectedTypeName();
            context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentPosition));
            return false;
        }

        context.Sequence.MoveNextNonWhiteSpace();

        if (context.Sequence.Current() is EndOfStatement)
        {
            context.Sequence.MoveNextNonWhiteSpace();
            var defName = new TypedefNameNode(typeName.GetStringRepresentation());
            context.TranslationTable.AddSymbol(defName, typeName.Position);
            typedef = new TypedefNode(defName, []);
            context.TranslationTable.AddSymbol(typedef, typeKeyword.Position);
            return true;
        }
        
        if (context.Sequence.Current() is not OpenCurlyBracket)
        {
            var record = PlampExceptionInfo.ExpectedBodyInCurlyBrackets();
            context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentPosition));
            return false;
        }

        context.Sequence.MoveNextNonWhiteSpace();
        var fields = new List<FieldDefNode>();

        var first = true;
        
        do
        {
            if (!first) context.Sequence.MoveNextNonWhiteSpace();
            if (TryParseField(context, out var fieldNode)) fields.AddRange(fieldNode);
            else break;
            first = false;
        } while (context.Sequence.Current() is EndOfStatement);

        if (context.Sequence.Current() is not CloseCurlyBracket)
        {
            var record = PlampExceptionInfo.ExpectedClosingCurlyBracket();
            context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentPosition));
            return false;
        }

        context.Sequence.MoveNextNonWhiteSpace();
        var name = new TypedefNameNode(typeName.GetStringRepresentation());
        context.TranslationTable.AddSymbol(name, typeName.Position);
        typedef = new TypedefNode(name, fields);
        context.TranslationTable.AddSymbol(typedef, typeKeyword.Position);
        return true;
    }

    public static bool TryParseField(ParsingContext context, out List<FieldDefNode> fieldNodes)
    {
        fieldNodes = [];
        if (context.Sequence.Current() is not Word) return false;

        var fieldNames = new List<FieldNameNode>();
        var first = true;
        do
        {
            var iterFork = context.Fork();
            if (!first) iterFork.Sequence.MoveNextNonWhiteSpace();
            if (iterFork.Sequence.Current() is not Word name) break;
            context.Merge(iterFork);

            var fieldName = new FieldNameNode(name.GetStringRepresentation());
            context.TranslationTable.AddSymbol(fieldName, context.Sequence.CurrentPosition);
            fieldNames.Add(fieldName);
            context.Sequence.MoveNextNonWhiteSpace();
            first = false;
        } while (context.Sequence.Current() is Comma);

        if (context.Sequence.Current() is not Colon)
        {
            var record = PlampExceptionInfo.ExpectedTypeName();
            context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentPosition));
            return false;
        }

        context.Sequence.MoveNextNonWhiteSpace();
        var typFork = context.Fork();
        if (!TryParseType(typFork, out var type))
        {
            var record = PlampExceptionInfo.ExpectedTypeName();
            context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentPosition));
            return false;
        }

        context.Merge(typFork);
        foreach (var name in fieldNames)
        {
            var fieldNode = new FieldDefNode(type, name);
            context.TranslationTable.TryGetSymbol(fieldNode, out var position);
            context.TranslationTable.AddSymbol(fieldNode, position);
            fieldNodes.Add(fieldNode);
        }

        return true;
    }

    #endregion

    #region Use

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
            if (!GetImportItems(context, out var list)) return false;
            importNode = new ImportNode(moduleName, list);
            context.TranslationTable.AddSymbol(importNode, context.Sequence.MakeRangeFromPrevNonWhitespace(importKeyword));
            return true;
        }

        importNode = new ImportNode(moduleName, null);
        context.TranslationTable.AddSymbol(importNode, context.Sequence.MakeRangeFromPrevNonWhitespace(importKeyword));
        ConsumeEndOfStatement(context);
        return true;
    }

    private static string? GetModuleNameOrDefault(ParsingContext context)
    {
        if (context.Sequence.Current() is not Word modName)
        {
            var record = PlampExceptionInfo.ExpectedModuleName();
            var current = context.Sequence.Current();
            context.Exceptions.Add(new PlampException(record, current.Position));
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
                context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentPosition));
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
        var itemName = word.GetStringRepresentation();
        context.Sequence.MoveNextNonWhiteSpace();
        if (context.Sequence.Current() is KeywordToken { Keyword: Keywords.As })
        {
            context.Sequence.MoveNextNonWhiteSpace();
            if (context.Sequence.Current() is not Word alias)
            {
                var record = PlampExceptionInfo.AliasExpected();
                context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentPosition));
                return false;
            }

            importItem = new ImportItemNode(itemName, alias.GetStringRepresentation());
            context.Sequence.MoveNextNonWhiteSpace();
            context.TranslationTable.AddSymbol(importItem, context.Sequence.MakeRangeFromPrevNonWhitespace(word));
        }
        else
        {
            importItem = new ImportItemNode(itemName, itemName);
            context.TranslationTable.AddSymbol(importItem, word.Position);
        }

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

            if (context.Sequence.Current() is EndOfFile) break;

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
        context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentPosition));

        return true;
    }

    #endregion

    #region Module definition

    public static bool TryParseModuleDef(
        ParsingContext context,
        [NotNullWhen(true)] out ModuleDefinitionNode? module)
    {
        module = null;
        if (context.Sequence.Current() is not KeywordToken { Keyword: Keywords.Module }) return false;
        var defStart = context.Sequence.Current();
        context.Sequence.MoveNextNonWhiteSpace();
        var moduleName = GetModuleNameOrDefault(context);
        if (moduleName == null) return false;
        module = new ModuleDefinitionNode(moduleName);

        context.TranslationTable.AddSymbol(module, context.Sequence.MakeRangeFromPrevNonWhitespace(defStart));
        ConsumeEndOfStatement(context);
        return true;
    }

    #endregion

    #region Func definition

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
            context.Exceptions.Add(new PlampException(record, current.Position));
            return false;
        }

        if (!TryParseArgSequence(context, out var list)) return false;
        TypeNode? type = null;
        var typeFork = context.Fork();
        if (context.Sequence.Current() is Word && TryParseType(typeFork, out type)) context.Merge(typeFork);

        if (!TryParseMultilineBody(context, out var body))
        {
            var current = context.Sequence.Current();
            var record = PlampExceptionInfo.ExpectedBodyInCurlyBrackets();
            context.Exceptions.Add(new PlampException(record, current.Position));
            return false;
        }

        var funcNameNode = new FuncNameNode(name);
        context.TranslationTable.AddSymbol(funcNameNode, funcName.Position);

        if (type == null)
        {
            var voidName = new TypeNameNode(RuntimeSymbols.SymbolTable.Void.TypeName);
            context.TranslationTable.AddSymbol(voidName, funcName.Position);
            type = new TypeNode(voidName);
            context.TranslationTable.AddSymbol(type, funcName.Position);
        }
        
        func = new FuncNode(type, funcNameNode, list, body);
        context.TranslationTable.AddSymbol(func, fnToken.Position);
        return true;
    }

    public static bool TryParseArgSequence(
        ParsingContext context,
        [NotNullWhen(true)] out List<ParameterNode>? parameterList)
    {
        parameterList = null;
        if (context.Sequence.Current() is not OpenParen)
        {
            var current = context.Sequence.Current();
            var record = PlampExceptionInfo.ExpectedOpenParen();
            context.Exceptions.Add(new PlampException(record, current.Position));
            return false;
        }

        context.Sequence.MoveNextNonWhiteSpace();

        if (context.Sequence.Current() is CloseParen)
        {
            parameterList = [];
            context.Sequence.MoveNextNonWhiteSpace();
            return true;
        }


        if (!TryParseArg(context, out var args))
        {
            var record = PlampExceptionInfo.ExpectedArgDefinition();
            context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentPosition));
            return false;
        }

        parameterList = args;

        while (context.Sequence.Current() is Comma)
        {
            context.Sequence.MoveNextNonWhiteSpace();
            var fork = context.Fork();
            if (TryParseArg(fork, out args))
            {
                parameterList.AddRange(args);
                context.Merge(fork);
            }
            else
            {
                var record = PlampExceptionInfo.ExpectedArgDefinition();
                context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentPosition));
                return false;
            }
        }

        if (context.Sequence.Current() is not CloseParen)
        {
            var record = PlampExceptionInfo.ExpectedCloseParen();
            context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentPosition));
        }
        else
        {
            context.Sequence.MoveNextNonWhiteSpace();
        }

        return true;
    }

    public static bool TryParseArg(
        ParsingContext context,
        [NotNullWhen(true)] out List<ParameterNode>? args)
    {
        args = null;
        if (context.Sequence.Current() is not Word start) return false;

        var argNames = new List<ParameterNameNode>();
        var first = true;
        do
        {
            var iterFork = context.Fork();
            if (!first) iterFork.Sequence.MoveNextNonWhiteSpace();
            if (iterFork.Sequence.Current() is not Word name) break;
            context.Merge(iterFork);

            var argNameNode = new ParameterNameNode(name.GetStringRepresentation());
            context.TranslationTable.AddSymbol(argNameNode, context.Sequence.CurrentPosition);
            argNames.Add(argNameNode);
            context.Sequence.MoveNextNonWhiteSpace();
            first = false;
        } while (context.Sequence.Current() is Comma);

        if (context.Sequence.Current() is not Colon)
        {
            var record = PlampExceptionInfo.ExpectedTypeName();
            context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentPosition));
            return false;
        }

        context.Sequence.MoveNextNonWhiteSpace();
        var typFork = context.Fork();
        if (!TryParseType(typFork, out var type))
        {
            var record = PlampExceptionInfo.ExpectedTypeName();
            context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentPosition));
            return false;
        }

        context.Merge(typFork);

        args = [];
        foreach (var name in argNames)
        {
            var param = new ParameterNode(type, name);
            context.TranslationTable.AddSymbol(param, context.Sequence.MakeRangeFromPrevNonWhitespace(start));
            args.Add(param);
        }

        return true;
    }

    #endregion

    #region Looping

    public static bool TryParseWhileLoop(
        ParsingContext context,
        [NotNullWhen(true)] out WhileNode? loop)
    {
        loop = null;
        if (context.Sequence.Current() is not KeywordToken { Keyword: Keywords.While }) return false;
        var loopToken = context.Sequence.Current();
        context.Sequence.MoveNextNonWhiteSpace();
        if (!TryParseConditionPredicate(context, out var predicate)) return false;
        if (!TryParseBody(context, out var body)) return false;
        loop = new WhileNode(predicate, body);
        context.TranslationTable.AddSymbol(loop, loopToken.Position);
        return true;
    }

    #endregion

    #region Conditional

    public static bool TryParseCondition(
        ParsingContext context,
        [NotNullWhen(true)] out ConditionNode? condition)
    {
        condition = null;
        if (context.Sequence.Current() is not KeywordToken { Keyword: Keywords.If }) return false;
        var conditionToken = context.Sequence.Current();
        context.Sequence.MoveNextNonWhiteSpace();

        if (!TryParseConditionPredicate(context, out var expression)) return false;
        if (!TryParseBody(context, out var body)) return false;

        if (context.Sequence.Current() is not KeywordToken { Keyword: Keywords.Else })
        {
            condition = new ConditionNode(expression, body, null);
            context.TranslationTable.AddSymbol(condition, conditionToken.Position);
            return true;
        }

        context.Sequence.MoveNextNonWhiteSpace();
        if (!TryParseBody(context, out var elseBody)) return false;
        condition = new ConditionNode(expression, body, elseBody);
        context.TranslationTable.AddSymbol(condition, conditionToken.Position);
        return true;
    }

    public static bool TryParseConditionPredicate(
        ParsingContext context,
        [NotNullWhen(true)] out NodeBase? conditionPredicate)
    {
        conditionPredicate = null;
        if (context.Sequence.Current() is not OpenParen)
        {
            var record = PlampExceptionInfo.ExpectedOpenParen();
            context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentPosition));
            return false;
        }

        context.Sequence.MoveNextNonWhiteSpace();
        if (!TryParsePrecedence(context, out conditionPredicate))
        {
            var record = PlampExceptionInfo.ExpectedExpression();
            var current = context.Sequence.Current();
            context.Exceptions.Add(new PlampException(record, current.Position));
            return false;
        }

        if (context.Sequence.Current() is not CloseParen)
        {
            var record = PlampExceptionInfo.ExpectedCloseParen();
            context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentPosition));
        }
        else
        {
            context.Sequence.MoveNextNonWhiteSpace();
        }

        return true;
    }

    #endregion

    #region Body and statements

    public static bool TryParseBody(
        ParsingContext context,
        [NotNullWhen(true)] out BodyNode? body)
    {
        body = null;

        var start = context.Sequence.Current();

        if (context.Sequence.Current() is not OpenCurlyBracket)
        {
            var expressions = new List<NodeBase>();
            if (TryParseStatement(context, out var expression)) expressions.Add(expression);
            body = new BodyNode(expressions);
            context.TranslationTable.AddSymbol(body, context.Sequence.MakeRangeFromPrevNonWhitespace(start));
            return true;
        }

        return TryParseMultilineBody(context, out body);
    }

    public static bool TryParseMultilineBody(
        ParsingContext context,
        [NotNullWhen(true)] out BodyNode? body)
    {
        body = null;
        if (context.Sequence.Current() is not OpenCurlyBracket open) return false;

        context.Sequence.MoveNextNonWhiteSpace();
        var expressions = new List<NodeBase>();
        while (context.Sequence.Current() is not EndOfFile and not CloseCurlyBracket)
        {
            if (!TryParseStatement(context, out var expression)) continue;
            expressions.Add(expression);
        }

        if (context.Sequence.Current() is EndOfFile)
        {
            var record = PlampExceptionInfo.ExpectedClosingCurlyBracket();
            context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentPosition));
            body = new BodyNode(expressions);
            context.TranslationTable.AddSymbol(body, context.Sequence.MakeRangeFromPrevNonWhitespace(open));
            return true;
        }

        body = new BodyNode(expressions);
        context.Sequence.MoveNextNonWhiteSpace();
        context.TranslationTable.AddSymbol(body, context.Sequence.MakeRangeFromPrevNonWhitespace(open));
        return true;
    }

    public static bool TryParseStatement(
        ParsingContext context,
        [NotNullWhen(true)] out NodeBase? expression)
    {
        expression = null;
        switch (context.Sequence.Current())
        {
            case KeywordToken { Keyword: Keywords.If }:
                if (!TryParseCondition(context, out var condition)) return false;
                expression = condition;
                return true;
            case KeywordToken { Keyword: Keywords.While }:
                if (!TryParseWhileLoop(context, out var loop)) return false;
                expression = loop;
                return true;
            //TODO: To separate method.
            case KeywordToken { Keyword: Keywords.Break }:
                var breakExpression = new BreakNode();
                var current = context.Sequence.Current();
                context.TranslationTable.AddSymbol(breakExpression, current.Position);
                context.Sequence.MoveNextNonWhiteSpace();
                ConsumeEndOfStatement(context);
                expression = breakExpression;
                return true;
            case KeywordToken { Keyword: Keywords.Continue }:
                var continueExpression = new ContinueNode();
                current = context.Sequence.Current();
                context.TranslationTable.AddSymbol(continueExpression, current.Position);
                context.Sequence.MoveNextNonWhiteSpace();
                ConsumeEndOfStatement(context);
                expression = continueExpression;
                return true;
            case KeywordToken { Keyword: Keywords.Return }:
                if (!TryParseReturn(context, out var node)) return false;
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

    #endregion

    #region Assignment

    public static bool TryParseAssignment(
        ParsingContext context,
        [NotNullWhen(true)] out AssignNode? assignment)
    {
        assignment = null;
        var memberFork = context.Fork();
        if (TryParseAssignmentTargetSequence(memberFork, out var targets))
        {
            context.Merge(memberFork);
        }
        else
        {
            context.Merge(memberFork);
            return false;
        }

        if (context.Sequence.Current() is not OperatorToken { Operator: OperatorEnum.Assign } assign)
        {
            var record = PlampExceptionInfo.ExpectedAssignment();
            context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentPosition));
            return false;
        }

        context.Sequence.MoveNextNonWhiteSpace();
        if (!TryParseAssignmentSourceSequence(context, out var sources))
        {
            return false;
        }

        assignment = new AssignNode(targets, sources);
        context.TranslationTable.AddSymbol(assignment, assign.Position);
        return true;
    }

    private static bool TryParseAssignmentTargetSequence(
        ParsingContext context,
        [NotNullWhen(true)] out List<NodeBase>? members)
    {
        var moveNext = false;
        members = null;
        if (context.Sequence.Current() is not Word) return false;
        members = [];
        do
        {
            if (moveNext) context.Sequence.MoveNextNonWhiteSpace();
            var memberToken = context.Sequence.Current();
            if (memberToken is not Word member)
            {
                var record = PlampExceptionInfo.ExpectedAssignmentTarget();
                context.Exceptions.Add(new PlampException(record, memberToken.Position));
                return false;
            }

            var memberNode = new MemberNode(member.GetStringRepresentation());
            context.TranslationTable.AddSymbol(memberNode, member.Position);
            context.Sequence.MoveNextNonWhiteSpace();


            bool postfixParsed;
            NodeBase baseMemberNode = memberNode;
            do
            {
                postfixParsed = false;
                if (context.Sequence.Current() is OpenSquareBracket)
                {
                    var indexerFork = context.Fork();
                    if (!TryParseArrayIndexerSequence(indexerFork, baseMemberNode, out var indexerSequence)) continue;
                    context.Merge(indexerFork);
                    baseMemberNode = indexerSequence;
                    postfixParsed = true;
                }
                else if(context.Sequence.Current() is OperatorToken { Operator: OperatorEnum.Access })
                {
                    var accessFork = context.Fork();
                    if (!TryParseFieldAccessSequence(accessFork, baseMemberNode, out var accessSequence)) continue;
                    context.Merge(accessFork);
                    baseMemberNode = accessSequence;
                    postfixParsed = true;
                }
            } while (postfixParsed);
            
            members.Add(baseMemberNode);
            moveNext = true;
        } while (context.Sequence.Current() is Comma);

        return true;
    }

    private static bool TryParseAssignmentSourceSequence(
        ParsingContext context,
        [NotNullWhen(true)] out List<NodeBase>? assignmentSources)
    {
        assignmentSources = null;
        var moveNext = false;
        do
        {
            if (moveNext) context.Sequence.MoveNextNonWhiteSpace();
            var nudFork = context.Fork();
            if (!TryParsePrecedence(nudFork, out var source))
            {
                var record = PlampExceptionInfo.ExpectedAssignmentSource();
                context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentPosition));
                return false;
            }

            assignmentSources ??= [];
            assignmentSources.Add(source);
            context.Merge(nudFork);
            moveNext = true;
        } while (context.Sequence.Current() is Comma);

        return true;
    }

    public static bool TryParseFieldAccessSequence(
        ParsingContext context,
        NodeBase from,
        [NotNullWhen(true)] out FieldAccessNode? accessNode)
    {
        accessNode = null;
        if (context.Sequence.Current() is not OperatorToken { Operator: OperatorEnum.Access }) return false;
        
        while (context.Sequence.Current() is OperatorToken { Operator: OperatorEnum.Access } access)
        {
            context.Sequence.MoveNextNonWhiteSpace();
            var current = context.Sequence.Current(); 
            if (current is not Word fieldName)
            {
                var record = PlampExceptionInfo.ExpectedFieldName();
                context.Exceptions.Add(new PlampException(record, current.Position));
                break;
            }
            
            var field = new FieldNode(fieldName.GetStringRepresentation());
            context.TranslationTable.AddSymbol(field, fieldName.Position);
            from = accessNode = new FieldAccessNode(from, field);
            context.TranslationTable.AddSymbol(from, access.Position);
            context.Sequence.MoveNextNonWhiteSpace();
        }

        if (accessNode == null) return false;
        return true;
    }

    #endregion

    #region Arrays

    private static bool TryParseArrayIndexerSequence(
        ParsingContext context,
        NodeBase from,
        [NotNullWhen(true)] out IndexerNode? indexerSequence)
    {
        indexerSequence = null;
        if (context.Sequence.Current() is not OpenSquareBracket) return false;

        while (context.Sequence.Current() is OpenSquareBracket indexerStart)
        {
            context.Sequence.MoveNextNonWhiteSpace();
            if (!TryParsePrecedence(context, out var member)) return false;
            if (context.Sequence.Current() is not CloseSquareBracket)
            {
                var record = PlampExceptionInfo.IndexerIsNotClosed();
                context.Exceptions.Add(new PlampException(record,
                    context.Sequence.MakeRangeFromPrevNonWhitespace(indexerStart)));
            }

            from = indexerSequence = new IndexerNode(from, member);
            context.Sequence.MoveNextNonWhiteSpace();
            context.TranslationTable.AddSymbol(from, context.Sequence.MakeRangeFromPrevNonWhitespace(indexerStart));
        }

        if (indexerSequence == null) return false;
        return true;
    }

    public static bool TryParseArrayInitialization(
        ParsingContext context,
        [NotNullWhen(true)] out InitArrayNode? arrayDefinition)
    {
        arrayDefinition = null;
        if (context.Sequence.Current() is not OpenSquareBracket start) return false;
        context.Sequence.MoveNextNonWhiteSpace();

        var lengthFork = context.Fork();
        if (!TryParsePrecedence(lengthFork, out var dimension))
        {
            var record = PlampExceptionInfo.ArrayInitializationMustHasLength();
            context.Exceptions.Add(
                new PlampException(record, lengthFork.Sequence.MakeRangeFromPrevNonWhitespace(start)));
            return false;
        }

        context.Merge(lengthFork);

        if (context.Sequence.Current() is not CloseSquareBracket)
        {
            var record = PlampExceptionInfo.ArrayDefinitionIsNotClosed();
            context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentPosition));
            return false;
        }

        context.Sequence.MoveNextNonWhiteSpace();
        if (!TryParseType(context, out var type)) return false;
        arrayDefinition = new InitArrayNode(type, dimension);
        if (!context.TranslationTable.TryGetSymbol(type, out _))
            throw new InvalidOperationException("Parser code is incorrect");
        context.TranslationTable.AddSymbol(arrayDefinition, context.Sequence.MakeRangeFromPrevNonWhitespace(start));
        return true;
    }

    public static bool TryParseArrayDefinitionSequence(
        ParsingContext context,
        [NotNullWhen(true)] out List<ArrayTypeSpecificationNode>? definitions)
    {
        definitions = [];
        if (context.Sequence.Current() is not OpenSquareBracket) return true;
        while (true)
        {
            if (context.Sequence.Current() is not OpenSquareBracket open) return true;
            context.Sequence.MoveNextNonWhiteSpace();
            if (context.Sequence.Current() is not CloseSquareBracket)
            {
                var currentToken = context.Sequence.Current();
                var record = PlampExceptionInfo.ArrayDefinitionIsNotClosed();
                context.Exceptions.Add(new PlampException(record, currentToken.Position));
                return false;
            }

            var definition = new ArrayTypeSpecificationNode();
            definitions.Add(definition);
            context.Sequence.MoveNextNonWhiteSpace();
            context.TranslationTable.AddSymbol(definition, context.Sequence.MakeRangeFromPrevNonWhitespace(open));
        }
    }

    #endregion

    #region Recursive descent precedence parsing

    public static bool TryParsePrecedence(
        ParsingContext context,
        [NotNullWhen(true)] out NodeBase? expression,
        int rbp = 0)
    {
        if (!TryParseNud(context, out expression)) return false;
        while (TryParsePostfix(context, expression, out var withPostfix))
        {
            expression = withPostfix;
        }

        while (TryParseLed(rbp, expression, out expression, context))
        {
        }

        return true;
    }

    public static bool TryParseNud(
        ParsingContext context,
        [NotNullWhen(true)] out NodeBase? node)
    {
        var start = context.Sequence.Current();
        node = null;
        var parenFork = context.Fork();
        if (parenFork.Sequence.Current() is OpenParen
            && parenFork.Sequence.MoveNextNonWhiteSpace()
            && TryParsePrecedence(parenFork, out var inner))
        {
            if (parenFork.Sequence.Current() is not CloseParen)
            {
                var record = PlampExceptionInfo.ExpectedCloseParen();
                parenFork.Exceptions.Add(new PlampException(record,
                    parenFork.Sequence.MakeRangeFromPrevNonWhitespace(start)));
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

        var initTypeFork = context.Fork();
        if (TryParseTypeInit(initTypeFork, out var typeInitNode))
        {
            context.Merge(initTypeFork);
            node = typeInitNode;
            return true;
        }

        var initArrayFork = context.Fork();
        if (TryParseArrayInitialization(initArrayFork, out var arrayDefinition))
        {
            context.Merge(initArrayFork);
            node = arrayDefinition;
            return true;
        }

        if (context.Sequence.Current() is KeywordToken keywordToken)
        {
            switch (keywordToken.Keyword)
            {
                case Keywords.Null:
                    node = new LiteralNode(null, RuntimeSymbols.SymbolTable.Any);
                    context.Sequence.MoveNextNonWhiteSpace();
                    return true;
                case Keywords.True:
                    node = new LiteralNode(true, RuntimeSymbols.SymbolTable.Bool);
                    context.Sequence.MoveNextNonWhiteSpace();
                    return true;
                case Keywords.False:
                    node = new LiteralNode(false, RuntimeSymbols.SymbolTable.Bool);
                    context.Sequence.MoveNextNonWhiteSpace();
                    return true;
            }
        }

        if (context.Sequence.Current() is Word member)
        {
            node = new MemberNode(member.GetStringRepresentation());
            context.Sequence.MoveNextNonWhiteSpace();
            context.TranslationTable.AddSymbol(node, context.Sequence.MakeRangeFromPrevNonWhitespace(start));
            return true;
        }

        if (context.Sequence.Current() is Literal literal)
        {
            if (!RuntimeSymbols.SymbolTable.TryGetFromClrType(literal.ActualType, out var typeRef)) return false;
            node = new LiteralNode(literal.ActualValue, typeRef);
            context.Sequence.MoveNextNonWhiteSpace();
            context.TranslationTable.AddSymbol(node, context.Sequence.MakeRangeFromPrevNonWhitespace(start));
            return true;
        }

        var prefixFork = context.Fork();
        if (prefixFork.Sequence.Current() is OperatorToken
            {
                Operator: OperatorEnum.Increment 
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
            context.TranslationTable.AddSymbol(node, op.Position);
            return true;
        }

        return false;
    }

    private static bool TryParsePostfix(
        ParsingContext context,
        NodeBase inner,
        [NotNullWhen(true)] out NodeBase? output)
    {
        output = null;

        if (context.Sequence.Current() is OperatorToken operatorToken)
        {
            switch (operatorToken.Operator)
            {
                case OperatorEnum.Increment:
                    if (inner is not MemberNode or IndexerNode) return false;
                    output = new PostfixIncrementNode(inner);
                    context.TranslationTable.AddSymbol(output, operatorToken.Position);
                    context.Sequence.MoveNextNonWhiteSpace();
                    return true;
                case OperatorEnum.Decrement:
                    if (inner is not MemberNode or IndexerNode) return false;
                    output = new PostfixDecrementNode(inner);
                    context.TranslationTable.AddSymbol(output, operatorToken.Position);
                    context.Sequence.MoveNextNonWhiteSpace();
                    return true;
                case OperatorEnum.Access:
                    var sequenceFork = context.Fork();
                    if (!TryParseFieldAccessSequence(sequenceFork, inner, out var access)) return false;
                    context.Merge(sequenceFork);
                    output = access;
                    return true;
            }
        }

        var indexerFork = context.Fork();
        if (!TryParseArrayIndexerSequence(indexerFork, inner, out var indexers)) return false;
        
        context.Merge(indexerFork);
        output = indexers;
        return true;
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
            default:
                throw new Exception();
        }

        context.Merge(opFork);
        context.TranslationTable.AddSymbol(output, token.Position);
        return true;
    }

    #endregion

    #region Func call

    public static bool TryParseFuncCall(
        ParsingContext context,
        [NotNullWhen(true)] out CallNode? call)
    {
        call = null;
        if (context.Sequence.Current() is not Word funcName) return false;
        var start = context.Sequence.Current();
        context.Sequence.MoveNextNonWhiteSpace();

        if (context.Sequence.Current() is not OpenParen)
        {
            var record = PlampExceptionInfo.ExpectedOpenParen();
            context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentPosition));
            return false;
        }

        context.Sequence.MoveNextNonWhiteSpace();

        var argExpressions = new List<NodeBase>();
        if (context.Sequence.Current() is CloseParen)
        {
            var funcNameNode = new FuncCallNameNode(funcName.GetStringRepresentation());
            context.TranslationTable.AddSymbol(funcNameNode, funcName.Position);
            call = new CallNode(null, funcNameNode, argExpressions);
            context.Sequence.MoveNextNonWhiteSpace();
            context.TranslationTable.AddSymbol(call, context.Sequence.MakeRangeFromPrevNonWhitespace(start));
            return true;
        }

        if (!TryParsePrecedence(context, out var arg))
        {
            var record = PlampExceptionInfo.ExpectedExpression();
            context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentPosition));
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
                context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentPosition));
                call = null;
                return false;
            }
        }

        if (context.Sequence.Current() is not CloseParen)
        {
            var record = PlampExceptionInfo.ExpectedCloseParen();
            context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentPosition));
        }
        else
        {
            context.Sequence.MoveNextNonWhiteSpace();
        }

        var funcCallNameNode = new FuncCallNameNode(funcName.GetStringRepresentation());
        context.TranslationTable.AddSymbol(funcCallNameNode, funcName.Position);
        call = new CallNode(null, funcCallNameNode, argExpressions);
        context.TranslationTable.AddSymbol(call, context.Sequence.MakeRangeFromPrevNonWhitespace(start));
        return true;
    }

    #endregion

    #region Misc

    public static bool TryParseReturn(
        ParsingContext context,
        [NotNullWhen(true)] out ReturnNode? node)
    {
        node = null;
        if (context.Sequence.Current() is not KeywordToken { Keyword: Keywords.Return }) return false;
        var returnToken = context.Sequence.Current();
        context.Sequence.MoveNextNonWhiteSpace();
        if (context.Sequence.Current() is EndOfStatement)
        {
            ConsumeEndOfStatement(context);
            node = new ReturnNode(null);
            context.TranslationTable.AddSymbol(node, returnToken.Position);
            return true;
        }

        if (!TryParsePrecedence(context, out var expr))
        {
            var record = PlampExceptionInfo.ExpectedExpression();
            var current = context.Sequence.Current();
            context.Exceptions.Add(new PlampException(record, current.Position));
            return false;
        }

        ConsumeEndOfStatement(context);
        node = new ReturnNode(expr);
        context.TranslationTable.AddSymbol(node, returnToken.Position);
        return true;
    }

    public static bool TryParseExpression(
        ParsingContext context,
        [NotNullWhen(true)] out NodeBase? expression)
    {
        expression = null;
        var variableDefContext = context.Fork();
        if (TryParseVariableDefinitionSequence(variableDefContext, out var definition))
        {
            context.Merge(variableDefContext);
            expression = definition;
            return true;
        }

        var assignContext = context.Fork();
        if (TryParseAssignment(assignContext, out var assignment))
        {
            context.Merge(assignContext);
            expression = assignment;
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
        context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentPosition));
        return false;
    }

    public static bool TryParseVariableDefinitionSequence(
        ParsingContext context,
        [NotNullWhen(true)] out VariableDefinitionNode? definition)
    {
        definition = null;
        if (context.Sequence.Current() is not Word start) return false;

        var varNames = new List<VariableNameNode>();
        var first = true;
        do
        {
            var iterFork = context.Fork();
            if (!first) iterFork.Sequence.MoveNextNonWhiteSpace();
            if (iterFork.Sequence.Current() is not Word name) break;
            context.Merge(iterFork);

            var varName = new VariableNameNode(name.GetStringRepresentation());
            context.TranslationTable.AddSymbol(varName, context.Sequence.CurrentPosition);
            varNames.Add(varName);
            context.Sequence.MoveNextNonWhiteSpace();
            first = false;
        } while (context.Sequence.Current() is Comma);

        if (context.Sequence.Current() is not Colon)
        {
            var record = PlampExceptionInfo.ExpectedTypeName();
            context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentPosition));
            return false;
        }

        context.Sequence.MoveNextNonWhiteSpace();
        var typFork = context.Fork();
        if (!TryParseType(typFork, out var type))
        {
            var record = PlampExceptionInfo.ExpectedTypeName();
            context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentPosition));
            return false;
        }

        context.Merge(typFork);
        definition = new VariableDefinitionNode(type, varNames);
        context.TranslationTable.AddSymbol(definition, context.Sequence.MakeRangeFromPrevNonWhitespace(start));

        return true;
    }

    public static bool TryParseType(
        ParsingContext context,
        [NotNullWhen(true)] out TypeNode? type)
    {
        type = null;
        var start = context.Sequence.Current();
        if (!TryParseArrayDefinitionSequence(context, out var definitions)) return false;

        if (context.Sequence.Current() is not Word typeName)
        {
            var record = PlampExceptionInfo.ExpectedTypeName();
            context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentPosition));
            return false;
        }


        var typeNameNode = new TypeNameNode(typeName.GetStringRepresentation());
        type = new TypeNode(typeNameNode) { ArrayDefinitions = definitions };
        context.Sequence.MoveNextNonWhiteSpace();
        context.TranslationTable.AddSymbol(type, context.Sequence.MakeRangeFromPrevNonWhitespace(start));
        context.TranslationTable.AddSymbol(typeNameNode, typeName.Position);
        return true;
    }

    public static bool TryParseTypeInit(
        ParsingContext context,
        [NotNullWhen(true)] out InitTypeNode? initTypeNode)
    {
        initTypeNode = null;
        var start = context.Sequence.Current();
        
        var typeFork = context.Fork();
        if (!TryParseType(typeFork, out var type)) return false;
        context.Merge(typeFork);

        if (context.Sequence.Current() is not OpenCurlyBracket)
        {
            var record = PlampExceptionInfo.ExpectedBodyInCurlyBrackets();
            context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentPosition));
            return false;
        }

        context.Sequence.MoveNextNonWhiteSpace();
        var fields = new List<InitFieldNode>();
        var first = true;
        do
        {
            if (!first) context.Sequence.MoveNextNonWhiteSpace();
            if(!TryParseFieldInit(context, out var init)) break;
            fields.Add(init);
            first = false;
        } while (context.Sequence.Current() is EndOfStatement);

        if (context.Sequence.Current() is not CloseCurlyBracket)
        {
            var record = PlampExceptionInfo.ExpectedClosingCurlyBracket();
            context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentPosition));
            return false;
        }

        context.Sequence.MoveNextNonWhiteSpace();
        initTypeNode = new InitTypeNode(type, fields);
        var initPos = context.Sequence.MakeRangeFromPrevNonWhitespace(start);
        context.TranslationTable.AddSymbol(initTypeNode, initPos);
        return true;
    }

    private static bool TryParseFieldInit(
        ParsingContext context, 
        [NotNullWhen(true)]out InitFieldNode? initNode)
    {
        initNode = null;
        if (context.Sequence.Current() is not Word) return false;
        
        if (context.Sequence.Current() is not Word fieldName)
        {
            var record = PlampExceptionInfo.ExpectedFieldName();
            context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentPosition));
            return false;
        }
        
        var nameNode = new FieldNameNode(fieldName.GetStringRepresentation());
        context.TranslationTable.AddSymbol(nameNode, fieldName.Position);
        context.Sequence.MoveNextNonWhiteSpace();
        
        if (context.Sequence.Current() is not Colon colonSep)
        {
            var record = PlampExceptionInfo.ExpectedColon();
            context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentPosition));
            return false;
        }
        
        context.Sequence.MoveNextNonWhiteSpace();
        var fieldValueFork = context.Fork();
        if (!TryParsePrecedence(fieldValueFork, out var fieldValue))
        {
            var record = PlampExceptionInfo.ExpectedFieldValue();
            context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentPosition));
            return false;
        }
        context.Merge(fieldValueFork);
        
        initNode = new InitFieldNode(nameNode, fieldValue);
        context.TranslationTable.AddSymbol(initNode, colonSep.Position);

        return true;
    }

#endregion
    
    #region Util

    private static void AddUnexpectedTokenException(ParsingContext context)
    {
        var token = context.Sequence.Current();
        var record =
            PlampExceptionInfo.UnexpectedToken(token.GetStringRepresentation());
        context.Exceptions.Add(new PlampException(record, token.Position));
    }

    private static void ConsumeEndOfStatement(ParsingContext context)
    {
        if (context.Sequence.Current() is EndOfStatement)
        {
            context.Sequence.MoveNextNonWhiteSpace();
            return;
        }
        var record = PlampExceptionInfo.ExpectedEndOfStatement();
        context.Exceptions.Add(new PlampException(record, context.Sequence.CurrentPosition));
    }

    #endregion
}