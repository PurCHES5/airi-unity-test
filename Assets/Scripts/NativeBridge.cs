// Assets/Scripts/NativeBridge.cs
using System.Runtime.InteropServices;

public static class NativeBridge
{
#if UNITY_IOS
    [DllImport("__Internal")]
    public static extern void SendToCapacitor(string eventName, string data);
#else
    public static void SendToCapacitor(string eventName, string data) { }
#endif
}