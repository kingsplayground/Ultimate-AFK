namespace UltimateAFK.API.Structs
{
    /// <summary>
    /// Represents data related to the role of SCP-079.
    /// </summary>
    public readonly struct Scp079RoleData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Scp079RoleData"/>.
        /// </summary>
        /// <param name="experience">The experience of SCP-079.</param>
        /// <param name="energy">The energy level of SCP-079.</param>
        public Scp079RoleData(int experience, float energy)
        {
            this.Experience = experience;
            this.Energy = energy;
        }

        /// <summary>
        /// Gets the experience SCP-079.
        /// </summary>
        public int Experience { get; }

        /// <summary>
        /// Gets the energy SCP-079.
        /// </summary>
        public float Energy { get; }
    }

}
