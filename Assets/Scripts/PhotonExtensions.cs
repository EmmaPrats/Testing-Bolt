using System.Linq;
using ExitGames.Client.Photon;

public static class PhotonExtensions
{
    public static string ToStringContentsLineByLine(this Hashtable hashtable, string indent = "    ", int indentCount = 0)
    {
        var linePrefix = "";
        for (var i = 0; i < indentCount; i++)
            linePrefix += indent;
        return string.Join(
            "\n",
            hashtable.Select(
                pair => $"{linePrefix}{pair.Key}: {pair.Value}"));
    }
}
