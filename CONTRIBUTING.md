# Contributing to Ping Delayer

Thank you for your interest in contributing to Ping Delayer! This document provides guidelines for contributing to the project.

## How to Contribute

### Reporting Bugs

If you find a bug, please create an issue with:

- **Clear title**: Brief description of the issue
- **Environment**: Windows version, .NET version, etc.
- **Steps to reproduce**: Detailed steps to reproduce the issue
- **Expected behavior**: What you expected to happen
- **Actual behavior**: What actually happened
- **Screenshots**: If applicable
- **Logs**: Any error messages from the application

### Suggesting Features

Feature suggestions are welcome! Please create an issue with:

- **Use case**: Describe why this feature would be useful
- **Proposed solution**: How you envision the feature working
- **Alternatives**: Other ways you've considered solving the problem

### Pull Requests

1. **Fork the repository**

2. **Create a feature branch**:
   ```bash
   git checkout -b feature/your-feature-name
   ```

3. **Make your changes**:
   - Follow the existing code style
   - Add comments for complex logic
   - Update documentation if needed

4. **Test your changes**:
   - Build the project successfully
   - Test on Windows 10/11
   - Verify the application runs as Administrator
   - Test with various delay values

5. **Commit your changes**:
   ```bash
   git commit -m "Add feature: your feature description"
   ```

6. **Push to your fork**:
   ```bash
   git push origin feature/your-feature-name
   ```

7. **Create a Pull Request**:
   - Provide a clear description of the changes
   - Reference any related issues
   - Include screenshots for UI changes

## Code Style Guidelines

### C# Conventions

- Follow Microsoft's C# coding conventions
- Use meaningful variable and method names
- Add XML documentation comments for public APIs
- Use `PascalCase` for class and method names
- Use `camelCase` for local variables and parameters
- Use `_camelCase` for private fields (if needed)

### XAML Conventions

- Use consistent indentation (4 spaces)
- Group related properties together
- Use meaningful names for UI elements (x:Name)
- Extract repeated styles to resources

### Code Organization

- One class per file
- Keep methods focused and short
- Extract complex logic into separate methods
- Use regions sparingly (only for large classes)

## Testing Guidelines

Since this is a system-level networking tool, testing can be challenging:

1. **Manual Testing**:
   - Test with various delay values (0ms, 50ms, 100ms, 500ms, 1000ms)
   - Monitor packet queue size
   - Test start/stop functionality
   - Verify clean shutdown
   - Test delay updates while running

2. **Performance Testing**:
   - Monitor CPU usage under high load
   - Check memory usage over time
   - Test with various network activities (browsing, gaming, downloading)

3. **Edge Cases**:
   - Very high packet rates
   - Very long delays (500ms+)
   - Quick start/stop cycles
   - System under heavy load

## Documentation

When adding features, please update:

- `README.md` - If it affects basic usage
- `docs/README.md` - For detailed documentation
- `docs/BUILD.md` - If build process changes
- XML comments in code - For API documentation

## Project Structure

```
ping-delayer/
├── src/
│   └── PingDelayer/          # Main WPF application
│       ├── MainWindow.xaml   # UI definition
│       ├── NetworkDelayEngine.cs  # Core packet delay logic
│       ├── PacketQueue.cs    # Priority queue for packets
│       └── HighResolutionTimer.cs # Timing utilities
├── docs/
│   ├── README.md             # Main documentation
│   └── BUILD.md              # Build instructions
└── README.md                 # Project overview
```

## Commit Message Guidelines

Write clear commit messages:

- Use present tense ("Add feature" not "Added feature")
- Keep first line under 50 characters
- Add detailed description after blank line if needed
- Reference issues with #issue-number

Examples:
```
Add port filtering feature

Implement UI controls and logic to filter packets by port number.
This allows users to delay only specific network services.

Closes #42
```

## Questions?

If you have questions about contributing:

- Check existing issues and discussions
- Create a new issue with the "question" label
- Be specific about what you need help with

## License

By contributing to Ping Delayer, you agree that your contributions will be licensed under the MIT License.

Thank you for contributing! 🎉
