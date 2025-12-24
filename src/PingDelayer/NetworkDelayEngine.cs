using System;
using System.Threading;
using WindivertDotnet;

namespace PingDelayer;

/// <summary>
/// Core network delay engine that uses WinDivert to intercept and delay network packets.
/// Implements low-jitter delay using high-resolution timers and priority queues.
/// </summary>
public class NetworkDelayEngine : IDisposable
{
    private WinDivert? handle;
    private Thread? captureThread;
    private Thread? releaseThread;
    private PacketQueue packetQueue;
    private volatile bool isRunning;
    private int delayMilliseconds;
    private readonly object lockObject = new object();

    // Buffer size for packet capture (64KB)
    private const int BufferSize = 65535;

    /// <summary>
    /// Event raised when an error occurs.
    /// </summary>
    public event EventHandler<string>? ErrorOccurred;

    /// <summary>
    /// Event raised when the engine status changes.
    /// </summary>
    public event EventHandler<string>? StatusChanged;

    public NetworkDelayEngine()
    {
        packetQueue = new PacketQueue();
        delayMilliseconds = 0;
    }

    /// <summary>
    /// Starts the network delay engine with the specified delay.
    /// </summary>
    /// <param name="delayMs">Delay in milliseconds to add to network traffic.</param>
    /// <returns>True if started successfully, false otherwise.</returns>
    public bool Start(int delayMs)
    {
        lock (lockObject)
        {
            if (isRunning)
            {
                OnStatusChanged("Engine is already running.");
                return false;
            }

            try
            {
                delayMilliseconds = delayMs;

                // Set high-resolution timer
                HighResolutionTimer.SetHighResolution();

                // Open WinDivert handle with a filter for all traffic
                // Filter: "true" means capture all packets
                handle = new WinDivert("true", WinDivertLayer.Network, 0, WinDivertFlag.None);

                if (handle == null || handle.IsInvalid)
                {
                    OnErrorOccurred("Failed to open WinDivert handle. Make sure the application is running as Administrator.");
                    return false;
                }

                isRunning = true;
                packetQueue.Clear();

                // Start capture thread with high priority
                captureThread = new Thread(CapturePackets)
                {
                    IsBackground = true,
                    Priority = ThreadPriority.Highest,
                    Name = "PacketCapture"
                };
                captureThread.Start();

                // Start release thread with high priority
                releaseThread = new Thread(ReleasePackets)
                {
                    IsBackground = true,
                    Priority = ThreadPriority.Highest,
                    Name = "PacketRelease"
                };
                releaseThread.Start();

                OnStatusChanged($"Engine started with {delayMs}ms delay.");
                return true;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Failed to start engine: {ex.Message}");
                Stop();
                return false;
            }
        }
    }

    /// <summary>
    /// Stops the network delay engine.
    /// </summary>
    public void Stop()
    {
        lock (lockObject)
        {
            if (!isRunning)
            {
                return;
            }

            isRunning = false;

            // Close WinDivert handle to interrupt blocking Recv calls
            if (handle != null && !handle.IsInvalid)
            {
                handle.Shutdown(WinDivertShutdown.Both);
                handle.Close();
            }
            handle = null;

            // Wait for threads to complete
            captureThread?.Join(2000);
            releaseThread?.Join(2000);

            // Reset timer resolution
            HighResolutionTimer.ResetResolution();

            // Clear remaining packets
            packetQueue.Clear();

            OnStatusChanged("Engine stopped.");
        }
    }

    /// <summary>
    /// Updates the delay without restarting the engine.
    /// </summary>
    /// <param name="delayMs">New delay in milliseconds.</param>
    public void UpdateDelay(int delayMs)
    {
        lock (lockObject)
        {
            delayMilliseconds = delayMs;
            OnStatusChanged($"Delay updated to {delayMs}ms.");
        }
    }

    /// <summary>
    /// Captures packets from the network and adds them to the delay queue.
    /// </summary>
    private void CapturePackets()
    {
        using var packet = new WinDivertPacket(BufferSize);
        var addr = new WinDivertAddress();

        try
        {
            while (isRunning && handle != null && !handle.IsInvalid)
            {
                try
                {
                    // Receive packet
                    handle.Recv(packet, addr);

                    if (packet.Length > 0)
                    {
                        // Calculate release timestamp
                        long currentTimestamp = HighResolutionTimer.GetTimestamp();
                        long delayTicks = HighResolutionTimer.MillisecondsToTicks(delayMilliseconds);
                        long releaseTimestamp = currentTimestamp + delayTicks;

                        // Copy packet data
                        byte[] packetData = new byte[packet.Length];
                        packet.Span.Slice(0, packet.Length).CopyTo(packetData);

                        // Add to queue
                        var delayedPacket = new DelayedPacket(packetData, addr, releaseTimestamp);
                        packetQueue.Enqueue(delayedPacket);
                    }
                }
                catch (Exception ex)
                {
                    if (isRunning)
                    {
                        OnErrorOccurred($"Error capturing packet: {ex.Message}");
                    }
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            if (isRunning)
            {
                OnErrorOccurred($"Capture thread error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Releases packets from the queue when their delay time has elapsed.
    /// </summary>
    private void ReleasePackets()
    {
        using var packet = new WinDivertPacket(BufferSize);
        
        try
        {
            while (isRunning)
            {
                if (packetQueue.Count == 0)
                {
                    // No packets to release, sleep briefly
                    Thread.Sleep(1);
                    continue;
                }

                if (packetQueue.TryPeek(out DelayedPacket? pkt) && pkt != null)
                {
                    long currentTimestamp = HighResolutionTimer.GetTimestamp();
                    long timeUntilRelease = pkt.ReleaseTimestamp - currentTimestamp;

                    if (timeUntilRelease <= 0)
                    {
                        // Time to release this packet
                        pkt = packetQueue.Dequeue();

                        if (handle != null && !handle.IsInvalid)
                        {
                            try
                            {
                                // Copy data to packet buffer
                                pkt.Data.AsSpan().CopyTo(packet.Span);
                                
                                // Re-inject packet into the network stack
                                handle.Send(packet, pkt.Address);
                            }
                            catch (Exception ex)
                            {
                                if (isRunning)
                                {
                                    OnErrorOccurred($"Error sending packet: {ex.Message}");
                                }
                                break;
                            }
                        }
                    }
                    else
                    {
                        // Calculate sleep time (avoid busy-waiting for long delays)
                        double sleepMs = HighResolutionTimer.TicksToMilliseconds(timeUntilRelease);
                        
                        if (sleepMs > 2)
                        {
                            // Sleep for most of the time, leaving buffer for precision
                            Thread.Sleep((int)(sleepMs - 1.5));
                        }
                        else
                        {
                            // For short delays, use spin-wait for better precision
                            Thread.SpinWait(10);
                        }
                    }
                }
                else
                {
                    Thread.Sleep(1);
                }
            }
        }
        catch (Exception ex)
        {
            if (isRunning)
            {
                OnErrorOccurred($"Release thread error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Gets the current delay in milliseconds.
    /// </summary>
    public int CurrentDelay => delayMilliseconds;

    /// <summary>
    /// Gets whether the engine is currently running.
    /// </summary>
    public bool IsRunning => isRunning;

    /// <summary>
    /// Gets the number of packets currently in the queue.
    /// </summary>
    public int QueuedPacketCount => packetQueue.Count;

    protected virtual void OnErrorOccurred(string message)
    {
        ErrorOccurred?.Invoke(this, message);
    }

    protected virtual void OnStatusChanged(string message)
    {
        StatusChanged?.Invoke(this, message);
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }
}
