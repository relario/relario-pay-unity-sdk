using System;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.Android;

public class SMSPaymentHandler : MonoBehaviour
{

    [Header("API Settings")]
    public string apiKey = "5c6546fb1e7048d8a0774f26d734956c";

    [Header("Request Settings")]
    [Tooltip("Should SMS be sent in background or should it switch to the sms app")]
    public bool switchToSmsApp = false;
    [Tooltip("Max number of times the app should check if transaction was successful or not")]
    public int _maxTrackingRetries = 5; // Updated variable name

    [Header("Debug")]
    public TMP_Text notifyText;
    private const string SMSPermission = "android.permission.SEND_SMS"; // SMS permission
    // private const string CallPermission = "android.permission.CALL_PHONE"; // Call permission

    private void Start()
    {
        if (String.IsNullOrEmpty(apiKey))
        {
            Debug.LogWarning("No API key provided for the RelarioTransaction Script");
        }

        if (!switchToSmsApp)
        {
            RequestPermissions();
        }
    }

    public string GetClickToSmsUrl(string[] ddis, string smsBody)
    {
        string[] phoneNumbers = ddis.Select(ddi => "+" + ddi).ToArray();

        string userAgent = SystemInfo.operatingSystem;

        // Escape SMS body text using UnityWebRequest
        //example: Tap Send 112304711:7287e45725
        string text = "Tap Send " + UnityWebRequest.EscapeURL(smsBody);

        if (Regex.IsMatch(userAgent, "iPad|iPhone|iPod") && !Application.isEditor)
        {
            return GenerateIosClickToSmsUrl(phoneNumbers, text);
        }
        else
        {
            return GenerateAndroidClickToSmsUrl(phoneNumbers, text);
        }
    }

    private string GenerateIosClickToSmsUrl(string[] phoneNumbers, string smsBody)
    {
        return "sms://open?addresses=" + string.Join(",", phoneNumbers) + "?&body=" + smsBody;
    }

    private string GenerateAndroidClickToSmsUrl(string[] phoneNumbers, string smsBody)
    {
        return "sms://" + string.Join(",", phoneNumbers) + "?&body=" + smsBody;
    }

    public void RequestPermissions()
    {
        // Check if SMS permission is granted, and request it if not
        if (!Permission.HasUserAuthorizedPermission(SMSPermission))
        { Permission.RequestUserPermission(SMSPermission); }
    }

}