using MEC;
using PluginAPI.Core;
using System.Collections.Generic;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp079;
using PlayerStatsSystem;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using UltimateAFK.Handlers.Components;
using UltimateAFK.Resources;
using UnityEngine;

namespace UltimateAFK.Handlers
{
    /// <summary>
    /// Main class where players are given the AFK component and replacement players are stored.
    /// </summary>
    public class MainHandler
    {
        private readonly UltimateAFK Plugin;

        public MainHandler(UltimateAFK plugin)
        {
            Plugin = plugin;
        }

        /// <summary>
        /// A dictionary where replacement players are stored to give them the stats and items of the original player.
        /// </summary>
        public static Dictionary<Player, AFKData> ReplacingPlayers = new();

        [PluginEvent(ServerEventType.PlayerJoined)]
        private void OnPlayerJoin(Player player)
        {
            if(!Plugin.Config.IsEnabled || player.UserId.Contains("@server")) return;
            
            if (player.GameObject.TryGetComponent<AfkComponent>(out var com))
            {
                com.Destroy();

                Log.Debug($"Adding the Component to  {player.Nickname}", UltimateAFK.Singleton.Config.DebugMode);
                player.GameObject.AddComponent<AfkComponent>();
            }
            else
            {
                Log.Debug($"Adding the Component to  {player.Nickname}", UltimateAFK.Singleton.Config.DebugMode);

                player.GameObject.AddComponent<AfkComponent>();
            }
        }

        [PluginEvent(ServerEventType.PlayerChangeRole)]
        private void OnChangingRole(Player player, PlayerRoleBase oldRole, RoleTypeId newRole, RoleChangeReason reason)
        {
            try
            {
                if (player == null || !ReplacingPlayers.TryGetValue(player, out var data) ||
                    !player.GameObject.TryGetComponent<AfkComponent>(out var _))
                    return;

                Log.Debug($"Detecting player {player.Nickname} ({player.UserId}) who replaced a player who was afk", UltimateAFK.Singleton.Config.DebugMode);
                
                Timing.CallDelayed(Plugin.Config.ReplaceDelay, () => ReplaceItemsAndStats(player, data, newRole));
            }
            catch (System.Exception e)
            {
                Log.Error($"Error on {GetType().Name} (OnChangingRole) || {e} {e.StackTrace}");
            }
        }

        [PluginEvent(ServerEventType.RoundStart)]
        private void OnRoundStarted()
        {
            ReplacingPlayers.Clear();
        }

        [PluginEvent(ServerEventType.PlayerDeath)]
        private void OnPlayerDeath(Player player, Player attacker, DamageHandlerBase damageHandler)
        {
            // This works both in case the player is killed in the middle of the AFK detection, as well as if the player is disconnected, since when disconnected he goes through the PlayerDeath event.
            if (player != null && ReplacingPlayers.TryGetValue(player, out var data))
            {
                ReplacingPlayers.Remove(player);
            }
        }

        /// <summary>
        /// Performs the change of stats and items to the replacement player
        /// </summary>
        private void ReplaceItemsAndStats(Player ply, AFKData data, RoleTypeId newrole)
        {
            Log.Debug($"Replacing player is {ply.Nickname} ({ply.UserId})", Plugin.Config.DebugMode);
            
            Scp079Role scp079Role;
            if (newrole == RoleTypeId.Scp079 && data.SCP079.Role != null &&
                (scp079Role = ply.ReferenceHub.roleManager.CurrentRole as Scp079Role) != null
                && scp079Role.SubroutineModule.TryGetSubroutine(out Scp079TierManager scp079TierManager) &&
                scp079Role.SubroutineModule.TryGetSubroutine(out Scp079AuxManager scp079AuxManager))
            {
                Log.Debug("The new role is a SCP079, transferring energy and experience.", UltimateAFK.Singleton.Config.DebugMode);

                ply.SendBroadcastToPlayer(string.Format(UltimateAFK.Singleton.Config.MsgReplace, data.NickName), 16, shouldClearPrevious: true);
                ply.SendConsoleMessage(string.Format(UltimateAFK.Singleton.Config.MsgReplace, data.NickName), "white");

                scp079TierManager.TotalExp = data.SCP079.Experience;
                scp079AuxManager.CurrentAux = data.SCP079.Energy;
                ReplacingPlayers.Remove(ply);
                return;
            }
            
            ply.ClearPlayerInventory();
            Log.Debug($"Adding Ammo to {ply.Nickname} ({ply.UserId})", UltimateAFK.Singleton.Config.DebugMode);
            // I add the ammunition first since it is the slowest thing to be done.
            ply.SendAmmo(data.Ammo);
            
            // This call delayed is necessary
            Timing.CallDelayed(0.3f, () =>
            {
                Log.Debug($"Changing player {ply.Nickname} ({ply.UserId})  position and HP", Plugin.Config.DebugMode);
                ply.Position = data.Position;
                ply.SendBroadcastToPlayer(string.Format(UltimateAFK.Singleton.Config.MsgReplace, data.NickName), 16, shouldClearPrevious: true);
                ply.SendConsoleMessage(string.Format(UltimateAFK.Singleton.Config.MsgReplace, data.NickName), "white");
                ply.Health = data.Health;
                Log.Debug($"Adding items to {ply.Nickname} ({ply.UserId})", UltimateAFK.Singleton.Config.DebugMode);
                ply.SendItems(data.Items);
                // I apply the modifications of the replacement player not of the afk, I could do it but I sincerely prefer this method.
                ply.ApplyAttachments();
            });
            
            Log.Debug("Removing the replacement player from the dictionary", UltimateAFK.Singleton.Config.DebugMode);
            ReplacingPlayers.Remove(ply);
        }
    }
}