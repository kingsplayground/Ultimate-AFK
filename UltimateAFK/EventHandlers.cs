using EXILED;
using System;


namespace UltimateAFK
{
	public class EventHandlers
	{
		public Plugin plugin;
		public EventHandlers(Plugin plugin) => this.plugin = plugin;

		public void OnPlayerJoin(PlayerJoinEvent ev)
		{
			// Add a component to the player to check AFK status.
			ev.Player.gameObject.AddComponent<AFKComponent>();
		}
		
		// This check was moved here, because player's rank's are set AFTER OnPlayerJoin()
		public void OnSetClass(SetClassEvent ev)
		{
			try
			{
				if (ev.Player != null)
				{
					AFKComponent afkComponent = ev.Player.gameObject.GetComponent<AFKComponent>();

					if (afkComponent != null)
						if (ev.Player.CheckPermission("uafk.ignore"))
							afkComponent.disabled = true;
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
		public void OnDoorInteract(ref DoorInteractionEvent ev)
		{
			ResetAFKTime(ev.Player);
		}

		public void OnPlayerShoot(ref ShootEvent ev)
		{
			ResetAFKTime(ev.Shooter);
		}

		public void On914Activate(ref Scp914ActivationEvent ev)
		{
			ResetAFKTime(ev.Player);
		}
		public void On914Change(ref Scp914KnobChangeEvent ev)
		{
			ResetAFKTime(ev.Player);
		}

		public void OnLockerInteract(LockerInteractionEvent ev)
		{
			ResetAFKTime(ev.Player);
		}
		public void OnDropItem(ref DropItemEvent ev)
		{
			ResetAFKTime(ev.Player);
		}

		public void OnSCP079Exp(Scp079ExpGainEvent ev)
		{
			ResetAFKTime(ev.Player);
		}

		// Thanks iopietro!
		public void ResetAFKTime(ReferenceHub player)
		{
			try
			{
				if (player != null)
				{
					AFKComponent afkComponent = player.gameObject.GetComponent<AFKComponent>();

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