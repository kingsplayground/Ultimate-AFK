using MEC;
using PluginAPI.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using NWAPIPermissionSystem;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp079;
using PlayerRoles.PlayableScps.Scp096;
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

        // Rotation of your head/camera
        private Vector3 _cameraPosition;

        private Quaternion _cameraRotation;

        private float _graceTime = UltimateAFK.Singleton.Config.GraceTime;

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
        /// Destroys the component and clears the variables and events recorded in Exiled.
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
                yield return Timing.WaitForSeconds(1.2f);

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

            var position = Owner.Position;
            var cameraPosition = Owner.ReferenceHub.roleManager.CurrentRole is Scp079Role scp079 ? scp079.CurrentCamera.CameraPosition : Owner.Position;
            var cameraRotation = Owner.ReferenceHub.roleManager.CurrentRole is Scp079Role scp0792 ? new Quaternion(scp0792.CurrentCamera.HorizontalRotation, scp0792.CurrentCamera.VerticalRotation, scp0792.CurrentCamera.RollRotation, 0f ) : Owner.Camera.rotation;
            
            // Player is moving
            if (cameraPosition != _cameraPosition || position != _ownerPosition || _cameraRotation != cameraRotation)
            {
                _cameraPosition = cameraPosition;
                _cameraRotation = cameraRotation;
                _ownerPosition = position;
                _afkTime = 0f;
            }
            else if (!(Owner.Role == RoleTypeId.Scp096 && (Owner.ReferenceHub.roleManager.CurrentRole as Scp096Role).IsAbilityState(Scp096AbilityState.TryingNotToCry)))
            {
                Log.Debug($"{Owner.Nickname} is in not moving, AFKTime: {_afkTime}", UltimateAFK.Singleton.Config.DebugMode);

                if(_afkTime++ < UltimateAFK.Singleton.Config.AfkTime) return;

                var graceNumb = UltimateAFK.Singleton.Config.GraceTime - (_afkTime - UltimateAFK.Singleton.Config.AfkTime);
                
                if (graceNumb > 0)
                {
                    // The player is in grace time, so let's warn him that he has been afk for too long.
                    Owner.SendBroadcast(string.Format(UltimateAFK.Singleton.Config.MsgGrace, graceNumb), 1,
                        shouldClearPrevious: true);
                }
                else
                {
                    Log.Info($"{Owner.Nickname} ({Owner.UserId}) Detected as AFK");

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
            Player replacement = GetReplacement();

            if (replacement == null)
            {
                Log.Debug("Unable to find replacement player, moving to spectator...",
                    UltimateAFK.Singleton.Config.DebugMode);

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

            Log.Debug($"Replacement Player found\nNickname: {replacement.Nickname}\nUserID: {replacement.UserId}",
                UltimateAFK.Singleton.Config.DebugMode);

            Scp079Role scp079Role;
            if (role == RoleTypeId.Scp079 &&
                (scp079Role = player.ReferenceHub.roleManager.CurrentRole as Scp079Role) != null &&
                scp079Role.SubroutineModule.TryGetSubroutine(out Scp079TierManager scp079TierManager) &&
                scp079Role.SubroutineModule.TryGetSubroutine(out Scp079AuxManager scp079AuxManager))
            {
                MainHandler.ReplacingPlayers.Add(replacement, new AFKData
                {
                    NickName = player.Nickname,
                    Position = player.Position,
                    Role = player.Role,
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

                player.ClearInventory();
                player.SetRole(RoleTypeId.Spectator);

                if (UltimateAFK.Singleton.Config.AfkCount != -1)
                {
                    AfkCount++;

                    if (AfkCount >= UltimateAFK.Singleton.Config.AfkCount)
                    {
                        player.SendConsoleMessage(UltimateAFK.Singleton.Config.MsgKick, "white");

                        player.Kick(UltimateAFK.Singleton.Config.MsgKick);

                        replacement.SetRole(player.Role);
                        return;
                    }
                }

                player.SendBroadcast(UltimateAFK.Singleton.Config.MsgFspec, 30, shouldClearPrevious: true);
                player.SendConsoleMessage(UltimateAFK.Singleton.Config.MsgFspec, "white");

                replacement.SetRole(player.Role);
            }
            else
            {
                MainHandler.ReplacingPlayers.Add(replacement, new AFKData
                {
                    NickName = player.Nickname,
                    Position = player.Position,
                    Role = player.Role,
                    Ammo = player.ReferenceHub.inventory.UserInventory.ReserveAmmo,
                    Health = player.Health,
                    Items = player.GetItems(),
                    SCP079 = new Scp079Data
                    {
                        Role = null,
                        Energy = 0f,
                        Experience = 0,
                    }
                });

                player.ClearInventory();
                player.SetRole(RoleTypeId.Spectator);

                if (UltimateAFK.Singleton.Config.AfkCount != -1)
                {
                    AfkCount++;

                    if (AfkCount >= UltimateAFK.Singleton.Config.AfkCount)
                    {
                        player.SendConsoleMessage(UltimateAFK.Singleton.Config.MsgKick, "white");

                        player.Kick(UltimateAFK.Singleton.Config.MsgKick);

                        replacement.SetRole(player.Role);
                        return;
                    }
                }

                player.SendBroadcast(UltimateAFK.Singleton.Config.MsgFspec, 30, shouldClearPrevious: true);
                player.SendConsoleMessage(UltimateAFK.Singleton.Config.MsgFspec, "white");

                replacement.SetRole(player.Role);
            }
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

        private bool Continue(Player ply)
        {
            return ply.IsAlive && Round.IsRoundStarted && Player.Count >= UltimateAFK.Singleton.Config.MinPlayers &&
                   (Owner.Role != RoleTypeId.Tutorial || !UltimateAFK.Singleton.Config.IgnoreTut);
        }
    }
}