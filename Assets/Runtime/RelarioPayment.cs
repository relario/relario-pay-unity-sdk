using System;
using System.Collections;
using Relario.Network;
using Relario.Network.Models;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class TransactionEvent : UnityEvent<Transaction> { }

public class RelarioPayment : MonoBehaviour {
    // TODO: Move apiKey into Relario.API static class
    // public string apiKey = "468a803211ae498e8489a6fe6dda7dea";
    [HideInInspector] public string transactionId;
    [HideInInspector] public TransactionEvent transactionPaid = new TransactionEvent ();
    [HideInInspector] public TransactionEvent transactionFailed = new TransactionEvent ();

    private Transaction transaction;
    private PayViaSMS payViaSMS;
    private PayViaVoice payViaVoice;
    private SMSPaymentHandler paymentsHandler;

    private bool isPaid {
        get {
            if (this.transaction == null) {
                Debug.LogWarning ("[Relario] isPaid boolean is false for null transactions.");
                return false;
            }

            if (this.transaction.paymentType == PaymentType.sms) {
                return (this.transaction.smsCount <= this.transaction.payments.Count);
            } else {
                int totalCallDuration = 0;
                foreach (Payment payment in this.transaction.payments) {
                    totalCallDuration += payment.callDuration;
                }

                Debug.Log ("totalCallDuration " + totalCallDuration);

                return (this.transaction.callDuration <= totalCallDuration);
            }
        }
    }

    int retryCount = 0;

    void Start () {
        payViaSMS = gameObject.AddComponent<PayViaSMS> ();
        payViaVoice = gameObject.AddComponent<PayViaVoice> ();
        paymentsHandler = FindObjectOfType<SMSPaymentHandler>();
    }

    public void LaunchPayment () {
        this.ReadTransaction ((Exception exception, Transaction transaction) => {
            if (exception != null) {
                Debug.LogError (exception);
                return;
            }

            Debug.Log ("[Relario] LaunchPayment " + transaction.transactionId + " | " + JsonUtility.ToJson (transaction));
            try {
                if (transaction.paymentType == PaymentType.sms) {
                    payViaSMS.transaction = transaction;
                    payViaSMS.Send ();
                } else {
                    payViaVoice.transaction = transaction;
                    payViaVoice.Call ();
                }

                this.StartTracking ();
            } catch (System.Exception err) {
                Debug.LogError (err);
                this.StopTracking ();
            }
        });

        //let's reset retry count
        retryCount = 0;
    }
    private void StartTracking () {
        this.StopTracking ();
        Debug.Log ("[Relario] Start tracking " + transactionId);
        InvokeRepeating ("ReadUpdateTransaction", 1, 5F);
    }

    public void StopTracking () {
        Debug.Log ("[Relario] Stop tracking " + transactionId);
        CancelInvoke ("ReadUpdateTransaction");
    }

    private void ReadTransaction (Action<Exception, Transaction> callback) {
        StartCoroutine (
            API.readTransaction (paymentsHandler.apiKey, transactionId, callback)
        );
    }

    public void GetTransactionStatus (string _transactionId, Action<Exception, Transaction> callback) {
        StartCoroutine (
            API.readTransaction (paymentsHandler.apiKey, _transactionId, callback)
        );
    }

    private void ReadUpdateTransaction () {
        StartCoroutine (
            API.readTransaction (paymentsHandler.apiKey, transactionId, this.UpdateTransaction)
        );
    }

    private void UpdateTransaction (Exception exception, Transaction transaction) {
        if (exception != null) {
            Debug.LogError (exception);
            return;
        }

        this.transaction = transaction;

        if (this.isPaid) {
            transactionPaid.Invoke (this.transaction);
            this.StopTracking ();
            return;
        }

        if (retryCount > paymentsHandler._maxTrackingRetries-1)
        {
            transactionFailed.Invoke (this.transaction);
            this.StopTracking ();
        }
        retryCount++;
    }
}