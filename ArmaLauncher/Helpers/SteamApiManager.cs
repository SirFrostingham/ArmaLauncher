using System;
using System.Printing;
using ArmaLauncher.Models;
using SteamLib.MasterServer;

namespace ArmaLauncher.Helpers
{
    public class SteamApiManager
    {
        private readonly SourceMasterServer masterQuery;

        public SteamApiManager(SourceMasterServer masterQuery)
        {
            this.masterQuery = masterQuery;
        }

        private void GetServers(Object sender, EventArgs e)
        {
            //var filter = SourceMasterServer.QueryFilter.None;
            //SourceMasterServer.QueryGame game = default(SourceMasterServer.QueryGame);

            var textMap = string.Empty;
            var textMod = "arma3";

            masterQuery.QueryAsync(SourceMasterServer.QueryGame.Arma, 
                SourceMasterServer.QueryRegionCode.All, 
                SourceMasterServer.QueryFilter.None, 
                textMap.Trim(), 
                textMod.Trim(),
                SourceMasterQuery_Callback);
        }

        private void SourceMasterQuery_Callback(object sender, SourceMasterServer.QueryAsyncEventArgs e)
        {
            var server = new Server
            {
                Passworded = e.GameServer.Passworded,
                Mod = e.GameServer.Mod,
                Name = e.GameServer.Name,
                Island = e.GameServer.Map,
                NumPlayers = e.GameServer.NumPlayers,
                MaxPlayers = e.GameServer.MaxPlayers,
                Game = {Players = e.GameServer.Players},
                Host = e.GameServer.Host,
                QueryPort = e.GameServer.QueryPort
            };

            server.Name = e.GameServer.Name;

            Globals.Current.ArmaServers.Add(server);

            //var item = new ListViewItem(new[]
            //{
            //    e.GameServer.Name,
            //    e.GameServer.Mod,
            //    string.Format("{0} / {1}", e.GameServer.NumPlayers, e.GameServer.MaxPlayers),
            //    e.GameServer.Map
            //});
            //item.Tag = e.GameServer;
            //serverListItems.Add(item);
            //AddServerListItem(item);
        }
    }
}