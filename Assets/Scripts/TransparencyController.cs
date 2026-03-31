using UnityEngine;

public class TransparencyController : MonoBehaviour
{
    void Start()
    {
        // Only run on actual Android devices
        if (Application.platform == RuntimePlatform.Android)
        {
            using (AndroidJavaClass activityClass = new AndroidJavaClass("ai.moeru.airipocket.MainActivity"))
            {
                activityClass.CallStatic("makeTransparent");
            }
        }
    }
}