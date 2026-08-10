// GameSharp Sandbox is designed to prevent sandboxed code from accessing the file system and the internet.
// The following methods attempt to access these resources, which should fail when executed in a sandboxed environment.

using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace SandboxEscape.Client;

internal sealed partial class SandboxTest(ILogger logger) : ISandboxTest
{
    public void AccessFileSystem()
    {
        string userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        LogAccessingDisallowedResource(logger, userProfilePath);
        FileInfo testFile = new(Path.Combine(userProfilePath, "sandbox_test.txt"));
        Stopwatch stopwatch = Stopwatch.StartNew();      
        try
        {
            using (FileStream stream = testFile.Create())
            {
                stream.Write("Hello world!"u8);
            }
            stopwatch.Stop();
            LogSuccessfullyAccessedResource(logger, testFile.FullName, stopwatch.Elapsed.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            LogFailedToAccessResource(logger, ex, testFile.FullName, stopwatch.Elapsed.TotalMilliseconds);
            throw;
        }
        finally
        {
            if (testFile.Exists)
            {
                testFile.Delete();
            }
        }
    }

    public async Task AccessInternetAsync()
    {
        const string RequestUri = "https://www.google.com";
        LogAccessingDisallowedResource(logger, RequestUri);
        Stopwatch stopwatch = Stopwatch.StartNew();
        HttpClient client = new()
        {
            Timeout = TimeSpan.FromSeconds(3)
        };
        try
        {
            await client.GetAsync(RequestUri);
            stopwatch.Stop();
            LogSuccessfullyAccessedResource(logger, RequestUri, stopwatch.Elapsed.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            LogFailedToAccessResource(logger, ex, RequestUri, stopwatch.Elapsed.TotalMilliseconds);
            throw;
        }
        finally
        {
            client.Dispose();
        }
    }

    [LoggerMessage(EventId = 200, Level = LogLevel.Information, Message = "Attempting to access '{resourceName}' ({methodName}).")]
    private static partial void LogAccessingDisallowedResource(ILogger logger, string resourceName, [CallerMemberName] string? methodName = null);

    [LoggerMessage(EventId = 201, Level = LogLevel.Error, Message = "Successfully accessed '{resourceName}' ({methodName}) in {elapsedMilliseconds}ms.")]
    private static partial void LogSuccessfullyAccessedResource(ILogger logger, string resourceName, double elapsedMilliseconds, [CallerMemberName] string? methodName = null);

    [LoggerMessage(EventId = 202, Level = LogLevel.Error, Message = "Failed to access '{resourceName}' ({methodName}) in {elapsedMilliseconds}ms.")]
    private static partial void LogFailedToAccessResource(ILogger logger, Exception exception, string resourceName, double elapsedMilliseconds, [CallerMemberName] string? methodName = null);
}
