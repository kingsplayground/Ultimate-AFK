using MEC;
using PluginAPI.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using NWAPIPermissionSystem;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp079;
using PlayerRoles.PlayableScps.Scp096;
using PluginAPI.Core.Items;
using UltimateAFK.Resources;
using UnityEngine;

namespace UltimateAFK.Handlers.Components
{
    /// <summary>
    /// Component that performs a constant afk check.
    /// </summary>
    public class AfkComponent : MonoBehaviour
    {
        /// <summary>
        /// The player who owns this component.
        /// </summary>
        public Player Owner { get; private set; }

        // Position in the world
        private Vector3 _ownerPosition;

        // Player camera position
        private Vector3 _cameraPosition;

        // Player camera rotation
        private Quaternion _cameraRotation;

        // The time the player was afk
        private float _afkTime;

        /// <summary>
        /// Number of times a player was detected as AFK
        /// </summary>
        public int AfkCount { get; private set; }

        // Using a MEC Coroutine is more optimized than using Unity methods.
        private CoroutineHandle _checkHandle;

        private void Awake()
        {
            if (!(Player.Get(gameObject) is Player ply))
            {
                Log.Error($"{this} Error Getting Player");

                Destroy();
                return;
            }

            Owner = ply;

            // Coroutine dies when the component or the ReferenceHub (Player) is destroyed.
            _checkHandle = Timing.RunCoroutine(CheckAfkPerSecond().CancelWith(this).CancelWith(gameObject));
            Log.Debug($"Component full loaded Owner: {Owner.Nickname} ({Owner.UserId})", UltimateAFK.Singleton.Config.DebugMode);
        }

        /// <summary>
        /// Destroys the component and clears the variables
        /// </summary>
        public void Destroy()
        {
            try
            {
                Log.Debug($"Calling Destroy", UltimateAFK.Singleton.Config.DebugMode);

                if (Owner is null)
                    Log.Debug("When trying to destroy the component, owner is null", UltimateAFK.Singleton.Config.DebugMode);

                Timing.KillCoroutines(_checkHandle);

                Destroy(this);
            }
            catch (Exception e)
            {
                Log.Error($"Error while trying to destroy {this} {e}");
                throw;
            }
        }

        private IEnumerator<float> CheckAfkPerSecond()
        {
            for (;;)
            {
                yield return Timing.WaitForSeconds(1.3f);

                Log.Debug("Calling CheckAFK", UltimateAFK.Singleton.Config.DebugMode && UltimateAFK.Singleton.Config.SpamLogs);

                try
                {
                    CheckAfk();
                }
                catch (Exception e)
                {
                    Log.Error($"{this} error on CheckAFK: {e} || {e.StackTrace}");
                }
            }
        }

        private void CheckAfk()
        {
            if (!Continue(Owner))
                return;

            if (Owner == null)
            {
                Log.Error($"{nameof(CheckAfk)}: Player is null");
                Destroy();
                return;
            }

            
            var position = Owner.Position;
            // Yes.. Player.Camera does not change if you are playing a SCP that moves with the cameras :))))))))
            var cameraPosition = Owner.ReferenceHub.roleManager.CurrentRole is Scp079Role scp079 ? scp079.CurrentCamera.CameraPosition : Owner.Camera.position;
            var cameraRotation = Owner.ReferenceHub.roleManager.CurrentRole is Scp079Role scp0792 ? new Quaternion(scp0792.CurrentCamera.HorizontalRotation, scp0792.CurrentCamera.VerticalRotation, scp0792.CurrentCamera.RollRotation, 0f ) : Owner.Camera.rotation;
            
            // Player is moving
            if (cameraPosition != _cameraPosition || position != _ownerPosition || _cameraRotation != cameraRotation)
            {
                _cameraPosition = cameraPosition;
                _cameraRotation = cameraRotation;
                _ownerPosition = position;
                _afkTime = 0f;
            }
            // The player is not moving and is not SCP-096 with his TryToNotCry ability.
            else if (!(Owner.Role == RoleTypeId.Scp096 && (Owner.ReferenceHub.roleManager.CurrentRole as Scp096Role).IsAbilityState(Scp096AbilityState.TryingNotToCry)))
            {
                Log.Debug($"{Owner.Nickname} is in not moving, AFKTime: {_afkTime}", UltimateAFK.Singleton.Config.DebugMode);

                if(_afkTime++ < UltimateAFK.Singleton.Config.AfkTime) return;

                var graceNumb = UltimateAFK.Singleton.Config.GraceTime - (_afkTime - UltimateAFK.Singleton.Config.AfkTime);
                
                if (graceNumb > 0)
                {
                    // The player is in grace time, so let's warn him that he has been afk for too long.
                    Owner.SendBroadcastToPlayer(string.Format(UltimateAFK.Singleton.Config.MsgGrace, graceNumb), 2,
                        shouldClearPrevious: true);
                }
                else
                {
                    Log.Info($"{Owner.Nickname} ({Owner.UserId}) Detected as AFK");

                    _afkTime = 0f;
                    Replace(Owner, Owner.Role);
                }
            }
        }

        /// <summary>
        /// Performs player replacement.
        /// </summary>
        /// <param name="player">Player to be replaced</param>
        /// <param name="ondisconnect">This replacement happens when the player is disconnected from the server ?</param>
        public void Replace(Player player, RoleTypeId role)
        {
            // Check if role is blacklisted
            if (UltimateAFK.Singleton.Config.RoleTypeBlacklist.Contains(role))
            {
                Log.Debug($"player {player.Nickname} ({player.UserId}) has a role that is blacklisted so he will not be searched for a replacement player", UltimateAFK.Singleton.Config.DebugMode);
                player.ClearPlayerInventory();
                player.SetRole(RoleTypeId.Spectator);
                
                if (UltimateAFK.Singleton.Config.AfkCount != -1)
                {
                    AfkCount++;

                    if (AfkCount >= UltimateAFK.Singleton.Config.AfkCount)
                    {
                        player.SendConsoleMessage(UltimateAFK.Singleton.Config.MsgKick, "white");
                        player.Kick(UltimateAFK.Singleton.Config.MsgKick);
                        return;
                    }
                }
                
                player.SendBroadcast(UltimateAFK.Singleton.Config.MsgFspec, 30, shouldClearPrevious: true);
                player.SendConsoleMessage(UltimateAFK.Singleton.Config.MsgFspec, "white");
                return;
            }
            
            // Get player replacement
            Player replacement = GetReplacement();

            // If replacement is null
            if (replacement == null)
            {
                Log.Debug("Unable to find replacement player, moving to spectator...", UltimateAFK.Singleton.Config.DebugMode);

                player.SetRole(RoleTypeId.Spectator);

                if (UltimateAFK.Singleton.Config.AfkCount != -1)
                {
                    AfkCount++;

                    if (AfkCount >= UltimateAFK.Singleton.Config.AfkCount)
                    {
                        player.SendConsoleMessage(UltimateAFK.Singleton.Config.MsgKick, "white");

                        player.Kick(UltimateAFK.Singleton.Config.MsgKick);

                        return;
                    }
                }

                player.SendBroadcast(UltimateAFK.Singleton.Config.MsgFspec, 30, shouldClearPrevious: true);
                player.SendConsoleMessage(UltimateAFK.Singleton.Config.MsgFspec, "white");

                return;
            }

            // is not
            Log.Debug($"Replacement Player found Nickname: {replacement.Nickname} UserID: {replacement.UserId}", UltimateAFK.Singleton.Config.DebugMode);

            // Check if role is SCP-079 to be able to pass the level and energy
            if (role == RoleTypeId.Scp079)
            {
                //Adds the replacement player to the dictionary with all the necessary information
                AddData(player, replacement, true);
                
                // Self-explanatory
                player.SetRole(RoleTypeId.Spectator);
                
                if (UltimateAFK.Singleton.Config.AfkCount != -1)
                {
                    AfkCount++;

                    // Check if the player should be removed from the server for being too many times afk
                    if (AfkCount >= UltimateAFK.Singleton.Config.AfkCount)
                    {
                        player.SendConsoleMessage(UltimateAFK.Singleton.Config.MsgKick, "white");

                        player.Kick(UltimateAFK.Singleton.Config.MsgKick);

                        replacement.SetRole(player.Role);
                        return;
                    }
                }

                //Send player a broadcast for being too long afk
                player.SendBroadcastToPlayer(UltimateAFK.Singleton.Config.MsgFspec, 30, shouldClearPrevious: true);
                player.SendConsoleMessage(UltimateAFK.Singleton.Config.MsgFspec, "white");
                
                // Sends replacement to the role that had the afk
                replacement.SetRole(role);
            }
            else
            {
                // Adds the replacement player to the dictionary with all the necessary information
                AddData(player, replacement, false);

                if (UltimateAFK.Singleton.Config.AfkCount != -1)
                {
                    AfkCount++;
                    
                    // Check if the player should be removed from the server for being too many times afk
                    if (AfkCount >= UltimateAFK.Singleton.Config.AfkCount)
                    {
                        player.SendConsoleMessage(UltimateAFK.Singleton.Config.MsgKick, "white");

                        player.Kick(UltimateAFK.Singleton.Config.MsgKick);

                        replacement.SetRole(player.Role);
                        return;
                    }
                }

                try
                {
                    // Clear player inventory
                    player.ClearPlayerInventory();
                    //Send player a broadcast for being too long afk
                    player.SendBroadcastToPlayer(UltimateAFK.Singleton.Config.MsgFspec, 25, shouldClearPrevious: true);
                    player.SendConsoleMessage(UltimateAFK.Singleton.Config.MsgFspec, "white");
                    // Sends player to spectator
                    player.SetRole(RoleTypeId.Spectator);
                    // Sends replacement to the role that had the afk
                    replacement.SetRole(role);
                }
                catch (Exception e)
                {
                    Log.Error($"Error on {nameof(Replace)}:  {e} -- {e.StackTrace} || player is null? {player is null}", "Ultimate-AFK");
                }
            }
        }

        /// <summary>
        /// Add player data to ReplacingPlayers dictionary.
        /// </summary>
        private void AddData(Player player, Player replacement, bool is079 = false)
        {
            if (is079)
            {
                Scp079Role scp079Role;
                // This if is horrendous but I must get the subroutines from the player afk.
                if ((scp079Role = player.ReferenceHub.roleManager.CurrentRole as Scp079Role) != null &&
                    scp079Role.SubroutineModule.TryGetSubroutine(out Scp079TierManager scp079TierManager) &&
                    scp079Role.SubroutineModule.TryGetSubroutine(out Scp079AuxManager scp079AuxManager))
                {
                    MainHandler.ReplacingPlayers.Add(replacement, new AFKData
                    {
                        NickName = player.Nickname,
                        Position = player.Position,
                        Role = player.Role,
                        // same has do ammo = null xd
                        Ammo = player.ReferenceHub.inventory.UserInventory.ReserveAmmo,
                        Health = player.Health,
                        Items = player.GetItems(),
                        SCP079 = new Scp079Data
                        {
                            Role = scp079Role,
                            Energy = scp079AuxManager.CurrentAux,
                            Experience = scp079TierManager.TotalExp,
                        }
                    });
                }
                return;
            }
            
            // If I make Ammo = player.ReferenceHub.inventory.UserInventory.ReserveAmmo for some reason it gets buggy and ammo becomes null when changing the player to spectator.
            // So I create a temporary dictionary stored in cache (ram) and then clean the information by deleting it from the ReplacingPlayers.
            var ammo = GetAmmo(player);
            MainHandler.ReplacingPlayers.Add(replacement, new AFKData
            {
                NickName = player.Nickname,
                Position = player.Position,
                Role = player.Role,
                Ammo = ammo,
                Health = player.Health,
                Items = player.GetItems(),
                SCP079 = new Scp079Data
                {
                    Role = null,
                    Energy = 0f,
                    Experience = 0
                }
            });
            
        }

        
        /// <summary>
        /// NORTHWOOD FIX AMMO INVENTORY NOW!
        /// </summary>
        private Dictionary<ItemType, ushort> GetAmmo(Player player)
        {
            var result = new Dictionary<ItemType, ushort>();

            foreach (var ammo in player.ReferenceHub.inventory.UserInventory.ReserveAmmo)
            {
                result.Add(ammo.Key, ammo.Value);
            }

            return result;
        }

        /// <summary>
        /// Obtains a player who qualifies for replacement.
        /// </summary>
        private Player GetReplacement()
        {
            foreach (var player in Player.GetPlayers())
            {
                if (player.IsAlive || player == Owner || player.CheckPermission("uafk.ignore") || player.IsServer)
                    continue;

                return player;
            }

            return null;
        }

        /// <summary>
        /// Check if the player is alive, if the round has started, if the players on the server meet the requirements for check afk to work, if the player is tutorial and the configuration allows the tutorial to be detected as afk.
        /// </summary>
        /// <returns>True if all requirements are met</returns>
        private bool Continue(Player ply)
        {
            return ply.IsAlive && Round.IsRoundStarted && Player.Count >= UltimateAFK.Singleton.Config.MinPlayers &&
                   (Owner.Role != RoleTypeId.Tutorial || !UltimateAFK.Singleton.Config.IgnoreTut);
        }
    }
}