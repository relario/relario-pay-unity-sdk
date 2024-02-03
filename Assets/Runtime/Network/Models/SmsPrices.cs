using System;
using System.Collections.Generic;
using UnityEngine;

namespace Relario.Network.Models
{
    [Serializable]
    public class SMSPricesResponse
    {
        public List<SMSPricing> rates;
    }

    [Serializable]
    public class SMSPricing
    {
        public string countryCode;
        public string mccmnc;
        public string operatorName;
        public float price;
        public string currency;
    }
}
