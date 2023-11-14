using MEC;
using NWAPIPermissionSystem;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp079;
using PlayerRoles.PlayableScps.Scp096;
using PlayerRoles.Spectating;
using PluginAPI.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UltimateAFK.API.Structs;
using UnityEngine;

namespace UltimateAFK.API.Components
{
    /// <summary>
    /// Player component who check if the player is not moving.
    /// </summary>
    public class AfkCheckComponent : MonoBehaviour
    {
        // Main fields

        /// <summary>
        /// Player who owns this component.
        /// </summary>
        private protected Player Owner = null!;

        /// <summary>
        /// Cached <see cref="Player.UserId"/> of the <see cref="Owner"/>
        /// </summary>
        private protected string OwnerUserId = string.Empty;

        /// <summary>
        /// Plugin config instance.
        /// </summary>
        private Config PluginConfig => EntryPoint.Instance.Config;

        private void Start()
        {
            if (!TryGetOwner(out Owner))
            {
                Log.Debug($"{GetType().Name}::{nameof(Start)}: Error on getting player or player is not ready | {Owner is null}", PluginConfig.DebugMode);
                Disable();
                return;
            }

            OwnerUserId = Owner.UserId;
            // Starts the coroutine that checks if the player is moving, the coroutine is cancelled if the gameobject (the player) becomes null or the component is destroyed.
            CoroutineHandle = Timing.RunCoroutine(AfkStatusChecker().CancelWith(gameObject).CancelWith(this));
            Log.Debug($"Component {GetType().Name} fully loaded for {Owner.LogName}", PluginConfig.DebugMode, "UltimateAfk");
        }

        private void OnDestroy()
        {
            if (CoroutineHandle.IsRunning)
                CoroutineHandle.IsRunning = false; // Using this is the same as using Timing.KillCoroutine(CoroutineHandle); | Inspect CoroutineHandle struc for more info.
        }

        /// <summary>
        /// Destroy the component.
        /// </summary>
        public void Disable()
        {
            Destroy(this);
        }

        /// <summary>
        /// Coroutine that continuously checks and updates the AFK status of players.
        /// </summary>
        private IEnumerator<float> AfkStatusChecker()
        {

            while (true)
            {
                try
                {
                    CheckAfk();
                }
                catch (Exception e)
                {
                    Log.Error($"Error on {nameof(AfkCheckComponent)}::{nameof(AfkStatusChecker)}: {e.Message} || typeof {e.GetType()}");
                }

                yield return Timing.WaitForSeconds(1);
            }
        }

        /// <summary>
        /// Gets how many times this player has detected has AFK.
        /// </summary>
        /// <returns></returns>
        public int GetAfkTimes() => afkTimes;

        /// <summary>
        /// Sets how many times this player has detected has AFK.
        /// </summary>
        /// <param name="times"></param>
        public void SetAfkTimes(int times) => afkTimes = times;

        // Main Method

        /// <summary>
        /// Checks the AFK status of the player and takes appropriate actions.
        /// </summary>
        private void CheckAfk()
        {
            if (!Round.IsRoundStarted || Player.Count < PluginConfig.MinPlayers || Owner.Role == RoleTypeId.Tutorial && PluginConfig.IgnoreTut
                || PluginConfig.UserIdIgnored.Contains(Owner.UserId) || Owner.TemporaryData.StoredData.ContainsKey("uafk_disable_check"))
                return;

            // Retrieve player information.
            Vector3 worldPosition = Owner.Position;
            Vector3 cameraPosition = Owner.Camera.position;
            Quaternion cameraRotation = Owner.Camera.rotation;

            switch (Owner.Role)
            {
                case RoleTypeId.Scp096:
                    {
                        Scp096Role ??= Owner.RoleBase as Scp096Role;
                        if (Scp096Role is null)
                            break;

                        if (Scp096Role.IsAbilityState(Scp096AbilityState.TryingNotToCry) || IsMoving(worldPosition, cameraPosition, cameraRotation))
                        {
                            // set afk time to 0 player is moving.
                            secondsNotMoving = 0;
                        }
                        else
                        {
                            Log.Debug($"Player {Owner.LogName} is not moving. Seconds not moving: {secondsNotMoving}", PluginConfig.DebugMode);
                            HandleAfkState();
                        }

                        break;
                    }
                case RoleTypeId.Scp079:
                    {
                        Scp079Role ??= Owner.RoleBase as Scp079Role;

                        if (Scp079Role is null)
                            break;

                        cameraPosition = Scp079Role.CameraPosition;
                        cameraRotation = Scp079Role.CurrentCamera._cameraAnchor.transform.rotation;

                        if (IsMoving(worldPosition, cameraPosition, cameraRotation))
                        {
                            // Do nothing player is moving
                            secondsNotMoving = 0;
                        }
                        else
                        {
                            Log.Debug($"Player {Owner.LogName} is not moving. Seconds not moving: {secondsNotMoving}", PluginConfig.DebugMode);
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
                            Log.Debug($"Player {Owner.LogName} is not moving. Seconds not moving: {secondsNotMoving}", PluginConfig.DebugMode, "UltimateAfk");

                            HandleAfkState();
                        }
                        break;
                    }
            }
        }

        /// <summary>
        /// Replaces the specified player with a suitable replacement based on the specified role type.
        /// </summary>
        /// <param name="player">The player to be replaced.</param>
        /// <param name="roleType">The role type of the player to be replaced.</param>
        private void Replace(Player player, RoleTypeId roleType)
        {
            // Check if role is blacklisted
            if (PluginConfig.RoleTypeBlacklist?.Contains(roleType) == true)
            {
                Log.Debug($"player {player.Nickname} ({player.UserId}) has a role that is blacklisted so he will not be searched for a replacement player", PluginConfig.DebugMode, "UltimateAfk");

                player.ClearInventory();
                player.SetRole(RoleTypeId.Spectator);

                if (PluginConfig.AfkCount > -1)
                {
                    if (++afkTimes >= PluginConfig.AfkCount)
                    {
                        player.SendConsoleMessage(PluginConfig.MsgKick, "white");
                        player.Kick(PluginConfig.MsgKick);
                        return;
                    }
                }

                player.SendBroadcast(PluginConfig.MsgFspec, 30, shouldClearPrevious: true);
                player.SendConsoleMessage(PluginConfig.MsgFspec, "white");
                return;
            }

            // Get a replacement player
            Player? replacement = GetReplacement();

            if (replacement == null)
            {
                Log.Debug("Unable to find replacement player, moving to spectator...", PluginConfig.DebugMode, "UltimateAfk");

                player.ClearInventory();

                if (player.IsSCP)
                    player.ReferenceHub.roleManager.ServerSetRole(RoleTypeId.Spectator, RoleChangeReason.RemoteAdmin, RoleSpawnFlags.None);
                else
                    player.SetRole(RoleTypeId.Spectator);

                if (PluginConfig.AfkCount > -1)
                {
                    if (++afkTimes >= PluginConfig.AfkCount)
                    {
                        player.SendConsoleMessage(PluginConfig.MsgKick, "white");

                        player.Kick(PluginConfig.MsgKick);

                        return;
                    }
                }

                player.SendBroadcast(PluginConfig.MsgFspec, 30, shouldClearPrevious: true);
                player.SendConsoleMessage(PluginConfig.MsgFspec, "white");

                API.EventArgs.Events.OnDetectedAfkPlayer(new(player, false));
            }
            else
            {
                Log.Debug($"Replacement Player found: {replacement.LogName}", PluginConfig.DebugMode, "UltimateAfk");

                SaveData(replacement.UserId, roleType is RoleTypeId.Scp079);

                if (PluginConfig.AfkCount > -1)
                {
                    if (++afkTimes >= PluginConfig.AfkCount)
                    {
                        player.ClearInventory();
                        player.SendConsoleMessage(PluginConfig.MsgKick, "white");
                        player.Kick(PluginConfig.MsgKick);
                        replacement.SetRole(roleType);
                        return;
                    }
                }

                Log.Debug($"Cleaning player {player.Nickname} inventory", PluginConfig.DebugMode, "UltimateAfk");
                // Clear player inventory
                player.ClearInventory();
                //Send player a broadcast for being too long afk
                player.SendBroadcast(PluginConfig.MsgFspec, 25, shouldClearPrevious: true);
                player.SendConsoleMessage(PluginConfig.MsgFspec, "white");

                // Sends replacement to the role that had the afk
                Log.Debug($"Changing replacement player {replacement.LogName} role to {roleType}", PluginConfig.DebugMode, "UltimateAfk");
                replacement.SetRole(roleType);
                // Sends player to spectator
                Log.Debug($"Changing player {player.Nickname} to spectator", PluginConfig.DebugMode, "UltimateAfk");

                if (player.IsSCP)
                    player.ReferenceHub.roleManager.ServerSetRole(RoleTypeId.Spectator, RoleChangeReason.RemoteAdmin, RoleSpawnFlags.None);
                else
                    player.SetRole(RoleTypeId.Spectator);

                player.SendConsoleMessage(string.Format(PluginConfig.MsgReplaced, replacement.Nickname), "white");

                API.EventArgs.Events.OnDetectedAfkPlayer(new(player, false));
            }
        }

        /// <summary>
        /// Saves data for the specified replacement player.
        /// </summary>
        /// <param name="replacementUserId">The user ID of the replacement player.</param>
        /// <param name="isScp079">Flag indicating whether the replacement player is SCP-079.</param>
        private void SaveData(string replacementUserId, bool isScp079 = false)
        {
            Log.Debug($"Saving data for {replacementUserId}", PluginConfig.DebugMode, "UltimateAfk");

            AfkData data;

            if (isScp079)
            {
                Log.Debug($"Saving data: Player is SCP-079", PluginConfig.DebugMode, "UltimateAfk");
                if (Owner.RoleBase is Scp079Role scp079Role && scp079Role.SubroutineModule.TryGetSubroutine(out Scp079TierManager tierManager)
                    && scp079Role.SubroutineModule.TryGetSubroutine(out Scp079AuxManager energyManager))
                {
                    data = new AfkData(Owner.Nickname, Owner.Position, Owner.Role, null, new(), Owner.Health, new(tierManager.TotalExp, energyManager.CurrentAux));
                }
                else
                {
                    data = new AfkData(Owner.Nickname, Owner.Position, Owner.Role, null, new(), Owner.Health, null);
                    // Return early if SCP-079 data cannot be obtained
                    return;
                }
            }
            else
            {
                data = new AfkData(Owner.Nickname, Owner.Position, Owner.Role, Owner.GetAmmo(), Owner.GetItemTypes(), Owner.Health, null);
            }

            Log.Debug($"Saving data: Saving in ReplacingPlayersData", PluginConfig.DebugMode, "UltimateAfk");
            MainHandler.ReplacingPlayersData.Add(replacementUserId, data);
        }

        /// <summary>
        /// Handles the AFK state for the player.
        /// </summary>
        private void HandleAfkState()
        {
            // Increment the time the player has not been moving
            if (secondsNotMoving++ < PluginConfig.AfkTime)
                return;

            // Calculate remaining grace time
            var graceTimeRemaining = PluginConfig.GraceTime - (secondsNotMoving - PluginConfig.AfkTime);

            if (graceTimeRemaining > 0)
            {
                // The player is being considered AFK and is being warned that they have X time to move again or they will be kicked/replaced
                Owner.SendBroadcast(string.Format(PluginConfig.MsgGrace, graceTimeRemaining), 2,
                    shouldClearPrevious: true);
            }
            else
            {
                // The player has exceeded the grace time and is now officially AFK
                Log.Info($"{Owner.LogName} was detected as AFK");

                secondsNotMoving = 0;
                // Optionally invoke custom event or action when a player is detected as AFK
                // API.AfkEvents.Instance.InvokePlayerAfkDetected(Owner, false);
                Replace(Owner, Owner.Role);
            }
        }

        /// <summary>
        /// Gets the player to be used as a replacement, typically the longest-active spectator.
        /// </summary>
        /// <returns>The player to be used as a replacement, or <c>null</c> if no suitable replacement is found.</returns>
        private Player? GetReplacement()
        {
            try
            {
                Player? longestSpectator = null;
                float maxActiveTime = 0f;

                Log.Info($"Player user id using PluginAPI is " + Owner.UserId);
                Log.Info($"Player user id using manual method is " + Owner.ReferenceHub.authManager.UserId);

                // Find the longest-active spectator among non-ignored players
                foreach (var player in Player.GetPlayers().Where(p => !IgnorePlayer(p)))
                {
                    if (player.Role == RoleTypeId.Spectator && player.RoleBase is SpectatorRole spectatorRole)
                    {
                        // Update the longest spectator if the current one has a longer active time
                        if (spectatorRole.ActiveTime > maxActiveTime)
                        {
                            maxActiveTime = spectatorRole.ActiveTime;
                            longestSpectator = player;
                        }
                    }
                }

                return longestSpectator;
            }
            catch (Exception e)
            {
                // Log and handle any exceptions that occur during the process
                Log.Error($"Error in {nameof(GetReplacement)}: {e} || Type: {e.GetType()}");
                return null;
            }
        }

        /// <summary>
        /// Determines whether the specified player should be ignored in the context of AFK checks.
        /// </summary>
        /// <param name="player">The player to check.</param>
        /// <returns>
        ///   <c>true</c> if the player should be ignored; otherwise, <c>false</c>.
        /// </returns>
        private bool IgnorePlayer(Player player)
        {
            // Check various conditions to determine if the player should be ignored
            if (!player.IsReady ||                            // Player is not ready
                PluginConfig.UserIdIgnored.Contains(OwnerUserId) ||   // Player's user ID is in the ignored list
                player.TemporaryData.StoredData.ContainsKey("uafk_disable") ||   // Player has AFK checking disabled
                player.UserId == OwnerUserId ||                // Player is the same as the owner
                player.IsAlive ||                              // Player is alive
                player.CheckPermission("uafk.ignore") ||      // Player has the uafk.ignore permission
                player.IsServer ||                             // Player is a server
                player.UserId.Contains("@server") ||           // Player's user ID contains "@server"
                player.UserId.Contains("@npc") ||              // Player's user ID contains "@npc"
                MainHandler.ReplacingPlayersData.TryGetValue(player.UserId, out _))  // Player is being replaced
            {
                return true;
            }

            return false;
        }


        /// <summary>
        /// Determines whether an object is moving based on its world position, camera position, and camera rotation.
        /// </summary>
        /// <param name="worldPosition">The world position of the object.</param>
        /// <param name="cameraPosition">The position of the camera.</param>
        /// <param name="cameraRotation">The rotation of the camera.</param>
        /// <returns>
        ///   <c>true</c> if the object is moving; otherwise, <c>false</c>.
        /// </returns>
        private bool IsMoving(Vector3 worldPosition, Vector3 cameraPosition, Quaternion cameraRotation)
        {
            // Check if any of the parameters have changed since the last check
            bool hasChanged = worldPosition != _lastPosition ||
                              cameraPosition != _lastCameraPosition ||
                              cameraRotation != _lastCameraRotation;

            // Update the last recorded values
            if (hasChanged)
            {
                _lastPosition = worldPosition;
                _lastCameraPosition = cameraPosition;
                _lastCameraRotation = cameraRotation;
            }

            // Return true if there is a change, indicating movement
            return hasChanged;
        }


        /// <summary>
        /// Try get the Owner of this component, if fails the component is destroyed.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        protected bool TryGetOwner(out Player player)
        {
            player = Player.Get(gameObject);
            return player != null && player.IsReady;
        }
        // fields //

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
        private CoroutineHandle CoroutineHandle;
        // Cached Scp079Role
        private Scp079Role? Scp079Role = null;
        // Cached Scp096Role
        private Scp096Role? Scp096Role = null;
    }
}
