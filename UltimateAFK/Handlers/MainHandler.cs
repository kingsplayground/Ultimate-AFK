using Exiled.API.Features;
using Exiled.API.Features.Roles;
using Exiled.CustomItems.API.Features;
using Exiled.Events.EventArgs;
using MEC;
using System.Collections.Generic;
using UltimateAFK.Handlers.Components;
using UltimateAFK.Resources;

namespace UltimateAFK.Handlers
{
    /// <summary>
    /// Class where the <see cref="Components.AFKComponent"/> will be given to the <see cref="Player"/> and the afk counter will be reset by the player's actions.
    /// </summary>
    public class MainHandler : API.Base.Handler
    {
        // A list of players who are replacing someone else
        public static Dictionary<Player, AFKData> ReplacingPlayers = new Dictionary<Player, AFKData>();

        public override void Start()
        {
            Log.Warn("Loading MainHandler");

            Exiled.Events.Handlers.Player.Verified += OnVerify;
            Exiled.Events.Handlers.Player.ChangingRole += OnChangingRole;

            Exiled.Events.Handlers.Player.Shooting += OnShooting;
            Exiled.Events.Handlers.Player.InteractingDoor += OnInteractDoor;
            Exiled.Events.Handlers.Player.InteractingElevator += OnInteractElevator;
            Exiled.Events.Handlers.Player.InteractingLocker += OnInteractLocker;
            Exiled.Events.Handlers.Player.InteractingScp330 += OnInteract330;

            Exiled.Events.Handlers.Scp914.Activating += OnInteract914;
            Exiled.Events.Handlers.Scp914.ChangingKnobSetting += OnChaningKnob914;

            Exiled.Events.Handlers.Player.DroppingItem += OnDroppingItems;
            Exiled.Events.Handlers.Player.DroppingAmmo += OnDroppingAmmo;

            Exiled.Events.Handlers.Scp079.GainingExperience += OnGaingExp;

            Exiled.Events.Handlers.Scp079.ChangingCamera += OnChangeCamara;

            Exiled.Events.Handlers.Server.RoundStarted += OnRoundStarted;

            Log.Warn("MainHandler Fully loaded");
        }

        public override void Stop()
        {
            Exiled.Events.Handlers.Player.Verified -= OnVerify;
            Exiled.Events.Handlers.Player.ChangingRole -= OnChangingRole;

            Exiled.Events.Handlers.Player.Shooting -= OnShooting;
            Exiled.Events.Handlers.Player.InteractingDoor -= OnInteractDoor;
            Exiled.Events.Handlers.Player.InteractingElevator -= OnInteractElevator;
            Exiled.Events.Handlers.Player.InteractingLocker -= OnInteractLocker;
            Exiled.Events.Handlers.Player.InteractingScp330 -= OnInteract330;

            Exiled.Events.Handlers.Scp914.Activating -= OnInteract914;
            Exiled.Events.Handlers.Scp914.ChangingKnobSetting -= OnChaningKnob914;

            Exiled.Events.Handlers.Player.DroppingItem -= OnDroppingItems;
            Exiled.Events.Handlers.Player.DroppingAmmo -= OnDroppingAmmo;

            Exiled.Events.Handlers.Scp079.GainingExperience -= OnGaingExp;

            Exiled.Events.Handlers.Scp079.ChangingCamera -= OnChangeCamara;

            Exiled.Events.Handlers.Server.RoundStarted -= OnRoundStarted;

            ReplacingPlayers.Clear();
            ReplacingPlayers = null;
        }


        // Player Join server
        public void OnVerify(VerifiedEventArgs ev)
        {
            if (ev.Player.GameObject.TryGetComponent<AFKComponent>(out var com))
            {
                com.Destroy();

                ev.Player.GameObject.AddComponent<AFKComponent>();
            }
            else
            {
                ev.Player.GameObject.AddComponent<AFKComponent>();
            }


        }

        public void OnChangingRole(ChangingRoleEventArgs ev)
        {
            if (ev.Player != null && ev.Player.GameObject.TryGetComponent<AFKComponent>(out var component))
            {
                try
                {
                    if (ev.Player.SessionVariables.ContainsKey("IsNPC"))
                    {
                        component.Destroy();
                        Log.Warn("Destroying the component to a player who was an NPC");
                        return;
                    }

                    if (ReplacingPlayers.TryGetValue(ev.Player, out var data))
                    {
                        Log.Debug("Detecting player who replaces an AFK", Plugin.Config.DebugMode);

                        ev.Items.Clear();

                        Log.Debug("Adding items from previous player", Plugin.Config.DebugMode);
                        foreach (var item in data.Items)
                        {
                            ev.Items.Add(item.Type);
                        }

                        Timing.CallDelayed(0.8f, () =>
                        {
                            Log.Debug("Changing player position and HP", Plugin.Config.DebugMode);

                            ev.Player.Position = data.Position;
                            ev.Player.Broadcast(16, UltimateAFK.Instance.Config.MsgReplace, Broadcast.BroadcastFlags.Normal, true);
                            ev.Player.SendConsoleMessage(UltimateAFK.Instance.Config.MsgReplace, "white");
                            ev.Player.Health = data.Health;
                            Log.Debug("Adding Ammo", Plugin.Config.DebugMode);

                            ev.Player.Inventory.UserInventory.ReserveAmmo = data.Ammo;
                            ev.Player.Inventory.SendAmmoNextFrame = true;


                            if (ev.NewRole == RoleType.Scp079 && data.SCP079Role != null)
                            {
                                Log.Debug("The new role is a SCP079, transferring level and experience.", Plugin.Config.DebugMode);

                                var scprole = ev.Player.Role as Scp079Role;
                                scprole.Level = data.SCP079Role.Level;
                                scprole.Energy = data.SCP079Role.Energy;
                                scprole.Experience = data.SCP079Role.Experience;
                            }

                            if (data.CustomItems != null)
                            {
                                Log.Debug("The AFK had CustomItems added to its replacement.", Plugin.Config.DebugMode);
                                foreach (var item in data.CustomItems)
                                {
                                    if (CustomItem.TryGet(item, out var citem))
                                    {
                                        Log.Debug($"CustomItem {citem.Name} was added to the player's inventory", Plugin.Config.DebugMode);

                                        citem.Give(ev.Player, false);
                                    }
                                }
                            }

                            Log.Debug("Removing the replacement player from the dictionary", Plugin.Config.DebugMode);

                            ReplacingPlayers.Remove(ev.Player);

                        });
                    }
                }
                catch (System.Exception e)
                {
                    Log.Error($"Error when trying to replace a player  || {e} {e.StackTrace}");
                }

            }
        }



        public void OnRoundStarted()
        {
            ReplacingPlayers.Clear();
        }

        private void ResetAFKTime(Player ply)
        {
            if (ply != null && ply.GameObject.TryGetComponent<AFKComponent>(out var comp))
            {
                comp.AFKTime = 0;
            }
        }

        private IEnumerator<float> CountHandle()
        {
            while (true)
            {
                yield return Timing.WaitForSeconds(1.5f);
            }
        }

        #region Reset AFK Timers

        public void OnInteractDoor(InteractingDoorEventArgs ev)
        {
            ResetAFKTime(ev.Player);
        }

        public void OnInteractElevator(InteractingElevatorEventArgs ev)
        {
            ResetAFKTime(ev.Player);
        }

        public void OnInteractLocker(InteractingLockerEventArgs ev)
        {
            ResetAFKTime(ev.Player);
        }

        public void OnInteract330(InteractingScp330EventArgs ev)
        {
            ResetAFKTime(ev.Player);
        }

        public void OnInteract914(ActivatingEventArgs ev)
        {
            ResetAFKTime(ev.Player);
        }

        public void OnChaningKnob914(ChangingKnobSettingEventArgs ev)
        {
            ResetAFKTime(ev.Player);
        }

        public void OnDroppingItems(DroppingItemEventArgs ev)
        {
            ResetAFKTime(ev.Player);
        }

        public void OnDroppingAmmo(DroppingAmmoEventArgs ev)
        {
            ResetAFKTime(ev.Player);
        }

        public void OnGaingExp(GainingExperienceEventArgs ev)
        {
            ResetAFKTime(ev.Player);

        }

        public void OnChangeCamara(ChangingCameraEventArgs ev)
        {
            ResetAFKTime(ev.Player);
        }

        public void OnShooting(ShootingEventArgs ev)
        {
            ResetAFKTime(ev.Shooter);
        }
        #endregion

    }
}
