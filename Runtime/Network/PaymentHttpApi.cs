using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Relario.Network.Models;
using UnityEngine;
using UnityEngine.Networking;

namespace Relario.Network
{
    public static class PaymentHttpApi
    {
        private const string BASE_URL = "https://payment.relario.com/api/web/";

        public static IEnumerator CreateTransaction(
            string apiKey,
            NewTransactionBody transactionBody,
            Action<Exception, Transaction> callback
        )
        {
            string url = BASE_URL + "transactions";

            var request = new UnityWebRequest(url, "POST");

            string body = JsonUtility.ToJson(transactionBody);

            byte[] bodyRaw = Encoding.UTF8.GetBytes(body);
            request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);

            Debug.Log("[Relario] POST " + body);

            yield return request.SendWebRequest();

            if (request.error != null)
            {
                Debug.Log("[Relario] Request error: " + request.error);
                Debug.Log("[Relario] Request error body: " + request.downloadHandler.text);
                Exception exception = new Exception("[Relario] Request error - body: " + request.downloadHandler.text);
                callback(exception, null);
            }
            else
            {
                Debug.Log("[Relario] Transaction created");
                Debug.Log("[Relario] Response Body: " + request.downloadHandler.text);

                Transaction relarioTransaction = Transaction.FromJSON(request.downloadHandler.text);
                callback(null, relarioTransaction);
            }
        }

        public static IEnumerator GetTransaction(
            string apiKey,
            string transactionId,
            Action<Exception, Transaction> callback
        )
        {
            string url = BASE_URL + "transactions/" + transactionId;

            var request = new UnityWebRequest(url, "GET");

            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);

            Debug.Log("[Relario] GET " + url);

            yield return request.SendWebRequest();

            if (request.error != null)
            {
                Debug.Log("[Relario] Request error: " + request.error);
                Debug.Log("[Relario] Request error body: " + request.downloadHandler.text);
                Exception exception = new Exception("[Relario] Request error - body: " + request.downloadHandler.text);
                callback(exception, null);
            }
            else
            {
                Debug.Log("[Relario] Transaction retrieved");
                Debug.Log("[Relario] Response Body: " + request.downloadHandler.text);

                Transaction relarioTransaction = Transaction.FromJSON(request.downloadHandler.text);
                callback(null, relarioTransaction);
            }
        }
        

        public static IEnumerator GetPrices(string apiKey, string ipAddress, Action<Exception, SMSPricesResponse> callback)
        {
            string url = BASE_URL + $"rates?customerIpAddress={ipAddress}";

            var request = new UnityWebRequest(url, "GET");

            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);

            Debug.Log("[Relario] GET SMS Prices " + url);

            yield return request.SendWebRequest();

            if (request.error != null)
            {
                Debug.Log("[Relario] Request error: " + request.error);
                Debug.Log("[Relario] Request error body: " + request.downloadHandler.text);
                Exception exception = new Exception("[Relario] Request error - body: " + request.downloadHandler.text);
                callback(exception, null);
            }
            else
            {
                Debug.Log("[Relario] SMS Prices retrieved");
                Debug.Log("[Relario] Response Body: " + request.downloadHandler.text);

                try
                {
                    SMSPricesResponse smsPricesResponse = JsonUtility.FromJson<SMSPricesResponse>(request.downloadHandler.text);
                    callback(null, smsPricesResponse);
                }
                catch (Exception ex)
                {
                    Debug.LogError("[Relario] Failed to parse SMS prices response: " + ex.Message);
                    callback(new Exception("Failed to parse SMS prices response"), null);
                }
            }
        }
        
        public static IEnumerator GetCurrentIP(Action<string> callback)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get("https://checkip.amazonaws.com/"))
            {
                Debug.Log("[Relario] Getting IP.");
                webRequest.SendWebRequest();

                while (!webRequest.isDone)
                {
                    yield return null;
                }

                if (!string.IsNullOrEmpty(webRequest.error))
                {
                    Debug.LogError("[Relario] Failed to obtain external IP: " + webRequest.error);
                }
                else if (webRequest.isDone)
                {
                    callback(webRequest.downloadHandler.text.Replace("\n", ""));
                }
            }
        }
    }


}