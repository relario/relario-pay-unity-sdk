using Relario.Network.Models;
using System;
using System.Collections.Generic;

public static class MockTransaction
{
    public static Transaction CreateMockTransaction(int phoneNumbersCount, int paymentsCount)
    {
        // Mock data
        string transactionId = "3795827";
        int merchantId = 123;
        string productId = "my-product-id";
        string productName = "Product name";
        string customerId = "my-customer-id";
        PaymentType paymentType = PaymentType.sms;
        // Create payments dynamically based on paymentsCount
        List<Payment> payments = CreatePayments(paymentsCount);
        int callDuration = 0;
        string customerIpAddress = "79.25.254.4";
        string customerMsisdn = null;
        string customerMccmnc = "22201";
        string customerCountryCode = "IT";
        string customerLanguage = "en";
        string audioFileName = null;
        string clickToCallUrl = null;
        string androidClickToSmsUrl = "sms://+6745594739,+6745594798,+6745591627,+6745594705,+6745591555;?&body=3795827:e701895342";
        string phoneNumber = null;
        // Create phone numbers dynamically based on phoneNumbersCount
        List<string> phoneNumbersList = CreatePhoneNumbers(phoneNumbersCount);
        string smsBody = "3795827:e701895342";
        int smsCount = 5;
        string status = "ok";
        bool test = true;
        long createdAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Create and return the mock transaction
        return new Transaction(
            transactionId, merchantId, productId, productName, customerId, paymentType, payments,
            callDuration, customerIpAddress, customerMsisdn, customerMccmnc, customerCountryCode,
            customerLanguage, audioFileName, clickToCallUrl, androidClickToSmsUrl, phoneNumber,
            phoneNumbersList, smsBody, smsCount, status, test, createdAt
        );
    }

    private static List<Payment> CreatePayments(int count)
    {
        List<Payment> payments = new List<Payment>();
        for (int i = 0; i < count; i++)
        {
            Payment payment = new Payment
            {
                id = 448843 + i,
                transactionId = 3795827,
                cli = $"55427998{i}",
                ddi = $"6745594798{i}",
                smsBody = "3795827:e701895342",
                initiatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                billable = true
            };
            payments.Add(payment);
        }
        return payments;
    }

    private static List<string> CreatePhoneNumbers(int count)
    {
        List<string> phoneNumbersList = new List<string>();
        for (int i = 0; i < count; i++)
        {
            phoneNumbersList.Add($"67455947{i}");
        }
        return phoneNumbersList;
    }
}
