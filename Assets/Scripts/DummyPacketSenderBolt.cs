using Testing;

namespace TestingBolt
{
    public class DummyPacketSenderBolt : DummyPacketSender
    {
        protected override INetTransport GetTransport()
        {
            var streamManagerBolt = FindObjectOfType<CustomStreamManagerBolt>();
            return streamManagerBolt;
        }
    }
}
