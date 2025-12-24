# Ping Delayer - User Interface Guide

## Main Window Overview

The Ping Delayer application features a clean, single-window interface designed for ease of use.

### Window Layout

```
┌──────────────────────────────────────────────────────────────┐
│  Ping Delayer - Network Latency Simulator          [_][□][X] │
├──────────────────────────────────────────────────────────────┤
│                                                                │
│  ┌─ Status ────────────────────────────────────────────────┐ │
│  │  Status: ● Inactive                                     │ │
│  │  Queued Packets: 0                                      │ │
│  └──────────────────────────────────────────────────────────┘ │
│                                                                │
│  ┌─ Delay Settings ─────────────────────────────────────────┐ │
│  │  Current Delay: 100 ms                                  │ │
│  │                                                          │ │
│  │  Delay Amount (0-1000 ms):                              │ │
│  │  [━━━━━━━━━━━●━━━━━━━━━━━━━━━━━━━━━━━━━━]  [100   ]     │ │
│  │  0 ms         500 ms        1000 ms                     │ │
│  └──────────────────────────────────────────────────────────┘ │
│                                                                │
│  ┌─ Control ────────────────────────────────────────────────┐ │
│  │            [  Start Delay  ]  [  Stop Delay  ]          │ │
│  └──────────────────────────────────────────────────────────┘ │
│                                                                │
│  ┌─ Information ────────────────────────────────────────────┐ │
│  │  • This application adds configurable delay to network  │ │
│  │  • Uses WinDivert for packet interception               │ │
│  │  • Minimal jitter design for consistent delay           │ │
│  │  • Must run as Administrator (WinDivert requirement)    │ │
│  └──────────────────────────────────────────────────────────┘ │
│                                                                │
│  ┌─ Messages ───────────────────────────────────────────────┐ │
│  │ [02:15:30] Application started. Ready to add delay.     │ │
│  │ [02:15:30] Note: Must run as Administrator.             │ │
│  │                                                          │ │
│  │                                                          │ │
│  │                                                          │ │
│  └──────────────────────────────────────────────────────────┘ │
│                                                                │
│  Ping Delayer v1.0 | Low-Jitter Network Delay Simulator      │
└──────────────────────────────────────────────────────────────┘
```

## UI Components

### Status Section

**Location**: Top of window

**Components**:
1. **Status Indicator**:
   - Red circle (●) when Inactive
   - Green circle (●) when Active
   - Bold text showing current state

2. **Queued Packets Counter**:
   - Shows number of packets currently delayed
   - Updates in real-time (every 100ms)
   - Helps monitor system load

### Delay Settings Section

**Location**: Upper-middle of window

**Components**:
1. **Current Delay Display**:
   - Large, bold text showing active delay
   - Format: "100 ms"
   - Blue color for visibility

2. **Delay Slider**:
   - Range: 0 to 1000 milliseconds
   - Tick marks every 50ms
   - Snaps to nearest tick
   - Smooth dragging

3. **Delay Text Box**:
   - Accepts numeric input only
   - Updates slider when changed
   - Clamps values to 0-1000 range

4. **Scale Labels**:
   - Shows 0 ms, 500 ms, 1000 ms markers
   - Helps with visual estimation

### Control Section

**Location**: Middle of window

**Components**:
1. **Start Delay Button**:
   - Light green background
   - Enabled when engine is stopped
   - Disabled when engine is running
   - Starts packet interception

2. **Stop Delay Button**:
   - Light red/coral background
   - Disabled when engine is stopped
   - Enabled when engine is running
   - Stops packet interception

### Information Section

**Location**: Middle-lower of window

**Content**:
- Bullet-point list of key features
- Brief description of capabilities
- Administrator requirement notice
- Non-editable text

### Messages Section

**Location**: Lower portion of window (expandable)

**Components**:
1. **Log Text Box**:
   - Read-only console-style log
   - Monospace font (Consolas)
   - Timestamped messages
   - Auto-scrolls to bottom
   - White/light gray background

**Message Types**:
- Informational (normal text)
- Errors (prefixed with "ERROR:")
- Status changes (engine start/stop)

### Footer

**Location**: Bottom of window

**Content**:
- Application name and version
- Brief tagline
- Centered, small gray text

## Color Scheme

### Status Indicators
- **Inactive**: Red (#FF0000)
- **Active**: Green (#00FF00)

### Buttons
- **Start**: Light Green (#90EE90)
- **Stop**: Light Coral (#F08080)
- **Hover**: Slightly darker shade
- **Disabled**: Gray

### Text
- **Headers**: Bold, default color
- **Labels**: Normal weight
- **Values**: Bold or colored for emphasis
- **Footer**: Light gray

### Background
- **Window**: White/default
- **GroupBoxes**: Light border
- **Log**: WhiteSmoke (#F5F5F5)

## User Interactions

### Setting the Delay

**Method 1: Slider**
1. Click and drag slider thumb
2. Slider snaps to 50ms increments
3. Text box updates automatically
4. If engine is running, delay updates immediately

**Method 2: Text Input**
1. Click in text box
2. Type desired value (0-1000)
3. Press Enter or click outside box
4. Slider updates automatically
5. If engine is running, delay updates immediately

### Starting the Delay

1. Set desired delay using slider or text box
2. Click "Start Delay" button
3. Status indicator turns green
4. "Start Delay" button becomes disabled
5. "Stop Delay" button becomes enabled
6. Messages log shows "Engine started with Xms delay"
7. Queued Packets counter begins updating

### Stopping the Delay

1. Click "Stop Delay" button
2. Engine stops intercepting packets
3. Remaining queued packets are released
4. Status indicator turns red
5. "Stop Delay" button becomes disabled
6. "Start Delay" button becomes enabled
7. Messages log shows "Engine stopped"
8. Queued Packets counter returns to 0

### Adjusting Delay While Running

1. Engine must be running (status = Active)
2. Move slider or enter new value
3. Delay updates without stopping engine
4. Messages log shows "Delay updated to Xms"
5. New delay applies to newly captured packets
6. Existing queued packets maintain original delay

### Monitoring Status

**Watch the Status Indicator**:
- Red = Not affecting network
- Green = Actively delaying packets

**Watch Queued Packets**:
- 0 = No packets currently delayed
- 1-100 = Normal range
- 100+ = High load, consider lowering delay
- 1000+ = Very high load, may impact system

**Read Messages Log**:
- Timestamps show when events occurred
- "ERROR:" prefix indicates problems
- Scroll up to see history

## Keyboard Shortcuts

Currently, the application does not implement custom keyboard shortcuts. Standard Windows shortcuts apply:

- **Alt+F4**: Close application
- **Tab**: Navigate between controls
- **Space**: Activate focused button
- **Arrow Keys**: Adjust slider (when focused)

## Window Behavior

- **Minimize**: Allowed (engine continues running)
- **Resize**: Fixed size (CanMinimize only)
- **Close**: Prompts shutdown of engine if running
- **Always On Top**: Not implemented (can be added)

## Accessibility

- **Keyboard Navigation**: Full keyboard support via Tab
- **Screen Readers**: Labels associated with controls
- **High Contrast**: Respects Windows theme
- **Font Scaling**: Supports Windows DPI settings

## Tips for Best User Experience

1. **Start with low delays**: Begin with 20-50ms to verify it works
2. **Monitor packet queue**: Keep an eye on the counter
3. **Use slider for quick adjustments**: Fast and visual
4. **Use text box for precise values**: Exact millisecond control
5. **Watch the log**: It provides helpful feedback
6. **Stop when done**: Always stop before closing

## Future UI Enhancements

Potential improvements for future versions:

- [ ] Dark mode support
- [ ] Minimize to system tray
- [ ] Keyboard shortcuts (Ctrl+S to start, Ctrl+X to stop)
- [ ] Traffic statistics graphs
- [ ] Filter configuration panel
- [ ] Profile management (save/load presets)
- [ ] Collapsible sections
- [ ] Always-on-top option
