using System.Threading.Tasks;

namespace Testing
{
    public interface INetTransport
    {
        Task<bool> IsInitialized { get; }
        
        bool IsServer { get; }

        void SendReliable(byte[] data, int length);
        void SendUnreliable(byte[] data, int length);
    }
}
