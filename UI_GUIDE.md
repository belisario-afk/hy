# HydroRust UI Features - Visual Guide

## Overview
This document describes the visual appearance and behavior of new UI features in HydroRust plugin suite v5.1.0.

## 1. Player Counter in HUD

### Location
- **Top center of screen**
- Part of main HUD bar (0.23-0.77 horizontal, 0.92-0.99 vertical)

### Appearance
```
┌─────────────────────────────────────────────────┐
│ Track1 [Race]  CP 2/5  Pos 1/4 [Players: 4]    │
└─────────────────────────────────────────────────┘
```

### Behavior
- Shows during active races
- Updates every 0.1 seconds
- Format: `Pos X/Y [Players: Y]`
  - X = Player's current position
  - Y = Total players in race

### States
- **Racing**: `Pos 1/4 [Players: 4]`
- **Time Trial**: `TIME` (no player count)
- **Not Racing**: Empty or speed only

## 2. Queue Counter in Player Panel

### Location
- **Bottom right corner of screen**
- Player quick panel (0.84-0.995 horizontal, 0.02-0.16 vertical)
- Top-right corner of panel

### Appearance
```
┌────────────────┐
│   Queue: 3     │  ← Counter appears here
├────────────────┤
│  [Join Race]   │
│  [Leave Race]  │
├────────────────┤
│[Stats][Normal] │
│      [Battle]  │
└────────────────┘
```

### Behavior
- Only visible when queue count > 0
- Updates when queue changes
- Small text (11pt), accent color
- Position: top-right corner of player panel

### States
- **Empty Queue**: No text shown
- **Players Queued**: `Queue: N` where N is player count

## 3. Voting Overlay

### Location
- **Center of screen**
- Modal overlay (35-65% width, 40-60% height)

### Appearance
```
╔═══════════════════════════════════════════╗
║     Semi-transparent dark backdrop        ║
║                                           ║
║  ┌─────────────────────────────────────┐ ║
║  │   VOTE FOR RACE MODE                │ ║
║  ├─────────────────────────────────────┤ ║
║  │                                     │ ║
║  │  ┌─────────┐       ┌──────────┐   │ ║
║  │  │ NORMAL  │       │  BATTLE  │   │ ║
║  │  │ (Green) │       │  (Red)   │   │ ║
║  │  └─────────┘       └──────────┘   │ ║
║  │                                     │ ║
║  │      10s remaining                  │ ║
║  └─────────────────────────────────────┘ ║
║                                           ║
╚═══════════════════════════════════════════╝
```

### Visual Details
- **Background**: Semi-transparent black (0 0 0 0.75)
- **Panel**: Theme panel color
- **Title**: 20pt, white, centered
- **Buttons**: 
  - Normal: Green background, 18pt text
  - Battle: Red background, 18pt text
- **Timer**: 14pt, gray text, bottom center

### Behavior
- Appears instantly during voting phase
- Blocks game view (intentional for voting)
- Buttons are clickable
- Auto-dismisses after vote or timeout
- Smooth appearance/disappearance

### Button Actions
- **NORMAL**: Executes `/hydro vote normal`
- **BATTLE**: Executes `/hydro vote battle`

## 4. Countdown Overlay

### Location
- **Center of screen**
- No backdrop (transparent for visibility)
- 40-60% width, 45-55% height

### Appearance (Numbers)
```
        ╔═══════════╗
        ║           ║
        ║     10    ║  ← 80pt, Primary color
        ║           ║
        ╚═══════════╝
```

### Appearance (GO!)
```
        ╔═══════════╗
        ║           ║
        ║    GO!    ║  ← 80pt, Green color
        ║           ║
        ╚═══════════╝
```

### Sequence
1. **10** (1 second)
2. **9** (1 second)
3. **8** (1 second)
4. **7** (1 second)
5. **6** (1 second)
6. **5** (1 second)
7. **4** (1 second)
8. **3** (1 second)
9. **2** (1 second)
10. **1** (1 second)
11. **GO!** (brief, then disappears)

### Visual Details
- **Font Size**: 80pt (very large)
- **Color (Numbers)**: Theme Primary (blue)
- **Color (GO!)**: Theme Good (green)
- **Alignment**: Center
- **Background**: Transparent (no panel)
- **Updates**: Every 1 second

### Behavior
- Appears after voting phase completes
- Counts down from configured value (default 10)
- Changes each second
- "GO!" appears at 0
- Disappears when race starts
- Non-intrusive (transparent bg)

## UI State Flow

```
┌─────────────────┐
│  Player Joins   │
│     Server      │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Welcome Screen  │ ← Locked until lobby join
│   (Fullscreen)  │
└────────┬────────┘
         │ Click "Join Lobby"
         ▼
┌─────────────────┐
│  Lobby Spawn    │
│  + HUD Visible  │ ← HUD appears
│  + Player Panel │ ← Panel appears
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  Queue Counter  │ ← Shows when queued
│    Updates      │
└────────┬────────┘
         │ Race Starts
         ▼
┌─────────────────┐
│ Voting Overlay  │ ← Shows during voting
│   (Centered)    │
└────────┬────────┘
         │ Vote Complete
         ▼
┌─────────────────┐
│Countdown Overlay│ ← Shows during countdown
│ 10...9...8...1  │
└────────┬────────┘
         │ "GO!"
         ▼
┌─────────────────┐
│  Race Running   │
│ Player Counter  │ ← Shows in HUD
│    Updates      │
└─────────────────┘
```

## Color Scheme (Default Theme: HydroBlue)

```
Primary:     0.0  0.75 1.0  0.95  (Cyan Blue)
Accent:      0.2  0.9  1.0  0.95  (Light Cyan)
Background:  0.02 0.05 0.08 0.92  (Dark Blue-Gray)
Panel:       0.07 0.10 0.15 0.96  (Medium Blue-Gray)
Text:        1.0  1.0  1.0  1.0   (White)
MutedText:   0.85 0.92 1.0  0.90  (Light Blue-White)
Progress:    0.0  0.75 1.0  0.95  (Cyan Blue)
Boost:       1.0  0.7  0.1  0.95  (Orange)
Alert:       1.0  0.25 0.25 0.95  (Red)
Good:        0.25 1.0  0.4  0.95  (Green)
Battle:      1.0  0.3  0.3  0.95  (Red)
```

## Responsive Behavior

### Screen Resolutions
- Tested: 1920x1080, 1280x720
- UI scales with anchors (percentage-based)
- Works on ultrawide monitors
- May need adjustment for 4:3 ratios

### Performance
- HUD: 10 updates/second (0.1s tick)
- Overlays: Update only on change
- No frame drops expected
- Minimal CPU usage

## Accessibility

### Visibility
- Large fonts for countdown (80pt)
- High contrast colors
- Clear button labels
- Semi-transparent backdrops

### Usability
- Click targets: Large buttons
- Feedback: Immediate on click
- Clear visual hierarchy
- Consistent positioning

## Configuration Impact

### ShowVotingOverlay = false
- Voting overlay hidden
- Players must use chat: `/hydro vote normal|battle`
- HUD still shows `[Voting]` tag

### ShowCountdownOverlay = false
- Countdown overlay hidden
- Countdown still happens (race starts on time)
- Chat messages still broadcast countdown

## Integration with Existing UI

### Does Not Conflict With:
- Welcome screen
- Admin track editor
- Player quick panel
- Menu toggle button
- HUD elements

### Complements:
- Mode tag in HUD (`[Voting]`, `[Battle]`, `[Race]`)
- Position display
- Progress bars
- Speed meter

## Future Enhancements (Not in v5.1.0)
- Customizable countdown sounds
- Vote result visualization
- Player avatar display in queue
- Animated transitions
- More theme options
