using System.Collections.Generic;
using System.IO;
using System.Text;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Symbols.SymTable;
using plamp.Alternative.Parsing;
using plamp.Alternative.SymbolsBuildingImpl;
using plamp.Alternative.Visitors.ModulePreCreation;
using plamp.Alternative.Visitors.ModulePreCreation.TypeInference;
using plamp.Alternative.Visitors.SymbolTableBuilding;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Visitors.ModulePreCreation.TypeInference;

public class FieldAccessInferenceTests
{
    private const string ModuleDefinitionsCode = """
                                                 module test;
                                                 data Complex{ Re, Im: float }
                                                 data Point { X, Y, Z: Complex }
                                                 """;
    
     [Fact]
     //Присваивание в поле - типы совпадают
     public void AssignToField_Correct()
     {
         var symbols = MakeSymTable();
         const string code = """
                             {
                                 a := Complex{};
                                 a.Re := 1;
                             }
                             """;
         var (body, translationTable) = ParseBody(code);
         var context = WeaveTypes(translationTable, body, symbols);
         context.Exceptions.ShouldBeEmpty();
     }

     [Fact]
     //Присваивание в поле - типы не совпадают
     public void AssignToField_Incorrect()
     {
         var symbols = MakeSymTable();
         const string code = """
                             {
                                 a := Complex{};
                                 a.Re := "nre";
                             }
                             """;
         var (body, translationTable) = ParseBody(code);
         var context = WeaveTypes(translationTable, body, symbols);
         var ex = context.Exceptions.ShouldHaveSingleItem();
         ex.Code.ShouldBe(PlampExceptionInfo.CannotAssign().Code);
     }

     [Fact]
     //Присваивание в цепочку из 2х полей - типы совпадают
     public void AssignToFieldSequence_Correct()
     {
         var symbols = MakeSymTable();
         const string code = """
                             {
                                 a := Point{};
                                 a.X.Re := 1;
                             }
                             """;
         var (body, translationTable) = ParseBody(code);
         var context = WeaveTypes(translationTable, body, symbols);
         context.Exceptions.ShouldBeEmpty();
     }
     
     [Fact]
     //Присваивание в цепочку из 2х полей - типы не совпадают
     public void AssignToFieldSequence_Incorrect()
     {
         var symbols = MakeSymTable();
         const string code = """
                             {
                                 a := Point{};
                                 a.X.Re := "nre";
                             }
                             """;
         var (body, translationTable) = ParseBody(code);
         var context = WeaveTypes(translationTable, body, symbols);
         var ex = context.Exceptions.ShouldHaveSingleItem();
         ex.Code.ShouldBe(PlampExceptionInfo.CannotAssign().Code);
     }

     [Fact]
     //Присваивание из поля - типы совпадают
     public void AssignFromField_Correct()
     {
         var symbols = MakeSymTable();
         const string code = """
                             {
                                 b: float;
                                 a := Complex{};
                                 b := a.Im;
                             }
                             """;
         var (body, translationTable) = ParseBody(code);
         var context = WeaveTypes(translationTable, body, symbols);
         context.Exceptions.ShouldBeEmpty();
     }

     [Fact]
     //Присваивание из поля - типы не совпадают
     public void AssignFromField_Incorrect()
     {
         var symbols = MakeSymTable();
         const string code = """
                             {
                                 b: string;
                                 a := Complex{};
                                 b := a.Im;
                             }
                             """;
         var (body, translationTable) = ParseBody(code);
         var context = WeaveTypes(translationTable, body, symbols);
         var ex = context.Exceptions.ShouldHaveSingleItem();
         ex.Code.ShouldBe(PlampExceptionInfo.CannotAssign().Code);
     }

     [Fact]
     //Присваивание из цепочки из 2х полей - типы совпадают
     public void AssignFromFieldSequence_Correct()
     {
         var symbols = MakeSymTable();
         const string code = """
                             {
                                 b: float;
                                 a := Point{};
                                 b := a.X.Im;
                             }
                             """;
         var (body, translationTable) = ParseBody(code);
         var context = WeaveTypes(translationTable, body, symbols);
         context.Exceptions.ShouldBeEmpty();
     }

     [Fact]
     //Присваивание из цепочки из 2х полей - типы не совпадают
     public void AssignFromFieldSequence_Incorrect()
     {
         var symbols = MakeSymTable();
         const string code = """
                             {
                                 b: string;
                                 a := Point{};
                                 b := a.Z.Im;
                             }
                             """;
         var (body, translationTable) = ParseBody(code);
         var context = WeaveTypes(translationTable, body, symbols);
         var ex = context.Exceptions.ShouldHaveSingleItem();
         ex.Code.ShouldBe(PlampExceptionInfo.CannotAssign().Code);
     }

     [Fact]
     //Присваивание в поле, у объекта которого нет типа - ошибка только на объекте.
     public void AssignFieldToNonExistingType_Incorrect()
     {
         const string code = """
                             {
                                 a := NeverHood{}
                                 a.DefinitelyExistingField := 1;
                             }
                             """;
         var (body, translationTable) = ParseBody(code);
         var context = WeaveTypes(translationTable, body, null);
         var ex = context.Exceptions.ShouldHaveSingleItem();
         ex.Code.ShouldBe(PlampExceptionInfo.TypeIsNotFound("").Code);
     }

     [Fact]
     //Присваивание в поле, которое не найдено - ошибка.
     public void AssignToNonExistingField_Incorrect()
     {
         var symbols = MakeSymTable();
         const string code = """
                             {
                                 a := Complex{};
                                 a.Real := 1;
                             }
                             """;
         var (body, translationTable) = ParseBody(code);
         var context = WeaveTypes(translationTable, body, symbols);
         var ex = context.Exceptions.ShouldHaveSingleItem();
         ex.Code.ShouldBe(PlampExceptionInfo.FieldIsNotFound().Code);
     }
     
     [Fact]
     //Присваивание из поля, которое не найдено - ошибка.
     public void AssignFromNonExistingField_Incorrect()
     {
         var symbols = MakeSymTable();
         const string code = """
                             {
                                 a := Complex{};
                                 b := a.Imaginary;
                             }
                             """;
         var (body, translationTable) = ParseBody(code);
         var context = WeaveTypes(translationTable, body, symbols);
         var ex = context.Exceptions.ShouldHaveSingleItem();
         ex.Code.ShouldBe(PlampExceptionInfo.FieldIsNotFound().Code);
     }

    private PreCreationContext WeaveTypes(ITranslationTable translationTable, BodyNode body, ISymTable? symbols)
    {
        List<ISymTable> deps = symbols == null ? [Builtins.SymTable] : [symbols, Builtins.SymTable];
        var preCreationContext = new PreCreationContext(translationTable, deps);
        var weaver = new TypeInferenceWeaver();
        preCreationContext = weaver.WeaveDiffs(body, preCreationContext);
        return preCreationContext;
    }
    
    private (BodyNode, ITranslationTable) ParseBody(string code)
    {
        var context = CompilationPipelineBuilder.CreateParsingContext(code);
        Parser.TryParseBody(context, out var body);
        body.ShouldNotBeNull();
        return (body, context.TranslationTable);
    }
    
    private ISymTable MakeSymTable()
    {
        using var ms = new MemoryStream();
        ms.Write(Encoding.UTF8.GetBytes(ModuleDefinitionsCode));
        ms.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(ms);

        var parsing = CompilationPipeline.RunParsingAsync(reader, "f.plp").Result;
        var symTable = new SymTableBuilder();

        var context = new SymbolTableBuildingContext(parsing.Context.TranslationTable,
            SymbolTableInitHelper.CreateDefaultTables(), symTable);
        CompilationPipeline.RunSymTableBuilding(parsing.Ast, context);
        return symTable;
    }
}