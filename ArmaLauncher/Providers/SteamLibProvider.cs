using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using ArmaLauncher.Helpers;
using ArmaLauncher.Models;
using ArmaLauncher.ViewModel;
using SteamLib;
using SteamLib.MasterServer;

namespace ArmaLauncher.Providers
{
    public class SteamLibProvider
    {
        public SteamLibProvider()
        {
            _masterQuery = new SourceMasterServer();
            //masterQuery.SynchronizingObject = this;
            _masterQuery.QueryAsyncCompleted += SourceMasterServer_QueryAsyncCompleted;
        }

        private SourceMasterServer _masterQuery;

        public void CancelQuery()
        {
            _masterQuery.CancelAsyncQuery();
        }

        public bool IsCancelPending()
        {
            return _masterQuery.QueryCancellationPending;
        }

        public void GetServerList()
        {
            var textMap = String.Empty;
            var textMod = Globals.Current.ArmaGameType.ToDescriptionString();

            _masterQuery.QueryAsync(SourceMasterServer.QueryGame.Arma,
                SourceMasterServer.QueryRegionCode.All,
                SourceMasterServer.QueryFilter.None,
                textMap.Trim(),
                textMod.Trim(),
                SourceMasterQueryCallback);
        }

        private void SourceMasterQueryCallback(object sender, SourceMasterServer.QueryAsyncEventArgs e)
        {
            Action action = async delegate() { await ProcessServers(e); };

            Application.Current.Dispatcher.Invoke(DispatcherPriority.ApplicationIdle, action);
        }

        private async Task ProcessServers(SourceMasterServer.QueryAsyncEventArgs e)
        {
            //todo:areed - fix slow async UI
            try
            {
                await Application.Current.Dispatcher.Invoke(async () =>
                {
                    var newServer = await Task.Run(() => GetServer(e));
                    GetExtendedServerDetail(newServer);
                });
            }
            catch (Exception exception)
            {
                Globals.Current.Logger.Error(exception);
                //Debug.Write(exception);
            }
        }

        private static Server GetServer(SourceMasterServer.QueryAsyncEventArgs e)
        {
            var server = new Server();
            //TODO:figure this out... might need to be a switch statement
            Globals.Current.ViewModel.MapNewServer(e.GameServer, server);
            return server;
        }


        private void GetExtendedServerDetail(Server newServer)
        {
            // if we did not get a GamePort, guess at GamePort based off of QueryPort
            if (newServer.GamePort == 0)
            {
                if (newServer.QueryPort == null && newServer.QueryPort == 0)
                    return;

                newServer.GamePort = (int)(newServer.QueryPort - 1);
            }

            var armaPasswordServersIds = string.Empty;

            if (Globals.Current.ArmaPasswordServers == null)
                Globals.Current.ArmaPasswordServers = new ObservableCollection<Server>();

            // PERF: get IDs for better perf
            foreach (var server in Globals.Current.ArmaPasswordServers)
            {
                if (server.Metadata.ArmaGameType == Globals.Current.ArmaGameType)
                {
                    if (server == Globals.Current.ArmaPasswordServers.Last())
                    {
                        armaPasswordServersIds += server.Id;
                    }
                    else
                    {
                        armaPasswordServersIds += server.Id + ",";
                    }
                }
            }

            var armaFavoriteServersIds = string.Empty;

            if (Globals.Current.ArmaFavoriteServers == null)
                Globals.Current.ArmaFavoriteServers = new ObservableCollection<Server>();

            // PERF: get IDs for better perf
            foreach (var server in Globals.Current.ArmaFavoriteServers)
            {
                if (server.Metadata.ArmaGameType == Globals.Current.ArmaGameType)
                {
                    if (server == Globals.Current.ArmaFavoriteServers.Last())
                    {
                        armaFavoriteServersIds += server.Id;
                    }
                    else
                    {
                        armaFavoriteServersIds += server.Id + ",";
                    }
                }
            }

            var armaNoteServersIds = string.Empty;

            if (Globals.Current.ArmaNotesServers == null)
                Globals.Current.ArmaNotesServers = new ObservableCollection<Server>();

            // PERF: get IDs for better perf
            foreach (var server in Globals.Current.ArmaNotesServers)
            {
                if (server.Metadata.ArmaGameType == Globals.Current.ArmaGameType)
                {
                    if (server == Globals.Current.ArmaNotesServers.Last())
                    {
                        armaNoteServersIds += server.Id;
                    }
                    else
                    {
                        armaNoteServersIds += server.Id + ",";
                    }
                }
            }

            var armaRecentServersIds = string.Empty;

            if (Globals.Current.ArmaRecentServers == null)
                Globals.Current.ArmaRecentServers = new ObservableCollection<Server>();

            // PERF: get IDs for better perf
            foreach (var server in Globals.Current.ArmaRecentServers)
            {
                if (server.Metadata.ArmaGameType == Globals.Current.ArmaGameType)
                {
                    if (server == Globals.Current.ArmaRecentServers.Last())
                    {
                        armaRecentServersIds += server.Id;
                    }
                    else
                    {
                        armaRecentServersIds += server.Id + ",";
                    }
                }
            }

            // mark passworded servers from feed
            if (newServer.Passworded == true)
                newServer.Metadata.EnterPasswordOnConnect = true;

            // mark passworded servers from disk
            if (armaPasswordServersIds.Contains(newServer.Id))
            {
                newServer.Metadata.EnterPasswordOnConnect = true;
                var server =
                    Globals.Current.ArmaPasswordServers.FirstOrDefault(i => i.Id == newServer.Id);
                if (server != null)
                {
                    newServer.Metadata.ServerPassword = server.Metadata.ServerPassword;
                }
            }

            // mark favorites
            if (armaFavoriteServersIds.Contains(newServer.Id))
                newServer.Metadata.IsFavorite = true;

            // mark notes
            if (armaNoteServersIds.Contains(newServer.Id))
            {
                newServer.Metadata.HasNotes = true;
                var server =
                    Globals.Current.ArmaNotesServers.FirstOrDefault(i => i.Id == newServer.Id);
                if (server != null)
                {
                    newServer.Metadata.Notes = server.Metadata.Notes;
                }
            }

            // mark recent dates
            if (armaRecentServersIds.Contains(newServer.Id))
            {
                var server =
                    Globals.Current.ArmaRecentServers.FirstOrDefault(i => i.Id == newServer.Id);
                if (server != null)
                {
                    newServer.Metadata.LastPlayedDate = server.Metadata.LastPlayedDate;
                }
            }

            // update Arma game type metadata
            newServer.Metadata.ArmaGameType = Globals.Current.ArmaGameType;

            //PING
            Action action = async delegate()
            {
                await Task.Run(() =>
                {
                    //PingServerForList(newServer,true,false);
                    var pingededServer = new Server();
                    pingededServer = PingSingleServer(newServer);
                    //TODO:Do I need a ping index?
                    Globals.Current.ViewModel.PingCurrentIndex++;
                    return pingededServer;
                });
            };
            Application.Current.Dispatcher.Invoke(DispatcherPriority.ApplicationIdle, action);

            // FINALLY add serverToUpdate to global model 
            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                // add server
                if (newServer != null)
                    Globals.Current.ArmaServers.Add(newServer);

                //build player servers lists
                foreach (var clientPlayer in Globals.Current.ArmaPlayers)
                {
                    foreach (SteamLib.Player player in newServer.Game.Players)
                    {
                        if (clientPlayer.Player.Name == player.Name)
                        {
                            clientPlayer.IsOnline = true;
                            clientPlayer.PlayerServers.Add(newServer);

                            //save all players
                            Globals.Current.SaveArmaPlayersToDisk();
                        }
                    }
                }
            });

            // cache result
            if (Globals.Current.ArmaServers != null)
            {
                Globals.Current.GetServersProcessSaveServersToDisk(Globals.Current.ArmaServers);
            }
        }

        public Server PingSingleServer(Server server)
        {
            if (Globals.Current.DebugModeTestLocalOnly)
                return server;

            try
            {
                Application.Current.Dispatcher.Invoke((Action)(async () =>
                {
                    var pingReply = await Task.Run(() =>
                    {
                        var pingSender = new Ping();
                        var addresss = IPAddress.Parse(server.Host);
                        var reply = pingSender.Send(addresss);
                        return reply;
                    });

                    if (pingReply != null && pingReply.Status == IPStatus.Success)
                    {
                        server.Metadata.Ping = pingReply.RoundtripTime;
                    }
                    else if (pingReply != null &&
                             pingReply.Status == IPStatus.TimeExceeded)
                    {
                        server.Metadata.Ping = 88888;
                    }
                    else
                    {
                        server.Metadata.Ping = 99999;
                    }
                }));
            }
            catch (Exception e)
            {
                Globals.Current.Logger.Error(e);
            }

            return server;
        }

        public void SourceMasterServer_QueryAsyncCompleted(object sender, EventArgs e)
        {
            //StopSearching();
        }
    }
}
