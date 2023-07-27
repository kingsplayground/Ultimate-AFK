using CommandSystem;
using CustomPlayerEffects;
using MapGeneration;
using NWAPIPermissionSystem;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp079;
using PluginAPI.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UltimateAFK.Handlers;
using UltimateAFK.Resources;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace UltimateAFK.Command
{
    [CommandHandler(typeof(ClientCommandHandler))]
    public class AfkCommand : ICommand
    {
        public string Command { get; } = "afk";
        public string[] Aliases { get; }
        public string Description { get; } = "By using this command you will be moved to spectator and if the server allows it a player will replace you.";

        public Dictionary<string, float> InCooldown = new();
        private UltimateAFK Plugin = UltimateAFK.Singleton;

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

                if(ply == null)
                {
                    response = "Player is null";
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
                if(ply.TemporaryData.StoredData.ContainsKey("uafk_disable"))
                {
                    response = UltimateAFK.Singleton.Config.CommandConfig.Responses.OnUafkDisable;
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

                Replace(ply, ply.Role);
                InCooldown.Add(ply.UserId, Time.time + UltimateAFK.Singleton.Config.CommandConfig.Cooldown);
                response = UltimateAFK.Singleton.Config.CommandConfig.Responses.OnSuccess;
                return true;
            }
            catch (Exception e)
            {
                Log.Error($"Error on {GetType().Name}.{nameof(Execute)}: {e}");
                response = $"Error on {nameof(Execute)}: {e}";
                return false;
            }
        }


        private void Replace(Player player, RoleTypeId roleType)
        {
            // Check if role is blacklisted
            if (UltimateAFK.Singleton.Config.RoleTypeBlacklist?.Count > 0 && UltimateAFK.Singleton.Config.RoleTypeBlacklist.Contains(roleType))
            {
                Log.Debug($"player {player.Nickname} ({player.UserId}) has a role that is blacklisted so he will not be searched for a replacement player", UltimateAFK.Singleton.Config.DebugMode);

                player.ClearInventory();
                player.SetRole(RoleTypeId.Spectator);

                player.SendBroadcast(UltimateAFK.Singleton.Config.MsgFspec, 30, shouldClearPrevious: true);
                player.SendConsoleMessage(UltimateAFK.Singleton.Config.MsgFspec, "white");
                return;
            }
            
            // Get player replacement
            Player replacement = GetReplacement(player.UserId);
            
            // If no replacement player is found, I change the player's role to spectator
            if (replacement is null)
            {
                Log.Debug("Unable to find replacement player, moving to spectator...", UltimateAFK.Singleton.Config.DebugMode);
                //player.ClearInventory();
                player.SetRole(RoleTypeId.Spectator);
                player.SendBroadcast(UltimateAFK.Singleton.Config.MsgFspec, 30, shouldClearPrevious: true);
                player.SendConsoleMessage(UltimateAFK.Singleton.Config.MsgFspec, "white");
            }
            else
            {
                Log.Debug($"Replacement Player found: {replacement.Nickname} ({replacement.UserId})", UltimateAFK.Singleton.Config.DebugMode);
                Log.Debug($"Saving data of player {player.Nickname} in the dictionary.", UltimateAFK.Singleton.Config.DebugMode);

                SaveData(player, replacement.UserId, roleType is RoleTypeId.Scp079);
                Log.Debug($"Cleaning player {player.Nickname} inventory", UltimateAFK.Singleton.Config.DebugMode);
                // Clear player inventory
                player.ClearInventory();
                //Send player a broadcast for being too long afk
                player.SendBroadcast(UltimateAFK.Singleton.Config.MsgFspec, 25, shouldClearPrevious: true);
                player.SendConsoleMessage(UltimateAFK.Singleton.Config.MsgFspec, "white");

                Log.Debug($"Changing player {player.Nickname} to spectator", UltimateAFK.Singleton.Config.DebugMode);
                // Sends player to spectator
                player.SetRole(RoleTypeId.Spectator);
                // Sends replacement to the role that had the afk
                Log.Debug($"Changing replacement player  {replacement.Nickname} role to {roleType}", UltimateAFK.Singleton.Config.DebugMode);
                replacement.SetRole(roleType);
                player.SendConsoleMessage(string.Format(UltimateAFK.Singleton.Config.MsgReplaced, replacement.Nickname), "white");
            }
        }

        private Player GetReplacement(string afkUserId)
        {
            var players = new List<Player>();

            foreach (var player in Player.GetPlayers())
            {
                if (player is null || !player.IsReady || player.IsAlive || player.UserId == afkUserId || player.CheckPermission("uafk.ignore") || player.IsServer || player.UserId.Contains("@server")
                    || UltimateAFK.Singleton.Config.IgnoreOverwatch && player.IsOverwatchEnabled || player.TemporaryData.StoredData.ContainsKey("uafk_disable") || MainHandler.ReplacingPlayersData.TryGetValue(player.UserId, out _))
                    continue;

                players.Add(player);
            }

            return players.Any() ? players.ElementAtOrDefault(UnityEngine.Random.Range(0, players.Count)) : null;
        }

        private void SaveData(Player player, string replacementUserId, bool isScp079 = false)
        {
            if (isScp079)
            {
                if (player.RoleBase is Scp079Role scp079Role && scp079Role.SubroutineModule.TryGetSubroutine(out Scp079TierManager tierManager) && scp079Role.SubroutineModule.TryGetSubroutine(out Scp079AuxManager energyManager))
                {
                    var afkData = new AFKData()
                    {
                        NickName = player.Nickname,
                        Position = player.Position,
                        Role = player.Role,
                        Ammo = null,
                        Health = player.Health,
                        Items = null,
                        SCP079 = new Scp079Data
                        {
                            Role = scp079Role,
                            Energy = energyManager.CurrentAux,
                            Experience = tierManager.TotalExp,
                        }
                    };

                    MainHandler.ReplacingPlayersData.Add(replacementUserId, afkData);
                }

                return;
            }

            var ammo = Extensions.GetAmmo(player);

            var data = new AFKData()
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
            };

            MainHandler.ReplacingPlayersData.Add(replacementUserId, data);
        }
    }
}