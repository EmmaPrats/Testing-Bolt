using Biped.Multiplayer.Photon;
using Testing;

namespace TestingPhoton
{
    public class PhotonColorChanger : ColorChanger
    {
        protected override INotifyReceivingPacketsOfLength4 GetPacketReceivedNotifier()
        {
            return FindObjectOfType<PhotonTransport>();
        }

        protected override INetTransport GetNetTransport()
        {
            return FindObjectOfType<PhotonTransport>();
        }
    }
}
