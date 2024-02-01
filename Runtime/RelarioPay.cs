using System;
using System.Collections;
using JetBrains.Annotations;
using Relario.Network;
using Relario.Network.Models;
using UnityEngine;
using UnityEngine.Android;

namespace Relario
{
    public class RelarioPay : MonoBehaviour
    {
        public SubscriptionType subscriptionType = SubscriptionType.Daily;

        public Action<Transaction> OnSuccessfulPay;
        public Action<Transaction> OnPartialPay;
        public Action<Exception, Transaction> OnFailedPay;

        [Header("API Settings")] public string apiKey = "6c0da5e46c7a42aaa33e0aa28545475d";

        [Header("Request Settings")] [Tooltip("Should SMS be sent in background or should it switch to the sms app")]
        public bool switchToSmsApp;

        [Tooltip("Max number of times the app should check if transaction was successful or not")]
        public int maxNumberOfTransactionChecks = 5; // Updated variable name

        [Tooltip("Duration between each transaction status check")]
        public int intervalOfTransactionChecks = 10; // Updated variable name

        [Tooltip("Threshold ratio to consider a partial paid transaction as successful, defaults to 1")]
        public int minThresholdForSuccess = 1; // Updated variable name

        private bool _isPaused;
        private bool _isFocused;

        // private Transaction transaction;
        private PayBySMS _payBySms;
        private SubscriptionManager _subscriptionManager;

        private AndroidJavaClass _unityClass;
        private AndroidJavaObject _unityActivity;
        private AndroidJavaObject _applicationContext;
        private AndroidJavaObject _pluginInstance;
        private bool _isChecking;
        private string _currentTransactionId;
        private const string TransactionIdStateKey = "TransactionId";
        private const string PlayerPrefsKey = "subscriptionClaimTime";


        void Start()
        {
#if UNITY_ANDROID
            InitializePlugin("com.relario.subscription.SubscriptionManager");
            _payBySms = new PayBySMS(_unityActivity, _pluginInstance);
#endif
        }

        private void InitializePlugin(string pluginName)
        {
            _unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            _unityActivity = _unityClass.GetStatic<AndroidJavaObject>("currentActivity");
            _applicationContext = _unityActivity.Call<AndroidJavaObject>("getApplicationContext");
            _pluginInstance =
                new AndroidJavaObject(pluginName, _applicationContext, "6c0da5e46c7a42aaa33e0aa28545475d");
            if (_pluginInstance == null)
            {
                Debug.Log("Plugin Instance Error");
            }
        }

        void OnApplicationPause(bool pauseStatus)
        {
            _isPaused = pauseStatus;
            if (pauseStatus)
            {
                // App is going to the background
                // Save the state if a check is ongoing
                if (_isChecking)
                {
                    SaveCheckState(_currentTransactionId);
                }
            }
            else
            {
                // App is resuming from the background
                // Check if we need to resume checks
                LoadAndResumeCheckState();
            }
        }

        private void SaveCheckState(string transactionId)
        {
            // Use PlayerPrefs or another method to save the state
            PlayerPrefs.SetString(TransactionIdStateKey, transactionId);
            PlayerPrefs.Save();
        }

        private void LoadAndResumeCheckState()
        {
            // Check if there was an ongoing check
            if (!PlayerPrefs.HasKey(TransactionIdStateKey)) return;
            var transactionId = PlayerPrefs.GetString(TransactionIdStateKey);

            // Clear the saved state
            PlayerPrefs.DeleteKey(TransactionIdStateKey);
            PlayerPrefs.DeleteKey("ApiKey");
            // Resume the check
            StartTransactionCheck(transactionId);
        }

        public void StartTransactionCheck(string transactionId)
        {
            _isChecking = true;
            _currentTransactionId = transactionId;

            StartCoroutine(CheckTransactionUpdateCoroutine(transactionId));
        }


        public void RequestSmsPermission(Action<PermissionStatus> callback)
        {
            if (Permission.HasUserAuthorizedPermission(Utility.SMSPermission))
            {
                callback.Invoke(PermissionStatus.Granted);
                return;
            }

            var permissionCallbacks = new PermissionCallbacks();
            permissionCallbacks.PermissionDenied += _ => callback.Invoke(PermissionStatus.Rejected);
            permissionCallbacks.PermissionGranted += _ => callback.Invoke(PermissionStatus.Granted);
            permissionCallbacks.PermissionDeniedAndDontAskAgain +=
                _ => callback.Invoke(PermissionStatus.RejectedNoAsk);
            Permission.RequestUserPermission(Utility.SMSPermission, permissionCallbacks);
        }


        public void Subscribe(SubscriptionOptions options)
        {
#if UNITY_ANDROID
            RequestSmsPermission((s =>
            {
                if (s == PermissionStatus.Granted)
                {
                    _pluginInstance.Call("subscribe", options.IntervalRate, options.TimeUnit.ToString(), options.SmsCount,
                        options.ProductId, options.ProductName, options.CustomerId);
                }
                else
                {
                    throw new Exception("Permission is not granted");
                }
            }));
#endif
        }

        public void CancelSubscription(string productId)
        {
#if UNITY_ANDROID
            _pluginInstance.Call("cancelSubscription", productId);
#else
                throw new Exception("This works only on Android");
#endif
        }

        public void Pay(int smsCount,
            string productId,
            string productName,
            string customerId,
            Action<Exception, Transaction> callback)
        {
            InitiateTransaction(
                smsCount,
                productId,
                productName,
                customerId: customerId ?? Guid.NewGuid().ToString(),
                null,
                callback
            );
        }

        private void InitiateTransaction(
            int smsCount,
            string productId,
            string productName,
            string customerId,
            [CanBeNull] string customerMccMnc,
            Action<Exception, Transaction> callback)
        {
            Debug.Log("[Relario] Initiating relario transaction.");
            StartCoroutine(PaymentHttpApi.GetCurrentIP(ip =>
            {
                Debug.Log("[Relario] Got external IP.");
                Debug.Log("[Relario] Creating transaction body for " + PaymentType.sms);

                var newTransaction = new NewTransactionBody(
                    smsCount,
                    productId,
                    productName,
                    customerIpAddress: ip,
                    customerMccMnc,
                    customerId
                );

                Debug.Log("[Relario] Creating transaction body: " + newTransaction.ToJson());
                var validationMsg = newTransaction.Validate();
                if (validationMsg != null)
                {
                    var exception =
                        new Exception("[Relario] NewTransactionBody validation failed: " + validationMsg);
                    Debug.LogError(exception);
                    callback?.Invoke(exception, null);
                    return;
                }

                StartCoroutine(PaymentHttpApi.CreateTransaction(apiKey, newTransaction, ((exception, transaction) =>
                {
                    callback(exception, transaction);
                    try
                    {
                        _payBySms.Send(transaction, switchToSmsApp);
                        StartTransactionCheck(transaction.transactionId);
                    }
                    catch (Exception err)
                    {
                        Debug.LogError(err);
                        OnFailedPay?.Invoke(err, null);
                    }
                })));
            }));
        }

        private IEnumerator CheckTransactionUpdateCoroutine(
            string transactionId)
        {
            int attempts = 0;
            bool isTransactionSuccessful = false;
            Transaction finalTransaction = null;


            while (attempts < maxNumberOfTransactionChecks)
            {
                yield return PaymentHttpApi.GetTransaction(apiKey, transactionId,
                    (exception, transaction) =>
                    {
                        if (exception != null)
                        {
                            OnFailedPay.Invoke(exception, null);
                        }

                        // Check the transaction status
                        if (transaction.IsFullyPaid(minThresholdForSuccess))
                        {
                            isTransactionSuccessful = true;
                        }

                        finalTransaction = transaction;
                    });

                if (isTransactionSuccessful)
                {
                    break; // Exit the loop early if the transaction is confirmed successful
                }

                attempts++;
                yield return new WaitForSeconds(intervalOfTransactionChecks);
            }

            if (finalTransaction == null)
            {
                OnFailedPay.Invoke(new Exception("Failed to check transaction"), null);
            }
            else
            {
                if (isTransactionSuccessful)
                {
                    OnSuccessfulPay?.Invoke(finalTransaction);
                }
                else if (finalTransaction.IsPartiallyPaid())
                {
                    OnPartialPay?.Invoke(finalTransaction);
                }
                else
                {
                    OnFailedPay?.Invoke(null, finalTransaction);
                }
            }

            _currentTransactionId = null;
            _isChecking = false;
        }
        
        private DateTime LoadLastRewardTime()
        {
            // Load the last reward time and subscription type from player prefs
            if (PlayerPrefs.HasKey(PlayerPrefsKey))
            {
                string savedData = PlayerPrefs.GetString(PlayerPrefsKey);
                string[] dataParts = savedData.Split('|');

                if (dataParts.Length == 2)
                {
                    // Parse the saved data
                    DateTime savedTime = DateTime.Parse(dataParts[0]);
                    SubscriptionType savedSubscriptionType = (SubscriptionType)Enum.Parse(typeof(SubscriptionType), dataParts[1]);

                    // Update the current subscription type
                    subscriptionType = savedSubscriptionType;

                    return savedTime;
                }
            }

            // If not saved previously, return a default value based on debug mode
            DateTime defaultTime = WorldTimeAPI.Instance.GetCurrentDateTime();
            SaveLastRewardTime(defaultTime); // Save the default values

            return defaultTime;
        }

        private void SaveLastRewardTime(DateTime time)
        {
            // Save the last reward time and subscription type to player prefs
            string dataToSave = $"{time.ToString()}|{(int)subscriptionType}";
            PlayerPrefs.SetString(PlayerPrefsKey, dataToSave);
            PlayerPrefs.Save();
        }
    }

    public enum PermissionStatus
    {
        Granted,
        Rejected,
        RejectedNoAsk
    }

    public enum TimeUnit
    {
        MINUTES,
        HOURS,
        DAYS
    }
    
    public enum SubscriptionType
    {
        Daily,
        Weekly,
        Monthly
    }

    public class SubscriptionOptions
    {
        public string ProductId { get; }

        public string ProductName { get; }

        public int SmsCount { get; }

        public string CustomerId { get; }

        /**
         * Minimum interval rate in case of TimeUnit.Minutes is 15
         */
        public int IntervalRate { get; }

        public TimeUnit TimeUnit { get; }

        public SubscriptionOptions(string productId, string productName, int smsCount, string customerId,
            int intervalRate, TimeUnit timeUnit)
        {
            ProductId = productId;
            ProductName = productName;
            SmsCount = smsCount;
            CustomerId = customerId ?? Guid.NewGuid().ToString();
            if (timeUnit == TimeUnit.MINUTES && intervalRate < 15)
            {
                intervalRate = 15;
            }

            IntervalRate = intervalRate;
            TimeUnit = timeUnit;
        }
    }

    public class PayCallbacks
    {
        public delegate void SuccessfulPayDelegate(Transaction transaction);

        public delegate void PartialPayDelegate(Transaction transaction);

        public delegate void FailedPayDelegate(Exception exception, Transaction transaction);

        public SuccessfulPayDelegate OnSuccessfulPay { get; set; }
        public PartialPayDelegate OnPartialPay { get; set; }
        public FailedPayDelegate OnFailedPay { get; set; }

        public PayCallbacks()
        {
        }

        public PayCallbacks(
            SuccessfulPayDelegate onSuccess,
            PartialPayDelegate onPartial,
            FailedPayDelegate onFailure)
        {
            OnSuccessfulPay = onSuccess;
            OnPartialPay = onPartial;
            OnFailedPay = onFailure;
        }
    }
}