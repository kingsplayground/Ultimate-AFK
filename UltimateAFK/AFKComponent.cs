using UnityEngine;
using EXILED.Extensions;
using Log = EXILED.Log;
using System.Linq;
using MEC;

namespace UltimateAFK
{
    public class AFKComponent : MonoBehaviour
    {
        ReferenceHub rh;

        public Vector3 AFKLastPosition;
        public Vector3 AFKLastAngle;

        public int AFKTime = 0;
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
                AFKChecker();
            }
        }

        // Called every 1 second according to the player's Update function. This is way more efficient than the old way of doing a forloop for every player.
        // Also, since the gameObject for the player is deleted when they disconnect, we don't need to worry about cleaning any bariables :) 
        private void AFKChecker()
        {
            if (this.gameObject != null)
            {
                if (this.rh != null)
                {
                    // AFK Manager is a little fucky for computer, so let's not allow that. 
                    // Also, let's not check dead people, because that's not nice. 
                    if (this.rh.GetTeam() != Team.RIP && this.rh.characterClassManager.CurClass != RoleType.Scp079)
                    {
                        Vector3 CurrentPos = this.rh.GetPosition();
                        Vector3 CurrentAngle = this.rh.GetRotationVector();

                        if (CurrentPos == this.AFKLastPosition && CurrentAngle == this.AFKLastAngle)
                        {
                            this.AFKTime++;
                            if (this.AFKTime > Config.afk_time)
                            {
                                int secondsuntilspec = (Config.afk_time + Config.grace_time) - this.AFKTime;
                                if (secondsuntilspec > 0)
                                {
                                    string warning = Config.grace_message;
                                    if (Config.kick)
                                        warning = warning.Replace("%action%", "kicked");
                                    else
                                        warning = warning.Replace("%action%", "moved to spec");
                                    warning = warning.Replace("%timeleft%", secondsuntilspec.ToString());

                                    this.rh.ClearBroadcasts();
                                    this.rh.Broadcast(1, $"{Config.msg_prefix} {warning}", false);
                                }
                                else
                                {
                                    Log.Info($"{this.rh.nicknameSync.MyNick} ({this.rh.GetUserId()}) was detected as AFK!");
                                    this.AFKTime = 0;
                                    if (Config.kick)
                                        ServerConsole.Disconnect(this.gameObject, Config.kick_message);
                                    else if (Config.replace)
                                        TryReplacePlayer(this.rh);
                                    else
                                    {
                                        this.rh.SetRole(RoleType.Spectator);
                                        this.rh.Broadcast(30, $"{Config.msg_prefix} {Config.fspec_message}", false);
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
            }
        }
        
        // Credit: https://github.com/Cyanox62/DCReplace
        public static void TryReplacePlayer(ReferenceHub toreplace)
        {
            if (toreplace.GetTeam() != Team.RIP)
            {
                Inventory.SyncListItemInfo items = toreplace.inventory.items;
                RoleType role = toreplace.GetRole();
                Vector3 pos = toreplace.transform.position;
                int health = (int)toreplace.playerStats.health;
                string ammo = toreplace.ammoBox.amount;

                ReferenceHub player = Player.GetHubs().FirstOrDefault(x => x.GetRole() == RoleType.Spectator && x.characterClassManager.UserId != string.Empty && !x.GetOverwatch() && x != toreplace);
                if (player != null)
                {
                    player.SetRole(role);
                    Timing.CallDelayed(0.3f, () =>
                    {
                        player.SetPosition(pos);
                        player.inventory.items.ToList().Clear();
                        foreach (var item in items) player.inventory.AddNewItem(item.id);
                        player.playerStats.health = health;
                        player.ammoBox.Networkamount = ammo;

                        player.Broadcast(10, $"{Config.msg_prefix} {Config.replace_message}", false);
                        // Clear their items because we are giving said items to the player already.
                        toreplace.inventory.Clear();
                        toreplace.characterClassManager.SetClassID(RoleType.Spectator);
                        toreplace.Broadcast(30, $"{Config.msg_prefix} {Config.fspec_message}", false);
                    });
                }
                else
                {
                    toreplace.characterClassManager.SetClassID(RoleType.Spectator);
                    toreplace.Broadcast(30, $"{Config.msg_prefix} {Config.fspec_message}", false);
                }
            }
        }
    }
}
