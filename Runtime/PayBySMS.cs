using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Android;
using Relario.Network.Models;

namespace Relario
{
    public class PayBySMS
    {
        readonly AndroidJavaObject _currentActivity;
        private AndroidJavaObject _pluginInstance;
        private Transaction _transaction;

        public PayBySMS(AndroidJavaObject unityActivity, AndroidJavaObject pluginInstance)
        {
#if UNITY_ANDROID

            this._pluginInstance = pluginInstance;
            this._currentActivity = unityActivity;
#endif
        }

        public void Send(Transaction transaction, bool switchToSmsApp)
        {
            if (Application.isEditor)
            {
                throw new Exception("Can't send SMS in editor");
            }

#if UNITY_ANDROID
            Debug.LogWarning("[Relario] SMS Pay");
            this._transaction = transaction;
            // If switchToSmsApp is true, use SMSHandler to generate SMS URL
            if (switchToSmsApp)
            {
                // Convert the List<string> to an array of strings
                string[] phoneNumbersArray = transaction.phoneNumbersList.ToArray();

                string smsUrl = Utility.GetClickToSmsUrl(phoneNumbersArray, transaction.smsBody);
                Application.OpenURL(smsUrl);
            }
            else
            {
                if (!Permission.HasUserAuthorizedPermission(Utility.SMSPermission))
                {
                    PermissionCallbacks pc = new PermissionCallbacks();

                    pc.PermissionDenied += delegate(string str)
                    {
                        RunAndroidUiThread(noPermissionToast);
                        Send(transaction, switchToSmsApp);
                    };

                    pc.PermissionDeniedAndDontAskAgain += delegate(string str)
                    {
                        RunAndroidUiThread(noPermissionToast);
                    };

                    pc.PermissionGranted += delegate(string str) { RunAndroidUiThread(SendProcess); };
                    Permission.RequestUserPermission(Utility.SMSPermission, pc);
                }
                else
                {
                    RunAndroidUiThread(SendProcess);
                }
            }
#else
            Debug.LogWarning("Effective only on UNITY_ANDROID");
#endif
        }

        void RunAndroidUiThread(Action action)
        {
            _currentActivity.Call("runOnUiThread", new AndroidJavaRunnable(action));
        }

        private void SendProcess()
        {
            Debug.Log("Running on UI thread");

            if (!Permission.HasUserAuthorizedPermission(Utility.SMSPermission))
            {
                Debug.Log("SMS permission is required, yet denied");
                RequestPermissions();
            }

            string toastMessage;

            try
            {
                SmsStatusCallback smsStatusCallback = new SmsStatusCallback();
                smsStatusCallback.OnSmsStatus += (successCount, failedCount) =>
                {
                    Debug.Log("Success: " + successCount + ", Failed: " + failedCount);
                };
                _pluginInstance.Call("sendSms", _transaction.phoneNumbersList.ToArray(), _transaction.smsBody, smsStatusCallback);
                toastMessage = _transaction.phoneNumbersList.Count + "x SMS are delivering.";
            }
            catch (Exception e)
            {
                Debug.Log("Error");
                toastMessage = "Failed to send SMS.";
                // Debug.Log(toastMessage);

                throw e;
            }

            Debug.Log(toastMessage);
        }

        // Helper function passed to RunAndroidUiThread which has no parameter, since showToast has one
        void noPermissionToast()
        {
            Debug.Log("SMS permissions required");
            RequestPermissions();
        }

        public void RequestPermissions()
        {
            // Check if SMS permission is granted, and request it if not
            if (!Permission.HasUserAuthorizedPermission(Utility.SMSPermission))
            {
                Permission.RequestUserPermission(Utility.SMSPermission);
            }

            // // Check if Call permission is granted, and request it if not
            // if (!Permission.HasUserAuthorizedPermission(CallPermission))
            // { Permission.RequestUserPermission(CallPermission); }
        }

        void showToast(string message)
        {
            AndroidJavaObject context = _currentActivity.Call<AndroidJavaObject>("getApplicationContext");
            AndroidJavaClass Toast = new AndroidJavaClass("android.widget.Toast");
            AndroidJavaObject javaString = new AndroidJavaObject("java.lang.String", message);
            AndroidJavaObject toast = Toast.CallStatic<AndroidJavaObject>("makeText", context, javaString,
                Toast.GetStatic<int>("LENGTH_LONG"));
            toast.Call("show");
        }
    }

    class SmsStatusCallback : AndroidJavaProxy
    {
        public Action<int, int> OnSmsStatus;

        public SmsStatusCallback() : base("com.relario.subscription.SmsStatusCallback")
        {
        }

        public void onBatchSmsStatus(int successCount, int failureCount)
        {
            Debug.Log("OnBatchSmsStatus Callback, Success: " + successCount + ", Failed: " + failureCount);
            OnSmsStatus?.Invoke(successCount, failureCount);
        }
    }
}