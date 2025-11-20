using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("HydroLobby", "belisario-afk + copilot", "1.1.0")]
    [Description("Central lobby spawn for HydroRust and other minigames (join, respawn, pre/post game).")]
    public class HydroLobby : RustPlugin
    {
        #region Config

        private class LobbyConfig
        {
            [JsonProperty("Lobby.Position")] public Vector3 LobbyPosition = Vector3.zero;
            [JsonProperty("Lobby.RotationYaw")] public float LobbyRotationYaw = 0f;
            [JsonProperty("Teleport.OnConnect")] public bool TeleportOnConnect = true;
            [JsonProperty("Teleport.OnRespawn")] public bool TeleportOnRespawn = true;
            [JsonProperty("Teleport.CommandEnabled")] public bool CommandEnabled = true;
            [JsonProperty("Teleport.Permission")] public string LobbyPerm = "hydrolobby.use";
            [JsonProperty("ChatPrefix")] public string ChatPrefix = "[HydroLobby] ";
        }

        private LobbyConfig config;

        protected override void LoadDefaultConfig()
        {
            config = new LobbyConfig();
            SaveConfig();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try { config = Config.ReadObject<LobbyConfig>() ?? new LobbyConfig(); }
            catch
            {
                PrintError("Invalid config, using defaults.");
                config = new LobbyConfig();
            }
            SaveConfig();
        }

        protected override void SaveConfig() => Config.WriteObject(config, true);

        private Quaternion LobbyRotation => Quaternion.Euler(0f, config.LobbyRotationYaw, 0f);

        #endregion

        #region Hooks

        private void Init()
        {
            permission.RegisterPermission(config.LobbyPerm, this);
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (!config.TeleportOnConnect) return;
            if (!HasLobbySet()) return;
            TeleportToLobby(player);
            Message(player, "Welcome to the lobby.");
        }

        private void OnPlayerRespawned(BasePlayer player)
        {
            if (!config.TeleportOnRespawn) return;
            if (!HasLobbySet()) return;
            timer.Once(0.1f, () =>
            {
                if (player != null && player.IsConnected)
                    TeleportToLobby(player);
            });
        }

        private void OnPlayerRespawn(BasePlayer player)
        {
            OnPlayerRespawned(player);
        }

        #endregion

        #region Core Logic

        private bool HasLobbySet() => config.LobbyPosition != Vector3.zero;

        private void TeleportToLobby(BasePlayer player)
        {
            if (player == null || !HasLobbySet()) return;
            player.EnsureDismounted();
            player.Teleport(config.LobbyPosition);
            player.transform.rotation = LobbyRotation;
            player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
            player.UpdateNetworkGroup();
            player.SendNetworkUpdateImmediate();
        }

        private void Message(BasePlayer player, string msg)
        {
            player?.ChatMessage($"{config.ChatPrefix}{msg}");
        }

        #endregion

        #region Commands

        [ChatCommand("hydrolobby")]
        private void CmdHydroLobby(BasePlayer player, string cmd, string[] args)
        {
            if (player == null) return;
            if (!permission.UserHasPermission(player.UserIDString, config.LobbyPerm) && !player.IsAdmin)
            {
                Message(player, "You do not have permission.");
                return;
            }

            if (args == null || args.Length == 0)
            {
                ShowHelp(player);
                return;
            }

            switch (args[0].ToLowerInvariant())
            {
                case "set": LobbySet(player); break;
                case "join":
                case "go": LobbyJoin(player); break;
                case "info": LobbyInfo(player); break;
                case "toggleconnect": ToggleConnect(player); break;
                case "togglerespawn": ToggleRespawn(player); break;
                default: ShowHelp(player); break;
            }
        }

        private void ShowHelp(BasePlayer player)
        {
            Message(player, "HydroLobby Commands:");
            Message(player, "/hydrolobby set");
            Message(player, "/hydrolobby join");
            Message(player, "/hydrolobby info");
            Message(player, "/hydrolobby toggleconnect");
            Message(player, "/hydrolobby togglerespawn");
        }

        private void LobbySet(BasePlayer player)
        {
            config.LobbyPosition = player.transform.position;
            config.LobbyRotationYaw = player.transform.rotation.eulerAngles.y;
            SaveConfig();
            Message(player, $"Lobby set at {config.LobbyPosition} yaw {config.LobbyRotationYaw:0.0}");
        }

        private void LobbyJoin(BasePlayer player)
        {
            if (!HasLobbySet())
            {
                Message(player, "Lobby not set. /hydrolobby set");
                return;
            }
            TeleportToLobby(player);
            Message(player, "Teleported to lobby.");
        }

        private void LobbyInfo(BasePlayer player)
        {
            if (!HasLobbySet())
            {
                Message(player, "Lobby not set.");
                return;
            }
            Message(player, $"Lobby: {config.LobbyPosition}, yaw: {config.LobbyRotationYaw:0.0}");
            Message(player, $"OnConnect: {config.TeleportOnConnect}, OnRespawn: {config.TeleportOnRespawn}");
        }

        private void ToggleConnect(BasePlayer player)
        {
            config.TeleportOnConnect = !config.TeleportOnConnect;
            SaveConfig();
            Message(player, $"Teleport on connect: {config.TeleportOnConnect}");
        }

        private void ToggleRespawn(BasePlayer player)
        {
            config.TeleportOnRespawn = !config.TeleportOnRespawn;
            SaveConfig();
            Message(player, $"Teleport on respawn: {config.TeleportOnRespawn}");
        }

        #endregion

        #region API

        private void SendToLobby(BasePlayer player) => TeleportToLobby(player);
        private Vector3 GetLobbyPosition() => config.LobbyPosition;
        private Quaternion GetLobbyRotation() => LobbyRotation;

        // HydroRust may call to get active lobby players (within radius 20)
        private List<BasePlayer> GetLobbyPlayers()
        {
            var list = new List<BasePlayer>();
            if (!HasLobbySet()) return list;
            foreach (var p in BasePlayer.activePlayerList)
            {
                if (Vector3.Distance(p.transform.position, config.LobbyPosition) <= 20f)
                    list.Add(p);
            }
            return list;
        }

        #endregion
    }
}