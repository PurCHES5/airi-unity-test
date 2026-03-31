// Assets/Scripts/UnityBridge.cs
using System.Collections;
using UnityEngine;

public class UnityBridge : MonoBehaviour
{
    public static UnityBridge Instance { get; private set; }

    [SerializeField] private Transform target2D;

    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Called FROM Vue/Capacitor via native bridge
    public void ReceiveMessage(string jsonPayload)
    {
        Debug.Log($"[Unity] Received: {jsonPayload}");
        StartCoroutine(RotateZ360(target2D, 0.5f));
    }

    private IEnumerator RotateZ360(Transform t, float duration)
    {
        float elapsed = 0f;
        float startZ = t.eulerAngles.z;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float angle = Mathf.Lerp(0f, 360f, elapsed / duration);
            t.eulerAngles = new Vector3(t.eulerAngles.x, t.eulerAngles.y, startZ + angle);
            yield return null;
        }

        // Snap back to original rotation to avoid floating-point drift
        t.eulerAngles = new Vector3(t.eulerAngles.x, t.eulerAngles.y, startZ);
    }

    // Send message TO Vue/Capacitor
    public void SendToVue(string eventName, string data)
    {
#if UNITY_IOS
        NativeBridge.SendToCapacitor(eventName, data);
#elif UNITY_ANDROID
        using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        activity.Call("sendToCapacitor", eventName, data);
#endif
    }
}