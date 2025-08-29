using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Kafka.Ksql.Linq.Tests.Integration;

internal static class DockerHelper
{
    // Resolve compose file path dynamically to work from any test working dir
    private static string ComposeFile => GetComposeFilePath();

    private static string GetComposeFilePath()
    {
        var envRoot = Environment.GetEnvironmentVariable("RC02_ROOT");
        if (!string.IsNullOrWhiteSpace(envRoot))
        {
            var p = System.IO.Path.Combine(envRoot, "physicalTests", "docker-compose.yaml");
            if (System.IO.File.Exists(p)) return p;
        }

        var dir = new System.IO.DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 8 && dir != null; i++)
        {
            var candidate = System.IO.Path.Combine(dir.FullName, "physicalTests", "docker-compose.yaml");
            if (System.IO.File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }

        return "physicalTests/docker-compose.yaml"; // best-effort fallback
    }

    public static Task StopServiceAsync(string service) =>
        RunAsync($"docker compose -f {ComposeFile} stop {service}");

    public static async Task StartServiceAsync(string service)
    {
        await RunAsync($"docker compose -f {ComposeFile} start {service}");
        // wait briefly for service to become available
        await Task.Delay(TimeSpan.FromSeconds(5));
    }

    private static async Task RunAsync(string command)
    {
        var psi = new ProcessStartInfo("bash", $"-lc \"{command}\"")
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
