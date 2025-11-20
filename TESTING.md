# HydroRust Plugin Suite - Testing Guide

## Prerequisites
- Rust server with Oxide/uMod installed
- ImageLibrary plugin (optional, for textures)
- At least 2 test players recommended

## Installation
1. Copy `HydroRust.cs`, `HydroUI.cs`, and `HydroLobby.cs` to `oxide/plugins/`
2. Server will auto-compile and load plugins
3. Check console for any compilation errors
4. Verify plugins loaded: `oxide.plugins` in server console

## Quick Test Procedure

### 1. Initial Setup (Admin)
```
/hydrolobby set          # Set lobby spawn at your current position
/hydro track.new Test1   # Create a test track
/hydro track.setstart    # Set race start position
/hydro track.setfinish   # Set finish position (30m+ away)
/hydro track.addcp.aim 10 -1  # Add a checkpoint
/hydro track.save        # Save the track
```

### 2. Player Experience Test
1. **Connect to server**
   - Welcome screen should appear
   - Click "Play" tab
   - Click "Join Lobby" button
   - Should teleport to lobby
   - HUD should appear at top of screen
   - Player panel should appear at bottom right

2. **Queue Test**
   - Stand near lobby (within 20m)
   - Check player panel - should show "Queue: 1" (or more if others queued)
   - Queue counter should update automatically when others join/leave

3. **Race Start Test**
   - Wait for auto-race to start (or admin: `/hydro race.start Test1`)
   - **Voting Phase**: Centered voting overlay should appear
     - Two buttons: "Normal" and "Battle"
     - Shows remaining vote time
     - Click one to vote
     - Overlay disappears after voting or timeout
   
4. **Countdown Test**
   - After voting completes: Large countdown should appear
   - Shows: "10" ... "9" ... "8" etc.
   - Each number shown for 1 second
   - Final display: "GO!" in green
   - Countdown disappears when race starts

5. **During Race**
   - HUD shows: "Pos X/Y [Players: Y]"
   - Player counter shows total racers
   - All stats update smoothly

### 3. Configuration Test
As admin, test config toggles:

```
# Edit oxide/config/HydroUI.json
"ShowVotingOverlay": false   # Voting UI will not appear
"ShowCountdownOverlay": false # Countdown will not appear

# Reload plugin
oxide.reload HydroUI

# Test again - overlays should be disabled
```

### 4. Multi-Player Test
With 2+ players:
- Both join lobby
- Check queue counter for both
- Start race
- Both should see voting overlay
- Both should see countdown
- Verify synchronization

## What to Look For

### ✅ Success Indicators
- [ ] Welcome screen displays on connect
- [ ] Lobby teleport works
- [ ] HUD appears after joining lobby
- [ ] Queue counter shows and updates
- [ ] Player counter in HUD shows correct number
- [ ] Voting overlay appears centered
- [ ] Voting buttons work (votes registered)
- [ ] Countdown appears with large numbers
- [ ] Countdown counts down correctly
- [ ] "GO!" appears at race start
- [ ] All UI disappears/hides appropriately
- [ ] No console errors
- [ ] Smooth performance (no lag)

### ❌ Failure Indicators
- Console errors mentioning HydroUI or HydroRust
- UI elements not appearing
- UI stuck on screen after race
- Countdown not updating
- Queue counter not updating
- CUI spam in console
- Performance degradation
- Voting not registering
- Crashes on plugin reload

## Common Issues & Solutions

### Issue: HUD not showing
- **Solution**: Make sure you clicked "Join Lobby" in welcome screen
- **Check**: `st.HudVisible` should be true after lobby join

### Issue: Overlays not appearing
- **Check Config**: Verify `ShowVotingOverlay` and `ShowCountdownOverlay` are `true`
- **Check State**: Voting needs active race in Voting state
- **Check Console**: Look for any errors

### Issue: Voting buttons not working
- **Check Permissions**: User needs `hydrorust.player` permission
- **Check Race**: Must be in active race and voting phase
- **Test Direct**: Try `/hydro vote normal` in chat

### Issue: Countdown stuck or not counting
- **Check**: `CountdownSecondsRemaining` in HUD data should be decreasing
- **Verify**: Race state should be `Countdown`
- **Reload**: `oxide.reload HydroRust` to reset state

### Issue: Queue counter not updating
- **Wait**: Updates happen when queue actually changes
- **Check**: Stand within 20m of lobby position
- **Verify**: `/hydrolobby info` shows correct lobby position

## Performance Testing

### Monitor Console Output
```
# Watch for excessive messages like:
CuiHelper.AddUi called repeatedly (CUI spam)
NullReferenceException (null safety issues)
Timer errors (cleanup issues)
```

### Check Memory
- Monitor server RAM usage
- Watch for gradual increase (memory leak)
- Test plugin reload multiple times

### Stress Test
- 10+ players joining simultaneously
- Rapid race starts
- Multiple plugin reloads
- Queue players quickly

## Admin Commands Quick Reference

```bash
# Lobby
/hydrolobby set              # Set lobby location
/hydrolobby go               # Teleport to lobby

# Track Editor
/hydro track.new <name>      # Create track
/hydro track.select <name>   # Select track
/hydro track.save            # Save changes
/hydro track.setstart        # Set start at aim
/hydro track.setfinish       # Set finish at aim
/hydro track.addcp.aim 10 -1 # Add checkpoint
/hydro track.laps <n>        # Set lap count

# Race Control
/hydro race.start <track>    # Manual start
/hydro race.join             # Join race
/hydro race.leave            # Leave race

# Voting
/hydro vote normal           # Vote for normal mode
/hydro vote battle           # Vote for battle mode
```

## Debugging Tips

1. **Enable verbose logging**: Check Oxide config
2. **Monitor F1 console**: Look for errors in real-time
3. **Test incrementally**: One feature at a time
4. **Use single player first**: Easier to isolate issues
5. **Check plugin load order**: HydroRust → HydroUI → HydroLobby
6. **Verify dependencies**: All three plugins must be loaded

## Expected Performance
- HUD update: 10 FPS (0.1s interval)
- No noticeable lag with 10 players
- Overlay transitions: Instant
- Queue updates: Real-time
- Memory stable after 1 hour runtime

## Reporting Issues
If you find bugs, please report:
1. Console error messages (full stack trace)
2. Steps to reproduce
3. Number of players
4. Server specs
5. Plugin versions
6. Config files (sanitized)
