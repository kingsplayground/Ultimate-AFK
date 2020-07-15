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
        public AFKComponent(MainClass plugin)
        {
            this.plugin = plugin;
        }

        public bool disabled = false;

        Exiled.API.Features.Player rh;

        public Vector3 AFKLastPosition;
        public Vector3 AFKLastAngle;

        public int AFKTime = 0;
        public int AFKCount = 0;
        private float timer = 0.0f;
        // Do not change this delay. It will screw up the detection
        public float delay = 1.0f;

        void Awake()
        {
            rh = this.gameObject.GetComponent<Exiled.API.Features.Player>();
            Exiled.API.Features.Log.Info($"AWAKE AFK Component for {rh.Nickname}");
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
            Exiled.API.Features.Log.Info($"AFKChecker() for  {rh.Nickname}");
            // AFK Manager is a little fucky for computer, so let's not allow that. 
            // Also, let's not check dead people, because that's not nice. 
            if (this.isValidPlayerAfk(this.rh))
            {
                bool isScp079 = false;
                if (this.rh.Role == RoleType.Scp079)
                    isScp079 = true;

                Vector3 CurrentPos = this.rh.Position;
                Vector3 CurrentAngle;

                // For some reason, GetRotationVector does not return the proper angle, so we use the camera angle from 079
                if (isScp079)
                    CurrentAngle = this.rh.Camera.targetPosition.position;
                else
                    CurrentAngle = this.rh.Rotation;

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

                            this.rh.ClearBroadcasts();
                            this.rh.Broadcast(1, $"{plugin.Config.MsgPrefix} {warning}");
                        }
                        else
                        {
                            Log.Info($"{this.rh.Nickname} ({this.rh.UserId}) was detected as AFK!");
                            this.AFKTime = 0;

                            if (this.rh.Team != Team.RIP)
                            {
                                if (plugin.Config.TryReplace && !this.past_replace_time())
                                {
                                    // Credit: DCReplace :)

                                    Inventory.SyncListItemInfo items = this.rh.Inventory.items;
                                    RoleType role = this.rh.Role;
                                    Vector3 pos = this.rh.Position;
                                    float health = this.rh.Health;
                                    
                                    // New strange ammo system because the old one was fucked.
                                    Dictionary<Exiled.API.Enums.AmmoType, uint> ammo = new Dictionary<Exiled.API.Enums.AmmoType, uint>();
                                    foreach (Exiled.API.Enums.AmmoType atype in (Exiled.API.Enums.AmmoType[])Enum.GetValues(typeof(Exiled.API.Enums.AmmoType)))
                                    {
                                        ammo.Add(atype, this.rh.GetAmmo(atype));
                                    }
                                    
                                    // Stuff for 079
                                    byte Level079 = 0;
                                    float Exp079 = 0f, AP079 = 0f;
                                    if (isScp079)
                                    {
                                        Level079 = this.rh.Level;
                                        Exp079 = this.rh.Experience;
                                        AP079 = this.rh.Energy;
                                    }
                                    Exiled.API.Features.Player player = Player.List.FirstOrDefault(x => x.Role == RoleType.Spectator && x.UserId != string.Empty && !x.IsOverwatchEnabled && x != this.rh);
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
                                                    this.rh.SetAmmo(atype, amount);
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
                                            this.rh.Inventory.Clear();
                                            this.rh.SetRole(RoleType.Spectator);
                                            this.rh.Broadcast(30, $"{plugin.Config.MsgPrefix} {plugin.Config.MsgFspec}");
                                        });
                                    }
                                    else
                                    {
                                        // Couldn't find a valid player to spawn, just fspec anyways.
                                        this.fspec(this.rh);
                                    }
                                }
                                else
                                {
                                    // Replacing is disabled, just fspec
                                    this.fspec(this.rh);
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
            if (hub != null && hub.UserId != null)
                if (hub.Team != Team.RIP)
                    return true;
            return false;
        }
    }
}
