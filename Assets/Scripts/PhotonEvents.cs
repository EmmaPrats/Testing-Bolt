namespace TestingPhoton
{
    public enum EEvent
    {

        EventNone = 0,
        
        //user op
        ECreateLocalOp,
        ECreateNetGameOp,
        EJoinNetGameOp,
        EStartGameOp,
        ELeaveRoomOp,
        ELeaveGameOp,
        EChangeLevelOp,
        ELobbyReadyOp,

        ELevelUnLoadFinish,
        ELevelTerm,
        ELoadSavePoint,
        ELevelLoadProcess,

        EOutfitUnLock,
        ESaveTrigger,
        EChallengeStart,
        EChallengeOver,

        //game net
        ELostConnect, 
        EAddPeer,
        ERemovePeer,  
        



        EGameVaildCheck,
        EGameVaildCheckRsp,

        ESvrStartGame,
        ENetGameLoadFinish,
        EAllLoadFinish,


        //level 
        ELevelAwake,
        ELevelStart,
        ELevelFinish,
        ELevelUIFinish,
        ELevelSnapInit,


        //LobbyEvent
        ELobbyCreate,
        ELobbyEnter,
        ELobbyOtherEnter,
        ELobbyOtherLeave,
        ELobbySearch,
        ELobbyJoinRequest,
        ELobbyHostSucc,

        ELobbyMemberOtherCHange,

        ELobbyClientRoomInfo,

        ELobbyUpdateUserInfo,

        EQosStart,
        EQosEnd,

        //Steam P2P
        ESteamP2PRequest,

       
        ESteamLogout,

        //voice
        EJoinRoomVoice,

        //Controller
        EControllerTypeChange,
        
        //GAME
        EAutoTakeTrackCoin,
        ETakenUnique,

        ESteamTrophyRegister,

        /// <summary>This means QUIT. Should rename it some time.</summary>
        EQuickGame,
        
        //UINOTIFY
        EUINotify,
        ELanguageChange,
        EOutfitChange,
        ENavChange,
        //GamePlayEvent max
        GamePlayEventMax = 1000,
    }

    public enum CreateResponse
    {
        k_EResultOK = 1,// - the lobby was successfully created
        k_EResultNoConnection,// - your Steam client doesn't have a connection to the back-end
        k_EResultTimeout, // - you the message to the Steam servers, but it didn't respond
        k_EResultFail, // - the server responded, but with an unknown internal error
        k_EResultAccessDenied, //- your game isn't set to allow lobbies, or your client does haven't rights to play the game
        k_EResultLimitExceeded
    }

    public enum EnterResponse
    {
        k_EChatRoomEnterResponseSuccess = 1,        // Success
        k_EChatRoomEnterResponseDoesntExist = 2,    // Chat doesn't exist (probably closed)
        k_EChatRoomEnterResponseNotAllowed = 3,     // General Denied - You don't have the permissions needed to join the chat
        k_EChatRoomEnterResponseFull = 4,           // Chat room has reached its maximum size
        k_EChatRoomEnterResponseError = 5,          // Unexpected Error
        k_EChatRoomEnterResponseBanned = 6,         // You are banned from this chat room and may not join
        k_EChatRoomEnterResponseLimited = 7,        // Joining this chat is not allowed because you are a limited user (no value on account)
        k_EChatRoomEnterResponseClanDisabled = 8,   // Attempt to join a clan chat when the clan is locked or disabled
        k_EChatRoomEnterResponseCommunityBan = 9,   // Attempt to join a chat when the user has a community lock on their account
        k_EChatRoomEnterResponseMemberBlockedYou = 10, // Join failed - some member in the chat has blocked you from joining
        k_EChatRoomEnterResponseYouBlockedMember = 11, // Join failed - you have blocked some member already in the chat
        k_EChatRoomEnterResponseRatelimitExceeded = 12,
        k_EChatRoomGameVersionMatchFail = 13,
        k_EChatRoomGameP2PRegionDiff = 14,
        k_EChatRoomEnterConnFail = 15,
        k_EChatRoomEnterFriendOnly = 16
    }

    public class EventBase
    {
        public EEvent eventType;
    }

    public class LobbyRoomCreateEvent : EventBase
    {
        public CreateResponse response;
        public ulong RoomID;
        public ulong RoomOwner;
        public string Seq;
    }

    public class LobbyRoomEnterEvent : EventBase
    {
        public EnterResponse response;
        public ulong RoomID;
        public ulong TriggerID;
        public ulong OwnerID;
        public string roomSeq;
    }

    public enum SearchRet
    {
        SearchSucc,
        VersionMatchFail,
        FriendOnly,
        DiffRegion,
        NoSlot,
        NotFound,
        ConnectFail,
    }

    public class LobbyRoomSearchResult : EventBase
    {
        public SearchRet ret_;
        public ulong roomID;
        public string gameversion;
        public int playerNum;
        public string roomSeq;
    }

    public class LobbyRoomMemberChange : EventBase
    {
        public ulong RoomID;
        public ulong TriggerID;
        public ulong OwnerID;
    }

    public class LobbyRoomLeaveEvent : EventBase
    {
        public ulong ownerID;
        public ulong TriggerID;
        public ulong RoomID;
    }
}