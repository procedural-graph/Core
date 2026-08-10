using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace GameSharp.Sandbox.Tests;

public sealed partial class SandboxTests(TestFixture fixture) : IClassFixture<TestFixture>
{
    [Fact]
    public async Task TestInternetConnectivityAsync()
    {
        await Assert.ThrowsAsync<HttpRequestException>(fixture.SandboxTest.AccessInternetAsync);
    }

    [Fact]
    public void TestFileSystemAccess()
    {
        Assert.Throws<UnauthorizedAccessException>(fixture.SandboxTest.AccessFileSystem);
    }
}
