using MEC;
using PluginAPI.Core;
using System.Collections.Generic;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp079;
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
        public static Dictionary<Player, AFKData> ReplacingPlayers = new Dictionary<Player, AFKData>();

        [PluginEvent(ServerEventType.PlayerJoined)]
        private void OnPlayerJoin(Player player)
        {
            if(!Plugin.Config.IsEnabled) return;
            
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
                
                Timing.CallDelayed(Plugin.Config.ReplaceDelay, () => RepleaceItemsAndStats(player, data, newRole));
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

        /// <summary>
        /// Performs the change of stats and items to the replacement player
        /// </summary>
        private void RepleaceItemsAndStats(Player ply, AFKData data, RoleTypeId newrole)
        {
            Log.Debug($"Replacing {ply.Nickname} ({ply.UserId}) data", Plugin.Config.DebugMode);

            Scp079Role scp079Role;
            if (newrole == RoleTypeId.Scp079 && data.SCP079.Role != null &&
                (scp079Role = ply.ReferenceHub.roleManager.CurrentRole as Scp079Role) != null
                && scp079Role.SubroutineModule.TryGetSubroutine(out Scp079TierManager scp079TierManager) &&
                scp079Role.SubroutineModule.TryGetSubroutine(out Scp079AuxManager scp079AuxManager))
            {
                Log.Debug("The new role is a SCP079, transferring energy and experience.", UltimateAFK.Singleton.Config.DebugMode);

                ply.SendBroadcast(UltimateAFK.Singleton.Config.MsgReplace, 16, shouldClearPrevious: true);
                ply.SendConsoleMessage(UltimateAFK.Singleton.Config.MsgReplace, "white");

                scp079TierManager.TotalExp = data.SCP079.Experience;
                scp079AuxManager.CurrentAux = data.SCP079.Energy;
                ReplacingPlayers.Remove(ply);
                return;
            }

            Log.Debug($"Changing player {ply.Nickname} ({ply.UserId})  position and HP", Plugin.Config.DebugMode);

            ply.Position = data.Position;
            ply.SendBroadcast(UltimateAFK.Singleton.Config.MsgReplace, 16, shouldClearPrevious: true);
            ply.SendConsoleMessage(UltimateAFK.Singleton.Config.MsgReplace, "white");
            ply.Health = data.Health;

            Log.Debug($"Adding Ammo to {ply.Nickname} ({ply.UserId})", UltimateAFK.Singleton.Config.DebugMode);
            ply.SendAmmo(data.Ammo);
            Log.Debug($"Adding items to {ply.Nickname} ({ply.UserId})");
            ply.SendItems(data.Items);

            Log.Debug("Removing the replacement player from the dictionary", UltimateAFK.Singleton.Config.DebugMode);

            ReplacingPlayers.Remove(ply);
        }
    }
}