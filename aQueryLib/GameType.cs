namespace SteamLib
{
    /// <summary>
    /// Gameserver types
    /// </summary>
    public enum GameType
    {

        /// <summary>
        /// Half-Life
        /// </summary>
        Arma = GameProtocol.Arma,
        /// <summary>
        /// Half-Life
        /// </summary>
        HalfLife = GameProtocol.HalfLife,
        /// <summary>
        /// Counter-Strike: v1.6
        /// </summary>
        CounterStrike_16 = GameProtocol.HalfLife,
        /// <summary>
        /// Counter-Strike: Condition Zero
        /// </summary>
        CounterStrike_ConditionZero = GameProtocol.HalfLife,
        /// <summary>
        /// Day of Defeat
        /// </summary>
        DayOfDefeat = GameProtocol.HalfLife,
        /// <summary>
        /// Gunman Chronicles
        /// </summary>
        GunmanChronicles = GameProtocol.HalfLife,


        /// <summary>
        /// Source Engine (Generic Protocol)
        /// </summary>
        Source = GameProtocol.Source,
        /// <summary>
        /// Half-Life 2
        /// </summary>
        HalfLife2 = GameProtocol.Source,
        /// <summary>
        /// Counter-Strike: Source
        /// </summary>
        CounterStrikeSource = GameProtocol.Source,

        /// <summary>
        /// Not listed game
        /// </summary>
        Unknown = GameProtocol.None
    }

    /// <summary>
    /// Gameserver protocol
    /// </summary>
    public enum GameProtocol
    {
        /// <summary>
        /// Halflife and HL-Mods
        /// </summary>
        Arma,
        /// <summary>
        /// Halflife and HL-Mods
        /// </summary>
        HalfLife,
        /// <summary>
        /// Halflife Source
        /// </summary>
        Source,
        /// <summary>
        /// Unknown
        /// </summary>
        None
    }
}


