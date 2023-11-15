using MEC;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp079;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Events;
using System.Collections.Generic;
using System.Linq;
using UltimateAFK.API;
using UltimateAFK.API.Components;
using UltimateAFK.API.Structs;
using UltimateAFK.Command;

namespace UltimateAFK
{
    /// <summary>
    /// Main handler where players are given the AFK component and replacement players are stored.
    /// </summary>
    public class MainHandler
    {
        private Config PluginConfig => EntryPoint.Instance.Config;

        /// <summary>
        /// A dictionary where replacement players are stored to give them the stats and items of the original player.
        /// </summary>
        public static Dictionary<string, AfkData> ReplacingPlayersData = new();

        /// <summary>
        /// When a player join the server i give him the <see cref="AfkCheckComponent"/>
        /// </summary>
        /// <param name="ev"></param>
        [PluginEvent]
        private void OnPlayerJoined(PlayerJoinedEvent ev)
        {
            if (!PluginConfig.IsEnabled || ev.Player is null || ev.Player.UserId.Contains("@server") || ev.Player.ReferenceHub.authManager.InstanceMode != CentralAuth.ClientInstanceMode.ReadyClient)
                return;

            ev.Player.GameObject.AddComponent<AfkCheckComponent>();
        }

        /// <summary>
        /// On map generation clear al cached data and update cached elevators.
        /// </summary>
        /// <param name="_"></param>
        [PluginEvent]
        private void OnMapGenerated(MapGeneratedEvent _)
        {
            API.EventArgs.Events.DetectedAfkPlayer += OnDetectAfk;
            ReplacingPlayersData.Clear();
            AfkCommand.ClearAllCachedData();
            Extensions.AllElevators = Map.Elevators.ToList();
        }

        /// <summary>
        /// Im lazy...
        /// </summary>
        /// <param name="_"></param>
        [PluginEvent]
        private void OnRoundEnd(RoundEndEvent _)
        {
            API.EventArgs.Events.DetectedAfkPlayer -= OnDetectAfk;
        }

        private void OnDetectAfk(API.EventArgs.DetectedAfkPlayerEventArgs ev)
        {
            if (ev.IsForCommand)
            {
                Log.Info($"{ev.Player.LogName} use the command to be moved to spectator");
            }
        }

        /// <summary>
        /// I make sure that no player is in the dictionary in case it has not been cleaned correctly, this event is also executed when a player disconnects.
        /// </summary>
        [PluginEvent]
        private void OnPlayerDeath(PlayerDeathEvent ev)
        {
            if (!ev.Player.IsReady)
                return;

            if (ReplacingPlayersData.TryGetValue(ev.Player.UserId, out _))
            {
                ReplacingPlayersData.Remove(ev.Player.UserId);
            }
        }

        private void OnChangeRole(PlayerChangeRoleEvent ev)
        {
            if (ev.Player is null || !ev.Player.IsReady || ev.NewRole == PlayerRoles.RoleTypeId.Spectator || !ReplacingPlayersData.TryGetValue(ev.Player.UserId, out var data))
                return;

            Log.Debug($"Detecting player {ev.Player.LogName} who replaced a player {data.Nickname} who was afk", PluginConfig.DebugMode);

            Timing.CallDelayed(PluginConfig.ReplaceDelay, () => GiveData(ev.Player, data, ev.NewRole));
        }

        private void GiveData(Player player, AfkData data, RoleTypeId roleType)
        {
            try
            {
                Log.Debug($"Replacing player is {player.Nickname} ({player.UserId}) new role is {roleType}", PluginConfig.DebugMode);

                if (roleType == RoleTypeId.Scp079)
                {
                    Log.Debug("The new role is a SCP-079, transferring energy and experience.", PluginConfig.DebugMode);

                    if (player.RoleBase is Scp079Role scp079Role && scp079Role.SubroutineModule.TryGetSubroutine(out Scp079TierManager tierManager)
                        && scp079Role.SubroutineModule.TryGetSubroutine(out Scp079AuxManager energyManager))
                    {
                        player.SendBroadcast(string.Format(PluginConfig.MsgReplace, data.Nickname), 16, shouldClearPrevious: true);
                        player.SendConsoleMessage(string.Format(PluginConfig.MsgReplace, data.Nickname), "white");

                        if (data.Scp079Data.HasValue)
                        {
                            tierManager.TotalExp = data.Scp079Data.Value.Experience;
                            energyManager.CurrentAux = data.Scp079Data.Value.Energy;
                            Log.Debug($"Energy and experience transferred to the player", PluginConfig.DebugMode);
                        }

                        Log.Debug($"Removing player from diccionary.", PluginConfig.DebugMode);
                        ReplacingPlayersData.Remove(player.UserId);
                    }
                    else
                    {
                        Log.Error($"Error transferring experience and level to the replacement player, Player.RoleBase is not Scp079 or there was an error obtaining the subroutines.");
                    }
                }
                else
                {
                    // Clear default role inventory.
                    player.ClearInventory();

                    // I add the ammunition first since it is the slowest thing to be done.
                    if (data.Ammo != null)
                        player.SendAmmo(data.Ammo);

                    // This call delayed is necessary.
                    Timing.CallDelayed(0.3f, () =>
                    {
                        // Teleport the player to the afk position, please don't fall off the map.
                        player.Position = data.Position;
                        // Set player health to afk health.
                        player.Health = data.Health;
                        // Give afk items
                        player.SendItems(data.Items);
                        // I apply the modifications of the replacement player not of the afk, I could do it but I sincerely prefer this method.
                        player.ApplyAttachments();
                        // I refill the ammunition of the weapons, since it is annoying to appear without a loaded weapon.
                        player.ReloadAllWeapons();
                        // Send the broadcast to the player
                        player.SendBroadcast(string.Format(PluginConfig.MsgReplace, data.Nickname), 16, shouldClearPrevious: true);
                        player.SendConsoleMessage(string.Format(PluginConfig.MsgReplace, data.Nickname), "white");

                        ReplacingPlayersData.Remove(player.UserId);
                    });
                }
            }
            catch (System.Exception e)
            {
                Log.Error($"{nameof(GiveData)}: {e}");
            }
        }
    }
}
