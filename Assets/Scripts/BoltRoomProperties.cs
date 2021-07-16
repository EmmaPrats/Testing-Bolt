using Photon.Bolt;
using Photon.Bolt.Matchmaking;
using TMPro;
using UdpKit.Platform.Photon;
using UnityEngine;

public class BoltRoomProperties : MonoBehaviour
{
    [SerializeField] private TMP_Text m_Label;

    private void Update()
    {
        if (BoltNetwork.IsRunning &&
            BoltMatchmaking.CurrentSession != null &&
            BoltMatchmaking.CurrentSession is PhotonSession photonSession)
            m_Label.text = photonSession.Properties.ToStringContentsLineByLine();
    }
}
