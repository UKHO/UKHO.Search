using System.Diagnostics;

namespace FileShareImageBuilder;

public sealed class ImageLoader
{
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var env = ConfigurationReader.GetEnvironmentName();
        var dataImagePath = ConfigurationReader.GetDataImagePath();
        var imageName = $"fss-data-{env}";
        var tarPath = Path.Combine(dataImagePath, $"{imageName}.tar");

        if (!File.Exists(tarPath)) throw new FileNotFoundException("Docker image tar file not found.", tarPath);

        // Capture the currently-tagged image id so we can remove it after loading the new one.
        var previousImageId = await TryGetImageIdAsync(imageName, cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(previousImageId))
            Console.WriteLine($"[ImageLoader] Existing docker image detected. ImageId={previousImageId}");

        Console.WriteLine($"[ImageLoader] Loading docker image from '{tarPath}'...");
        await RunDockerAsync($"load -i \"{tarPath}\"", cancellationToken).ConfigureAwait(false);

        Console.WriteLine($"[ImageLoader] Verifying docker image '{imageName}' exists locally...");
        var output = await RunDockerCaptureAsync("image ls --format \"{{.Repository}}\"", cancellationToken)
            .ConfigureAwait(false);

        var repositories = output.Split(["\r\n", "\n"],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (!repositories.Contains(imageName, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Docker image '{imageName}' was not found after load.");

        Console.WriteLine($"[ImageLoader] Docker image '{imageName}' is present.");

        var loadedImageId = await TryGetImageIdAsync(imageName, cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(previousImageId)
            && !string.IsNullOrWhiteSpace(loadedImageId)
            && !string.Equals(previousImageId, loadedImageId, StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"[ImageLoader] Removing previous docker image. ImageId={previousImageId}");
            await RunDockerAsync($"image rm -f {previousImageId}", cancellationToken).ConfigureAwait(false);
        }

        // Remove any dangling layers left behind by tag replacement.
        Console.WriteLine("[ImageLoader] Pruning dangling docker images...");
        await RunDockerAsync("image prune -f", cancellationToken).ConfigureAwait(false);
    }

    private static async Task<string?> TryGetImageIdAsync(string repository, CancellationToken cancellationToken)
    {
        var output = await RunDockerCaptureAsync("image ls --format \"{{.Repository}}|{{.ID}}\"", cancellationToken)
            .ConfigureAwait(false);
        var lines = output.Split(["\r\n", "\n"],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var line in lines)
        {
            var parts = line.Split('|', 2);
            if (parts.Length != 2) continue;

            if (string.Equals(parts[0], repository, StringComparison.OrdinalIgnoreCase)) return parts[1];
        }

        return null;
    }

    private static async Task RunDockerAsync(string args, CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var p = new Process { StartInfo = psi, EnableRaisingEvents = true };

        var stdoutClosed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var stderrClosed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        p.OutputDataReceived += (_, e) =>
        {
            if (e.Data is null)
            {
                stdoutClosed.TrySetResult();
                return;
            }

            if (!string.IsNullOrWhiteSpace(e.Data)) Console.WriteLine($"[docker] {e.Data}");
        };

        p.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is null)
            {
                stderrClosed.TrySetResult();
                return;
            }

            if (!string.IsNullOrWhiteSpace(e.Data)) Console.Error.WriteLine($"[docker] {e.Data}");
        };

        if (!p.Start()) throw new InvalidOperationException("Failed to start docker process.");

        p.BeginOutputReadLine();
        p.BeginErrorReadLine();

        await p.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        await Task.WhenAll(stdoutClosed.Task, stderrClosed.Task).ConfigureAwait(false);

        if (p.ExitCode != 0) throw new InvalidOperationException($"docker {args} failed with exit code {p.ExitCode}.");
    }

    private static async Task<string> RunDockerCaptureAsync(string args, CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var p = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start docker process.");
        var stdout = await p.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        var stderr = await p.StandardError.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

        await p.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        if (p.ExitCode != 0)
            throw new InvalidOperationException(
                $"docker {args} failed with exit code {p.ExitCode}.\nSTDERR:\n{stderr}");

        return stdout;
    }
}