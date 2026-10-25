using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FFGrabber.Services;

public sealed record SystemUsageSnapshot(
    DateTimeOffset SampledAt,
    int ProcessId,
    double ProcessCpuPercent,
    double SystemCpuPercent,
    double ProcessWorkingSetMiB,
    double ProcessPrivateMemoryMiB,
    int ProcessThreadCount,
    double TotalMemoryGiB,
    double UsedMemoryGiB,
    double UsedMemoryPercent);

/// <summary>
/// Samples FFmpeg process CPU/memory/thread use and total Windows CPU/RAM.
/// Reported CPU percentages are normalized to the full logical-processor capacity.
/// This is observational; it does not inject into or modify the FFmpeg process.
/// </summary>
public static class SystemUsageMonitor
{
    public static async Task RunAsync(int processId, Action<SystemUsageSnapshot> onSample, Func<bool> isRunning)
    {
        ArgumentNullException.ThrowIfNull(onSample);
        ArgumentNullException.ThrowIfNull(isRunning);

        TimeSpan? previousProcessCpu = null;
        var previousWall = Stopwatch.GetTimestamp();
        var previousSystem = TryReadSystemTimes();

        while (isRunning())
        {
            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(900)).ConfigureAwait(false);
                if (!isRunning()) break;

                using var process = Process.GetProcessById(processId);
                var wallNow = Stopwatch.GetTimestamp();
                var cpuNow = process.TotalProcessorTime;
                var wallSeconds = (wallNow - previousWall) / (double)Stopwatch.Frequency;
                var processCpu = previousProcessCpu is { } oldCpu && wallSeconds > 0
                    ? Math.Clamp((cpuNow - oldCpu).TotalSeconds / wallSeconds / Math.Max(1, Environment.ProcessorCount) * 100d, 0d, 100d)
                    : 0d;
                previousWall = wallNow;
                previousProcessCpu = cpuNow;

                var systemNow = TryReadSystemTimes();
                var systemCpu = 0d;
                if (previousSystem is { } oldSystem && systemNow is { } currentSystem)
                {
                    var totalDelta = currentSystem.Total - oldSystem.Total;
                    var idleDelta = currentSystem.Idle - oldSystem.Idle;
                    if (totalDelta > 0)
                        systemCpu = Math.Clamp((totalDelta - idleDelta) / (double)totalDelta * 100d, 0d, 100d);
                }
                previousSystem = systemNow;

                var memory = ReadMemoryStatus();
                onSample(new SystemUsageSnapshot(
                    DateTimeOffset.Now,
                    processId,
                    processCpu,
                    systemCpu,
                    process.WorkingSet64 / 1024d / 1024d,
                    process.PrivateMemorySize64 / 1024d / 1024d,
                    process.Threads.Count,
                    memory.TotalGiB,
                    memory.UsedGiB,
                    memory.UsedPercent));
            }
            catch (ArgumentException)
            {
                break; // The process has already exited.
            }
            catch (InvalidOperationException)
            {
                break;
            }
            catch (Exception ex) when (ex is Win32Exception or PlatformNotSupportedException or DllNotFoundException or EntryPointNotFoundException)
            {
                // A missing system counter must never disrupt an encode. Continue with the
                // values available on the next sample.
            }
        }
    }

    private static (double TotalGiB, double UsedGiB, double UsedPercent) ReadMemoryStatus()
    {
        try
        {
            var status = new MemoryStatusEx { Length = (uint)Marshal.SizeOf<MemoryStatusEx>() };
            if (GlobalMemoryStatusEx(ref status) && status.TotalPhysicalBytes > 0)
            {
                var used = status.TotalPhysicalBytes - status.AvailablePhysicalBytes;
                return (
                    status.TotalPhysicalBytes / 1024d / 1024d / 1024d,
                    used / 1024d / 1024d / 1024d,
                    Math.Clamp(used / (double)status.TotalPhysicalBytes * 100d, 0d, 100d));
            }
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or PlatformNotSupportedException)
        {
        }
        return (0d, 0d, 0d);
    }

    private static SystemTimes? TryReadSystemTimes()
    {
        try
        {
            if (GetSystemTimes(out var idle, out var kernel, out var user))
            {
                var idleTicks = ToUInt64(idle);
                var kernelTicks = ToUInt64(kernel);
                var userTicks = ToUInt64(user);
                return new SystemTimes(idleTicks, kernelTicks + userTicks);
            }
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or PlatformNotSupportedException)
        {
        }
        return null;
    }

    private static ulong ToUInt64(FILETIME fileTime) =>
        ((ulong)fileTime.dwHighDateTime << 32) | fileTime.dwLowDateTime;

    private readonly record struct SystemTimes(ulong Idle, ulong Total);

    [StructLayout(LayoutKind.Sequential)]
    private struct FILETIME
    {
        public uint dwLowDateTime;
        public uint dwHighDateTime;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MemoryStatusEx
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhysicalBytes;
        public ulong AvailablePhysicalBytes;
        public ulong TotalPageFileBytes;
        public ulong AvailablePageFileBytes;
        public ulong TotalVirtualBytes;
        public ulong AvailableVirtualBytes;
        public ulong AvailableExtendedVirtualBytes;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemTimes(out FILETIME idleTime, out FILETIME kernelTime, out FILETIME userTime);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);
}
