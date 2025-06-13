using plamp.CodeEmission.Tests.Infrastructure;

namespace plamp.CodeEmission.Tests;

public class LoopEmissionTests
{
    [Fact]
    public void EmitWhileLoop()
    {
        const string methodName = "WhileIter";
        var argType = typeof(int);
        var (_, typeBuilder, methodBuilder, _) =
            EmissionSetupHelper.CreateMethodBuilder(methodName, typeof(int), [argType]);
        var arg = new TestParameter(argType, "n");

        /*
         * int iter
         * iter = 0
         * while(iter < n)
         *     iter = iter + 1
         * return iter
         */
    }
}