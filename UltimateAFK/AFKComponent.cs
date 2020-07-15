using UnityEngine;
using Exiled.API.Features;
using Exiled.Events.EventArgs;
using Exiled.Permissions.Extensions;
using System.Linq;
using MEC;
using System;
using System.Collections.Generic;

namespace UltimateAFK
{
    public class AFKComponent : MonoBehaviour
    {
        public MainClass plugin;

        public bool disabled = false;

        Exiled.API.Features.Player ply;

        public Vector3 AFKLastPosition;
        public Vector3 AFKLastAngle;

        public int AFKTime = 0;
        public int AFKCount = 0;
        private float timer = 0.0f;
        // Do not change this delay. It will screw up the detection
        public float delay = 1.0f;

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
            // AFK Manager is a little fucky for computer, so let's not allow that. 
            // Also, let's not check dead people, because that's not nice. 

            if (this.ply.Team != Team.RIP)
            {
                bool isScp079 = false;
                if (this.ply.Role == RoleType.Scp079)
                    isScp079 = true;

                Vector3 CurrentPos = this.ply.Position;
                Vector3 CurrentAngle;

                // For some reason, GetRotationVector does not return the proper angle, so we use the camera angle from 079
                if (isScp079)
                    CurrentAngle = this.ply.Camera.targetPosition.position;
                else
                    CurrentAngle = this.ply.Rotations;

                if (CurrentPos == this.AFKLastPosition && CurrentAngle == this.AFKLastAngle)
                {
                    this.AFKTime++;
                    if (this.AFKTime > plugin.Config.AfkTime)
                    {
                        int secondsuntilspec = (plugin.Config.AfkTime + plugin.Config.GraceTime) - this.AFKTime;
                        if (secondsuntilspec > 0)
                        {
                            string warning = plugin.Config.MsgGrace;
                            warning = warning.Replace("%timeleft%", secondsuntilspec.ToString());

                            this.ply.ClearBroadcasts();
                            this.ply.Broadcast(1, $"{plugin.Config.MsgPrefix} {warning}");
                        }
                        else
                        {
                            Log.Info($"{this.ply.Nickname} ({this.ply.UserId}) was detected as AFK!");
                            this.AFKTime = 0;

                            if (this.ply.Team != Team.RIP)
                            {
                                if (plugin.Config.TryReplace && !this.past_replace_time())
                                {
                                    // Credit: DCReplace :)

                                    Inventory.SyncListItemInfo items = this.ply.Inventory.items;
                                    RoleType role = this.ply.Role;
                                    Vector3 pos = this.ply.Position;
                                    float health = this.ply.Health;
                                    
                                    // New strange ammo system because the old one was fucked.
                                    Dictionary<Exiled.API.Enums.AmmoType, uint> ammo = new Dictionary<Exiled.API.Enums.AmmoType, uint>();
                                    foreach (Exiled.API.Enums.AmmoType atype in (Exiled.API.Enums.AmmoType[])Enum.GetValues(typeof(Exiled.API.Enums.AmmoType)))
                                    {
                                        ammo.Add(atype, this.ply.GetAmmo(atype));
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
                                    Exiled.API.Features.Player player = Player.List.FirstOrDefault(x => x.Role == RoleType.Spectator && x.UserId != string.Empty && !x.IsOverwatchEnabled && x != this.ply);
                                    if (player != null)
                                    {
                                        player.SetRole(role);
                                        Timing.CallDelayed(0.3f, () =>
                                        {
                                            player.Position = pos;
                                            player.Inventory.Clear();
                                            foreach (var item in items) player.Inventory.AddNewItem(item.id);
                                            player.Health = health;

                                            foreach (Exiled.API.Enums.AmmoType atype in (Exiled.API.Enums.AmmoType[])Enum.GetValues(typeof(Exiled.API.Enums.AmmoType)))
                                            {
                                                uint amount;
                                                if (ammo.TryGetValue(atype, out amount))
                                                {
                                                    this.ply.SetAmmo(atype, amount);
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
                                            // Clear their items because we are giving said items to the player already.
                                            this.ply.Inventory.Clear();
                                            this.ply.SetRole(RoleType.Spectator);
                                            this.ply.Broadcast(30, $"{plugin.Config.MsgPrefix} {plugin.Config.MsgFspec}");
                                        });
                                    }
                                    else
                                    {
                                        // Couldn't find a valid player to spawn, just fspec anyways.
                                        this.fspec(this.ply);
                                    }
                                }
                                else
                                {
                                    // Replacing is disabled, just fspec
                                    this.fspec(this.ply);
                                }
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
                    }
                }
                else
                {
                    this.AFKLastPosition = CurrentPos;
                    this.AFKLastAngle = CurrentAngle;
                    this.AFKTime = 0;
                }
            }
        }

        private void fspec(Exiled.API.Features.Player hub)
        {
            hub.SetRole(RoleType.Spectator);
            hub.Broadcast(30, $"{plugin.Config.MsgPrefix} {plugin.Config.MsgFspec}");
        }

        private bool past_replace_time()
        {
            if (plugin.Config.MaxReplaceTime != -1)
                if (Exiled.API.Features.Round.ElapsedTime.TotalSeconds > plugin.Config.MaxReplaceTime)
                {
                    Exiled.API.Features.Log.Info("Past allowed replace time, will not look for replacement player.");
                    return true;
                }
            return false;
        }

        // Try to prevent errors from null users.
        private bool isValidPlayerAfk(Exiled.API.Features.Player hub)
        {
            Exiled.API.Features.Log.Info($"isValidPlayerAfk for {hub.Nickname}");
            if (hub != null && hub.UserId != null)
                if (hub.Team != Team.RIP)
                    return true;
            return false;
        }
    }
}
