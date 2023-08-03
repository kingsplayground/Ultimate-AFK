using MEC;
using NWAPIPermissionSystem;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp079;
using PlayerRoles.PlayableScps.Scp096;
using PlayerRoles.Spectating;
using PluginAPI.Core;
using System;
using System.Collections.Generic;
using UltimateAFK.Handlers;
using UnityEngine;

namespace UltimateAFK.Resources.Component
{
    public class AfkComponent : MonoBehaviour
    {
        // ---------- Fields --------
        private Player Owner;
        private Scp079Role Scp079Role = null;
        private Scp096Role Scp096Role = null;
        private readonly UltimateAFK Plugin = UltimateAFK.Singleton;

        // Seconds a player was not moving
        private int secondsNotMoving;
        // Position in the world
        private Vector3 _lastPosition;
        // Player camera position
        private Vector3 _lastCameraPosition;
        // Player camera rotation
        private Quaternion _lastCameraRotation;
        // Number of times that the player was detected as AFK
        private int afkTimes = 0;
        // Coroutine that handles afk checking.
        private CoroutineHandle checkCoroutine;
        // Cached owner user id
        private string ownerUserId;

        // ----------- Unity Methods ---------------

        private void Start()
        {
            if (!Player.TryGet(gameObject, out Owner))
            {
                Log.Error($"Error on {nameof(AfkComponent)}::{nameof(Start)}: Error on try get player");
                Destroy(this);
                return;
            }

            // Check if the player is not fully connected to the server.
            if (!Owner.IsReady)
            {
                Log.Debug("Destroying a component because the player is not fully connected to the server", UltimateAFK.Singleton.Config.DebugMode);
                Destroy(this);
                return;
            }

            // Cached Rolebase
            switch (Owner.RoleBase)
            {
                case Scp079Role scp079Role:
                    {
                        Scp079Role = scp079Role;
                        break;
                    }
                case Scp096Role scp096Role:
                    {
                        Scp096Role = scp096Role;
                        break;
                    }
            }

            ownerUserId = Owner.UserId;
            // Starts the coroutine that checks if the player is moving, the coroutine is cancelled if the gameobject (the player) becomes null or the component is destroyed.
            checkCoroutine = Timing.RunCoroutine(AfkStatusChecker().CancelWith(gameObject).CancelWith(this));
        }

        private void OnDestroy()
        {
            Timing.KillCoroutines(checkCoroutine);
        }

        // ---------------- Main method --------------

        private void CheckAfkStatus()
        {
            if (!Round.IsRoundStarted || Player.Count < Plugin.Config.MinPlayers || Owner.Role == RoleTypeId.Tutorial && Plugin.Config.IgnoreTut
                || Owner.TemporaryData.StoredData.ContainsKey("uafk_disable_check"))
                return;

            Vector3 worldPosition = Owner.Position;
            Vector3 cameraPosition = Owner.Camera.position;
            Quaternion cameraRotation = Owner.Camera.rotation;

            switch (Owner.Role)
            {
                case RoleTypeId.Scp096:
                    {
                        Scp096Role ??= Owner.RoleBase as Scp096Role;

                        // Player is moving or its crying on a wall/door
                        if (Scp096Role.IsAbilityState(Scp096AbilityState.TryingNotToCry) || IsMoving(worldPosition, cameraPosition, cameraRotation))
                        {
                            // Do nothing player is moving
                            secondsNotMoving = 0;
                        }
                        else
                        {
                            Log.Debug($"Player {Owner.LogName} is not moving. Seconds not moving: {secondsNotMoving}", UltimateAFK.Singleton.Config.DebugMode);

                            HandleAfkState();
                        }

                        break;
                    }
                case RoleTypeId.Scp079:
                    {
                        Scp079Role ??= Owner.RoleBase as Scp079Role;

                        cameraPosition = Scp079Role.CameraPosition;
                        cameraRotation = new Quaternion(Scp079Role.CurrentCamera.HorizontalRotation, Scp079Role.CurrentCamera.VerticalRotation, Scp079Role.CurrentCamera.RollRotation, 0f);
                        if (IsMoving(worldPosition, cameraPosition, cameraRotation))
                        {
                            // Do nothing player is moving
                            secondsNotMoving = 0;
                        }
                        else
                        {
                            Log.Debug($"Player {Owner.LogName} is not moving. Seconds not moving: {secondsNotMoving}", UltimateAFK.Singleton.Config.DebugMode);

                            HandleAfkState();
                        }
                        break;
                    }
                case RoleTypeId.Overwatch:
                case RoleTypeId.Filmmaker:
                case RoleTypeId.None:
                case RoleTypeId.Spectator:
                    {
                        // Do nothing player is not alive.
                        secondsNotMoving = 0;
                        break;
                    }
                default:
                    {
                        // Player is human or a SCP with more simple mechanic arround camera/position

                        if (IsMoving(worldPosition, cameraPosition, cameraRotation))
                        {
                            // Do nothing player is moving
                            secondsNotMoving = 0;
                        }
                        else
                        {
                            Log.Debug($"Player {Owner.LogName} is not moving. Seconds not moving: {secondsNotMoving}", UltimateAFK.Singleton.Config.DebugMode);

                            HandleAfkState();
                        }
                        break;
                    }
            }
        }

        private void HandleAfkState()
        {
            if (secondsNotMoving++ < UltimateAFK.Singleton.Config.AfkTime) return;

            // Calculate grace time
            var graceTimeRemaining = UltimateAFK.Singleton.Config.GraceTime - (secondsNotMoving - UltimateAFK.Singleton.Config.AfkTime);

            if (graceTimeRemaining > 0)
            {
                // The player is being considered afk and is being warned that they have X time to move again or they will be kicked/replaced
                Owner.SendBroadcast(string.Format(UltimateAFK.Singleton.Config.MsgGrace, graceTimeRemaining), 2,
                    shouldClearPrevious: true);
            }
            else
            {
                Log.Info($"{Owner.LogName} was detected as AFK");
                secondsNotMoving = 0;
                API.AfkEvents.Instance.InvokePlayerAfkDetected(Owner, false);
                Replace(Owner, Owner.Role);
            }
        }

        // ---------------- Private methods --------------

        private bool IsMoving(Vector3 worlPosition, Vector3 cameraPosition, Quaternion cameraRotation)
        {
            if (worlPosition != _lastPosition || cameraPosition != _lastCameraPosition || cameraRotation != _lastCameraRotation)
            {
                _lastPosition = worlPosition;
                _lastCameraPosition = cameraPosition;
                _lastCameraRotation = cameraRotation;
                return true;
            }

            return false;
        }

        private void Replace(Player player, RoleTypeId roleType)
        {
            // Check if role is blacklisted
            if (Plugin.Config.RoleTypeBlacklist?.Contains(roleType) == true)
            {
                Log.Debug($"player {player.Nickname} ({player.UserId}) has a role that is blacklisted so he will not be searched for a replacement player", Plugin.Config.DebugMode);

                player.ClearInventory();
                player.SetRole(RoleTypeId.Spectator);

                if (Plugin.Config.AfkCount > -1)
                {
                    afkTimes++;

                    if (afkTimes >= Plugin.Config.AfkCount)
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

            // Get a replacement player
            Player replacement = GetReplacement();

            if (replacement == null)
            {
                Log.Debug("Unable to find replacement player, moving to spectator...", UltimateAFK.Singleton.Config.DebugMode);

                player.ClearInventory();
                player.SetRole(RoleTypeId.Spectator);

                if (Plugin.Config.AfkCount > -1)
                {
                    afkTimes++;

                    if (afkTimes >= Plugin.Config.AfkCount)
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
                Log.Debug($"Replacement Player found: {replacement.LogName}", Plugin.Config.DebugMode);

                SaveData(replacement.UserId, roleType is RoleTypeId.Scp079);

                if (Plugin.Config.AfkCount > -1)
                {
                    afkTimes++;

                    if (afkTimes >= Plugin.Config.AfkCount)
                    {
                        player.ClearInventory();
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
                Log.Debug($"Changing replacement player {replacement.LogName} role to {roleType}", Plugin.Config.DebugMode);
                replacement.SetRole(roleType);
                // Sends player to spectator
                Log.Debug($"Changing player {player.Nickname} to spectator", Plugin.Config.DebugMode);
                player.SetRole(RoleTypeId.Spectator);
                player.SendConsoleMessage(string.Format(UltimateAFK.Singleton.Config.MsgReplaced, replacement.Nickname), "white");
            }
        }

        /// <summary>
        /// Searches and returns the player with the longest active time in spectator mode.
        /// If no valid player is found, it returns null.
        /// </summary>
        /// <returns>The player with the longest active time or null if none is found.</returns>
        private Player GetReplacement()
        {
            try
            {
                Player longestSpectator = null;
                float maxActiveTime = 0f;

                foreach (var player in Player.GetPlayers())
                {
                    if (IgnorePlayer(player))
                        continue;

                    // It is faster to compare an enum than to compare a class.
                    if (player.Role == RoleTypeId.Spectator && player.RoleBase is SpectatorRole role)
                    {
                        if (role.ActiveTime > maxActiveTime)
                        {
                            maxActiveTime = role.ActiveTime;
                            longestSpectator = player;
                        }
                    }
                }

                return longestSpectator;
            }
            catch (Exception e)
            {
                Log.Error($"Error on {nameof(AfkComponent)}::{nameof(GetReplacement)}: {e.Message} || typeof {e.GetType()}");
                return null;
            }
        }

        /// <summary>
        /// Checks if the provided player should be ignored in the AFK replacement process.
        /// </summary>
        /// <param name="player">The player to check.</param>
        /// <returns>True if the player should be ignored, otherwise false.</returns>
        private bool IgnorePlayer(Player player)
        {
            if (!player.IsReady || player.TemporaryData.StoredData.ContainsKey("uafk_disable") || player.UserId == ownerUserId || player.IsAlive || player.CheckPermission("uafk.ignore") || player.IsServer || player.UserId.Contains("@server") || player.UserId.Contains("@npc") || MainHandler.ReplacingPlayersData.TryGetValue(player.UserId, out _))
                return true;

            return false;
        }

        /// <summary>
        /// Saves the relevant data of a player for potential AFK replacement.
        /// </summary>
        /// <param name="replacementUserId">The user ID of the player who will be the replacement.</param>
        /// <param name="isScp079">Specifies if the player is SCP-079 (True if SCP-079, False if not).</param>
        private void SaveData(string replacementUserId, bool isScp079 = false)
        {
            AFKData data;

            if (isScp079)
            {
                if (Owner.RoleBase is Scp079Role scp079Role && scp079Role.SubroutineModule.TryGetSubroutine(out Scp079TierManager tierManager)
                    && scp079Role.SubroutineModule.TryGetSubroutine(out Scp079AuxManager energyManager))
                {
                    data = new AFKData()
                    {
                        NickName = Owner.Nickname,
                        Position = Owner.Position,
                        Role = Owner.Role,
                        Ammo = null,
                        Health = Owner.Health,
                        Items = null,
                        SCP079 = new Scp079Data
                        {
                            Energy = energyManager.CurrentAux,
                            Experience = tierManager.TotalExp,
                        }
                    };
                }
                else
                {
                    // Return early if SCP-079 data cannot be obtained
                    return;
                }
            }
            else
            {
                var ammo = Extensions.GetAmmo(Owner);

                data = new AFKData()
                {
                    NickName = Owner.Nickname,
                    Position = Owner.Position,
                    Role = Owner.Role,
                    Ammo = ammo,
                    Health = Owner.Health,
                    Items = Owner.GetItems(),
                    SCP079 = new Scp079Data
                    {
                        Energy = 0f,
                        Experience = 0
                    }
                };
            }

            MainHandler.ReplacingPlayersData.Add(replacementUserId, data);
        }


        // ---------------- Coroutines -------------------

        /// <summary>
        /// Coroutine that continuously checks and updates the AFK status of players.
        /// </summary>
        private IEnumerator<float> AfkStatusChecker()
        {

            while (true)
            {
                try
                {
                    CheckAfkStatus();
                }
                catch (Exception e)
                {
                    Log.Error($"Error on {nameof(AfkComponent)}::{nameof(AfkStatusChecker)}: {e.Message} || typeof {e.GetType()}");
                }

                yield return Timing.WaitForSeconds(1);
            }
        }
    }
}
