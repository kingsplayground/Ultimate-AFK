using Exiled.API.Features;
using Exiled.API.Features.Roles;
using Exiled.Events.EventArgs;
using Exiled.Permissions.Extensions;
using Exiled.API.Extensions;
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

            Log.Debug($"{MyPlayer.Nickname} component fully loaded", UltimateAFK.Instance.Config.DebugMode);
        }

        public void Destroy()
        {
            try
            {
                Log.Debug($"Calling Destroy", UltimateAFK.Instance.Config.DebugMode);

                if (MyPlayer is null)
                    Log.Debug("Player is null in Destroy()");

                MyPlayer = null;
                ReplacementPlayer = null;

                Exiled.Events.Handlers.Player.Destroying -= OnDestroying;
                Exiled.Events.Handlers.Player.Jumping -= OnJumping;

                Destroy(this);
            }
            catch (Exception e)
            {
                Log.Error($"{this} " + e);
                throw;
            }
        }

        public void CheckAFK()
        {
            Log.Debug("CheckAFK before the if call", UltimateAFK.Instance.Config.DebugMode && UltimateAFK.Instance.Config.SpamLogs);

            var cantcontinue = MyPlayer.IsDead || Player.List.Count() <= UltimateAFK.Instance.Config.MinPlayers || (UltimateAFK.Instance.Config.IgnoreTut && MyPlayer.IsTutorial) || Round.IsLobby;

            if (!cantcontinue)
            {
                Log.Debug("CheckAFK inside the if called", UltimateAFK.Instance.Config.DebugMode && UltimateAFK.Instance.Config.SpamLogs);

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
                    Log.Debug("CheckAFK() player is moving, changing position variables", UltimateAFK.Instance.Config.DebugMode);

                    this.LastPosition = position;
                    this.LastRotation = vector;
                    this.AFKTime = 0;

                    if (ReplacementPlayer != null)
                        this.ReplacementPlayer = null;
                }
                else
                {

                    var isAfk = AFKTime++ >= UltimateAFK.Instance.Config.AfkTime;

                    Log.Debug($"{MyPlayer.Nickname} is in not moving, AFKTime: {AFKTime}", UltimateAFK.Instance.Config.DebugMode);

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

                            customitems = null;


                            var list = Player.List.Where(p => p.IsDead && p.UserId != MyPlayer.UserId && !p.IsOverwatchEnabled && !p.CheckPermission("uafk.ignore") && !p.SessionVariables.ContainsKey("IsNPC"));
                            ReplacementPlayer = list.FirstOrDefault();

                            if (ReplacementPlayer == null)
                            {
                                Log.Debug("Unable to find replacement player, moving to spectator...", UltimateAFK.Instance.Config.DebugMode);

                                if (UltimateAFK.Instance.Config.AfkCount != -1)
                                {
                                    AFKCount++;

                                    if (AFKCount >= UltimateAFK.Instance.Config.AfkCount)
                                    {
                                        MyPlayer.SendConsoleMessage(UltimateAFK.Instance.Config.MsgKick, "white");

                                        MyPlayer.Kick(UltimateAFK.Instance.Config.MsgKick, "[UltimateAFK]");

                                        return;
                                    }
                                }

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


                                Log.Debug("Moving replacement player to the previous player's role", UltimateAFK.Instance.Config.DebugMode);

                                this.ReplacementPlayer.SetRole(role);

                                if (UltimateAFK.Instance.Config.AfkCount != -1)
                                {
                                    AFKCount++;

                                    if (AFKCount >= UltimateAFK.Instance.Config.AfkCount)
                                    {
                                        MyPlayer.SendConsoleMessage(UltimateAFK.Instance.Config.MsgKick, "white");

                                        MyPlayer.Kick(UltimateAFK.Instance.Config.MsgKick, "[UltimateAFK]");

                                        return;
                                    }
                                }
                                // I do this, to avoid that when changing to spectator the items that the player had are thrown to the ground :)
                                MyPlayer.ClearInventory();

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
                Log.Debug("Calling CheckAfkPerSecond() before If", UltimateAFK.Instance.Config.DebugMode && UltimateAFK.Instance.Config.SpamLogs);

                if (!IsDisable && Round.IsStarted)
                {
                    try
                    {
                        Log.Debug("Call of CheckAfkPerSecond() inside If", UltimateAFK.Instance.Config.DebugMode && UltimateAFK.Instance.Config.SpamLogs);

                        CheckAFK();
                    }
                    catch (Exception e)
                    {
                        Log.Error($"{this} Error on CheckAFK(): {e} || {e.StackTrace}");
                    }

                }

                yield return Timing.WaitForSeconds(1.2f);
            }
        }

        // Technically this is not necessary since the component is destroyed when the player is destroyed but for fear of leaving a ghost component I better do this.
        public void OnDestroying(DestroyingEventArgs ev)
        {
            if (ev.Player == MyPlayer)
            {
                Log.Debug($"OnDestroying | My player was destroyed by DestroyingEventArg, destroying component", UltimateAFK.Instance.Config.DebugMode);
                Destroy();
            }
        }

        // It is better to do it here than in MainHandler. For those who do not understand when jumping the player restarts his AFK time, since if he jumps technically he is not afk.
        public void OnJumping(JumpingEventArgs ev)
        {
            if (ev.Player != null && ev.Player == MyPlayer && !IsDisable)
            {
                Log.Debug($"OnJumping | My player is jumping, resetting AFK counter", UltimateAFK.Instance.Config.DebugMode);
                AFKTime = 0;
            }
        }
    }
}
