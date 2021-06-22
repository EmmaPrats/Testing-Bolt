using Biped.Multiplayer.Photon;
using Testing;
using TestingBolt;

namespace TestingPhoton
{
    public class DummyPacketSenderPhoton : DummyPacketSender
    {
        protected override INetTransport GetTransport()
        {
            var streamManagerPhoton = FindObjectOfType<PhotonTransport>();
            return streamManagerPhoton;
        }
    }
}
