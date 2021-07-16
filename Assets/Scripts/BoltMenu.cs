using System;
using System.Linq;
using Photon.Bolt;
using Photon.Bolt.Matchmaking;
using Photon.Bolt.Utils;
using Testing;
using UdpKit;
using UdpKit.Platform.Photon;
using UnityEngine;
using UnityEngine.UI;

namespace TestingBolt
{
    public class BoltMenu : GlobalEventListener, INotifyReceivingPacketsOfLength4
    {
        [SerializeField] private Button m_CreateServerButton;
        [SerializeField] private Button m_JoinServerButton;

        public Action<byte[]> ReceivedPacketOfLength4 { get; set; }

        private void Start()
        {
            m_CreateServerButton.onClick.AddListener(OnCreateServerButtonClicked);
            m_JoinServerButton.onClick.AddListener(OnJoinServerButtonClicked);
        }

        private void OnDestroy()
        {
            m_CreateServerButton.onClick.RemoveListener(OnCreateServerButtonClicked);
            m_JoinServerButton.onClick.RemoveListener(OnJoinServerButtonClicked);
        }

        private void OnCreateServerButtonClicked()
        {
            BoltLauncher.StartServer();
        }

        private void OnJoinServerButtonClicked()
        {
            BoltLauncher.StartClient();
        }

#region GlobalEventListener

        public override void BoltStartDone()
        {
            MyDebug.Log("BoltStartDone()");

            if (BoltNetwork.IsServer)
            {
                var matchName = Guid.NewGuid().ToString();
                
                var roomProperties = new PhotonRoomProperties();
                roomProperties.AddRoomProperty("PROPERTY_SET_BEFORE_CREATION_1", "qwer");
                roomProperties.AddRoomProperty("PROPERTY_SET_BEFORE_CREATION_2", "asdf");

                BoltMatchmaking.CreateSession(sessionID: matchName, roomProperties);
            }
        }

        public override void SessionListUpdated(Map<Guid, UdpSession> sessionList)
        {
            MyDebug.Log("SessionListUpdated() :: SessionList:\n" +
                        string.Join("\n", sessionList.Select(pair => "    " + pair.Value.Id)));

            foreach (var guidSession in sessionList)
            {
                var session = guidSession.Value;
                if (session.Source == UdpSessionSource.Photon)
                {
                    MyDebug.Log($"Joining session {session.Id} with properties:\n" +
                                ((PhotonSession) session).Properties.ToStringContentsLineByLine(indentCount: 1));
                    BoltMatchmaking.JoinSession(session);
                }
            }
        }

        public override void SessionCreatedOrUpdated(UdpSession session)
        {
            MyDebug.Log($"SessionCreatedOrUpdated(session: {session.Id})" +
                      "\nsession.Properties:\n" +
                      ((session as PhotonSession)?.Properties?.ToStringContentsLineByLine(indentCount: 1) ?? "NULL") +
                      "\nBoltMatchmaking.CurrentSession.Properties:\n" +
                      (((PhotonSession) BoltMatchmaking.CurrentSession).Properties?.ToStringContentsLineByLine(indentCount: 1) ?? "NULL") +
                      $"\nHostObject: {session.HostObject?.GetType().Name ?? "NULL"}");
        }

        public override void Connected(BoltConnection connection)
        {
            MyDebug.Log($"Connected(connection: {connection.ConnectionId})");

            connection.SetStreamBandwidth(1024 * 100);
        }

        public override void StreamDataStarted(BoltConnection connection, UdpChannelName channel, ulong streamID)
        {
            var message = $"Connection {connection} is transfering data on channel {channel} :: Transfer {streamID}...";
            BoltLog.Warn(message);
        }

        public override void StreamDataProgress(BoltConnection connection, UdpChannelName channel, ulong streamID, float progress)
        {
            var message = $"[{(int)(progress * 100)}%] Connection {connection} is transfering data on channel {channel} :: Transfer ID {streamID}";
            BoltLog.Info(message);
        }

        public override void StreamDataAborted(BoltConnection connection, UdpChannelName channel, ulong streamID)
        {
            var message = $"Stream {streamID} on channel {channel} from connection {connection} has been aborted.";
            BoltLog.Error(message);
        }

        public override void StreamDataReceived(BoltConnection connection, UdpStreamData data)
        {
            var message = $"Received data from {connection} on channel {data.Channel}: {data.Data.Length} bytes";
            BoltLog.Info(message);

            if (data.Data.Length == 4)
                ReceivedPacketOfLength4?.Invoke(data.Data);
        }

#endregion
    }
}
