using System;
using System.Collections.Generic;
using System.Linq;
using CommandSystem;
using CustomPlayerEffects;
using MapGeneration;
using NWAPIPermissionSystem;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp079;
using PluginAPI.Commands;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using UltimateAFK.Handlers;
using UltimateAFK.Resources;
using UnityEngine;
using Utils.NonAllocLINQ;
using Random = UnityEngine.Random;

namespace UltimateAFK.Command
{
    [CommandHandler(typeof(ClientCommandHandler))]
    public class AfkCommand : ICommand
    {
        public string Command { get; } = "afk";
        public string[] Aliases { get; }
        public string Description { get; } = "By using this command you will be moved to spectator and if the server allows it a player will replace you.";

        public Dictionary<string, float> InCooldown = new();
        private readonly UltimateAFK Plugin = UltimateAFK.Singleton;

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
                if (!ply.IsAlive)
                {
                    response = UltimateAFK.Singleton.Config.CommandConfig.Responses.OnPlayerIsDead;
                    return false;
                }
                if (ply.Zone == FacilityZone.Other || ply.EffectsManager.GetEffect<Corroding>().IsEnabled)
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
                    !UltimateAFK.Singleton.Config.CommandConfig.UserGroupsAllowed.Contains(ply.RoleName))
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
                Log.Error($"Error on {nameof(AfkCommand)}: {e}");
                response = $"Error: {e}";
                return false;
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

                player.SendBroadcast(Plugin.Config.MsgFspec, 30, shouldClearPrevious: true);
                player.SendConsoleMessage(Plugin.Config.MsgFspec, "white");
                return;
            }

            // Get player replacement
            Player replacement = FindReplacement(player.UserId);

            // If no replacement player is found, I change the player's role to spectator
            if (replacement == null)
            {
                Log.Debug("Unable to find replacement player, moving to spectator...", UltimateAFK.Singleton.Config.DebugMode);

                player.ClearInventory();
                player.SetRole(RoleTypeId.Spectator);

                player.SendBroadcast(Plugin.Config.MsgFspec, 30, shouldClearPrevious: true);
                player.SendConsoleMessage(Plugin.Config.MsgFspec, "white");
            }
            else
            {
                Log.Debug($"Replacement Player found: {replacement.Nickname} ({replacement.UserId})", Plugin.Config.DebugMode);
                Log.Debug($"Saving data of player {player.Nickname} in the dictionary.", Plugin.Config.DebugMode);

                SaveData(player, replacement.UserId, roleType == RoleTypeId.Scp079);

                Log.Debug($"Cleaning player {player.Nickname} inventory", Plugin.Config.DebugMode);
                // Clear player inventory
                player.ClearInventory();
                //Send player a broadcast for being too long afk
                player.SendBroadcast(Plugin.Config.MsgFspec, 25, shouldClearPrevious: true);
                player.SendConsoleMessage(Plugin.Config.MsgFspec, "white");

                Log.Debug($"Changing player {player.Nickname} to spectator", Plugin.Config.DebugMode);
                // Sends player to spectator
                player.SetRole(RoleTypeId.Spectator);
                // Sends replacement to the role that had the afk
                Log.Debug($"Changing replacement player  {replacement.Nickname} role to {roleType}", Plugin.Config.DebugMode);
                replacement.SetRole(roleType);

            }
        }

        private Player FindReplacement(string afkUserId)
        {
            var players = new List<Player>();
            foreach (var player in Player.GetPlayers())
            {
                if (player.IsAlive || player.UserId == afkUserId || player.CheckPermission("uafk.ignore") || player.IsServer || player.UserId.Contains("@server")
                    || UltimateAFK.Singleton.Config.IgnoreOverwatch && player.IsOverwatchEnabled || MainHandler.ReplacingPlayersData.TryGetValue(player.UserId, out _))
                    continue;
                
                players.Add(player);
            }
            
            return players.Any() ? players.ElementAtOrDefault(Random.Range(0, players.Count)) : null;
        }

        private void SaveData(Player player, string replacementUserId, bool isScp079 = false)
        {
            if (isScp079)
            {
                if (player.RoleBase is Scp079Role scp079Role && scp079Role.SubroutineModule.TryGetSubroutine(out Scp079TierManager tierManager)
                       && scp079Role.SubroutineModule.TryGetSubroutine(out Scp079AuxManager energyManager))
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
    }
}