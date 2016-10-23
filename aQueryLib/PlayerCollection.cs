namespace SteamLib
{
    /// <summary>
    /// A strongly typed collection for holding player information.
    /// </summary>
    public class PlayerCollection : System.Collections.CollectionBase
    {
        /// <summary>
        /// Adds an item to the PlayerCollection.
        /// </summary>
        /// <param name="value">The Player to add to the PlayerCollection</param>
        /// <returns>The position into which the new element was inserted.</returns>
        public int Add(Player value)
        {
            return base.List.Add(value);
        }

        /// <summary>
        /// Removes the first occurrence of a specific Player from the PlayerCollection.
        /// </summary>
        /// <param name="value">The Player to remove from the PlayerCollection</param>
        public void Remove(Player value)
        {
            base.List.Remove(value);
        }

        /// <summary>
        /// Inserts an item to the PlayerCollection at the specified position.
        /// </summary>
        /// <param name="index">The zero-based index at which value should be inserted.</param>
        /// <param name="value">The Player to insert into the PlayerCollection.</param>
        public void Insert(int index, Player value)
        {
            base.List.Insert(index, value);
        }

        /// <summary>
        /// Determines whether the PlayerCollection contains a specific value.
        /// </summary>
        /// <param name="value">The Player to locate in the PlayerCollection.</param>
        /// <returns>true if the Player is found in the PlayerCollection; otherwise, false.</returns>
        public bool Contains(Player value)
        {
            return base.List.Contains(value);
        }

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        public Player this[int index]
        {
            get { return (Player)base.List[index]; }
            set { base.List[index] = value; }
        }

        /// <summary>
        /// Creates a new instance of the PlayerCollection class.
        /// </summary>
        public PlayerCollection()
            : base()
        {
        }
    }
}


