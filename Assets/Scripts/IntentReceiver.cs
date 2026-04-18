using TMPro;
using UnityEngine;

public class IntentReceiver : MonoBehaviour
{   
    [SerializeField]
    TextMeshPro testText;

    // This method name must match the one in your Swift code
    public void ReceiveMessageFromIntent(string message)
    {
        Debug.Log("Received from Action Button / App Intent: " + message);
        testText.text = message;
    }
}
