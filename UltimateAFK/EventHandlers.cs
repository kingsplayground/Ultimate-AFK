using System.Linq;
using EXILED;
using EXILED.Extensions;
using MEC;
using UnityEngine;

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
		
		/*
		 * The following events are only here as additional AFK checks for some very basic player interactions
		 * I can add more interactions, but this seems good for now.
		 */
		public void OnDoorInteract(ref DoorInteractionEvent ev)
		{
			if (ev.Player.gameObject != null)
				ev.Player.gameObject.GetComponent<AFKComponent>().AFKTime = 0;
		}

		public void OnPlayerShoot(ref ShootEvent ev)
		{
			if (ev.Shooter.gameObject != null)
				ev.Shooter.gameObject.GetComponent<AFKComponent>().AFKTime = 0;
		}

		public void On914Activate(ref Scp914ActivationEvent ev)
		{
			if (ev.Player.gameObject != null)
				ev.Player.gameObject.GetComponent<AFKComponent>().AFKTime = 0;
		}
		public void On914Change(ref Scp914KnobChangeEvent ev)
		{
			if (ev.Player.gameObject != null)
				ev.Player.gameObject.GetComponent<AFKComponent>().AFKTime = 0;
		}

		public void OnLockerInteract(LockerInteractionEvent ev)
		{
			if(ev.Player.gameObject != null)
				ev.Player.gameObject.GetComponent<AFKComponent>().AFKTime = 0;
		}
		public void OnDropItem(ref DropItemEvent ev)
		{
			if (ev.Player.gameObject != null)
				ev.Player.gameObject.GetComponent<AFKComponent>().AFKTime = 0;
		}

		public void OnSCP079Exp(Scp079ExpGainEvent ev)
		{
			if (ev.Player.gameObject != null)
				ev.Player.gameObject.GetComponent<AFKComponent>().AFKTime = 0;
		}
	}
}