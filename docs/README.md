# Ping Delayer - Network Latency Simulator

A Windows 11 desktop application that simulates network latency with minimal jitter. This tool allows you to add a configurable delay to all network traffic on your PC, perfect for testing applications under high-latency conditions or simulating distant server connections.

## Features

- **Low-Jitter Delay**: Implements consistent, predictable delay without variance that causes rubber-banding
- **High-Resolution Timing**: Uses `Stopwatch.GetTimestamp()` for ~100 nanosecond resolution
- **Priority Queue Management**: Ensures proper packet ordering with timestamp-based release
- **High-Priority Processing**: Dedicated threads with `ThreadPriority.Highest` for packet handling
- **Multimedia Timer Resolution**: Sets system timer to 1ms resolution for precise timing
- **Simple WPF Interface**: Clean, easy-to-use desktop UI
- **Configurable Delay**: Supports delays from 0 to 1000+ milliseconds
- **Real-time Updates**: Change delay on-the-fly without restarting

## Why Ping Delayer?

Unlike other tools like [Clumsy](https://github.com/jagt/clumsy), Ping Delayer is specifically designed to provide **consistent delay with minimal jitter**. This makes it ideal for:

- Testing online games under high-latency conditions without rubber-banding
- Simulating overseas server connections for development
- Testing application behavior under various network conditions
- Load testing network-dependent applications

## Technology Stack

- **.NET 8.0** targeting Windows
- **WPF** (Windows Presentation Foundation) for UI
- **WinDivert** via [WindivertDotnet](https://www.nuget.org/packages/WindivertDotnet/) for packet interception
- High-resolution performance counters for precise timing

## Requirements

- **Windows 10 or Windows 11** (64-bit)
- **.NET 8.0 Runtime** or later
- **Administrator privileges** (required for WinDivert driver)
- **Visual Studio 2022** (for building from source)

## How It Works

### WinDivert

Ping Delayer uses the [WinDivert](https://reqrypt.org/windivert.html) library, which is a user-mode packet capture and manipulation library for Windows. WinDivert works by:

1. Installing a Windows driver that hooks into the Windows network stack
2. Intercepting packets before they reach the network adapter (for outbound) or application (for inbound)
3. Allowing user-mode applications to inspect, modify, or delay packets
4. Re-injecting packets back into the network stack

This approach provides:
- **Low overhead**: Packets are processed in kernel mode
- **Flexibility**: All packets can be intercepted and modified
- **Transparency**: Applications don't need to be modified

### Low-Jitter Implementation

The key to minimal jitter is consistent timing. Ping Delayer implements several techniques:

1. **High-Resolution Timers**: Uses `Stopwatch.GetTimestamp()` instead of `DateTime.Now` for ~100 nanosecond resolution
2. **Priority Queue**: Packets are stored in a `PriorityQueue<Packet, long>` sorted by release timestamp
3. **High-Priority Threads**: Packet capture and release threads run at `ThreadPriority.Highest`
4. **Multimedia Timer**: Calls `timeBeginPeriod(1)` to set system timer resolution to 1ms
5. **Efficient Sleep**: Combines `Thread.Sleep` with spin-waiting for better timing accuracy

## Building from Source

### Using Visual Studio

1. Clone the repository:
   ```bash
   git clone https://github.com/huttala/ping-delayer.git
   cd ping-delayer
   ```

2. Open the solution:
   ```bash
   cd src/PingDelayer
   ```

3. Open `PingDelayer.csproj` in Visual Studio 2022

4. Build the solution (Ctrl+Shift+B)

### Using .NET CLI

1. Clone the repository:
   ```bash
   git clone https://github.com/huttala/ping-delayer.git
   cd ping-delayer
   ```

2. Build the project:
   ```bash
   cd src/PingDelayer
   dotnet build -c Release
   ```

3. The executable will be in `bin/Release/net8.0-windows/PingDelayer.exe`

## Usage

### Running the Application

1. **Right-click** on `PingDelayer.exe` and select **"Run as administrator"**
   - Administrator privileges are required for the WinDivert driver to function

2. The application will open with the following interface:
   - **Status indicator**: Shows whether delay is Active (green) or Inactive (red)
   - **Delay slider**: Adjust delay from 0 to 1000 milliseconds
   - **Delay text box**: Enter a specific delay value
   - **Start/Stop buttons**: Control the delay simulation
   - **Queued Packets counter**: Shows how many packets are currently delayed
   - **Message log**: Displays status messages and errors

### Basic Operation

1. **Set the delay**: Use the slider or text box to set your desired delay (e.g., 100ms)
2. **Click "Start Delay"**: The application will begin intercepting and delaying packets
3. **Monitor status**: Watch the status indicator turn green and packet count
4. **Adjust on-the-fly**: You can change the delay while running
5. **Click "Stop Delay"**: Stops interception and releases all queued packets

### Best Practices

- **Start with small delays**: Begin with 20-50ms to verify the application works correctly
- **Monitor packet queue**: Large queue sizes may indicate system overload
- **Test your application**: Verify your application behaves as expected under delay
- **Stop when done**: Always stop the delay before closing the application

## Security Considerations

### WinDivert Security

WinDivert is a legitimate tool used for network testing and development, but it is a powerful utility that requires administrator privileges. Here are important security considerations:

1. **Administrator Privileges**: Required for driver installation and operation
2. **Antivirus Warnings**: Some antivirus software may flag WinDivert as potentially unwanted
   - This is a false positive due to its packet interception capabilities
   - You may need to add an exception for Ping Delayer
3. **Network Interception**: While running, all network traffic is intercepted
   - The application only delays packets; it does not inspect or modify payload data
   - Sensitive data is not logged or stored
4. **Driver Installation**: WinDivert installs a kernel-mode driver
   - The driver is digitally signed
   - It is removed when the application closes

### Responsible Use

- **Only use on systems you own or have permission to modify**
- **Be aware that delaying network traffic may affect system stability**
- **Do not use in production environments without proper testing**
- **Understand that network delays can affect time-sensitive applications**

## Troubleshooting

### "Failed to open WinDivert handle"

**Problem**: Application cannot start the delay engine

**Solutions**:
1. Ensure you are running as Administrator
2. Check if antivirus is blocking WinDivert
3. Verify Windows Defender hasn't quarantined WinDivert files
4. Try restarting your computer

### Antivirus Interference

**Problem**: Antivirus software blocks or removes WinDivert files

**Solutions**:
1. Add an exception for the Ping Delayer installation directory
2. Temporarily disable real-time protection while using (re-enable after)
3. Whitelist `WinDivert.dll`, `WinDivert64.sys`, and `PingDelayer.exe`

### High Packet Queue / Application Unresponsive

**Problem**: Packet queue grows very large, system becomes slow

**Solutions**:
1. Stop the delay immediately
2. Reduce the delay amount
3. Close bandwidth-intensive applications
4. Check for system resource constraints

### Driver Installation Issues

**Problem**: WinDivert driver fails to load

**Solutions**:
1. Ensure Secure Boot is not blocking unsigned drivers (WinDivert is signed)
2. Check Windows Event Viewer for driver installation errors
3. Try running with test signing enabled: `bcdedit /set testsigning on` (requires restart)
4. Verify your Windows version is supported

### Application Crashes on Startup

**Problem**: Application crashes when clicking Start

**Solutions**:
1. Ensure .NET 8.0 Runtime is installed
2. Check Windows Event Viewer for error details
3. Verify all DLL dependencies are present
4. Try rebuilding from source

### Network Disconnects

**Problem**: Network connection is lost when using the application

**Solutions**:
1. Stop the delay immediately
2. This may indicate system resource exhaustion
3. Try lower delay values
4. Ensure your network adapter drivers are up to date

## Known Limitations

- **Windows Only**: WinDivert only works on Windows
- **Administrator Required**: Cannot function without administrator privileges
- **All Traffic Affected**: Delays all network traffic, not just specific applications
- **High CPU at High Throughput**: Processing many packets per second uses CPU resources
- **No Protocol Filtering**: Currently delays all packets without filtering by protocol/port

## Future Enhancements

Potential improvements for future versions:

- [ ] Port/protocol filtering options
- [ ] Inbound vs. outbound traffic selection
- [ ] Traffic statistics and graphs
- [ ] Packet loss simulation
- [ ] Bandwidth throttling
- [ ] Per-application filtering
- [ ] Configurable jitter amount
- [ ] Save/load delay profiles

## License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.

## Acknowledgments

- [WinDivert](https://reqrypt.org/windivert.html) by Basil Projects for packet interception
- [WindivertDotnet](https://github.com/TechnikEmpire/WinDivertSharp) for the .NET wrapper
- Inspired by [Clumsy](https://github.com/jagt/clumsy) but with focus on low jitter

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## Support

For issues, questions, or suggestions:
- Open an issue on [GitHub](https://github.com/huttala/ping-delayer/issues)
- Check the Troubleshooting section above
- Review WinDivert documentation for driver-related issues

## Disclaimer

This tool is provided for testing and development purposes only. Use responsibly and only on systems you own or have permission to modify. The authors are not responsible for any misuse or damage caused by this software.
