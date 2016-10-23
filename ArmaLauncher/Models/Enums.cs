using System.ComponentModel;
using ArmaLauncher.Helpers;
using ArmaLauncher.Properties;

namespace ArmaLauncher.Models
{
    public enum GameType
    {
        [LocalizableDescription(@"", typeof(Resources))]
        [Description("")]
        None = 0,
        [LocalizableDescription(@"Arma2", typeof(Resources))]
        [Description("arma2arrowpc")]
        Arma2 = 1,
        [LocalizableDescription(@"Arma3", typeof(Resources))]
        [Description("arma3")]
        Arma3 = 2,
        [LocalizableDescription(@"DayZ", typeof(Resources))]
        [Description("DayZ")]
        DayZ = 3,
        [LocalizableDescription(@"Empyrion", typeof(Resources))]
        [Description("egs")]
        Empyrion = 4,
        [LocalizableDescription(@"Ark", typeof(Resources))]
        [Description("ARK_Survival_Evolved")]
        Ark = 5
    }

    public enum SteamProviderType
    {
        SteamProvider = 0,
        QueryMaster = 1
    }
}