using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.CompilerEmission;
using plamp.CodeEmission.Tests.Infrastructure;
using plamp.ILCodeEmitters;

namespace plamp.CodeEmission.Tests;

/// <summary>
/// Here live constants
/// </summary>
public class ConstantEmissionTests
{
    //Decimal is not literal type
    [Theory]
    //Int
    [InlineData(0)]
    [InlineData(-1024)]
    [InlineData(1024)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    //Uint
    [InlineData(0u)]
    [InlineData(1024u)]
    [InlineData(uint.MaxValue)]
    //Short
    [InlineData(short.MinValue)]
    [InlineData(short.MaxValue)]
    //Ushort
    [InlineData(ushort.MaxValue)]
    [InlineData(ushort.MinValue)]
    //Long
    [InlineData(0L)]
    [InlineData(-1024L)]
    [InlineData(1L + int.MaxValue)]
    [InlineData(-1L - int.MinValue)]
    [InlineData(long.MaxValue)]
    [InlineData(long.MinValue)]
    //Ulong
    [InlineData(0ul)]
    [InlineData(1024ul)]
    [InlineData(1ul + uint.MaxValue)]
    [InlineData(ulong.MaxValue)]
    //Byte
    [InlineData(byte.MinValue)]
    [InlineData(byte.MaxValue)]
    //Float
    [InlineData(0.0f)]
    [InlineData(10.0f)]
    [InlineData(-10.0f)]
    [InlineData(float.MaxValue)]
    [InlineData(float.MinValue)]
    //Double
    [InlineData(0.0d)]
    [InlineData(-1d)]
    [InlineData(1d)]
    [InlineData(double.MaxValue)]
    [InlineData(double.MinValue)]
    //String
    [InlineData("abc")]
    [InlineData(null, typeof(string))]
    //Char
    [InlineData(' ')]
    [InlineData('a')]
    //Bool
    [InlineData(true)]
    [InlineData(false)]
    public async Task EmitConstantValid(object? constantValue, Type? constantType = null)
    {
        constantType ??= constantValue!.GetType();
        var tempVarName = "temp";
        /*
         * var temp
         * temp = literal
         * return var1
         */
        var ast = new BodyNode([
            new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(constantType), new MemberNode(tempVarName)),
            new AssignNode(new MemberNode(tempVarName), new LiteralNode(constantValue!, constantType)),
            new ReturnNode(new MemberNode(tempVarName))
        ]);

        var (instance, createdMethod) = await EmissionSetupHelper.CreateInstanceWithMethodAsync([], ast, constantType);
        var result = createdMethod!.Invoke(instance, []);

        if (constantValue == null)
        {
            Assert.Null(result);
            Assert.Equal(createdMethod.ReturnType, constantType);
        }
        else
        {
            Assert.Equal(constantValue, result);
            Assert.Equal(constantType, result!.GetType());
        }
    }

    [Theory]
    [InlineData(null, typeof(ArgumentException), typeof(int))]
    [InlineData(null, typeof(ArgumentException), typeof(long))]
    [InlineData(null, typeof(ArgumentException), typeof(byte))]
    [InlineData(null, typeof(ArgumentException), typeof(bool))]
    [InlineData(null, typeof(ArgumentException), typeof(short))]
    [InlineData(null, typeof(ArgumentException), typeof(ushort))]
    [InlineData(null, typeof(ArgumentException), typeof(uint))]
    [InlineData(null, typeof(ArgumentException), typeof(ulong))]
    public async Task EmitConstantInvalid(object? constantValue, Type exceptionType, Type? constantType = null)
    {
        constantType ??= constantValue?.GetType();
        const string methodName = "Test";
        var emitter = new DefaultIlCodeEmitter();
        var (_, _, methodBuilder, _) 
            = EmissionSetupHelper.CreateMethodBuilder(methodName, constantType!, []);
        var tempVarName = "temp";
        /*
         * var temp
         * temp = literal
         * return var1
         */
        var ast = new BodyNode([
            new VariableDefinitionNode(EmissionSetupHelper.CreateTypeNode(constantType!), new MemberNode(tempVarName)),
            new AssignNode(new MemberNode(tempVarName), new LiteralNode(constantValue!, constantType!)),
            new ReturnNode(new MemberNode(tempVarName))
        ]);
        var context = new CompilerEmissionContext(ast, methodBuilder, [], null);
        await Assert.ThrowsAsync(exceptionType, async () => await emitter.EmitMethodBodyAsync(context, CancellationToken.None));
    }
}