using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Relario.Network.Models
{
    [Serializable]
    public class Transaction
    {
        public string transactionId;
        public int merchantId;
        public string productId;
        public string productName = null;
        public string customerId;
        public PaymentType paymentType;
        public List<Payment> payments = new List<Payment>();
        public int callDuration = 0;
        public string customerIpAddress;
        public string customerMsisdn = null;
        public string customerMccmnc = null;
        public string customerCountryCode = null;
        public string customerLanguage = null;
        public string audioFileName = null;
        public string clickToCallUrl = null;
        public string androidClickToSmsUrl = null;
        public string phoneNumber = null;
        public List<string> phoneNumbersList = new List<string>();
        public string smsBody = null;
        public int smsCount = 0;
        public string status;
        public bool test;
        public long createdAt;

        public Transaction(string transactionId,
            int merchantId,
            string productId,
            string productName,
            string customerId,
            PaymentType paymentType,
            List<Payment> payments,
            int callDuration,
            string customerIpAddress,
            string customerMsisdn,
            string customerMccmnc,
            string customerCountryCode,
            string customerLanguage,
            string audioFileName,
            string clickToCallUrl,
            string androidClickToSmsUrl,
            string phoneNumber,
            List<string> phoneNumbersList,
            string smsBody,
            int smsCount,
            string status,
            bool test,
            long createdAt
        )
        {
            this.transactionId = transactionId;
            this.merchantId = merchantId;
            this.productId = productId;
            this.productName = productName;
            this.customerId = customerId;
            this.paymentType = paymentType;
            this.payments = payments;
            this.callDuration = callDuration;
            this.customerIpAddress = customerIpAddress;
            this.customerMsisdn = customerMsisdn;
            this.customerMccmnc = customerMccmnc;
            this.customerCountryCode = customerCountryCode;
            this.customerLanguage = customerLanguage;
            this.audioFileName = audioFileName;
            this.clickToCallUrl = clickToCallUrl;
            this.androidClickToSmsUrl = androidClickToSmsUrl;
            this.phoneNumber = phoneNumber;
            this.phoneNumbersList = phoneNumbersList;
            this.smsBody = smsBody;
            this.smsCount = smsCount;
            this.status = status;
            this.test = test;
            this.createdAt = createdAt;
        }

        // JsonUtility.Parse can't convet string to enum
        // This is the solution for paymentType enum
        public static Transaction FromJSON(string json)
        {
            PaymentTypeS paymentTypeHelper = JsonUtility.FromJson<PaymentTypeS>(json);
            Transaction directParse = JsonUtility.FromJson<Transaction>(json);
            return new Transaction(
                directParse.transactionId,
                directParse.merchantId,
                directParse.productId,
                directParse.productName,
                directParse.customerId,
                (PaymentType)Enum.Parse(typeof(PaymentType), paymentTypeHelper.paymentType),
                directParse.payments,
                directParse.callDuration,
                directParse.customerIpAddress,
                directParse.customerMsisdn,
                directParse.customerMccmnc,
                directParse.customerCountryCode,
                directParse.customerLanguage,
                directParse.audioFileName,
                directParse.clickToCallUrl,
                directParse.androidClickToSmsUrl,
                directParse.phoneNumber,
                directParse.phoneNumbersList,
                directParse.smsBody,
                directParse.smsCount,
                directParse.status,
                directParse.test,
                directParse.createdAt);
        }

        // Helper function to remove payments with matching ddi from phoneNumbersList
        public void RemovePaymentsWithMatchingDdi()
        {
            // Create a HashSet for faster lookup
            HashSet<string> phoneNumberSet = new HashSet<string>(phoneNumbersList);

            // Iterate through payments in reverse order to safely remove items
            for (int i = payments.Count - 1; i >= 0; i--)
            {
                Payment payment = payments[i];

                // Check if the ddi is in phoneNumbersList
                if (phoneNumberSet.Contains(payment.ddi))
                {
                    payments.RemoveAt(i);
                }
            }
        }

        public bool IsPartiallyPaid()
        {
            if (payments.Count == 0)
            {
                return false;
            }

            return payments.Count / smsCount > 0;
        }

        public bool IsFullyPaid(int threshold = 1)
        {
            return payments.Count / smsCount >= threshold;
        }

        public bool IsNotPaid()
        {
            return payments.Count == 0;
        }
    }

    // Helper for Transaction.FromJSON
    class PaymentTypeS
    {
        public string paymentType;
    }
}