using Exiled.API.Features;
using Exiled.API.Features.Roles;
using Exiled.Events.EventArgs;
using MEC;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UltimateAFK.Handlers.Components
{
    /// <summary>
    /// Cringe componente for Players. Warning doing an AFKChecker is not a pretty thing to look at but I will try to document so you understand what I am trying to do :)
    /// </summary>
    public class AFKComponent : MonoBehaviour
    {
        #region Variables and Stuff
        public Player MyPlayer { get; set; }

        /// <summary>
        /// If True, the component will ignore this player, but will not be destroyed.
        /// </summary>
        public bool IsDisable { get; set; }

        public Player ReplacementPlayer { get; internal set; }

        // Using a MEC Coroutine is more optimized than using Unity methods.
        private CoroutineHandle CountHandler;

        public Vector3 AFKLastPosition;

        public Vector3 AFKLastAngle;

        public int AFKTime;

        public int AFKCount;
        #endregion

        private void Awake()
        {
            if (!(Player.Get(gameObject) is Player ply))
            {
                Log.Error($"{this} Error Getting Player");
                Destroy();
                return;
            }

            MyPlayer = ply;

            Exiled.Events.Handlers.Player.Destroying += OnDestroying;
            Exiled.Events.Handlers.Player.Jumping += OnJumping;

            // Coroutine dies when the component or the ReferenceHub (Player) is destroyed.
            CountHandler = Timing.RunCoroutine(CheckAfkPerSecond().CancelWith(this).CancelWith(gameObject));
        }

        private void OnDestroy()
        {
            Destroy(true);
        }

        // With this you can destroy the component in a more controlled way.
        public void Destroy(bool value = false)
        {
            try
            {
                if (value)
                {
                    // This is to avoid a loop where the component constantly calls OnDestroy because Destroy calls OnDestroy and OnDestroy calls Destroy.
                    Exiled.Events.Handlers.Player.Destroying -= OnDestroying;
                    Exiled.Events.Handlers.Player.Jumping -= OnJumping;
                }
                else
                {
                    Exiled.Events.Handlers.Player.Destroying -= OnDestroying;
                    Exiled.Events.Handlers.Player.Jumping -= OnJumping;
                    Destroy(this);
                }
            }
            catch (Exception e)
            {
                Log.Error($"Exception: {e}\n Couldn't destroy: {this}\nIs ReferenceHub null? {MyPlayer is null}");
            }
        }


        private IEnumerator<float> CheckAfkPerSecond()
        {
            while (true)
            {
                if (!IsDisable)
                {
                    AFKChecker();
                }

                yield return Timing.WaitForSeconds(1.2f);
            }
        }

        #region AFKChecker | CRINGE AAAAAAAAAAAAAAA 
        private void AFKChecker()
        {

            bool cantContinue = MyPlayer.IsDead || Player.List.Count() <= UltimateAFK.Instance.Config.MinPlayers || (UltimateAFK.Instance.Config.IgnoreTut && MyPlayer.IsTutorial) || Round.IsLobby;

            //Log.Debug($"Can continue ? || Player is Dead {MyPlayer.IsDead} || Player list is low {Player.List.Count() <= UltimateAFK.Instance.Config.MinPlayers} || Player is tutorial and config says no {UltimateAFK.Instance.Config.IgnoreTut && MyPlayer.IsTutorial} || Round is in lobby {Round.IsLobby}");
            if (!cantContinue)
            {

                #region Check if player is 079 or 096
                Scp079Role scp079Role = MyPlayer.Role as Scp079Role;
                Scp096Role scp096Role = MyPlayer.Role as Scp096Role;

                bool isNotCrying = false;
                bool is079 = scp079Role != null;
                bool is096 = scp096Role != null;

                if (is096 || is079)
                {
                    if (is096)
                    {
                        scp096Role = MyPlayer.Role as Scp096Role;
                        isNotCrying = scp096Role.State == PlayableScps.Scp096PlayerState.TryNotToCry;
                    }
                    else if (is079)
                    {
                        // Unnecessary, but whatever
                        scp079Role = MyPlayer.Role as Scp079Role;
                    }
                }
                #endregion

                Vector3 position = MyPlayer.Position;
                Vector3 vector = is079 ? scp079Role.Camera.HeadPosition : (Vector3)MyPlayer.Rotation;

                bool isMoving = position != AFKLastPosition || vector != AFKLastAngle || isNotCrying;

                if (isMoving)
                {

                    this.AFKLastPosition = position;
                    this.AFKLastAngle = vector;
                    this.AFKTime = 0;
                    this.ReplacementPlayer = null;
                }
                else
                {
                    AFKTime++;

                    bool isNotAFK = AFKTime < UltimateAFK.Instance.Config.AfkTime;

                    if (!isNotAFK)
                    {
                        int num = UltimateAFK.Instance.Config.AfkTime + UltimateAFK.Instance.Config.GraceTime - this.AFKTime;

                        bool isInGrace = num > 0;

                        if (isInGrace)
                        {
                            var message = string.Format(UltimateAFK.Instance.Config.MsgGrace, num);

                            MyPlayer.Broadcast(2, message, Broadcast.BroadcastFlags.Normal, true);
                        }
                        else
                        {
                            Log.Info($"{MyPlayer.Nickname} ({MyPlayer.UserId}) Detected as AFK");

                            if (MyPlayer.IsAlive)
                            {
                                var items = MyPlayer.Items;
                                var role = MyPlayer.Role;
                                var plyposition = MyPlayer.Position;
                                var health = MyPlayer.Health;
                                var ammo = MyPlayer.Ammo;
                                ReplacementPlayer = Player.List.FirstOrDefault(p => p.Role.Type == RoleType.Spectator && !p.IsHost && !p.IsOverwatchEnabled && p != MyPlayer);

                                if (ReplacementPlayer != null)
                                {
                                    MyPlayer.ClearInventory();
                                    MyPlayer.SetRole(RoleType.Spectator, Exiled.API.Enums.SpawnReason.ForceClass, false);
                                    MyPlayer.Broadcast(30, UltimateAFK.Instance.Config.MsgFspec, Broadcast.BroadcastFlags.Normal, false);
                                    MyPlayer.SendConsoleMessage(UltimateAFK.Instance.Config.MsgFspec, "white");

                                    MainHandler.ReplacingPlayers.Add(this.ReplacementPlayer, new Resources.AFKData
                                    {
                                        AfkComp = this,
                                        SpawnLocation = plyposition,
                                        Ammo = ammo,
                                        Items = (List<ItemType>)items,
                                        Is079 = is079,
                                        Level = scp079Role != null ? scp079Role.Level : (byte)0,
                                        Xp = scp079Role != null ? scp079Role.Experience : 0f,
                                        Energy = scp079Role != null ? scp079Role.Energy : 0f,
                                        Health = health,
                                    });

                                    if (UltimateAFK.Instance.Config.AfkCount != -1)
                                    {
                                        AFKCount++;

                                        if (AFKCount > UltimateAFK.Instance.Config.AfkCount)
                                        {
                                            MyPlayer.SendConsoleMessage(UltimateAFK.Instance.Config.MsgKick, "white");

                                            MyPlayer.Kick(UltimateAFK.Instance.Config.MsgKick, "[UltimateAFK]");
                                        }
                                    }


                                    this.ReplacementPlayer.SetRole(role);

                                }
                                else
                                {
                                    MyPlayer.SetRole(RoleType.Spectator);
                                    MyPlayer.Broadcast(30, UltimateAFK.Instance.Config.MsgFspec, Broadcast.BroadcastFlags.Normal, true);
                                    MyPlayer.SendConsoleMessage(UltimateAFK.Instance.Config.MsgFspec, "white");
                                }
                            }
                            else
                            {
                                //It does nothing because the player is dead, so why detect him as an afk?
                            }

                        }
                    }
                }
            }

        }
        #endregion

        #region Events
        // Technically this is not necessary since the component is destroyed when the player is destroyed but for fear of leaving a ghost component I better do this.
        public void OnDestroying(DestroyingEventArgs ev)
        {
            if (ev.Player == MyPlayer)
            {
                Destroy();
            }
        }

        // It is better to do it here than in MainHandler. For those who do not understand when jumping the player restarts his AFK time, since if he jumps technically he is not afk.
        public void OnJumping(JumpingEventArgs ev)
        {
            if (ev.Player != null && ev.Player == MyPlayer && !IsDisable)
            {
                AFKTime = 0;
            }
        }

        #endregion
    }
}
