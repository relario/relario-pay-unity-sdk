using System;
using System.Collections;
using Relario.Network;
using Relario.Network.Models;
using UnityEngine;
using UnityEngine.Networking;

public class RelarioTransaction : MonoBehaviour {
    // TODO: Move apiKey into Relario.API static class
    // public string apiKey;
    public PaymentType paymentType;
    public int smsCount = 1;
    public int callDuration = 5;
    public string customerMccmncc = "";
    public string customerId = "";
    private string customerIpAddress = "";
    public string productId = "";
    public string productName = "";

    private SMSPaymentHandler smsHandler;

    private void Start() {
        smsHandler = FindObjectOfType<SMSPaymentHandler>();
    }

    public void CreateTransaction (Action<Exception, Transaction> callback) {
        Debug.Log ("[Relario] Creating relario transaction.");
        StartCoroutine (GetIP ((IP) => {
            Debug.Log ("[Relario] Got external IP.");
            Debug.Log ("[Relario] Creating transaction body for " + this.paymentType);

            NewTransactionBody transactionBody;
            if (this.paymentType == PaymentType.sms) {
                transactionBody = new NewTransactionBody (
                    smsCount,
                    customerMccmncc,
                    customerId,
                    IP,
                    productId,
                    productName
                );
            } else {
                transactionBody = new NewTransactionBody (
                    callDuration,
                    customerMccmncc,
                    customerId,
                    IP,
                    productId,
                    productName
                );
            }

            string validationMsg = transactionBody.Validate ();
            if (validationMsg != null) {
                Exception exception = new Exception ("[Relario] NewTransactionBody validation failed: " + validationMsg);
                Debug.LogError (exception);
                callback (exception, null);
                return;
            } else {
                StartCoroutine (API.createTransaction (smsHandler.apiKey, transactionBody, callback));
            }
        }));
    }

    IEnumerator GetIP (Action<string> callback) {
        using (UnityWebRequest webRequest = UnityWebRequest.Get ("https://checkip.amazonaws.com/")) {
            Debug.Log ("[Relario] Getting IP.");
            webRequest.SendWebRequest ();

            while (!webRequest.isDone) {
                yield return null;
            }

            if (!string.IsNullOrEmpty (webRequest.error)) {
                Debug.LogError ("[Relario] Failed to obtain external IP: " + webRequest.error);
            } else if (webRequest.isDone) {
                callback (webRequest.downloadHandler.text.Replace ("\n", ""));
            }
        }
    }
}