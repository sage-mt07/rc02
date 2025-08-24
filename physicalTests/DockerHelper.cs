using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Kafka.Ksql.Linq.Tests.Integration;

internal static class DockerHelper
{
    private const string ComposeFile = "tools/docker-compose.kafka.yml";

    public static Task StopServiceAsync(string service) =>
        RunAsync($"docker-compose -f {ComposeFile} stop {service}");

    public static async Task StartServiceAsync(string service)
    {
        await RunAsync($"docker-compose -f {ComposeFile} start {service}");
        // wait briefly for service to become available
        await Task.Delay(TimeSpan.FromSeconds(5));
    }

    private static async Task RunAsync(string command)
    {
        var psi = new ProcessStartInfo("bash", $"-c \"{command}\"")
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };
        using var process = Process.Start(psi)!;
        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            throw new InvalidOperationException(error);
        }
    }
}
