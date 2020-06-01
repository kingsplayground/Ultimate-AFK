using UnityEngine;
using EXILED;
using EXILED.Extensions;
using Log = EXILED.Log;
using System.Linq;
using MEC;
using System;
using System.Collections.Generic;

namespace UltimateAFK
{
    public class AFKComponent : MonoBehaviour
    {
        public bool disabled = false;

        ReferenceHub rh;

        public Vector3 AFKLastPosition;
        public Vector3 AFKLastAngle;

        public int AFKTime = 0;
        public int AFKCount = 0;
        private float timer = 0.0f;
        // Do not change this delay. It will screw up the detection
        public float delay = 1.0f;

        void Awake()
        {
            rh = this.gameObject.GetComponent<ReferenceHub>();
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
            if (this.isValidPlayerAfk(this.rh))
            {
                bool isScp079 = false;
                if (this.rh.GetRole() == RoleType.Scp079)
                    isScp079 = true;

                Vector3 CurrentPos = this.rh.GetPosition();
                Vector3 CurrentAngle;

                // For some reason, GetRotationVector does not return the proper angle, so we use the camera angle from 079
                if (isScp079)
                {
                    Camera079 cam = Scp079.GetCamera(this.rh);
                    CurrentAngle = cam.targetPosition.position;
                }
                else
                    CurrentAngle = this.rh.GetRotations(); // This is a hack because GetRotationVector is fucked.


                if (CurrentPos == this.AFKLastPosition && CurrentAngle == this.AFKLastAngle)
                {
                    this.AFKTime++;
                    if (this.AFKTime > Config.afk_time)
                    {
                        int secondsuntilspec = (Config.afk_time + Config.grace_time) - this.AFKTime;
                        if (secondsuntilspec > 0)
                        {
                            string warning = Config.grace_message;
                            warning = warning.Replace("%timeleft%", secondsuntilspec.ToString());

                            this.rh.ClearBroadcasts();
                            this.rh.Broadcast(1, $"{Config.msg_prefix} {warning}", false);
                        }
                        else
                        {
                            Log.Info($"{this.rh.nicknameSync.MyNick} ({this.rh.GetUserId()}) was detected as AFK!");
                            this.AFKTime = 0;

                            if (this.rh.GetTeam() != Team.RIP)
                            {
                                if (Config.replace && !this.past_replace_time())
                                {
                                    // Credit: DCReplace :)

                                    Inventory.SyncListItemInfo items = this.rh.inventory.items;
                                    RoleType role = this.rh.GetRole();
                                    Vector3 pos = this.rh.transform.position;
                                    float health = this.rh.GetHealth();
                                    
                                    // New strange ammo system because the old one was fucked.
                                    Dictionary<EXILED.ApiObjects.AmmoType, uint> ammo = new Dictionary<EXILED.ApiObjects.AmmoType, uint>();
                                    foreach (EXILED.ApiObjects.AmmoType atype in (EXILED.ApiObjects.AmmoType[])Enum.GetValues(typeof(EXILED.ApiObjects.AmmoType)))
                                    {
                                        ammo.Add(atype, this.rh.GetAmmo(atype));
                                    }
                                    
                                    // Stuff for 079
                                    int Level079 = 0;
                                    float Exp079 = 0f, AP079 = 0f;
                                    if (isScp079)
                                    {
                                        Level079 = Scp079.GetLevel(this.rh);
                                        Exp079 = Scp079.GetExperience(this.rh);
                                        AP079 = Scp079.GetEnergy(this.rh);
                                    }

                                    ReferenceHub player = Player.GetHubs().FirstOrDefault(x => x.GetRole() == RoleType.Spectator && x.characterClassManager.UserId != string.Empty && !x.GetOverwatch() && x != this.rh);
                                    if (player != null)
                                    {
                                        player.SetRole(role);
                                        Timing.CallDelayed(0.3f, () =>
                                        {
                                            player.SetPosition(pos);
                                            player.inventory.Clear();
                                            foreach (var item in items) player.inventory.AddNewItem(item.id);
                                            player.SetHealth(health);

                                            foreach (EXILED.ApiObjects.AmmoType atype in (EXILED.ApiObjects.AmmoType[])Enum.GetValues(typeof(EXILED.ApiObjects.AmmoType)))
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
                                                Scp079.SetLevel(player, Level079, false);
                                                Scp079.SetExperience(player, Exp079);
                                                Scp079.SetEnergy(player, AP079);
                                            }
                                            player.Broadcast(10, $"{Config.msg_prefix} {Config.replace_message}", false);
                                            // Clear their items because we are giving said items to the player already.
                                            this.rh.inventory.Clear();
                                            this.rh.characterClassManager.SetClassID(RoleType.Spectator);
                                            this.rh.Broadcast(30, $"{Config.msg_prefix} {Config.fspec_message}", false);
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
                            if (Config.num_before_kick != -1)
                            {
                                // Increment AFK Count
                                this.AFKCount++;
                                if (this.AFKCount >= Config.num_before_kick)
                                {
                                    // Since AFKCount is greater than the config we're going to kick that player for being AFK too many times in one match.
                                    ServerConsole.Disconnect(this.gameObject, Config.kick_message);
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

        private void fspec(ReferenceHub hub)
        {
            hub.characterClassManager.SetClassID(RoleType.Spectator);
            hub.Broadcast(30, $"{Config.msg_prefix} {Config.fspec_message}", false);
        }

        private bool past_replace_time()
        {
            if (Config.max_time_for_replace != -1)
                if (EventPlugin.GetRoundDuration() > Config.max_time_for_replace)
                {
                    Log.Info("Past allowed replace time, will not look for replacement player.");
                    return true;
                }
            return false;
        }

        // Try to prevent errors from null users.
        private bool isValidPlayerAfk(ReferenceHub hub)
        {
            if (hub != null && hub.GetUserId() != null)
                if (hub.GetTeam() != Team.RIP)
                    return true;
            return false;
        }
    }
}
