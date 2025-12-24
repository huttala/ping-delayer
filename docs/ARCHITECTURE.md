# Ping Delayer - Architecture Overview

## System Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     MainWindow (WPF UI)                     │
│  - Delay slider/input                                       │
│  - Start/Stop controls                                      │
│  - Status display                                           │
│  - Message log                                              │
└────────────────┬────────────────────────────────────────────┘
                 │
                 │ Commands/Events
                 │
┌────────────────▼────────────────────────────────────────────┐
│              NetworkDelayEngine                             │
│  - Manages WinDivert handle                                 │
│  - Coordinates capture and release threads                  │
│  - Provides delay configuration                             │
└────┬─────────────────────────────────────────┬──────────────┘
     │                                         │
     │ Capture Thread                          │ Release Thread
     │ (High Priority)                         │ (High Priority)
     │                                         │
┌────▼─────────────────┐               ┌──────▼──────────────┐
│  Packet Capture      │               │  Packet Release     │
│  - WinDivert.Recv    │               │  - WinDivert.Send   │
│  - Add timestamp     │               │  - Check timestamp  │
│  - Enqueue packet    │               │  - Dequeue packet   │
└────┬─────────────────┘               └──────▲──────────────┘
     │                                         │
     │                                         │
     │         ┌───────────────────────────┐   │
     └────────►│    PacketQueue            │───┘
               │  PriorityQueue<Packet>    │
               │  Sorted by release time   │
               └───────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                   HighResolutionTimer                       │
│  - Stopwatch.GetTimestamp() for timing                      │
│  - timeBeginPeriod(1) for 1ms system resolution             │
│  - PreciseSleep() for accurate waits                        │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                     WinDivert Driver                        │
│  - Kernel-mode packet interception                          │
│  - Hooks into Windows network stack                         │
│  - Provides user-mode API                                   │
└─────────────────────────────────────────────────────────────┘
```

## Component Details

### MainWindow (UI Layer)

**Purpose**: User interface for controlling the delay engine

**Key Components**:
- `MainWindow.xaml`: XAML UI definition
- `MainWindow.xaml.cs`: UI logic and event handling

**Responsibilities**:
- Display current status (active/inactive)
- Accept user input for delay configuration
- Show real-time packet queue statistics
- Display log messages
- Handle window lifecycle

**Interactions**:
- Creates and manages `NetworkDelayEngine` instance
- Subscribes to engine events (StatusChanged, ErrorOccurred)
- Uses `DispatcherTimer` for UI updates

### NetworkDelayEngine (Core Layer)

**Purpose**: Orchestrates packet capture, delay, and re-injection

**Key Features**:
- Thread-safe start/stop operations
- Dynamic delay updates without restart
- Error handling and reporting
- Clean resource cleanup

**Threads**:
1. **Capture Thread** (High Priority):
   - Continuously receives packets from WinDivert
   - Calculates release timestamp
   - Enqueues packets into priority queue

2. **Release Thread** (High Priority):
   - Monitors priority queue
   - Releases packets when delay time elapsed
   - Uses efficient sleep mechanism

**State Management**:
- `isRunning`: Volatile flag for thread coordination
- `delayMilliseconds`: Current delay setting
- `lockObject`: Synchronizes state changes

### PacketQueue (Data Structure)

**Purpose**: Manages delayed packets with proper ordering

**Implementation**:
- Uses `PriorityQueue<DelayedPacket, long>`
- Priority is the release timestamp (earlier = higher priority)
- Thread-safe operations with lock synchronization

**DelayedPacket**:
```csharp
{
    byte[] Data;              // Packet payload
    WinDivertAddress Address; // Routing information
    long ReleaseTimestamp;    // When to send (in ticks)
}
```

### HighResolutionTimer (Utility)

**Purpose**: Provides precise timing capabilities

**Key Features**:
1. **High-Resolution Timestamps**:
   - Uses `Stopwatch.GetTimestamp()`
   - ~100 nanosecond resolution
   - Frequency: `Stopwatch.Frequency` ticks per second

2. **System Timer Resolution**:
   - Calls `timeBeginPeriod(1)` via P/Invoke
   - Sets system timer to 1ms granularity
   - Improves accuracy of `Thread.Sleep()`

3. **Precise Sleep**:
   - Hybrid approach: Sleep + SpinWait
   - Sleeps for (target - 1.5ms), then spin-waits
   - Minimizes jitter while avoiding busy-wait

## Timing Architecture

### Why Low Jitter Matters

**Jitter** = Variance in packet delay

High jitter causes:
- Rubber-banding in games
- Choppy video streaming
- Poor VoIP quality
- Unpredictable application behavior

### Low-Jitter Techniques

1. **High-Resolution Timestamps**:
   ```
   Standard DateTime.Now:  ~15ms resolution
   Stopwatch.GetTimestamp: ~100ns resolution
   
   Improvement: 150,000x better precision
   ```

2. **Priority Queue Ordering**:
   ```
   Without queue: Packets may be released out of order
   With queue: Strict ordering by timestamp
   
   Result: Maintains packet sequence
   ```

3. **High-Priority Threads**:
   ```
   Normal priority: May be preempted by other threads
   Highest priority: CPU scheduler favors these threads
   
   Result: More consistent processing
   ```

4. **Multimedia Timer Resolution**:
   ```
   Default timer: ~15.6ms granularity
   With timeBeginPeriod(1): ~1ms granularity
   
   Result: More accurate Thread.Sleep()
   ```

5. **Hybrid Sleep**:
   ```
   All Sleep: Subject to timer granularity (~1-15ms)
   All SpinWait: 100% CPU usage, system unresponsive
   Hybrid: Sleep most of time, spin-wait final portion
   
   Result: Accurate timing with reasonable CPU usage
   ```

## Threading Model

```
Main Thread (UI)
  - Handles WPF UI events
  - Updates status display
  - Processes user input

Capture Thread (High Priority)
  - Blocks on WinDivert.Recv()
  - CPU usage: Low (waiting for packets)
  - Lock contention: Low (only for queue operations)

Release Thread (High Priority)
  - Monitors packet queue
  - CPU usage: Low to Moderate (depends on packet rate)
  - Lock contention: Low (only for queue operations)

Timer Thread (Normal Priority)
  - DispatcherTimer for UI updates
  - CPU usage: Minimal (runs every 100ms)
  - No contention with delay engine
```

## Data Flow

### Packet Capture Flow

```
1. Network packet arrives
2. WinDivert driver intercepts packet
3. Capture thread receives via WinDivert.Recv()
4. Calculate release time: Now + Delay
5. Create DelayedPacket with data + address + timestamp
6. Lock queue, enqueue packet, unlock
7. Continue loop
```

### Packet Release Flow

```
1. Check if queue is empty (sleep if yes)
2. Peek at next packet (earliest timestamp)
3. Calculate time until release
4. If release time reached:
   a. Dequeue packet
   b. Copy data to packet buffer
   c. Call WinDivert.Send()
   d. Continue loop
5. Else:
   a. Sleep/spin-wait for remaining time
   b. Continue loop
```

## Performance Characteristics

### Memory Usage

- **Packet Queue**: O(n) where n = packets in flight
- **Steady State**: ~50-500 packets at 100ms delay
- **Per Packet**: ~1.5KB average (MTU + overhead)
- **Total**: Typically <1MB for queue

### CPU Usage

- **Idle Network**: <1% CPU
- **Moderate Traffic** (100 packets/sec): 2-5% CPU
- **High Traffic** (1000 packets/sec): 10-20% CPU
- **Note**: High priority threads don't starve other processes

### Latency Characteristics

- **Target Delay**: User-configured (0-1000ms)
- **Actual Delay**: Target ± jitter
- **Expected Jitter**: ±1-5ms (compared to ±50ms+ for some tools)
- **Factors Affecting Jitter**:
  - System load
  - Packet rate
  - CPU speed
  - Timer resolution

## Error Handling

### Driver Errors

- **Driver not loaded**: Show error, suggest admin privileges
- **Driver busy**: Show error, check for other tools using WinDivert
- **Driver crash**: Catch exception, clean shutdown

### Thread Errors

- **Receive error**: Log, break capture loop, trigger stop
- **Send error**: Log, break release loop, trigger stop
- **Lock timeout**: Should not occur (no timeouts used)

### Resource Errors

- **Out of memory**: Rare (packets are small), but would cause exceptions
- **Handle leaks**: Prevented by using `using` statements and Dispose()

## Security Considerations

### Required Privileges

- **Administrator**: Required for WinDivert driver loading
- **Manifest**: app.manifest requests `requireAdministrator`

### Attack Surface

- **Minimal**: Application doesn't expose network services
- **Local only**: No remote access
- **Read/Write**: Can read/modify network traffic (by design)

### Best Practices

- Run only when needed
- Stop before closing
- Monitor system behavior
- Don't use on untrusted networks

## Future Architecture Improvements

### Potential Enhancements

1. **Filter Pipeline**:
   - Per-protocol filters (TCP, UDP, ICMP)
   - Per-port filters
   - Per-application filters

2. **Statistics Module**:
   - Packet rate tracking
   - Delay distribution histogram
   - Jitter measurement

3. **Configuration System**:
   - Save/load profiles
   - Preset configurations
   - Import/export settings

4. **Multi-Engine Support**:
   - Different delays per filter
   - Separate inbound/outbound delays

5. **Plugin Architecture**:
   - Custom delay algorithms
   - Packet modification
   - Traffic shaping
