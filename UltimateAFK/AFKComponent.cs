using UnityEngine;
using EXILED.Extensions;
using Log = EXILED.Log;

namespace UltimateAFK
{
    public class AFKComponent : MonoBehaviour
    {
        public Vector3 AFKLastPosition;
        public int AFKTime = 0;
        private float timer = 0.0f;
        // Do not change this delay. It will screw up the detection
        public float delay = 1.0f;

        void Start()
        {
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
                ReferenceHub rh = this.gameObject.GetComponent<ReferenceHub>();
                if (rh != null)
                {
                    // AFK Manager is a little fucky for computer, so let's not allow that. 
                    // Also, let's not check dead people, because that's not nice. 
                    if (rh.GetTeam() != Team.RIP && rh.characterClassManager.CurClass != RoleType.Scp079)
                    {
                        Vector3 CurrentPos = this.gameObject.transform.position;

                        if (CurrentPos == this.AFKLastPosition)
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

                                    rh.ClearBroadcasts();
                                    rh.Broadcast(1, $"{Config.msg_prefix} {warning}", false);
                                }
                                else
                                {
                                    Log.Info($"{rh.nicknameSync.MyNick} ({rh.GetUserId()}) was detected as AFK!");
                                    this.AFKTime = 0;
                                    if (Config.kick)
                                        ServerConsole.Disconnect(this.gameObject, Config.kick_message);
                                    else if (Config.replace)
                                        EventHandlers.TryReplacePlayer(rh);
                                    else
                                    {
                                        rh.SetRole(RoleType.Spectator);
                                        rh.Broadcast(30, $"{Config.msg_prefix} {Config.fspec_message}", false);
                                    }
                                }
                            }
                        }
                        else
                            this.AFKLastPosition = CurrentPos;
                    }
                }
            }
        }
    }
}
