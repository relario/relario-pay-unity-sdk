using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Relario.Network;
using Relario.Network.Models;
using UnityEngine.Networking;

namespace Relario
{

    [RequireComponent(typeof(RelarioPay))]
    public class SubscriptionManager : MonoBehaviour
    {
        public SubscriptionType subscriptionType = SubscriptionType.Daily;

        [Header("Transaction")]
        public PaymentType paymentType;
        public int smsCount = 1;
        public int callDuration = 5;
        public string customerMccmncc = "";
        public string productId = "";
        public string productName = "";
        [Header("Transaction (Optional)")]
        public string customerId = "";
        private string customerIpAddress = "";

        [Space]
        [Header("Callbacks")]
        public UnityEvent TransactionLaunched;
        public UnityEvent RewardReadyEvent;


        //subscribe to these events
        public delegate void TransactionEventHandler(Transaction transaction);
        public static event TransactionEventHandler TransactionPaid;
        public static event TransactionEventHandler TransactionFailed;

        public static event TransactionEventHandler PartialPaymentsExists;
        public static event TransactionEventHandler PartialPaymentsRecieved;
        public static event TransactionEventHandler PartialTransactionPaid;
        //sms pricing events
        public static event Action<Exception, SMSPricesResponse> SMSPricesFetched;

        [Header("Debug")]
        // Flag to determine whether to use mock transactions and extra logging
        public bool debugMode = false; // Toggle debug mode
        public bool makeCompleteTransaction = true;
        [Tooltip("Define how many payments should go through. This number shouldn't be more than smsCount")]
        public int numSuccessfulPayments = 1;
        public bool alwaysCheckSubscription = false;
        public bool logRemainingDays = false;
        public bool logDaysDifference = false;

        private DateTime lastRewardTime;
        private RelarioPay relarioPayment;
        private const string PlayerPrefsKey = "subscriptionClaimTime";
        Coroutine periodicCoroutine;
        bool transactionInProgress = false;

        private void OnEnable()
        {
            RewardReadyEvent.AddListener(LaunchTransaction);
        }

        private void OnDisable()
        {
            RewardReadyEvent.RemoveListener(LaunchTransaction);
        }

        private void Start()
        {
            relarioPayment = this.GetComponent<RelarioPay>();

            //let's get any cached partial subscription payments
            if (PlayerPrefs.GetString("sub_partialPayments") != "")
            {
                GetPartialPaymentFromServer();
            }

            // Load last reward time from player prefs
            lastRewardTime = LoadLastRewardTime();

            if (PlayerPrefs.GetInt("Subscribed") == 1 && alwaysCheckSubscription)
            {
                // Start the periodic check coroutine
                periodicCoroutine = StartCoroutine(CheckRewardsPeriodically());
            }
            else if (PlayerPrefs.GetInt("Subscribed") == 1)
            {
                CheckSubscriptionRewards();
            }

            //you can get sms prices too here
            // StartCoroutine(GetSMSPrices());
        }

        void GetPartialPaymentFromServer()
        {
            // string cacheTransactionId = PlayerPrefs.GetString("sub_partialPayments");
            // relarioPayment.GetTransactionStatus(cacheTransactionId, (Exception exception, Transaction transaction) =>
            // {
            //     if (exception != null)
            //     {
            //         Debug.LogError(exception);
            //         return;
            //     }
            //
            //     Debug.Log("[Relario] Get Partial Payment " + transaction.transactionId + " | " + JsonUtility.ToJson(transaction));
            //     try
            //     {
            //         if (transaction.paymentType == PaymentType.sms)
            //         {
            //             partialTransaction = transaction;
            //         }
            //         else
            //         {
            //             partialTransaction = transaction;
            //         }
            //     }
            //     catch (System.Exception err)
            //     {
            //         Debug.LogError(err);
            //     }
            // });

            // PartialPaymentsExists.Invoke(partialTransaction);
        }

        public void CreateTransaction(Action<Exception, Transaction> callback)
        {
            // // Check the flag to decide whether to use mock transactions
            // if (debugMode)
            // {
            //     // Use mock transaction
            //     Transaction mockTransaction = MockTransaction.CreateMockTransaction(smsCount, numSuccessfulPayments);
            //     callback(null, mockTransaction);
            // }
            // else
            // {
            //     Debug.Log("[Relario] Creating relario transaction.");
            //     StartCoroutine(GetIP((IP) =>
            //     {
            //         Debug.Log("[Relario] Got external IP.");
            //         Debug.Log("[Relario] Creating transaction body for " + this.paymentType);
            //
            //         NewTransactionBody transactionBody;
            //         if (this.paymentType == PaymentType.sms)
            //         {
            //             transactionBody = new NewTransactionBody(
            //                 paymentType,
            //                 smsCount,
            //                 customerMccmncc,
            //                 customerId,
            //                 IP,
            //                 productId,
            //                 productName
            //             );
            //         }
            //         else
            //         {
            //             transactionBody = new NewTransactionBody(
            //                 paymentType,
            //                 callDuration,
            //                 customerMccmncc,
            //                 customerId,
            //                 IP,
            //                 productId,
            //                 productName
            //             );
            //         }
            //
            //         string validationMsg = transactionBody.Validate();
            //         if (validationMsg != null)
            //         {
            //             Exception exception = new Exception("[Relario] NewTransactionBody validation failed: " + validationMsg);
            //             Debug.LogError(exception);
            //             callback(exception, null);
            //             return;
            //         }
            //         else
            //         {
            //             StartCoroutine(PaymentHttpApi.createTransaction(smsHandler.apiKey, transactionBody, callback));
            //         }
            //     }));
            // }
        }

        // IEnumerator GetSMSPrices()
        // {
            // Fetch the IP address
            // yield return StartCoroutine(GetIP(ipAddress =>
            // {
            //     // Use the fetched IP address to get SMS prices
            //     StartCoroutine(PaymentHttpApi.GetSMSPrices(apiKey, ipAddress, (exception, smsPricesResponse) =>
            //     {
            //         if (exception != null)
            //         {
            //             Debug.LogError("[Relario] Failed to get SMS prices: " + exception.Message);
            //         }
            //         else
            //         {
            //             Debug.Log("[Relario] SMS Prices fetched successfully.");
            //             SMSPricesFetched?.Invoke(null, smsPricesResponse);
            //         }
            //     }));
            // }));
        // }

        // IEnumerator GetIP(Action<string> callback)
        // {
            // using (UnityWebRequest webRequest = UnityWebRequest.Get("https://checkip.amazonaws.com/"))
            // {
            //     Debug.Log("[Relario] Getting IP.");
            //     webRequest.SendWebRequest();
            //
            //     while (!webRequest.isDone)
            //     {
            //         yield return null;
            //     }
            //
            //     if (!string.IsNullOrEmpty(webRequest.error))
            //     {
            //         Debug.LogError("[Relario] Failed to obtain external IP: " + webRequest.error);
            //     }
            //     else if (webRequest.isDone)
            //     {
            //         callback(webRequest.downloadHandler.text.Replace("\n", ""));
            //     }
            // }
        // }

        public void SubscribeRewards()
        {
            LaunchTransaction();
        }

        private IEnumerator CheckRewardsPeriodically()
        {
            while (true)
            {
                CheckSubscriptionRewards();
                yield return new WaitForSeconds(1f); // Adjust the interval as needed
            }
        }

        // Check if rewards are ready based on the subscription type and time difference
        private void CheckSubscriptionRewards()
        {
            DateTime currentDateTime = debugMode ? DateTime.Now : WorldTimeAPI.Instance.GetCurrentDateTime();

            lastRewardTime = LoadLastRewardTime();

            // Calculate the time difference between now and the last reward time
            TimeSpan timeDifference = currentDateTime - lastRewardTime;
            // Calculate the total days directly from the TimeSpan
            int totalDays = Mathf.FloorToInt((float)(timeDifference.TotalDays * Math.Sign(timeDifference.TotalDays)));

            if (logDaysDifference)
            {
                Debug.Log($"Time difference is current date {currentDateTime} and last {lastRewardTime}");
                Debug.Log($"Days difference is now {totalDays}");
            }

            if (logRemainingDays)
            {
                // Calculate the remaining days based on the subscription interval
                int remainingDays = 0;

                switch (subscriptionType)
                {
                    case SubscriptionType.Daily:
                        remainingDays = 1 - (totalDays % 1); // Remaining days until next daily reward
                        break;

                    case SubscriptionType.Weekly:
                        remainingDays = 7 - (totalDays % 7); // Remaining days until next weekly reward
                        break;

                    case SubscriptionType.Monthly:
                        int daysInMonth = DateTime.DaysInMonth(currentDateTime.Year, currentDateTime.Month);
                        remainingDays = daysInMonth - (totalDays % daysInMonth); // Remaining days until next monthly reward
                        break;
                }


                // Log the remaining days
                Debug.Log($"Remaining days for subscription type {subscriptionType}: {remainingDays}");

            }

            // Check if the time difference is greater than or equal to the subscription interval
            switch (subscriptionType)
            {
                case SubscriptionType.Daily:
                    if (totalDays >= 1)
                    {
                        RewardReadyEvent?.Invoke();
                        GetAndSaveLastRewardTime();
                    }
                    break;

                case SubscriptionType.Weekly:
                    if (totalDays >= 7)
                    {
                        RewardReadyEvent?.Invoke();
                        GetAndSaveLastRewardTime();
                    }
                    break;

                case SubscriptionType.Monthly:
                    int daysInMonth = DateTime.DaysInMonth(currentDateTime.Year, currentDateTime.Month);
                    if (totalDays >= daysInMonth)
                    {
                        RewardReadyEvent?.Invoke();
                        GetAndSaveLastRewardTime();
                    }
                    break;
            }
        }

        private void LaunchTransaction()
        {
            if (!transactionInProgress)
            {
                if (Application.isEditor || debugMode)
                {
                    Debug.LogWarning("Cannot send SMS in editor, using a mock transaction");
                    LaunchMockTransaction();
                }
                else
                {
                    TransactionLaunched.Invoke();
                    CreateTransaction(TransactionCallback);
                }
            }
            transactionInProgress = true;
        }

        //This is for logging purposes and can be removed if undesired
        void LaunchMockTransaction()
        {
            if (numSuccessfulPayments > smsCount)
            {
                Debug.LogWarning("The number of full payments can't be more than the smsCount");
                numSuccessfulPayments = smsCount;
            }

            if (numSuccessfulPayments == smsCount || makeCompleteTransaction)
            {
                numSuccessfulPayments = smsCount; //let's ensure the transaction is complete in inspector
                CreateTransaction(OnTransactionPaidCallback);
            }
            else
            {
                CreateTransaction(OnTransactionFailedCallback);
            }

        }

        void TransactionCallback(Exception exception, Transaction transaction)
        {
            // if (exception != null)
            // {
            //     Debug.LogError(exception.Message);
            //     return;
            // }
            //
            // Debug.Log("Created transaction " + transaction.transactionId);
            // _successfulTransfers = 0;
            // relarioPayment.transactionId = transaction.transactionId;
            // relarioPayment.transactionPaid.AddListener(OnTransactionPaid);
            // relarioPayment.transactionFailed.AddListener(OnTransactionFailed);
            //
            // relarioPayment.LaunchPayment();
        }

        private void OnTransactionPaidCallback(Exception exception, Transaction transaction)
        {
            OnTransactionPaid(transaction);
        }

        private void OnTransactionFailedCallback(Exception exception, Transaction transaction)
        {
            OnTransactionFailed(transaction);
        }

        void OnTransactionPaid(Transaction transaction)
        {
            if (PlayerPrefs.GetInt("Subscribed") != 1)
            {
                PlayerPrefs.SetInt("Subscribed", 1);
            }
            // Let's invoke event -- maybe your rewards and stuff can go here
            TransactionPaid.Invoke(transaction);
            transactionInProgress = false;

            //For periodic checking of rewards
            if (alwaysCheckSubscription)
            {
                //you can remove this if you want
                periodicCoroutine = StartCoroutine(CheckRewardsPeriodically());
            }
            else
            {
                //let's begin checking for rewards once payment has been recieved by server
                CheckSubscriptionRewards();
            }
            GetAndSaveLastRewardTime();
        }

        void OnTransactionFailed(Transaction transaction)
        {
            // _successfulTransfers = transaction.payments.Count;
            //
            // Debug.LogError($"Only {_successfulTransfers} sms was sent succesfully");
            //
            // //you may want to save any partial payments if transaction fails
            // //so that players can later use this
            // if (_successfulTransfers > 0)
            // {
            //     Debug.Log("There were partial payments");
            //     partialTransaction = transaction;
            //
            //     // Invoke the PartialPayments event with the transaction argument
            //     PartialPaymentsRecieved?.Invoke(transaction);
            //     PlayerPrefs.SetString("sub_partialPayments", transaction.transactionId);
            // }
            // else
            // {
            //     // Invoke the TransactionFailed event with the transaction argument
            //     TransactionFailed?.Invoke(transaction);
            // }
            //
            // relarioPayment.StopTracking();
            // transactionInProgress = false;
            //
            // // Stop checking coroutine
            // if (periodicCoroutine != null)
            // { StopCoroutine(periodicCoroutine); }
            // GetAndSaveLastRewardTime();
        }

        public void LaunchPartialPayment()
        {
            // if (!debugMode)
            // {
            //     partialTransaction.RemovePaymentsWithMatchingDdi();
            //
            //     relarioPayment.transactionId = partialTransaction.transactionId;
            //     relarioPayment.transactionPaid.AddListener(OnPartialTransactionPaid);
            //     relarioPayment.transactionFailed.AddListener(OnTransactionFailed);
            //     relarioPayment.LaunchPayment();
            // }
            // else
            // {
            //     makeCompleteTransaction = true;
            //     OnPartialTransactionPaid(partialTransaction);
            // }

        }

        void OnPartialTransactionPaid(Transaction transaction)
        {
            PlayerPrefs.SetString("sub_partialPayments", "");
            // Let's invoke event -- maybe your rewards and stuff can go here
            PartialTransactionPaid.Invoke(transaction);
            transactionInProgress = false;

            //For periodic checking of rewards
            if (alwaysCheckSubscription)
            {
                //you can remove this if you want
                periodicCoroutine = StartCoroutine(CheckRewardsPeriodically());
            }
            else
            {
                //let's begin checking for rewards once payment has been recieved by server
                CheckSubscriptionRewards();
            }

            GetAndSaveLastRewardTime();
        }

        public void GetAndSaveLastRewardTime()
        {
            // Save reward info based on debug mode
            if (debugMode)
            {
                lastRewardTime = DateTime.Now;
            }
            else
            {
                lastRewardTime = WorldTimeAPI.Instance.GetCurrentDateTime();
            }

            SaveLastRewardTime(lastRewardTime);
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
            DateTime defaultTime = debugMode ? DateTime.Now : WorldTimeAPI.Instance.GetCurrentDateTime();
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

        // Skip days for debugging--------------------------
        public void SkipDays()
        {
            int daysToSkip = 0;

            // Determine the number of days to skip based on the subscription type
            switch (subscriptionType)
            {
                case SubscriptionType.Daily:
                    daysToSkip = 2;
                    break;

                case SubscriptionType.Weekly:
                    daysToSkip = 8;
                    break;

                case SubscriptionType.Monthly:
                    daysToSkip = DateTime.DaysInMonth(lastRewardTime.Year, lastRewardTime.Month) + 1;
                    break;
            }

            // Skip the determined number of days
            lastRewardTime = lastRewardTime.AddDays(daysToSkip);

            // Save the last reward time to player prefs only once after all modifications
            SaveLastRewardTime(lastRewardTime);

            // Log the number of days skipped
            Debug.Log($"Skipped {daysToSkip} days for subscription type {subscriptionType}");
            Debug.Log($"New lastRewardTime: {lastRewardTime}");


            // Stop checking coroutine
            if (periodicCoroutine != null)
            {
                StopCoroutine(periodicCoroutine);
            }
            // Check for available rewards after skipping days
            CheckSubscriptionRewards();
            // Reset the rewarded time to avoid any conflicts in time management
            ResetLastRewardTime();
            // Start Coroutine again
            // periodicCoroutine = StartCoroutine(CheckRewardsPeriodically());
            // StartCoroutine(ResetText());
        }

        void ResetLastRewardTime()
        {
            // Save reward info based on debug mode
            if (debugMode)
            {
                lastRewardTime = DateTime.Now;
            }
            else
            {
                lastRewardTime = WorldTimeAPI.Instance.GetCurrentDateTime();
            }

            SaveLastRewardTime(lastRewardTime);
        }

        IEnumerator ResetText()
        {
            yield return new WaitForSeconds(5f);
            yield break;
        }
    }
}