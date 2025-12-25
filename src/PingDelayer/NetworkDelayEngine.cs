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
    private volatile bool isDisposed;
    private int delayMilliseconds;
    private readonly object lockObject = new object();
    private int consecutiveErrors = 0;
    private const int MaxConsecutiveErrors = 10;

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
            if (isDisposed)
            {
                OnErrorOccurred("Cannot start: Engine has been disposed.");
                return false;
            }

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

            // Shutdown the handle first to unblock any pending Recv() calls
            if (handle != null && !handle.IsInvalid)
            {
                try
                {
                    handle.Shutdown(WinDivertShutdown.Both);
                }
                catch
                {
                    // Ignore shutdown errors
                }
            }

            // Wait for threads to complete with proper timeout
            bool captureThreadExited = true;
            bool releaseThreadExited = true;

            if (captureThread != null && captureThread.IsAlive)
            {
                try
                {
                    captureThreadExited = captureThread.Join(5000);
                    if (!captureThreadExited)
                    {
                        // Force abort if thread doesn't exit gracefully
                        try
                        {
                            captureThread.Interrupt();
                            captureThreadExited = captureThread.Join(2000);
                        }
                        catch
                        {
                            // Ignore interrupt errors
                        }
                    }
                }
                catch
                {
                    // Ignore join errors
                }
            }

            if (releaseThread != null && releaseThread.IsAlive)
            {
                try
                {
                    releaseThreadExited = releaseThread.Join(5000);
                    if (!releaseThreadExited)
                    {
                        // Force abort if thread doesn't exit gracefully
                        try
                        {
                            releaseThread.Interrupt();
                            releaseThreadExited = releaseThread.Join(2000);
                        }
                        catch
                        {
                            // Ignore interrupt errors
                        }
                    }
                }
                catch
                {
                    // Ignore join errors
                }
            }

            // CRITICAL: Wait longer for I/O completion callbacks to drain
            // This prevents the race condition where IOCompletionPoller callbacks
            // are invoked after handle disposal
            if (handle != null && !handle.IsInvalid)
            {
                try
                {
                    // Give the thread pool more time to process any queued I/O completions
                    // AccessViolationExceptions that slip through will be caught by global handlers
                    Thread.Sleep(2000); // Increased from 1000ms to 2000ms
                    
                    // Force thread pool to process pending items
                    ThreadPool.QueueUserWorkItem(_ => { });
                    Thread.Sleep(200); // Increased from 100ms
                }
                catch
                {
                    // Ignore sleep errors
                }
            }

            // Close the handle - this must happen after all threads exit
            // and I/O callbacks drain
            if (handle != null && !handle.IsInvalid)
            {
                try
                {
                    handle.Close();
                }
                catch
                {
                    // Ignore close errors - may throw if operations still pending
                }
            }
            handle = null;

            // Clear remaining packets
            try
            {
                while (packetQueue.Count > 0)
                {
                    try
                    {
                        var pkt = packetQueue.Dequeue();
                        pkt?.Dispose();
                    }
                    catch
                    {
                        break;
                    }
                }
                packetQueue.Clear();
            }
            catch
            {
                // Ignore cleanup errors
            }

            // Reset timer resolution
            try
            {
                HighResolutionTimer.ResetResolution();
            }
            catch
            {
                // Ignore
            }

            try
            {
                OnStatusChanged("Engine stopped.");
            }
            catch
            {
                // Ignore event notification errors
            }
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
            if (isDisposed)
            {
                return;
            }

            delayMilliseconds = delayMs;
            OnStatusChanged($"Delay updated to {delayMs}ms.");
        }
    }

    /// <summary>
    /// Captures packets from the network and adds them to the delay queue.
    /// </summary>
    private void CapturePackets()
    {
        try
        {
            while (isRunning && handle != null && !handle.IsInvalid)
            {
                try
                {
                    // Create a new packet and address for each receive
                    var packet = new WinDivertPacket(BufferSize);
                    var addr = new WinDivertAddress();
                    
                    // Receive packet - this may throw when handle is shutdown
                    handle.Recv(packet, addr);

                    // Check if we're still running after Recv unblocks
                    if (!isRunning)
                    {
                        packet.Dispose();
                        break;
                    }

                    if (packet.Length > 0)
                    {
                        // For debugging: try sending immediately to verify Send() works
                        if (delayMilliseconds == 0)
                        {
                            // Zero delay - send immediately and dispose
                            if (handle != null && !handle.IsInvalid && isRunning)
                            {
                                try
                                {
                                    handle.Send(packet, addr);
                                }
                                catch
                                {
                                    // Ignore send errors during shutdown
                                }
                            }
                            packet.Dispose();
                            continue;
                        }
                        
                        // Calculate release timestamp
                        long currentTimestamp = HighResolutionTimer.GetTimestamp();
                        long delayTicks = HighResolutionTimer.MillisecondsToTicks(delayMilliseconds);
                        long releaseTimestamp = currentTimestamp + delayTicks;

                        // Add to queue with the packet object itself
                        var delayedPacket = new DelayedPacket(packet, addr, releaseTimestamp);
                        packetQueue.Enqueue(delayedPacket);
                    }
                    else
                    {
                        packet.Dispose();
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

                        // Check handle validity before sending
                        WinDivert? currentHandle = handle;
                        if (currentHandle != null && !currentHandle.IsInvalid && isRunning)
                        {
                            try
                            {
                                // Send the packet object directly - no copying needed
                                currentHandle.Send(pkt.Packet, pkt.Address);
                                
                                // Reset error counter on successful send
                                consecutiveErrors = 0;
                            }
                            catch (Exception ex)
                            {
                                consecutiveErrors++;
                                
                                if (consecutiveErrors <= 3 && isRunning)
                                {
                                    // Only report first few errors to avoid spam
                                    OnErrorOccurred($"Error sending packet: {ex.Message}");
                                }
                            }
                            finally
                            {
                                // Dispose the packet after sending (success or failure)
                                try
                                {
                                    pkt.Dispose();
                                }
                                catch
                                {
                                    // Ignore disposal errors
                                }
                            }
                        }
                        else
                        {
                            // Handle is invalid, just dispose the packet
                            try
                            {
                                pkt.Dispose();
                            }
                            catch
                            {
                                // Ignore disposal errors
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
    /// Gets whether the engine has been disposed.
    /// </summary>
    public bool IsDisposed => isDisposed;

    /// <summary>
    /// Gets the number of packets currently in the queue.
    /// </summary>
    public int QueuedPacketCount => packetQueue.Count;

    protected virtual void OnErrorOccurred(string message)
    {
        try
        {
            // Prevent events from firing after disposal
            if (!isDisposed)
            {
                ErrorOccurred?.Invoke(this, message);
            }
        }
        catch
        {
            // Ignore errors in event handlers during shutdown
        }
    }

    protected virtual void OnStatusChanged(string message)
    {
        try
        {
            // Prevent events from firing after disposal
            if (!isDisposed)
            {
                StatusChanged?.Invoke(this, message);
            }
        }
        catch
        {
            // Ignore errors in event handlers during shutdown
        }
    }

    public void Dispose()
    {
        if (isDisposed)
        {
            return;
        }

        isDisposed = true;
        Stop();
        
        GC.SuppressFinalize(this);
    }
}
