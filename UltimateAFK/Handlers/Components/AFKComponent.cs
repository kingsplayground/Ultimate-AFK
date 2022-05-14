using Exiled.API.Features;
using Exiled.API.Features.Roles;
using Exiled.CustomItems.API.Features;
using Exiled.Events.EventArgs;
using Exiled.Permissions.Extensions;
using MEC;
using System;
using System.Collections.Generic;
using System.Linq;
using UltimateAFK.Resources;
using UnityEngine;

namespace UltimateAFK.Handlers.Components
{
    public class AFKComponent : MonoBehaviour
    {
        #region Variables
        public Player MyPlayer;
        public Player ReplacementPlayer = null;

        /// <summary>
        /// If True, the component will ignore this player, but will not be destroyed.
        /// </summary>
        public bool IsDisable = false;
        // ------------->>>>> Sex <<<<------------
        public Vector3 LastPosition;
        public Vector3 LastRotation;
        // -------------- Gwa gwa ---------------
        public int AFKTime = 0;
        public int AFKCount = 0;

        // Coroutine handle
        // Using a MEC Coroutine is more optimized than using Unity methods.
        private CoroutineHandle CountHandler;
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

            if (MyPlayer.CheckPermission("uafk.ignore"))
            {
                Log.Debug($"The player {MyPlayer.Nickname} has the permission \"uafk.ignore\" disabling component");
                IsDisable = true;
            }
        }

        public void Destroy()
        {
            try
            {
                MyPlayer = null;
                ReplacementPlayer = null;

                Exiled.Events.Handlers.Player.Destroying -= OnDestroying;
                Exiled.Events.Handlers.Player.Jumping -= OnJumping;
                Destroy(this);
            }
            catch (Exception)
            {

                throw;
            }
        }

        public void CheckAFK()
        {
            var cantcontinue = MyPlayer.IsDead || Player.List.Count() <= UltimateAFK.Instance.Config.MinPlayers || (UltimateAFK.Instance.Config.IgnoreTut && MyPlayer.IsTutorial) || Round.IsLobby;

            if (!cantcontinue)
            {
                bool isSCP079 = MyPlayer.Role is Scp079Role;
                bool isSCP096 = MyPlayer.Role is Scp096Role;
                bool isNotCrying = false;
                Scp079Role scp079role = null;

                if (isSCP096 || isSCP079)
                {
                    if (isSCP096)
                    {
                        var role = MyPlayer.Role as Scp096Role;
                        isNotCrying = role.TryingNotToCry;
                    }
                    else if (isSCP079)
                    {
                        scp079role = MyPlayer.Role as Scp079Role;
                    }
                }

                Vector3 position = MyPlayer.Position;
                Vector3 vector = isSCP079 ? scp079role.Camera.HeadPosition : (Vector3)MyPlayer.Rotation;

                bool isMoving = position != LastPosition || vector != LastRotation || isNotCrying;

                if (isMoving)
                {
                    this.LastPosition = position;
                    this.LastRotation = vector;
                    this.AFKTime = 0;

                    if (ReplacementPlayer != null)
                        this.ReplacementPlayer = null;
                }
                else
                {

                    var isAfk = AFKTime++ >= UltimateAFK.Instance.Config.AfkTime;

                    if (isAfk)
                    {
                        var graceNumb = UltimateAFK.Instance.Config.AfkTime + UltimateAFK.Instance.Config.GraceTime - this.AFKTime;

                        var inGraceTime = graceNumb > 0;

                        if (inGraceTime)
                        {
                            var message = string.Format(UltimateAFK.Instance.Config.MsgGrace, graceNumb);

                            MyPlayer.Broadcast(2, message, Broadcast.BroadcastFlags.Normal, true);
                        }
                        else
                        {
                            Log.Info($"{MyPlayer.Nickname} ({MyPlayer.UserId}) Detected as AFK");

                            //--- I am going to save the variables that I will need later

                            var items = MyPlayer.Items.ToList();
                            var role = MyPlayer.Role;
                            var plyposition = MyPlayer.Position;
                            var health = MyPlayer.Health;
                            var ammo = MyPlayer.Ammo;
                            var customitems = new List<string>();

                            foreach (var item in MyPlayer.Items)
                            {
                                if (CustomItem.TryGet(item, out var citem))
                                {
                                    customitems.Add(citem.Name);
                                    items.Remove(item);
                                }
                            }

                            var list = Player.List.Where(p => p.IsDead && p.UserId != MyPlayer.UserId && !p.IsOverwatchEnabled && !p.CheckPermission("uafk.ignore") && !p.SessionVariables.ContainsKey("IsNPC"));
                            ReplacementPlayer = list.FirstOrDefault();

                            if (ReplacementPlayer == null)
                            {
                                Log.Debug("Unable to find replacement player moving spectator", UltimateAFK.Instance.Config.DebugMode);

                                MyPlayer.SetRole(RoleType.Spectator);
                                MyPlayer.Broadcast(30, UltimateAFK.Instance.Config.MsgFspec, Broadcast.BroadcastFlags.Normal, true);
                                MyPlayer.SendConsoleMessage(UltimateAFK.Instance.Config.MsgFspec, "white");
                            }
                            else
                            {
                                Log.Debug($"Replacement Player found\nNickname: {ReplacementPlayer.Nickname}\nUserID: {ReplacementPlayer.UserId}\n Role: {ReplacementPlayer.Role.Type}", UltimateAFK.Instance.Config.DebugMode);

                                MainHandler.ReplacingPlayers.Add(ReplacementPlayer, new AFKData
                                {
                                    Position = plyposition,
                                    Role = role,
                                    Ammo = ammo,
                                    Health = health,
                                    Items = items,
                                    CustomItems = customitems,
                                    SCP079Role = scp079role

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

                                Log.Debug("Moving replacement player to the previous player's role", UltimateAFK.Instance.Config.DebugMode);

                                this.ReplacementPlayer.SetRole(role);

                                MyPlayer.SetRole(RoleType.Spectator);
                                MyPlayer.Broadcast(30, UltimateAFK.Instance.Config.MsgFspec, Broadcast.BroadcastFlags.Normal, true);
                                MyPlayer.SendConsoleMessage(UltimateAFK.Instance.Config.MsgFspec, "white");
                            }
                        }
                    }
                }
            }
        }

        private IEnumerator<float> CheckAfkPerSecond()
        {
            while (true)
            {
                if (!IsDisable && Round.IsStarted)
                {
                    CheckAFK();
                }

                yield return Timing.WaitForSeconds(1.2f);
            }
        }

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
    }
}
