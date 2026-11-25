namespace PennMUSH.ParityMcp.Sandboxes;

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using PennMUSH.ParityMcp.Telnet;

/// <summary>
/// Manages ephemeral PennMUSH C (Docker) and PennMUSH .NET server instances
/// on free localhost ports for isolated telnet parity testing.
/// </summary>
public sealed class ParitySandboxManager : IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _repoRoot;

    private string? _dockerContainerId;
    private Process? _dotnetProcess;
    private string? _tempDir;

    private TelnetSession? _cSession;
    private TelnetSession? _dotnetSession;

    public int CPort { get; private set; }
    public int DotnetPort { get; private set; }
    public string RepoRoot => _repoRoot;

    public ParitySandboxManager()
    {
        _repoRoot = ResolveRepoRoot();
    }

    public async Task EnsureStartedAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            if (_cSession is { IsConnected: true } && _dotnetSession is { IsConnected: true })
                return;

            await StopInternalAsync();
            await StartInternalAsync(ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ResetAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            await StopInternalAsync();
            await StartInternalAsync(ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ReconnectAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            if (_cSession != null)
            {
                await _cSession.DisposeAsync();
                _cSession = null;
            }
            if (_dotnetSession != null)
            {
                await _dotnetSession.DisposeAsync();
                _dotnetSession = null;
            }

            if (CPort <= 0 || DotnetPort <= 0)
            {
                await StopInternalAsync();
                await StartInternalAsync(ct);
                return;
            }

            _cSession = new TelnetSession();
            await _cSession.ConnectAndLoginAsync("127.0.0.1", CPort, "connect One", ct: ct);

            _dotnetSession = new TelnetSession();
            await _dotnetSession.ConnectAndLoginAsync("127.0.0.1", DotnetPort, "connect One", ct: ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<string> EvalCAsync(string command, bool queued = false, TimeSpan? timeout = null, CancellationToken ct = default)
    {
        await EnsureStartedAsync(ct);
        return await _cSession!.ExecuteAsync(command, queued, timeout, ct);
    }

    public async Task<string> EvalDotnetAsync(string command, bool queued = false, TimeSpan? timeout = null, CancellationToken ct = default)
    {
        await EnsureStartedAsync(ct);
        return await _dotnetSession!.ExecuteAsync(command, queued, timeout, ct);
    }

    private async Task StartInternalAsync(CancellationToken ct)
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"pennmush-parity-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        CPort = GetFreeTcpPort();
        DotnetPort = GetFreeTcpPort();
        int dotnetHttpPort = GetFreeTcpPort();

        // 1. Start PennMUSH C in Docker with unthrottled mush.cnf overrides
        string overrides = string.Join("\n", new[]
        {
            "port 4201",
            "ssl_port 0",
            "connect_fail_limit 0",
            "player_queue_limit 100000",
            "queue_chunk 1000",
            "active_q_chunk 0",
            "function_recursion_limit 500",
            "function_invocation_limit 10000000",
            "starting_quota 100000",
            "use_dns no",
            "log_commands no"
        });

        string shellCmd = $"cp mushcnf.dst /tmp/parity.cnf && cat <<'EOF' >> /tmp/parity.cnf\n{overrides}\nEOF\nexec ./netmush --no-session /tmp/parity.cnf";
        var dockerPsi = new ProcessStartInfo("docker")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        dockerPsi.ArgumentList.Add("run");
        dockerPsi.ArgumentList.Add("--rm");
        dockerPsi.ArgumentList.Add("-d");
        dockerPsi.ArgumentList.Add("-p");
        dockerPsi.ArgumentList.Add($"127.0.0.1:{CPort}:4201");
        dockerPsi.ArgumentList.Add("pennmush:latest");
        dockerPsi.ArgumentList.Add("sh");
        dockerPsi.ArgumentList.Add("-c");
        dockerPsi.ArgumentList.Add(shellCmd);

        using var dockerStart = Process.Start(dockerPsi)
            ?? throw new InvalidOperationException("Failed to start docker process.");
        string containerOut = (await dockerStart.StandardOutput.ReadToEndAsync(ct)).Trim();
        string containerErr = (await dockerStart.StandardError.ReadToEndAsync(ct)).Trim();
        await dockerStart.WaitForExitAsync(ct);
        if (dockerStart.ExitCode != 0 || string.IsNullOrWhiteSpace(containerOut))
            throw new InvalidOperationException($"docker run failed ({dockerStart.ExitCode}): {containerErr}");

        _dockerContainerId = containerOut;

        // 2. Start PennMUSH .NET against an isolated temp SQLite DB
        string dbPath = Path.Combine(_tempDir, "parity.db");
        string serverProj = Path.Combine(_repoRoot, "PennMUSH.Server", "PennMUSH.Server.csproj");
        var dotnetPsi = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = _repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        dotnetPsi.ArgumentList.Add("run");
        dotnetPsi.ArgumentList.Add("--no-build");
        dotnetPsi.ArgumentList.Add("--project");
        dotnetPsi.ArgumentList.Add(serverProj);
        dotnetPsi.ArgumentList.Add("--");
        dotnetPsi.ArgumentList.Add("--db");
        dotnetPsi.ArgumentList.Add(dbPath);
        dotnetPsi.ArgumentList.Add($"--Telnet:Port={DotnetPort}");
        dotnetPsi.ArgumentList.Add($"--urls=http://127.0.0.1:{dotnetHttpPort}");

        _dotnetProcess = Process.Start(dotnetPsi)
            ?? throw new InvalidOperationException("Failed to start PennMUSH.Server.");

        // Wait for both TCP listeners and log in as #1 ("connect One")
        await WaitForPortAsync("127.0.0.1", CPort, TimeSpan.FromSeconds(20), ct);
        await WaitForPortAsync("127.0.0.1", DotnetPort, TimeSpan.FromSeconds(20), ct);

        _cSession = new TelnetSession();
        await _cSession.ConnectAndLoginAsync("127.0.0.1", CPort, "connect One", TimeSpan.FromSeconds(15), ct);

        _dotnetSession = new TelnetSession();
        await _dotnetSession.ConnectAndLoginAsync("127.0.0.1", DotnetPort, "connect One", TimeSpan.FromSeconds(15), ct);
    }

    private async Task StopInternalAsync()
    {
        if (_cSession != null)
        {
            await _cSession.DisposeAsync();
            _cSession = null;
        }

        if (_dotnetSession != null)
        {
            await _dotnetSession.DisposeAsync();
            _dotnetSession = null;
        }

        if (!string.IsNullOrWhiteSpace(_dockerContainerId))
        {
            try
            {
                var stopPsi = new ProcessStartInfo("docker", $"rm -f {_dockerContainerId}")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                };
                using var p = Process.Start(stopPsi);
                if (p != null)
                    await p.WaitForExitAsync();
            }
            catch { }
            _dockerContainerId = null;
        }

        if (_dotnetProcess != null)
        {
            try
            {
                if (!_dotnetProcess.HasExited)
                    _dotnetProcess.Kill(entireProcessTree: true);
            }
            catch { }
            _dotnetProcess.Dispose();
            _dotnetProcess = null;
        }

        if (!string.IsNullOrWhiteSpace(_tempDir) && Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { }
            _tempDir = null;
        }
    }

    private static async Task WaitForPortAsync(string host, int port, TimeSpan timeout, CancellationToken ct)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                using var tcp = new TcpClient();
                await tcp.ConnectAsync(host, port, ct);
                return;
            }
            catch
            {
                await Task.Delay(150, ct);
            }
        }

        throw new TimeoutException($"Timed out waiting for {host}:{port} to accept connections.");
    }

    private static int GetFreeTcpPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static string ResolveRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "PennMUSH.Server", "PennMUSH.Server.csproj")))
                return dir.FullName;
            dir = dir.Parent;
        }

        var cwd = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (cwd != null)
        {
            if (File.Exists(Path.Combine(cwd.FullName, "PennMUSH.Server", "PennMUSH.Server.csproj")))
                return cwd.FullName;
            cwd = cwd.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate PennMUSH.Server/PennMUSH.Server.csproj from current directory.");
    }

    public async ValueTask DisposeAsync()
    {
        await _gate.WaitAsync();
        try
        {
            await StopInternalAsync();
        }
        finally
        {
            _gate.Release();
            _gate.Dispose();
        }
    }
}
