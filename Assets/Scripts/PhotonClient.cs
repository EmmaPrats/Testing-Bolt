using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ExitGames.Client.Photon;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.Assertions;

namespace Biped.Multiplayer.Photon
{
    /// <summary>This class is a facade and interface with Photon's LoadBalancingClient.</summary>
    public class PhotonClient : MonoBehaviour, ILoadBalancingClientFacade, IConnectionCallbacks, IMatchmakingCallbacks, IInRoomCallbacks, IOnEventCallback
    {
        private readonly LoadBalancingClient mClient = new LoadBalancingClient();
        public LoadBalancingClient Client => mClient; //TODO delete this, it should not be exposed.

        private bool mIsConnected;

        private Thread mServiceLoopThread;
        private bool mPlayServiceLoop;

        private void OnDestroy()
        {
            Disconnect();
        }

        public void Connect()
        {
            if (mIsConnected)
                return;

            Application.runInBackground = true;

            mClient.AddCallbackTarget(this);
            mClient.StateChanged += OnStateChange;

            StartPhotonLoadBalancingClient();

            mIsConnected = true;
        }

        public void Disconnect()
        {
            if (!mIsConnected)
                return;

            mClient.RemoveCallbackTarget(this);
            mClient.StateChanged -= OnStateChange;

            mPlayServiceLoop = false;
            mClient.Disconnect();

            mIsConnected = false;
        }

        private void OnQuittingGame(object ev)
        {
            mPlayServiceLoop = false;
            mServiceLoopThread?.Abort();
        }

#region ILoadBalancingClientFacade

        //Cached callbacks:
        private Action<CreateRoomResponse> mOnCreatedRoomCallback;
        private Action<JoinRoomResponse> mOnJoinedRoomCallback;
        private Action<JoinRandomRoomResponse> mOnJoinedRandomRoomCallback;
        private Action mOnLeftRoomCallback;
        private Action mOnConnectedToMasterCallback;

        private bool mIsJoiningRoom;
        private bool mIsJoiningRandomRoom;

        public void CreateRoom(EnterRoomParams enterRoomParams, Action<CreateRoomResponse> callback)
        {
            Assert.IsTrue(mOnCreatedRoomCallback == null);

            mOnCreatedRoomCallback = callback;
            var wasOperationSent = mClient.OpCreateRoom(enterRoomParams);
            if (!wasOperationSent)
            {
                mOnCreatedRoomCallback = null;
                callback?.Invoke(new CreateRoomResponse(EResult.k_EResultFail));
            }
        }

        public void JoinRoom(EnterRoomParams enterRoomParams, Action<JoinRoomResponse> callback)
        {
            Assert.IsTrue(!mIsJoiningRoom && !mIsJoiningRandomRoom &&
                          mOnJoinedRoomCallback == null && mOnJoinedRandomRoomCallback == null);

            mIsJoiningRoom = true;
            mIsJoiningRandomRoom = false;
            mOnJoinedRoomCallback = callback;
            var wasOperationSent = mClient.OpJoinRoom(enterRoomParams);
            if (!wasOperationSent)
            {
                mIsJoiningRoom = false;
                mIsJoiningRandomRoom = false;
                mOnJoinedRoomCallback = null;
                callback?.Invoke(new JoinRoomResponse(EResult.k_EResultFail));
            }
        }

        public void JoinRandomRoom(OpJoinRandomRoomParams joinRandomRoomParams, Action<JoinRandomRoomResponse> callback)
        {
            Assert.IsTrue(!mIsJoiningRoom && !mIsJoiningRandomRoom &&
                          mOnJoinedRoomCallback == null && mOnJoinedRandomRoomCallback == null);

            mIsJoiningRoom = true;
            mIsJoiningRandomRoom = true;
            mOnJoinedRandomRoomCallback = callback;
            var wasOperationSent = mClient.OpJoinRandomRoom(joinRandomRoomParams);
            if (!wasOperationSent)
            {
                mIsJoiningRoom = false;
                mIsJoiningRandomRoom = false;
                mOnJoinedRoomCallback = null;
                callback?.Invoke(new JoinRandomRoomResponse(EResult.k_EResultFail));
            }
        }

        /// <remarks>The callback is called when OnConnectedToMaster instead of OnLeftRoom.</remarks>
        public void LeaveRoom(bool shouldBecomeInactive, Action callback)
        {
            Assert.IsTrue(mOnConnectedToMasterCallback == null);
            mOnConnectedToMasterCallback = callback;
            var wasOperationSent = mClient.OpLeaveRoom(shouldBecomeInactive);
            if (!wasOperationSent)
            {
                mOnConnectedToMasterCallback = null;
                callback?.Invoke();
            }
        }

        public void SetCustomRoomProperties(Hashtable properties)
        {
            mClient.CurrentRoom.SetCustomProperties(properties);
        }

        public Hashtable GetCustomRoomProperties()
        {
            return mClient.CurrentRoom.CustomProperties;
        }

        public bool GetIsLoggedIn()
        {
            switch (mClient.State)
            {
                case ClientState.PeerCreated:
                case ClientState.Authenticating:
                    return false;
                case ClientState.Authenticated:
                case ClientState.JoiningLobby:
                case ClientState.JoinedLobby:
                    return true;
                case ClientState.DisconnectingFromMasterServer:
                case ClientState.ConnectingToGameServer:
                    return false;
                case ClientState.ConnectedToGameServer:
                case ClientState.Joining:
                case ClientState.Joined:
                case ClientState.Leaving:
                    return true;
                case ClientState.DisconnectingFromGameServer:
                case ClientState.ConnectingToMasterServer:
                case ClientState.Disconnecting:
                case ClientState.Disconnected:
                    return false;
                case ClientState.ConnectedToMasterServer:
                    return true;
                case ClientState.ConnectingToNameServer:
                case ClientState.ConnectedToNameServer:
                case ClientState.DisconnectingFromNameServer:
                case ClientState.ConnectWithFallbackProtocol:
                default:
                    return false;
            }
        }

        public bool IsInRoom => mClient.InRoom;

        public ulong GetLocalUID()
        {
            ulong localUID;
            if (ulong.TryParse(mClient.LocalPlayer.UserId, out localUID))
                return localUID;
            return 0;
        }

        public Player GetPlayer(string id)
        {
            if (id == mClient.LocalPlayer.UserId)
                return mClient.LocalPlayer;

            var playersInRoom = mClient.CurrentRoom.Players;
            var player = playersInRoom.FirstOrDefault(keyValuePair => keyValuePair.Value.UserId == id).Value;
            return player;
        }

        public void SendReliable(PhotonPacket photonPacket)
        {
            mClient.OpRaiseEvent(PhotonPacket.CODE, photonPacket.Hashtable, RaiseEventOptions.Default, SendOptions.SendReliable);
        }

        public void SendUnreliable(PhotonPacket photonPacket)
        {
            mClient.OpRaiseEvent(PhotonPacket.CODE, photonPacket.Hashtable, RaiseEventOptions.Default, SendOptions.SendUnreliable);
        }

#endregion

#if UNITY_EDITOR
    #if PHOTON_TEST_PLAYER
        private static ulong mHardcodedLocalUID = 300;
        private static string mHardcodedNickname = "Tester in Editor";
    #else
        private static ulong mHardcodedLocalUID = 100;
        private static string mHardcodedNickname = "Dev in Editor";
    #endif
#else
    #if PHOTON_TEST_PLAYER
        private static ulong mHardcodedLocalUID = 400;
        private static string mHardcodedNickname = "Tester in Build";
    #else
        private static ulong mHardcodedLocalUID = 200;
        private static string mHardcodedNickname = "Dev in Build";
    #endif
#endif

        private void StartPhotonLoadBalancingClient()
        {
            Debug.Log($"#### {name} :: {GetType().Name} :: StartPhotonLoadBalancingClient()");

            mClient.AuthValues = new AuthenticationValues {UserId = mHardcodedLocalUID.ToString(),};
            mClient.ConnectUsingSettings(
                new AppSettings { AppIdRealtime = "3988f29a-92ab-4d5f-9773-f7976fdad848", FixedRegion = "eu" });

            mServiceLoopThread = new Thread(ServiceLoop);
            mPlayServiceLoop = true;
            mServiceLoopThread.Start();
        }

        private void ServiceLoop(object state)
        {
            try
            {
                while (mPlayServiceLoop)
                {
                    mClient.Service();
                    Thread.Sleep(33);
                }
            }
            catch (ThreadAbortException)
            {
                Thread.ResetAbort();
            }
        }

        private void Update()
        {
            lock (mPendingCallbacks)
            {
                while (mPendingCallbacks.Count > 0)
                {
                    mPendingCallbacks.Dequeue().Invoke();
                }
            }
        }

        /// <summary>For logging ClientState info only.</summary>
        /// <remarks>Gets called in same thread as mClient.Service().</remarks>
        private void OnStateChange(ClientState arg1, ClientState arg2) //called in thread
        {
            Debug.Log($"#### {GetType().Name} :: OnStateChange() :: {arg1.ToString()} -> {arg2.ToString()}");
        }

#region Callback queuing

        //The IMatchmakingCallbacks get called in the same thread as the ServiceLoop.
        //However, I want the responses to be executed in the main thread. that is
        //why we cache the callbacks and execute them in Update().

        private readonly Queue<PhotonCallbackBase> mPendingCallbacks = new Queue<PhotonCallbackBase>();

    #region MatchmakingCallback classes

        private abstract class PhotonCallbackBase
        {
            public abstract void Invoke();
        }

        private class PhotonCallback : PhotonCallbackBase
        {
            private readonly Action mAction;

            public PhotonCallback(Action action)
            {
                mAction = action;
            }

            public override void Invoke() => mAction?.Invoke();
        }

        private class PhotonCallback<T> : PhotonCallbackBase
        {
            private readonly Action<T> mAction;
            private readonly T mParam1;

            public PhotonCallback(Action<T> action, T param1)
            {
                mAction = action;
                mParam1 = param1;
            }

            public override void Invoke() => mAction?.Invoke(mParam1);
        }

        private class PhotonCallback<T1, T2> : PhotonCallbackBase
        {
            private readonly Action<T1, T2> mAction;
            private readonly T1 mParam1;
            private readonly T2 mParam2;

            public PhotonCallback(Action<T1, T2> action, T1 param1, T2 param2)
            {
                mAction = action;
                mParam1 = param1;
                mParam2 = param2;
            }

            public override void Invoke() => mAction?.Invoke(mParam1, mParam2);
        }

    #endregion

#endregion

#region IConnectionCallbacks

        void IConnectionCallbacks.OnConnected()
        {
            Debug.Log($"#### {GetType().Name} :: OnConnected()");
            mClient.LocalPlayer.NickName = mHardcodedNickname;
            lock (mPendingCallbacks)
            {
                mPendingCallbacks.Enqueue(new PhotonCallback(DoOnConnected));
            }
        }

        void IConnectionCallbacks.OnConnectedToMaster()
        {
            Debug.Log($"#### {GetType().Name} :: OnConnectedToMaster()");
            lock (mPendingCallbacks)
            {
                mPendingCallbacks.Enqueue(new PhotonCallback(DoOnConnectedToMaster));
            }
        }

        void IConnectionCallbacks.OnDisconnected(DisconnectCause cause)
        {
            Debug.Log($"#### {GetType().Name} :: OnDisconnected(cause: {cause.ToString()})");
            lock (mPendingCallbacks)
            {
                mPendingCallbacks.Enqueue(new PhotonCallback<DisconnectCause>(DoOnDisconnected, cause));
            }
        }

        void IConnectionCallbacks.OnRegionListReceived(RegionHandler regionHandler)
        {
            Debug.Log($"#### {GetType().Name} :: OnRegionListReceived()");
            lock (mPendingCallbacks)
            {
                mPendingCallbacks.Enqueue(new PhotonCallback<RegionHandler>(DoOnRegionListReceived, regionHandler));
            }
        }

        void IConnectionCallbacks.OnCustomAuthenticationResponse(Dictionary<string, object> data)
        {
            Debug.Log($"#### {GetType().Name} :: OnCustomAuthenticationResponse()");
            lock (mPendingCallbacks)
            {
                mPendingCallbacks.Enqueue(new PhotonCallback<Dictionary<string, object>>(DoOnCustomAuthenticationResponse, data));
            }
        }

        void IConnectionCallbacks.OnCustomAuthenticationFailed(string debugMessage)
        {
            Debug.Log($"#### {GetType().Name} :: OnCustomAuthenticationFailed()");
            lock (mPendingCallbacks)
            {
                mPendingCallbacks.Enqueue(new PhotonCallback<string>(DoOnCustomAuthenticationFailed, debugMessage));
            }
        }

#endregion

#region Do On IConnectionCallbacks

        private void DoOnConnected()
        {
            Debug.Log($"#### {name} :: {GetType().Name} :: DoOnConnected()");
        }

        private void DoOnConnectedToMaster()
        {
            Debug.Log($"#### {name} :: {GetType().Name} :: DoOnConnectedToMaster()");
            var callback = mOnConnectedToMasterCallback;
            mOnConnectedToMasterCallback = null;
            callback?.Invoke();
        }

        private void DoOnDisconnected(DisconnectCause cause)
        {
            Debug.Log($"#### {name} :: {GetType().Name} :: DoOnDisconnected()");
        }

        private void DoOnRegionListReceived(RegionHandler regionHandler)
        {
            Debug.Log($"#### {name} :: {GetType().Name} :: DoOnRegionListReceived()");
        }

        private void DoOnCustomAuthenticationResponse(Dictionary<string, object> data)
        {
            Debug.Log($"#### {name} :: {GetType().Name} :: DoOnCustomAuthenticationResponse()");
        }

        private void DoOnCustomAuthenticationFailed(string debugMessage)
        {
            Debug.Log($"#### {name} :: {GetType().Name} :: DoOnCustomAuthenticationFailed()");
        }

#endregion

#region IMatchmakingCallbacks

        /// <remarks>Gets called in same thread as mClient.Service().</remarks>
        void IMatchmakingCallbacks.OnFriendListUpdate(List<global::Photon.Realtime.FriendInfo> friendList)
        {
            Debug.Log($"#### {GetType().Name} :: OnFriendListUpdate()");
            lock (mPendingCallbacks)
            {
                mPendingCallbacks.Enqueue(new PhotonCallback<List<global::Photon.Realtime.FriendInfo>>(DoOnFriendListUpdate, friendList));                
            }
        }

        /// <remarks>Gets called in same thread as mClient.Service().</remarks>
        void IMatchmakingCallbacks.OnCreatedRoom()
        {
            Debug.Log($"#### {GetType().Name} :: OnCreatedRoom()");
            lock (mPendingCallbacks)
            {
                mPendingCallbacks.Enqueue(new PhotonCallback(DoOnCreatedRoom));                
            }
        }

        /// <remarks>Gets called in same thread as mClient.Service().</remarks>
        void IMatchmakingCallbacks.OnCreateRoomFailed(short returnCode, string message)
        {
            Debug.Log($"#### {GetType().Name} :: OnCreateRoomFailed(returnCode: {returnCode}, message: {message})");
            lock (mPendingCallbacks)
            {
                mPendingCallbacks.Enqueue(new PhotonCallback<short, string>(DoOnCreateRoomFailed, returnCode, message));                
            }
        }

        /// <remarks>Gets called in same thread as mClient.Service().</remarks>
        void IMatchmakingCallbacks.OnJoinedRoom()
        {
            var isJoiningRandomRoom = mIsJoiningRandomRoom;
            mIsJoiningRoom = false;
            mIsJoiningRandomRoom = false;
            Debug.Log($"#### {GetType().Name} :: OnJoinedRoom(){(isJoiningRandomRoom ? " (random)" : "")}");
            lock (mPendingCallbacks)
            {
                if (isJoiningRandomRoom)
                    mPendingCallbacks.Enqueue(new PhotonCallback(DoOnJoinedRandomRoom));
                else
                    mPendingCallbacks.Enqueue(new PhotonCallback(DoOnJoinedRoom));
            }
        }

        /// <remarks>Gets called in same thread as mClient.Service().</remarks>
        void IMatchmakingCallbacks.OnJoinRoomFailed(short returnCode, string message)
        {
            mIsJoiningRoom = false;
            mIsJoiningRandomRoom = false;
            Debug.Log($"#### {GetType().Name} :: OnJoinRoomFailed(returnCode: {returnCode}, message: {message})");
            lock (mPendingCallbacks)
            {
                mPendingCallbacks.Enqueue(new PhotonCallback<short, string>(DoOnJoinRoomFailed, returnCode, message));                
            }
        }

        /// <remarks>Gets called in same thread as mClient.Service().</remarks>
        void IMatchmakingCallbacks.OnJoinRandomFailed(short returnCode, string message)
        {
            mIsJoiningRoom = false;
            mIsJoiningRandomRoom = false;
            Debug.Log($"#### {GetType().Name} :: OnJoinRandomFailed(returnCode: {returnCode}, message: {message})");
            lock (mPendingCallbacks)
            {
                mPendingCallbacks.Enqueue(new PhotonCallback<short, string>(DoOnJoinRandomFailed, returnCode, message));                
            }
        }

        /// <remarks>Gets called in same thread as mClient.Service().</remarks>
        void IMatchmakingCallbacks.OnLeftRoom()
        {
            Debug.Log($"#### {GetType().Name} :: OnLeftRoom()");
            lock (mPendingCallbacks)
            {
                mPendingCallbacks.Enqueue(new PhotonCallback(DoOnLeftRoom));                
            }
        }

#endregion

#region Do On IMatchmakingCallbacks

        private void DoOnFriendListUpdate(List<global::Photon.Realtime.FriendInfo> friendList)
        {
            Debug.Log($"#### {name} :: {GetType().Name} :: DoOnFriendListUpdate()");
        }

        private void DoOnCreatedRoom()
        {
            Debug.Log($"#### {name} :: {GetType().Name} :: DoOnCreatedRoom()");
            var callback = mOnCreatedRoomCallback;
            mOnCreatedRoomCallback = null;
            callback?.Invoke(new CreateRoomResponse(EResult.k_EResultOK, mClient.CurrentRoom));
        }

        private void DoOnCreateRoomFailed(short returnCode, string message)
        {
            Debug.Log($"#### {name} :: {GetType().Name} :: DoOnCreateRoomFailed()");
            var callback = mOnCreatedRoomCallback;
            mOnCreatedRoomCallback = null;
            callback?.Invoke(new CreateRoomResponse(EResult.k_EResultFail));
        }

        private void DoOnJoinedRoom()
        {
            Debug.Log($"#### {name} :: {GetType().Name} :: DoOnJoinedRoom()");
            var callback = mOnJoinedRoomCallback;
            mOnJoinedRoomCallback = null;
            mOnJoinedRandomRoomCallback = null;
            callback?.Invoke(new JoinRoomResponse(EResult.k_EResultOK, mClient.CurrentRoom));
        }

        private void DoOnJoinRoomFailed(short returnCode, string message)
        {
            Debug.Log($"#### {name} :: {GetType().Name} :: DoOnJoinRoomFailed()");
            var callback = mOnJoinedRoomCallback;
            mOnJoinedRoomCallback = null;
            mOnJoinedRandomRoomCallback = null;
            callback?.Invoke(new JoinRoomResponse(EResult.k_EResultFail));
        }

        private void DoOnJoinedRandomRoom()
        {
            Debug.Log($"#### {name} :: {GetType().Name} :: DoOnJoinedRandomRoom()");
            var callback = mOnJoinedRandomRoomCallback;
            mOnJoinedRoomCallback = null;
            mOnJoinedRandomRoomCallback = null;
            callback?.Invoke(new JoinRandomRoomResponse(EResult.k_EResultOK, mClient.CurrentRoom));
        }

        private void DoOnJoinRandomFailed(short returnCode, string message)
        {
            Debug.Log($"#### {name} :: {GetType().Name} :: DoOnJoinRandomFailed()");
            var callback = mOnJoinedRandomRoomCallback;
            mOnJoinedRoomCallback = null;
            mOnJoinedRandomRoomCallback = null;
            callback?.Invoke(new JoinRandomRoomResponse(EResult.k_EResultFail));
        }

        private void DoOnLeftRoom()
        {
            Debug.Log($"#### {name} :: {GetType().Name} :: DoOnLeftRoom()");
            var callback = mOnLeftRoomCallback;
            mOnLeftRoomCallback = null;
            callback?.Invoke();
        }

#endregion

#region IInRoomCallbacks

        public Action<Player, Room> OtherPlayerEnteredRoom;

        void IInRoomCallbacks.OnPlayerEnteredRoom(Player newPlayer)
        {
            Debug.Log($"#### {GetType().Name} :: OnPlayerEnteredRoom(newPlayer: {newPlayer.UserId})");
            lock (mPendingCallbacks)
            {
                mPendingCallbacks.Enqueue(new PhotonCallback<Player>(DoOnOtherPlayerEnteredRoom, newPlayer));
            }
        }

        private void DoOnOtherPlayerEnteredRoom(Player newPlayer)
        {
            Debug.Log($"#### {name} :: {GetType().Name} :: DoOnOtherPlayerEnteredRoom(newPlayer: {newPlayer.UserId})");
            OtherPlayerEnteredRoom?.Invoke(newPlayer, mClient.CurrentRoom);
        }

        void IInRoomCallbacks.OnPlayerLeftRoom(Player otherPlayer)
        {
            Debug.Log($"#### {GetType().Name} :: OnPlayerLeftRoom(newPlayer: {otherPlayer.UserId})");
            lock (mPendingCallbacks)
            {
                mPendingCallbacks.Enqueue(new PhotonCallback<Player>(DoOnOtherPlayerLeftRoom, otherPlayer));
            }
        }

        public Action<Player, Room> OtherPlayerLeftRoom;

        private void DoOnOtherPlayerLeftRoom(Player otherPlayer)
        {
            Debug.Log($"#### {name} :: {GetType().Name} :: DoOnOtherPlayerLeftRoom(player: {otherPlayer.UserId})");
            OtherPlayerLeftRoom?.Invoke(otherPlayer, mClient.CurrentRoom);
        }

        void IInRoomCallbacks.OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
        {
            Debug.Log($"#### {GetType().Name} :: OnRoomPropertiesUpdate(propertiesThatChanged: [\"{string.Join("\", \"", propertiesThatChanged.Keys.ToArray().Cast<string>())}\"])");
        }

        void IInRoomCallbacks.OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            Debug.Log($"#### {GetType().Name} :: OnPlayerPropertiesUpdate(targetPlayer: {targetPlayer.UserId}, changedProps: [\"{string.Join("\", \"", changedProps.Keys.ToArray().Cast<string>())}\"])");
            lock (mPendingCallbacks)
            {
                mPendingCallbacks.Enqueue(new PhotonCallback(DoOnPlayerPropertiesUpdate));
            }
        }

        public Action PlayerPropertiesUpdated;

        private void DoOnPlayerPropertiesUpdate()
        {
            Debug.Log($"#### {name} :: {GetType().Name} :: DoOnPlayerPropertiesUpdate()");
            PlayerPropertiesUpdated?.Invoke();
        }

        void IInRoomCallbacks.OnMasterClientSwitched(Player newMasterClient)
        {
            Debug.Log($"#### {GetType().Name} :: OnMasterClientSwitched(newMasterClient: {newMasterClient.UserId})");
        }

#endregion

#region IOnEventCallback

        public readonly ConcurrentQueue<PhotonPacket> PacketsReceivedPendingProcess = new ConcurrentQueue<PhotonPacket>();

        void IOnEventCallback.OnEvent(EventData photonEvent)
        {
#if CONNECTION_DEBUG
            Debug.Log($"#### {GetType().Name} :: OnEvent(photonEvent: {photonEvent.Code} {GetCodeText(photonEvent.Code)})");
#endif
            if (photonEvent.Code != PhotonPacket.CODE)
                return;

            var hashtable = photonEvent.CustomData as Hashtable;
            if (hashtable != null)
            {
                var photonPacket = new PhotonPacket(hashtable);
#if CONNECTION_DEBUG
                MultiplayerDebug.Instance.LogReceiving($"\n\n== RECEIVED PHOTON EVENT ==   Code: {photonEvent.Code}    TimeCounter.TickID: {TimeCounter.TickID}\n{string.Join("\n", photonPacket.ToStringLineByLine("    ", true))}");
#endif
                PacketsReceivedPendingProcess.Enqueue(photonPacket);
            }
#if CONNECTION_DEBUG
            else
                MultiplayerDebug.Instance.LogReceiving($"\n\n== RECEIVED PHOTON EVENT ==   Code: {photonEvent.Code}    TimeCounter.TickID: {TimeCounter.TickID}\n    photonEvent.CustomData is NOT Hashtable");
#endif
        }

        private static string GetCodeText(byte code)
        {
            switch (code)
            {
                case EventCode.Join:
                    return "Join";
                case EventCode.Leave:
                    return "Leave";
                case EventCode.PropertiesChanged:
                    return "PropertiesChanged";
                case 252:
                    return "Disconnect (obsolete)";
                case EventCode.ErrorInfo:
                    return "ErrorInfo";
                case EventCode.CacheSliceChanged:
                    return "CacheSliceChanged";
                case EventCode.GameList:
                    return "GameList";
                case EventCode.GameListUpdate:
                    return "GameListUpdate";
                case EventCode.QueueState:
                    return "QueueState";
                case EventCode.Match:
                    return "Match";
                case EventCode.AppStats:
                    return "AppStats";
                case EventCode.LobbyStats:
                    return "LobbyStats";
                case EventCode.AuthEvent:
                    return "AuthEvent";
                default:
                    return "";
            }
        }

#endregion

    }
}
