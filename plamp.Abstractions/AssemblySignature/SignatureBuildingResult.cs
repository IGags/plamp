using System.Collections.Generic;
using System.Reflection.Emit;
using plamp.Abstractions.Ast;

namespace plamp.Abstractions.AssemblySignature;

public record SignatureBuildingResult(
    AssemblyBuilder AssemblyBuilder, 
    List<PlampException> Exceptions);