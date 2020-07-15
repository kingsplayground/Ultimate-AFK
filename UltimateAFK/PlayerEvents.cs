using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.Events.EventArgs;
using Exiled.Permissions.Extensions;

namespace UltimateAFK
{
    public class PlayerEvents
    {
        public MainClass plugin;
        public PlayerEvents(MainClass plugin) => this.plugin = plugin;

		public void OnPlayerJoin(JoinedEventArgs ev)
		{
			// Add a component to the player to check AFK status.
			ev.Player.GameObject.gameObject.AddComponent<AFKComponent>();
		}

		// This check was moved here, because player's rank's are set AFTER OnPlayerJoin()
		public void OnSetClass(ChangingRoleEventArgs ev)
		{
			try
			{
				if (ev.Player != null)
				{
					AFKComponent afkComponent = ev.Player.GameObject.gameObject.GetComponent<AFKComponent>();
					
					Exiled.API.Features.Log.Info($"Setting AFK Component for {ev.Player.Nickname}");
					//if (afkComponent != null)
					//	if (ev.Player.CheckPermission("uafk.ignore"))
					//		afkComponent.disabled = true;
				}
			}
			catch (Exception e)
			{
				Log.Error($"ERROR In OnSetClass(): {e}");
			}
		}

		/*
		 * The following events are only here as additional AFK checks for some very basic player interactions
		 * I can add more interactions, but this seems good for now.
		 */
		public void OnDoorInteract(InteractingDoorEventArgs ev)
		{
			try
			{
				ResetAFKTime(ev.Player);
			}
			catch (Exception e)
			{
				Log.Error($"ERROR In OnDoorInteract(): {e}");
			}
		}

		public void OnPlayerShoot(ShootingEventArgs ev)
		{
			try
			{
				ResetAFKTime(ev.Shooter);
			}
			catch (Exception e)
			{
				Log.Error($"ERROR In ResetAFKTime(): {e}");
			}
		}
		public void On914Activate(ActivatingEventArgs ev)
		{
			try
			{
				ResetAFKTime(ev.Player);
			}
			catch (Exception e)
			{
				Log.Error($"ERROR In On914Activate(): {e}");
			}
		}
		public void On914Change(ChangingKnobSettingEventArgs ev)
		{
			try
			{
				ResetAFKTime(ev.Player);
			}
			catch (Exception e)
			{
				Log.Error($"ERROR In OnLockerInteract(): {e}");
			}
		}

		public void OnLockerInteract(InteractingLockerEventArgs ev)
		{
			try
			{
				ResetAFKTime(ev.Player);
			}
			catch (Exception e)
			{
				Log.Error($"ERROR In OnLockerInteract(): {e}");
			}
		}
		public void OnDropItem(ItemDroppedEventArgs ev)
		{
			try
			{
				ResetAFKTime(ev.Player);
			}
			catch (Exception e)
			{
				Log.Error($"ERROR In OnDropItem(): {e}");
			}
		}

		public void OnSCP079Exp(GainingExperienceEventArgs ev)
		{
			try
			{
				ResetAFKTime(ev.Player);
			}
			catch (Exception e)
			{
				Log.Error($"ERROR In OnSCP079Exp(): {e}");
			}
		}

		// Thanks iopietro!
		public void ResetAFKTime(Exiled.API.Features.Player player)
		{
			try
			{
				if (player != null)
				{
					AFKComponent afkComponent = player.GameObject.gameObject.GetComponent<AFKComponent>();

					if (afkComponent != null)
						afkComponent.AFKTime = 0;
				}
			}
			catch (Exception e)
			{
				Log.Error($"ERROR In ResetAFKTime(): {e}");
			}
		}
	}
}
