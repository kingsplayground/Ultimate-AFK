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
		public static Vector3 GetPlayerPosition(ReferenceHub hub)
		{
			return hub.plyMovementSync.GetRealPosition();
		}

		// Credit: https://github.com/Cyanox62/DCReplace
		public static void TryReplacePlayer(ReferenceHub toreplace)
		{
			if (toreplace.GetTeam() != Team.RIP)
			{
				Inventory.SyncListItemInfo items = toreplace.inventory.items;
				RoleType role = toreplace.GetRole();
				Vector3 pos = toreplace.transform.position;
				int health = (int)toreplace.playerStats.health;
				string ammo = toreplace.ammoBox.amount;

				ReferenceHub player = Player.GetHubs().FirstOrDefault(x => x.GetRole() == RoleType.Spectator && x.characterClassManager.UserId != string.Empty && !x.GetOverwatch() && x != toreplace);
				if (player != null)
				{
					player.SetRole(role);
					Timing.CallDelayed(0.3f, () =>
					{
						player.SetPosition(pos);
						player.inventory.items.ToList().Clear();
						foreach (var item in items) player.inventory.AddNewItem(item.id);
						player.playerStats.health = health;
						player.ammoBox.Networkamount = ammo;

						player.Broadcast(10, $"{Config.msg_prefix} {Config.replace_message}", false);
						// Clear their items because we are giving said items to the player already.
						toreplace.inventory.Clear();
						toreplace.characterClassManager.SetClassID(RoleType.Spectator);
						toreplace.Broadcast(30, $"{Config.msg_prefix} {Config.fspec_message}", false);
					});
				}
				else
				{
					toreplace.characterClassManager.SetClassID(RoleType.Spectator);
					toreplace.Broadcast(30, $"{Config.msg_prefix} {Config.fspec_message}", false);
				}
			}
		}
	}
}