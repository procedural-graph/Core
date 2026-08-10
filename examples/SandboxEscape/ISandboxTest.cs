using PolyType;
using StreamJsonRpc;
using System.Threading.Tasks;

namespace SandboxEscape;

[JsonRpcContract, GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial interface ISandboxTest
{
    Task AccessInternetAsync();

    void AccessFileSystem();
}
