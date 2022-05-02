using Exiled.API.Features;
using Exiled.API.Features.Roles;
using Exiled.Events.EventArgs;
using Exiled.Permissions.Extensions;
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

            Exiled.Events.Handlers.Server.RoundStarted += OnRoundStarted;
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

            Exiled.Events.Handlers.Server.RoundStarted -= OnRoundStarted;

            ReplacingPlayers.Clear();
            ReplacingPlayers = null;
        }


        public void OnVerify(VerifiedEventArgs ev)
        {
            if (!ev.Player.GameObject.TryGetComponent<AFKComponent>(out var _))
            {
                ev.Player.GameObject.AddComponent<AFKComponent>();
            }
        }

        public void OnChangingRole(ChangingRoleEventArgs ev)
        {
            if (ev.Player != null && ev.Player.GameObject.TryGetComponent<AFKComponent>(out var component))
            {
                if (ev.Player.CheckPermission("uafk.ignore") || ev.Player.SessionVariables.ContainsKey("IsNPC"))
                {
                    component.IsDisable = true;
                }

                if (ReplacingPlayers.TryGetValue(ev.Player, out var data))
                {
                    ev.Items.Clear();
                    ev.Items.AddRange(data.Items);

                    Timing.CallDelayed(0.8f, () =>
                    {
                        data.AfkComp.ReplacementPlayer = null;

                        ev.Player.Position = data.SpawnLocation;
                        ev.Player.Broadcast(16, UltimateAFK.Instance.Config.MsgReplace, Broadcast.BroadcastFlags.Normal, true);
                        ev.Player.SendConsoleMessage(UltimateAFK.Instance.Config.MsgReplace, "white");
                        ev.Player.Health = data.Health;
                        ev.Player.Inventory.UserInventory.ReserveAmmo = data.Ammo;
                        ev.Player.Inventory.SendAmmoNextFrame = true;

                        if (ev.NewRole == RoleType.Scp079 && data.Is079)
                        {
                            var scprole = ev.Player.Role as Scp079Role;

                            scprole.Level = data.Level;

                            scprole.Experience = data.Xp;

                            scprole.Energy = data.Energy;
                        }

                        ReplacingPlayers.Remove(ev.Player);

                    });
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

        public void OnShooting(ShootingEventArgs ev)
        {
            ResetAFKTime(ev.Shooter);
        }
        #endregion
    }
}
