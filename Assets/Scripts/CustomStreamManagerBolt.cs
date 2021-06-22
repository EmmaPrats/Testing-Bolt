using System.Threading.Tasks;
using Photon.Bolt;
using Testing;
using UdpKit;

namespace TestingBolt
{
    public class CustomStreamManagerBolt : GlobalEventListener, INetTransport
    {
        private const string m_ReliableChannelName = "Reliable Channel";
        private const string m_UnreliableChannelName = "Unreliable Channgl";

        private static UdpChannelName ReliableChannel;
        private static UdpChannelName UnreliableChannel;

        private readonly TaskCompletionSource<bool> mIsInitialized = new TaskCompletionSource<bool>();
        public Task<bool> IsInitialized => mIsInitialized.Task;

        public bool IsServer => BoltNetwork.IsServer;

        public override void BoltStartBegin()
        {
            CreateChannels();
        }

        public override void Connected(BoltConnection connection)
        {
            mIsInitialized.SetResult(true);
        }

        private void CreateChannels()
        {
            MyDebug.Log("CreateChannels()");

            ReliableChannel = BoltNetwork.CreateStreamChannel(
                m_ReliableChannelName,
                UdpKit.UdpChannelMode.Reliable,
                4);
            UnreliableChannel = BoltNetwork.CreateStreamChannel(
                m_UnreliableChannelName,
                UdpKit.UdpChannelMode.Unreliable,
                1);
        }

        public void SendReliable(byte[] data, int length)
        {
            if (BoltNetwork.IsServer)
                foreach (var client in BoltNetwork.Clients)
                    client.StreamBytes(ReliableChannel, data);
            else
                BoltNetwork.Server.StreamBytes(ReliableChannel, data);
        }

        public void SendUnreliable(byte[] data, int length)
        {
            if (BoltNetwork.IsServer)
                foreach (var client in BoltNetwork.Clients)
                    client.StreamBytes(UnreliableChannel, data);
            else
                BoltNetwork.Server.StreamBytes(UnreliableChannel, data);
        }
    }
}
