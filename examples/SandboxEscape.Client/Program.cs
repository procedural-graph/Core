using GameSharp.Sandbox;
using GameSharp.Sandbox.Services;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace SandboxEscape.Client;

internal sealed partial class Program
{
    private static async Task Main(string[] args)
    {
        LoggerProvider loggerProvider = await LoggerProvider.CreateAsync();
        ILogger local = loggerProvider.CreateLogger("Local");

        try
        {
            LogBeginInitalizing(local);

            // Validates the arguments and creates the application builder.
            ApplicationBuilder builder = Application.CreateDefaultBuilder(args);

            LogSettingUpServices(local);

            // Add our local and remote services to the application builder.
            builder.Services.AddRemote<ILogger>();
            builder.Services.AddLocal<SandboxTest>(CreateTestService);

            LogEndInitializing(local);

            // Build the application.
            using Application app = builder.Build();

            // Gets the remote logger service. Throws an argument exception if the logger service is not registered.
            ILogger remote = app.Services.GetOne<ILogger>();

            if (remote.IsEnabled(LogLevel.Information))
            {
                string assemblyName = Assembly.GetExecutingAssembly().Location;
                LogRemoteProcessStarted(remote, assemblyName, args[0], args[1]);
                await RunApplicationAsync(app);
                LogRemoteProcessStopping(remote, assemblyName);
            }
            else
            {
                await RunApplicationAsync(app);
            }
        }
        catch (Exception ex)
        {
            local.LogError(ex, "An error occurred while running the application.");
            throw;
        }
        finally
        {
            await loggerProvider.DisposeAsync();
        }
    }

    // A custom service resolver that creates a SandboxTest service.
    private static ServiceResolution CreateTestService(Type type, ref readonly ServiceResolver resolver)
    {
        if (resolver.TryGetService(out ILogger? logger))
        {
            SandboxTest test = new(logger);
            return resolver.Local(test); // Register the SandboxTest service as a local service.
        }

        // The ServiceCollectionBuilder builds the service provider over multiple passes.
        // If the ILogger service is not available yet (because this entry preceeds it in the list of services), 
        // we return an unresolved dependency. The service resolver will call this method again in the next pass, when the ILogger service is available.
        return ServiceResolution.UnresolvedDependency<ILogger>();
    }

    private static async Task RunApplicationAsync(Application app)
    {
        // Performs asynchronous initialization of the application, returning a Task that completes when the application is ready to run.
        await app.StartAsync();

        // Waits for the application to complete. Either because it has finished running or because it has been stopped.
        await app;
    }

    [LoggerMessage(EventId = 100, Level = LogLevel.Information, Message = "{assemblyName} started with arguments: {outboundPipeHandle}, {inboundPipeHandle}")]
    private static partial void LogRemoteProcessStarted(ILogger logger, string assemblyName, string outboundPipeHandle, string inboundPipeHandle);

    [LoggerMessage(EventId = 101, Level = LogLevel.Information, Message = "{assemblyName} is stopping.")]
    private static partial void LogRemoteProcessStopping(ILogger logger, string assemblyName);

    [LoggerMessage(EventId = 102, Level = LogLevel.Information, Message = "Initializing guest process.")]
    private static partial void LogBeginInitalizing(ILogger logger);

    [LoggerMessage(EventId = 103, Level = LogLevel.Information, Message = "Setting up services.")]
    private static partial void LogSettingUpServices(ILogger logger);

    [LoggerMessage(EventId = 104, Level = LogLevel.Information, Message = "Finished initializing guest process.")]
    private static partial void LogEndInitializing(ILogger logger);
}