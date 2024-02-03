using System.Linq;
using System.Text.RegularExpressions;
using Relario.Network.Models;
using UnityEngine.Device;
using UnityEngine.Networking;

namespace Relario {
    public static class Utility {
        public static string paymentTypeToString (PaymentType paymentType) {
            switch (paymentType) {
                case PaymentType.sms:
                    return "sms";
                default:
                    return paymentType.ToString ();
            }
        }
        public const string SMSPermission = "android.permission.SEND_SMS";
        
        public static string GetClickToSmsUrl(string[] ddis, string smsBody)
        {
            string[] phoneNumbers = ddis.Select(ddi => "+" + ddi).ToArray();

            string userAgent = SystemInfo.operatingSystem;

            // Escape SMS body text using UnityWebRequest
            //example: Tap Send 112304711:7287e45725
            string text = "Tap Send " + UnityWebRequest.EscapeURL(smsBody);

            if (Regex.IsMatch(userAgent, "iPad|iPhone|iPod") && !Application.isEditor)
            {
                return Utility.GenerateIosClickToSmsUrl(phoneNumbers, text);
            }
            else
            {
                return Utility.GenerateAndroidClickToSmsUrl(phoneNumbers, text);
            }
        }

        private static string GenerateIosClickToSmsUrl(string[] phoneNumbers, string smsBody)
        {
            return "sms://open?addresses=" + string.Join(",", phoneNumbers) + "?&body=" + smsBody;
        }

        private static string GenerateAndroidClickToSmsUrl(string[] phoneNumbers, string smsBody)
        {
            return "sms://" + string.Join(",", phoneNumbers) + "?&body=" + smsBody;
        }
    }
}