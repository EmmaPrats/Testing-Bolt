using System.Threading.Tasks;

namespace TestingBolt
{
    public interface INetTransport
    {
        Task<bool> IsInitialized { get; }

        void SendReliable(byte[] data, int length);
        void SendUnreliable(byte[] data, int length);
    }
}
