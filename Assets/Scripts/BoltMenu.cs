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

        public override void BoltStartBegin()
        {
            MyDebug.Log("BoltStartBegin()");

            BoltNetwork.RegisterTokenClass<PhotonRoomProperties>(); //Not sure it's necessary...
            BoltNetwork.RegisterTokenClass<BoltPlayer>();
            BoltNetwork.RegisterTokenClass<AcceptToken>();
        }

        public override void BoltStartDone()
        {
            MyDebug.Log("BoltStartDone()");

            if (BoltNetwork.IsServer)
            {
                var matchName = Guid.NewGuid().ToString();
                
                var roomProperties = new PhotonRoomProperties();
                roomProperties.AddRoomProperty("SET_BEFORE_CREATION_1", "Server BoltStartDone()");
                roomProperties.AddRoomProperty("SET_BEFORE_CREATION_2", "Server BoltStartDone()");

                BoltMatchmaking.CreateSession(sessionID: matchName, roomProperties);
            }
        }

#if UNITY_EDITOR
        private static ulong mHardcodedLocalUID = 100;
        private static string mHardcodedNickname = "Dev in Editor";
#else
        private static ulong mHardcodedLocalUID = 200;
        private static string mHardcodedNickname = "Dev in Build";
#endif

        public override void SessionListUpdated(Map<Guid, UdpSession> sessionList)
        {
            MyDebug.Log("SessionListUpdated() :: SessionList:\n" +
                        string.Join("\n", sessionList.Select(pair => "    " + pair.Value.Id)));

            foreach (var guidSession in sessionList)
            {
                var session = guidSession.Value;
                if (session.Source == UdpSessionSource.Photon)
                {
                    var player = new BoltPlayer(mHardcodedLocalUID, mHardcodedNickname);
                    MyDebug.Log($"Joining session {session.Id} with properties:\n" +
                                ((PhotonSession) session).Properties.ToStringContentsLineByLine(indentCount: 1));
                    BoltMatchmaking.JoinSession(session, player);
                    
                    //IF YOU JOIN SESSION BY NAME, session FROM SessionConnected(UdpSession session) will have ID == 0,
                    //AND Properties WILL BE NULL WTF!?!?
                    //BoltMatchmaking.JoinSession(session.HostName, player);

                    break;
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

        //Is only called if acceptMode == manual
        public override void ConnectRequest(UdpEndPoint endpoint, IProtocolToken token)
        {
            MyDebug.Log($"ConnectRequest(endpoint: {endpoint.SteamId}, token: {token?.GetType().Name})" +
                        $"token: {token?.ToString() ?? "null"}\n");

            var player = (BoltPlayer) token;
            if (player == null)
                return;

            //Properties set here are updated only some times. It seems that only if it's slow enough (add breakpoints).
            if (BoltMatchmaking.CurrentSession is PhotonSession)
            {
                var roomProperties = new PhotonRoomProperties();
                roomProperties.AddRoomProperty("CLIENT_ID", player.ID + " Server ConnectRequest()");
                roomProperties.AddRoomProperty("CLIENT_NICKNAME", player.Nickname + " Server ConnectRequest()");
                BoltMatchmaking.UpdateSession(roomProperties);
            }

            var acceptToken = new AcceptToken(player, ((PhotonSession) BoltMatchmaking.CurrentSession).Properties);
            Debug.Log($"#### {GetType().Name} :: ConnectRequest() :: Accepting with token:\n{acceptToken}");
            BoltNetwork.Accept(endpoint, acceptToken);
        }

        public override void Connected(BoltConnection connection) //Not called by the server when they create the room.
        {
            MyDebug.Log("Connected(connection: " +
                        (connection == null
                            ? "NULL)"
                            : $"{connection.ConnectionId})" +
                              $"\nconnection.ConnectToken: {connection.ConnectToken?.ToString() ?? "NULL"}" +
                              $"\nconnection.AcceptToken: {connection.AcceptToken?.ToString() ?? "NULL"}"));

            connection?.SetStreamBandwidth(1024 * 100);

            //Properties set directly to photonSession.Properties are visible only in local machine.

            //Reliably visible on both Server and Client:
            if (BoltNetwork.IsServer && BoltMatchmaking.CurrentSession is PhotonSession)
            {
                var roomProperties = new PhotonRoomProperties();
                roomProperties.AddRoomProperty("ADDED_AFTER_CLIENT_JOINED_CONNECTED_1", "Server Connected()");
                BoltMatchmaking.UpdateSession(roomProperties);
                roomProperties = new PhotonRoomProperties();
                roomProperties.AddRoomProperty("ADDED_AFTER_CLIENT_JOINED_CONNECTED_2", "Server Connected()"); //This is the only one that is visible!
                BoltMatchmaking.UpdateSession(roomProperties);
            }
        }

        //Called only on Client.
        public override void SessionConnected(UdpSession session, IProtocolToken token)
        {
            MyDebug.Log($"SessionConnected(session: {session?.Id}, connectionToken: {token?.GetType().Name})" +
                        $"\nConnection Token:\n{token}" +
                        $"\nphotonSession.Properties:" +
                        (((PhotonSession) session)?.Properties == null
                            ? " NULL"
                            : $"\n{((PhotonSession) session).Properties.ToStringContentsLineByLine(indentCount: 1)}"));
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
