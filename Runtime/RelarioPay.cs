using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Relario.Network;
using Relario.Network.Models;
using UnityEngine;
using UnityEngine.Android;

namespace Relario
{
    public class RelarioPay : MonoBehaviour
    {
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
        private PaymentManager _paymentManager;
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
            _paymentManager = new PaymentManager(_unityActivity, _pluginInstance);
            // SubscriptionManager = new SubscriptionManager(_unityActivity, _pluginInstance);
#endif
        }

        private void InitializePlugin(string pluginName)
        {
            _unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            _unityActivity = _unityClass.GetStatic<AndroidJavaObject>("currentActivity");
            _applicationContext = _unityActivity.Call<AndroidJavaObject>("getApplicationContext");
            _pluginInstance =
                new AndroidJavaObject(pluginName, _applicationContext, apiKey);
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

        private void StartTransactionCheck(string transactionId)
        {
            _isChecking = true;
            _currentTransactionId = transactionId;

            StartCoroutine(CheckTransactionUpdateCoroutine(transactionId));
        }

        private void RequestSmsPermission(Action<PermissionStatus> callback)
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
                    _pluginInstance.Call("subscribe", options.IntervalRate, options.TimeUnit.ToString(),
                        options.SmsCount,
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
                    callback?.Invoke(exception, transaction);
                    try
                    {
                        _paymentManager.Send(transaction, switchToSmsApp);
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


        public List<Transaction> GetSubscriptionTransactions()
        {
            var transactionsJson = _pluginInstance.Call<string>("retrieveTransactions");
            var wrappedArrayJson = JsonArrayResult<Transaction>.WrapJsonArray(transactionsJson);
            var transactionsList =
                JsonUtility.FromJson<JsonArrayResult<Transaction>>(wrappedArrayJson);
            return new List<Transaction>(transactionsList.result);
        }

        public Transaction GetLastSubscriptionTransaction()
        {
            var transactions = GetSubscriptionTransactions();
            var sortedTransactions = transactions.OrderBy(t => t.createdAt).ToList();
            var lastTransaction = sortedTransactions.LastOrDefault();
            return lastTransaction;
        }

        public bool IsTransactionSuccessful(Transaction transaction)
        {
            return transaction.IsFullyPaid(minThresholdForSuccess);
        }


        public void RetryTransaction(string transactionId, Action<Exception, Transaction> callback)
        {
            StartCoroutine(PaymentHttpApi.GetTransaction(apiKey, transactionId, (exception, transaction) =>
            {
                callback?.Invoke(exception, transaction);
                RetryTransaction(transaction);
            }));
        }

        public void RetryTransaction(Transaction transaction)
        {
            var requiredPayments = transaction.smsCount - transaction.payments.Count;
            try
            {
                _paymentManager.Send(transaction, switchToSmsApp, requiredPayments);
                StartTransactionCheck(transaction.transactionId);
            }
            catch (Exception err)
            {
                Debug.LogError(err);
                OnFailedPay?.Invoke(err, null);
            }
        }

        public void GetSmsPrices(Action<Exception, List<SMSPricing>> callback)
        {
            StartCoroutine(PaymentHttpApi.GetCurrentIP(ip =>
                StartCoroutine(PaymentHttpApi.GetPrices(apiKey, ip, (exception, response) =>
                {
                    if (exception != null)
                    {
                        callback(exception, null);
                        return;
                    }

                    callback(null, response.rates);
                }))
            ));
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

    [System.Serializable]
    public class JsonArrayResult<T>
    {
        public T[] result;

        public static string WrapJsonArray(string json)
        {
            return "{\"result\":" + json + "}";
        }
    }
}