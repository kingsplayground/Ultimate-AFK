using MEC;
using NWAPIPermissionSystem;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp079;
using PlayerRoles.PlayableScps.Scp096;
using PluginAPI.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UltimateAFK.Handlers;
using UnityEngine;

namespace UltimateAFK.Resources.Component
{
    [RequireComponent(typeof(ReferenceHub))]
    public class AFKComponent : MonoBehaviour
    {
        private void Awake()
        {
            // Gets PluginAPI.Core.Player from the gameobject.
            if (Player.Get(gameObject) is not { } ply)
            {
                Log.Error($"{this} Error Getting Player");
                Destroy(this);
                return;
            }

            // Sets owner variable
            Owner = ply;
            _ownerId = ply.UserId;

            // Coroutine dies when the component or the ReferenceHub (Player) is destroyed.
            _checkHandle = Timing.RunCoroutine(Check().CancelWith(this).CancelWith(gameObject));
        }

        private void OnDestroy()
        {
            Timing.KillCoroutines(_checkHandle);
        }

        private IEnumerator<float> Check()
        {
            for (; ; )
            {
                yield return Timing.WaitForSeconds(1.3f);

                Log.Debug("Calling CheckAFK", Plugin.Config.DebugMode && Plugin.Config.SpamLogs);
                try
                {
                    AfkCheck();
                }
                catch (Exception e)
                {
                    Log.Error($"Error in {nameof(Check)}: &2{e}&r");
                }
            }
        }

        private void AfkCheck()
        {
            if (!UglyCheck())
                return;

            if (Owner.Role is RoleTypeId.Tutorial && Plugin.Config.IgnoreTut)
                return;

            // save current player position
            var worldPosition = Owner.Position;
            var cameraPosition = Owner.ReferenceHub.roleManager.CurrentRole is Scp079Role scp079 ? scp079.CurrentCamera.CameraPosition : Owner.Camera.position;
            var cameraRotation = Owner.ReferenceHub.roleManager.CurrentRole is Scp079Role scp0792 ? new Quaternion(scp0792.CurrentCamera.HorizontalRotation, scp0792.CurrentCamera.VerticalRotation, scp0792.CurrentCamera.RollRotation, 0f) : Owner.Camera.rotation;

            // Player is moving.
            if (cameraPosition != _lastCameraPosition || worldPosition != _lastPosition || _lastCameraRotation != cameraRotation)
            {
                _lastCameraPosition = cameraPosition;
                _lastCameraRotation = cameraRotation;
                _lastPosition = worldPosition;
                _afkTime = 0f;
            }
            else
            {
                // Check if the player role is Scp096 and current state is in TryNotToCry if it is return.
                if (Owner.Role == RoleTypeId.Scp096 && (Owner.RoleBase as Scp096Role).IsAbilityState(Scp096AbilityState.TryingNotToCry))
                    return;

                Log.Debug($"{Owner.Nickname} is in not moving, AFKTime: {_afkTime}", UltimateAFK.Singleton.Config.DebugMode);
                // If the time the player is afk is less than the limit, return.
                if (_afkTime++ < UltimateAFK.Singleton.Config.AfkTime) return;

                // Get grace time
                var graceNumb = UltimateAFK.Singleton.Config.GraceTime - (_afkTime - UltimateAFK.Singleton.Config.AfkTime);

                if (graceNumb > 0)
                {
                    // The player is being considered afk and is being warned that they have X time to move again or they will be kicked/replaced
                    Owner.SendBroadcast(string.Format(UltimateAFK.Singleton.Config.MsgGrace, graceNumb), 2,
                        shouldClearPrevious: true);
                }
                else
                {
                    Log.Info($"{Owner.Nickname} ({Owner.UserId}) was detected as AFK");
                    _afkTime = 0f;
                    Replace(Owner, Owner.Role);
                }
            }
        }

        private void Replace(Player player, RoleTypeId roleType)
        {
            // Check if role is blacklisted
            if (Plugin.Config.RoleTypeBlacklist?.Count > 0 && Plugin.Config.RoleTypeBlacklist.Contains(roleType))
            {
                Log.Debug($"player {player.Nickname} ({player.UserId}) has a role that is blacklisted so he will not be searched for a replacement player", Plugin.Config.DebugMode);

                player.ClearInventory();
                player.SetRole(RoleTypeId.Spectator);

                if (Plugin.Config.AfkCount > -1)
                {
                    AfkTimes++;

                    if (AfkTimes >= Plugin.Config.AfkCount)
                    {
                        player.SendConsoleMessage(Plugin.Config.MsgKick, "white");
                        player.Kick(Plugin.Config.MsgKick);
                        return;
                    }
                }

                player.SendBroadcast(Plugin.Config.MsgFspec, 30, shouldClearPrevious: true);
                player.SendConsoleMessage(Plugin.Config.MsgFspec, "white");
                return;
            }

            // Get player replacement
            Player replacement = GetReplacement();

            // If no replacement player is found, I change the player's role to spectator
            if (replacement == null)
            {
                Log.Debug("Unable to find replacement player, moving to spectator...", UltimateAFK.Singleton.Config.DebugMode);

                player.ClearInventory();
                player.SetRole(RoleTypeId.Spectator);

                if (Plugin.Config.AfkCount > -1)
                {
                    AfkTimes++;

                    if (AfkTimes >= Plugin.Config.AfkCount)
                    {
                        player.SendConsoleMessage(Plugin.Config.MsgKick, "white");

                        player.Kick(Plugin.Config.MsgKick);

                        return;
                    }
                }

                player.SendBroadcast(Plugin.Config.MsgFspec, 30, shouldClearPrevious: true);
                player.SendConsoleMessage(Plugin.Config.MsgFspec, "white");
            }
            else
            {
                Log.Debug($"Replacement Player found: {replacement.Nickname} ({replacement.UserId})", Plugin.Config.DebugMode);
                Log.Debug($"Saving data of player {player.Nickname} in the dictionary.", Plugin.Config.DebugMode);
                SaveData(replacement.UserId, roleType is RoleTypeId.Scp079);

                if (Plugin.Config.AfkCount > -1)
                {
                    AfkTimes++;

                    if (AfkTimes >= Plugin.Config.AfkCount)
                    {
                        player.SendConsoleMessage(Plugin.Config.MsgKick, "white");

                        player.Kick(Plugin.Config.MsgKick);

                        replacement.SetRole(roleType);
                        return;
                    }
                }

                Log.Debug($"Cleaning player {player.Nickname} inventory", Plugin.Config.DebugMode);
                // Clear player inventory
                player.ClearInventory();
                //Send player a broadcast for being too long afk
                player.SendBroadcast(Plugin.Config.MsgFspec, 25, shouldClearPrevious: true);
                player.SendConsoleMessage(Plugin.Config.MsgFspec, "white");

                // Sends replacement to the role that had the afk
                Log.Debug($"Changing replacement player  {replacement.Nickname} role to {roleType}", Plugin.Config.DebugMode);
                replacement.SetRole(roleType);
                // Sends player to spectator
                Log.Debug($"Changing player {player.Nickname} to spectator", Plugin.Config.DebugMode);
                player.SetRole(RoleTypeId.Spectator);
                player.SendConsoleMessage(string.Format(UltimateAFK.Singleton.Config.MsgReplaced, replacement.Nickname), "white");
            }
        }

        private void SaveData(string replacementUserId, bool isScp079 = false)
        {
            if (isScp079)
            {
                if (Owner.RoleBase is Scp079Role scp079Role && scp079Role.SubroutineModule.TryGetSubroutine(out Scp079TierManager tierManager)
                       && scp079Role.SubroutineModule.TryGetSubroutine(out Scp079AuxManager energyManager))
                {
                    var afkData = new AFKData()
                    {
                        NickName = Owner.Nickname,
                        Position = Owner.Position,
                        Role = Owner.Role,
                        Ammo = null,
                        Health = Owner.Health,
                        Items = null,
                        SCP079 = new Scp079Data
                        {
                            Role = scp079Role,
                            Energy = energyManager.CurrentAux,
                            Experience = tierManager.TotalExp,
                        }
                    };
                    Log.Info($"Datos guardados: {afkData.SCP079.Experience} | {afkData.SCP079.Experience}");
                    MainHandler.ReplacingPlayersData.Add(replacementUserId, afkData);
                }

                return;
            }

            var ammo = Extensions.GetAmmo(Owner);

            var data = new AFKData()
            {
                NickName = Owner.Nickname,
                Position = Owner.Position,
                Role = Owner.Role,
                Ammo = ammo,
                Health = Owner.Health,
                Items = Owner.GetItems(),
                SCP079 = new Scp079Data
                {
                    Role = null,
                    Energy = 0f,
                    Experience = 0
                }
            };

            MainHandler.ReplacingPlayersData.Add(replacementUserId, data);
        }

        /// <summary>
        /// Obtains a player who qualifies for replacement.
        /// </summary>
        private Player GetReplacement()
        {
            try
            {
                var players = new List<Player>();

                foreach (var player in Player.GetPlayers())
                {
                    if(player is null) continue;

                    if (player.IsAlive || player == Owner || player.CheckPermission("uafk.ignore") || player.IsServer || player.UserId.Contains("@server")
                        || Plugin.Config.IgnoreOverwatch && player.IsOverwatchEnabled || MainHandler.ReplacingPlayersData.TryGetValue(player.UserId, out _))
                        continue;

                    players.Add(player);
                }

                return players.Any() ? players.ElementAtOrDefault(UnityEngine.Random.Range(0, players.Count)) : null;
            }
            catch (Exception e)
            {
                Log.Error($"Error in {nameof(GetReplacement)} of type {e.GetType()}: {e} ");
                return null;
            }
        }

        private bool UglyCheck()
        {
            return Owner.IsAlive && Round.IsRoundStarted && Player.Count >= UltimateAFK.Singleton.Config.MinPlayers;
        }

        // Owner PluginAPI.Core.Player
        private Player Owner;

        // How many times was the owner afk
        private int AfkTimes;

        // Owner UserID
        private string _ownerId;

        // Position in the world
        private Vector3 _lastPosition;

        // Player camera position
        private Vector3 _lastCameraPosition;

        // Player camera rotation
        private Quaternion _lastCameraRotation;

        // The time the player was afk
        private float _afkTime;

        // Using a MEC Coroutine is more optimized than using Unity methods.
        private CoroutineHandle _checkHandle;

        private readonly UltimateAFK Plugin = UltimateAFK.Singleton;
    }
}
