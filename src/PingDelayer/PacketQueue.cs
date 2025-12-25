using System;
using System.Collections.Generic;
using WindivertDotnet;

namespace PingDelayer;

/// <summary>
/// Represents a packet with its data and scheduled release time.
/// </summary>
public class DelayedPacket : IDisposable
{
    public WinDivertPacket Packet { get; set; }
    public long ReleaseTimestamp { get; set; }
    public WinDivertAddress Address { get; set; }

    public DelayedPacket(WinDivertPacket packet, WinDivertAddress address, long releaseTimestamp)
    {
        Packet = packet;
        Address = address;
        ReleaseTimestamp = releaseTimestamp;
    }

    public void Dispose()
    {
        Packet?.Dispose();
    }
}

/// <summary>
/// Priority queue for managing delayed packets.
/// Packets are ordered by their release timestamp to ensure proper packet ordering.
/// </summary>
public class PacketQueue
{
    private readonly PriorityQueue<DelayedPacket, long> queue;
    private readonly object lockObject = new object();

    public PacketQueue()
    {
        queue = new PriorityQueue<DelayedPacket, long>();
    }

    /// <summary>
    /// Adds a packet to the queue with the specified release timestamp.
    /// </summary>
    /// <param name="packet">The packet to enqueue.</param>
    /// <param name="releaseTimestamp">The timestamp when the packet should be released.</param>
    public void Enqueue(DelayedPacket packet)
    {
        lock (lockObject)
        {
            queue.Enqueue(packet, packet.ReleaseTimestamp);
        }
    }

    /// <summary>
    /// Tries to peek at the next packet without removing it.
    /// </summary>
    /// <param name="packet">The next packet if available.</param>
    /// <returns>True if a packet is available, false otherwise.</returns>
    public bool TryPeek(out DelayedPacket? packet)
    {
        lock (lockObject)
        {
            return queue.TryPeek(out packet, out _);
        }
    }

    /// <summary>
    /// Dequeues the next packet from the queue.
    /// </summary>
    /// <returns>The next packet.</returns>
    public DelayedPacket Dequeue()
    {
        lock (lockObject)
        {
            return queue.Dequeue();
        }
    }

    /// <summary>
    /// Gets the number of packets in the queue.
    /// </summary>
    public int Count
    {
        get
        {
            lock (lockObject)
            {
                return queue.Count;
            }
        }
    }

    /// <summary>
    /// Clears all packets from the queue.
    /// </summary>
    public void Clear()
    {
        lock (lockObject)
        {
            queue.Clear();
        }
    }
}
