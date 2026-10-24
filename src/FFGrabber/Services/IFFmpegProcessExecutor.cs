namespace FFGrabber.Services;

/// <summary>
/// Injectable process boundary used by FFmpeg import validation tests. Production calls
/// still use FFmpegProcessRunner; tests can return simulated version banners without any
/// native FFmpeg files or process execution.
/// </summary>
internal interface IFFmpegProcessExecutor
{
    Task<ProcessRunResult> RunAsync(
        string executable,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default);
}

internal sealed class FFmpegProcessExecutor : IFFmpegProcessExecutor
{
    public Task<ProcessRunResult> RunAsync(
        string executable,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default) =>
        FFmpegProcessRunner.RunAsync(
            executable,
            arguments,
            cancellationToken: cancellationToken);
}
