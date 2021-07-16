using Photon.Bolt;
using UdpKit;

public class BoltPlayer : IProtocolToken
{
    public ulong ID;
    public string Nickname;
    public bool IsJustSearching;

    public BoltPlayer() { }

    public BoltPlayer(ulong id, string nickname, bool isJustSearching = false)
    {
        ID = id;
        Nickname = nickname;
        IsJustSearching = isJustSearching;
    }

    public void Read(UdpPacket packet)
    {
        ID = packet.ReadULong();
        Nickname = packet.ReadString();
        IsJustSearching = packet.ReadBool();
    }

    public void Write(UdpPacket packet)
    {
        packet.WriteULong(ID);
        packet.WriteString(Nickname);
        packet.WriteBool(IsJustSearching);
    }

    public override string ToString() => GetType().Name + "\n" + ToStringContentsLineByLine("    ", 1);

    public string ToStringContentsLineByLine(string indent = "    ", int indentCount = 0)
    {
        var linePrefix = "";
        for (var i = 0; i < indentCount; i++)
            linePrefix += indent;
        return $"{linePrefix}ID: {ID}" +
               $"\n{linePrefix}Nickname: {Nickname}" +
               $"\n{linePrefix}IsJustSearching: {IsJustSearching}";
    }
}
