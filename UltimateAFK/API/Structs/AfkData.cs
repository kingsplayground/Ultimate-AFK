using PlayerRoles;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateAFK.API.Structs
{
    /// <summary>
    /// Represents data related to an AFK (Away From Keyboard) player.
    /// </summary>
    public readonly struct AfkData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AfkData"/>.
        /// </summary>
        /// <param name="nickname">The nickname of the AFK player.</param>
        /// <param name="position">The position of the AFK player in the game.</param>
        /// <param name="roleType">The role type of the AFK player.</param>
        /// <param name="ammo">A dictionary representing the ammunition of the AFK player (nullable).</param>
        /// <param name="items">A list of item types associated with the AFK player.</param>
        /// <param name="hp">The health points of the AFK player.</param>
        /// <param name="data">SCP-079 role data of the AFK player (nullable).</param>
        public AfkData(string nickname, Vector3 position, RoleTypeId roleType, Dictionary<ItemType, ushort>? ammo, List<ItemType> items, float hp, Scp079RoleData? data)
        {
            this.Nickname = nickname;
            this.Position = position;
            this.RoleType = roleType;
            this.Ammo = ammo;
            this.Items = items;
            this.Health = hp;
            this.Scp079Data = data;
        }

        /// <summary>
        /// Gets the nickname of the AFK player.
        /// </summary>
        public string Nickname { get; }

        /// <summary>
        /// Gets the position of the AFK player in the game.
        /// </summary>
        public Vector3 Position { get; }

        /// <summary>
        /// Gets the role type of the AFK player.
        /// </summary>
        public RoleTypeId RoleType { get; }

        /// <summary>
        /// Gets the ammunition dictionary of the AFK player (nullable).
        /// </summary>
        public Dictionary<ItemType, ushort>? Ammo { get; }

        /// <summary>
        /// Gets the list of item types associated with the AFK player.
        /// </summary>
        public List<ItemType> Items { get; }

        /// <summary>
        /// Gets the health points of the AFK player.
        /// </summary>
        public float Health { get; }

        /// <summary>
        /// Gets the SCP-079 role data of the AFK player (nullable).
        /// </summary>
        public Scp079RoleData? Scp079Data { get; }
    }

}
