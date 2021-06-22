using Biped.Multiplayer.Photon;
using TestingBolt;
using UnityEngine;
using UnityEngine.UI;

namespace TestingPhoton
{
    public class PhotonMenu : MonoBehaviour
    {
        [SerializeField] private Button m_CreateServerButton;
        [SerializeField] private Button m_JoinServerButton;

        [SerializeField] private PhotonClient m_PhotonClient;
        [SerializeField] private PhotonLobby m_PhotonLobby;
        [SerializeField] private PhotonTransport m_PhotonTransport;

        private void Start()
        {
            m_PhotonLobby.CreateRoomResponse += OnCreateRoomResponse;
            m_PhotonLobby.JoinRoomResponse += OnJoinRoomResponse;
            m_PhotonLobby.SearchRoomResponse += OnSearchRoomResponse;
            
            m_CreateServerButton.onClick.AddListener(OnCreateServerButtonClicked);
            m_JoinServerButton.onClick.AddListener(OnJoinServerButtonClicked);

            m_PhotonClient.Connect();
            m_PhotonLobby.SetUp();
        }

        private void OnDestroy()
        {
            m_CreateServerButton.onClick.RemoveListener(OnCreateServerButtonClicked);
            m_JoinServerButton.onClick.RemoveListener(OnJoinServerButtonClicked);
        }

        private void OnCreateServerButtonClicked()
        {
            m_PhotonLobby.CreateRoom("test_room_seq");
        }

        private void OnJoinServerButtonClicked()
        {
            m_PhotonLobby.SearchRoom("test_room_seq");
        }

        private void OnCreateRoomResponse(LobbyRoomCreateEvent response)
        {
            MyDebug.Log($"OnCreateRoomResponse(response: {response.response.ToString()})");

            if (response.response == CreateResponse.k_EResultOK)
                m_PhotonTransport.Init();
        }

        private void OnJoinRoomResponse(LobbyRoomEnterEvent response)
        {
            MyDebug.Log($"OnJoinRoomResponse(response: {response.response.ToString()})");

            if (response.response == EnterResponse.k_EChatRoomEnterResponseSuccess)
                m_PhotonTransport.Init();
        }

        private void OnSearchRoomResponse(LobbyRoomSearchResult response)
        {
            MyDebug.Log($"OnSearchRoomResponse(response: {response.ret_.ToString()})");

            if (response.ret_ == SearchRet.SearchSucc)
                m_PhotonLobby.JoinRoom(response.roomID, false);
        }
    }
}
