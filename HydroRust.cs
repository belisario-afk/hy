using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("HydroRust", "belisario-afk + copilot", "5.0.6")]
    [Description("Boat racing with automatic queue, battle mode, kits, voting, track editor, time trials.")]
    public class HydroRust : RustPlugin
    {
        #region Permissions & Constants

        private const string PERM_ADMIN = "hydrorust.admin";
        private const string PERM_PLAYER = "hydrorust.player";

        private const string DATA_TRACKS = "HydroRust_Tracks";
        private const string DATA_STATS = "HydroRust_PlayerStats";
        private const string DATA_ROTATION = "HydroRust_RotationIndex";

        private const float EditorRadiusStep = 0.5f;

        #endregion

        #region Config

        private enum TrackSelectionMode { CurrentSelected, Random, Rotation }

        private class KitItem
        {
            [JsonProperty] public string ShortName;
            [JsonProperty] public int Amount;
            [JsonProperty] public ulong SkinId;
        }

        private class KitDefinition
        {
            [JsonProperty] public string Name;
            [JsonProperty] public List<KitItem> Items = new List<KitItem>();
        }

        private class PluginConfig
        {
            [JsonProperty("HudUpdateIntervalSeconds")] public float HudUpdateIntervalSeconds = 0.3f;

            [JsonProperty("RaceCountdownSeconds")] public int RaceCountdownSeconds = 10;
            [JsonProperty("RaceTimeoutSeconds")] public int RaceTimeoutSeconds = 600;

            [JsonProperty("Physics.MaxSpeedMultiplier")] public float MaxSpeedMultiplier = 2.0f;
            [JsonProperty("Physics.BoostForce")] public float BoostForce = 20f;
            [JsonProperty("Physics.BoostDurationSeconds")] public float BoostDurationSeconds = 1.5f;
            [JsonProperty("Physics.BoostCooldownSeconds")] public float BoostCooldownSeconds = 4f;

            [JsonProperty("Boats.AllowedBoatPrefabs")] public List<string> AllowedBoatPrefabs = new List<string>
            {
                "assets/content/vehicles/boats/rowboat/rowboat.prefab",
                "assets/content/vehicles/boats/rhib/rhib.prefab"
            };

            [JsonProperty("Boats.SpawnNewRaceBoats")] public bool SpawnNewRaceBoats = true;

            [JsonProperty("Controls.OverrideBoatControls")] public bool OverrideBoatControls = true;
            [JsonProperty("Controls.ForwardForce")] public float ControlsForwardForce = 6f;
            [JsonProperty("Controls.ReverseForce")] public float ControlsReverseForce = 4f;
            [JsonProperty("Controls.SteerTorque")] public float ControlsSteerTorque = 3f;
            [JsonProperty("Controls.LinearDrag")] public float ControlsLinearDrag = 0.15f;
            [JsonProperty("Controls.BoostKeySprint")] public bool ControlsBoostKeySprint = true;
            [JsonProperty("Controls.BoostForce")] public float ControlsBoostForce = 12f;
            [JsonProperty("Controls.BoostMaxSpeedMultiplier")] public float ControlsBoostMaxSpeedMultiplier = 2.5f;

            [JsonProperty("Visuals.Enabled")] public bool VisualsEnabled = true;
            [JsonProperty("Visuals.CheckpointPrefab")] public string VisualsCheckpointPrefab = "assets/bundled/prefabs/modding/events/twitch/br_sphere_green.prefab";
            [JsonProperty("Visuals.FinishPrefab")] public string VisualsFinishPrefab = "assets/bundled/prefabs/modding/events/twitch/br_sphere_purple.prefab";
            [JsonProperty("Visuals.BoostPrefab")] public string VisualsBoostPrefab = "assets/bundled/prefabs/modding/events/twitch/br_sphere_red.prefab";
            [JsonProperty("Visuals.MarkerBaseScale")] public float VisualsMarkerBaseScale = 1.0f;
            [JsonProperty("Visuals.CheckpointScaleMultiplier")] public float CheckpointScaleMultiplier = 1.0f;
            [JsonProperty("Visuals.FinishScaleMultiplier")] public float FinishScaleMultiplier = 1.2f;
            [JsonProperty("Visuals.BoostScaleMultiplier")] public float BoostScaleMultiplier = 1.0f;
            [JsonProperty("Visuals.ScaleWithRadius")] public bool ScaleWithRadius = true;
            [JsonProperty("Visuals.RadiusToScaleFactor")] public float RadiusToScaleFactor = 0.08f;
            [JsonProperty("Visuals.DefaultCheckpointColor")] public string DefaultCheckpointColor = "0 0.8 1 0.8";
            [JsonProperty("Visuals.DefaultFinishColor")] public string DefaultFinishColor = "0 1 0 0.8";
            [JsonProperty("Visuals.DefaultBoostColor")] public string DefaultBoostColor = "1 0.8 0 0.8";

            [JsonProperty("Lobby.Position")] public Vector3 LobbyPosition = Vector3.zero;
            [JsonProperty("Lobby.RotationYaw")] public float LobbyRotationYaw = 0f;

            [JsonProperty("Boost.MarkerRespawnSeconds")] public float BoostMarkerRespawnSeconds = 10f;

            [JsonProperty("Physics.MaxUpwardVelocity")] public float MaxUpwardVelocity = 4f;
            [JsonProperty("Physics.MaxDownwardVelocity")] public float MaxDownwardVelocity = 35f;
            [JsonProperty("Physics.UprightStrength")] public float UprightStrength = 2.0f;
            [JsonProperty("Physics.AirborneUpwardDamping")] public float AirborneUpwardDamping = 0.6f;
            [JsonProperty("Physics.ExtraGravity")] public float ExtraGravity = 2.0f;

            [JsonProperty("Airtime.MaxAllowedSeconds")] public float MaxAllowedAirtimeSeconds = 3f;

            [JsonProperty("Wipeout.RespawnIfBoatDestroyed")] public bool RespawnIfBoatDestroyed = true;
            [JsonProperty("Wipeout.RespawnIfPlayerDrowned")] public bool RespawnIfPlayerDrowned = true;
            [JsonProperty("Wipeout.RespawnHeightBelowBoat")] public float RespawnHeightBelowBoat = 10f;

            [JsonProperty("AutoRace.Enabled")] public bool AutoRaceEnabled = true;
            [JsonProperty("AutoRace.SlotSize")] public int AutoRaceSlotSize = 10;
            [JsonProperty("AutoRace.QueueStartDelaySeconds")] public float AutoRaceQueueStartDelaySeconds = 15f;
            [JsonProperty("AutoRace.TrackSelectionMode")] public TrackSelectionMode AutoRaceTrackSelectionMode = TrackSelectionMode.CurrentSelected;
            [JsonProperty("AutoRace.TrackRotationList")] public List<string> AutoRaceRotationList = new List<string>();

            [JsonProperty("BattleRace.Enabled")] public bool BattleRaceEnabled = true;
            [JsonProperty("BattleRace.VoteDurationSeconds")] public int BattleRaceVoteDurationSeconds = 10;
            [JsonProperty("BattleRace.DefaultKit")] public string BattleRaceDefaultKit = "Standard";
            [JsonProperty("BattleRace.ClearKitItemsAfterRace")] public bool BattleRaceClearKitItemsAfterRace = true;
            [JsonProperty("BattleRace.EnableMountedWeaponUse")] public bool BattleRaceEnableMountedWeaponUse = true;

            [JsonProperty("BattleRace.Kits")] public List<KitDefinition> BattleRaceKits = new List<KitDefinition>
            {
                new KitDefinition
                {
                    Name = "Standard",
                    Items = new List<KitItem>
                    {
                        new KitItem{ ShortName="rifle.ak", Amount=1, SkinId=0 },
                        new KitItem{ ShortName="pistol.python", Amount=1, SkinId=0 },
                        new KitItem{ ShortName="ammo.rifle", Amount=120, SkinId=0 },
                        new KitItem{ ShortName="ammo.pistol", Amount=60, SkinId=0 },
                        new KitItem{ ShortName="medical.syringe", Amount=8, SkinId=0 }
                    }
                }
            };
        }

        private PluginConfig config;
        private Quaternion LobbyRotation => Quaternion.Euler(0f, (config?.LobbyRotationYaw ?? 0f), 0f);

        #endregion

        #region Data Structures

        [JsonObject(MemberSerialization.OptIn)]
        private class Checkpoint
        {
            [JsonProperty] public Vector3 Position;
            [JsonProperty] public float Radius;
            [JsonProperty] public string Color;
        }

        [JsonObject(MemberSerialization.OptIn)]
        private class BoostZone
        {
            [JsonProperty] public Vector3 Position;
            [JsonProperty] public float Radius;
            [JsonProperty] public float StrengthMultiplier;
            [JsonProperty] public float DurationSeconds;
            [JsonProperty] public string Color;
        }

        [JsonObject(MemberSerialization.OptIn)]
        private class TrackDefinition
        {
            [JsonProperty] public string Name;
            [JsonProperty] public Vector3 StartPosition;
            [JsonProperty] public float StartYaw;
            [JsonProperty] public float StartRadius = 12f;
            [JsonProperty] public Vector3 FinishPosition;
            [JsonProperty] public float FinishRadius = 12f;
            [JsonProperty] public string FinishColor;
            [JsonProperty] public List<Checkpoint> Checkpoints = new List<Checkpoint>();
            [JsonProperty] public List<BoostZone> BoostZones = new List<BoostZone>();
            [JsonProperty] public int Laps = 1;
            [JsonProperty] public string Designer = "Unknown";
            [JsonProperty] public string ImageKey = "";
            [JsonIgnore] public Quaternion StartRotation
            {
                get => Quaternion.Euler(0f, StartYaw, 0f);
                set => StartYaw = value.eulerAngles.y;
            }
        }

        [JsonObject(MemberSerialization.OptIn)]
        private class TrackStats
        {
            [JsonProperty] public float BestTimeSeconds = -1f;
            [JsonProperty] public int Wins;
            [JsonProperty] public int CompletedRaces;
        }

        [JsonObject(MemberSerialization.OptIn)]
        private class PlayerStats
        {
            [JsonProperty] public ulong PlayerId;
            [JsonProperty] public Dictionary<string, TrackStats> TrackStats =
                new Dictionary<string, TrackStats>(StringComparer.OrdinalIgnoreCase);
        }

        private class TrackBuilder
        {
            public string Name;
            public TrackDefinition Track = new TrackDefinition();
        }

        private enum RaceState { None, Staging, Voting, Countdown, Running, Finished }
        private enum RaceMode { Normal, Battle }

        private class RaceParticipant
        {
            public BasePlayer Player;
            public BaseBoat Boat;
            public int CurrentCheckpointIndex;
            public int CurrentLap;
            public float TimeElapsed;
            public bool Finished;
            public float FinishTime;
            public Dictionary<int, float> ActiveBoosts = new Dictionary<int, float>();
            public Dictionary<int, float> BoostCooldowns = new Dictionary<int, float>();
            public float AirborneTime;
            public List<ItemId> GivenBattleItemIds = new List<ItemId>();
        }

        private class RaceSession
        {
            public TrackDefinition Track;
            public RaceState State = RaceState.None;
            public float CountdownRemaining;
            public float TimeSinceStart;
            public Dictionary<ulong, RaceParticipant> Participants = new Dictionary<ulong, RaceParticipant>();
            public List<BaseBoat> SpawnedBoats = new List<BaseBoat>();
            public Timer CountdownTimer;
            public Timer VoteTimer;
            public RaceMode Mode = RaceMode.Normal;
            public Dictionary<ulong, RaceMode> Votes = new Dictionary<ulong, RaceMode>();
            public string BattleKitUsed = "";
        }

        private class TimeTrialSession
        {
            public TrackDefinition Track;
            public BasePlayer Player;
            public BaseBoat Boat;
            public int CurrentCheckpointIndex;
            public int CurrentLap;
            public float TimeElapsed;
            public bool Finished;
            public Dictionary<int, float> ActiveBoosts = new Dictionary<int, float>();
            public Dictionary<int, float> BoostCooldowns = new Dictionary<int, float>();
            public float AirborneTime;
        }

        private class MarkerInfo
        {
            public BaseEntity Entity;
            public TrackDefinition Track;
            public int CheckpointIndex = -1;
            public int BoostIndex = -1;
            public bool IsFinish;
        }

        private enum EditorSelectType { None, Start, Finish, Checkpoint, Boost }

        private class EditorState
        {
            public bool Active;
            public TrackDefinition SelectedTrack;
            public EditorSelectType SelectedType = EditorSelectType.None;
            public int SelectedCheckpointIndex = -1;
            public int SelectedBoostIndex = -1;
            public bool MovingMarker;
        }

        #endregion

        #region State

        private readonly Dictionary<string, TrackDefinition> tracks = new Dictionary<string, TrackDefinition>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<ulong, PlayerStats> playerStats = new Dictionary<ulong, PlayerStats>();

        private TrackBuilder currentTrackBuilder;
        private RaceSession currentRace;
        private readonly Dictionary<ulong, TimeTrialSession> activeTimeTrials = new Dictionary<ulong, TimeTrialSession>();

        private Timer raceTickTimer;
        private Timer timeTrialTickTimer;
        private Timer controlTickTimer;
        private Timer editorTickTimer;
        private Timer autoRaceMonitor;

        private readonly List<MarkerInfo> activeTrackMarkers = new List<MarkerInfo>();
        private readonly Dictionary<ulong, EditorState> editorStates = new Dictionary<ulong, EditorState>();
        private readonly Dictionary<string, HashSet<int>> disabledBoostMarkers = new Dictionary<string, HashSet<int>>(StringComparer.OrdinalIgnoreCase);

        private readonly LinkedList<ulong> raceQueue = new LinkedList<ulong>();
        private int rotationIndex = 0;

        #endregion

        #region Init / Unload

        [PluginReference] private Plugin HydroLobby;

        private void EnsureConfig()
        {
            if (config == null)
            {
                PrintWarning("Config null; creating defaults.");
                config = new PluginConfig();
            }
            if (config.AllowedBoatPrefabs == null || config.AllowedBoatPrefabs.Count == 0)
            {
                config.AllowedBoatPrefabs = new List<string>
                {
                    "assets/content/vehicles/boats/rowboat/rowboat.prefab",
                    "assets/content/vehicles/boats/rhib/rhib.prefab"
                };
            }
        }

        private void LoadConfigSafe()
        {
            try
            {
                if (!Config.Exists())
                {
                    LoadDefaultConfig();
                    SaveConfigSafe();
                }
                else
                {
                    config = Config.ReadObject<PluginConfig>();
                }
            }
            catch (Exception e)
            {
                PrintWarning($"LoadConfig failed: {e.Message}");
                config = null;
            }
            EnsureConfig();
            SaveConfigSafe();
        }

        protected override void LoadDefaultConfig()
        {
            config = new PluginConfig();
            PrintWarning("Created default HydroRust config.");
        }

        private void SaveConfigSafe()
        {
            try
            {
                if (config != null)
                    Config.WriteObject(config, true);
            }
            catch (Exception e)
            {
                PrintWarning($"SaveConfig failed: {e.Message}");
            }
        }

        private void Init()
        {
            permission.RegisterPermission(PERM_ADMIN, this);
            permission.RegisterPermission(PERM_PLAYER, this);
            LoadConfigSafe();
            LoadDataFiles();
            LoadRotationIndex();
        }

        private void OnServerInitialized()
        {
            EnsureConfig();
            StartTickTimers();
            StartAutoRaceMonitor();
            var players = BasePlayer.activePlayerList;
            if (players != null)
            {
                foreach (var player in players)
                {
                    if (player == null) continue;
                    EnsurePlayerPermission(player);
                    TryAddToQueueFromLobby(player);
                }
            }
        }

        private void Unload()
        {
            StopTickTimers();
            autoRaceMonitor?.Destroy();
            SaveDataFiles();
            SaveRotationIndex();
            SaveConfigSafe();
            DestroyTrackMarkers();
            currentRace = null;
            activeTimeTrials.Clear();
        }

        private void EnsurePlayerPermission(BasePlayer player)
        {
            if (player == null) return;
            if (!permission.UserHasPermission(player.UserIDString, PERM_PLAYER))
                permission.GrantUserPermission(player.UserIDString, PERM_PLAYER, null);
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            EnsureConfig();
            EnsurePlayerPermission(player);
            timer.Once(2f, () => TryAddToQueueFromLobby(player));
        }

        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            editorStates.Remove(player.userID);
            currentRace?.Participants.Remove(player.userID);
            activeTimeTrials.Remove(player.userID);
            RemoveFromQueue(player.userID);
        }

        private void OnPlayerRespawn(BasePlayer player)
        {
            timer.Once(0.5f, () => TryAddToQueueFromLobby(player));
        }

        private void TryAddToQueueFromLobby(BasePlayer player)
        {
            EnsureConfig();
            if (config == null) return;
            if (player == null || !player.IsConnected) return;
            if (!config.AutoRaceEnabled) return;

            Vector3 lobbyPos = GetLobbyPosition();
            if (lobbyPos == Vector3.zero) return;

            if (Vector3.Distance(player.transform.position, lobbyPos) <= 20f)
                EnqueuePlayer(player.userID);
        }

        #endregion

        #region Data Load/Save

        private void LoadDataFiles()
        {
            try
            {
                var tracksData = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, TrackDefinition>>(DATA_TRACKS);
                if (tracksData != null)
                {
                    foreach (var kvp in tracksData)
                    {
                        var t = kvp.Value;
                        if (t == null) continue;
                        if (string.IsNullOrEmpty(t.FinishColor))
                            t.FinishColor = config?.DefaultFinishColor ?? "0 1 0 0.8";
                        for (int i = 0; i < t.Checkpoints.Count; i++)
                            if (string.IsNullOrEmpty(t.Checkpoints[i].Color))
                                t.Checkpoints[i].Color = config?.DefaultCheckpointColor ?? "0 0.8 1 0.8";
                        for (int i = 0; i < t.BoostZones.Count; i++)
                            if (string.IsNullOrEmpty(t.BoostZones[i].Color))
                                t.BoostZones[i].Color = config?.DefaultBoostColor ?? "1 0.8 0 0.8";
                        tracks[kvp.Key] = t;
                    }
                }
            }
            catch (Exception e) { PrintWarning($"Failed to load tracks data: {e.Message}"); }

            try
            {
                var statsData = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, PlayerStats>>(DATA_STATS);
                if (statsData != null)
                    foreach (var kvp in statsData)
                        if (kvp.Value != null)
                            playerStats[kvp.Key] = kvp.Value;
            }
            catch (Exception e) { PrintWarning($"Failed to load stats data: {e.Message}"); }
        }

        private void SaveDataFiles()
        {
            try
            {
                Interface.Oxide.DataFileSystem.WriteObject(DATA_TRACKS, new Dictionary<string, TrackDefinition>(tracks, StringComparer.OrdinalIgnoreCase), true);
                Interface.Oxide.DataFileSystem.WriteObject(DATA_STATS, new Dictionary<ulong, PlayerStats>(playerStats), true);
            }
            catch (Exception e)
            {
                PrintWarning($"SaveDataFiles error: {e.Message}");
            }
        }

        private void LoadRotationIndex()
        {
            try
            {
                rotationIndex = Interface.Oxide.DataFileSystem.ReadObject<int>(DATA_ROTATION);
            }
            catch { rotationIndex = 0; }
        }

        private void SaveRotationIndex()
        {
            try
            {
                Interface.Oxide.DataFileSystem.WriteObject(DATA_ROTATION, rotationIndex);
            }
            catch (Exception e)
            {
                PrintWarning($"SaveRotationIndex error: {e.Message}");
            }
        }

        private PlayerStats GetOrCreatePlayerStats(ulong id)
        {
            if (!playerStats.TryGetValue(id, out var ps))
            {
                ps = new PlayerStats { PlayerId = id };
                playerStats[id] = ps;
            }
            return ps;
        }

        private TrackStats GetOrCreateTrackStats(PlayerStats ps, string trackName)
        {
            if (ps == null) return null;
            if (!ps.TrackStats.TryGetValue(trackName, out var ts))
            {
                ts = new TrackStats();
                ps.TrackStats[trackName] = ts;
            }
            return ts;
        }

        private void UpdatePlayerStatsOnFinish(ulong id, string trackName, float time)
        {
            var ps = GetOrCreatePlayerStats(id);
            var ts = GetOrCreateTrackStats(ps, trackName);
            if (ts == null) return;
            if (ts.BestTimeSeconds < 0f || time < ts.BestTimeSeconds)
                ts.BestTimeSeconds = time;
            ts.CompletedRaces++;
            SaveDataFiles();
        }

        #endregion

        #region Timers

        private void StartTickTimers()
        {
            StopTickTimers();
            raceTickTimer = timer.Every(0.1f, RaceTick);
            timeTrialTickTimer = timer.Every(0.1f, TimeTrialTick);
            controlTickTimer = timer.Every(0.05f, ControlTick);
            editorTickTimer = timer.Every(0.05f, EditorTick);
        }

        private void StopTickTimers()
        {
            raceTickTimer?.Destroy();
            timeTrialTickTimer?.Destroy();
            controlTickTimer?.Destroy();
            editorTickTimer?.Destroy();
            raceTickTimer = timeTrialTickTimer = controlTickTimer = editorTickTimer = null;
        }

        private void StartAutoRaceMonitor()
        {
            autoRaceMonitor?.Destroy();
            autoRaceMonitor = timer.Every(3f, AutoRaceCheck);
        }

        #endregion

        #region Queue & Auto Race

        private void EnqueuePlayer(ulong id)
        {
            if (currentRace != null && currentRace.State != RaceState.Finished)
            {
                if (currentRace.Participants.ContainsKey(id)) return;
            }
            if (raceQueue.Contains(id)) return;
            raceQueue.AddLast(id);
        }

        private void RemoveFromQueue(ulong id)
        {
            var node = raceQueue.Find(id);
            if (node != null) raceQueue.Remove(node);
        }

        private TrackDefinition ResolveAutoRaceTrack()
        {
            EnsureConfig();
            if (tracks.Count == 0) return null;
            var mode = config.AutoRaceTrackSelectionMode;

            switch (mode)
            {
                case TrackSelectionMode.CurrentSelected:
                    var selected = editorStates.Values.FirstOrDefault(s => s.SelectedTrack != null)?.SelectedTrack;
                    return selected ?? tracks.Values.First();

                case TrackSelectionMode.Random:
                    return tracks.Values.ElementAt(UnityEngine.Random.Range(0, tracks.Count));

                case TrackSelectionMode.Rotation:
                    if (config.AutoRaceRotationList == null || config.AutoRaceRotationList.Count == 0)
                        return tracks.Values.ElementAt(rotationIndex % tracks.Count);
                    var valid = config.AutoRaceRotationList.Where(n => tracks.ContainsKey(n)).ToList();
                    if (valid.Count == 0) return tracks.Values.First();
                    if (rotationIndex >= valid.Count) rotationIndex = 0;
                    var name = valid[rotationIndex];
                    rotationIndex = (rotationIndex + 1) % valid.Count;
                    SaveRotationIndex();
                    return tracks[name];
            }
            return tracks.Values.First();
        }

        private void AutoRaceCheck()
        {
            EnsureConfig();
            if (config == null) return;
            if (!config.AutoRaceEnabled) return;
            if (currentRace != null && currentRace.State != RaceState.Finished) return;

            int slotSize = Mathf.Max(1, config.AutoRaceSlotSize);
            if (raceQueue.Count == 0) return;

            if (raceQueue.Count >= slotSize)
            {
                BeginAutoStage(slotSize);
            }
            else
            {
                if (config.AutoRaceQueueStartDelaySeconds > 0f)
                {
                    if (_pendingDelayedStart == null)
                    {
                        _pendingDelayedStart = timer.Once(config.AutoRaceQueueStartDelaySeconds, () =>
                        {
                            _pendingDelayedStart = null;
                            if (currentRace == null || currentRace.State == RaceState.Finished)
                            {
                                if (raceQueue.Count > 0 && raceQueue.Count < slotSize)
                                    BeginAutoStage(Math.Min(slotSize, raceQueue.Count));
                            }
                        });
                    }
                }
            }
        }

        private Timer _pendingDelayedStart;

        private void BeginAutoStage(int takeCount)
        {
            var track = ResolveAutoRaceTrack();
            if (track == null)
            {
                PrintWarning("AutoRace: No track available.");
                return;
            }

            currentRace = new RaceSession
            {
                Track = track,
                State = RaceState.Staging,
                CountdownRemaining = config?.RaceCountdownSeconds ?? 10
            };

            int added = 0;
            foreach (var id in raceQueue.ToList())
            {
                var player = BasePlayer.FindByID(id);
                if (player != null && player.IsConnected)
                {
                    currentRace.Participants[id] = new RaceParticipant { Player = player };
                    added++;
                    raceQueue.Remove(id);
                    if (added >= takeCount) break;
                }
                else raceQueue.Remove(id);
            }

            BroadcastToRace($"Auto race staged on {track.Name}. Voting for mode begins!");
            if ((config?.VisualsEnabled ?? false))
            {
                DestroyTrackMarkers();
                SpawnTrackMarkers(track);
            }

            StartBattleVote();
        }

        private void StartBattleVote()
        {
            EnsureConfig();
            if (currentRace == null) return;

            if (!(config?.BattleRaceEnabled ?? false))
            {
                currentRace.Mode = RaceMode.Normal;
                StartCountdownPhase();
                return;
            }

            currentRace.State = RaceState.Voting;
            currentRace.Votes.Clear();
            BroadcastToRace($"Vote race mode: /hydro vote normal OR /hydro vote battle ({config?.BattleRaceVoteDurationSeconds ?? 10}s).");

            currentRace.VoteTimer?.Destroy();
            currentRace.VoteTimer = timer.Once(config?.BattleRaceVoteDurationSeconds ?? 10, FinishVoteAndProceed);
        }

        private void FinishVoteAndProceed()
        {
            if (currentRace == null || currentRace.State != RaceState.Voting) return;
            int battle = currentRace.Votes.Values.Count(v => v == RaceMode.Battle);
            int normal = currentRace.Votes.Values.Count(v => v == RaceMode.Normal);

            currentRace.Mode = (battle > normal) ? RaceMode.Battle : RaceMode.Normal;
            BroadcastToRace($"Race mode chosen: {currentRace.Mode}");
            StartCountdownPhase();
        }

        private void StartCountdownPhase()
        {
            if (currentRace == null) return;
            currentRace.State = RaceState.Countdown;
            currentRace.CountdownRemaining = config?.RaceCountdownSeconds ?? 10;

            BroadcastToRace($"Race ({currentRace.Mode}) starting in {currentRace.CountdownRemaining} seconds...");
            currentRace.CountdownTimer?.Destroy();
            currentRace.CountdownTimer = timer.Every(1f, () =>
            {
                if (currentRace == null || currentRace.State != RaceState.Countdown) return;
                currentRace.CountdownRemaining--;
                if (currentRace.CountdownRemaining <= 0)
                {
                    currentRace.CountdownTimer?.Destroy();
                    currentRace.CountdownTimer = null;
                    BeginRace();
                }
                else BroadcastToRace($"{currentRace.Mode} race starting in {currentRace.CountdownRemaining}...");
            });
        }

        #endregion

        #region Airtime / Wipeout

        private void HandleAirtimeAndWipeout(TrackDefinition track, RaceParticipant part, BaseBoat boat, float delta)
        {
            if (part == null || part.Player == null) return;

            if (boat == null || boat.IsDestroyed)
            {
                if (config?.RespawnIfBoatDestroyed ?? true)
                    RespawnParticipantAtLastCheckpoint(track, part);
                return;
            }

            bool airborne = IsBoatAirborne(boat);
            if (airborne)
            {
                part.AirborneTime += delta;
                if (part.AirborneTime > (config?.MaxAllowedAirtimeSeconds ?? 3f))
                {
                    RespawnParticipantAtLastCheckpoint(track, part);
                    part.AirborneTime = 0f;
                }
            }
            else part.AirborneTime = 0f;

            if ((config?.RespawnIfPlayerDrowned ?? true) && part.Player.GetMounted() == null)
            {
                float diff = boat.transform.position.y - part.Player.transform.position.y;
                if (diff > (config?.RespawnHeightBelowBoat ?? 10f))
                    RespawnParticipantAtLastCheckpoint(track, part);
            }
        }

        private void HandleAirtimeAndWipeout(TrackDefinition track, TimeTrialSession session, BaseBoat boat, float delta)
        {
            if (session == null || session.Player == null) return;

            if (boat == null || boat.IsDestroyed)
            {
                if (config?.RespawnIfBoatDestroyed ?? true)
                    RespawnTimeTrialAtLastCheckpoint(track, session);
                return;
            }

            bool airborne = IsBoatAirborne(boat);
            if (airborne)
            {
                session.AirborneTime += delta;
                if (session.AirborneTime > (config?.MaxAllowedAirtimeSeconds ?? 3f))
                {
                    RespawnTimeTrialAtLastCheckpoint(track, session);
                    session.AirborneTime = 0f;
                }
            }
            else session.AirborneTime = 0f;

            if ((config?.RespawnIfPlayerDrowned ?? true) && session.Player.GetMounted() == null)
            {
                float diff = boat.transform.position.y - session.Player.transform.position.y;
                if (diff > (config?.RespawnHeightBelowBoat ?? 10f))
                    RespawnTimeTrialAtLastCheckpoint(track, session);
            }
        }

        private Vector3 GetCheckpointPosition(TrackDefinition track, int checkpointIndex)
        {
            if (track == null) return Vector3.zero;
            if (checkpointIndex <= 0 || track.Checkpoints.Count == 0) return track.StartPosition;
            int idx = Math.Min(checkpointIndex - 1, track.Checkpoints.Count - 1);
            return track.Checkpoints[idx].Position;
        }

        private Quaternion GetCheckpointRotation(TrackDefinition track, int checkpointIndex)
        {
            if (track == null) return Quaternion.identity;
            if (checkpointIndex <= 0 || track.Checkpoints.Count == 0) return track.StartRotation;
            int idx = Math.Min(checkpointIndex - 1, track.Checkpoints.Count - 1);
            Vector3 from = track.Checkpoints[idx].Position;
            Vector3 to = (idx + 1 < track.Checkpoints.Count) ? track.Checkpoints[idx + 1].Position : track.FinishPosition;
            Vector3 dir = to - from; dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) return track.StartRotation;
            return Quaternion.LookRotation(dir.normalized, Vector3.up);
        }

        private void RespawnParticipantAtLastCheckpoint(TrackDefinition track, RaceParticipant part)
        {
            if (track == null || part == null || part.Player == null) return;
            Vector3 cpPos = GetCheckpointPosition(track, part.CurrentCheckpointIndex);
            Quaternion cpRot = GetCheckpointRotation(track, part.CurrentCheckpointIndex);
            part.Boat?.Kill();
            var boat = SpawnBoatForPlayer(part.Player, cpPos, cpRot);
            StabilizeBoat(boat);
            part.Boat = boat;
            part.AirborneTime = 0f;
            part.Player.ChatMessage("<color=#ff8080>Respawned.</color>");
        }

        private void RespawnTimeTrialAtLastCheckpoint(TrackDefinition track, TimeTrialSession session)
        {
            if (track == null || session == null || session.Player == null) return;
            Vector3 cpPos = GetCheckpointPosition(track, session.CurrentCheckpointIndex);
            Quaternion cpRot = GetCheckpointRotation(track, session.CurrentCheckpointIndex);
            session.Boat?.Kill();
            var boat = SpawnBoatForPlayer(session.Player, cpPos, cpRot);
            StabilizeBoat(boat);
            session.Boat = boat;
            session.AirborneTime = 0f;
            session.Player.ChatMessage("<color=#ff8080>Respawned.</color>");
        }

        #endregion

        #region Boost & Controls

        private void TickBoosts(TrackDefinition track, BaseBoat boat,
            Dictionary<int, float> activeBoosts, Dictionary<int, float> boostCooldowns,
            float delta, BasePlayer player)
        {
            if (boat == null || boat.IsDestroyed || track == null) return;

            foreach (var idx in activeBoosts.Keys.ToList())
            {
                activeBoosts[idx] -= delta;
                if (activeBoosts[idx] <= 0f) activeBoosts.Remove(idx);
            }

            foreach (var idx in boostCooldowns.Keys.ToList())
            {
                boostCooldowns[idx] -= delta;
                if (boostCooldowns[idx] <= 0f) boostCooldowns.Remove(idx);
            }

            var rb = GetBoatRigidbody(boat);
            if (rb == null) return;

            Vector3 boatPos = boat.transform.position;

            for (int i = 0; i < track.BoostZones.Count; i++)
            {
                var zone = track.BoostZones[i];
                if (Vector3.Distance(boatPos, zone.Position) <= zone.Radius)
                {
                    if (boostCooldowns.ContainsKey(i) || activeBoosts.ContainsKey(i)) continue;

                    float duration = zone.DurationSeconds > 0 ? zone.DurationSeconds : (config?.BoostDurationSeconds ?? 1.5f);
                    float strength = zone.StrengthMultiplier > 0 ? zone.StrengthMultiplier : 1f;

                    activeBoosts[i] = duration;
                    boostCooldowns[i] = (config?.BoostCooldownSeconds ?? 4f);

                    Vector3 forward = boat.transform.forward.normalized;
                    rb.AddForce(forward * (config?.BoostForce ?? 20f) * strength, ForceMode.VelocityChange);

                    float maxSpeed = GetBaseBoatSpeed(boat) * (config?.MaxSpeedMultiplier ?? 2f) * strength;
                    if (rb.velocity.magnitude > maxSpeed)
                        rb.velocity = rb.velocity.normalized * maxSpeed;

                    player?.ChatMessage("<color=#00ffff>BOOST!</color>");
                    DisableBoostMarkerTemporarily(track, i, config?.BoostMarkerRespawnSeconds ?? 10f);
                }
            }

            if (rb.velocity.sqrMagnitude > 0.01f)
            {
                float baseMax = GetBaseBoatSpeed(boat) * (config?.MaxSpeedMultiplier ?? 2f);
                if (rb.velocity.magnitude > baseMax)
                    rb.velocity = rb.velocity.normalized * baseMax;
            }
        }

        private Rigidbody GetBoatRigidbody(BaseBoat boat)
        {
            return boat?.GetComponent<Rigidbody>() ?? boat?.GetComponentInChildren<Rigidbody>();
        }

        private float GetBaseBoatSpeed(BaseBoat boat)
        {
            if (boat == null) return 10f;
            string prefab = boat.ShortPrefabName?.ToLowerInvariant() ?? "";
            if (prefab.Contains("rowboat")) return 8f;
            if (prefab.Contains("rhib")) return 14f;
            return 10f;
        }

        private bool IsBoatAirborne(BaseBoat boat)
        {
            if (boat == null) return false;
            return !Physics.Raycast(boat.transform.position + Vector3.up * 0.5f, Vector3.down, out _, 3f,
                LayerMask.GetMask("Terrain", "World", "Water"));
        }

        private void ApplyArcadeControls(BasePlayer player, BaseBoat boat, float deltaTime)
        {
            if (player?.serverInput == null || boat == null || boat.IsDestroyed) return;
            if (!(config?.OverrideBoatControls ?? true)) return;

            var rb = GetBoatRigidbody(boat);
            if (rb == null) return;

            var input = player.serverInput;
            bool forward = input.IsDown(BUTTON.FORWARD);
            bool backward = input.IsDown(BUTTON.BACKWARD);
            bool left = input.IsDown(BUTTON.LEFT);
            bool right = input.IsDown(BUTTON.RIGHT);
            bool boostKey = (config?.ControlsBoostKeySprint ?? true) ? input.IsDown(BUTTON.SPRINT) : input.IsDown(BUTTON.RELOAD);

            Vector3 forwardDir = boat.transform.forward;
            rb.velocity *= (1f - (config?.ControlsLinearDrag ?? 0.15f) * deltaTime);

            float baseSpeed = GetBaseBoatSpeed(boat);
            float maxSpeed = baseSpeed * (config?.MaxSpeedMultiplier ?? 2f);

            if (forward) rb.AddForce(forwardDir * (config?.ControlsForwardForce ?? 6f), ForceMode.Acceleration);
            if (backward) rb.AddForce(-forwardDir * (config?.ControlsReverseForce ?? 4f), ForceMode.Acceleration);

            float steer = 0f;
            if (left) steer -= 1f;
            if (right) steer += 1f;
            if (Mathf.Abs(steer) > 0.01f)
                rb.AddTorque(Vector3.up * steer * (config?.ControlsSteerTorque ?? 3f), ForceMode.Acceleration);

            if (boostKey)
            {
                float boostMax = baseSpeed * (config?.ControlsBoostMaxSpeedMultiplier ?? 2.5f);
                rb.AddForce(forwardDir * (config?.ControlsBoostForce ?? 12f), ForceMode.Acceleration);
                if (rb.velocity.magnitude > boostMax)
                    rb.velocity = rb.velocity.normalized * boostMax;
            }
            else if (rb.velocity.magnitude > maxSpeed)
                rb.velocity = rb.velocity.normalized * maxSpeed;

            var vel = rb.velocity;
            Vector3 up = boat.transform.up;
            float tilt = Vector3.Angle(up, Vector3.up);

            if (tilt > 10f)
            {
                Vector3 axis = Vector3.Cross(up, Vector3.up);
                float strength = Mathf.Min(1.0f, (config?.UprightStrength ?? 2f)) * (tilt / 90f);
                rb.AddTorque(axis.normalized * (-strength), ForceMode.Acceleration);
                if (tilt > 25f) rb.AddForce(Vector3.down * (config?.ExtraGravity ?? 2f), ForceMode.Acceleration);
            }

            if (IsBoatAirborne(boat) && vel.y > 0f)
                vel.y *= Mathf.Clamp01(1f - (config?.AirborneUpwardDamping ?? 0.6f));

            vel.y = Mathf.Clamp(vel.y, -(config?.MaxDownwardVelocity ?? 35f), (config?.MaxUpwardVelocity ?? 4f));
            rb.velocity = vel;
        }

        #endregion

        #region Track Logic

        private void HandleCheckpointLogic(TrackDefinition track, RaceParticipant part)
        {
            if (part?.Boat == null || track == null || track.Checkpoints.Count == 0) return;

            int cpIndex = part.CurrentCheckpointIndex;
            if (cpIndex >= track.Checkpoints.Count) return;

            var cp = track.Checkpoints[cpIndex];
            if (Vector3.Distance(part.Boat.transform.position, cp.Position) <= cp.Radius)
            {
                part.CurrentCheckpointIndex++;
                if (part.CurrentCheckpointIndex >= track.Checkpoints.Count)
                {
                    part.CurrentCheckpointIndex = 0;
                    part.CurrentLap++;
                    part.Player?.ChatMessage($"<color=#ffff00>Lap {part.CurrentLap}/{track.Laps}</color>");
                }
                else part.Player?.ChatMessage($"Checkpoint {part.CurrentCheckpointIndex}/{track.Checkpoints.Count}");
            }
        }

        private void HandleCheckpointLogic(TrackDefinition track, TimeTrialSession session)
        {
            if (session?.Boat == null || track == null || track.Checkpoints.Count == 0) return;

            int cpIndex = session.CurrentCheckpointIndex;
            if (cpIndex >= track.Checkpoints.Count) return;

            var cp = track.Checkpoints[cpIndex];
            if (Vector3.Distance(session.Boat.transform.position, cp.Position) <= cp.Radius)
            {
                session.CurrentCheckpointIndex++;
                if (session.CurrentCheckpointIndex >= track.Checkpoints.Count)
                {
                    session.CurrentCheckpointIndex = 0;
                    session.CurrentLap++;
                    session.Player?.ChatMessage($"<color=#ffff00>Lap {session.CurrentLap}/{track.Laps}</color>");
                }
                else session.Player?.ChatMessage($"Checkpoint {session.CurrentCheckpointIndex}/{track.Checkpoints.Count}");
            }
        }

        private bool HasFinishedTrack(TrackDefinition track, RaceParticipant part)
        {
            if (part?.Boat == null || track == null || part.CurrentLap < track.Laps) return false;
            return Vector3.Distance(part.Boat.transform.position, track.FinishPosition) <= track.FinishRadius;
        }

        private bool HasFinishedTrack(TrackDefinition track, TimeTrialSession session)
        {
            if (session?.Boat == null || track == null || session.CurrentLap < track.Laps) return false;
            return Vector3.Distance(session.Boat.transform.position, track.FinishPosition) <= track.FinishRadius;
        }

        #endregion

        #region Race Flow

        private void BeginRace()
        {
            if (currentRace == null) return;

            int count = Math.Min(currentRace.Participants.Count, config?.AutoRaceSlotSize ?? 10);
            int index = 0;

            foreach (var kvp in currentRace.Participants.ToList())
            {
                var part = kvp.Value;
                var player = part.Player;
                if (player == null || !player.IsConnected) continue;

                Vector3 startPos = GetStartSlotPosition(currentRace.Track, index, count);
                Quaternion startRot = currentRace.Track.StartRotation;
                TeleportTo(startPos, startRot, player);

                if (config?.SpawnNewRaceBoats ?? true)
                {
                    var boat = SpawnBoatForPlayer(player, startPos, startRot);
                    if (boat != null)
                    {
                        part.Boat = boat;
                        currentRace.SpawnedBoats.Add(boat);
                        EnableMountedCombatIfBattle(boat);
                    }
                }
                else
                {
                    part.Boat = GetPlayerBoat(player);
                    MountPlayerInBoatDriverSeat(player, part.Boat);
                    EnableMountedCombatIfBattle(part.Boat);
                }

                index++;
                if (index >= count) break;
            }

            DistributeBattleKitsIfNeeded();
            currentRace.State = RaceState.Running;
            BroadcastToRace(currentRace.Mode == RaceMode.Battle
                ? "<color=#ff4444>BATTLE GO!</color>"
                : "<color=#00ff00>GO!</color>");
        }

        private void EnableMountedCombatIfBattle(BaseBoat boat)
        {
            if (currentRace == null || currentRace.Mode != RaceMode.Battle) return;
            if (!(config?.BattleRaceEnableMountedWeaponUse ?? true) || boat == null) return;

            var mounts = boat.GetComponentsInChildren<BaseMountable>();
            foreach (var m in mounts)
                TryEnableItemsOnMount(m);
        }

        private void TryEnableItemsOnMount(BaseMountable m)
        {
            if (m == null) return;
            SetBoolFieldOrProperty(m, "disableItemUsage", false);
            SetBoolFieldOrProperty(m, "canWieldItems", true);
            SetBoolFieldOrProperty(m, "canHideHeldEntity", false);
        }

        private void SetBoolFieldOrProperty(object obj, string name, bool value)
        {
            if (obj == null) return;
            var type = obj.GetType();

            var fi = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fi != null && fi.FieldType == typeof(bool))
            {
                try { fi.SetValue(obj, value); } catch { }
                return;
            }

            var pi = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (pi != null && pi.CanWrite && pi.PropertyType == typeof(bool))
            {
                try { pi.SetValue(obj, value, null); } catch { }
            }
        }

        private void DistributeBattleKitsIfNeeded()
        {
            if (currentRace == null || currentRace.Mode != RaceMode.Battle) return;
            var kits = config?.BattleRaceKits;
            if (kits == null || kits.Count == 0) return;
            var kit = kits.FirstOrDefault(k => k.Name.Equals(config.BattleRaceDefaultKit, StringComparison.OrdinalIgnoreCase))
                      ?? kits.FirstOrDefault();
            if (kit == null) return;
            currentRace.BattleKitUsed = kit.Name;

            foreach (var part in currentRace.Participants.Values)
            {
                var player = part.Player;
                if (player == null || !player.IsConnected) continue;
                foreach (var itemDef in kit.Items)
                {
                    var item = ItemManager.CreateByName(itemDef.ShortName, itemDef.Amount, itemDef.SkinId);
                    if (item == null) continue;
                    if (!player.inventory.GiveItem(item))
                    {
                        item.Remove();
                        continue;
                    }
                    part.GivenBattleItemIds.Add(item.uid);
                }
                player.ChatMessage($"Battle Kit [{kit.Name}] received.");
            }
        }

        private void FinishRace()
        {
            if (currentRace == null) return;

            currentRace.State = RaceState.Finished;
            var standings = currentRace.Participants.Values.Where(p => p.Finished).OrderBy(p => p.FinishTime).ToList();

            if (standings.Any())
            {
                var winner = standings[0];
                BroadcastToRace($"Winner: {winner.Player.displayName} in {winner.FinishTime:0.00}s");
                var stats = GetOrCreatePlayerStats(winner.Player.userID);
                var ts = GetOrCreateTrackStats(stats, currentRace.Track.Name);
                if (ts != null) ts.Wins++;
            }
            else BroadcastToRace("No finishers.");

            CleanupBattleItemsIfNeeded();

            foreach (var part in currentRace.Participants.Values)
            {
                part.Boat?.Kill();
                if (part.Player != null && part.Player.IsConnected)
                    TeleportToLobby(part.Player);
            }

            foreach (var boat in currentRace.SpawnedBoats)
                boat?.Kill();
            currentRace.SpawnedBoats.Clear();

            DestroyTrackMarkers();
            currentRace = null;
            SaveDataFiles();

            timer.Once(5f, () => AutoRaceCheck());
        }

        private static IEnumerable<Item> GetAllPlayerItems(BasePlayer player)
        {
            if (player?.inventory == null) yield break;

            var cm = player.inventory.containerMain?.itemList;
            if (cm != null)
                foreach (var it in cm)
                    if (it != null) yield return it;

            var cb = player.inventory.containerBelt?.itemList;
            if (cb != null)
                foreach (var it in cb)
                    if (it != null) yield return it;

            var cw = player.inventory.containerWear?.itemList;
            if (cw != null)
                foreach (var it in cw)
                    if (it != null) yield return it;
        }

        private void CleanupBattleItemsIfNeeded()
        {
            if (currentRace == null || currentRace.Mode != RaceMode.Battle) return;
            if (!(config?.BattleRaceClearKitItemsAfterRace ?? true)) return;

            foreach (var part in currentRace.Participants.Values)
            {
                var player = part.Player;
                if (player == null || !player.IsConnected) continue;

                var allItems = GetAllPlayerItems(player).ToList();
                foreach (var item in allItems)
                {
                    if (item == null) continue;
                    if (part.GivenBattleItemIds.Contains(item.uid))
                        item.Remove();
                }
                player.ChatMessage("Battle kit items cleared.");
            }
        }

        private bool AllRaceParticipantsFinished()
        {
            if (currentRace == null) return true;
            return currentRace.Participants.Values.All(p => p.Finished);
        }

        private void BroadcastToRace(string msg)
        {
            if (currentRace == null) return;
            foreach (var part in currentRace.Participants.Values)
                part.Player?.ChatMessage(msg);
        }

        private void BroadcastToAdmins(string msg)
        {
            foreach (var player in BasePlayer.activePlayerList)
                if (permission.UserHasPermission(player.UserIDString, PERM_ADMIN))
                    player.ChatMessage(msg);
        }

        private Vector3 GetStartSlotPosition(TrackDefinition track, int slotIndex, int totalSlots)
        {
            totalSlots = Mathf.Clamp(totalSlots, 1, 10);
            slotIndex = Mathf.Clamp(slotIndex, 0, totalSlots - 1);
            float laneSpacing = 6f;
            float centerOffset = -(totalSlots - 1) * 0.5f * laneSpacing;
            float localX = centerOffset + slotIndex * laneSpacing;
            Vector3 localOffset = new Vector3(localX, 0f, -4f);
            return track.StartPosition + track.StartRotation * localOffset;
        }

        #endregion

        #region Time Trials

        private void StartTimeTrial(BasePlayer player, TrackDefinition track)
        {
            if (player == null || track == null) return;

            if (activeTimeTrials.ContainsKey(player.userID))
            {
                SendReply(player, "Already in a time trial.");
                return;
            }

            TeleportTo(track.StartPosition, track.StartRotation, player);

            BaseBoat boat = (config?.SpawnNewRaceBoats ?? true)
                ? SpawnBoatForPlayer(player, track.StartPosition, track.StartRotation)
                : GetPlayerBoat(player);

            MountPlayerInBoatDriverSeat(player, boat);

            var session = new TimeTrialSession
            {
                Player = player,
                Track = track,
                Boat = boat,
                CurrentCheckpointIndex = 0,
                CurrentLap = 0
            };

            activeTimeTrials[player.userID] = session;

            if (config?.VisualsEnabled ?? false)
            {
                DestroyTrackMarkers();
                SpawnTrackMarkers(track);
            }

            SendReply(player, $"Time trial started on {track.Name}!");
        }

        #endregion

        #region Boat / Lobby Helpers

        private BaseBoat SpawnBoatForPlayer(BasePlayer player, Vector3 position, Quaternion rotation)
        {
            EnsureConfig();
            var prefab = config?.AllowedBoatPrefabs?.FirstOrDefault();
            if (string.IsNullOrEmpty(prefab)) return null;

            var ent = GameManager.server.CreateEntity(prefab, position, rotation) as BaseBoat;
            if (ent == null)
            {
                PrintWarning($"Failed to create boat entity: {prefab}");
                return null;
            }

            ent.Spawn();
            StabilizeBoat(ent);
            MountPlayerInBoatDriverSeat(player, ent);
            return ent;
        }

        private void StabilizeBoat(BaseBoat boat)
        {
            if (boat == null || boat.IsDestroyed) return;
            var rb = GetBoatRigidbody(boat);
            if (rb == null) return;

            var euler = boat.transform.rotation.eulerAngles;
            boat.transform.rotation = Quaternion.Euler(0f, euler.y, 0f);
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            boat.transform.position += Vector3.down * 0.5f;
            try { rb.centerOfMass += new Vector3(0f, -0.5f, 0f); } catch { }
            rb.AddForce(Vector3.down * 2f, ForceMode.VelocityChange);
        }

        private void MountPlayerInBoatDriverSeat(BasePlayer player, BaseBoat boat)
        {
            if (player == null || boat == null || boat.IsDestroyed) return;

            player.EnsureDismounted();
            player.Teleport(boat.transform.position + Vector3.up * 0.5f);

            var mounts = boat.GetComponentsInChildren<BaseMountable>();
            BaseMountable driver = null;
            float bestDot = -999f;
            foreach (var m in mounts)
            {
                if (m == null) continue;
                Vector3 dir = (m.transform.position - boat.transform.position).normalized;
                float dot = Vector3.Dot(boat.transform.forward, dir);
                if (dot > bestDot)
                {
                    bestDot = dot;
                    driver = m;
                }
            }
            if (driver == null && mounts.Length > 0) driver = mounts[0];
            driver?.AttemptMount(player);
        }

        private BaseBoat GetPlayerBoat(BasePlayer player)
        {
            if (player == null) return null;

            var mount = player.GetMounted();
            if (mount != null)
            {
                var boat = mount.GetComponentInParent<BaseBoat>();
                if (boat != null) return boat;
            }

            var entities = new List<BaseEntity>();
            Vis.Entities(player.transform.position, 8f, entities, Rust.Layers.Mask.Vehicle_Detailed);
            foreach (var entity in entities)
            {
                if (entity is BaseBoat b)
                    return b;
            }
            return null;
        }

        private void TeleportTo(Vector3 pos, Quaternion rot, BasePlayer player)
        {
            if (player == null) return;
            player.EnsureDismounted();
            player.Teleport(pos);
            player.transform.rotation = rot;
            player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
            player.UpdateNetworkGroup();
            player.SendNetworkUpdateImmediate();
        }

        private void TeleportToLobby(BasePlayer player)
        {
            if (player == null) return;
            if (HydroLobby != null)
            {
                HydroLobby.Call("SendToLobby", player);
                return;
            }
            if (config != null && config.LobbyPosition != Vector3.zero)
                TeleportTo(config.LobbyPosition, LobbyRotation, player);
        }

        private Vector3 GetLobbyPosition()
        {
            if (HydroLobby != null)
            {
                var pos = HydroLobby.Call("GetLobbyPosition");
                if (pos is Vector3 v) return v;
            }
            return config?.LobbyPosition ?? Vector3.zero;
        }

        #endregion

        #region Visual Markers & Editor

        private void SpawnTrackMarkers(TrackDefinition track)
        {
            if (!(config?.VisualsEnabled ?? false) || track == null) return;
            DestroyTrackMarkers();

            for (int i = 0; i < track.Checkpoints.Count; i++)
            {
                var cp = track.Checkpoints[i];
                var marker = SpawnMarker(config.VisualsCheckpointPrefab, cp.Position,
                    cp.Color ?? config.DefaultCheckpointColor, config.CheckpointScaleMultiplier, cp.Radius);
                if (marker != null)
                    activeTrackMarkers.Add(new MarkerInfo { Entity = marker, Track = track, CheckpointIndex = i });
            }

            var finishMarker = SpawnMarker(config.VisualsFinishPrefab, track.FinishPosition,
                track.FinishColor ?? config.DefaultFinishColor, config.FinishScaleMultiplier, track.FinishRadius);
            if (finishMarker != null)
                activeTrackMarkers.Add(new MarkerInfo { Entity = finishMarker, Track = track, IsFinish = true });

            disabledBoostMarkers.TryGetValue(track.Name, out var disabled);
            for (int i = 0; i < track.BoostZones.Count; i++)
            {
                if (disabled != null && disabled.Contains(i)) continue;
                var bz = track.BoostZones[i];
                var marker = SpawnMarker(config.VisualsBoostPrefab, bz.Position,
                    bz.Color ?? config.DefaultBoostColor, config.BoostScaleMultiplier, bz.Radius);
                if (marker != null)
                    activeTrackMarkers.Add(new MarkerInfo { Entity = marker, Track = track, BoostIndex = i });
            }
        }

        private void RefreshTrackMarkers(TrackDefinition track)
        {
            if (!(config?.VisualsEnabled ?? false) || track == null) return;
            SpawnTrackMarkers(track);
        }

        private BaseEntity SpawnMarker(string prefab, Vector3 pos, string color, float typeScaleMultiplier, float triggerRadius)
        {
            if (string.IsNullOrEmpty(prefab)) return null;
            var ent = GameManager.server.CreateEntity(prefab, pos, Quaternion.identity);
            if (ent == null)
            {
                PrintWarning($"Failed to spawn marker prefab: {prefab}");
                return null;
            }
            ent.Spawn();

            float baseScale = Mathf.Max(0.01f, config?.VisualsMarkerBaseScale ?? 1f);
            float radiusScale = 1f;
            if ((config?.ScaleWithRadius ?? true) && triggerRadius > 0f)
                radiusScale = triggerRadius * Mathf.Max(0.001f, config?.RadiusToScaleFactor ?? 0.08f);
            float finalScale = baseScale * Mathf.Max(0.01f, typeScaleMultiplier) * radiusScale;
            ent.transform.localScale = new Vector3(finalScale, finalScale, finalScale);
            return ent;
        }

        private void DestroyTrackMarkers()
        {
            foreach (var m in activeTrackMarkers)
                if (m.Entity != null && !m.Entity.IsDestroyed)
                    m.Entity.Kill();
            activeTrackMarkers.Clear();
        }

        private MarkerInfo RaycastMarker(BasePlayer player)
        {
            if (player == null) return null;
            Ray ray = new Ray(player.eyes.position, player.eyes.BodyForward());
            if (!Physics.Raycast(ray, out var hit, 200f, LayerMask.GetMask("Deployed", "Default", "Construction")))
                return null;
            var ent = hit.GetEntity();
            return activeTrackMarkers.FirstOrDefault(m => m.Entity == ent);
        }

        private bool RaycastGroundOrWater(BasePlayer player, out Vector3 hitPos)
        {
            hitPos = Vector3.zero;
            if (player == null) return false;
            Ray ray = new Ray(player.eyes.position, player.eyes.BodyForward());
            if (Physics.Raycast(ray, out var hit, 400f, LayerMask.GetMask("Terrain", "World", "Water", "Default")))
            {
                hitPos = hit.point;
                return true;
            }
            return false;
        }

        private void MoveSelectedMarker(EditorState state, Vector3 pos)
        {
            if (state.SelectedTrack == null) return;
            switch (state.SelectedType)
            {
                case EditorSelectType.Checkpoint:
                    if (state.SelectedCheckpointIndex >= 0 && state.SelectedCheckpointIndex < state.SelectedTrack.Checkpoints.Count)
                        state.SelectedTrack.Checkpoints[state.SelectedCheckpointIndex].Position = pos;
                    break;
                case EditorSelectType.Boost:
                    if (state.SelectedBoostIndex >= 0 && state.SelectedBoostIndex < state.SelectedTrack.BoostZones.Count)
                        state.SelectedTrack.BoostZones[state.SelectedBoostIndex].Position = pos;
                    break;
                case EditorSelectType.Finish:
                    state.SelectedTrack.FinishPosition = pos;
                    break;
                case EditorSelectType.Start:
                    state.SelectedTrack.StartPosition = pos;
                    break;
            }
            SaveDataFiles();
        }

        private void DisableBoostMarkerTemporarily(TrackDefinition track, int boostIndex, float respawnSeconds)
        {
            if (track == null) return;

            if (!disabledBoostMarkers.TryGetValue(track.Name, out var disabled))
            {
                disabled = new HashSet<int>();
                disabledBoostMarkers[track.Name] = disabled;
            }
            if (!disabled.Add(boostIndex)) return;

            var existing = activeTrackMarkers.FirstOrDefault(m => m.Track == track && m.BoostIndex == boostIndex);
            if (existing != null && existing.Entity != null && !existing.Entity.IsDestroyed)
            {
                existing.Entity.Kill();
                activeTrackMarkers.Remove(existing);
            }

            timer.Once(respawnSeconds, () =>
            {
                if (!disabledBoostMarkers.TryGetValue(track.Name, out var disabledNow)) return;
                if (disabledNow.Remove(boostIndex))
                    RespawnSingleBoostMarker(track, boostIndex);
            });
        }

        private void RespawnSingleBoostMarker(TrackDefinition track, int boostIndex)
        {
            if (!(config?.VisualsEnabled ?? false) || track == null) return;
            if (boostIndex < 0 || boostIndex >= track.BoostZones.Count) return;
            var bz = track.BoostZones[boostIndex];
            var marker = SpawnMarker(config.VisualsBoostPrefab, bz.Position, bz.Color ?? config.DefaultBoostColor,
                config.BoostScaleMultiplier, bz.Radius);
            if (marker != null)
                activeTrackMarkers.Add(new MarkerInfo { Entity = marker, Track = track, BoostIndex = boostIndex });
        }

        #endregion

        #region External UI Endpoints

        public object UI_GetHudData(BasePlayer player)
        {
            var dict = new Dictionary<string, object>
            {
                ["TrackName"] = "",
                ["Lap"] = 0,
                ["TotalLaps"] = 0,
                ["Checkpoint"] = 0,
                ["TotalCheckpoints"] = 0,
                ["Progress01"] = 0f,
                ["Speed"] = 0f,
                ["Boost01"] = 0f,
                ["Position"] = 0,
                ["Racers"] = 0,
                ["IsRace"] = false,
                ["Finished"] = false,
                ["FinishTime"] = 0f,
                ["RaceMode"] = "None",
                ["Voting"] = false,
                ["VoteSecondsRemaining"] = 0,
                ["CountdownSecondsRemaining"] = -1,
                ["QueuedPlayers"] = raceQueue.Count
            };

            if (player == null || !player.IsConnected) return dict;

            var boat = GetPlayerBoat(player);
            var rb = boat ? GetBoatRigidbody(boat) : null;
            dict["Speed"] = rb != null ? (object)rb.velocity.magnitude : 0f;

            if (currentRace != null &&
                (currentRace.State == RaceState.Running || currentRace.State == RaceState.Countdown || currentRace.State == RaceState.Voting) &&
                currentRace.Participants.TryGetValue(player.userID, out var part))
            {
                var t = currentRace.Track;
                dict["TrackName"] = t?.Name ?? "";
                dict["Lap"] = part.CurrentLap;
                dict["TotalLaps"] = t?.Laps ?? 0;
                dict["Checkpoint"] = part.CurrentCheckpointIndex;
                dict["TotalCheckpoints"] = t?.Checkpoints?.Count ?? 0;
                dict["IsRace"] = true;
                dict["Finished"] = part.Finished;
                dict["FinishTime"] = part.FinishTime;
                dict["RaceMode"] = currentRace.Mode.ToString();
                dict["Voting"] = currentRace.State == RaceState.Voting;
                
                // Countdown support
                if (currentRace.State == RaceState.Countdown)
                    dict["CountdownSecondsRemaining"] = Mathf.Max(0, Mathf.CeilToInt(currentRace.CountdownRemaining));
                else
                    dict["CountdownSecondsRemaining"] = -1;

                float laps = Mathf.Max(1, t?.Laps ?? 1);
                float cps = Mathf.Max(1, t?.Checkpoints.Count ?? 1);
                float prog = (part.CurrentLap + (part.CurrentCheckpointIndex / cps)) / laps;
                dict["Progress01"] = Mathf.Clamp01(prog);

                float maxRemain = part.ActiveBoosts.Count > 0 ? part.ActiveBoosts.Values.Max() : 0f;
                dict["Boost01"] = Mathf.Clamp01((config?.BoostDurationSeconds ?? 1.5f) <= 0f ? 0f : (maxRemain / (config?.BoostDurationSeconds ?? 1.5f)));

                dict["Racers"] = currentRace.Participants.Count;
                dict["Position"] = EstimateRacePosition(currentRace, part);
                return dict;
            }

            if (activeTimeTrials.TryGetValue(player.userID, out var session))
            {
                var t = session.Track;
                dict["TrackName"] = t?.Name ?? "";
                dict["Lap"] = session.CurrentLap;
                dict["TotalLaps"] = t?.Laps ?? 0;
                dict["Checkpoint"] = session.CurrentCheckpointIndex;
                dict["TotalCheckpoints"] = t?.Checkpoints?.Count ?? 0;
                dict["IsRace"] = false;
                dict["Finished"] = session.Finished;
                dict["FinishTime"] = session.TimeElapsed;

                float laps = Mathf.Max(1, t?.Laps ?? 1);
                float cps = Mathf.Max(1, t?.Checkpoints.Count ?? 1);
                float prog = (session.CurrentLap + (session.CurrentCheckpointIndex / cps)) / laps;
                dict["Progress01"] = Mathf.Clamp01(prog);

                float maxRemain2 = session.ActiveBoosts.Count > 0 ? session.ActiveBoosts.Values.Max() : 0f;
                dict["Boost01"] = Mathf.Clamp01((config?.BoostDurationSeconds ?? 1.5f) <= 0f ? 0f : (maxRemain2 / (config?.BoostDurationSeconds ?? 1.5f)));

                dict["Position"] = 1;
                dict["Racers"] = 1;
            }

            return dict;
        }

        private int EstimateRacePosition(RaceSession race, RaceParticipant me)
        {
            var t = race.Track;
            int cpCount = Mathf.Max(1, t.Checkpoints.Count);

            float Score(RaceParticipant p)
            {
                float baseScore = p.CurrentLap * 1000f + p.CurrentCheckpointIndex;
                int idx = Mathf.Clamp(p.CurrentCheckpointIndex, 0, cpCount - 1);
                Vector3 nextPos = (idx < cpCount) ? t.Checkpoints[idx].Position : t.FinishPosition;
                Vector3 fromPos = p.Boat ? p.Boat.transform.position : p.Player.transform.position;
                float dist = Vector3.Distance(fromPos, nextPos);
                return baseScore - dist * 0.01f;
            }

            var ordered = race.Participants.Values
                .OrderByDescending(p => p.Finished)
                .ThenBy(p => p.FinishTime)
                .ThenByDescending(p => Score(p))
                .ToList();

            int pos = ordered.FindIndex(p => p == me) + 1;
            return pos <= 0 ? race.Participants.Count : pos;
        }

        public object UI_ListTrackNames() => tracks.Keys.OrderBy(k => k).ToList();

        public object UI_GetEditorContext(BasePlayer player)
        {
            var st = GetOrCreateEditorState(player);
            var dict = new Dictionary<string, object>
            {
                ["HasTrack"] = st.SelectedTrack != null,
                ["TrackName"] = st.SelectedTrack?.Name ?? "",
                ["Laps"] = st.SelectedTrack?.Laps ?? 0,
                ["Designer"] = st.SelectedTrack?.Designer ?? "Unknown",
                ["ImageKey"] = st.SelectedTrack?.ImageKey ?? "",
                ["SelectedType"] = st.SelectedType.ToString(),
                ["SelectedCheckpointIndex"] = st.SelectedCheckpointIndex,
                ["SelectedBoostIndex"] = st.SelectedBoostIndex,
                ["CheckpointCount"] = st.SelectedTrack?.Checkpoints.Count ?? 0,
                ["BoostCount"] = st.SelectedTrack?.BoostZones.Count ?? 0
            };
            return dict;
        }

        private EditorState GetOrCreateEditorState(BasePlayer player)
        {
            if (!editorStates.TryGetValue(player.userID, out var st))
            {
                st = new EditorState();
                editorStates[player.userID] = st;
            }
            return st;
        }

        private bool ValidateAdmin(BasePlayer player)
        {
            if (player == null) return false;
            if (!permission.UserHasPermission(player.UserIDString, PERM_ADMIN))
            {
                player.ChatMessage("No permission.");
                return false;
            }
            return true;
        }

        #endregion

        #region Editor API (core)

        public object UI_NewTrack(BasePlayer player, string name) => (bool)(NewTrackInternal(player, name) ? true : false);
        private bool NewTrackInternal(BasePlayer player, string name)
        {
            if (!ValidateAdmin(player)) return false;
            if (string.IsNullOrEmpty(name)) { player.ChatMessage("Track name required."); return false; }
            if (tracks.ContainsKey(name)) { player.ChatMessage("Track already exists."); return false; }

            var t = new TrackDefinition
            {
                Name = name,
                StartPosition = player.transform.position,
                StartYaw = player.transform.rotation.eulerAngles.y,
                FinishPosition = player.transform.position + player.transform.forward * 30f,
                FinishRadius = 12f,
                Laps = 1,
                FinishColor = config?.DefaultFinishColor ?? "0 1 0 0.8",
                Designer = player.displayName
            };
            tracks[name] = t;
            SaveDataFiles();

            var st = GetOrCreateEditorState(player);
            st.Active = true;
            st.SelectedTrack = t;
            st.SelectedType = EditorSelectType.None;
            st.SelectedCheckpointIndex = -1;
            st.SelectedBoostIndex = -1;

            if (config?.VisualsEnabled ?? false) { DestroyTrackMarkers(); SpawnTrackMarkers(t); }

            player.ChatMessage($"New track '{name}' created and selected.");
            return true;
        }

        public object UI_RenameTrack(BasePlayer player, string newName)
        {
            if (!ValidateAdmin(player)) return false;
            var st = GetOrCreateEditorState(player);
            if (st.SelectedTrack == null) { player.ChatMessage("No track selected."); return false; }
            if (string.IsNullOrWhiteSpace(newName)) { player.ChatMessage("New name required."); return false; }
            if (tracks.ContainsKey(newName)) { player.ChatMessage("A track with that name already exists."); return false; }

            string oldName = st.SelectedTrack.Name;
            var track = st.SelectedTrack;
            tracks.Remove(oldName);
            track.Name = newName;
            tracks[newName] = track;
            SaveDataFiles();
            player.ChatMessage($"Track renamed '{oldName}' -> '{newName}'.");
            return true;
        }

        public object UI_DeleteTrack(BasePlayer player, string name)
        {
            if (!ValidateAdmin(player)) return false;
            string target = name;
            var st = GetOrCreateEditorState(player);
            if (string.IsNullOrEmpty(target))
                target = st.SelectedTrack?.Name ?? "";
            if (string.IsNullOrEmpty(target) || !tracks.ContainsKey(target)) { player.ChatMessage("Track not found."); return false; }

            tracks.Remove(target);
            SaveDataFiles();
            if (st.SelectedTrack != null && st.SelectedTrack.Name.Equals(target, StringComparison.OrdinalIgnoreCase))
            {
                st.SelectedTrack = null;
                st.SelectedType = EditorSelectType.None;
                st.SelectedCheckpointIndex = -1;
                st.SelectedBoostIndex = -1;
            }
            DestroyTrackMarkers();
            player.ChatMessage($"Track '{target}' deleted.");
            return true;
        }

        public object UI_SaveTrack(BasePlayer player)
        {
            if (!ValidateAdmin(player)) return false;
            var st = GetOrCreateEditorState(player);
            if (st.SelectedTrack == null) { player.ChatMessage("No track selected."); return false; }
            tracks[st.SelectedTrack.Name] = st.SelectedTrack;
            SaveDataFiles();
            player.ChatMessage("Track saved.");
            return true;
        }

        public object UI_ReloadMarkers(BasePlayer player)
        {
            if (!ValidateAdmin(player)) return false;
            var st = GetOrCreateEditorState(player);
            if (st.SelectedTrack == null) { player.ChatMessage("No track selected."); return false; }
            DestroyTrackMarkers();
            SpawnTrackMarkers(st.SelectedTrack);
            player.ChatMessage("Markers reloaded.");
            return true;
        }

        public object UI_SelectTrack(BasePlayer player, string trackName)
        {
            if (!ValidateAdmin(player)) return false;
            if (!tracks.TryGetValue(trackName, out var t)) { player.ChatMessage("Track not found."); return false; }

            var st = GetOrCreateEditorState(player);
            st.Active = true;
            st.SelectedTrack = t;
            st.SelectedType = EditorSelectType.None;
            st.SelectedCheckpointIndex = -1;
            st.SelectedBoostIndex = -1;

            if (config?.VisualsEnabled ?? false) { DestroyTrackMarkers(); SpawnTrackMarkers(t); }
            player.ChatMessage($"Selected track '{t.Name}'.");
            return true;
        }

        public object UI_SelectStart(BasePlayer player)
        {
            if (!ValidateAdmin(player)) return false;
            var st = GetOrCreateEditorState(player);
            if (st.SelectedTrack == null) { player.ChatMessage("No track selected."); return false; }
            st.SelectedType = EditorSelectType.Start;
            st.SelectedCheckpointIndex = -1;
            st.SelectedBoostIndex = -1;
            return true;
        }

        public object UI_SelectFinish(BasePlayer player)
        {
            if (!ValidateAdmin(player)) return false;
            var st = GetOrCreateEditorState(player);
            if (st.SelectedTrack == null) { player.ChatMessage("No track selected."); return false; }
            st.SelectedType = EditorSelectType.Finish;
            st.SelectedCheckpointIndex = -1;
            st.SelectedBoostIndex = -1;
            return true;
        }

        public object UI_MoveSelectedToAim(BasePlayer player)
        {
            if (!ValidateAdmin(player)) return false;
            var st = GetOrCreateEditorState(player);
            if (st.SelectedTrack == null || st.SelectedType == EditorSelectType.None) { player.ChatMessage("Nothing selected."); return false; }
            if (!RaycastGroundOrWater(player, out var pos)) { player.ChatMessage("Aim at ground/water."); return false; }
            MoveSelectedMarker(st, pos);
            RefreshTrackMarkers(st.SelectedTrack);
            player.ChatMessage("Moved to aim.");
            return true;
        }

        public object UI_SetSelectedRadius(BasePlayer player, float radius)
        {
            if (!ValidateAdmin(player)) return false;
            var st = GetOrCreateEditorState(player);
            if (st.SelectedTrack == null || st.SelectedType == EditorSelectType.None) return false;
            radius = Mathf.Max(0.1f, radius);
            switch (st.SelectedType)
            {
                case EditorSelectType.Finish: st.SelectedTrack.FinishRadius = radius; break;
                case EditorSelectType.Checkpoint:
                    if (st.SelectedCheckpointIndex < 0 || st.SelectedCheckpointIndex >= st.SelectedTrack.Checkpoints.Count) return false;
                    st.SelectedTrack.Checkpoints[st.SelectedCheckpointIndex].Radius = radius; break;
                case EditorSelectType.Boost:
                    if (st.SelectedBoostIndex < 0 || st.SelectedBoostIndex >= st.SelectedTrack.BoostZones.Count) return false;
                    st.SelectedTrack.BoostZones[st.SelectedBoostIndex].Radius = radius; break;
                default: return false;
            }
            SaveDataFiles();
            RefreshTrackMarkers(st.SelectedTrack);
            return true;
        }

        public object UI_AdjustSelectedRadius(BasePlayer player, float delta)
        {
            if (!ValidateAdmin(player)) return false;
            var st = GetOrCreateEditorState(player);
            if (st.SelectedTrack == null) return false;
            switch (st.SelectedType)
            {
                case EditorSelectType.Finish:
                    st.SelectedTrack.FinishRadius = Mathf.Max(0.1f, st.SelectedTrack.FinishRadius + delta); break;
                case EditorSelectType.Checkpoint:
                    if (st.SelectedCheckpointIndex < 0 || st.SelectedCheckpointIndex >= st.SelectedTrack.Checkpoints.Count) return false;
                    st.SelectedTrack.Checkpoints[st.SelectedCheckpointIndex].Radius =
                        Mathf.Max(0.1f, st.SelectedTrack.Checkpoints[st.SelectedCheckpointIndex].Radius + delta);
                    break;
                case EditorSelectType.Boost:
                    if (st.SelectedBoostIndex < 0 || st.SelectedBoostIndex >= st.SelectedTrack.BoostZones.Count) return false;
                    st.SelectedTrack.BoostZones[st.SelectedBoostIndex].Radius =
                        Mathf.Max(0.1f, st.SelectedTrack.BoostZones[st.SelectedBoostIndex].Radius + delta);
                    break;
                default: return false;
            }
            SaveDataFiles();
            RefreshTrackMarkers(st.SelectedTrack);
            return true;
        }

        public object UI_SetSelectedColor(BasePlayer player, string color)
        {
            if (!ValidateAdmin(player)) return false;
            if (string.IsNullOrEmpty(color)) return false;
            var st = GetOrCreateEditorState(player);
            if (st.SelectedTrack == null || st.SelectedType == EditorSelectType.None) return false;
            switch (st.SelectedType)
            {
                case EditorSelectType.Finish: st.SelectedTrack.FinishColor = color; break;
                case EditorSelectType.Checkpoint:
                    if (st.SelectedCheckpointIndex < 0 || st.SelectedCheckpointIndex >= st.SelectedTrack.Checkpoints.Count) return false;
                    st.SelectedTrack.Checkpoints[st.SelectedCheckpointIndex].Color = color; break;
                case EditorSelectType.Boost:
                    if (st.SelectedBoostIndex < 0 || st.SelectedBoostIndex >= st.SelectedTrack.BoostZones.Count) return false;
                    st.SelectedTrack.BoostZones[st.SelectedBoostIndex].Color = color; break;
                default: return false;
            }
            SaveDataFiles();
            RefreshTrackMarkers(st.SelectedTrack);
            return true;
        }

        public object UI_AddCheckpointAtAim(BasePlayer player, float radius, int insertIndex = -1)
        {
            if (!ValidateAdmin(player)) return false;
            var st = GetOrCreateEditorState(player);
            if (st.SelectedTrack == null) return false;
            if (!RaycastGroundOrWater(player, out var pos)) return false;
            var cp = new Checkpoint
            {
                Position = pos,
                Radius = radius > 0 ? radius : 10f,
                Color = config?.DefaultCheckpointColor ?? "0 0.8 1 0.8"
            };
            if (insertIndex >= 0 && insertIndex <= st.SelectedTrack.Checkpoints.Count)
                st.SelectedTrack.Checkpoints.Insert(insertIndex, cp);
            else
                st.SelectedTrack.Checkpoints.Add(cp);
            SaveDataFiles();
            RefreshTrackMarkers(st.SelectedTrack);
            return true;
        }

        public object UI_AddBoostAtAim(BasePlayer player, float strength, float duration, float radius)
        {
            if (!ValidateAdmin(player)) return false;
            var st = GetOrCreateEditorState(player);
            if (st.SelectedTrack == null) return false;
            if (!RaycastGroundOrWater(player, out var pos)) return false;
            var bz = new BoostZone
            {
                Position = pos,
                Radius = Mathf.Max(1f, radius > 0 ? radius : 6f),
                StrengthMultiplier = Mathf.Max(0.1f, strength > 0 ? strength : 1.5f),
                DurationSeconds = Mathf.Max(0.1f, duration > 0 ? duration : (config?.BoostDurationSeconds ?? 1.5f)),
                Color = config?.DefaultBoostColor ?? "1 0.8 0 0.8"
            };
            st.SelectedTrack.BoostZones.Add(bz);
            SaveDataFiles();
            RefreshTrackMarkers(st.SelectedTrack);
            return true;
        }

        public object UI_DeleteSelected(BasePlayer player)
        {
            if (!ValidateAdmin(player)) return false;
            var st = GetOrCreateEditorState(player);
            if (st.SelectedTrack == null) return false;
            bool changed = false;
            switch (st.SelectedType)
            {
                case EditorSelectType.Checkpoint:
                    if (st.SelectedCheckpointIndex >= 0 && st.SelectedCheckpointIndex < st.SelectedTrack.Checkpoints.Count)
                    {
                        st.SelectedTrack.Checkpoints.RemoveAt(st.SelectedCheckpointIndex);
                        st.SelectedCheckpointIndex = -1;
                        st.SelectedType = EditorSelectType.None;
                        changed = true;
                    }
                    break;
                case EditorSelectType.Boost:
                    if (st.SelectedBoostIndex >= 0 && st.SelectedBoostIndex < st.SelectedTrack.BoostZones.Count)
                    {
                        st.SelectedTrack.BoostZones.RemoveAt(st.SelectedBoostIndex);
                        st.SelectedBoostIndex = -1;
                        st.SelectedType = EditorSelectType.None;
                        changed = true;
                    }
                    break;
            }
            if (changed)
            {
                SaveDataFiles();
                RefreshTrackMarkers(st.SelectedTrack);
                return true;
            }
            return false;
        }

        public object UI_SetTrackLaps(BasePlayer player, int laps)
        {
            if (!ValidateAdmin(player)) return false;
            var st = GetOrCreateEditorState(player);
            if (st.SelectedTrack == null) return false;
            st.SelectedTrack.Laps = Mathf.Clamp(laps, 1, 50);
            SaveDataFiles();
            player.ChatMessage($"Laps set to {st.SelectedTrack.Laps}");
            return true;
        }

        #endregion

        #region Voting Chat Commands

        private void HandleVote(BasePlayer player, RaceMode mode)
        {
            if (currentRace == null || currentRace.State != RaceState.Voting)
            {
                SendReply(player, "No active vote.");
                return;
            }
            if (!currentRace.Participants.ContainsKey(player.userID))
            {
                SendReply(player, "You are not in the race.");
                return;
            }
            currentRace.Votes[player.userID] = mode;
            SendReply(player, $"You voted for {mode} race.");
            int totalParticipants = currentRace.Participants.Count;
            int votesCast = currentRace.Votes.Count;
            if (votesCast >= totalParticipants)
            {
                currentRace.VoteTimer?.Destroy();
                FinishVoteAndProceed();
            }
        }

        #endregion

        #region Chat Commands

        [ChatCommand("hydro")]
        private void CmdHydro(BasePlayer player, string cmd, string[] args)
        {
            if (args == null || args.Length == 0)
            {
                ShowHelp(player);
                return;
            }

            string sub = args[0].ToLowerInvariant();
            switch (sub)
            {
                case "vote":
                    if (args.Length < 2)
                    {
                        SendReply(player, "Usage: /hydro vote normal|battle");
                        return;
                    }
                    var choice = args[1].ToLowerInvariant();
                    if (choice == "normal") HandleVote(player, RaceMode.Normal);
                    else if (choice == "battle") HandleVote(player, RaceMode.Battle);
                    else SendReply(player, "Invalid vote. Use normal or battle.");
                    break;

                case "track.new": CmdTrackNew(player, args); break;
                case "track.addcp": CmdTrackAddCp(player, args); break;
                case "track.setstart": CmdTrackSetStartSmart(player, args); break;
                case "track.setfinish": CmdTrackSetFinishSmart(player, args); break;
                case "track.addboost": CmdTrackAddBoost(player, args); break;
                case "track.save": CmdTrackSaveSmart(player, args); break;
                case "track.list": CmdTrackList(player, args); break;
                case "track.info": CmdTrackInfo(player, args); break;
                case "track.delete": CmdTrackDelete(player, args); break;

                case "track.select": CmdTrackSelect(player, args); break;
                case "track.rename": CmdTrackRename(player, args); break;
                case "track.reload": CmdTrackReload(player, args); break;
                case "track.move": CmdTrackMove(player, args); break;
                case "track.addcp.aim": CmdTrackAddCpAim(player, args); break;
                case "track.addboost.aim": CmdTrackAddBoostAim(player, args); break;
                case "track.laps": CmdTrackLaps(player, args); break;
                case "track.setradius": CmdTrackSetRadius(player, args); break;
                case "track.radius": CmdTrackRadiusStep(player, args); break;
                case "track.setcolor": CmdTrackSetColor(player, args); break;
                case "track.delete.selected": CmdTrackDeleteSelected(player, args); break;

                case "race.start": CmdRaceStart(player, args); break;
                case "race.join": CmdRaceJoin(player, args); break;
                case "race.leave": CmdRaceLeave(player, args); break;
                case "time": CmdTimeTrialCommand(player, args); break;
                case "stats": CmdStats(player, args); break;

                default:
                    ShowHelp(player);
                    break;
            }
        }

        private void ShowHelp(BasePlayer player)
        {
            SendReply(player, "HydroRust Commands:");
            if (permission.UserHasPermission(player.UserIDString, PERM_ADMIN))
            {
                SendReply(player, "Editor:");
                SendReply(player, "/hydro track.select <name>");
                SendReply(player, "/hydro track.rename <newName>");
                SendReply(player, "/hydro track.laps <n>");
                SendReply(player, "/hydro track.move");
                SendReply(player, "/hydro track.addcp.aim [radius] [insertIndex]");
                SendReply(player, "/hydro track.addboost.aim [strength] [duration] [radius]");
                SendReply(player, "/hydro track.setradius <r>");
                SendReply(player, "/hydro track.radius +/-");
                SendReply(player, "/hydro track.setcolor <r> <g> <b> <a>");
                SendReply(player, "/hydro track.save");
                SendReply(player, "/hydro track.reload");
                SendReply(player, "/hydro track.delete.selected");
                SendReply(player, "Legacy:");
                SendReply(player, "/hydro track.new <name>");
                SendReply(player, "/hydro track.addcp [radius]");
                SendReply(player, "/hydro track.setstart");
                SendReply(player, "/hydro track.setfinish");
                SendReply(player, "/hydro track.addboost <track> [strength] [duration] [radius]");
                SendReply(player, "/hydro track.list");
                SendReply(player, "/hydro track.info <name>");
                SendReply(player, "/hydro track.delete <name>");
                SendReply(player, "/hydro race.start <track>");
            }
            SendReply(player, "/hydro race.join   /hydro race.leave");
            SendReply(player, "/hydro time <track>  /hydro stats [playerName]");
            SendReply(player, "Voting: /hydro vote normal | battle");
        }

        private void CmdTrackSetStartSmart(BasePlayer player, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PERM_ADMIN)) { SendReply(player, "No permission."); return; }
            var st = GetOrCreateEditorState(player);
            if (st.SelectedTrack != null)
            {
                UI_SelectStart(player);
                UI_MoveSelectedToAim(player);
                SendReply(player, "Start set @aim.");
                return;
            }
            CmdTrackSetStart(player, args);
        }

        private void CmdTrackSetFinishSmart(BasePlayer player, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PERM_ADMIN)) { SendReply(player, "No permission."); return; }
            var st = GetOrCreateEditorState(player);
            if (st.SelectedTrack != null)
            {
                UI_SelectFinish(player);
                UI_MoveSelectedToAim(player);
                SendReply(player, "Finish set @aim.");
                return;
            }
            CmdTrackSetFinish(player, args);
        }

        private void CmdTrackSaveSmart(BasePlayer player, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PERM_ADMIN)) { SendReply(player, "No permission."); return; }
            var st = GetOrCreateEditorState(player);
            if (st.SelectedTrack != null)
            {
                UI_SaveTrack(player);
                return;
            }
            CmdTrackSave(player, args);
        }

        private void CmdTrackSelect(BasePlayer player, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PERM_ADMIN)) { SendReply(player, "No permission."); return; }
            if (args.Length < 2) { SendReply(player, "Usage: /hydro track.select <name>"); return; }
            if (!tracks.ContainsKey(args[1])) { SendReply(player, $"Track '{args[1]}' not found."); return; }
            UI_SelectTrack(player, args[1]);
        }

        private void CmdTrackRename(BasePlayer player, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PERM_ADMIN)) { SendReply(player, "No permission."); return; }
            if (args.Length < 2) { SendReply(player, "Usage: /hydro track.rename <newName>"); return; }
            UI_RenameTrack(player, args[1]);
        }

        private void CmdTrackReload(BasePlayer player, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PERM_ADMIN)) { SendReply(player, "No permission."); return; }
            UI_ReloadMarkers(player);
        }

        private void CmdTrackMove(BasePlayer player, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PERM_ADMIN)) { SendReply(player, "No permission."); return; }
            UI_MoveSelectedToAim(player);
        }

        private void CmdTrackAddCpAim(BasePlayer player, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PERM_ADMIN)) { SendReply(player, "No permission."); return; }
            float radius = 10f; int insertIndex = -1;
            if (args.Length >= 2) float.TryParse(args[1], out radius);
            if (args.Length >= 3) int.TryParse(args[2], out insertIndex);
            UI_AddCheckpointAtAim(player, radius, insertIndex);
        }

        private void CmdTrackAddBoostAim(BasePlayer player, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PERM_ADMIN)) { SendReply(player, "No permission."); return; }
            float strength = 1.5f, duration = config?.BoostDurationSeconds ?? 1.5f, radius = 6f;
            if (args.Length >= 2) float.TryParse(args[1], out strength);
            if (args.Length >= 3) float.TryParse(args[2], out duration);
            if (args.Length >= 4) float.TryParse(args[3], out radius);
            UI_AddBoostAtAim(player, strength, duration, radius);
        }

        private void CmdTrackLaps(BasePlayer player, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PERM_ADMIN)) { SendReply(player, "No permission."); return; }
            if (args.Length < 2 || !int.TryParse(args[1], out var laps)) { SendReply(player, "Usage: /hydro track.laps <n>"); return; }
            UI_SetTrackLaps(player, laps);
        }

        private void CmdTrackSetRadius(BasePlayer player, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PERM_ADMIN)) { SendReply(player, "No permission."); return; }
            if (args.Length < 2 || !float.TryParse(args[1], out var r)) { SendReply(player, "Usage: /hydro track.setradius <r>"); return; }
            UI_SetSelectedRadius(player, r);
        }

        private void CmdTrackRadiusStep(BasePlayer player, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PERM_ADMIN)) { SendReply(player, "No permission."); return; }
            if (args.Length < 2) { SendReply(player, "Usage: /hydro track.radius +/-"); return; }
            float delta = (args[1] == "-" ? -1f : 1f) * EditorRadiusStep;
            UI_AdjustSelectedRadius(player, delta);
        }

        private void CmdTrackSetColor(BasePlayer player, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PERM_ADMIN)) { SendReply(player, "No permission."); return; }
            if (args.Length < 5)
            {
                SendReply(player, "Usage: /hydro track.setcolor <r> <g> <b> <a>");
                return;
            }
            string color = $"{args[1]} {args[2]} {args[3]} {args[4]}";
            UI_SetSelectedColor(player, color);
        }

        private void CmdTrackDeleteSelected(BasePlayer player, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PERM_ADMIN)) { SendReply(player, "No permission."); return; }
            UI_DeleteSelected(player);
        }

        private void CmdTrackNew(BasePlayer player, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PERM_ADMIN)) { SendReply(player, "No permission."); return; }
            if (args.Length < 2) { SendReply(player, "Usage: /hydro track.new <name>"); return; }
            if (tracks.ContainsKey(args[1])) { SendReply(player, "Track exists."); return; }
            NewTrackInternal(player, args[1]);
        }
        private void CmdTrackAddCp(BasePlayer player, string[] args) => SendReply(player, "Legacy builder not active (use track.addcp.aim).");
        private void CmdTrackSetStart(BasePlayer player, string[] args) => SendReply(player, "Legacy builder not active (use track.setstart and track.move).");
        private void CmdTrackSetFinish(BasePlayer player, string[] args) => SendReply(player, "Legacy builder not active (use track.setfinish and track.move).");
        private void CmdTrackAddBoost(BasePlayer player, string[] args) => SendReply(player, "Legacy builder not active (use track.addboost.aim).");
        private void CmdTrackSave(BasePlayer player, string[] args) => SendReply(player, "Legacy builder not active (use track.save).");

        private void CmdTrackList(BasePlayer player, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PERM_ADMIN)) { SendReply(player, "No permission."); return; }
            if (tracks.Count == 0) { SendReply(player, "No tracks."); return; }
            foreach (var kvp in tracks)
                SendReply(player, $"- {kvp.Key} | CP:{kvp.Value.Checkpoints.Count} Boosts:{kvp.Value.BoostZones.Count} Laps:{kvp.Value.Laps}");
        }

        private void CmdTrackInfo(BasePlayer player, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PERM_ADMIN)) { SendReply(player, "No permission."); return; }
            if (args.Length < 2) { SendReply(player, "Usage: /hydro track.info <name>"); return; }
            if (!tracks.TryGetValue(args[1], out var track))
            {
                SendReply(player, $"Track '{args[1]}' not found.");
                return;
            }
            SendReply(player, $"Track '{track.Name}': Laps {track.Laps} CP {track.Checkpoints.Count} Boosts {track.BoostZones.Count}");
        }

        private void CmdTrackDelete(BasePlayer player, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PERM_ADMIN)) { SendReply(player, "No permission."); return; }
            if (args.Length < 2) { SendReply(player, "Usage: /hydro track.delete <name>"); return; }
            if (!tracks.Remove(args[1]))
            {
                SendReply(player, $"Track '{args[1]}' not found.");
                return;
            }
            SaveDataFiles();
            DestroyTrackMarkers();
            SendReply(player, $"Track '{args[1]}' deleted.");
        }

        private void CmdRaceStart(BasePlayer player, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PERM_ADMIN)) { SendReply(player, "No permission."); return; }
            if (args.Length < 2) { SendReply(player, "Usage: /hydro race.start <track>"); return; }
            if (!tracks.TryGetValue(args[1], out var track))
            {
                SendReply(player, $"Track '{args[1]}' not found.");
                return;
            }
            if (currentRace != null && currentRace.State != RaceState.Finished)
            {
                SendReply(player, "Race already active.");
                return;
            }
            currentRace = new RaceSession
            {
                Track = track,
                State = RaceState.Staging
            };
            BroadcastToAdmins($"Manual race staged on {track.Name}. Players /hydro race.join. Begin vote.");
            StartBattleVote();
        }

        private void CmdRaceJoin(BasePlayer player, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PERM_PLAYER)) { SendReply(player, "No permission."); return; }

            if (currentRace == null ||
                (currentRace.State != RaceState.Staging && currentRace.State != RaceState.Voting && currentRace.State != RaceState.Countdown))
            {
                SendReply(player, "No race staged.");
                return;
            }

            if (currentRace.Participants.ContainsKey(player.userID))
            {
                SendReply(player, "Already joined.");
                return;
            }

            currentRace.Participants[player.userID] = new RaceParticipant { Player = player };
            SendReply(player, $"Joined race '{currentRace.Track.Name}'.");
        }

        private void CmdRaceLeave(BasePlayer player, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PERM_PLAYER)) { SendReply(player, "No permission."); return; }
            if (currentRace == null) { SendReply(player, "No race."); return; }

            if (currentRace.Participants.Remove(player.userID))
                SendReply(player, "Left race.");
            else
                SendReply(player, "Not in race.");
        }

        private void CmdTimeTrialCommand(BasePlayer player, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PERM_PLAYER)) { SendReply(player, "No permission."); return; }
            if (args.Length < 2) { SendReply(player, "Usage: /hydro time <track>"); return; }

            if (!tracks.TryGetValue(args[1], out var track))
            {
                SendReply(player, $"Track '{args[1]}' not found.");
                return;
            }
            StartTimeTrial(player, track);
        }

        private void CmdStats(BasePlayer player, string[] args)
        {
            BasePlayer target = player;
            if (args.Length >= 2)
            {
                var found = BasePlayer.Find(args[1]) ?? BasePlayer.FindSleeping(args[1]);
                if (found != null) target = found;
            }

            if (!playerStats.TryGetValue(target.userID, out var stats) || stats.TrackStats.Count == 0)
            {
                SendReply(player, $"{target.displayName} has no stats.");
                return;
            }

            SendReply(player, $"Stats for {target.displayName}:");
            foreach (var kvp in stats.TrackStats)
            {
                var t = kvp.Value;
                string best = t.BestTimeSeconds > 0f ? $"{t.BestTimeSeconds:0.00}s" : "--";
                SendReply(player, $"- {kvp.Key}: Best {best}, Wins {t.Wins}, Finished {t.CompletedRaces}");
            }
        }

        #endregion

        #region Race / Time Trial Ticks

        private void RaceTick()
        {
            if (currentRace == null || currentRace.State != RaceState.Running) return;
            float delta = 0.1f;
            currentRace.TimeSinceStart += delta;

            if (currentRace.TimeSinceStart > (config?.RaceTimeoutSeconds ?? 600))
            {
                BroadcastToRace("Race timed out!");
                FinishRace();
                return;
            }

            foreach (var kvp in currentRace.Participants.ToList())
            {
                var part = kvp.Value;
                var player = part.Player;
                if (player == null || !player.IsConnected) continue;
                part.TimeElapsed += delta;

                var boat = part.Boat;
                if (boat == null || boat.IsDestroyed)
                    part.Boat = boat = GetPlayerBoat(player);

                HandleAirtimeAndWipeout(currentRace.Track, part, boat, delta);
                if (boat == null || boat.IsDestroyed) continue;

                TickBoosts(currentRace.Track, boat, part.ActiveBoosts, part.BoostCooldowns, delta, player);
                HandleCheckpointLogic(currentRace.Track, part);

                if (!part.Finished && HasFinishedTrack(currentRace.Track, part))
                {
                    part.Finished = true;
                    part.FinishTime = part.TimeElapsed;
                    BroadcastToRace($"{player.displayName} finished in {part.FinishTime:0.00}s");
                    UpdatePlayerStatsOnFinish(player.userID, currentRace.Track.Name, part.FinishTime);

                    if (AllRaceParticipantsFinished() && currentRace.State == RaceState.Running)
                    {
                        FinishRace();
                        break;
                    }
                }
            }
        }

        private void TimeTrialTick()
        {
            if (activeTimeTrials.Count == 0) return;
            float delta = 0.1f;

            foreach (var kvp in activeTimeTrials.ToList())
            {
                var session = kvp.Value;
                var player = session.Player;
                if (player == null || !player.IsConnected)
                {
                    activeTimeTrials.Remove(kvp.Key);
                    continue;
                }

                session.TimeElapsed += delta;

                var boat = session.Boat;
                if (boat == null || boat.IsDestroyed)
                    session.Boat = boat = GetPlayerBoat(player);

                HandleAirtimeAndWipeout(session.Track, session, boat, delta);
                if (boat == null || boat.IsDestroyed) continue;

                TickBoosts(session.Track, boat, session.ActiveBoosts, session.BoostCooldowns, delta, player);
                HandleCheckpointLogic(session.Track, session);

                if (!session.Finished && HasFinishedTrack(session.Track, session))
                {
                    session.Finished = true;
                    SendReply(player, $"Time Trial finished in {session.TimeElapsed:0.00}s");
                    UpdatePlayerStatsOnFinish(player.userID, session.Track.Name, session.TimeElapsed);
                    activeTimeTrials.Remove(kvp.Key);
                }
            }
        }

        private void ControlTick()
        {
            EnsureConfig();
            if (config == null) return;

            if (currentRace != null && currentRace.State == RaceState.Running)
            {
                foreach (var kvp in currentRace.Participants)
                {
                    var part = kvp.Value;
                    var player = part.Player;
                    if (player == null || !player.IsConnected) continue;

                    var input = player.serverInput;
                    if (input != null && input.WasJustPressed(BUTTON.RELOAD))
                    {
                        ManualRespawnAtLastCheckpoint(player);
                        continue;
                    }

                    if (part.Boat == null || part.Boat.IsDestroyed) continue;
                    ApplyArcadeControls(player, part.Boat, 0.05f);
                }
            }

            foreach (var kvp in activeTimeTrials)
            {
                var session = kvp.Value;
                var player = session.Player;
                if (player == null || !player.IsConnected) continue;

                var input = player.serverInput;
                if (input != null && input.WasJustPressed(BUTTON.RELOAD))
                {
                    ManualRespawnAtLastCheckpoint(player);
                    continue;
                }

                if (session.Boat == null || session.Boat.IsDestroyed) continue;
                ApplyArcadeControls(player, session.Boat, 0.05f);
            }
        }

        private void EditorTick()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (player == null) continue;
                if (!editorStates.TryGetValue(player.userID, out var state) || !state.Active || state.SelectedTrack == null)
                    continue;

                var input = player.serverInput;
                if (input == null) continue;

                bool click = input.WasJustPressed(BUTTON.RELOAD);
                if (!click) continue;

                if (!state.MovingMarker)
                {
                    var marker = RaycastMarker(player);
                    if (marker != null)
                    {
                        if (marker.IsFinish)
                        {
                            state.SelectedType = EditorSelectType.Finish;
                            state.SelectedCheckpointIndex = -1;
                            state.SelectedBoostIndex = -1;
                        }
                        else if (marker.CheckpointIndex >= 0)
                        {
                            state.SelectedType = EditorSelectType.Checkpoint;
                            state.SelectedCheckpointIndex = marker.CheckpointIndex;
                            state.SelectedBoostIndex = -1;
                        }
                        else if (marker.BoostIndex >= 0)
                        {
                            state.SelectedType = EditorSelectType.Boost;
                            state.SelectedBoostIndex = marker.BoostIndex;
                            state.SelectedCheckpointIndex = -1;
                        }
                        state.MovingMarker = true;
                        player.ChatMessage("Marker selected. Aim new location and press RELOAD again.");
                    }
                }
                else
                {
                    if (RaycastGroundOrWater(player, out var pos))
                    {
                        MoveSelectedMarker(state, pos);
                        state.MovingMarker = false;
                        player.ChatMessage("Marker moved. Save track when ready.");
                        RefreshTrackMarkers(state.SelectedTrack);
                    }
                    else player.ChatMessage("No valid surface.");
                }
            }
        }

        private void ManualRespawnAtLastCheckpoint(BasePlayer player)
        {
            if (player == null || !player.IsConnected) return;

            if (currentRace != null && currentRace.State == RaceState.Running &&
                currentRace.Participants.TryGetValue(player.userID, out var part))
            {
                RespawnParticipantAtLastCheckpoint(currentRace.Track, part);
                return;
            }

            if (activeTimeTrials.TryGetValue(player.userID, out var session))
                RespawnTimeTrialAtLastCheckpoint(session.Track, session);
        }

        #endregion

        #region Arrow-Key / Radius Console Commands

        [ConsoleCommand("hydro_key_nudge_local")]
        private void CCKeyNudgeLocal(ConsoleSystem.Arg arg)
        {
            var player = arg.Player(); if (player == null) return;
            float lx = arg.GetFloat(0);
            float lz = arg.GetFloat(1);
            float step = arg.GetFloat(2);
            Vector3 forward = player.eyes.BodyForward(); forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f) forward = player.transform.forward;
            forward.y = 0f; forward.Normalize();
            Vector3 right = new Vector3(forward.z, 0f, -forward.x);
            var st = GetOrCreateEditorState(player);
            if (st.SelectedTrack == null || st.SelectedType == EditorSelectType.None) return;
            Vector3 delta = right * (lx * step) + forward * (lz * step);
            switch (st.SelectedType)
            {
                case EditorSelectType.Start: st.SelectedTrack.StartPosition += delta; break;
                case EditorSelectType.Finish: st.SelectedTrack.FinishPosition += delta; break;
                case EditorSelectType.Checkpoint:
                    if (st.SelectedCheckpointIndex >= 0 && st.SelectedCheckpointIndex < st.SelectedTrack.Checkpoints.Count)
                        st.SelectedTrack.Checkpoints[st.SelectedCheckpointIndex].Position += delta; break;
                case EditorSelectType.Boost:
                    if (st.SelectedBoostIndex >= 0 && st.SelectedBoostIndex < st.SelectedTrack.BoostZones.Count)
                        st.SelectedTrack.BoostZones[st.SelectedBoostIndex].Position += delta; break;
            }
            SaveDataFiles();
            RefreshTrackMarkers(st.SelectedTrack);
        }

        [ConsoleCommand("hydro_key_radius")]
        private void CCKeyRadius(ConsoleSystem.Arg arg)
        {
            var player = arg.Player(); if (player == null) return;
            float delta = arg.GetFloat(0);
            UI_AdjustSelectedRadius(player, delta);
        }

        #endregion
    }
}