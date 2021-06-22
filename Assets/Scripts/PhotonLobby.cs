using System;
using System.Collections.Generic;
using Biped.Multiplayer.Photon;
using ExitGames.Client.Photon;
using Photon.Realtime;
using UnityEngine;

namespace TestingPhoton
{
    public class PhotonLobby : MonoBehaviour
    {
        [SerializeField] private PhotonClient m_PhotonClient;

        private const string ROOM_SEQ_LOBBY = "C0";
        private const LobbyType LOBBY_TYPE = LobbyType.SqlLobby;

        private bool mIsInitialized;
        private static bool mIsRunning;
        private static object mStopObject = new object();

        private string mCachedRoomName;

#region Initialization, Set Up, Tear Down

        public void SetUp()
        {
            if (mIsInitialized)
                return;

            m_PhotonClient.PlayerPropertiesUpdated += OnPlayerPropertiesUpdated;
            m_PhotonClient.OtherPlayerEnteredRoom += OnPlayerEnteredRoom;
            m_PhotonClient.OtherPlayerLeftRoom += OnOtherPlayerLeftRoom;

            m_PhotonClient.Connect();

            mIsInitialized = true;
            mIsRunning = true; //this could go inside a lock
        }

        private void TearDown()
        {
            if (!mIsInitialized)
                return;

            m_PhotonClient.PlayerPropertiesUpdated -= OnPlayerPropertiesUpdated;
            m_PhotonClient.OtherPlayerEnteredRoom -= OnPlayerEnteredRoom;
            m_PhotonClient.OtherPlayerLeftRoom -= OnOtherPlayerLeftRoom;
            int currentRoomId;
            if (!string.IsNullOrEmpty(mCachedRoomName) &&
                int.TryParse(mCachedRoomName, out currentRoomId) && currentRoomId > 0)
                LeaveRoom();

            lock (mStopObject)
            {
                mIsRunning = false;
                m_PhotonClient.Disconnect(); //this could go outside of the lock.
            }
            mIsInitialized = false; //this is my own addition.
        }

        private void OnDestroy()
        {
            TearDown();
        }

#endregion

#region Create Room

        private bool mIsCreatingRoom;

        public void CreateRoom(string roomSeq)
        {
            Debug.Log($"#### {GetType().Name} :: CreateRoom()");

            if (mIsCreatingRoom || m_PhotonClient.IsInRoom)
            {
                Debug.LogWarning("Is already in room or creating one.");
                return;
            }

            //var roomSeq = LobbySystem.GenerateNextSeq();
            var isRoomFriendOnly = false;
            var enterRoomParams = new EnterRoomParams
            {
                RoomName = "99998888", //Manually setting a name that can be parsed into ulong roomID.
                Lobby = new TypedLobby(
                    name: roomSeq, //Will be used for searching the room.
                    type: LOBBY_TYPE),
                RoomOptions = new RoomOptions
                {
                    MaxPlayers = (byte) 2,
                    PublishUserId = true,
                    CustomRoomProperties = new Hashtable
                    {
                        {LobbySystem.ROOM_SEQ, roomSeq},
                        {ROOM_SEQ_LOBBY, roomSeq},
                        {LobbySystem.GAME_VERSION, "0.0.1"},
                        {LobbySystem.P2P, 2.ToString()},
                        {LobbySystem.OWNER, m_PhotonClient.GetLocalUID().ToString()},
                        {LobbySystem.FRIEND_ONLY, isRoomFriendOnly ? "1" : "0"},
                    },
                    CustomRoomPropertiesForLobby = new []{ROOM_SEQ_LOBBY}
                },
            };
            mIsCreatingRoom = true;
            m_PhotonClient.CreateRoom(enterRoomParams, OnCreateRoomResponse);
        }

        public Action<LobbyRoomCreateEvent> CreateRoomResponse;

        private void OnCreateRoomResponse(CreateRoomResponse response)
        {
            Debug.Log($"#### {GetType().Name} :: OnCreateRoomResponse({response.ResultCode.ToString()})");
            mIsCreatingRoom = false;
            LobbyRoomCreateEvent lobbyRoomCreateEvent;
            if (response.ResultCode == EResult.k_EResultOK)
            {
                mCachedRoomName = response.Room.Name;
                lobbyRoomCreateEvent = new LobbyRoomCreateEvent
                {
                    response = (CreateResponse) response.ResultCode,
                    RoomID = ulong.Parse(response.Room.Name),
                    Seq = (string) response.Room.CustomProperties[LobbySystem.ROOM_SEQ],
                    RoomOwner = ulong.Parse((string) response.Room.CustomProperties[LobbySystem.OWNER])
                };
            }
            else
            {
                mCachedRoomName = "";
                lobbyRoomCreateEvent = new LobbyRoomCreateEvent {response = (CreateResponse) response.ResultCode};
            }
            CreateRoomResponse?.Invoke(lobbyRoomCreateEvent);
        }

#endregion

#region Join Room

        public void JoinRoom(ulong roomID, bool isInvitee)
        {
            Debug.Log($"#### {GetType().Name} :: JoinRoom(roomId: {roomID}, isInvitee: {isInvitee})");

            var enterRoomParams = new EnterRoomParams
            {
                RoomName = roomID.ToString(),
                RoomOptions = new RoomOptions {MaxPlayers = (byte) 2, PublishUserId = true},
            };
            m_PhotonClient.JoinRoom(enterRoomParams, OnJoinRoomResponse);
        }

        public Action<LobbyRoomEnterEvent> JoinRoomResponse;

        private void OnJoinRoomResponse(JoinRoomResponse response)
        {
            Debug.Log($"#### {GetType().Name} :: OnJoinRoomResponse({response.ResultCode.ToString()})");
            LobbyRoomEnterEvent lobbyRoomEnterEvent;
            if (response.ResultCode == EResult.k_EResultOK)
            {
                mCachedRoomName = response.Room.Name;
                lobbyRoomEnterEvent = new LobbyRoomEnterEvent
                {
                    response = (EnterResponse) response.ResultCode,
                    RoomID = ulong.Parse(response.Room.Name),
                    TriggerID = LocalUID(), //Mine?
                    roomSeq = (string) response.Room.CustomProperties[LobbySystem.ROOM_SEQ],
                    OwnerID = ulong.Parse((string) response.Room.CustomProperties[LobbySystem.OWNER])
                };
            }
            else
            {
                mCachedRoomName = "";
                lobbyRoomEnterEvent = new LobbyRoomEnterEvent {response = (EnterResponse) response.ResultCode};
            }
            JoinRoomResponse?.Invoke(lobbyRoomEnterEvent);
        }

#endregion

        public void LeaveRoom()
        {
            Debug.Log($"#### {GetType().Name} :: LeaveRoom()");
            mCachedRoomName = "";
            m_PhotonClient.LeaveRoom(true, null);
        }

        public ulong LocalUID()
        {
//            Debug.Log($"#### {GetType().Name} :: LocalUID() :: {mCachedLocalUID} {PhotonClient.Instance.Client.UserId}");
            return m_PhotonClient.GetLocalUID();
        }

        public class FriendInfo
        {
            public ulong friendID;
            public string friendName;
            public int avatarHanlde;
        }

        public FriendInfo GetFriendInfo(ulong friend)
        {
//            Debug.Log($"#### {GetType().Name} :: GetFriendInfo(friendId: {friend})");
            var player = m_PhotonClient.GetPlayer(friend.ToString());
            if (player == null)
                Debug.LogError($"Could not find friend with id {friend} in Room.");
            return new FriendInfo
            {
                friendID = friend,
                friendName = player?.NickName ?? ""
            };
        }

#region Search Room

        public void SearchRoom(string roomSeq)
        {
            Debug.Log($"#### {GetType().Name} :: SearchRoom(roomSeq: {roomSeq})");

            if (!string.IsNullOrEmpty(roomSeq))
            {
                var searchTar = roomSeq.Trim();

                Debug.Log("SearchLobby StringFilter:" + searchTar);

                var joinRandomRoomParams = new OpJoinRandomRoomParams
                {
                    TypedLobby = new TypedLobby(
                        name: roomSeq,
                        type: LOBBY_TYPE),
                    ExpectedMaxPlayers = (byte) 2,
                    SqlLobbyFilter = $"{ROOM_SEQ_LOBBY} = '{roomSeq}'"
                };
                m_PhotonClient.JoinRandomRoom(joinRandomRoomParams, OnJoinRandomRoomResponse);
            }
        }

        public Action<LobbyRoomSearchResult> SearchRoomResponse;
        
        private void OnJoinRandomRoomResponse(JoinRandomRoomResponse response)
        {
            Debug.Log($"#### {GetType().Name} :: OnJoinRandomRoomResponse(resultCode: {response.ResultCode.ToString()})");
            if (response.ResultCode == EResult.k_EResultOK)
            {
                mCachedRoomName = "";
                mLobbyRoomSearchResult = GetLobbyRoomSearchResult(response.Room);
                m_PhotonClient.LeaveRoom(false, NotifyLobbyRoomSearchResult);
            }
            else
            {
                mCachedRoomName = "";
                var searchResult = new LobbyRoomSearchResult {ret_ = SearchRet.NotFound};
                SearchRoomResponse?.Invoke(searchResult);
            }
        }

        private LobbyRoomSearchResult mLobbyRoomSearchResult;

        private void NotifyLobbyRoomSearchResult()
        {
            SearchRoomResponse?.Invoke(mLobbyRoomSearchResult);
        }

        private static LobbyRoomSearchResult GetLobbyRoomSearchResult(Room room)
        {
            var searchResult = new LobbyRoomSearchResult {ret_ = SearchRet.NotFound};

            var roomProperties = room.CustomProperties;

            object value;
            if (!roomProperties.TryGetValue(LobbySystem.P2P, out value) ||
                (value is string && int.Parse((string) value) != 2))
            {
                searchResult.ret_ = SearchRet.DiffRegion;
                return searchResult;
            }

//            var p2pType = int.Parse((string) value);

            if (!roomProperties.TryGetValue(LobbySystem.GAME_VERSION, out value) ||
                (value is string && (string) value != "0.0.1"))
            {
                searchResult.ret_ = SearchRet.VersionMatchFail;
                Debug.Log($"SearchLobby CallBack ID:{room.Name} versionMatchFail {(string) value}");
                return searchResult;
            }

            searchResult.playerNum = room.PlayerCount;
            //Since we are in the room at this time, we must account for ourselves being in it too.
            //(In fact, we shouldn't be able to enter if the room was already full, so this check
            //should never be true.)
            if (searchResult.playerNum >= 2 + 1)
            {
                searchResult.ret_ = SearchRet.NoSlot;
                return searchResult;
            }

            if (roomProperties.TryGetValue(LobbySystem.ROOM_SEQ, out value) && value is string)
                searchResult.roomSeq = (string) value;

            searchResult.roomID = ulong.Parse(room.Name);
            searchResult.gameversion = "0.0.1";
            searchResult.ret_ = SearchRet.SearchSucc;

            return searchResult;
        }

#endregion

        public void UpdateRoomData(string key, string value)
        {
            Debug.Log($"#### {GetType().Name} :: UpdateRoomData(key: {key}, value: {value})");
            m_PhotonClient.SetCustomRoomProperties(new Hashtable {{key, value}});
        }

        public string GetRoomData(string key)
        {
            Debug.Log($"#### {GetType().Name} :: GetRoomData(key: {key})");
            var roomProperties = m_PhotonClient.GetCustomRoomProperties();
            object result;
            if (roomProperties.TryGetValue(key, out result) && result is string)
                return (string) result;
            return "";
        }

        /// <remarks>This gets called in Unity's Update.</remarks>
        public bool IsLoggedIn()
        {
            return true;//PhotonClient.Instance.GetIsLoggedIn();
        }

#region IInRoomCallbacks

        private List<string> mPlayersAlreadyJoinedOnce = new List<string>();

        public Action<LobbyRoomEnterEvent> OtherPlayerEnteredRoom;
        public Action<LobbyRoomLeaveEvent> OtherPlayerLeftRoom;

        private void OnPlayerEnteredRoom(Player newPlayer, Room room)
        {
            //THIS IS VERY UGLY. THIS IS A WORKAROUND. SINCE WITH PHOTON WE CAN ONLY "FIND A ROOM" BY
            //TRYING TO JOIN IT (AS FAR AS I KNOW), WE ARE DOING THAT. SO I WANT TO IGNORE THE FIRST
            //TIME A PLAYER JOINED A ROOM, BECAUSE THEY WERE NOT JOINING, BUT SEARCHING FOR IT.
            Debug.Log($"#### {GetType().Name} :: OnPlayerEnteredRoom(newPlayer: {newPlayer.UserId} ({(mPlayersAlreadyJoinedOnce.Contains(newPlayer.UserId) ? "already joined" : "for the first time")}), room: {room.Name})");
            if (!mPlayersAlreadyJoinedOnce.Contains(newPlayer.UserId))
            {
                mPlayersAlreadyJoinedOnce.Add(newPlayer.UserId);
                return;
            }
            else
                mPlayersAlreadyJoinedOnce.Remove(newPlayer.UserId);

            var lobbyRoomEnterEvent = new LobbyRoomEnterEvent
            {
                response = EnterResponse.k_EChatRoomEnterResponseSuccess,
                RoomID = ulong.Parse(room.Name),
                OwnerID = ulong.Parse((string) room.CustomProperties[LobbySystem.OWNER]),
                TriggerID = ulong.Parse(newPlayer.UserId)
            };
            OtherPlayerEnteredRoom?.Invoke(lobbyRoomEnterEvent);
        }

        private void OnOtherPlayerLeftRoom(Player otherPlayer, Room room)
        {
            Debug.Log($"#### {GetType().Name} :: OnOtherPlayerLeftRoom(otherPlayer: {otherPlayer.UserId} ({(mPlayersAlreadyJoinedOnce.Contains(otherPlayer.UserId) ? "joined once, so we assume they were searching for the room" : "for real!")}))");
            if (mPlayersAlreadyJoinedOnce.Contains(otherPlayer.UserId))
                return;

            var lobbyRoomLeaveEvent = new LobbyRoomLeaveEvent
            {
                RoomID = ulong.Parse(room.Name),
                ownerID = ulong.Parse((string) room.CustomProperties[LobbySystem.OWNER]),
                TriggerID = ulong.Parse(otherPlayer.UserId)
            };
            OtherPlayerLeftRoom?.Invoke(lobbyRoomLeaveEvent);
        }

        public Action PlayerPropertiesUpdated;

        private void OnPlayerPropertiesUpdated()
        {
            PlayerPropertiesUpdated?.Invoke();
        }

#endregion

#region Packet reading

        /// <remarks>Since we are multi-threading, this method will be called in a background thread.
        /// Not the same as the one receiving photon events!</remarks>
        public static void ReadP2PPackageMultiThread(PhotonClient photonClient, List<byte[]> eventsToFill)
        {
            lock (mStopObject)
            {
                PhotonPacket packet;
                while (mIsRunning && photonClient.PacketsReceivedPendingProcess.TryDequeue(out packet) && eventsToFill.Count < 20)
                {
                    int dataSize;
                    var networkEvent = DecodePacketEventInLock(packet, out dataSize);
                    eventsToFill.Add(networkEvent);
                }
            }
        }

        /// <remarks>This method should be called from inside a `lock (mStopObject) { }`.</remarks>
        private static byte[] DecodePacketEventInLock(PhotonPacket photonPacket, out int dataSize)
        {
            var data = photonPacket.Data;
            var dataSizeUint = (uint) photonPacket.DataLength;
            dataSize = (int) dataSizeUint;
            return data;
        }

#endregion

    }
}
