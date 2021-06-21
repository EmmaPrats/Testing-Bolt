using TMPro;
using UnityEngine;

namespace TestingBolt
{
    [RequireComponent(typeof(TMP_Text))]
    public class MyDebug : MonoBehaviour
    {
        private static MyDebug mInstance;

        private TMP_Text mText;

        private void Awake()
        {
            if (mInstance == null)
                mInstance = this;
            else
                Destroy(gameObject);

            mText = GetComponent<TMP_Text>();
        }

        public static void Log(object message, Object context = null)
        {
            mInstance.mText.text += message.ToString() + "\n";

            if (context == null)
                Debug.Log(message);
            else
                Debug.Log(message, context);
        }
    }
}
