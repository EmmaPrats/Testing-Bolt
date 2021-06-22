using System;

namespace Testing
{
    public interface INotifyReceivingPacketsOfLength4
    {
        Action<byte[]> ReceivedPacketOfLength4 { get; set; }
    }
}
