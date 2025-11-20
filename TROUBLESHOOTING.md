# HydroRust Plugin Suite - Troubleshooting Guide

## Voting Overlay Not Appearing

If the voting overlay doesn't appear when races start, follow these steps:

### 1. Verify Battle Mode Configuration

**File**: `oxide/config/HydroRust.json`

Ensure this setting is enabled:
```json
{
  "BattleRaceEnabled": true,
  "BattleRaceVoteDurationSeconds": 10
}
```

**Why**: If `BattleRaceEnabled` is `false`, the voting phase is skipped entirely and races go straight to countdown.

### 2. Verify HydroUI Overlay Configuration

**File**: `oxide/config/HydroUI.json`

Ensure these settings are enabled:
```json
{
  "ShowVotingOverlay": true,
  "ShowCountdownOverlay": true
}
```

### 3. Verify Plugins Are Loaded

Run in console:
```
oxide.plugins
```

Should show:
- HydroRust
- HydroUI  
- HydroLobby

### 4. Check Player Is a Race Participant

The voting and countdown overlays **only appear for players who are participants in the active race**.

**To become a participant:**
1. Join the lobby via welcome screen
2. Either:
   - Use `/hydro race.join` command after an admin stages a race
   - Join the race queue and wait for auto-race

**To verify participation** (console):
```
hydro.status
```

### 5. Enable Debug Logging

**Temporary Debug Code** (add to HydroUI.cs in `HudTick` method):

```csharp
// Debug voting overlay
if (st.Display.Voting)
{
    Puts($"[DEBUG] Player {player.displayName} - Voting={st.Display.Voting}, OverlayVisible={st.VotingOverlayVisible}");
}

// Debug countdown overlay  
if (st.Display.CountdownSecondsRemaining > 0)
{
    Puts($"[DEBUG] Player {player.displayName} - Countdown={st.Display.CountdownSecondsRemaining}, OverlayVisible={st.CountdownOverlayVisible}");
}
```

Check console logs to see if the values are being received.

### 6. Manual Test Procedure

**Step-by-step test:**

1. **Setup** (admin):
   ```
   hydro.track.create TestTrack
   hydro.race.stage TestTrack
   ```

2. **Join race** (player):
   ```
   /hydro race.join
   ```

3. **Expected behavior**:
   - If `BattleRaceEnabled=true`: Voting overlay appears in center
   - After voting timer expires: Countdown overlay appears (10→1→GO!)
   - Race starts

4. **Check console** for any CUI errors

### 7. Common Issues

**Issue**: "No voting overlay appears"
- **Check**: Is `BattleRaceEnabled` set to `true`?
- **Check**: Is player actually in the race? (not just in lobby)
- **Check**: Is `ShowVotingOverlay` set to `true`?

**Issue**: "No countdown appears"
- **Check**: Is `ShowCountdownOverlay` set to `true`?
- **Check**: Is player in the race?
- **Check**: Did voting complete first?

**Issue**: "Queue counter not showing"
- **Check**: Is player panel visible? (join lobby first)
- **Check**: Is anyone in the queue? Counter only shows when `QueuedPlayers > 0`

**Issue**: "CUI errors in console"
- **Solution**: Reload both HydroRust and HydroUI plugins
- **Command**: `oxide.reload HydroRust; oxide.reload HydroUI`

### 8. Race State Flow

Understanding the race states helps diagnose issues:

```
1. STAGING
   ↓ (StartBattleVote called)
   
2. VOTING (if BattleRaceEnabled=true)
   - Voting overlay should appear
   - Duration: BattleRaceVoteDurationSeconds
   ↓ (FinishVoteAndProceed called)
   
3. COUNTDOWN
   - Countdown overlay should appear  
   - Shows: 10→9→8...→1→GO!
   - Duration: RaceCountdownSeconds
   ↓ (BeginRace called)
   
4. RUNNING
   - Race in progress
   - Overlays hidden
```

### 9. Data Flow Verification

Check the data is flowing correctly:

**HydroRust → HydroUI**:
1. HydroRust.UI_GetHudData() exposes: `Voting`, `CountdownSecondsRemaining`
2. HydroUI.PullHudTarget() reads the data
3. HydroUI.LerpHud() copies to Display
4. HydroUI.HudTick() checks conditions and shows overlays

**Verify in code** (HydroRust.cs line 1654, 1658):
```csharp
dict["Voting"] = currentRace.State == RaceState.Voting;
dict["CountdownSecondsRemaining"] = Mathf.Max(0, Mathf.CeilToInt(currentRace.CountdownRemaining));
```

**Verify in code** (HydroUI.cs line 384-413):
```csharp
// Voting overlay management
if (config.ShowVotingOverlay && st.Display.Voting) { ... }

// Countdown overlay management  
if (config.ShowCountdownOverlay && st.Display.CountdownSecondsRemaining > 0) { ... }
```

### 10. Reset and Fresh Start

If all else fails:

1. **Stop server**
2. **Delete configs**:
   ```
   oxide/config/HydroRust.json
   oxide/config/HydroUI.json
   ```
3. **Start server** (configs regenerate with defaults)
4. **Set BattleRaceEnabled to true** in HydroRust.json
5. **Reload plugins**: `oxide.reload HydroRust; oxide.reload HydroUI`
6. **Test again**

---

## Still Having Issues?

If overlays still don't appear after following all steps:

1. Check server console for any errors during plugin load
2. Verify Oxide/uMod is up to date
3. Test with a fresh server and minimal plugins
4. Share console logs showing the race start sequence
