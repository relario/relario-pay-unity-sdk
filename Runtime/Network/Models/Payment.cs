using UnityEngine;
using System;

namespace Relario.Network.Models
{
    [Serializable]
    public class Payment
    {
        public int id;
        public int transactionId;
        public string cli;
        public string cliMccmnc = null;
        public string ddi;
        public string smsBody = null;
        public int callDuration = 0;
        public long initiatedAt;
        public string ipnStatus;
        public bool billable;
    }
}