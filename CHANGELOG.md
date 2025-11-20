# HydroRust Plugin Suite - Changelog

## Version 5.1.0 (2025-11-20)

### New Features

#### 1. Player Counter in HUD
- **Description**: Shows the number of active race participants in the HUD
- **Display Format**: `Pos 1/4 [Players: 4]` during races
- **Updates**: Automatically refreshes every tick (0.1s)
- **Location**: Main HUD top bar, integrated into position display

#### 2. Queue Counter in Player Panel
- **Description**: Displays number of players waiting in auto-race queue
- **Display Format**: `Queue: X` where X is the queue count
- **Updates**: Only when queue count changes (efficient)
- **Location**: Top-right corner of player quick panel
- **Visibility**: Only shows when queue count > 0

#### 3. Mode Voting Overlay
- **Description**: Large centered UI for voting between Normal and Battle race modes
- **Activation**: Automatically appears during `RaceState.Voting`
- **Features**:
  - Two large buttons: "Normal" and "Battle"
  - Shows remaining vote time
  - Semi-transparent backdrop
  - Directly wired to `/hydro vote normal` and `/hydro vote battle` commands
- **Configuration**: `ShowVotingOverlay` (default: true)
- **Appearance**: Center screen (35-65% width, 40-60% height)

#### 4. Countdown Overlay
- **Description**: Large countdown numbers before race start
- **Display**: Shows "10", "9", "8", ..., "3", "2", "1", "GO!"
- **Font Size**: 80pt for maximum visibility
- **Colors**: 
  - Numbers: Theme Primary color
  - "GO!": Theme Good color (green)
- **Configuration**: `ShowCountdownOverlay` (default: true)
- **Timing**: Updates every second, hides when race begins

### Technical Changes

#### HydroRust.cs
- Extended `UI_GetHudData()` API with two new fields:
  - `CountdownSecondsRemaining` (int): Returns seconds remaining during countdown, -1 otherwise
  - `QueuedPlayers` (int): Returns current race queue size
- Added proper timer cleanup in `Unload()`:
  - `CountdownTimer`
  - `VoteTimer`
  - `_pendingDelayedStart`
- Race state now properly exposed for countdown phase

#### HydroUI.cs
- **Fixed** `HasUi()` method (was always returning false)
- New UI elements:
  - `ROOT_VOTING = "HydroUI.VotingOverlay"`
  - `ROOT_COUNTDOWN = "HydroUI.CountdownOverlay"`
- Enhanced `HudData` structure with:
  - `CountdownSecondsRemaining`
  - `QueuedPlayers`
- New overlay management methods:
  - `ShowVotingOverlay()`
  - `HideVotingOverlay()`
  - `ShowCountdownOverlay()`
  - `HideCountdownOverlay()`
- Smart refresh for player panel (only updates when queue changes)
- Updated `HudTick()` to manage overlay visibility
- Config options added:
  - `ShowVotingOverlay` (bool, default: true)
  - `ShowCountdownOverlay` (bool, default: true)

#### HydroLobby.cs
- No changes (existing APIs sufficient)

### Bug Fixes
- Fixed `HasUi()` always returning false
- Added missing timer cleanup preventing memory leaks
- Improved null-safety in cross-plugin calls
- Fixed overlay cleanup in `HideAll()` method

### Performance Improvements
- Player panel only refreshes when queue count changes
- Countdown overlay only redraws when second value changes
- No CUI spam - maintains efficient update patterns
- Optimized overlay show/hide logic

### Configuration

New configuration options in `HydroUI.json`:

```json
{
  "ShowVotingOverlay": true,
  "ShowCountdownOverlay": true
}
```

### API Changes

**HydroRust.UI_GetHudData() - New Fields:**
```csharp
dict["CountdownSecondsRemaining"] = -1 or 0+;  // -1 when not counting
dict["QueuedPlayers"] = raceQueue.Count;       // Current queue size
```

**No Breaking Changes**: All existing API methods remain unchanged and backwards compatible.

### Testing Recommendations

1. **Single Player Testing**:
   - Join lobby and verify HUD appears
   - Check queue counter in player panel
   - Start a race and verify player counter
   - Test countdown display (10...3...2...1...GO!)

2. **Multi-Player Testing**:
   - Queue multiple players
   - Verify queue count updates for all
   - Test mode voting overlay
   - Verify voting buttons work
   - Check countdown synchronization

3. **Configuration Testing**:
   - Disable `ShowVotingOverlay` and verify no voting UI
   - Disable `ShowCountdownOverlay` and verify no countdown
   - Test with different themes

4. **Performance Testing**:
   - Monitor with 10+ players
   - Check for CUI spam in console
   - Verify smooth transitions
   - Test plugin unload/reload

### Known Issues
None at this time.

### Migration Notes
- No migration required
- Configs auto-generate new fields with defaults
- Fully backwards compatible
- Existing data files unchanged

### Credits
- Plugin Suite: belisario-afk
- Enhancements: belisario-afk + GitHub Copilot
- Framework: Oxide/uMod for Rust
