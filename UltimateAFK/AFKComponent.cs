using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using MEC;
using Exiled.API.Features;
using Exiled.Loader;
using PlayableScps;
using scp035.API;
using System.Reflection;

namespace UltimateAFK
{
    public class AFKComponent : MonoBehaviour
    {
        public MainClass plugin;

        public bool disabled = false;

        Player ply;

        public Vector3 AFKLastPosition;
        public Vector3 AFKLastAngle;

        public int AFKTime = 0;
        public int AFKCount = 0;
        private float timer = 0.0f;

        // Do not change this delay. It will screw up the detection
        public float delay = 1.0f;

        private Player TryGet035() => Scp035Data.GetScp035();
        private void TrySpawn035(Player player) => Scp035Data.Spawn035(player);

        // Expose replacing player for plugin support
        public Player PlayerToReplace;


        void Awake()
        {
            ply = Player.Get(gameObject);
        }

        void Update()
        {
            timer += Time.deltaTime;
            if (timer > delay)
            {
                timer = 0f;
                if (!disabled)
                {
                    try
                    {
                        AFKChecker();
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                }
            }
        }

        // Called every 1 second according to the player's Update function. This is way more efficient than the old way of doing a forloop for every player.
        // Also, since the gameObject for the player is deleted when they disconnect, we don't need to worry about cleaning any variables :) 
        private void AFKChecker()
        {
            //Log.Info($"AFK Time: {AFKTime} AFK Count: {AFKCount}");
            if (ply.Team == Team.RIP) return;

            bool isScp079 = (ply.Role == RoleType.Scp079) ? true : false;
            bool scp096TryNotToCry = false;

            // When SCP096 is in the state "TryNotToCry" he cannot move or it will cancel,
            // therefore, we don't want to AFK check 096 while he's in this state.
            if (ply.Role == RoleType.Scp096)
            {
                PlayableScps.Scp096 scp096 = ply.ReferenceHub.scpsController.CurrentScp as PlayableScps.Scp096;
                scp096TryNotToCry = (scp096.PlayerState == Scp096PlayerState.TryNotToCry) ? true : false;
            }

            Vector3 CurrentPos = ply.Position;
            Vector3 CurrentAngle = (isScp079) ? ply.Camera.targetPosition.position : ply.Rotation;

            if (CurrentPos != AFKLastPosition || CurrentAngle != AFKLastAngle || scp096TryNotToCry)
            {
                AFKLastPosition = CurrentPos;
                AFKLastAngle = CurrentAngle;
                AFKTime = 0;
                PlayerToReplace = null;
                return;
            }

            // The player hasn't moved past this point.
            AFKTime++;

            // If the player hasn't reached the time yet don't continue.
            if (AFKTime < plugin.Config.AfkTime) return;

            // Check if we're still in the "grace" period
            int secondsuntilspec = (plugin.Config.AfkTime + plugin.Config.GraceTime) - AFKTime;
            if (secondsuntilspec > 0)
            {
                string warning = plugin.Config.MsgGrace;
                warning = warning.Replace("%timeleft%", secondsuntilspec.ToString());

                ply.ClearBroadcasts();
                ply.Broadcast(1, $"{plugin.Config.MsgPrefix} {warning}");
                return;
            }

            // The player is AFK and action will be taken.
            Log.Info($"{ply.Nickname} ({ply.UserId}) was detected as AFK!");
            AFKTime = 0;

            // Let's make sure they are still alive before doing any replacement.
            if (ply.Team == Team.RIP) return;

            if (plugin.Config.TryReplace && !IsPastReplaceTime())
            {
                Assembly easyEvents = Loader.Plugins.FirstOrDefault(pl => pl.Name == "EasyEvents")?.Assembly;

                var roleEasyEvents = easyEvents?.GetType("EasyEvents.Util")?.GetMethod("GetRole")?.Invoke(null, new object[] { ply });

				// SCP035 Support (Credit DCReplace)
				bool is035 = false;
                try
                {
                    is035 = ply.Id == TryGet035()?.Id;
                }
                catch (Exception e)
                {
                    Log.Debug($"SCP-035 is not installed, skipping method call: {e}");
                }

                // Credit: DCReplace :)
                // I mean at this point 90% of this has been rewritten lol...
                Inventory.SyncListItemInfo items = ply.Inventory.items;

                RoleType role = ply.Role;
                Vector3 pos = ply.Position;
                float health = ply.Health;

                // New strange ammo system because the old one was fucked.
                Dictionary<Exiled.API.Enums.AmmoType, uint> ammo = new Dictionary<Exiled.API.Enums.AmmoType, uint>();
                foreach (Exiled.API.Enums.AmmoType atype in (Exiled.API.Enums.AmmoType[])Enum.GetValues(typeof(Exiled.API.Enums.AmmoType)))
                {
                    ammo.Add(atype, ply.Ammo[(int)atype]);
                    ply.Ammo[(int)atype] = 0; // We remove the ammo so the player doesn't drop it (duplicate ammo)
                }

                // Stuff for 079
                byte Level079 = 0;
                float Exp079 = 0f, AP079 = 0f;
                if (isScp079)
                {
                    Level079 = ply.Level;
                    Exp079 = ply.Experience;
                    AP079 = ply.Energy;
                }

                PlayerToReplace = Player.List.FirstOrDefault(x => x.Role == RoleType.Spectator && x.UserId != string.Empty && !x.IsOverwatchEnabled && x != ply);
                if (PlayerToReplace != null)
                {
                    // Make the player a spectator first so other plugins can do things on player changing role with uAFK.
                    ply.Inventory.Clear(); // Clear their items to prevent dupes.
                    ply.SetRole(RoleType.Spectator);
                    ply.Broadcast(30, $"{plugin.Config.MsgPrefix} {plugin.Config.MsgFspec}");

                    PlayerToReplace.SetRole(role);

                    Timing.CallDelayed(0.3f, () =>
                    {
                        if (is035)
                        {
                            try
                            {
                                TrySpawn035(PlayerToReplace);
                            }
                            catch (Exception e)
                            {
                                Log.Debug($"SCP-035 is not installed, skipping method call: {e}");
                            }
                        }
                        PlayerToReplace.Position = pos;
                        PlayerToReplace.Inventory.Clear();

                        foreach (Inventory.SyncItemInfo item in items)
                        {
                            PlayerToReplace.Inventory.AddNewItem(item.id, item.durability, item.modSight, item.modBarrel, item.modOther);
                        }

                        PlayerToReplace.Health = health;

                        foreach (Exiled.API.Enums.AmmoType atype in (Exiled.API.Enums.AmmoType[])Enum.GetValues(typeof(Exiled.API.Enums.AmmoType)))
                        {
                            uint amount;
                            if (ammo.TryGetValue(atype, out amount))
                            {
                                PlayerToReplace.Ammo[(int)atype] = amount;
                            }
                            else
                                Log.Error($"[uAFK] ERROR: Tried to get a value from dict that did not exist! (Ammo)");
                        }

                        if (isScp079)
                        {
                            PlayerToReplace.Level = Level079;
                            PlayerToReplace.Experience = Exp079;
                            PlayerToReplace.Energy = AP079;
                        }

                        PlayerToReplace.Broadcast(10, $"{plugin.Config.MsgPrefix} {plugin.Config.MsgReplace}");
						if (roleEasyEvents != null) easyEvents?.GetType("EasyEvents.CustomRoles")?.GetMethod("ChangeRole")?.Invoke(null, new object[] { PlayerToReplace, roleEasyEvents });
                        PlayerToReplace = null;
                    });
                }
                else
                {
                    // Couldn't find a valid player to spawn, just ForceToSpec anyways.
                    ForceToSpec(ply);
                }
            }
            else
            {
                // Replacing is disabled, just ForceToSpec
                ForceToSpec(ply);
            }
            // If it's -1 we won't be kicking at all.
            if (plugin.Config.NumBeforeKick != -1)
            {
                // Increment AFK Count
                AFKCount++;
                if (AFKCount >= plugin.Config.NumBeforeKick)
                {
                    // Since AFKCount is greater than the config we're going to kick that player for being AFK too many times in one match.
                    ServerConsole.Disconnect(gameObject, plugin.Config.MsgKick);
                }
            }
        }

        private void ForceToSpec(Player hub)
        {
            hub.SetRole(RoleType.Spectator);
            hub.Broadcast(30, $"{plugin.Config.MsgPrefix} {plugin.Config.MsgFspec}");
        }

        private bool IsPastReplaceTime()
        {
            if (plugin.Config.MaxReplaceTime != -1)
            {
                if (Round.ElapsedTime.TotalSeconds > plugin.Config.MaxReplaceTime)
                {
                    Log.Info("Since we are past the allowed replace time, we will not look for replacement player.");
                    return true;
                }
            }
            return false;
        }
    }
}
