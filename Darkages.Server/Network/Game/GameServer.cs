﻿///************************************************************************
//Project Lorule: A Dark Ages Client (http://darkages.creatorlink.net/index/)
//Copyright(C) 2018 TrippyInc Pty Ltd
//
//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.
//
//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
//GNU General Public License for more details.
//
//You should have received a copy of the GNU General Public License
//along with this program.If not, see<http://www.gnu.org/licenses/>.
//*************************************************************************/


using Darkages.Network.Game.Components;
using Darkages.Network.Object;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Darkages.Network.Game
{
    public partial class GameServer
    {
        public Dictionary<Type, GameServerComponent> Components;

        private readonly TimeSpan _heavyUpdateSpan;

        private DateTime _lastHeavyUpdate = DateTime.UtcNow;

        public ObjectService ObjectFactory = new ObjectService();

        public GameServer(int capacity) : base(capacity)
        {
            _heavyUpdateSpan = TimeSpan.FromSeconds(1.0 / 30);

            InitializeGameServer();
        }

        private void AutoSave(GameClient client)
        {
            if ((DateTime.UtcNow - client.LastSave).TotalSeconds > ServerContextBase.GlobalConfig.SaveRate)
                client.Save();
        }

        private async void MainServerLoop()
        {
            _lastHeavyUpdate = DateTime.UtcNow;

            while (ServerContextBase.Running)
            {
                var elapsedTime = DateTime.UtcNow - _lastHeavyUpdate;

                try
                {
                    await UpdateClients(elapsedTime);
                    await UpdateComponents(elapsedTime);
                    await UpdateAreas(elapsedTime);
                }
                catch (Exception e)
                {
                    ServerContextBase.Report(e);
                }

                _lastHeavyUpdate = DateTime.UtcNow;
                await Task.Delay(_heavyUpdateSpan);
            }
        }

        public void InitializeGameServer()
        {
            InitComponentCache();
        }

        private void InitComponentCache()
        {
            Components = new Dictionary<Type, GameServerComponent>
            {
                [typeof(Save)] = new Save(this),
                [typeof(ObjectComponent)] = new ObjectComponent(this),
                [typeof(ClientTickComponent)] = new ClientTickComponent(this),
                [typeof(MonolithComponent)] = new MonolithComponent(this),
                [typeof(DaytimeComponent)] = new DaytimeComponent(this),
                [typeof(MundaneComponent)] = new MundaneComponent(this),
                [typeof(MessageComponent)] = new MessageComponent(this),
                [typeof(PingComponent)] = new PingComponent(this),
            };
        }


        private async Task UpdateComponents(TimeSpan elapsedTime)
        {
            try
            {
                foreach (var component in Components.Values)
                    component.Update(elapsedTime);
            }
            catch (Exception e)
            {
                await Task.FromException(e);
                ServerContextBase.Report(e);
                throw;
            }

            await Task.CompletedTask;
        }

        private async Task UpdateAreas(TimeSpan elapsedTime)
        {
            var values = ServerContextBase.GlobalMapCache.Select(i => i.Value).ToArray();

            try
            {
                foreach (var area in values)
                    area.Update(elapsedTime);
            }
            catch (Exception e)
            {
                await Task.FromException(e);
                ServerContextBase.Report(e);
                throw;
            }

            await Task.CompletedTask;
        }

        public async Task UpdateClients(TimeSpan elapsedTime)
        {
            foreach (var client in Clients)
            {
                if (client?.Aisling == null)
                    continue;

                if (!client.IsWarping && !client.InMapTransition && !client.MapOpen)
                {
                    await Pulse(elapsedTime, client);
                }
                else if (client.IsWarping && !client.InMapTransition)
                {
                    if (client.CanSendLocation && !client.IsRefreshing &&
                        client.Aisling.CurrentMapId == ServerContextBase.GlobalConfig.PVPMap)
                        client.SendLocation();
                }
                else if (!client.MapOpen && !client.IsWarping && client.InMapTransition)
                {
                    client.MapOpen = false;

                    if (client.InMapTransition && !client.MapOpen)
                        if (DateTime.UtcNow - client.DateMapOpened > TimeSpan.FromSeconds(0.2))
                        {
                            client.MapOpen = true;
                            client.InMapTransition = false;
                        }
                }

                if (!client.MapOpen) continue;
                if (!client.IsWarping && !client.IsRefreshing)
                    await Pulse(elapsedTime, client);
            }
        }

        private async Task Pulse(TimeSpan elapsedTime, GameClient client)
        {
            await client.Update(elapsedTime);

            ObjectComponent.UpdateClientObjects(client.Aisling);
        }

        public override void ClientDisconnected(GameClient client)
        {
            if (client?.Aisling == null)
                return;

            try
            {
                if ((DateTime.UtcNow - client.LastSave).TotalSeconds > 2) client.Save();

                client.Aisling.LoggedIn = false;
                client.Aisling.Remove(true);
            }
            catch (Exception e)
            {
                ServerContextBase.Report(e);
            }
            finally
            {
                base.ClientDisconnected(client);
            }
        }

        public override void Start(int port)
        {
            base.Start(port);

            var serverThread = new Thread(MainServerLoop) {Priority = ThreadPriority.Highest};

            try
            {
                serverThread.Start();
                ServerContextBase.Running = true;
            }
            catch (ThreadAbortException)
            {
                ServerContextBase.Running = false;
            }
        }
    }
}