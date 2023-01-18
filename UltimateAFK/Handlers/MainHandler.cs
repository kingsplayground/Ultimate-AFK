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
        public static Dictionary<Player, AFKData> ReplacingPlayers = new();

        /// <summary>
        /// When a player joins I give him the component.
        /// </summary>
        /// <param name="player"></param>
        [PluginEvent(ServerEventType.PlayerJoined)]
        private void OnPlayerJoin(Player player)
        {
            if(!Plugin.Config.IsEnabled || player.UserId.Contains("@server")) return;
            
            Log.Debug($"Adding the Component to  {player.Nickname} | Player already have component: {player.GameObject.TryGetComponent<AfkComponent>(out _)}", UltimateAFK.Singleton.Config.DebugMode);

            player.GameObject.AddComponent<AfkComponent>();
        }

        /// <summary>
        /// When a player changes roles I make sure that if it is a replacement I give him all the things of the person who replaced.
        /// </summary>
        [PluginEvent(ServerEventType.PlayerChangeRole)]
        private void OnChangingRole(Player player, PlayerRoleBase oldRole, RoleTypeId newRole, RoleChangeReason reason)
        {
            try
            {
                if (player == null || !ReplacingPlayers.TryGetValue(player, out var data) ||
                    !player.GameObject.TryGetComponent<AfkComponent>(out var _))
                    return;

                Log.Debug($"Detecting player {player.Nickname} ({player.UserId}) who replaced a player who was afk", UltimateAFK.Singleton.Config.DebugMode);
                
                Timing.CallDelayed(Plugin.Config.ReplaceDelay, () => GiveAfkData(player, data, newRole));
            }
            catch (System.Exception e)
            {
                Log.Error($"Error on {GetType().Name} (OnChangingRole) || {e} {e.StackTrace}");
            }
        }

        /// <summary>
        /// Performs the change of stats and items to the replacement player
        /// </summary>
        private void GiveAfkData(Player ply, AFKData data, RoleTypeId newRole)
        {
            Log.Debug($"Replacing player is {ply.Nickname} ({ply.UserId}) new role is {newRole}", Plugin.Config.DebugMode);

            if (newRole == RoleTypeId.Scp079)
            {
                Log.Debug("The new role is a SCP079, transferring energy and experience.", UltimateAFK.Singleton.Config.DebugMode);
                if (ply.RoleBase is Scp079Role scp079Role && scp079Role.SubroutineModule.TryGetSubroutine(out Scp079TierManager tierManager) 
                   && scp079Role.SubroutineModule.TryGetSubroutine(out Scp079AuxManager energyManager))
                {
                    ply.SendBroadcast(string.Format(UltimateAFK.Singleton.Config.MsgReplace, data.NickName), 16, shouldClearPrevious: true);
                    ply.SendConsoleMessage(string.Format(UltimateAFK.Singleton.Config.MsgReplace, data.NickName), "white");

                    tierManager.TotalExp = data.SCP079.Experience;
                    energyManager.CurrentAux = data.SCP079.Energy;
                    ReplacingPlayers.Remove(ply);
                    
                    Log.Debug($"Energy and experience transferred to the player", UltimateAFK.Singleton.Config.DebugMode);
                }
                else
                {
                    Log.Error($"Error transferring experience and level to the replacement player, Player.RoleBase is not Scp079 or there was an error obtaining the subroutines.");
                }
                
                Log.Debug("Removing the replacement player from the dictionary", UltimateAFK.Singleton.Config.DebugMode);
                ReplacingPlayers.Remove(ply);
            }
            else
            {
                Log.Debug("Clearing replacement player inventory", Plugin.Config.DebugMode);
                ply.ClearInventory();
                Log.Debug($"Adding Ammo to {ply.Nickname} ({ply.UserId})", UltimateAFK.Singleton.Config.DebugMode);
                // I add the ammunition first since it is the slowest thing to be done.
                ply.SendAmmo(data.Ammo);
                
                // This call delayed is necessary.
                Timing.CallDelayed(0.1f, () =>
                {
                    Log.Debug($"Changing player {ply.Nickname} ({ply.UserId})  position and HP", Plugin.Config.DebugMode);
                    ply.Position = data.Position;
                    ply.Health = data.Health;
                    Log.Debug($"Adding items to {ply.Nickname} ({ply.UserId})", UltimateAFK.Singleton.Config.DebugMode);
                    ply.SendItems(data.Items);
                    // I apply the modifications of the replacement player not of the afk, I could do it but I sincerely prefer this method.
                    ply.ApplyAttachments();
                    // I refill the ammunition of the weapons, since it is annoying to appear without a loaded weapon.
                    ply.ReloadAllWeapons();
                    // Send the broadcast to the player
                    ply.SendBroadcast(string.Format(UltimateAFK.Singleton.Config.MsgReplace, data.NickName), 16, shouldClearPrevious: true);
                    ply.SendConsoleMessage(string.Format(UltimateAFK.Singleton.Config.MsgReplace, data.NickName), "white");
                });
                
                Log.Debug("Removing the replacement player from the dictionary", UltimateAFK.Singleton.Config.DebugMode);
                ReplacingPlayers.Remove(ply);
            }
        }
        
        /// <summary>
        /// At the beginning of a round I clean the ReplacingPlayers dictionary.
        /// </summary>
        [PluginEvent(ServerEventType.RoundStart)]
        private void OnRoundStarted()
        {
            ReplacingPlayers.Clear();
        }

        /// <summary>
        /// I make sure that no player is in the dictionary in case it has not been cleaned correctly, this event is also executed when a player disconnects.
        /// </summary>
        [PluginEvent(ServerEventType.PlayerDeath)]
        private void OnPlayerDeath(Player player, Player attacker, DamageHandlerBase damageHandler)
        {
            if (player != null && ReplacingPlayers.TryGetValue(player, out var data))
            {
                ReplacingPlayers.Remove(player);
            }
        }
    }
}