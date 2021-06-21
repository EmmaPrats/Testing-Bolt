using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TestingBolt
{
    public abstract class DummyPacketSender : MonoBehaviour
    {
        [Header("References:")]
        [SerializeField] private Button m_StartSendingReliablyButton;
        [SerializeField] private Button m_StartSendingUnreliablyButton;
        [SerializeField] private Button m_StopSendingReliablyButton;
        [SerializeField] private Button m_StopSendingUnreliablyButton;
        [Space(20)]
        [SerializeField] private Slider m_ReliablePacketSizeSlider;
        [SerializeField] private Slider m_ReliableSendWaitMsSlider;
        [SerializeField] private Slider m_UnreliablePacketSizeSlider;
        [SerializeField] private Slider m_UnreliableSendWaitMsSlider;
        [Space(20)]
        [SerializeField] private TMP_Text m_ReliablePacketSizeLabel;
        [SerializeField] private TMP_Text m_ReliableSendWaitMsLabel;
        [SerializeField] private TMP_Text m_UnreliablePacketSizeLabel;
        [SerializeField] private TMP_Text m_UnreliableSendWaitMsLabel;

        private int mReliablePacketSize;
        private int mReliableSendWaitMs;
        private int mUnreliablePacketSize;
        private int mUnreliableSendWaitMs;

        private Thread mSendReliableThread;
        private Thread mSendUnreliableThread;
        private bool mRun = true;

        private INetTransport mTransport;

        protected abstract INetTransport GetTransport();
        
        private async void Start()
        {
            mTransport = GetTransport();
            await mTransport.IsInitialized;

            m_StartSendingReliablyButton.onClick.AddListener(OnStartSendingReliablyButtonClicked);
            m_StartSendingUnreliablyButton.onClick.AddListener(OnStartSendingUnreliablyButtonClicked);
            m_StopSendingReliablyButton.onClick.AddListener(OnStopSendingReliablyButtonClicked);
            m_StopSendingUnreliablyButton.onClick.AddListener(OnStopSendingUnreliablyButtonClicked);
        }

        private void Update()
        {
            mReliablePacketSize = (int) m_ReliablePacketSizeSlider.value;
            mReliableSendWaitMs = (int) m_ReliableSendWaitMsSlider.value;
            mUnreliablePacketSize = (int) m_UnreliablePacketSizeSlider.value;
            mUnreliableSendWaitMs = (int) m_UnreliableSendWaitMsSlider.value;

            m_ReliablePacketSizeLabel.text = mReliablePacketSize.ToString();
            m_ReliableSendWaitMsLabel.text = mReliableSendWaitMs.ToString();
            m_UnreliablePacketSizeLabel.text = mUnreliablePacketSize.ToString();
            m_UnreliableSendWaitMsLabel.text = mUnreliableSendWaitMs.ToString();
        }

        private void OnDestroy()
        {
            mRun = false;
            m_StartSendingReliablyButton.onClick.RemoveListener(OnStartSendingReliablyButtonClicked);
            m_StartSendingUnreliablyButton.onClick.RemoveListener(OnStartSendingUnreliablyButtonClicked);
            m_StopSendingReliablyButton.onClick.RemoveListener(OnStopSendingReliablyButtonClicked);
            m_StopSendingUnreliablyButton.onClick.RemoveListener(OnStopSendingUnreliablyButtonClicked);
        }

        private void OnStartSendingReliablyButtonClicked() => StartSendingReliably();
        private void OnStartSendingUnreliablyButtonClicked() => StartSendingUnreliably();
        private void OnStopSendingReliablyButtonClicked() => StopSendingReliably();
        private void OnStopSendingUnreliablyButtonClicked() => StopSendingUnreliably();

        private void StartSendingReliably()
        {
            MyDebug.Log("StartSendingReliably()");
            mSendReliableThread = new Thread(SendReliableJob);
            mSendReliableThread.Start();
        }

        private void StartSendingUnreliably()
        {
            MyDebug.Log("StartSendingUnreliably()");
            mSendUnreliableThread = new Thread(SendUnreliableJob);
            mSendUnreliableThread.Start();
        }

        private void StopSendingReliably()
        {
            MyDebug.Log("StopSendingReliably()");
            mSendReliableThread.Interrupt();
        }

        private void StopSendingUnreliably()
        {
            MyDebug.Log("StopSendingUnreliably()");
            mSendUnreliableThread.Interrupt();
        }

        private void SendReliableJob()
        {
            try
            {
                while (mRun)
                {
                    Thread.Sleep(mReliableSendWaitMs);
                    mTransport.SendReliable(CreateData(mReliablePacketSize), mReliablePacketSize);
                }
            }
            catch (ThreadAbortException)
            {
                Thread.ResetAbort();
            }
        }

        private void SendUnreliableJob()
        {
            try
            {
                while (mRun)
                {
                    Thread.Sleep(mUnreliableSendWaitMs);
                    mTransport.SendUnreliable(CreateData(mUnreliablePacketSize), mUnreliablePacketSize);
                }
            }
            catch (ThreadAbortException)
            {
                Thread.ResetAbort();
            }
        }

        private static readonly System.Random mRandom = new System.Random();

        /// <param name="size">In bytes.</param>
        private static byte[] CreateData(int size)
        {
            var data = new byte[size];
            mRandom.NextBytes(data);
            return data;
        }
    }
}
