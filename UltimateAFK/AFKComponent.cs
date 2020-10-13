using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using MEC;
using Exiled.API.Features;
using PlayableScps;
using scp035.API;

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
                if (!this.disabled)
                    AFKChecker();
            }
        }

        // Called every 1 second according to the player's Update function. This is way more efficient than the old way of doing a forloop for every player.
        // Also, since the gameObject for the player is deleted when they disconnect, we don't need to worry about cleaning any variables :) 
        private void AFKChecker()
        {
            //Log.Info($"AFK Time: {this.AFKTime} AFK Count: {this.AFKCount}");
            if (this.ply.Team == Team.RIP) return;

            bool isScp079 = (this.ply.Role == RoleType.Scp079) ? true : false;
            bool scp096TryNotToCry = false;

            // When SCP096 is in the state "TryNotToCry" he cannot move or it will cancel,
            // therefore, we don't want to AFK check 096 while he's in this state.
            if (this.ply.Role == RoleType.Scp096)
            {
                Scp096 scp096 = this.ply.ReferenceHub.scpsController.CurrentScp as Scp096;
                scp096TryNotToCry = (scp096.PlayerState == Scp096PlayerState.TryNotToCry) ? true : false;
            }

            Vector3 CurrentPos = this.ply.Position;
            Vector3 CurrentAngle = (isScp079) ? this.ply.Camera.targetPosition.position : this.ply.Rotation;

            if (CurrentPos != this.AFKLastPosition || CurrentAngle != this.AFKLastAngle || scp096TryNotToCry)
            {
                this.AFKLastPosition = CurrentPos;
                this.AFKLastAngle = CurrentAngle;
                this.AFKTime = 0;
                return;
            }
            
            // The player hasn't moved past this point.
            this.AFKTime++;

            // If the player hasn't reached the time yet don't continue.
            if (this.AFKTime < plugin.Config.AfkTime) return;

            // Check if we're still in the "grace" period
            int secondsuntilspec = (plugin.Config.AfkTime + plugin.Config.GraceTime) - this.AFKTime;
            if (secondsuntilspec > 0)
            {
                string warning = plugin.Config.MsgGrace;
                warning = warning.Replace("%timeleft%", secondsuntilspec.ToString());

                this.ply.ClearBroadcasts();
                this.ply.Broadcast(1, $"{plugin.Config.MsgPrefix} {warning}");
                return;
            }
            
            // The player is AFK and action will be taken.
            Log.Info($"{this.ply.Nickname} ({this.ply.UserId}) was detected as AFK!");
            this.AFKTime = 0;

            // Let's make sure they are still alive before doing any replacement.
            if (this.ply.Team == Team.RIP) return; 

            if (plugin.Config.TryReplace && !this.IsPastReplaceTime())
            {
                var role = Exiled.Loader.Plugins.FirstOrDefault(pl => pl.Name == "EasyEvents")?.Assembly.GetType("EasyEvents.Util")?.GetMethod("GetRole")?.Invoke(null, new object[] {this.ply});

                // SCP035 Support (Credit DCReplace)
                bool is035 = false;
                try
                {
                    is035 = this.ply.Id == TryGet035()?.Id;
                }
                catch (Exception e)
                {
                    Log.Debug($"SCP-035 is not installed, skipping method call: {e}");
                }

                // Credit: DCReplace :)
                // I mean at this point 90% of this has been rewritten lol...
                Inventory.SyncListItemInfo items = this.ply.Inventory.items;

                RoleType role = this.ply.Role;
                Vector3 pos = this.ply.Position;
                float health = this.ply.Health;
                
                // New strange ammo system because the old one was fucked.
                Dictionary<Exiled.API.Enums.AmmoType, uint> ammo = new Dictionary<Exiled.API.Enums.AmmoType, uint>();
                foreach (Exiled.API.Enums.AmmoType atype in (Exiled.API.Enums.AmmoType[])Enum.GetValues(typeof(Exiled.API.Enums.AmmoType)))
                {
                    ammo.Add(atype, this.ply.GetAmmo(atype));
                    this.ply.SetAmmo(atype, 0); // We remove the ammo so the player doesn't drop it (duplicate ammo)
                }
                
                // Stuff for 079
                byte Level079 = 0;
                float Exp079 = 0f, AP079 = 0f;
                if (isScp079)
                {
                    Level079 = this.ply.Level;
                    Exp079 = this.ply.Experience;
                    AP079 = this.ply.Energy;
                }

                Player player = Player.List.FirstOrDefault(x => x.Role == RoleType.Spectator && x.UserId != string.Empty && !x.IsOverwatchEnabled && x != this.ply);
                if (player != null)
                {
                    player.SetRole(role);
                    Timing.CallDelayed(0.3f, () =>
                    {
                        if (is035)
                        {
                            try
                            {
                                TrySpawn035(player);
                            }
                            catch (Exception e)
                            {
                                Log.Debug($"SCP-035 is not installed, skipping method call: {e}");
                            }
                        }
                        player.Position = pos;
                        player.Inventory.Clear();

                        foreach (Inventory.SyncItemInfo item in items)
                        {
                            player.Inventory.AddNewItem(item.id, item.durability, item.modSight, item.modBarrel, item.modOther);
                        }

                        player.Health = health;

                        foreach (Exiled.API.Enums.AmmoType atype in (Exiled.API.Enums.AmmoType[])Enum.GetValues(typeof(Exiled.API.Enums.AmmoType)))
                        {
                            uint amount;
                            if (ammo.TryGetValue(atype, out amount))
                            {
                                player.SetAmmo(atype, amount);
                            }
                            else
                                Log.Error($"[uAFK] ERROR: Tried to get a value from dict that did not exist! (Ammo)");
                        }

                        if (isScp079)
                        {
                            player.Level = Level079;
                            player.Experience = Exp079;
                            player.Energy = AP079;
                        }

                        player.Broadcast(10, $"{plugin.Config.MsgPrefix} {plugin.Config.MsgReplace}");
                        if(role != null) Exiled.Loader.Plugins.FirstOrDefault(pl => pl.Name == "EasyEvents")?.Assembly.GetType("EasyEvents.CustomRoles")?.GetMethod("ChangeRole")?.Invoke(null, new object[] {player, role});
                        
                        this.ply.Inventory.Clear(); // Clear their items to prevent dupes.
                        this.ply.SetRole(RoleType.Spectator);
                        this.ply.Broadcast(30, $"{plugin.Config.MsgPrefix} {plugin.Config.MsgFspec}");
                    });
                }
                else
                {
                    // Couldn't find a valid player to spawn, just ForceToSpec anyways.
                    this.ForceToSpec(this.ply);
                }
            }
            else
            {
                // Replacing is disabled, just ForceToSpec
                this.ForceToSpec(this.ply);
            }
            // If it's -1 we won't be kicking at all.
            if (plugin.Config.NumBeforeKick != -1)
            {
                // Increment AFK Count
                this.AFKCount++;
                if (this.AFKCount >= plugin.Config.NumBeforeKick)
                {
                    // Since AFKCount is greater than the config we're going to kick that player for being AFK too many times in one match.
                    ServerConsole.Disconnect(this.gameObject, plugin.Config.MsgKick);
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
