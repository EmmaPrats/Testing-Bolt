using UnityEngine;

namespace TestingPhoton
{
    public class LobbySystem
    {
        public const string ALLOW_CONN = "ALLOWCONNECT";
        public const string ROOM_SEQ = "SEQ";
        public const string GAME_VERSION = "Version";
        public const string FRIEND_ONLY = "FriendOnly";

        public const string CUR_ROOM = "CUR_ROOM";

        public const string OWNER = "OWNER";
        public const string TIME = "Time";
        public const string P2P = "P2P";

        public static string GenerateNextSeq()
        {
            char[] values_ = {'1', '2', '3', '4', '6', '7', '8', '9', 'A', 'C', 'D', 'E', 'F', 'H', 'M', 'N', 'L', 'K', 'U', 'X' };
            var seq_ = "";
#if FINAL_RELEASE
            for (int i = 0; i < 6; ++i)
                seq_ += (values_[UnityEngine.Random.Range(0, values_.Length - 1)]);
#else
            for (int i = 0; i < 4; ++i)
                seq_ += values_[Random.Range(0, 9)];
#endif
            return seq_;
        }
    }
}
