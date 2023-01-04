using System.Collections.Generic;
using PlayerRoles;
using UnityEngine;

namespace UltimateAFK.Resources
{
    public struct AFKData
    {
        public string NickName { get; set; }
        public Vector3 Position { get; set; }

        public RoleTypeId Role { get; set; }

        public Dictionary<ItemType, ushort> Ammo { get; set; }

        public List<ItemType> Items { get; set; }

        public float Health { get; set; }

        public Scp079Data SCP079 { get; set; }
    }
}