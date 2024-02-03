using System;
using Relario.Network.Models;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Android;

[RequireComponent(typeof(RelarioPayment))]
[RequireComponent(typeof(RelarioTransaction))]
public class RelarioPaymentButton : MonoBehaviour {
    // public bool payOnTap = true;
    RelarioPayment relarioPayment;
    private SMSPaymentHandler paymentsHandler;
    private const string SMSPermission = "android.permission.SEND_SMS"; // SMS permission
    // private const string CallPermission = "android.permission.CALL_PHONE"; // Call permission
    private int _successfulTransfers;

    [Header("Callbacks")]
    public UnityEvent onTransactionLaunch;
    public UnityEvent onTransactionComplete, onTransactionFailed;


    void Start () {
        relarioPayment = GetComponent<RelarioPayment> ();
        if (!relarioPayment)
        { relarioPayment = gameObject.AddComponent<RelarioPayment> (); }
        paymentsHandler = FindObjectOfType<SMSPaymentHandler>();
        if (!paymentsHandler)
        { Debug.LogError("You need to create a new GameObject and add the SMSHandler script to it"); }

        GetComponent<Button>().onClick.AddListener(OnPaymentClick);
    }

    void OnPaymentClick () {
        if (PlayerPrefs.GetInt("partial_Payments") >= GetComponent<RelarioTransaction>().smsCount)
        {
            OnCounterTransactionPaid();
        }else{
            //let's request permissions if we aren't switching app
            if (!paymentsHandler.switchToSmsApp)
            {
                RequestPermissions();
            }

            if (String.IsNullOrEmpty (paymentsHandler.apiKey)) {
                Debug.LogError ("No API key provided for the RelarioTransaction Script");
            } else {
                onTransactionLaunch.Invoke();
                GetComponent<RelarioTransaction>().CreateTransaction (TransactionCallback);
            }
        }
    }

    void TransactionCallback (Exception exception, Transaction transaction) {
        if (exception != null) {
            Debug.LogError (exception);
            return;
        }

        Debug.Log ("Created transaction " + transaction.transactionId);
        _successfulTransfers = 0;
        relarioPayment.transactionId = transaction.transactionId;
        relarioPayment.transactionPaid.AddListener (OnTransactionPaid);
        relarioPayment.transactionFailed.AddListener (OnTransactionFailed);
        
        relarioPayment.LaunchPayment();
    }

    void OnTransactionPaid (Transaction transaction) {
        onTransactionComplete.Invoke();
        // payOnTap = false;
    }

    void OnCounterTransactionPaid ()
    {
        onTransactionComplete.Invoke();
    }

    void OnTransactionFailed (Transaction transaction) {
        _successfulTransfers = transaction.payments.Count;

        Debug.LogError($"Only {_successfulTransfers} sms was sent succesfully");

        //you may want to save any partial payments if transaction fails
        //so that players can later use this
        if (_successfulTransfers > 0)
        {
            Debug.Log("There were partial payments");

            int partialPayments = PlayerPrefs.GetInt("partial_Payments");
            partialPayments += _successfulTransfers;
            PlayerPrefs.SetInt("partial_Payments", partialPayments);
            UIController.instance.UpdatePartialPaymentsText();
            UIController.instance.CheckPartialPayments();
        }

        onTransactionFailed.Invoke();
        relarioPayment.StopTracking();
        UIController.instance.UpdateTimerText($"There was an issue with transactions");
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
}