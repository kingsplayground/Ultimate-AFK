using System;
using System.Reflection;
using Exiled.API.Features;
using Exiled.Events.EventArgs;
using Exiled.Permissions.Extensions;
using Exiled.Loader;
using System.Linq;
using System.Collections.Generic;
using MEC;

namespace UltimateAFK
{
	public class PlayerEvents
	{
		public MainClass plugin;

		internal static Dictionary<Player, AFKData> ReplacingPlayers = new Dictionary<Player, AFKData>();

		public PlayerEvents(MainClass plugin)
		{
			this.plugin = plugin;
		}

		public void OnPlayerVerified(VerifiedEventArgs ev)
		{
			// Add a component to the player to check AFK status.
			AFKComponent afkComponent = ev.Player.GameObject.gameObject.AddComponent<AFKComponent>();
			afkComponent.plugin = plugin;
		}

		// This check was moved here, because player's rank's are set AFTER OnPlayerJoin()
		public void OnSetClass(ChangingRoleEventArgs ev)
		{
			try
			{
				if (ev.Player == null) return;
				AFKComponent afkComponent = ev.Player.GameObject.gameObject.GetComponent<AFKComponent>();

				if (afkComponent != null)
				{
					if (!plugin.Config.IgnorePermissionsAndIP)
						if (ev.Player.CheckPermission("uafk.ignore") || ev.Player.IPAddress == "127.0.0.1") //127.0.0.1 is sometimes used for "Pets" which causes issues
							afkComponent.disabled = true;
					if (IsGhost(ev.Player))
						afkComponent.disabled = true;
				}

				if (ReplacingPlayers.ContainsKey(ev.Player))
				{
					AFKData data = ReplacingPlayers[ev.Player];
					ev.Items.Clear();
					ev.Items.AddRange(data.items);
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
		public void OnDropItem(DroppingItemEventArgs ev)
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

		public void OnRoundStarted()
		{
			ReplacingPlayers.Clear();
		}

		public void OnSpawning(SpawningEventArgs ev)
		{
			if (ReplacingPlayers.ContainsKey(ev.Player))
			{
				AFKData data = ReplacingPlayers[ev.Player];
				data.afkComp.PlayerToReplace = null;
				ev.Position = data.spawnLocation;
				ev.Player.Broadcast(10, $"{plugin.Config.MsgPrefix} {plugin.Config.MsgReplace}");
				Timing.CallDelayed(0.4f, () =>
				{
					Assembly easyEvents = Loader.Plugins.FirstOrDefault(pl => pl.Name == "EasyEvents")?.Assembly;
					ev.Player.Health = data.health;
					foreach (ItemType ammoType in data.ammo.Keys)
					{
						ev.Player.Inventory.UserInventory.ReserveAmmo[ammoType] = data.ammo[ammoType];
						ev.Player.Inventory.SendAmmoNextFrame = true;
					}

					if (data.is079)
					{
						ev.Player.Level = data.level;
						ev.Player.Experience = data.xp;
						ev.Player.Energy = data.energy;
					}

					if (data.roleEasyEvents != null) easyEvents?.GetType("EasyEvents.CustomRoles")?.GetMethod("ChangeRole")?.Invoke(null, new object[] { ev.Player, data.roleEasyEvents });
					PlayerEvents.ReplacingPlayers.Remove(ev.Player);
				});
			}
		}

		/// <summary>
		/// Reset the AFK time of a player.
		/// Thanks iopietro!
		/// </summary>
		/// <param name="player"></param>
		public void ResetAFKTime(Player player)
		{
			try
			{
				if (player == null) return;

				AFKComponent afkComponent = player.GameObject.gameObject.GetComponent<AFKComponent>();

				if (afkComponent != null)
					afkComponent.AFKTime = 0;

			}
			catch (Exception e)
			{
				Log.Error($"ERROR In ResetAFKTime(): {e}");
			}
		}

		/// <summary>
		/// Checks if a player is a "ghost" using GhostSpectator's API.
		/// </summary>
		/// <param name="player"></param>
		/// <returns></returns>
		public static bool IsGhost(Player player)
		{
			Assembly assembly = Loader.Plugins.FirstOrDefault(pl => pl.Name == "GhostSpectator")?.Assembly;
			if (assembly == null) return false;
			return ((bool)assembly.GetType("GhostSpectator.API")?.GetMethod("IsGhost")?.Invoke(null, new object[] { player })) == true;
		}
	}
}
