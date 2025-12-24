# Ping Delayer

A Windows 11 desktop application that simulates network latency with **minimal jitter**. Add a configurable delay to all network traffic for testing applications under high-latency conditions.

## Quick Start

1. **Build**: Requires .NET 8.0 SDK and Visual Studio 2022
   ```bash
   cd src/PingDelayer
   dotnet build -c Release
   ```

2. **Run**: Must run as **Administrator**
   ```bash
   # Right-click PingDelayer.exe → Run as administrator
   ```

3. **Use**: Set delay with slider, click "Start Delay"

## Key Features

- **Low-Jitter Design**: Consistent delay without variance
- **High-Resolution Timers**: ~100 nanosecond precision
- **Priority Queue**: Proper packet ordering
- **Simple WPF UI**: Easy to use
- **Real-time Updates**: Change delay on-the-fly

## Documentation

See [docs/README.md](docs/README.md) for:
- Detailed build instructions
- Usage guide
- How WinDivert works
- Security considerations
- Troubleshooting

## Requirements

- Windows 10/11 (64-bit)
- .NET 8.0 Runtime
- Administrator privileges
- WinDivert (included via NuGet)

## License

MIT License - see [LICENSE](LICENSE) file for details.
