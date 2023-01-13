using System.Collections.Generic;
using System.Linq;
using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Attachments;
using PluginAPI.Core;
using UnityEngine;

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
                ply.SetAmmo(ammoItem.Key, ammoItem.Value);
            }
        }
        
        public static List<ItemType> GetItems(this Player ply)
        {
            var items = ply.ReferenceHub.inventory.UserInventory.Items;
            var returnitems = new List<ItemType>();
            
            foreach(var i in items.Values)
            {
                if(i.ItemTypeId is ItemType.Ammo9x19 or ItemType.Ammo12gauge or ItemType.Ammo44cal or ItemType.Ammo556x45 or ItemType.Ammo762x39) continue;
                
                returnitems.Add(i.ItemTypeId);
            }

            return returnitems;
        }

        /// <summary>
        /// Until NW Fix SendBroadcast.
        /// </summary>
        /// <param name="ply"></param>
        /// <param name="message"></param>
        /// <param name="duration"></param>
        /// <param name="type"></param>
        /// <param name="shouldClearPrevious"></param>
        public static void SendBroadcastToPlayer(this Player ply, string message, ushort duration,
            Broadcast.BroadcastFlags type = Broadcast.BroadcastFlags.Normal, bool shouldClearPrevious = false)
        {
            if (shouldClearPrevious) ClearBroadcasts(ply);
            
            Broadcast.Singleton.TargetAddElement(ply.Connection, message, duration, type);
        }

        private static void ClearBroadcasts(Player ply)
        {
            Broadcast.Singleton.TargetClearElements(ply.Connection);
        }

        /// <summary>
        /// Until NW fix clear inventory.
        /// </summary>
        /// <param name="ply"></param>
        /// <param name="clearAmmo"></param>
        /// <param name="clearItems"></param>
        public static void ClearPlayerInventory(this Player ply, bool clearAmmo = true, bool clearItems = true)
        {
            if (clearAmmo)
            {
                ply.ReferenceHub.inventory.UserInventory.ReserveAmmo.Clear();
            }
            if (clearItems)
            {
                var inventory = ply.ReferenceHub.inventory.UserInventory;
                while (inventory.Items.Count > 0)
                {
                    ply.ReferenceHub.inventory.ServerRemoveItem(inventory.Items.ElementAt(0).Key, null);
                }
            }
        }

        /// <summary>
        /// Applies player attachments.
        /// </summary>
        /// <param name="ply"></param>
        public static void ApplyAttachments(this Player ply)
        {
            var item = ply.Items.Where(i => i is Firearm);

            foreach (var fire in item)
            {
                if (fire is Firearm fireArm)
                {
                    if (AttachmentsServerHandler.PlayerPreferences.TryGetValue(ply.ReferenceHub, out var value) && value.TryGetValue(fireArm.ItemTypeId, out var value2))
                        fireArm.ApplyAttachmentsCode(value2, reValidate: true);
                    var firearmStatusFlags = FirearmStatusFlags.MagazineInserted;
                    if (fireArm.HasAdvantageFlag(AttachmentDescriptiveAdvantages.Flashlight))
                        firearmStatusFlags |= FirearmStatusFlags.FlashlightEnabled;

                    fireArm.Status = new FirearmStatus(fireArm.AmmoManagerModule.MaxAmmo, firearmStatusFlags, fireArm.GetCurrentAttachmentsCode());
                }
            }
        }

        public static void ReloadAllWeapons(this Player ply)
        {
            var item = ply.Items.Where(i => i is Firearm);

            foreach (var weapon  in item)
            {
                if (weapon is Firearm firearm)
                {
                    firearm.Status = new FirearmStatus(firearm.AmmoManagerModule.MaxAmmo, firearm.Status.Flags,
                        firearm.GetCurrentAttachmentsCode());
                }
            }
        }
    }
}