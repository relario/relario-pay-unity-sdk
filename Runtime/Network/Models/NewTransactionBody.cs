using System;
using System.Net;
using JetBrains.Annotations;
using UnityEngine;

namespace Relario.Network.Models
{
    [Serializable]
    public class NewTransactionBody
    {
        public int smsCount;
        [CanBeNull] public string customerMccMnc;
        public string customerId;
        public string customerIpAddress;
        public string productId;
        public string productName;
        public string paymentType = "sms";

        public NewTransactionBody(
            int smsCount,
            string productId,
            string productName,
            string customerIpAddress,
            string customerMccMnc,
            string customerId
        )
        {
            this.smsCount = smsCount;
            this.customerMccMnc = customerMccMnc;
            this.customerId = customerId;
            this.customerIpAddress = customerIpAddress;
            this.productId = productId;
            this.productName = productName;
        }

        public string DebugPrint()
        {
            return "smsCount: " + smsCount + " | " +
                   "customerMccMnc: " + customerMccMnc + " | " +
                   "customerId: " + customerId + " | " +
                   "customerIpAddress: " + customerIpAddress + " | " +
                   "productId: " + productId + " | " +
                   "productName: " + productName + " | " +
                   "paymentType: " + paymentType + " | ";
        }

        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        public string Validate()
        {
            if (smsCount <= 0)
            {
                return "smsCount should be more than 0 for SMS payments.";
            }

            if (!ValidateIP(customerIpAddress))
            {
                return "Invalid customerIpAddress";
            }

            return string.IsNullOrEmpty(productId) ? "productId is required" : null;
        }

        public static bool ValidateIP(string ip)
        {
            return !string.IsNullOrEmpty(ip) &&
                   IPAddress.TryParse(ip, out _);
        }
    }
}