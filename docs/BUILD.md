# Building Ping Delayer

## Prerequisites

- Windows 10 or Windows 11 (64-bit)
- Visual Studio 2022 with:
  - .NET desktop development workload
  - .NET 8.0 SDK
- OR .NET 8.0 SDK (command line build)

## Option 1: Visual Studio 2022

1. Open `PingDelayer.sln` in Visual Studio 2022

2. Select Release configuration:
   - Build → Configuration Manager
   - Active solution configuration: Release

3. Build the solution:
   - Build → Build Solution (Ctrl+Shift+B)

4. The executable will be located at:
   ```
   src/PingDelayer/bin/Release/net8.0-windows/PingDelayer.exe
   ```

## Option 2: .NET CLI

1. Open a command prompt or PowerShell

2. Navigate to the project directory:
   ```bash
   cd src/PingDelayer
   ```

3. Restore dependencies:
   ```bash
   dotnet restore
   ```

4. Build in Release mode:
   ```bash
   dotnet build -c Release
   ```

5. The executable will be located at:
   ```
   bin/Release/net8.0-windows/PingDelayer.exe
   ```

## Option 3: Build from Repository Root

From the repository root directory:

```bash
dotnet build -c Release
```

Output: `src/PingDelayer/bin/Release/net8.0-windows/PingDelayer.exe`

## Publishing a Standalone Executable

To create a single-file executable that doesn't require .NET Runtime:

```bash
cd src/PingDelayer
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Output: `bin/Release/net8.0-windows/win-x64/publish/PingDelayer.exe`

Note: This will be a larger file (~80MB) but includes the .NET runtime.

## Troubleshooting Build Issues

### Missing .NET 8.0 SDK

**Error**: `The current .NET SDK does not support targeting .NET 8.0`

**Solution**: Download and install .NET 8.0 SDK from https://dotnet.microsoft.com/download

### Missing Windows SDK

**Error**: `Windows SDK not found`

**Solution**: Install Visual Studio 2022 with Windows SDK component

### NuGet Package Restore Failed

**Error**: `Unable to find package WindivertDotnet`

**Solution**:
```bash
dotnet nuget add source https://api.nuget.org/v3/index.json
dotnet restore
```

### Build Fails on Non-Windows Platform

**Note**: This is a Windows-only application and cannot be built or run on Linux/macOS. The `EnableWindowsTargeting` flag allows the project to restore packages on non-Windows platforms for CI/CD purposes, but the executable will only run on Windows.

## Next Steps

After building, see [docs/README.md](../docs/README.md) for usage instructions.

Remember: The application must be run as Administrator for WinDivert to function.
