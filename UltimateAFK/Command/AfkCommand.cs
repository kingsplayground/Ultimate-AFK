using CommandSystem;
using CustomPlayerEffects;
using MapGeneration;
using MEC;
using NWAPIPermissionSystem;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp079;
using PlayerRoles.Spectating;
using PluginAPI.Core;
using System;
using System.Collections.Generic;
using UltimateAFK.API;
using UltimateAFK.Handlers;
using UltimateAFK.Resources;
using UltimateAFK.Resources.Component;
using UnityEngine;

namespace UltimateAFK.Command
{
    [CommandHandler(typeof(ClientCommandHandler))]
    public class AfkCommand : ICommand
    {
        public string Command { get; } = "afk";
        public string[] Aliases { get; } = Array.Empty<string>();
        public string Description { get; } = "By using this command you will be moved to spectator and if the server allows it a player will replace you.";

        public Dictionary<string, float> InCooldown = new();

        public static Dictionary<string, int> CommandUses = new();
        public static List<string> InProcess = new();
        public static List<CoroutineHandle> AllCoroutines = new();

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            try
            {
                if (!UltimateAFK.Singleton.Config.CommandConfig.IsEnabled)
                {
                    response = UltimateAFK.Singleton.Config.CommandConfig.Responses.OnDisable;
                    return false;
                }
                if (!Round.IsRoundStarted)
                {
                    response = UltimateAFK.Singleton.Config.CommandConfig.Responses.OnRoundIsNotStarted;
                    return false;
                }

                var ply = Player.Get(sender);
                if (ply == null || !ply.IsReady)
                {
                    response = "The player is not fully connected to the server. you cannot use this command, please try to reconnect to the server.";
                    return false;
                }
                if (!ply.IsAlive)
                {
                    response = UltimateAFK.Singleton.Config.CommandConfig.Responses.OnPlayerIsDead;
                    return false;
                }
                if (ply.Zone == FacilityZone.Other || ply.EffectsManager.TryGetEffect<Corroding>(out var corroding) && corroding.IsEnabled)
                {
                    response = UltimateAFK.Singleton.Config.CommandConfig.Responses.OnPocketDimension;
                    return false;
                }
                if (UltimateAFK.Singleton.Config.CommandConfig.DisableForCertainRole && UltimateAFK.Singleton.Config.CommandConfig.RoleTypeIdBlackList.Contains(ply.Role))
                {
                    response = string.Format(UltimateAFK.Singleton.Config.CommandConfig.Responses.OnBlackListedRole,
                        ply.Role);
                    return false;
                }
                if (UltimateAFK.Singleton.Config.CommandConfig.ExclusiveForGroups &&
                    !UltimateAFK.Singleton.Config.CommandConfig.UserGroupsAllowed.Contains(ply.ReferenceHub.serverRoles.Group?.BadgeText))
                {
                    response = UltimateAFK.Singleton.Config.CommandConfig.Responses.OnGroupExclusive;
                    return false;
                }
                if (ply.EffectsManager.TryGetEffect<SeveredHands>(out var severedHands) && severedHands.IsEnabled)
                {
                    response = UltimateAFK.Singleton.Config.CommandConfig.Responses.OnSevereHands;
                    return false;
                }
                if (ply.EffectsManager.TryGetEffect<CardiacArrest>(out var cardiacArrest) && cardiacArrest.IsEnabled)
                {
                    response = UltimateAFK.Singleton.Config.CommandConfig.Responses.OnHearthAttack;
                    return false;
                }
                if (ply.InElevator())
                {
                    response = UltimateAFK.Singleton.Config.CommandConfig.Responses.OnElevatorMoving;
                    return false;
                }
                if (ply.TemporaryData.StoredData.ContainsKey("uafk_disable"))
                {
                    response = UltimateAFK.Singleton.Config.CommandConfig.Responses.OnUafkDisable;
                    return false;
                }
                if (CommandUses.TryGetValue(ply.UserId, out var uses) && UltimateAFK.Singleton.Config.CommandConfig.UseLimitsPerRound > 0 && uses > UltimateAFK.Singleton.Config.CommandConfig.UseLimitsPerRound)
                {
                    response = UltimateAFK.Singleton.Config.CommandConfig.Responses.OnLimit;
                    return false;
                }
                if (InProcess.Contains(ply.UserId))
                {
                    response = string.Format(UltimateAFK.Singleton.Config.CommandConfig.Responses.OnTryToExecute, UltimateAFK.Singleton.Config.CommandConfig.SecondsStill);
                    return false;
                }
                if (InCooldown.TryGetValue(ply.UserId, out var cooldown))
                {
                    // In cooldown
                    if (cooldown >= Time.time)
                    {
                        var cooldownTimer = (int)(cooldown - Time.time);

                        response = string.Format(UltimateAFK.Singleton.Config.CommandConfig.Responses.OnCooldown,
                            cooldownTimer);
                        return false;
                    }
                    else
                    {
                        // Not in cooldown
                        InCooldown.Remove(ply.UserId);
                    }
                }

                AllCoroutines.Add(Timing.RunCoroutine(AfkCountdown(ply, ply.UserId, ply.Role, ply.Position)));
                InCooldown.Add(ply.UserId, Time.time + UltimateAFK.Singleton.Config.CommandConfig.Cooldown);
                response = string.Format(UltimateAFK.Singleton.Config.CommandConfig.Responses.OnSuccess, UltimateAFK.Singleton.Config.CommandConfig.SecondsStill);
                return true;
            }
            catch (Exception e)
            {
                Log.Error($"Error on {nameof(AfkCommand)}::{nameof(Execute)}: {e} || Typeof {e.GetType()}");
                response = $"Error on {nameof(AfkCommand)}::{nameof(Execute)}: {e.Message}";
                return false;
            }
        }


        private IEnumerator<float> AfkCountdown(Player player, string userid, RoleTypeId roleType, Vector3 position)
        {
            InProcess.Add(userid);

            for (int i = UltimateAFK.Singleton.Config.CommandConfig.SecondsStill; i >= 0; i--)
            {
                yield return Timing.WaitForSeconds(1);

                if (player == null || !player.IsAlive || !Round.IsRoundStarted)
                {
                    Log.Info("Killing coroutine in first if");
                    InProcess.Remove(userid);
                    yield break;
                }

                if (position != player.Position)
                {
                    InProcess.Remove(userid);
                    player.SendConsoleMessage(string.Format(UltimateAFK.Singleton.Config.CommandConfig.Responses.OnMoving, UltimateAFK.Singleton.Config.CommandConfig.SecondsStill), "red");
                    player.ReceiveHint(string.Format(UltimateAFK.Singleton.Config.CommandConfig.Responses.OnMoving, UltimateAFK.Singleton.Config.CommandConfig.SecondsStill), 5);
                    yield break;
                }

                string text = string.Format(UltimateAFK.Singleton.Config.CommandConfig.Responses.OnWaitingForAfk, i);
                player.ReceiveHint(text, 1);
                player.SendConsoleMessage(text, "red");
            }

            Replace(player, roleType);
            AfkEvents.Instance.InvokePlayerAfkDetected(player, true);
            if (UltimateAFK.Singleton.Config.CommandConfig.UseLimitsPerRound > 0)
                AddUse(userid);
            InProcess.Remove(userid);
        }

        private void Replace(Player player, RoleTypeId roleType)
        {
            try
            {
                // Check if role is blacklisted
                if (UltimateAFK.Singleton.Config.RoleTypeBlacklist?.Contains(roleType) == true)
                {
                    Log.Debug($"player {player.Nickname} ({player.UserId}) has a role that is blacklisted so he will not be searched for a replacement player", UltimateAFK.Singleton.Config.DebugMode);

                    player.ClearInventory();
                    player.SetRole(RoleTypeId.Spectator);

                    player.SendBroadcast(UltimateAFK.Singleton.Config.MsgFspec, 30, shouldClearPrevious: true);
                    player.SendConsoleMessage(UltimateAFK.Singleton.Config.MsgFspec, "white");
                    return;
                }

                // Get a replacement player
                Player replacement = GetReplacement(player.UserId);
                if (replacement == null)
                {
                    Log.Debug("Unable to find replacement player, moving to spectator...", UltimateAFK.Singleton.Config.DebugMode);

                    player.ClearInventory();
                    player.SetRole(RoleTypeId.Spectator);

                    player.SendBroadcast(UltimateAFK.Singleton.Config.MsgFspec, 30, shouldClearPrevious: true);
                    player.SendConsoleMessage(UltimateAFK.Singleton.Config.MsgFspec, "white");
                }
                else
                {
                    Log.Debug($"Replacement Player found: {replacement.LogName}", UltimateAFK.Singleton.Config.DebugMode);

                    SaveData(player, replacement.UserId, roleType is RoleTypeId.Scp079);

                    Log.Debug($"Cleaning player {player.Nickname} inventory", UltimateAFK.Singleton.Config.DebugMode);
                    // Clear player inventory
                    player.ClearInventory();
                    //Send player a broadcast for being too long afk
                    player.SendBroadcast(UltimateAFK.Singleton.Config.MsgFspec, 25, shouldClearPrevious: true);
                    player.SendConsoleMessage(UltimateAFK.Singleton.Config.MsgFspec, "white");

                    // Sends replacement to the role that had the afk
                    Log.Debug($"Changing replacement player {replacement.LogName} role to {roleType}", UltimateAFK.Singleton.Config.DebugMode);
                    replacement.SetRole(roleType);
                    // Sends player to spectator
                    Log.Debug($"Changing player {player.Nickname} to spectator", UltimateAFK.Singleton.Config.DebugMode);
                    player.SetRole(RoleTypeId.Spectator);
                    player.SendConsoleMessage(string.Format(UltimateAFK.Singleton.Config.MsgReplaced, replacement.Nickname), "white");
                }
            }
            catch (Exception e)
            {
                Log.Error($"Error on {nameof(AfkCommand)}::{nameof(Replace)}: {e} || Typeof {e.GetType()}");
            }
        }

        /// <summary>
        /// Searches and returns the player with the longest active time in spectator mode.
        /// If no valid player is found, it returns null.
        /// </summary>
        /// <returns>The player with the longest active time or null if none is found.</returns>
        private Player GetReplacement(string userid)
        {
            try
            {
                Player longestSpectator = null;
                float maxActiveTime = 0f;

                foreach (var player in Player.GetPlayers())
                {
                    if (IgnorePlayer(player, userid))
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
        private bool IgnorePlayer(Player player, string ignoreUserId)
        {
            if (!player.IsReady || player.TemporaryData.StoredData.ContainsKey("uafk_disable") || player.UserId == ignoreUserId || player.IsAlive || player.CheckPermission("uafk.ignore") || player.IsServer || player.UserId.Contains("@server") || player.UserId.Contains("@npc") || MainHandler.ReplacingPlayersData.TryGetValue(player.UserId, out _))
                return true;

            return false;
        }

        /// <summary>
        /// Saves the relevant data of a player for potential AFK replacement.
        /// </summary>
        /// <param name="replacementUserId">The user ID of the player who will be the replacement.</param>
        /// <param name="isScp079">Specifies if the player is SCP-079 (True if SCP-079, False if not).</param>
        private void SaveData(Player player, string replacementUserId, bool isScp079 = false)
        {
            AFKData data;

            if (isScp079)
            {
                if (player.RoleBase is Scp079Role scp079Role && scp079Role.SubroutineModule.TryGetSubroutine(out Scp079TierManager tierManager)
                    && scp079Role.SubroutineModule.TryGetSubroutine(out Scp079AuxManager energyManager))
                {
                    data = new AFKData()
                    {
                        NickName = player.Nickname,
                        Position = player.Position,
                        Role = player.Role,
                        Ammo = null,
                        Health = player.Health,
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
                var ammo = Extensions.GetAmmo(player);

                data = new AFKData()
                {
                    NickName = player.Nickname,
                    Position = player.Position,
                    Role = player.Role,
                    Ammo = ammo,
                    Health = player.Health,
                    Items = player.GetItems(),
                    SCP079 = new Scp079Data
                    {
                        Energy = 0f,
                        Experience = 0
                    }
                };
            }

            MainHandler.ReplacingPlayersData.Add(replacementUserId, data);
        }

        private void AddUse(string userID)
        {
            if (CommandUses.TryGetValue(userID, out var uses))
            {
                CommandUses[userID] = uses + 1;
            }
            else
            {
                CommandUses.Add(userID, 1);
            }
        }


        public static void ClearAllCachedData()
        {
            AllCoroutines.ForEach(c => Timing.KillCoroutines(c));
            AllCoroutines.Clear();
            InProcess.Clear();
            CommandUses.Clear();
        }
    }
}