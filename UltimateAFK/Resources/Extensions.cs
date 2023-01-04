using System.Collections.Generic;
using InventorySystem.Items;
using PluginAPI.Core;

namespace UltimateAFK.Resources
{
    public static class Extensions
    {
        public static void SendItems(this Player player, List<ItemType> types)
        {
            foreach (var item in types)
            {
                player.AddItem(item);
            }
        }

        public static void SendAmmo(this Player ply, Dictionary<ItemType, ushort> ammo)
        {
            foreach (var ammoItem in ammo)
            {
                ply.AddAmmo(ammoItem.Key, ammoItem.Value);
            }
        }
        
        public static List<ItemType> GetItems(this Player ply)
        {
            var items = ply.ReferenceHub.inventory.UserInventory.Items;
            var returnitems = new List<ItemType>();
            
            foreach(var i in items.Values)
            {
                returnitems.Add(i.ItemTypeId);
            }

            return returnitems;
        }
    }
}