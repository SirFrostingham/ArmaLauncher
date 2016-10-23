using ArmaLauncher.Models;
using ArmaLauncher.Providers;

namespace ArmaLauncher.Managers
{
    public class SteamProviderManager
    {
        private SteamLibProvider SteamLibProvider { get; set; }
        private QueryMasterProvider QueryMasterProvider { get; set; }

        public SteamProviderManager(SteamProviderType steamProviderType)
        {
            Globals.Current.SteamProviderType = steamProviderType;
            SteamLibProvider = new SteamLibProvider();
            QueryMasterProvider = new QueryMasterProvider();
        }

        public Server PingSingleServer(Server dataGridSelectedItem)
        {
            //todo
            switch (Globals.Current.SteamProviderType)
            {
                case SteamProviderType.SteamProvider:
                    SteamLibProvider.PingSingleServer(dataGridSelectedItem);
                    break;
                case SteamProviderType.QueryMaster:
                    //TODO:Add this in
                    return new Server();
            }
            //todo:what is default?
            return new Server();
        }

        public void GetServerList()
        {
            //todo
            switch (Globals.Current.SteamProviderType)
            {
                case SteamProviderType.SteamProvider:
                    SteamLibProvider.GetServerList();
                    break;
                case SteamProviderType.QueryMaster:
                    //TODO:Add this in
                    break;
            }
        }

        public void CancelServerQuery()
        {
            //todo
            switch (Globals.Current.SteamProviderType)
            {
                case SteamProviderType.SteamProvider:
                    SteamLibProvider.CancelQuery();
                    break;
                case SteamProviderType.QueryMaster:
                    //TODO:Add this in
                    break;
            }
        }

        public bool IsQueryCancelPending()
        {
            switch (Globals.Current.SteamProviderType)
            {
                case SteamProviderType.SteamProvider:
                    SteamLibProvider.IsCancelPending();
                    break;
                case SteamProviderType.QueryMaster:
                    //TODO:Add this in
                    return false;
            }
            return false;
        }
    }
}