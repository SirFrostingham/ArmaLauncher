using NLog;

namespace SteamLib
{
    public class GlobalsLib
    {
        private static GlobalsLib _globalsLib;

        public static GlobalsLib Current
        {
            get
            {
                if (_globalsLib == null)
                    _globalsLib = new GlobalsLib();

                return _globalsLib;
            }
        }

        public Logger Logger = LogManager.GetCurrentClassLogger();
    }
}