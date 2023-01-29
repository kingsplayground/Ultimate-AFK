using System;
using System.Collections.Generic;
using CommandSystem;
using CustomPlayerEffects;
using NWAPIPermissionSystem;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp079;
using PluginAPI.Commands;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using UltimateAFK.Handlers;
using UltimateAFK.Resources;
using UnityEngine;

namespace UltimateAFK.Command
{
    [CommandHandler(typeof(ClientCommandHandler))]
    public class AfkCommand : ICommand
    {
        public string Command { get; } = "afk";
        public string[] Aliases { get; }
        public string Description { get; } = "By using this command you will be moved to spectator and if the server allows it a player will replace you."; 
        public Dictionary<string, float> InCooldown = new();
        
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            try
            {
                if (!UltimateAFK.Singleton.Config.CommandConfig.IsEnabled)
                {
                    response = UltimateAFK.Singleton.Config.CommandConfig.TextOnDisable;
                }
                var ply = Player.Get(sender);
                
                if (ply.EffectsManager.TryGetEffect<SeveredHands>(out _))
                {
                    response = UltimateAFK.Singleton.Config.CommandConfig.TextOnSevereHands;
                    return false;
                }
                if (ply.EffectsManager.TryGetEffect<CardiacArrest>(out _))
                {
                    response = UltimateAFK.Singleton.Config.CommandConfig.TextOnHearthAttack;
                    return false;
                }
                if (InCooldown.TryGetValue(ply.UserId, out var cooldown))
                {
                    // In cooldown
                    if (cooldown >= Time.time)
                    {
                        var cooldownTimer = (int)(cooldown - Time.time);

                        response = string.Format(UltimateAFK.Singleton.Config.CommandConfig.TextOnCooldown,
                            cooldownTimer);
                        return false;
                    }
                    else
                    {
                        // Not in cooldown
                        InCooldown.Remove(ply.UserId);
                    }
                }
                
                GoAfk(ply, ply.Role);
                InCooldown.Add(ply.UserId, Time.time + UltimateAFK.Singleton.Config.CommandConfig.Cooldown);
                response = UltimateAFK.Singleton.Config.CommandConfig.TextOnSuccess;
                return true;
            }
            catch (Exception e)
            {
                response = $"Error: {e.Data} -- {e}";
                return false;
            }
        }

        private void GoAfk(Player ply, RoleTypeId roleType)
        {
                // Check if role is blacklisted
                if (UltimateAFK.Singleton.Config.RoleTypeBlacklist?.Count > 0 && UltimateAFK.Singleton.Config.RoleTypeBlacklist.Contains(roleType))
                {
                    Log.Debug($"In the command | player {ply.Nickname} ({ply.UserId}) has a role that is blacklisted so he will not be searched for a replacement player", UltimateAFK.Singleton.Config.DebugMode);
                    
                    ply.ClearInventory();
                    ply.SetRole(RoleTypeId.Spectator);
                    
                    ply.SendBroadcast(UltimateAFK.Singleton.Config.MsgFspec, 30, shouldClearPrevious: true);
                    ply.SendConsoleMessage(UltimateAFK.Singleton.Config.MsgFspec, "white");
                    return;
                }
                
                // Get player replacement
                Player replacement = null;

                if (UltimateAFK.Singleton.Config.CommandConfig.Replace)
                    replacement = FindReplacement(ply);
                
                // If replacement is null
                if (replacement is null)
                {
                    Log.Debug("In the command | Unable to find replacement player, moving to spectator...", UltimateAFK.Singleton.Config.DebugMode);
                    
                    ply.ClearInventory();
                    ply.SetRole(RoleTypeId.Spectator);

                    ply.SendBroadcast(UltimateAFK.Singleton.Config.MsgFspec, 30, shouldClearPrevious: true);
                    ply.SendConsoleMessage(UltimateAFK.Singleton.Config.MsgFspec, "white");
                }
                else
                {
                    // if not
                    Log.Debug($"In the command | Replacement Player found: {replacement.Nickname} ({replacement.UserId})", UltimateAFK.Singleton.Config.DebugMode);

                    // Check if AFK role is SCP-079 
                    if (roleType is RoleTypeId.Scp079)
                    {
                        //Adds the replacement player to the dictionary with all the necessary information
                        AddData(ply, replacement, true);
                
                        // Self-explanatory
                        ply.SetRole(RoleTypeId.Spectator);
                        
                        //Send player a broadcast for being too long afk
                        ply.SendBroadcast(UltimateAFK.Singleton.Config.MsgFspec, 30, shouldClearPrevious: true);
                        ply.SendConsoleMessage(UltimateAFK.Singleton.Config.MsgFspec, "white");
                
                        // Sends replacement to the role that had the afk
                        replacement.SetRole(roleType);
                    }
                    else
                    {
                        // Adds the replacement player to the dictionary with all the necessary information
                        AddData(ply, replacement, false);
                        
                        // Clear player inventory
                        ply.ClearInventory();
                        //Send player a broadcast for being too long afk
                        ply.SendBroadcast(UltimateAFK.Singleton.Config.MsgFspec, 25, shouldClearPrevious: true);
                        ply.SendConsoleMessage(UltimateAFK.Singleton.Config.MsgFspec, "white");
                        // Sends player to spectator
                        ply.SetRole(RoleTypeId.Spectator);
                        // Sends replacement to the role that had the afk
                        replacement.SetRole(roleType);
                    }
                }
        }

        private Player FindReplacement(Player afk)
        {
            foreach (var player in Player.GetPlayers())
            {
                if (player.IsAlive || player == afk || player.CheckPermission("uafk.ignore") || player.IsServer || player.UserId.Contains("@server"))
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
    }
}