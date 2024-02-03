using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


namespace Relario
{

    public class SubscriptionTypeController : MonoBehaviour
    {
        public RelarioPay subscriptionManager;
        public Button skipDaysBtn;
        public TMP_Dropdown dropdown;

        private void Start()
        {
            if (!subscriptionManager)
            {
                subscriptionManager = FindObjectOfType<RelarioPay>();
            }
            // Subscribe to the Dropdown's OnValueChanged event
            dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
            skipDaysBtn.onClick.AddListener(SkipDays);
        }

        private void OnDropdownValueChanged(int value)
        {
            // Map the Dropdown value to the corresponding SubscriptionType enum
            SubscriptionType selectedType = (SubscriptionType)value;

            // Set the subscription type in your subscription manager script
            subscriptionManager.subscriptionType = selectedType;
        }

        void SkipDays()
        {
            // subscriptionManager.SkipDays();
        }
    }
}