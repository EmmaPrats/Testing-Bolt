using Testing;

namespace TestingBolt
{
    public class BoltColorChanger : ColorChanger
    {
        protected override INotifyReceivingPacketsOfLength4 GetPacketReceivedNotifier()
        {
            return FindObjectOfType<BoltMenu>();
        }

        protected override INetTransport GetNetTransport()
        {
            return FindObjectOfType<CustomStreamManagerBolt>();
        }
    }
}
