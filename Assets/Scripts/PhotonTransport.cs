using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Testing;
using TestingPhoton;
using UnityEngine;

namespace Biped.Multiplayer.Photon
{
    public class PhotonTransport : MonoBehaviour, INetTransport, INotifyReceivingPacketsOfLength4
    {
        [SerializeField] private PhotonClient m_PhotonClient;

        private readonly TaskCompletionSource<bool> mIsInitialized = new TaskCompletionSource<bool>();
        public Task<bool> IsInitialized => mIsInitialized.Task;

        public bool IsServer => m_PhotonClient.Client.LocalPlayer.IsMasterClient;

#region Unity events

        public void Update()
        {
            ProcessNetworkPackages();
        }

        public void FixedUpdate()
        {
            ProcessNetworkPackages();
        }

        private void OnDestroy()
        {
            TearDown();
        }

#endregion

#region Initialization, Set Up, Tear Down

        private bool mIsSetUp;

        private void SetUp()
        {
            if (mIsSetUp)
                return;

            mPacketReadThread = new Thread(StreamPacketReadLoop);
            mPacketReadThread.IsBackground = true;
            mPacketReadThread.Start();

            mIsSetUp = true;
            mIsInitialized.SetResult(mIsSetUp);
        }

        private void TearDown()
        {
            if (!mIsSetUp)
                return;

            mPlayPacketReadThread = false;
            mPacketReadThread?.Abort();

            mIsSetUp = false;
        }

        private void OnQuitGame(object ev)
        {
           TearDown();
        }

#endregion

#region NetTransport

        public void Init()
        {
            Debug.Log($"#### {name} :: {GetType().Name} :: Init()");
            SetUp();
        }

        private bool mIsServerStarted = false;

        public void StartServer()
        {
            Debug.Log($"#### {name} :: {GetType().Name} :: StartServer()");
            Application.runInBackground = true;
            mIsServerStarted = true;
        }

        public void StopServer()
        {
            Debug.Log($"#### {name} :: {GetType().Name} :: StopServer()");
            mIsServerStarted = false;
        }

        public void SendReliable(byte[] data, int len)
        {
            var photonPacket = new PhotonPacket(data, (uint) len, m_PhotonClient.GetLocalUID());
            m_PhotonClient.SendReliable(photonPacket);
        }

        public void SendUnreliable(byte[] data, int len)
        {
            var photonPacket = new PhotonPacket(data, (uint) len, m_PhotonClient.GetLocalUID());
            m_PhotonClient.SendUnreliable(photonPacket);
        }

#endregion

#region Packet reading

        private Thread mPacketReadThread;
        private bool mPlayPacketReadThread = true;

        private void StreamPacketReadLoop()
        {
            try
            {
                while (mPlayPacketReadThread)
                {
                    Thread.Sleep(2);
                    StreamPacketReadBackgroundThread();
                }
            }
            catch (ThreadAbortException)
            {
                Thread.ResetAbort();
            }
        }

        //Is this a static field instead of a function variable because of multi-threading?
        private static List<byte[]> mFrameProcList = new List<byte[]>();

        private void StreamPacketReadBackgroundThread()
        {
            mFrameProcList.Clear();

            PhotonLobby.ReadP2PPackageMultiThread(m_PhotonClient, mFrameProcList);
            foreach (var networkEvent in mFrameProcList)
                StreamMessageRead(networkEvent);

            mFrameProcList.Clear();
        }

        private Queue<byte[]> mMessages = new Queue<byte[]>();

        /// <remarks>Since we are multi-threading, this method will be called in the background thread.</remarks>
        private void StreamMessageRead(byte[] networkEvent)
        {
            if (networkEvent == null || networkEvent.Length == 0)
                return;

            lock (mMessages)
            {
                mMessages.Enqueue(networkEvent);
            }
        }

        public Action<byte[]> ReceivedPacketOfLength4 { get; set; }

        private Queue<byte[]> mMessagesToProcess = new Queue<byte[]>();

        private void ProcessNetworkPackages()
        {
            lock (mMessagesToProcess)
            {
                while (mMessages.Count > 0)
                    mMessagesToProcess.Enqueue(mMessages.Dequeue());
            }

            foreach (var message in mMessagesToProcess)
                if (message.Length == 4)
                    ReceivedPacketOfLength4?.Invoke(message);
        }

#endregion
    }
}
