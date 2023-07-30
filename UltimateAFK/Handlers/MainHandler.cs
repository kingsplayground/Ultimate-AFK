using MEC;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp079;
using PlayerStatsSystem;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using System.Collections.Generic;
using System.Linq;
using UltimateAFK.Resources;
using UltimateAFK.Resources.Component;

namespace UltimateAFK.Handlers
{
    /// <summary>
    /// Main class where players are given the AFK component and replacement players are stored.
    /// </summary>
    public class MainHandler
    {
        #region Ignore this

        private readonly UltimateAFK Plugin;

        public MainHandler(UltimateAFK plugin)
        {
            Plugin = plugin;
        }

        #endregion

        /// <summary>
        /// A dictionary where replacement players are stored to give them the stats and items of the original player.
        /// </summary>
        public static Dictionary<string, AFKData> ReplacingPlayersData = new();

        /// <summary>
        /// When a player joins I give him the component.
        /// </summary>
        /// <param name="player"></param>
        [PluginEvent(ServerEventType.PlayerJoined)]
        private void OnPlayerJoin(Player player)
        {
            if (!Plugin.Config.IsEnabled || player is null || player.UserId.Contains("@server") || !player.IsReady) return;

            Log.Debug($"Adding the Component to  {player.Nickname}", Plugin.Config.DebugMode);

            player.GameObject.AddComponent<AFKComponent>();
        }

        /// <summary>
        /// When a player changes roles I make sure that if it is a replacement I give him all the things of the person who replaced.
        /// </summary>
        [PluginEvent(ServerEventType.PlayerChangeRole)]
        private void OnChangingRole(Player player, PlayerRoleBase oldRole, RoleTypeId newRole, RoleChangeReason reason)
        {
            try
            {
                if (player == null || !player.IsReady || newRole == RoleTypeId.Spectator || !ReplacingPlayersData.TryGetValue(player.UserId, out var data))
                    return;

                Log.Debug($"Detecting player {player.Nickname} ({player.UserId}) who replaced a player {data.NickName} who was afk", UltimateAFK.Singleton.Config.DebugMode);

                Timing.CallDelayed(Plugin.Config.ReplaceDelay, () => GiveData(player, data, newRole));
            }
            catch (System.Exception e)
            {
                Log.Error($"{nameof(OnChangingRole)}: {e}");
            }
        }

        private void GiveData(Player player, AFKData data, RoleTypeId roleType)
        {
            try
            {
                Log.Debug($"Replacing player is {player.Nickname} ({player.UserId}) new role is {roleType}", Plugin.Config.DebugMode);

                if (roleType == RoleTypeId.Scp079)
                {
                    Log.Debug("The new role is a SCP-079, transferring energy and experience.", Plugin.Config.DebugMode);

                    if (player.RoleBase is Scp079Role scp079Role && scp079Role.SubroutineModule.TryGetSubroutine(out Scp079TierManager tierManager)
                        && scp079Role.SubroutineModule.TryGetSubroutine(out Scp079AuxManager energyManager))
                    {
                        player.SendBroadcast(string.Format(UltimateAFK.Singleton.Config.MsgReplace, data.NickName), 16, shouldClearPrevious: true);
                        player.SendConsoleMessage(string.Format(UltimateAFK.Singleton.Config.MsgReplace, data.NickName), "white");

                        tierManager.TotalExp = data.SCP079.Experience;
                        energyManager.CurrentAux = data.SCP079.Energy;
                        ReplacingPlayersData.Remove(player.UserId);

                        Log.Debug($"Energy and experience transferred to the player", UltimateAFK.Singleton.Config.DebugMode);
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
                        player.SendBroadcast(string.Format(Plugin.Config.MsgReplace, data.NickName), 16, shouldClearPrevious: true);
                        player.SendConsoleMessage(string.Format(Plugin.Config.MsgReplace, data.NickName), "white");

                        ReplacingPlayersData.Remove(player.UserId);
                    });
                }
            }
            catch (System.Exception e)
            {
                Log.Error($"{nameof(GiveData)}: {e}");
            }
        }


        [PluginEvent(ServerEventType.MapGenerated)]
        private void OnMapGenerated()
        {
            ReplacingPlayersData.Clear();
            Extensions.AllElevators = Map.Elevators.ToList();
        }

        /// <summary>
        /// I make sure that no player is in the dictionary in case it has not been cleaned correctly, this event is also executed when a player disconnects.
        /// </summary>
        [PluginEvent(ServerEventType.PlayerDeath)]
        private void OnPlayerDeath(Player player, Player attacker, DamageHandlerBase damageHandler)
        {
            if (!player.IsReady)
                return;

            if (ReplacingPlayersData.TryGetValue(player.UserId, out _))
            {
                ReplacingPlayersData.Remove(player.UserId);
            }
        }
    }
}