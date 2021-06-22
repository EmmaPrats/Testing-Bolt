using System;
using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;
using Photon.Realtime;

namespace Biped.Multiplayer.Photon
{
    //General result codes
    public enum EResult : int
    {
        k_EResultOK = 1,                              // success
        k_EResultFail = 2,                            // generic failure
    }

    public struct CreateRoomResponse
    {
        public readonly EResult ResultCode;
        public readonly Room Room;

        public CreateRoomResponse(EResult resultcode, Room room = null)
        {
            ResultCode = resultcode;
            Room = room;
        }
    }

    public struct JoinRoomResponse
    {
        public readonly EResult ResultCode;
        public readonly Room Room;

        public JoinRoomResponse(EResult resultcode, Room room = null)
        {
            ResultCode = resultcode;
            Room = room;
        }
    }

    public struct JoinRandomRoomResponse
    {
        public readonly EResult ResultCode;
        public readonly Room Room;

        public JoinRandomRoomResponse(EResult resultcode, Room room = null)
        {
            ResultCode = resultcode;
            Room = room;
        }
    }

    public struct PhotonPacket
    {
        public const byte CODE = 1;
        private static long mUniqueId = 0;

        public const string DATA = "Data";
        public const string DATA_LENGTH = "DataLength";
        public const string SENDER_ID = "SenderID";
        public const string DEBUG_ID = "DebugID";
        private const string ORIGINAL_DATA_ARRAY_LENGTH = "OriginalDataArrayLength";

        public readonly byte[] Data;
        public readonly long DataLength; //Photon has issues with uint.
        /// <remarks>Should be ulong-parseable.</remarks>>
        public readonly string SenderID;
        public readonly long DebugID;
        public readonly Hashtable Hashtable;

        public PhotonPacket(byte[] data, uint dataLength, ulong senderID)
        {
            try
            {
                var dataLengthInt = Convert.ToInt32(dataLength);
                Data = data.ToList().GetRange(0, dataLengthInt).ToArray();
            }
            catch (OverflowException)
            {
                Data = data;
            }
            DataLength = dataLength;
            SenderID = senderID.ToString();
            DebugID = System.Threading.Interlocked.Increment(ref mUniqueId);
            Hashtable = new Hashtable
            {
                {DATA, Data},
                {DATA_LENGTH, DataLength},
                {SENDER_ID, SenderID},
                {DEBUG_ID, DebugID},
                {ORIGINAL_DATA_ARRAY_LENGTH, data.Length}
            };
        }

        public PhotonPacket(Hashtable hashtable)
        {
            object result;
            var data = hashtable.TryGetValue(DATA, out result) && result is byte[] ? (byte[]) result : new byte[0];
            DataLength = hashtable.TryGetValue(DATA_LENGTH, out result) && result is long ? (long) result : 0;
            SenderID = hashtable.TryGetValue(SENDER_ID, out result) && result is string ? (string) result : "0";
            DebugID = hashtable.TryGetValue(DEBUG_ID, out result) && result is long ? (long) result : 0;
            var originalDataLength = hashtable.TryGetValue(ORIGINAL_DATA_ARRAY_LENGTH, out result) && result is int ? (int) result : 0;

            var difference = originalDataLength - data.Length;
            Data = difference > 0
                ? new List<byte>(data).Concat(new byte[difference]).ToArray()
                : data;

            Hashtable = hashtable;
        }

        public string[] ToStringLineByLine(string linePrefix = "", bool includeDataArray = false)
        {
            var list = new List<string>(ContentsToStringLineByLine(linePrefix + "    ", includeDataArray));
            list.Insert(0, $"{linePrefix}{GetType().Name}:");
            return list.ToArray();
        }

        public string[] ContentsToStringLineByLine(string linePrefix = "", bool includeDataArray = false)
        {
            if (includeDataArray)
                return new[]
                {
                    linePrefix + $"DebugID: {string.Join(", ", DebugID)}",
                    linePrefix + $"Data ({Data.Length} length):",
                    linePrefix + "    " + string.Join(", ", Data),
                    linePrefix + $"DataLength: {DataLength}",
                    linePrefix + $"SenderID: {SenderID}",
                };
            return new[]
            {
                linePrefix + $"DebugID: {string.Join(", ", DebugID)}",
                linePrefix + $"Data ({Data.Length} length):",
                linePrefix + $"DataLength: {DataLength}",
                linePrefix + $"SenderID: {SenderID}",
            };
        }
    }

    public interface ILoadBalancingClientFacade
    {
        void CreateRoom(EnterRoomParams enterRoomParams, Action<CreateRoomResponse> callback);
        void JoinRoom(EnterRoomParams enterRoomParams, Action<JoinRoomResponse> callback);
        void JoinRandomRoom(OpJoinRandomRoomParams joinRandomRoomParams, Action<JoinRandomRoomResponse> callback);
        void LeaveRoom(bool shouldBecomeInactive, Action callback);
        void SetCustomRoomProperties(Hashtable properties);
        Hashtable GetCustomRoomProperties();
        bool GetIsLoggedIn();
        bool IsInRoom { get; }

        void SendReliable(PhotonPacket photonPacket);
        void SendUnreliable(PhotonPacket photonPacket);
    }
}
