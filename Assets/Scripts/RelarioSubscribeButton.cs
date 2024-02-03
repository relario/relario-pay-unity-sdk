using System;
using Relario.Network.Models;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Android;
using Relario;

public class RelarioSubscribeButton : MonoBehaviour {
    RelarioPay relarioPay;

    [Header("Subscription Options")]
    public string productId, productName;
    public int smsCount;
    public string customerId;

    public TimeUnit timeUnit;
    public int intervalRate;

    [Space]
    [Header("Callbacks")]
    public UnityEvent TransactionLaunch;
    public UnityEvent TransactionComplete, TransactionFailed, PartialTransaction;


    void Start () {
        relarioPay = FindObjectOfType<RelarioPay>();
        if (relarioPay != null)
        {
            relarioPay.OnSuccessfulPay += HandleTransactionPaid;
            relarioPay.OnPartialPay += HandlePartialPaymentsRecieved;
            relarioPay.OnFailedPay += HandleTransactionFailed;
        }else{
            Debug.LogError("Ensure you have an object with script 'RelarioPay' attached to it");
        }


        GetComponent<Button>().onClick.AddListener(OnPaymentClick);
    }

    void OnPaymentClick () {
        relarioPay.Subscribe(new SubscriptionOptions(productId, productName, smsCount, customerId, intervalRate, timeUnit));
        TransactionLaunch.Invoke();
    }

    void HandleTransactionPaid(Transaction transaction)
    {
        TransactionComplete.Invoke();
        Debug.Log($"Rewards Granted, {transaction.payments.Count} Payments recieved");
    }


    void HandlePartialPaymentsRecieved(Transaction transaction)
    {
        TransactionFailed.Invoke();
        Debug.Log("Reload airtime and try again");
    }

    void HandleTransactionFailed(Exception exception, Transaction transaction)
    {
        PartialTransaction.Invoke();
        Debug.Log($"Partial transactions, {transaction.payments.Count} recieved");
    }
}