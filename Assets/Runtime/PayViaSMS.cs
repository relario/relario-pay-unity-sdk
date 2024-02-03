using System;
using UnityEngine;
using UnityEngine.Android;
using Relario.Network.Models;

public class PayViaSMS : MonoBehaviour
{
    private const string SMSPermission = "android.permission.SEND_SMS"; // SMS permission
    // private const string CallPermission = "android.permission.CALL_PHONE"; // Call permission

    public Transaction transaction;
    private bool switchToSmsApp;
    // Reference to the SMSHandler script
    public SMSPaymentHandler paymentsHandler;
    AndroidJavaObject currentActivity;

    public void Start()
    {
        paymentsHandler = FindAnyObjectByType<SMSPaymentHandler>();
        switchToSmsApp = paymentsHandler.switchToSmsApp;
#if PLATFORM_ANDROID
        AndroidJavaClass UnityPlayer = new AndroidJavaClass ("com.unity3d.player.UnityPlayer");
        this.currentActivity = UnityPlayer.GetStatic<AndroidJavaObject> ("currentActivity");
#endif
    }

    public void Send()
    {
        if (Application.isEditor)
        {
            throw new Exception("Can't send SMS in editor");
        }

#if PLATFORM_ANDROID
        Debug.LogWarning ("[Relario] SMS Pay");

        // If switchToSmsApp is true, use SMSHandler to generate SMS URL
        if (switchToSmsApp)
        {
            // Convert the List<string> to an array of strings
            string[] phoneNumbersArray = transaction.phoneNumbersList.ToArray();

            string smsUrl = paymentsHandler.GetClickToSmsUrl(phoneNumbersArray, transaction.smsBody);
            Application.OpenURL(smsUrl);
        }
        else
        {
            if (!Permission.HasUserAuthorizedPermission (PayViaSMS.SMSPermission)) {
                PermissionCallbacks pc = new PermissionCallbacks ();

                pc.PermissionDenied += delegate (string str) {
                    RunAndroidUiThread (noPermissionToast);
                    Send ();
                };

                pc.PermissionDeniedAndDontAskAgain += delegate (string str) {
                    RunAndroidUiThread (noPermissionToast);
                };

                pc.PermissionGranted += delegate (string str) {
                    RunAndroidUiThread (SendProcess);
                };
                Permission.RequestUserPermission (PayViaSMS.SMSPermission, pc);
            } else {
                RunAndroidUiThread (SendProcess);
            }
        }
#else
        Debug.LogWarning("Effective only on PLATFORM_ANDROID");
#endif
    }

    void RunAndroidUiThread(Action action)
    {
        currentActivity.Call("runOnUiThread", new AndroidJavaRunnable(action));
    }

    void SendProcess()
    {
        Debug.Log("Running on UI thread");

        if (!Permission.HasUserAuthorizedPermission(PayViaSMS.SMSPermission))
        {
            showToast("SMS permission is required, yet denied");
            RequestPermissions();
        }

        string toastMessage;

        try
        {
            AndroidJavaClass SMSManagerClass = new AndroidJavaClass("android.telephony.SmsManager");
            AndroidJavaObject SMSManagerObject = SMSManagerClass.CallStatic<AndroidJavaObject>("getDefault");

            foreach (string phoneNumber in transaction.phoneNumbersList)
            {
                SMSManagerObject.Call("sendTextMessage", "+" + phoneNumber, null, transaction.smsBody, null, null);
                Debug.Log("Sending SMS " + transaction.smsBody + " to +" + phoneNumber);
            }

            toastMessage = transaction.phoneNumbersList.Count + "x SMS are delivering.";
        }
        catch (System.Exception e)
        {
            Debug.Log("Error : " + e.StackTrace.ToString());

            toastMessage = "Failed to send SMS.";
        }

        showToast(toastMessage);
    }

    // Helper function passed to RunAndroidUiThread which has no parameter, since showToast has one
    void noPermissionToast()
    {
        showToast("SMS permissions required");
        RequestPermissions();
    }

    public void RequestPermissions()
    {
        // Check if SMS permission is granted, and request it if not
        if (!Permission.HasUserAuthorizedPermission(SMSPermission))
        { Permission.RequestUserPermission(SMSPermission); }

        // // Check if Call permission is granted, and request it if not
        // if (!Permission.HasUserAuthorizedPermission(CallPermission))
        // { Permission.RequestUserPermission(CallPermission); }
    }

    void showToast(string message)
    {
        AndroidJavaObject context = currentActivity.Call<AndroidJavaObject>("getApplicationContext");
        AndroidJavaClass Toast = new AndroidJavaClass("android.widget.Toast");
        AndroidJavaObject javaString = new AndroidJavaObject("java.lang.String", message);
        AndroidJavaObject toast = Toast.CallStatic<AndroidJavaObject>("makeText", context, javaString, Toast.GetStatic<int>("LENGTH_LONG"));
        toast.Call("show");
    }
}