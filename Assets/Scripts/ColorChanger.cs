using TestingBolt;
using UnityEngine;
using UnityEngine.UI;

namespace Testing
{
    public abstract class ColorChanger : MonoBehaviour
    {
        [Header("Params:")]
        [SerializeField] private Color m_Color1;
        [SerializeField] private Color m_Color2;

        [Header("References:")]
        [SerializeField] private Button m_ChangeColorButton;
        [SerializeField] private Image m_ServerImage;
        [SerializeField] private Image m_ClientImage;

        private INotifyReceivingPacketsOfLength4 mReceivedPacketOfLength4Notifier;
        private INetTransport mNetTransport;

        private bool mIsConnected;

        private Image mImage;
        private Color mColor;

        protected abstract INotifyReceivingPacketsOfLength4 GetPacketReceivedNotifier();
        protected abstract INetTransport GetNetTransport();
        
        private async void Start()
        {
            mColor = m_Color1;
            mImage = m_ServerImage;

            m_ChangeColorButton.onClick.AddListener(OnChangeColorButtonClicked);

            mReceivedPacketOfLength4Notifier = GetPacketReceivedNotifier();
            mReceivedPacketOfLength4Notifier.ReceivedPacketOfLength4 += OnReceivedColorChangePacket;

            mNetTransport = GetNetTransport();
            await mNetTransport.IsInitialized;

            mImage = mNetTransport.IsServer
                ? m_ServerImage
                : m_ClientImage;

            mIsConnected = true;
        }

        private void OnDestroy()
        {
            mReceivedPacketOfLength4Notifier.ReceivedPacketOfLength4 -= OnReceivedColorChangePacket;
            m_ChangeColorButton.onClick.RemoveListener(OnChangeColorButtonClicked);
        }

        private void OnReceivedColorChangePacket(byte[] newColor)
        {
            var image = newColor[0] == 0
                ? m_ServerImage
                : m_ClientImage;
            var color = ColorExtensions.FromBytes(newColor[1], newColor[2], newColor[3]);
            Debug.Log($"OnReceivedColorChangePacket(from: {newColor[0]}, color: {color})");
            SetColor(image, color);
        }

        private void SetColor(Image image, Color color)
        {
            image.color = color;
        }

        private void OnChangeColorButtonClicked()
        {
            mColor = mColor == m_Color1
                ? m_Color2
                : m_Color1;

            var color = mColor.ToBytes();
            var data = new[]
            {
                mImage == m_ServerImage
                    ? (byte) 0
                    : (byte) 1,
                color[0],
                color[1],
                color[2]
            };

            SetColor(mImage, mColor);

            if (mIsConnected)
                mNetTransport.SendReliable(data, 4);
        }
    }
    
    public static class ColorExtensions
    {
        public static byte[] ToBytes(this Color color)
        {
            return new[]
            {
                (byte) (color.r * 255f),
                (byte) (color.g * 255f),
                (byte) (color.b * 255f)
            };
        }

        public static Color FromBytes(byte r, byte g, byte b)
        {
            return new Color(
                r / 255f,
                g / 255f,
                b / 255f);
        }
    }
}
