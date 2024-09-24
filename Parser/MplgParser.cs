using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Parser.Assembly;
using Parser.Ast;
using Parser.Token;

namespace Parser;

public class MplgParser
{
    private readonly TokenSequence _tokenSequence;
    
    public MplgParser(string code)
    {
        _tokenSequence = code.Tokenize();
    }
    
    //TODO: Несколько проходок парсером для независимости сигнатур компонентов
    public List<FuncExpression> Parse(List<IAssemblyDescription> assemblies)
    {
        var expressionList = new List<FuncExpression>();
        
        while (_tokenSequence.Current() != null)
        {
            ParseTopLevel(expressionList, assemblies);
        }

        return expressionList;
    }

    //TODO: multi iteration parsing
    //TODO: ambiguous assemblies
    private void ParseTopLevel(List<FuncExpression> expressions, List<IAssemblyDescription> assemblyDescriptions)
    {
        var token = _tokenSequence.GetNextNonWhiteSpace();
        if (token is Word word)
        {
            switch (word.ToKeyword())
            {
                case Keywords.Def:
                    ParseFunction(expressions, assemblyDescriptions);
                    return;
                case Keywords.Use:
                    ParseUsing(assemblyDescriptions);
                    return;
            }
                
        }
        throw new ParserException(token, "use or def keyword");
    }

    //TODO: using to expression tree
    private void ParseUsing(List<IAssemblyDescription> assemblyDescriptions)
    {
        throw new NotImplementedException();
    }
    
    private void ParseFunction(List<FuncExpression> expressions, List<IAssemblyDescription> assemblyDescriptions)
    {
        var returnType = new TypeDescription(ParseType(assemblyDescriptions));
        var functionNameToken = _tokenSequence.GetNextToken();
        if (functionNameToken is not Word wordToken)
        {
            throw new ParserException(functionNameToken, "Function name");
        }

        var functionName = wordToken.GetString();
        var args = ParseCommaSeparated<ParameterDescription, OpenBracket, CloseBracket>(() =>
            ParseParameter(assemblyDescriptions)); 
    }

    private ParameterDescription ParseParameter(List<IAssemblyDescription> assemblyDescriptions)
    {
        var type = new TypeDescription(ParseType(assemblyDescriptions));
        var nameToken = _tokenSequence.GetNextNonWhiteSpace();
        if (nameToken is not Word)
        {
            throw new ParserException(nameToken, "argument name");
        }

        return new ParameterDescription(type, nameToken.GetString());
    }
    
    //TODO: более детальные ошибки парсинга
    private Type ParseType(List<IAssemblyDescription> assemblyDescriptions)
    {
        var returnType = _tokenSequence.GetNextNonWhiteSpace();
        if (returnType is not Word word)
        {
            throw new ParserException(returnType, "function return type");
        }

        if (word.ToKeyword() != Keywords.Unknown)
        {
            throw new ParserException(word, "non keyword type");
        }

        var type = assemblyDescriptions.SelectMany(x => x.TypeMap)
            .FirstOrDefault(x => x.Value == word.GetString()).Key;
        if (type == null)
        {
            throw new UnexistingTypeException(word.GetString());
        }

        if (_tokenSequence.PeekNextNonWhiteSpace() is not OpenSquareBracket)
        {
            return type;
        }
        
        if (!type.IsGenericType)
        {
            throw new InvalidGenericTypeException($"the type {word.GetString()} isn't actually generic");
        }
        var innerArgs =
            ParseCommaSeparated<Type, OpenSquareBracket, CloseSquareBracket>(() =>
                ParseType(assemblyDescriptions));
        try
        {
            var completeGeneric = type.MakeGenericType(innerArgs.ToArray());
            return completeGeneric;
        }
        catch (Exception e)
        {
            throw new InvalidGenericTypeException(
                $"the number or order of generic type arguments is invalid for type: {type}");
        }
    }

    private List<TReturn> ParseCommaSeparated<TReturn, TOpen, TClose>(Func<TReturn> parserFunc) where TOpen : TokenBase where TClose : TokenBase
    {
        var token = _tokenSequence.PeekNextNonWhiteSpace();
        if (token.GetType() != typeof(TOpen))
        {
            throw new ParserException(token, typeof(TOpen).Name);
        }

        _tokenSequence.GetNextToken();
        var result = new List<TReturn>();
        while (true)
        {
            token = _tokenSequence.PeekNextNonWhiteSpace();
            if (token.GetType() == typeof(TClose))
            {
                _tokenSequence.GetNextToken();
                break;
            }
            
            result.Add(parserFunc());

            token = _tokenSequence.PeekNextNonWhiteSpace();
            if (token.GetType() == typeof(Comma))
            {
                _tokenSequence.GetNextNonWhiteSpace();
                continue;
            }

            if(token.GetType() == typeof(TClose))
            {
                _tokenSequence.GetNextToken();
                break;
            }


            throw new ParserException(token, "comma or close bracket");
        }

        return result;
    }
}