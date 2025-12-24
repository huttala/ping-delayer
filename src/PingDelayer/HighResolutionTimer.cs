using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PingDelayer;

/// <summary>
/// Provides high-resolution timing utilities for precise packet delay simulation.
/// Uses Stopwatch.GetTimestamp() for ~100 nanosecond resolution.
/// </summary>
public static class HighResolutionTimer
{
    // P/Invoke declarations for multimedia timer resolution
    [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
    private static extern uint TimeBeginPeriod(uint period);

    [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
    private static extern uint TimeEndPeriod(uint period);

    private static bool isTimerPeriodSet = false;
    private static readonly object lockObject = new object();

    /// <summary>
    /// Frequency of the high-resolution timer in ticks per second.
    /// </summary>
    public static readonly long Frequency = Stopwatch.Frequency;

    /// <summary>
    /// Sets the system timer resolution to 1ms for more precise timing.
    /// This improves the accuracy of Thread.Sleep and other timing operations.
    /// </summary>
    public static void SetHighResolution()
    {
        lock (lockObject)
        {
            if (!isTimerPeriodSet)
            {
                TimeBeginPeriod(1); // Set timer resolution to 1ms
                isTimerPeriodSet = true;
            }
        }
    }

    /// <summary>
    /// Resets the system timer resolution to default.
    /// Should be called when the application exits.
    /// </summary>
    public static void ResetResolution()
    {
        lock (lockObject)
        {
            if (isTimerPeriodSet)
            {
                TimeEndPeriod(1); // Reset timer resolution
                isTimerPeriodSet = false;
            }
        }
    }

    /// <summary>
    /// Gets the current timestamp in high-resolution ticks.
    /// </summary>
    /// <returns>Current timestamp in ticks.</returns>
    public static long GetTimestamp()
    {
        return Stopwatch.GetTimestamp();
    }

    /// <summary>
    /// Converts milliseconds to high-resolution ticks.
    /// </summary>
    /// <param name="milliseconds">Time in milliseconds.</param>
    /// <returns>Equivalent time in ticks.</returns>
    public static long MillisecondsToTicks(double milliseconds)
    {
        return (long)(milliseconds * Frequency / 1000.0);
    }

    /// <summary>
    /// Converts high-resolution ticks to milliseconds.
    /// </summary>
    /// <param name="ticks">Time in ticks.</param>
    /// <returns>Equivalent time in milliseconds.</returns>
    public static double TicksToMilliseconds(long ticks)
    {
        return (double)ticks * 1000.0 / Frequency;
    }

    /// <summary>
    /// Performs a precise sleep operation with minimal jitter.
    /// Combines Thread.Sleep with spin-waiting for better accuracy.
    /// </summary>
    /// <param name="milliseconds">Time to sleep in milliseconds.</param>
    public static void PreciseSleep(double milliseconds)
    {
        if (milliseconds <= 0)
            return;

        long targetTicks = GetTimestamp() + MillisecondsToTicks(milliseconds);
        
        // Sleep for most of the time (if > 2ms), leaving some buffer for spin-wait
        if (milliseconds > 2)
        {
            Thread.Sleep((int)(milliseconds - 1.5));
        }
        
        // Spin-wait for the remaining time for better precision
        while (GetTimestamp() < targetTicks)
        {
            Thread.SpinWait(10);
        }
    }
}
