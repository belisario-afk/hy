using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("HydroUI", "belisario-afk + copilot", "2.4.0")]
    [Description("Fullscreen welcome, menu toggle, player quick panel, full Track Editor wired to /hydro chat, with input-focus fixes and race-mode HUD tag.")]
    public class HydroUI : RustPlugin
    {
        #region Plugin References

        [PluginReference] private Plugin HydroRust;
        [PluginReference] private Plugin HydroLobby;
        [PluginReference] private Plugin ImageLibrary;

        #endregion

        #region Constants

        private const string ROOT_START_MENU = "HydroUI.StartMenu";
        private const string ROOT_TOGGLE = "HydroUI.MenuToggle";
        private const string ROOT_ADMIN = "HydroUI.Admin";
        private const string ROOT_PLAYER = "HydroUI.PlayerPanel";
        private const string ROOT_VOTING = "HydroUI.VotingOverlay";
        private const string ROOT_COUNTDOWN = "HydroUI.CountdownOverlay";

        private const string START_HEADER = "HydroUI.Start.Header";
        private const string START_BODY = "HydroUI.Start.Body";
        private const string START_TAB_WELCOME = "HydroUI.Start.Tab.Welcome";
        private const string START_TAB_PLAY = "HydroUI.Start.Tab.Play";

        private const string ADMIN_HEADER = "HydroUI.Admin.Header";
        private const string ADMIN_SUMMARY_BAR = "HydroUI.Admin.SummaryBar";
        private const string ADMIN_SUMMARY_BAR_INNER = "HydroUI.Admin.SummaryBar.Inner";
        private const string ADMIN_TRACK_LIST = "HydroUI.Admin.TrackList";
        private const string ADMIN_TRACKLIST_BODY = "HydroUI.Admin.TrackList.Body";
        private const string ADMIN_INSPECTOR = "HydroUI.Admin.Inspector";
        private const string ADMIN_INSPECTOR_BODY = "HydroUI.Admin.Inspector.Body";

        private const string IMG_START_BG = "hydro_ui_start_bg";
        private const string IMG_TRACK_DEFAULT = "hydro_ui_track_default";

        private const float TICK_HUD = 0.10f;
        private const float TICK_ANIM = 0.05f;
        private const float TICK_LOCK = 0.10f;
        private const float TICK_EDITOR_REFRESH = 0.50f;

        private const int TRACKS_PER_PAGE = 16;

        #endregion

        #region Config

        private class Theme
        {
            [JsonProperty] public string Name = "HydroBlue";
            [JsonProperty] public string Primary = "0.0 0.75 1.0 0.95";
            [JsonProperty] public string Accent = "0.2 0.9 1.0 0.95";
            [JsonProperty] public string Background = "0.02 0.05 0.08 0.92";
            [JsonProperty] public string Panel = "0.07 0.10 0.15 0.96";
            [JsonProperty] public string Text = "1 1 1 1";
            [JsonProperty] public string MutedText = "0.85 0.92 1 0.90";
            [JsonProperty] public string Progress = "0.0 0.75 1.0 0.95";
            [JsonProperty] public string Boost = "1.0 0.7 0.1 0.95";
            [JsonProperty] public string Alert = "1.0 0.25 0.25 0.95";
            [JsonProperty] public string Good = "0.25 1.0 0.4 0.95";
            [JsonProperty] public string Battle = "1.0 0.3 0.3 0.95";
        }

        private class UITextures
        {
            [JsonProperty] public string StartBackgroundUrl = "https://i.imgur.com/z7X6G8R.png";
            [JsonProperty] public string TrackDefaultUrl = "https://i.imgur.com/GXq8mZZ.png";
            [JsonProperty] public string LogoUrl = "";
        }

        private class ChatCmds
        {
            [JsonProperty] public string NewTrack = "/hydro track.new {name}";
            [JsonProperty] public string SelectTrack = "/hydro track.select {name}";
            [JsonProperty] public string Save = "/hydro track.save";
            [JsonProperty] public string Rename = "/hydro track.rename {name}";
            [JsonProperty] public string SetLaps = "/hydro track.laps {laps}";
            [JsonProperty] public string SetStart = "/hydro track.setstart";
            [JsonProperty] public string SetFinish = "/hydro track.setfinish";
            [JsonProperty] public string MoveSelectedToAim = "/hydro track.move";
            [JsonProperty] public string AddCpAim = "/hydro track.addcp.aim {radius} {index}";
            [JsonProperty] public string AddBoostAim = "/hydro track.addboost.aim {strength} {duration} {radius}";
            [JsonProperty] public string DeleteSelected = "/hydro track.delete.selected";
            [JsonProperty] public string DeleteTrackByName = "/hydro track.delete {name}";
            [JsonProperty] public string ReloadMarkers = "/hydro track.reload";
            [JsonProperty] public string RadiusPlus = "/hydro track.radius +";
            [JsonProperty] public string RadiusMinus = "/hydro track.radius -";
            [JsonProperty] public string SetRadius = "/hydro track.setradius {radius}";
            [JsonProperty] public string SetColor = "/hydro track.setcolor {color}";

            [JsonProperty] public string RaceJoin = "/hydro race.join";
            [JsonProperty] public string RaceLeave = "/hydro race.leave";
            [JsonProperty] public string Stats = "/hydro stats";

            // Voting
            [JsonProperty] public string VoteNormal = "/hydro vote normal";
            [JsonProperty] public string VoteBattle = "/hydro vote battle";
        }

        private class PluginConfig
        {
            [JsonProperty] public List<Theme> Themes = new List<Theme> { new Theme { Name = "HydroBlue" } };
            [JsonProperty] public UITextures Textures = new UITextures();
            [JsonProperty] public ChatCmds Chat = new ChatCmds();

            [JsonProperty] public float FadeInSeconds = 0.35f;
            [JsonProperty] public float FadeOutSeconds = 0.25f;
            [JsonProperty] public float TweenSpeed = 8f;

            [JsonProperty] public string StartMenuAnchorMin = "0 0";
            [JsonProperty] public string StartMenuAnchorMax = "1 1";
            [JsonProperty] public float StartHeaderHeight = 0.14f;

            [JsonProperty] public string AdminAnchorMin = "0.76 0.05";
            [JsonProperty] public string AdminAnchorMax = "0.99 0.95";

            [JsonProperty] public string MenuToggleAnchorMin = "0.96 0.46";
            [JsonProperty] public string MenuToggleAnchorMax = "0.995 0.54";

            [JsonProperty] public string PlayerPanelAnchorMin = "0.84 0.02";
            [JsonProperty] public string PlayerPanelAnchorMax = "0.995 0.16";

            [JsonProperty] public bool ShowVotingOverlay = true;
            [JsonProperty] public bool ShowCountdownOverlay = true;

            [JsonProperty] public float RadiusStep = 0.5f;
        }

        private PluginConfig config;

        protected override void LoadDefaultConfig()
        {
            config = new PluginConfig();
            SaveConfig();
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try { config = Config.ReadObject<PluginConfig>() ?? new PluginConfig(); }
            catch { PrintWarning("Invalid config; using defaults."); config = new PluginConfig(); }
            SaveConfig();
        }
        protected override void SaveConfig() => Config.WriteObject(config, true);

        #endregion

        #region Prefs & State

        private class PlayerPrefs
        {
            public ulong UserId;
            public string ThemeName;
        }

        private class HudData
        {
            public string TrackName = "";
            public int Lap;
            public int TotalLaps;
            public int Checkpoint;
            public int TotalCheckpoints;
            public float Progress01;
            public float Speed;
            public float Boost01;
            public int Position;
            public int Racers;
            public bool IsRace;
            public bool Finished;
            public float FinishTime;
            public string RaceMode = "";
            public bool Voting;
            public int VoteSecondsRemaining;
            public int CountdownSecondsRemaining;
            public int QueuedPlayers;
        }

        private class PlayerUIState
        {
            public BasePlayer Player;
            public bool StartMenuVisible;
            public int CurrentTab = 1;
            public bool AdminVisible;

            public float StartAlpha;
            public float AdminAlpha;

            public HudData Target = new HudData();
            public HudData Display = new HudData();

            public float TargetYaw;
            public float DisplayYaw;

            public bool MenuToggleAllowed;
            public bool MenuToggleVisible;

            public bool WelcomeLocked;
            public Vector3 LockPosition;

            public int TrackListPage;
            public List<string> TrackList = new List<string>();

            public string EditorTrackName = "";
            public string EditorSelectedType = "None";
            public int EditorCpIndex = -1;
            public int EditorBoostIndex = -1;
            public int EditorCpCount;
            public int EditorBoostCount;
            public int EditorLaps = 1;
            public string EditorDesigner = "Unknown";
            public string EditorImageKey = "";

            public bool AdminCursorEnabled = true;

            public bool PlayerPanelVisible;
            
            // New overlay states
            public bool VotingOverlayVisible;
            public bool CountdownOverlayVisible;
            public int LastCountdownValue = -1;
            public int LastQueueCount = -1;

            // Input debounce markers by key ("new","select","delete")
            public Dictionary<string, float> ActiveInputEditing = new Dictionary<string, float>();

            // Avoid rebuilding track list during periodic refresh
            public bool TrackListDirty;
        }

        private readonly Dictionary<ulong, PlayerPrefs> prefs = new Dictionary<ulong, PlayerPrefs>();
        private readonly Dictionary<ulong, PlayerUIState> states = new Dictionary<ulong, PlayerUIState>();
        private const string DATA_PREFS = "HydroUI_Prefs";

        private Timer hudTick;
        private Timer animTick;
        private Timer lockTick;
        private Timer editorRefreshTick;

        #endregion

        #region Images

        private void InitImages()
        {
            if (ImageLibrary == null)
            {
                PrintWarning("ImageLibrary not found; UI will use colors.");
                return;
            }
            AddImage(IMG_START_BG, config.Textures.StartBackgroundUrl);
            AddImage(IMG_TRACK_DEFAULT, config.Textures.TrackDefaultUrl);
            if (!string.IsNullOrEmpty(config.Textures.LogoUrl))
                AddImage("hydro_ui_logo", config.Textures.LogoUrl);
        }

        private void AddImage(string shortname, string url)
        {
            if (ImageLibrary == null || string.IsNullOrEmpty(url)) return;
            ImageLibrary.CallHook("AddImage", url, shortname);
        }

        private string GetImage(string shortname)
        {
            if (ImageLibrary == null) return string.Empty;
            return ImageLibrary.CallHook("GetImage", shortname) as string ?? "";
        }

        #endregion

        #region Hooks & Timers

        private void Init()
        {
            LoadConfig();
            LoadPrefs();
        }

        private void OnServerInitialized()
        {
            InitImages();
            StartTimers();
            foreach (var p in BasePlayer.activePlayerList)
            {
                EnsureState(p);
                AutoShowLockedWelcome(p);
            }
        }

        private void Unload()
        {
            StopTimers();
            DestroyAllUI();
            SavePrefs();
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            EnsureState(player);
            AutoShowLockedWelcome(player);
        }

        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            HideAll(player, false);
            states.Remove(player.userID);
        }

        private void StartTimers()
        {
            hudTick?.Destroy();
            animTick?.Destroy();
            lockTick?.Destroy();
            editorRefreshTick?.Destroy();

            hudTick = timer.Every(TICK_HUD, HudTick);
            animTick = timer.Every(TICK_ANIM, AnimTick);
            lockTick = timer.Every(TICK_LOCK, LockTick);
            editorRefreshTick = timer.Every(TICK_EDITOR_REFRESH, EditorRefreshTick);
        }

        private void StopTimers()
        {
            hudTick?.Destroy();
            animTick?.Destroy();
            lockTick?.Destroy();
            editorRefreshTick?.Destroy();
            hudTick = animTick = lockTick = editorRefreshTick = null;
        }

        private void HudTick()
        {
            foreach (var st in states.Values)
            {
                var player = st.Player;
                if (player == null || !player.IsConnected) continue;
                PullHudTarget(player, st);
                LerpHud(st, config.TweenSpeed, TICK_HUD);
                
                // Update player panel if queue count changed
                if (st.PlayerPanelVisible && st.LastQueueCount != st.Display.QueuedPlayers)
                {
                    st.LastQueueCount = st.Display.QueuedPlayers;
                    ShowPlayerPanel(player);
                }
                
                // Manage voting overlay
                if (config.ShowVotingOverlay && st.Display.Voting)
                {
                    if (!st.VotingOverlayVisible)
                    {
                        ShowVotingOverlay(player, st);
                        st.VotingOverlayVisible = true;
                    }
                }
                else if (st.VotingOverlayVisible)
                {
                    HideVotingOverlay(player);
                    st.VotingOverlayVisible = false;
                }
                
                // Manage countdown overlay
                if (config.ShowCountdownOverlay && st.Display.CountdownSecondsRemaining > 0)
                {
                    if (!st.CountdownOverlayVisible || st.LastCountdownValue != st.Display.CountdownSecondsRemaining)
                    {
                        ShowCountdownOverlay(player, st, st.Display.CountdownSecondsRemaining);
                        st.CountdownOverlayVisible = true;
                        st.LastCountdownValue = st.Display.CountdownSecondsRemaining;
                    }
                }
                else if (st.CountdownOverlayVisible)
                {
                    HideCountdownOverlay(player);
                    st.CountdownOverlayVisible = false;
                    st.LastCountdownValue = -1;
                }
            }
        }

        private void AnimTick()
        {
            foreach (var st in states.Values)
            {
                st.StartAlpha = MoveToward(st.StartAlpha, st.StartMenuVisible ? 1f : 0f, TICK_ANIM / config.FadeInSeconds);
                st.AdminAlpha = MoveToward(st.AdminAlpha, st.AdminVisible ? 1f : 0f, TICK_ANIM / config.FadeInSeconds);
            }
        }

        private void LockTick()
        {
            foreach (var st in states.Values)
            {
                var player = st.Player;
                if (player == null || !player.IsConnected) continue;
                if (!st.WelcomeLocked) continue;

                if (Vector3.Distance(player.transform.position, st.LockPosition) > 0.25f)
                {
                    player.EnsureDismounted();
                    player.Teleport(st.LockPosition);
                    player.UpdateNetworkGroup();
                    player.SendNetworkUpdateImmediate();
                }
            }
        }

        private void EditorRefreshTick()
        {
            foreach (var st in states.Values)
            {
                if (!st.AdminVisible) continue;
                var player = st.Player;
                if (player == null || !player.IsConnected) continue;

                // Only refresh summary/inspector periodically to preserve input focus in track list
                var ctxObj = HydroRust?.Call("UI_GetEditorContext", player) as IDictionary<string, object>;
                if (ctxObj != null)
                {
                    st.EditorTrackName = SafeString(ctxObj, "TrackName");
                    st.EditorSelectedType = SafeString(ctxObj, "SelectedType");
                    st.EditorCpIndex = SafeInt(ctxObj, "SelectedCheckpointIndex");
                    st.EditorBoostIndex = SafeInt(ctxObj, "SelectedBoostIndex");
                    st.EditorCpCount = SafeInt(ctxObj, "CheckpointCount");
                    st.EditorBoostCount = SafeInt(ctxObj, "BoostCount");
                    st.EditorLaps = SafeInt(ctxObj, "Laps");
                    st.EditorDesigner = SafeString(ctxObj, "Designer");
                    st.EditorImageKey = SafeString(ctxObj, "ImageKey");
                }

                UpdateSummaryBar(player, st);
                UpdateInspectorPanel(player, st);
                // Track list is not rebuilt here to avoid stealing input focus
                // It is refreshed explicitly on open/pagination and after name mutations
            }
        }

        private static float MoveToward(float current, float target, float step)
        {
            if (Math.Abs(current - target) <= step) return target;
            return current + Mathf.Sign(target - current) * step;
        }

        private void LerpHud(PlayerUIState st, float speed, float dt)
        {
            st.Display.Speed = Mathf.Lerp(st.Display.Speed, st.Target.Speed, 1f - Mathf.Exp(-speed * dt));
            st.Display.Progress01 = Mathf.Lerp(st.Display.Progress01, st.Target.Progress01, 1f - Mathf.Exp(-speed * dt));
            st.Display.Boost01 = Mathf.Lerp(st.Display.Boost01, st.Target.Boost01, 1f - Mathf.Exp(-speed * dt));

            st.Display.Lap = st.Target.Lap;
            st.Display.TotalLaps = st.Target.TotalLaps;
            st.Display.Checkpoint = st.Target.Checkpoint;
            st.Display.TotalCheckpoints = st.Target.TotalCheckpoints;
            st.Display.TrackName = st.Target.TrackName;
            st.Display.Position = st.Target.Position;
            st.Display.Racers = st.Target.Racers;
            st.Display.IsRace = st.Target.IsRace;
            st.Display.Finished = st.Target.Finished;
            st.Display.FinishTime = st.Target.FinishTime;
            st.Display.RaceMode = st.Target.RaceMode;
            st.Display.Voting = st.Target.Voting;
            st.Display.VoteSecondsRemaining = st.Target.VoteSecondsRemaining;
            st.Display.CountdownSecondsRemaining = st.Target.CountdownSecondsRemaining;
            st.Display.QueuedPlayers = st.Target.QueuedPlayers;

            st.DisplayYaw = Mathf.LerpAngle(st.DisplayYaw, st.TargetYaw, 1f - Mathf.Exp(-speed * dt));
        }

        #endregion

        #region HUD

        private void PullHudTarget(BasePlayer player, PlayerUIState st)
        {
            var data = HydroRust?.Call("UI_GetHudData", player) as IDictionary<string, object>;
            if (data != null)
            {
                st.Target.TrackName = SafeString(data, "TrackName");
                st.Target.Lap = SafeInt(data, "Lap");
                st.Target.TotalLaps = SafeInt(data, "TotalLaps");
                st.Target.Checkpoint = SafeInt(data, "Checkpoint");
                st.Target.TotalCheckpoints = SafeInt(data, "TotalCheckpoints");
                st.Target.Progress01 = Mathf.Clamp01(SafeFloat(data, "Progress01"));
                st.Target.Speed = Mathf.Max(0f, SafeFloat(data, "Speed"));
                st.Target.Boost01 = Mathf.Clamp01(SafeFloat(data, "Boost01"));
                st.Target.Position = Mathf.Max(0, SafeInt(data, "Position"));
                st.Target.Racers = Mathf.Max(0, SafeInt(data, "Racers"));
                st.Target.IsRace = SafeBool(data, "IsRace");
                st.Target.Finished = SafeBool(data, "Finished");
                st.Target.FinishTime = Mathf.Max(0f, SafeFloat(data, "FinishTime"));
                st.Target.RaceMode = SafeString(data, "RaceMode");
                st.Target.Voting = SafeBool(data, "Voting");
                st.Target.VoteSecondsRemaining = SafeInt(data, "VoteSecondsRemaining");
                st.Target.CountdownSecondsRemaining = SafeInt(data, "CountdownSecondsRemaining");
                st.Target.QueuedPlayers = SafeInt(data, "QueuedPlayers");
            }
            else
            {
                st.Target.TrackName = "";
                st.Target.Lap = 0;
                st.Target.TotalLaps = 0;
                st.Target.Checkpoint = 0;
                st.Target.TotalCheckpoints = 0;
                st.Target.Progress01 = 0f;
                st.Target.Boost01 = 0f;
                st.Target.Position = 0;
                st.Target.Racers = 0;
                st.Target.IsRace = false;
                st.Target.Finished = false;
                st.Target.FinishTime = 0f;
                st.Target.Voting = false;
                st.Target.VoteSecondsRemaining = 0;
                st.Target.CountdownSecondsRemaining = -1;
                st.Target.QueuedPlayers = 0;

                var boat = player.GetMounted()?.GetComponentInParent<BaseBoat>();
                var rb = boat ? boat.GetComponent<Rigidbody>() ?? boat.GetComponentInChildren<Rigidbody>() : null;
                st.Target.Speed = rb != null ? rb.velocity.magnitude : 0f;
            }
            st.TargetYaw = player?.eyes != null ? player.eyes.rotation.eulerAngles.y : 0f;
        }


        #endregion

        #region Welcome Menu

        private void AutoShowLockedWelcome(BasePlayer player)
        {
            var st = EnsureState(player);
            st.MenuToggleAllowed = false;
            st.WelcomeLocked = true;
            st.LockPosition = player.transform.position;
            st.CurrentTab = 1;
            st.StartMenuVisible = true;
            BuildStartMenu(player);
        }

        [ChatCommand("hydro.menu")]
        private void CmdMenu(BasePlayer player, string cmd, string[] args)
        {
            var st = EnsureState(player);
            st.CurrentTab = 1;
            st.StartMenuVisible = true;
            if (!st.MenuToggleAllowed)
            {
                st.WelcomeLocked = true;
                st.LockPosition = player.transform.position;
            }
            BuildStartMenu(player);
        }

        [ConsoleCommand("hydro_ui.menu.tab")]
        private void CCMenuTab(ConsoleSystem.Arg arg)
        {
            var player = arg.Player(); if (player == null) return;
            int tab = Mathf.Clamp(arg.GetInt(0), 1, 2);
            var st = EnsureState(player);
            st.CurrentTab = tab;
            if (!st.StartMenuVisible) st.StartMenuVisible = true;
            BuildStartMenu(player);
        }

        [ConsoleCommand("hydro_ui.menu.toggle")]
        private void CCMenuToggle(ConsoleSystem.Arg arg)
        {
            var player = arg.Player(); if (player == null) return;
            var st = EnsureState(player);
            if (!st.MenuToggleAllowed)
            {
                player.ChatMessage("Menu locked until lobby join.");
                return;
            }
            if (st.StartMenuVisible)
            {
                DestroyStartMenu(player);
                st.StartMenuVisible = false;
            }
            else
            {
                st.CurrentTab = 2;
                st.StartMenuVisible = true;
                BuildStartMenu(player);
            }
        }

        [ConsoleCommand("hydro_ui.joinlobby")]
        private void CCJoinLobby(ConsoleSystem.Arg arg)
        {
            var player = arg.Player(); if (player == null) return;
            if (HydroLobby != null) HydroLobby.Call("SendToLobby", player);
            else player.ChatMessage("Lobby plugin missing.");

            var st = EnsureState(player);
            st.WelcomeLocked = false;
            st.MenuToggleAllowed = true;
            DestroyStartMenu(player);
            st.StartMenuVisible = false;
            ShowMenuToggle(player);

            ShowPlayerPanel(player);
        }

        private void BuildStartMenu(BasePlayer player)
        {
            var st = EnsureState(player);
            var theme = GetTheme(GetPrefs(player.userID).ThemeName);
            DestroyStartMenu(player);

            var c = new CuiElementContainer();
            c.Add(new CuiPanel { Image = { Color = theme.Background }, RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }, CursorEnabled = true }, "Overlay", ROOT_START_MENU);

            var bg = GetImage(IMG_START_BG);
            if (!string.IsNullOrEmpty(bg))
            {
                c.Add(new CuiElement
                {
                    Parent = ROOT_START_MENU,
                    Components =
                    {
                        new CuiRawImageComponent { Png = bg, Color = "1 1 1 0.85" },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                    }
                });
            }

            float headerMinY = Mathf.Clamp01(1f - config.StartHeaderHeight);
            string headerMin = "0 " + headerMinY.ToString("0.###", CultureInfo.InvariantCulture);

            c.Add(new CuiPanel { Image = { Color = theme.Panel }, RectTransform = { AnchorMin = headerMin, AnchorMax = "1 1" }, CursorEnabled = true }, ROOT_START_MENU, START_HEADER);

            AddTextButton(c, START_HEADER, "Welcome", "0.02 0.15", "0.20 0.85", "hydro_ui.menu.tab 1", theme.Panel, theme.Text, 16);
            AddTextButton(c, START_HEADER, "Play", "0.22 0.15", "0.40 0.85", "hydro_ui.menu.tab 2", theme.Panel, theme.Text, 16);

            string bodyMax = "1 " + headerMinY.ToString("0.###", CultureInfo.InvariantCulture);
            c.Add(new CuiPanel { Image = { Color = "0 0 0 0" }, RectTransform = { AnchorMin = "0 0", AnchorMax = bodyMax }, CursorEnabled = true }, ROOT_START_MENU, START_BODY);

            if (st.CurrentTab == 1) BuildWelcomeTab(c, START_BODY, player, theme);
            else BuildPlayTab(c, START_BODY, player, theme);

            CuiHelper.AddUi(player, c);
        }

        private void BuildWelcomeTab(CuiElementContainer c, string parent, BasePlayer player, Theme theme)
        {
            c.Add(new CuiLabel
            {
                Text = { Text = "Welcome to HydroRust", FontSize = 30, Align = TextAnchor.MiddleCenter, Color = theme.Text },
                RectTransform = { AnchorMin = "0.18 0.62", AnchorMax = "0.82 0.74" }
            }, parent, START_TAB_WELCOME + ".title");

            string logo = GetImage("hydro_ui_logo");
            if (!string.IsNullOrEmpty(logo))
            {
                c.Add(new CuiElement
                {
                    Parent = parent,
                    Components =
                    {
                        new CuiRawImageComponent { Png = logo, Color = "1 1 1 1" },
                        new CuiRectTransformComponent { AnchorMin = "0.47 0.48", AnchorMax = "0.53 0.58" }
                    }
                });
            }

            c.Add(new CuiLabel
            {
                Text = { Text = "Go to the Play tab and click Join Lobby to start racing. You are locked here until you join.", FontSize = 16, Align = TextAnchor.MiddleCenter, Color = theme.MutedText },
                RectTransform = { AnchorMin = "0.20 0.40", AnchorMax = "0.80 0.48" }
            }, parent, START_TAB_WELCOME + ".subtitle");

            var st = EnsureState(player);
            st.WelcomeLocked = !st.MenuToggleAllowed;
            st.LockPosition = player.transform.position;
        }

        private void BuildPlayTab(CuiElementContainer c, string parent, BasePlayer player, Theme theme)
        {
            c.Add(new CuiPanel { Image = { Color = theme.Panel }, RectTransform = { AnchorMin = "0.30 0.40", AnchorMax = "0.70 0.65" }, CursorEnabled = true }, parent, START_TAB_PLAY + ".tile");
            string png = GetImage(IMG_TRACK_DEFAULT);
            if (!string.IsNullOrEmpty(png))
            {
                c.Add(new CuiElement
                {
                    Parent = START_TAB_PLAY + ".tile",
                    Components =
                    {
                        new CuiRawImageComponent { Png = png, Color = "1 1 1 0.85" },
                        new CuiRectTransformComponent { AnchorMin = "0 0.35", AnchorMax = "1 1" }
                    }
                });
            }
            c.Add(new CuiLabel { Text = { Text = "Join Lobby", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = theme.Text }, RectTransform = { AnchorMin = "0.05 0.10", AnchorMax = "0.95 0.30" } }, START_TAB_PLAY + ".tile");
            c.Add(new CuiButton { Button = { Command = "hydro_ui.joinlobby", Color = "0 0 0 0" }, Text = { Text = "" }, RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" } }, START_TAB_PLAY + ".tile");
        }

        private void DestroyStartMenu(BasePlayer player) => CuiHelper.DestroyUi(player, ROOT_START_MENU);

        #endregion

        #region Toggle Button

        private void ShowMenuToggle(BasePlayer player)
        {
            var st = EnsureState(player);
            var theme = GetTheme(GetPrefs(player.userID).ThemeName);
            DestroyMenuToggle(player);

            var c = new CuiElementContainer();
            c.Add(new CuiPanel { Image = { Color = theme.Panel }, RectTransform = { AnchorMin = config.MenuToggleAnchorMin, AnchorMax = config.MenuToggleAnchorMax }, CursorEnabled = false }, "Overlay", ROOT_TOGGLE);
            c.Add(new CuiButton { Button = { Command = "hydro_ui.menu.toggle", Color = theme.Primary }, Text = { Text = "≡", FontSize = 18, Align = TextAnchor.MiddleCenter, Color = theme.Text }, RectTransform = { AnchorMin = "0.05 0.05", AnchorMax = "0.95 0.95" } }, ROOT_TOGGLE);
            CuiHelper.AddUi(player, c);
            st.MenuToggleVisible = true;
        }

        private void DestroyMenuToggle(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, ROOT_TOGGLE);
            EnsureState(player).MenuToggleVisible = false;
        }

        #endregion

        #region Player Quick Panel

        private void ShowPlayerPanel(BasePlayer player)
        {
            var st = EnsureState(player);
            var theme = GetTheme(GetPrefs(player.userID).ThemeName);

            DestroyPlayerPanel(player);

            var c = new CuiElementContainer();
            c.Add(new CuiPanel
            {
                Image = { Color = theme.Background },
                RectTransform = { AnchorMin = config.PlayerPanelAnchorMin, AnchorMax = config.PlayerPanelAnchorMax },
                CursorEnabled = false
            }, "Overlay", ROOT_PLAYER);

            // Show queue count if available
            string queueText = st.Display.QueuedPlayers > 0 
                ? "Queue: " + st.Display.QueuedPlayers 
                : "";
            if (!string.IsNullOrEmpty(queueText))
            {
                c.Add(new CuiLabel
                {
                    Text = { Text = queueText, FontSize = 11, Align = TextAnchor.MiddleCenter, Color = theme.Accent },
                    RectTransform = { AnchorMin = "0.84 0.90", AnchorMax = "0.95 0.98" }
                }, ROOT_PLAYER);
            }

            AddTextButton(c, ROOT_PLAYER, "Join Race", "0.05 0.60", "0.95 0.90", "hydroui.player.join", theme.Good, theme.Text, 14);
            AddTextButton(c, ROOT_PLAYER, "Leave Race", "0.05 0.35", "0.95 0.55", "hydroui.player.leave", theme.Alert, theme.Text, 14);
            AddTextButton(c, ROOT_PLAYER, "Stats", "0.05 0.10", "0.95 0.30", "hydroui.player.stats", theme.Panel, theme.Text, 12);

            CuiHelper.AddUi(player, c);
            st.PlayerPanelVisible = true;
        }

        private void DestroyPlayerPanel(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, ROOT_PLAYER);
            EnsureState(player).PlayerPanelVisible = false;
        }

        [ConsoleCommand("hydroui.player.join")]
        private void CCPlayerJoin(ConsoleSystem.Arg arg)
        {
            var player = arg.Player(); if (player == null) return;
            RunChat(player, config.Chat.RaceJoin);
        }

        [ConsoleCommand("hydroui.player.leave")]
        private void CCPlayerLeave(ConsoleSystem.Arg arg)
        {
            var player = arg.Player(); if (player == null) return;
            RunChat(player, config.Chat.RaceLeave);
        }

        [ConsoleCommand("hydroui.player.stats")]
        private void CCPlayerStats(ConsoleSystem.Arg arg)
        {
            var player = arg.Player(); if (player == null) return;
            RunChat(player, config.Chat.Stats);
        }

        [ConsoleCommand("hydroui.vote.normal")]
        private void CCVoteNormal(ConsoleSystem.Arg arg)
        {
            var player = arg.Player(); if (player == null) return;
            RunChat(player, config.Chat.VoteNormal);
        }

        [ConsoleCommand("hydroui.vote.battle")]
        private void CCVoteBattle(ConsoleSystem.Arg arg)
        {
            var player = arg.Player(); if (player == null) return;
            RunChat(player, config.Chat.VoteBattle);
        }

        private void ShowVotingOverlay(BasePlayer player, PlayerUIState st)
        {
            var theme = GetTheme(GetPrefs(player.userID).ThemeName);
            HideVotingOverlay(player);

            var c = new CuiElementContainer();
            
            // Semi-transparent backdrop
            c.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0.75" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                CursorEnabled = false
            }, "Overlay", ROOT_VOTING);

            // Main voting panel
            c.Add(new CuiPanel
            {
                Image = { Color = theme.Panel },
                RectTransform = { AnchorMin = "0.35 0.40", AnchorMax = "0.65 0.60" },
                CursorEnabled = false
            }, ROOT_VOTING, ROOT_VOTING + ".Panel");

            // Title
            c.Add(new CuiLabel
            {
                Text = { Text = "VOTE FOR RACE MODE", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = theme.Text },
                RectTransform = { AnchorMin = "0.05 0.70", AnchorMax = "0.95 0.95" }
            }, ROOT_VOTING + ".Panel");

            // Normal button
            AddTextButton(c, ROOT_VOTING + ".Panel", "NORMAL", "0.05 0.35", "0.47 0.65", 
                "hydroui.vote.normal", theme.Good, theme.Text, 18);

            // Battle button
            AddTextButton(c, ROOT_VOTING + ".Panel", "BATTLE", "0.53 0.35", "0.95 0.65", 
                "hydroui.vote.battle", theme.Battle, theme.Text, 18);

            // Time remaining
            string timeText = st.Display.VoteSecondsRemaining > 0 
                ? st.Display.VoteSecondsRemaining + "s remaining" 
                : "Vote now!";
            c.Add(new CuiLabel
            {
                Text = { Text = timeText, FontSize = 14, Align = TextAnchor.MiddleCenter, Color = theme.MutedText },
                RectTransform = { AnchorMin = "0.05 0.10", AnchorMax = "0.95 0.30" }
            }, ROOT_VOTING + ".Panel");

            CuiHelper.AddUi(player, c);
        }

        private void HideVotingOverlay(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, ROOT_VOTING);
        }

        private void ShowCountdownOverlay(BasePlayer player, PlayerUIState st, int seconds)
        {
            var theme = GetTheme(GetPrefs(player.userID).ThemeName);
            HideCountdownOverlay(player);

            var c = new CuiElementContainer();
            
            // Main countdown display (no backdrop to keep visibility)
            string displayText = seconds > 0 ? seconds.ToString() : "GO!";
            string color = seconds > 0 ? theme.Primary : theme.Good;
            
            c.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                CursorEnabled = false
            }, "Overlay", ROOT_COUNTDOWN);

            c.Add(new CuiLabel
            {
                Text = { Text = displayText, FontSize = 80, Align = TextAnchor.MiddleCenter, Color = color },
                RectTransform = { AnchorMin = "0.40 0.45", AnchorMax = "0.60 0.55" }
            }, ROOT_COUNTDOWN);

            CuiHelper.AddUi(player, c);
        }

        private void HideCountdownOverlay(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, ROOT_COUNTDOWN);
        }

        #endregion

        #region Admin Track Editor

        [ChatCommand("hydroui")]
        private void CmdHydroAdminUI(BasePlayer player, string cmd, string[] args)
        {
            if (!player.IsAdmin && !permission.UserHasPermission(player.UserIDString, "hydrorust.admin"))
            {
                player.ChatMessage("No permission.");
                return;
            }
            DestroyStartMenu(player);
            ShowAdminUI(player);
            player.ChatMessage("Editor opened. Use New or the New Track field (press Enter), then Save.");
        }

        [ConsoleCommand("hydroui.toggle")]
        private void CCToggleEditor(ConsoleSystem.Arg arg)
        {
            var player = arg.Player(); if (player == null) return;
            var st = EnsureState(player);
            if (st.AdminVisible)
            {
                DestroyAdminUI(player);
                st.AdminVisible = false;
            }
            else
            {
                DestroyStartMenu(player);
                ShowAdminUI(player);
            }
        }

        [ConsoleCommand("hydroui.cursor")]
        private void CCCursor(ConsoleSystem.Arg arg)
        {
            var player = arg.Player(); if (player == null) return;
            var st = EnsureState(player);
            st.AdminCursorEnabled = !st.AdminCursorEnabled;
            if (st.AdminVisible) ShowAdminUI(player);
            player.ChatMessage("Editor cursor " + (st.AdminCursorEnabled ? "ENABLED" : "DISABLED"));
        }

        [ConsoleCommand("hydroui.setpanel")]
        private void CCSetPanel(ConsoleSystem.Arg arg)
        {
            var player = arg.Player(); if (player == null) return;
            float minX = arg.GetFloat(0), minY = arg.GetFloat(1), maxX = arg.GetFloat(2), maxY = arg.GetFloat(3);
            minX = Mathf.Clamp01(minX); minY = Mathf.Clamp01(minY); maxX = Mathf.Clamp01(maxX); maxY = Mathf.Clamp01(maxY);
            if (maxX <= minX || maxY <= minY)
            {
                player.ChatMessage("Invalid anchors. Example: hydroui.setpanel 0.76 0.05 0.99 0.95");
                return;
            }
            config.AdminAnchorMin = minX.ToString("0.##", CultureInfo.InvariantCulture) + " " + minY.ToString("0.##", CultureInfo.InvariantCulture);
            config.AdminAnchorMax = maxX.ToString("0.##", CultureInfo.InvariantCulture) + " " + maxY.ToString("0.##", CultureInfo.InvariantCulture);
            SaveConfig();
            if (EnsureState(player).AdminVisible) ShowAdminUI(player);
            player.ChatMessage("Admin panel anchors updated.");
        }

        [ConsoleCommand("hydroui.resetpanel")]
        private void CCResetPanel(ConsoleSystem.Arg arg)
        {
            var player = arg.Player(); if (player == null) return;
            config.AdminAnchorMin = "0.76 0.05";
            config.AdminAnchorMax = "0.99 0.95";
            SaveConfig();
            if (EnsureState(player).AdminVisible) ShowAdminUI(player);
            player.ChatMessage("Admin panel anchors reset.");
        }

        private void ShowAdminUI(BasePlayer player)
        {
            var st = EnsureState(player);
            st.AdminVisible = true;
            DestroyAdminUI(player);

            var theme = GetTheme(GetPrefs(player.userID).ThemeName);
            var c = new CuiElementContainer();

            // Root side panel
            c.Add(new CuiPanel
            {
                Image = { Color = theme.Background },
                RectTransform = { AnchorMin = config.AdminAnchorMin, AnchorMax = config.AdminAnchorMax },
                CursorEnabled = st.AdminCursorEnabled
            }, "Overlay", ROOT_ADMIN);

            // Header (New, Save, Reload, Cursor, Close)
            c.Add(new CuiPanel
            {
                Image = { Color = theme.Panel },
                RectTransform = { AnchorMin = "0 0.94", AnchorMax = "1 1" },
                CursorEnabled = st.AdminCursorEnabled
            }, ROOT_ADMIN, ADMIN_HEADER);

            AddTextButton(c, ADMIN_HEADER, "New", "0.01 0.10", "0.14 0.90", "hydroui.autonew", theme.Primary, theme.Text, 14);
            AddTextButton(c, ADMIN_HEADER, "Save", "0.16 0.10", "0.29 0.90", "hydroui.savetrack", theme.Good, theme.Text, 14);
            AddTextButton(c, ADMIN_HEADER, "Reload", "0.31 0.10", "0.46 0.90", "hydroui.reload", theme.Panel, theme.Text, 14);
            AddTextButton(c, ADMIN_HEADER, st.AdminCursorEnabled ? "Cursor Off" : "Cursor On", "0.48 0.10", "0.70 0.90", "hydroui.cursor", theme.Panel, theme.Text, 14);
            AddTextButton(c, ADMIN_HEADER, "Close", "0.82 0.10", "0.99 0.90", "hydroui.close", theme.Alert, theme.Text, 14);

            // Summary bar
            c.Add(new CuiPanel
            {
                Image = { Color = theme.Panel },
                RectTransform = { AnchorMin = "0 0.88", AnchorMax = "1 0.94" },
                CursorEnabled = false
            }, ROOT_ADMIN, ADMIN_SUMMARY_BAR);

            c.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.01 0.05", AnchorMax = "0.99 0.95" },
                CursorEnabled = false
            }, ADMIN_SUMMARY_BAR, ADMIN_SUMMARY_BAR_INNER);

            // Track list
            c.Add(new CuiPanel { Image = { Color = theme.Panel }, RectTransform = { AnchorMin = "0.02 0.46", AnchorMax = "0.98 0.86" }, CursorEnabled = st.AdminCursorEnabled }, ROOT_ADMIN, ADMIN_TRACK_LIST);
            c.Add(new CuiPanel { Image = { Color = "0 0 0 0" }, RectTransform = { AnchorMin = "0.02 0.02", AnchorMax = "0.98 0.98" }, CursorEnabled = st.AdminCursorEnabled }, ADMIN_TRACK_LIST, ADMIN_TRACKLIST_BODY);

            // Inspector
            c.Add(new CuiPanel { Image = { Color = theme.Panel }, RectTransform = { AnchorMin = "0.02 0.02", AnchorMax = "0.98 0.44" }, CursorEnabled = st.AdminCursorEnabled }, ROOT_ADMIN, ADMIN_INSPECTOR);
            c.Add(new CuiPanel { Image = { Color = "0 0 0 0" }, RectTransform = { AnchorMin = "0.02 0.02", AnchorMax = "0.98 0.98" }, CursorEnabled = st.AdminCursorEnabled }, ADMIN_INSPECTOR, ADMIN_INSPECTOR_BODY);

            CuiHelper.AddUi(player, c);
            // Build everything once when opening
            RefreshTrackList(player, st);
            RefreshEditorSnapshot(player, st);
            UpdateSummaryBar(player, st);
            UpdateInspectorPanel(player, st);
        }

        private void RefreshEditorSnapshot(BasePlayer player, PlayerUIState st)
        {
            var ctxObj = HydroRust?.Call("UI_GetEditorContext", player) as IDictionary<string, object>;
            if (ctxObj != null)
            {
                st.EditorTrackName = SafeString(ctxObj, "TrackName");
                st.EditorSelectedType = SafeString(ctxObj, "SelectedType");
                st.EditorCpIndex = SafeInt(ctxObj, "SelectedCheckpointIndex");
                st.EditorBoostIndex = SafeInt(ctxObj, "SelectedBoostIndex");
                st.EditorCpCount = SafeInt(ctxObj, "CheckpointCount");
                st.EditorBoostCount = SafeInt(ctxObj, "BoostCount");
                st.EditorLaps = SafeInt(ctxObj, "Laps");
                st.EditorDesigner = SafeString(ctxObj, "Designer");
                st.EditorImageKey = SafeString(ctxObj, "ImageKey");
            }
        }

        private void RefreshTrackList(BasePlayer player, PlayerUIState st)
        {
            var listObj = HydroRust?.Call("UI_ListTrackNames") as IList<object>;
            if (listObj != null)
                st.TrackList = listObj.Select(o => o?.ToString() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList();
            UpdateTrackListPanel(player, st);
            st.TrackListDirty = false;
        }

        private void UpdateSummaryBar(BasePlayer player, PlayerUIState st)
        {
            var theme = GetTheme(GetPrefs(player.userID).ThemeName);
            CuiHelper.DestroyUi(player, ADMIN_SUMMARY_BAR_INNER);

            var c = new CuiElementContainer();
            c.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.01 0.05", AnchorMax = "0.99 0.95" },
                CursorEnabled = false
            }, ADMIN_SUMMARY_BAR, ADMIN_SUMMARY_BAR_INNER);

            string line1 = "Track: " + (string.IsNullOrEmpty(st.EditorTrackName) ? "(none)" : st.EditorTrackName);
            string line2 = "Laps: " + st.EditorLaps + " | CP: " + st.EditorCpCount + " | Boosts: " + st.EditorBoostCount;
            string line3 = "Selected: " + st.EditorSelectedType + "  CP# " + st.EditorCpIndex + "  Boost# " + st.EditorBoostIndex;

            c.Add(new CuiLabel { Text = { Text = line1, FontSize = 12, Align = TextAnchor.MiddleLeft, Color = theme.Text }, RectTransform = { AnchorMin = "0.00 0.55", AnchorMax = "0.98 0.95" } }, ADMIN_SUMMARY_BAR_INNER);
            c.Add(new CuiLabel { Text = { Text = line2, FontSize = 11, Align = TextAnchor.MiddleLeft, Color = theme.MutedText }, RectTransform = { AnchorMin = "0.00 0.25", AnchorMax = "0.98 0.55" } }, ADMIN_SUMMARY_BAR_INNER);
            c.Add(new CuiLabel { Text = { Text = line3, FontSize = 11, Align = TextAnchor.MiddleLeft, Color = theme.MutedText }, RectTransform = { AnchorMin = "0.00 0.00", AnchorMax = "0.98 0.25" } }, ADMIN_SUMMARY_BAR_INNER);

            CuiHelper.AddUi(player, c);
        }

        private void UpdateTrackListPanel(BasePlayer player, PlayerUIState st)
        {
            var theme = GetTheme(GetPrefs(player.userID).ThemeName);
            CuiHelper.DestroyUi(player, ADMIN_TRACKLIST_BODY);

            var c = new CuiElementContainer();
            c.Add(new CuiPanel { Image = { Color = "0 0 0 0" }, RectTransform = { AnchorMin = "0.02 0.02", AnchorMax = "0.98 0.98" }, CursorEnabled = st.AdminCursorEnabled }, ADMIN_TRACK_LIST, ADMIN_TRACKLIST_BODY);

            // New by name
            string newBox = ADMIN_TRACKLIST_BODY + ".new";
            c.Add(new CuiPanel { Image = { Color = "0 0 0 0" }, RectTransform = { AnchorMin = "0.02 0.86", AnchorMax = "0.98 0.94" }, CursorEnabled = st.AdminCursorEnabled }, ADMIN_TRACKLIST_BODY, newBox);
            c.Add(new CuiLabel { Text = { Text = "New Track Name (Enter):", FontSize = 11, Align = TextAnchor.MiddleLeft, Color = theme.MutedText }, RectTransform = { AnchorMin = "0 0.55", AnchorMax = "0.50 0.95" } }, newBox);
            c.Add(new CuiElement
            {
                Parent = newBox,
                Components =
                {
                    new CuiInputFieldComponent { Command = "hydroui.newtrack", FontSize = 12, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1", Text = "" },
                    new CuiRectTransformComponent { AnchorMin = "0.00 0.05", AnchorMax = "0.98 0.55" }
                }
            });

            // Select by name
            string selBox = ADMIN_TRACKLIST_BODY + ".select";
            c.Add(new CuiPanel { Image = { Color = "0 0 0 0" }, RectTransform = { AnchorMin = "0.02 0.78", AnchorMax = "0.98 0.86" }, CursorEnabled = st.AdminCursorEnabled }, ADMIN_TRACKLIST_BODY, selBox);
            c.Add(new CuiLabel { Text = { Text = "Select Track (Enter):", FontSize = 11, Align = TextAnchor.MiddleLeft, Color = theme.MutedText }, RectTransform = { AnchorMin = "0 0.55", AnchorMax = "0.50 0.95" } }, selBox);
            c.Add(new CuiElement
            {
                Parent = selBox,
                Components =
                {
                    new CuiInputFieldComponent { Command = "hydroui.selecttrack", FontSize = 12, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1", Text = "" },
                    new CuiRectTransformComponent { AnchorMin = "0.00 0.05", AnchorMax = "0.98 0.55" }
                }
            });

            // Delete by name
            string delBox = ADMIN_TRACKLIST_BODY + ".delete";
            c.Add(new CuiPanel { Image = { Color = "0 0 0 0" }, RectTransform = { AnchorMin = "0.02 0.70", AnchorMax = "0.98 0.78" }, CursorEnabled = st.AdminCursorEnabled }, ADMIN_TRACKLIST_BODY, delBox);
            c.Add(new CuiLabel { Text = { Text = "Delete Track (Enter):", FontSize = 11, Align = TextAnchor.MiddleLeft, Color = theme.MutedText }, RectTransform = { AnchorMin = "0 0.55", AnchorMax = "0.50 0.95" } }, delBox);
            c.Add(new CuiElement
            {
                Parent = delBox,
                Components =
                {
                    new CuiInputFieldComponent { Command = "hydroui.deletetrack", FontSize = 12, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1", Text = "" },
                    new CuiRectTransformComponent { AnchorMin = "0.00 0.05", AnchorMax = "0.98 0.55" }
                }
            });

            // List with Open/Delete buttons
            int total = st.TrackList.Count;
            int pages = total == 0 ? 1 : ((total - 1) / TRACKS_PER_PAGE + 1);
            if (st.TrackListPage >= pages) st.TrackListPage = pages - 1;
            if (st.TrackListPage < 0) st.TrackListPage = 0;
            int start = st.TrackListPage * TRACKS_PER_PAGE;
            int end = Math.Min(total, start + TRACKS_PER_PAGE);

            float y = 0.66f;
            for (int i = start; i < end; i++)
            {
                string tname = st.TrackList[i];
                float y2 = y - 0.045f;

                string yMin = y2.ToString("0.00", CultureInfo.InvariantCulture);
                string yMax = y.ToString("0.00", CultureInfo.InvariantCulture);

                c.Add(new CuiLabel
                {
                    Text = { Text = tname, FontSize = 11, Align = TextAnchor.MiddleLeft, Color = theme.Text },
                    RectTransform = { AnchorMin = "0.02 " + yMin, AnchorMax = "0.58 " + yMax }
                }, ADMIN_TRACKLIST_BODY);

                AddTextButton(c, ADMIN_TRACKLIST_BODY, "Open",
                    "0.60 " + yMin, "0.75 " + yMax,
                    "hydroui.selecttrack " + EscapeArg(tname), theme.Good, theme.Text, 11);

                AddTextButton(c, ADMIN_TRACKLIST_BODY, "Delete",
                    "0.77 " + yMin, "0.92 " + yMax,
                    "hydroui.deletetrack " + EscapeArg(tname), theme.Alert, theme.Text, 11);

                y -= 0.05f;
                if (y < 0.10f) break;
            }

            AddTextButton(c, ADMIN_TRACKLIST_BODY, "< Prev", "0.02 0.02", "0.22 0.08", "hydroui.trackpage prev", theme.Panel, theme.Text, 11);
            AddTextButton(c, ADMIN_TRACKLIST_BODY, "Next >", "0.24 0.02", "0.44 0.08", "hydroui.trackpage next", theme.Panel, theme.Text, 11);

            CuiHelper.AddUi(player, c);
        }

        private void UpdateInspectorPanel(BasePlayer player, PlayerUIState st)
        {
            var theme = GetTheme(GetPrefs(player.userID).ThemeName);
            CuiHelper.DestroyUi(player, ADMIN_INSPECTOR_BODY);

            var c = new CuiElementContainer();
            c.Add(new CuiPanel { Image = { Color = "0 0 0 0" }, RectTransform = { AnchorMin = "0.02 0.02", AnchorMax = "0.98 0.98" }, CursorEnabled = st.AdminCursorEnabled }, ADMIN_INSPECTOR, ADMIN_INSPECTOR_BODY);

            float y = 0.94f;

            AddSectionLabel(c, ADMIN_INSPECTOR_BODY, "Rename", y, theme);
            y -= 0.05f;
            c.Add(new CuiElement
            {
                Parent = ADMIN_INSPECTOR_BODY,
                Components =
                {
                    new CuiInputFieldComponent { Command = "hydroui.renametrack", FontSize = 12, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1", Text = "" },
                    new CuiRectTransformComponent { AnchorMin = "0.02 " + (y - 0.05f).ToString("0.00", CultureInfo.InvariantCulture), AnchorMax = "0.98 " + y.ToString("0.00", CultureInfo.InvariantCulture) }
                }
            });
            y -= 0.06f;

            AddSectionLabel(c, ADMIN_INSPECTOR_BODY, "Laps", y, theme);
            y -= 0.05f;
            AddTextButton(c, ADMIN_INSPECTOR_BODY, "Laps -", "0.02 " + (y - 0.05f).ToString("0.00", CultureInfo.InvariantCulture), "0.18 " + y.ToString("0.00", CultureInfo.InvariantCulture), "hydroui.setlaps -1", theme.Panel, theme.Text, 11);
            AddTextButton(c, ADMIN_INSPECTOR_BODY, "Laps +", "0.20 " + (y - 0.05f).ToString("0.00", CultureInfo.InvariantCulture), "0.36 " + y.ToString("0.00", CultureInfo.InvariantCulture), "hydroui.setlaps 1", theme.Panel, theme.Text, 11);
            y -= 0.06f;

            AddSectionLabel(c, ADMIN_INSPECTOR_BODY, "Placement", y, theme);
            y -= 0.05f;
            AddTextButton(c, ADMIN_INSPECTOR_BODY, "Set Start @Aim", "0.02 " + (y - 0.05f).ToString("0.00", CultureInfo.InvariantCulture), "0.32 " + y.ToString("0.00", CultureInfo.InvariantCulture), "hydroui.setstartaim", theme.Primary, theme.Text, 11);
            AddTextButton(c, ADMIN_INSPECTOR_BODY, "Set Finish @Aim", "0.34 " + (y - 0.05f).ToString("0.00", CultureInfo.InvariantCulture), "0.66 " + y.ToString("0.00", CultureInfo.InvariantCulture), "hydroui.setfinishaim", theme.Primary, theme.Text, 11);
            AddTextButton(c, ADMIN_INSPECTOR_BODY, "Move Selected To Aim", "0.68 " + (y - 0.05f).ToString("0.00", CultureInfo.InvariantCulture), "0.98 " + y.ToString("0.00", CultureInfo.InvariantCulture), "hydroui.movetoaim", theme.Good, theme.Text, 11);
            y -= 0.06f;

            AddSectionLabel(c, ADMIN_INSPECTOR_BODY, "Checkpoints", y, theme);
            y -= 0.05f;
            AddTextButton(c, ADMIN_INSPECTOR_BODY, "Add CP @Aim", "0.02 " + (y - 0.05f).ToString("0.00", CultureInfo.InvariantCulture), "0.24 " + y.ToString("0.00", CultureInfo.InvariantCulture), "hydroui.addcp 10 -1", theme.Panel, theme.Text, 11);
            AddTextButton(c, ADMIN_INSPECTOR_BODY, "Radius -", "0.26 " + (y - 0.05f).ToString("0.00", CultureInfo.InvariantCulture), "0.38 " + y.ToString("0.00", CultureInfo.InvariantCulture), "hydroui.radiusminus", theme.Panel, theme.Text, 11);
            AddTextButton(c, ADMIN_INSPECTOR_BODY, "Radius +", "0.40 " + (y - 0.05f).ToString("0.00", CultureInfo.InvariantCulture), "0.52 " + y.ToString("0.00", CultureInfo.InvariantCulture), "hydroui.radiusplus", theme.Panel, theme.Text, 11);
            AddTextButton(c, ADMIN_INSPECTOR_BODY, "Delete Selected", "0.54 " + (y - 0.05f).ToString("0.00", CultureInfo.InvariantCulture), "0.76 " + y.ToString("0.00", CultureInfo.InvariantCulture), "hydroui.delete", theme.Alert, theme.Text, 11);
            AddTextButton(c, ADMIN_INSPECTOR_BODY, "Color Aqua", "0.78 " + (y - 0.05f).ToString("0.00", CultureInfo.InvariantCulture), "0.98 " + y.ToString("0.00", CultureInfo.InvariantCulture), "hydroui.setcolor 0 0.8 1 0.8", theme.Panel, theme.Text, 11);
            y -= 0.06f;

            AddSectionLabel(c, ADMIN_INSPECTOR_BODY, "Boosts", y, theme);
            y -= 0.05f;
            AddTextButton(c, ADMIN_INSPECTOR_BODY, "Add Boost @Aim", "0.02 " + (y - 0.05f).ToString("0.00", CultureInfo.InvariantCulture), "0.28 " + y.ToString("0.00", CultureInfo.InvariantCulture), "hydroui.addboost 1.5 1.5 6", theme.Panel, theme.Text, 11);
            AddTextButton(c, ADMIN_INSPECTOR_BODY, "Radius -", "0.30 " + (y - 0.05f).ToString("0.00", CultureInfo.InvariantCulture), "0.42 " + y.ToString("0.00", CultureInfo.InvariantCulture), "hydroui.radiusminus", theme.Panel, theme.Text, 11);
            AddTextButton(c, ADMIN_INSPECTOR_BODY, "Radius +", "0.44 " + (y - 0.05f).ToString("0.00", CultureInfo.InvariantCulture), "0.56 " + y.ToString("0.00", CultureInfo.InvariantCulture), "hydroui.radiusplus", theme.Panel, theme.Text, 11);
            AddTextButton(c, ADMIN_INSPECTOR_BODY, "Color Gold", "0.58 " + (y - 0.05f).ToString("0.00", CultureInfo.InvariantCulture), "0.80 " + y.ToString("0.00", CultureInfo.InvariantCulture), "hydroui.setcolor 1 0.8 0 0.8", theme.Panel, theme.Text, 11);

            CuiHelper.AddUi(player, c);
        }

        private void DestroyAdminUI(BasePlayer player) => CuiHelper.DestroyUi(player, ROOT_ADMIN);

        #endregion

        #region Admin Commands Wiring (forward to chat commands)

        private void RunChat(BasePlayer player, string template, params (string key, object value)[] vars)
        {
            if (player == null || string.IsNullOrEmpty(template)) return;
            string msg = template;
            foreach (var v in vars)
                msg = msg.Replace("{" + v.key + "}", v.value?.ToString() ?? "");
            msg = msg.Trim();
            if (!msg.StartsWith("/")) msg = "/" + msg;
            player.SendConsoleCommand("chat.say", msg);
        }

        [ConsoleCommand("hydroui.close")]
        private void CCAdminClose(ConsoleSystem.Arg arg)
        {
            var player = arg.Player(); if (player == null) return;
            DestroyAdminUI(player);
            EnsureState(player).AdminVisible = false;
        }

        [ConsoleCommand("hydroui.savetrack")]
        private void CCSaveTrack(ConsoleSystem.Arg arg)
        {
            var player = arg.Player(); if (player == null) return;
            var st = EnsureState(player);
            RunChat(player, config.Chat.Save);
            timer.Once(0.15f, () =>
            {
                RefreshEditorSnapshot(player, st);
                UpdateSummaryBar(player, st);
            });
        }

        [ConsoleCommand("hydroui.reload")]
        private void CCReload(ConsoleSystem.Arg arg)
        {
            var player = arg.Player(); if (player == null) return;
            RunChat(player, config.Chat.ReloadMarkers);
        }

        [ConsoleCommand("hydroui.autonew")]
        private void CCAutoNew(ConsoleSystem.Arg arg)
        {
            var player = arg.Player(); if (player == null) return;
            var st = EnsureState(player);
            string autoName = "Track " + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            RunChat(player, config.Chat.NewTrack, ("name", autoName));
            RunChat(player, config.Chat.SelectTrack, ("name", autoName));
            timer.Once(0.15f, () =>
            {
                RefreshEditorSnapshot(player, st);
                UpdateSummaryBar(player, st);
                RefreshTrackList(player, st);
            });
            player.ChatMessage("Created: " + autoName);
        }

        [ConsoleCommand("hydroui.trackpage")]
        private void CCTrackPage(ConsoleSystem.Arg arg)
        {
            var player = arg.Player(); if (player == null) return;
            var st = EnsureState(player);
            string dir = arg.GetString(0) ?? "next";
            st.TrackListPage = dir == "prev" ? Math.Max(0, st.TrackListPage - 1) : st.TrackListPage + 1;
            UpdateTrackListPanel(player, st);
        }

        [ConsoleCommand("hydroui.selecttrack")]
        private void CCSelectTrack(ConsoleSystem.Arg arg)
        {
            var player = arg.Player(); if (player == null) return;
            var st = EnsureState(player);
            string name = string.Join(" ", arg.Args ?? Array.Empty<string>()).Trim();
            if (!string.IsNullOrEmpty(name))
            {
                RunChat(player, config.Chat.SelectTrack, ("name", name));
                timer.Once(0.15f, () =>
                {
                    RefreshEditorSnapshot(player, st);
                    UpdateSummaryBar(player, st);
                });
            }
        }

        [ConsoleCommand("hydroui.deletetrack")]
        private void CCDeleteTrack(ConsoleSystem.Arg arg)
        {
            var player = arg.Player(); if (player == null) return;
            var st = EnsureState(player);
            string name = string.Join(" ", arg.Args ?? Array.Empty<string>()).Trim();
            if (!string.IsNullOrEmpty(name))
            {
                RunChat(player, config.Chat.DeleteTrackByName, ("name", name));
                timer.Once(0.15f, () =>
                {
                    RefreshEditorSnapshot(player, st);
                    UpdateSummaryBar(player, st);
                    RefreshTrackList(player, st);
                });
            }
        }

        [ConsoleCommand("hydroui.newtrack")]
        private void CCNewTrack(ConsoleSystem.Arg arg)
        {
            var player = arg.Player(); if (player == null) return;
            var st = EnsureState(player);
            string name = string.Join(" ", arg.Args ?? Array.Empty<string>()).Trim();
            if (string.IsNullOrEmpty(name)) { player.ChatMessage("Enter a name then press Enter."); return; }
            RunChat(player, config.Chat.NewTrack, ("name", name));
            RunChat(player, config.Chat.SelectTrack, ("name", name));
            timer.Once(0.15f, () =>
            {
                RefreshEditorSnapshot(player, st);
                UpdateSummaryBar(player, st);
                RefreshTrackList(player, st);
            });
        }

        [ConsoleCommand("hydroui.renametrack")]
        private void CCRenameTrack(ConsoleSystem.Arg arg)
        {
            var player = arg.Player(); if (player == null) return;
            var st = EnsureState(player);
            string newName = string.Join(" ", arg.Args ?? Array.Empty<string>()).Trim();
            if (string.IsNullOrEmpty(newName)) { player.ChatMessage("Enter a new name then press Enter."); return; }
            RunChat(player, config.Chat.Rename, ("name", newName));
            RunChat(player, config.Chat.SelectTrack, ("name", newName));
            timer.Once(0.15f, () =>
            {
                RefreshEditorSnapshot(player, st);
                UpdateSummaryBar(player, st);
                RefreshTrackList(player, st);
            });
        }

        [ConsoleCommand("hydroui.setlaps")]
        private void CCSetLaps(ConsoleSystem.Arg arg)
        {
            var player = arg.Player(); if (player == null) return;
            var st = EnsureState(player);
            int delta = arg.GetInt(0);
            int newLaps = Math.Max(1, st.EditorLaps + (delta == 0 ? 1 : delta));
            RunChat(player, config.Chat.SetLaps, ("laps", newLaps));
            timer.Once(0.15f, () =>
            {
                RefreshEditorSnapshot(player, st);
                UpdateSummaryBar(player, st);
            });
        }

        [ConsoleCommand("hydroui.setstartaim")]
        private void CCSetStartAim(ConsoleSystem.Arg arg)
        {
            var player = arg.Player(); if (player == null) return;
            var st = EnsureState(player);
            RunChat(player, config.Chat.SetStart);
            timer.Once(0.15f, () =>
            {
                RefreshEditorSnapshot(player, st);
                UpdateSummaryBar(player, st);
            });
        }

        [ConsoleCommand("hydroui.setfinishaim")]
        private void CCSetFinishAim(ConsoleSystem.Arg arg)
        {
            var player = arg.Player(); if (player == null) return;
            var st = EnsureState(player);
            RunChat(player, config.Chat.SetFinish);
            timer.Once(0.15f, () =>
            {
                RefreshEditorSnapshot(player, st);
                UpdateSummaryBar(player, st);
            });
        }

        [ConsoleCommand("hydroui.movetoaim")]
        private void CCMoveToAim(ConsoleSystem.Arg arg)
        {
            var player = arg.Player(); if (player == null) return;
            var st = EnsureState(player);
            RunChat(player, config.Chat.MoveSelectedToAim);
            timer.Once(0.15f, () =>
            {
                RefreshEditorSnapshot(player, st);
                UpdateSummaryBar(player, st);
            });
        }

        [ConsoleCommand("hydroui.radiusplus")]
        private void CCRadiusPlus(ConsoleSystem.Arg arg)
        {
            var player = arg.Player(); if (player == null) return;
            if (!string.IsNullOrEmpty(config.Chat.RadiusPlus))
                RunChat(player, config.Chat.RadiusPlus);
        }

        [ConsoleCommand("hydroui.radiusminus")]
        private void CCRadiusMinus(ConsoleSystem.Arg arg)
        {
            var player = arg.Player(); if (player == null) return;
            if (!string.IsNullOrEmpty(config.Chat.RadiusMinus))
                RunChat(player, config.Chat.RadiusMinus);
        }

        [ConsoleCommand("hydroui.setradius")]
        private void CCSetRadius(ConsoleSystem.Arg arg)
        {
            var player = arg.Player(); if (player == null) return;
            var st = EnsureState(player);
            float r = arg.GetFloat(0);
            RunChat(player, config.Chat.SetRadius, ("radius", r));
            timer.Once(0.15f, () =>
            {
                RefreshEditorSnapshot(player, st);
                UpdateSummaryBar(player, st);
            });
        }

        [ConsoleCommand("hydroui.setcolor")]
        private void CCSetColor(ConsoleSystem.Arg arg)
        {
            var player = arg.Player(); if (player == null) return;
            RunChat(player, config.Chat.SetColor, ("color", string.Join(" ", arg.Args ?? Array.Empty<string>()).Trim()));
        }

        [ConsoleCommand("hydroui.addcp")]
        private void CCAddCp(ConsoleSystem.Arg arg)
        {
            var player = arg.Player(); if (player == null) return;
            var st = EnsureState(player);
            float radius = arg.GetFloat(0);
            int insertIndex = (arg.Args != null && arg.Args.Length >= 2) ? arg.GetInt(1) : -1;
            if (radius <= 0f) radius = 10f;
            RunChat(player, config.Chat.AddCpAim, ("radius", radius), ("index", insertIndex));
            timer.Once(0.15f, () =>
            {
                RefreshEditorSnapshot(player, st);
                UpdateSummaryBar(player, st);
            });
        }

        [ConsoleCommand("hydroui.addboost")]
        private void CCAddBoost(ConsoleSystem.Arg arg)
        {
            var player = arg.Player(); if (player == null) return;
            var st = EnsureState(player);
            float strength = arg.GetFloat(0);
            float duration = arg.GetFloat(1);
            float radius = arg.GetFloat(2);
            if (strength <= 0f) strength = 1.5f;
            if (duration <= 0f) duration = 1.5f;
            if (radius <= 0f) radius = 6f;
            RunChat(player, config.Chat.AddBoostAim, ("strength", strength), ("duration", duration), ("radius", radius));
            timer.Once(0.15f, () =>
            {
                RefreshEditorSnapshot(player, st);
                UpdateSummaryBar(player, st);
            });
        }

        [ConsoleCommand("hydroui.delete")]
        private void CCDeleteSelected(ConsoleSystem.Arg arg)
        {
            var player = arg.Player(); if (player == null) return;
            var st = EnsureState(player);
            RunChat(player, config.Chat.DeleteSelected);
            timer.Once(0.15f, () =>
            {
                RefreshEditorSnapshot(player, st);
                UpdateSummaryBar(player, st);
                RefreshTrackList(player, st);
            });
        }

        #endregion

        #region Utility / Shared

        private PlayerUIState EnsureState(BasePlayer player)
        {
            if (player == null) return null;
            if (!states.TryGetValue(player.userID, out var st))
            {
                st = new PlayerUIState { Player = player };
                states[player.userID] = st;
            }
            return st;
        }

        private void HideAll(BasePlayer player, bool keepToggle)
        {
            var st = EnsureState(player);
            st.StartMenuVisible = false;
            st.AdminVisible = false;
            st.VotingOverlayVisible = false;
            st.CountdownOverlayVisible = false;

            DestroyStartMenu(player);
            CuiHelper.DestroyUi(player, ROOT_ADMIN);
            DestroyPlayerPanel(player);
            HideVotingOverlay(player);
            HideCountdownOverlay(player);

            if (!keepToggle)
                DestroyMenuToggle(player);
        }

        private void DestroyAllUI()
        {
            foreach (var p in BasePlayer.activePlayerList)
                HideAll(p, false);
        }

        private bool HasUi(BasePlayer player, string name)
        {
            if (player == null || string.IsNullOrEmpty(name)) return false;
            var st = EnsureState(player);
            if (st == null) return false;
            
            switch (name)
            {
                case ROOT_START_MENU: return st.StartMenuVisible;
                case ROOT_ADMIN: return st.AdminVisible;
                case ROOT_TOGGLE: return st.MenuToggleVisible;
                case ROOT_PLAYER: return st.PlayerPanelVisible;
                case ROOT_VOTING: return st.VotingOverlayVisible;
                case ROOT_COUNTDOWN: return st.CountdownOverlayVisible;
                default: return false;
            }
        }

        private Theme GetTheme(string name = null)
        {
            var t = !string.IsNullOrEmpty(name)
                ? config.Themes.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                : config.Themes.FirstOrDefault();
            return t ?? new Theme();
        }

        private void AddTextButton(CuiElementContainer c, string parent, string label, string min, string max,
            string cmd, string bgColor, string textColor, int fontSize = 12)
        {
            c.Add(new CuiButton
            {
                Button = { Command = cmd, Color = bgColor },
                Text = { Text = label, FontSize = fontSize, Align = TextAnchor.MiddleCenter, Color = textColor },
                RectTransform = { AnchorMin = min, AnchorMax = max }
            }, parent);
        }

        private void AddButton(CuiElementContainer c, string parent, string label, string min, string max, string cmd, string bgColor, string textColor, int fontSize = 12)
            => AddTextButton(c, parent, label, min, max, cmd, bgColor, textColor, fontSize);

        private void AddLabel(CuiElementContainer c, string parent, string text, string min, string max, int font, string color)
        {
            c.Add(new CuiLabel
            {
                Text = { Text = text, FontSize = font, Align = TextAnchor.MiddleLeft, Color = color },
                RectTransform = { AnchorMin = min, AnchorMax = max }
            }, parent);
        }

        private void AddSectionLabel(CuiElementContainer c, string parent, string text, float yTop, Theme theme)
        {
            string min = "0.02 " + (yTop - 0.04f).ToString("0.00", CultureInfo.InvariantCulture);
            string max = "0.98 " + yTop.ToString("0.00", CultureInfo.InvariantCulture);
            c.Add(new CuiLabel
            {
                Text = { Text = text, FontSize = 12, Align = TextAnchor.MiddleLeft, Color = theme.Accent },
                RectTransform = { AnchorMin = min, AnchorMax = max }
            }, parent);
        }

        private string EscapeArg(string value)
        {
            if (string.IsNullOrEmpty(value)) return "\"\"";
            bool needsQuotes = value.IndexOfAny(new[] { ' ', '"' }) >= 0;
            string escaped = value.Replace("\\", "\\\\").Replace("\"", "\\\"");
            return needsQuotes ? "\"" + escaped + "\"" : escaped;
        }

        private void LoadPrefs()
        {
            try
            {
                var data = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, PlayerPrefs>>(DATA_PREFS);
                if (data != null)
                {
                    prefs.Clear();
                    foreach (var kv in data)
                        prefs[kv.Key] = kv.Value;
                }
            }
            catch
            {
                PrintWarning("Failed to load HydroUI prefs.");
                prefs.Clear();
            }
        }

        private void SavePrefs() => Interface.Oxide.DataFileSystem.WriteObject(DATA_PREFS, prefs, true);

        private PlayerPrefs GetPrefs(ulong id)
        {
            if (!prefs.TryGetValue(id, out var p))
            {
                p = new PlayerPrefs
                {
                    UserId = id,
                    ThemeName = config.Themes.First().Name
                };
                prefs[id] = p;
            }
            return p;
        }

        private string SafeString(IDictionary<string, object> d, string key)
        {
            if (d == null || !d.ContainsKey(key) || d[key] == null) return "";
            return d[key].ToString();
        }
        private int SafeInt(IDictionary<string, object> d, string key)
        {
            if (d == null || !d.ContainsKey(key) || d[key] == null) return 0;
            try { return Convert.ToInt32(d[key]); } catch { return 0; }
        }
        private float SafeFloat(IDictionary<string, object> d, string key)
        {
            if (d == null || !d.ContainsKey(key) || d[key] == null) return 0f;
            try { return Convert.ToSingle(d[key], CultureInfo.InvariantCulture); } catch { return 0f; }
        }
        private bool SafeBool(IDictionary<string, object> d, string key)
        {
            if (d == null || !d.ContainsKey(key) || d[key] == null) return false;
            try { return Convert.ToBoolean(d[key]); } catch { return false; }
        }

        #endregion
    }
}