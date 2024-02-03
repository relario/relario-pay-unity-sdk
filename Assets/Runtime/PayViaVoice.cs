using System;
using UnityEngine;
using UnityEngine.Android;

using Relario.Network.Models;

public class PayViaVoice : MonoBehaviour {
    private const string PERMISSION = "android.permission.CALL_PHONE";
    public Transaction transaction;

    AndroidJavaObject currentActivity;

    public void Start () {
        AndroidJavaClass UnityPlayer = new AndroidJavaClass ("com.unity3d.player.UnityPlayer");
        currentActivity = UnityPlayer.GetStatic<AndroidJavaObject> ("currentActivity");
    }

    public void Call () {
        if (Application.isEditor) {
            throw new Exception ("Can't make voice call in editor");
        }

#if PLATFORM_ANDROID
        Debug.LogWarning ("[Relario] Voice Pay");

        if (!Permission.HasUserAuthorizedPermission (PayViaVoice.PERMISSION)) {
            PermissionCallbacks pc = new PermissionCallbacks ();

            pc.PermissionDenied += delegate (string str) {
                RunAndroidUiThread (noPermissionToast);
                Call ();
            };

            pc.PermissionDeniedAndDontAskAgain += delegate (string str) {
                RunAndroidUiThread (noPermissionToast);
            };

            pc.PermissionGranted += delegate (string str) {
                RunAndroidUiThread (CallProcess);
            };
            Permission.RequestUserPermission (PayViaVoice.PERMISSION, pc);
        } else {
            RunAndroidUiThread (CallProcess);
        }
#else
        Debug.LogWarning ("Effective only on PLATFORM_ANDROID");
#endif
    }

    void RunAndroidUiThread (Action action) {
        currentActivity.Call ("runOnUiThread", new AndroidJavaRunnable (action));
    }

    void CallProcess () {
        Debug.Log ("Running on UI thread");

        if (!Permission.HasUserAuthorizedPermission (PayViaVoice.PERMISSION)) {
            noPermissionToast ();
        }

        string toastMessage;

        try {
            AndroidJavaClass intent = new AndroidJavaClass ("android.content.Intent");
            string actionCall = intent.GetStatic<string> ("ACTION_CALL");

            AndroidJavaObject URI = new AndroidJavaClass ("android.net.Uri")
                .CallStatic<AndroidJavaObject> ("parse", transaction.clickToCallUrl);

            AndroidJavaObject callIntent = new AndroidJavaObject ("android.content.Intent", actionCall, URI);

            currentActivity.Call ("startActivity", callIntent);
            toastMessage = transaction.callDuration + " seconds voice call";
        } catch (System.Exception e) {
            Debug.LogError ("CallProcess failed: " + e.StackTrace.ToString ());
            toastMessage = "Failed to make voice call";
        }

        showToast (toastMessage);
    }

    // Helper function passed to RunAndroidUiThread which has no parameter, since showToast has one
    void noPermissionToast () {
        showToast ("Voice call permissions required");
    }

    void showToast (string message) {
        AndroidJavaObject context = currentActivity.Call<AndroidJavaObject> ("getApplicationContext");
        AndroidJavaClass Toast = new AndroidJavaClass ("android.widget.Toast");
        AndroidJavaObject javaString = new AndroidJavaObject ("java.lang.String", message);
        AndroidJavaObject toast = Toast.CallStatic<AndroidJavaObject> ("makeText", context, javaString, Toast.GetStatic<int> ("LENGTH_LONG"));
        toast.Call ("show");
    }
}