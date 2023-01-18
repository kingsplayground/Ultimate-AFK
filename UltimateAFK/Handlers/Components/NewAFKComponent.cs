using System;
using System.Collections.Generic;
using MEC;
using NWAPIPermissionSystem;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp079;
using PlayerRoles.PlayableScps.Scp096;
using PluginAPI.Core;
using UltimateAFK.Resources;
using UnityEngine;

namespace UltimateAFK.Handlers.Components
{
    [RequireComponent(typeof(ReferenceHub))]
    public class NewAFKComponent : MonoBehaviour
    {
        private void Awake()
        {
            if (Player.Get(gameObject) is not { } ply)
            {
                Log.Error($"{this} Error Getting Player");
                Destroy(this);
                return;
            }

            Owner = ply;
            // Coroutine dies when the component or the ReferenceHub (Player) is destroyed.
            _checkHandle = Timing.RunCoroutine(Check().CancelWith(this).CancelWith(gameObject));
            Log.Debug($"Component full loaded Owner: {Owner.Nickname} ({Owner.UserId})", UltimateAFK.Singleton.Config.DebugMode);
        }
        
        private void OnDestroy()
        {
            Log.Debug($"Calling OnDestroy", UltimateAFK.Singleton.Config.DebugMode);

            if (Owner is null)
                Log.Debug("Owner was null at the time of destroying the component", UltimateAFK.Singleton.Config.DebugMode);

            Timing.KillCoroutines(_checkHandle);
        }

        private IEnumerator<float> Check()
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
                    Log.Error($"Error in {nameof(Check)}: &2{e.TargetSite}&r ||  {e.StackTrace}");
                }
            }
        }

        private void CheckAfk()
        {
            if(!Continue(Owner)) return;
            
            if (Owner == null)
            {
                Log.Error($"{nameof(CheckAfk)}: Player is null");
                Destroy(this);
                return;
            }

            var ownerPosition = Owner.Position;
            var cameraPosition = Owner.ReferenceHub.roleManager.CurrentRole is Scp079Role scp079 ? scp079.CurrentCamera.CameraPosition : Owner.Camera.position;
            var cameraRotation = Owner.ReferenceHub.roleManager.CurrentRole is Scp079Role scp0792 ? new Quaternion(scp0792.CurrentCamera.HorizontalRotation, scp0792.CurrentCamera.VerticalRotation, scp0792.CurrentCamera.RollRotation, 0f ) : Owner.Camera.rotation;
            
            // Player is moving
            if (cameraPosition != _cameraPosition || ownerPosition != _ownerPosition || _cameraRotation != cameraRotation)
            {
                _cameraPosition = cameraPosition;
                _cameraRotation = cameraRotation;
                _ownerPosition = ownerPosition;
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

        #region API
        public static Player Owner { get; private set; }

        public RoleTypeId OwnerRoleType { get; } = Owner.Role;

        public Vector3 OwnerLastPosition { get; } = _ownerPosition;
        public int AfkTimes { get; set; }

        public bool IsKickEnabled { get; set; } = UltimateAFK.Singleton.Config.AfkCount > -1;

        #endregion

        #region Private variables

        // Position in the world
        private static Vector3 _ownerPosition;

        // Player camera position
        private Vector3 _cameraPosition;

        // Player camera rotation
        private Quaternion _cameraRotation;

        // The time the player was afk
        private float _afkTime;
        
        // Using a MEC Coroutine is more optimized than using Unity methods.
        private CoroutineHandle _checkHandle;
        #endregion

        #region Private Methods
        
        /// <summary>
        /// Check if the player is alive, if the round has started, if the players on the server meet the requirements for check afk to work, if the player is tutorial and the configuration allows the tutorial to be detected as afk.
        /// </summary>
        /// <returns>True if all requirements are met</returns>
        private bool Continue(Player ply)
        {
            return ply.IsAlive && Round.IsRoundStarted && Player.Count >= UltimateAFK.Singleton.Config.MinPlayers &&
                   (Owner.Role != RoleTypeId.Tutorial || !UltimateAFK.Singleton.Config.IgnoreTut);
        }
        
        /// <summary>
        /// Perform player replacement.
        /// </summary>
        /// <param name="ply">player to replace</param>
        /// <param name="roleType"><see cref="RoleTypeId"/> of the player afk</param>
        private void Replace(Player ply, RoleTypeId roleType)
        {
            try
            {
                // Check if role is blacklisted
                if (!UltimateAFK.Singleton.Config.RoleTypeBlacklist.IsEmpty() && UltimateAFK.Singleton.Config.RoleTypeBlacklist.Contains(roleType))
                {
                    Log.Debug($"player {ply.Nickname} ({ply.UserId}) has a role that is blacklisted so he will not be searched for a replacement player", UltimateAFK.Singleton.Config.DebugMode);
                    
                    ply.ClearInventory();
                    ply.SetRole(RoleTypeId.Spectator);
                
                    if (UltimateAFK.Singleton.Config.AfkCount != -1)
                    {
                        AfkTimes++;

                        if (AfkTimes >= UltimateAFK.Singleton.Config.AfkCount)
                        {
                            ply.SendConsoleMessage(UltimateAFK.Singleton.Config.MsgKick, "white");
                            ply.Kick(UltimateAFK.Singleton.Config.MsgKick);
                            return;
                        }
                    }
                
                    ply.SendBroadcast(UltimateAFK.Singleton.Config.MsgFspec, 30, shouldClearPrevious: true);
                    ply.SendConsoleMessage(UltimateAFK.Singleton.Config.MsgFspec, "white");
                    return;
                }
                
                // Get player replacement
                Player replacement = GetReplacement();
                
                // If replacement is null
                if (replacement is null)
                {
                    Log.Debug("Unable to find replacement player, moving to spectator...", UltimateAFK.Singleton.Config.DebugMode);
                    
                    ply.ClearInventory();
                    ply.SetRole(RoleTypeId.Spectator);

                    if (UltimateAFK.Singleton.Config.AfkCount != -1)
                    {
                        AfkTimes++;

                        if (AfkTimes >= UltimateAFK.Singleton.Config.AfkCount)
                        {
                            ply.SendConsoleMessage(UltimateAFK.Singleton.Config.MsgKick, "white");

                            ply.Kick(UltimateAFK.Singleton.Config.MsgKick);

                            return;
                        }
                    }

                    ply.SendBroadcast(UltimateAFK.Singleton.Config.MsgFspec, 30, shouldClearPrevious: true);
                    ply.SendConsoleMessage(UltimateAFK.Singleton.Config.MsgFspec, "white");
                }
                else
                {
                    // if not
                    Log.Debug($"Replacement Player found Nickname: {replacement.Nickname} UserID: {replacement.UserId}", UltimateAFK.Singleton.Config.DebugMode);

                    // Check if AFK role is SCP-079 
                    if (roleType is RoleTypeId.Scp079)
                    {
                        //Adds the replacement player to the dictionary with all the necessary information
                        AddData(ply, replacement, true);
                
                        // Self-explanatory
                        ply.SetRole(RoleTypeId.Spectator);
                
                        if (UltimateAFK.Singleton.Config.AfkCount != -1)
                        {
                            AfkTimes++;

                            // Check if the player should be removed from the server for being too many times afk
                            if (AfkTimes >= UltimateAFK.Singleton.Config.AfkCount)
                            {
                                ply.SendConsoleMessage(UltimateAFK.Singleton.Config.MsgKick, "white");

                                ply.Kick(UltimateAFK.Singleton.Config.MsgKick);

                                replacement.SetRole(roleType);
                                return;
                            }
                        }

                        //Send player a broadcast for being too long afk
                        ply.SendBroadcastToPlayer(UltimateAFK.Singleton.Config.MsgFspec, 30, shouldClearPrevious: true);
                        ply.SendConsoleMessage(UltimateAFK.Singleton.Config.MsgFspec, "white");
                
                        // Sends replacement to the role that had the afk
                        replacement.SetRole(roleType);
                    }
                    else
                    {
                        // Adds the replacement player to the dictionary with all the necessary information
                        AddData(ply, replacement, false);

                        if (UltimateAFK.Singleton.Config.AfkCount != -1)
                        {
                            AfkTimes++;
                    
                            // Check if the player should be removed from the server for being too many times afk
                            if (AfkTimes >= UltimateAFK.Singleton.Config.AfkCount)
                            {
                                ply.SendConsoleMessage(UltimateAFK.Singleton.Config.MsgKick, "white");

                                ply.Kick(UltimateAFK.Singleton.Config.MsgKick);

                                replacement.SetRole(roleType);
                                return;
                            }
                        }
                        
                        // Clear player inventory
                        ply.ClearPlayerInventory();
                        //Send player a broadcast for being too long afk
                        ply.SendBroadcastToPlayer(UltimateAFK.Singleton.Config.MsgFspec, 25, shouldClearPrevious: true);
                        ply.SendConsoleMessage(UltimateAFK.Singleton.Config.MsgFspec, "white");
                        // Sends player to spectator
                        ply.SetRole(RoleTypeId.Spectator);
                        // Sends replacement to the role that had the afk
                        replacement.SetRole(roleType);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"Error on {nameof(Replace)}: IsOwnerNull: {Owner is null} || {e.Data} -- {e.StackTrace}");
            }
        }
        
        /// <summary>
        /// Obtains a player who qualifies for replacement.
        /// </summary>
        private Player GetReplacement()
        {
            foreach (var player in Player.GetPlayers())
            {
                if (player.IsAlive || player == Owner || player.CheckPermission("uafk.ignore") || player.IsServer || player.UserId.Contains("@server"))
                    continue;

                return player;
            }

            return null;
        }
        
        /// <summary>
        /// Add player data to ReplacingPlayers dictionary.
        /// </summary>
        private void AddData(Player player, Player replacement, bool is079 = false)
        {
            try
            {
                if (is079)
                {
                    if (player.RoleBase is Scp079Role scp079Role && scp079Role.SubroutineModule.TryGetComponent(out Scp079TierManager tierManager)
                                                                 && scp079Role.SubroutineModule.TryGetSubroutine(out Scp079AuxManager energyManager))
                    {
                        MainHandler.ReplacingPlayers.Add(replacement, new AFKData
                        {
                            NickName = player.Nickname,
                            Position = player.Position,
                            Role = player.Role,
                            Ammo = null,
                            Health = player.Health,
                            Items = player.GetItems(),
                            SCP079 = new Scp079Data
                            {
                                Role = scp079Role,
                                Energy = energyManager.CurrentAux,
                                Experience = tierManager.TotalExp,
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
            catch (Exception e)
            {
                Log.Error($"Error on {nameof(AddData)}: {e.Data} -- {e.StackTrace}");
            }
        }
        
        /// <summary>
        /// Cache player's ammunition
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        private Dictionary<ItemType, ushort> GetAmmo(Player player)
        {
            var result = new Dictionary<ItemType, ushort>();

            foreach (var ammo in player.ReferenceHub.inventory.UserInventory.ReserveAmmo)
            {
                result.Add(ammo.Key, ammo.Value);
            }

            return result;
        }

        #endregion
        
    }
}