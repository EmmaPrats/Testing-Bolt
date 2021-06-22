using System;
using System.Collections.Generic;
using System.Threading;
using Photon.Realtime;
using UnityEngine;

namespace DefaultNamespace
{
    public class SimplePhotonClient : MonoBehaviour, IConnectionCallbacks
    {
        private readonly LoadBalancingClient client = new LoadBalancingClient();
        private bool quit;

        private void OnDestroy()
        {
            this.client.Disconnect();
            this.client.RemoveCallbackTarget(this);
        }

        public void StartClient()
        {
            this.client.AddCallbackTarget(this);
            this.client.StateChanged += this.OnStateChange;

            this.client.ConnectUsingSettings(new AppSettings() { AppIdRealtime = "<your appid>", FixedRegion = "eu" });

            Thread t = new Thread(this.Loop);
            t.Start();

            Console.WriteLine("Running until key pressed.");
            Console.ReadKey();
            this.quit = true;
        }

        private void Loop(object state)
        {
            while (!this.quit)
            {
                this.client.Service();
                Thread.Sleep(33);
            }
        }

        private void OnStateChange(ClientState arg1, ClientState arg2)
        {
            Console.WriteLine(arg1 + " -> " + arg2);
        }

        #region IConnectionCallbacks

        public void OnConnectedToMaster()
        {
            Console.WriteLine("OnConnectedToMaster Server: " + this.client.LoadBalancingPeer.ServerIpAddress);
        }

        public void OnConnected()
        {
        }

        public void OnDisconnected(DisconnectCause cause)
        {
        }

        public void OnRegionListReceived(RegionHandler regionHandler)
        {
        }

        public void OnCustomAuthenticationResponse(Dictionary<string, object> data)
        {
        }

        public void OnCustomAuthenticationFailed(string debugMessage)
        {
        }

        #endregion
    }
}