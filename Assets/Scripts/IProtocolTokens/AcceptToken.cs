using System.Linq;
using ExitGames.Client.Photon;
using Photon.Bolt;
using UdpKit;
using UnityEngine;

public class AcceptToken : IProtocolToken
{
    public BoltPlayer BoltPlayer;
    public Hashtable Properties;
    private int mPropertiesAmount;

    public AcceptToken() { }

    public AcceptToken(BoltPlayer boltPlayer, Hashtable properties)
    {
        BoltPlayer = boltPlayer;
        Properties = properties;
        mPropertiesAmount = properties.Count(pair => pair.Value is string);
    }

    public void Read(UdpPacket packet)
    {
        BoltPlayer = new BoltPlayer();
        BoltPlayer.Read(packet);

        mPropertiesAmount = packet.ReadInt();

        Properties = new Hashtable();
        for (var i = 0; i < mPropertiesAmount; i++)
        {
            var key = packet.ReadString();
            var value = packet.ReadString();
            Properties.Add(key, value);
        }
    }

    public void Write(UdpPacket packet)
    {
        BoltPlayer.Write(packet);

        packet.WriteInt(mPropertiesAmount);

        foreach (var keyValuePair in Properties)
        {
            if (keyValuePair.Value is string str)
            {
                packet.WriteString((string) keyValuePair.Key);
                packet.WriteString(str);
            }
            else
                Debug.LogWarning($"#### {GetType().Name} :: Write() :: Can't write property [{keyValuePair.Key}: {keyValuePair.Value}] because value is of type {keyValuePair.Value.GetType().Name}.");
        }
    }

    public override string ToString() => GetType().Name + "\n" + ToStringContentsLineByLine("    ", 1);

    public string ToStringContentsLineByLine(string indent = "    ", ushort indentCount = 0)
    {
        var linePrefix = "";
        for (var i = 0; i < indentCount; i++)
            linePrefix += indent;
        return $"{linePrefix}BoltPlayer:\n{BoltPlayer.ToStringContentsLineByLine(indent, indentCount + 1)}" +
               $"\n{linePrefix}mPropertiesAmount: {mPropertiesAmount}" +
               $"\n{linePrefix}Properties:\n{Properties.ToStringContentsLineByLine(indent, indentCount + 1)}";
    }
}
