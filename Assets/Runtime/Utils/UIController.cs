using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Relario;
using Relario.Network.Models;
using System;

public class UIController : MonoBehaviour
{
    public static UIController instance;

    public TMP_Text partialPaymentsText;
    public TMP_Text timerText;
    public GameObject subscribeBtn, skipDaysBtn, redeemBtn, loadingBar;
    public TMP_Dropdown dropdown;
    [Header("Subscription Manager")]
    public SubscriptionManager relarioSubscriptionManager;
    public Button completePartialBtn;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        SubscriptionManager.TransactionPaid += HandleTransactionPaid;
        SubscriptionManager.TransactionFailed += HandleTransactionFailed;

        SubscriptionManager.PartialPaymentsExists += HandlePartialPaymentsExists;
        SubscriptionManager.PartialPaymentsRecieved += HandlePartialPaymentsRecieved;
        SubscriptionManager.PartialTransactionPaid += HandlePartialPaymentsPaid;
        SubscriptionManager.SMSPricesFetched += HandleSMSPricesFetched;

        relarioSubscriptionManager.TransactionLaunched.AddListener(HandleTransactionLaunch);
        relarioSubscriptionManager.RewardReadyEvent.AddListener(HandleRewardReady);

        if (PlayerPrefs.GetInt("Subscribed") == 1)
        {
            subscribeBtn.SetActive(false);
            skipDaysBtn.SetActive(true);
        }

        UpdatePartialPaymentsText();
        redeemBtn.GetComponent<Button>().onClick.AddListener(RedeemPartialRewards);
        completePartialBtn.onClick.AddListener(relarioSubscriptionManager.LaunchPartialPayment);
        completePartialBtn.gameObject.SetActive(false);

        dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
        skipDaysBtn.GetComponent<Button>().onClick.AddListener(SkipDays);
    }

    void HandleTransactionPaid(Transaction transaction)
    {
        // Handle successful transaction
        UpdateTimerText($"Rewards Granted");
        loadingBar.SetActive(false);
        skipDaysBtn.SetActive(true);
        Debug.Log($"Rewards Granted, {transaction.payments.Count} Payments recieved");
    }

    void HandleTransactionFailed(Transaction transaction)
    {
        // Handle failed transaction
        UpdateTimerText($"You don't have enough sms units");
        ResetSubscription();
    }

    void HandlePartialPaymentsExists(Transaction transaction)
    {
        // Handle partial payments recieved
        //to display content------------- 
        subscribeBtn.SetActive(false);
        loadingBar.SetActive(false);
        completePartialBtn.gameObject.SetActive(true);
        UpdateTimerText($"You have partial payments. Reload your airtime and complete the transaction");
        //--------------------------------------------------
    }

    void HandlePartialPaymentsRecieved(Transaction transaction)
    {
        // Handle partial payments recieved
        //to display content------------- 
        completePartialBtn.gameObject.SetActive(true);
        loadingBar.SetActive(false);
        UpdatePartialPaymentsText();
        CheckPartialPayments();
        UpdateTimerText($"There were partial payments made. Reload your airtime and complete the transaction");
        //--------------------------------------------------
    }

    void HandlePartialPaymentsPaid(Transaction transaction)
    {
        // Handle partial payments
        //to display content------------- 
        UpdateTimerText($"Rewards granted through partial completion"); 
        Debug.LogWarning($"Rewards granted through partial completion");
        loadingBar.SetActive(false);
        skipDaysBtn.SetActive(true);
        //--------------------------------------------------
    }

    void HandleTransactionLaunch()
    {
        //display progress----------------- you can take this out
        loadingBar.SetActive(true);
        skipDaysBtn.SetActive(false);
        //-------------------------------------------
    }

    void HandleRewardReady()
    {
        UpdateTimerText($"Reward is ready");
    }

    private void HandleSMSPricesFetched(Exception exception, SMSPricesResponse smsPricesResponse)
    {
        if (exception != null)
        {
            Debug.LogError("[YourOtherScript] Failed to fetch SMS prices: " + exception.Message);
        }
        else
        {
            // Handle SMS prices response
            Debug.Log("[YourOtherScript] SMS Prices fetched successfully.");

            // Access smsPricesResponse.rates for SMS pricing details
            if (smsPricesResponse != null && smsPricesResponse.rates != null)
            {
                foreach (var rate in smsPricesResponse.rates)
                {
                    Debug.Log($"Country Code: {rate.countryCode}, Operator: {rate.operatorName}, Price: {rate.price} {rate.currency}");
                }
            }
            else
            {
                Debug.LogWarning("[YourOtherScript] SMS Prices response or rates are null.");
            }
        }
    }

    public void ResetSubscription()
    {
        subscribeBtn.SetActive(true);
        skipDaysBtn.SetActive(false);
        loadingBar.SetActive(false);

        PlayerPrefs.SetInt("Subscribed", 0);
    }

    public void UpdatePartialPaymentsText()
    {
        partialPaymentsText.text = "Partial Payments: " + PlayerPrefs.GetInt("partial_Payments").ToString();
    }

    public void UpdateTimerText(string text)
    {
        timerText.text = text;
    }

    void RedeemPartialRewards()
    {
        int pp = PlayerPrefs.GetInt("partial_Payments");
        pp -= 2;

        relarioSubscriptionManager.GetAndSaveLastRewardTime();

        PlayerPrefs.SetInt("partial_Payments", pp);
        UpdatePartialPaymentsText();
        CheckPartialPayments();
    }

    public void CheckPartialPayments()
    {
        if (PlayerPrefs.GetInt("partial_Payments") >= 2)
        {
            redeemBtn.SetActive(true);
        }
        else
        {
            redeemBtn.SetActive(false);
        }
    }

    private void OnDropdownValueChanged(int value)
    {
        // Map the Dropdown value to the corresponding SubscriptionType enum
        SubscriptionType selectedType = (SubscriptionType)value;

        // Set the subscription type in your subscription manager script
        relarioSubscriptionManager.subscriptionType = selectedType;
    }

    void SkipDays()
    {
        StartCoroutine(RelaxSkipDays());
        relarioSubscriptionManager.SkipDays();
    }

    IEnumerator RelaxSkipDays()
    {
        skipDaysBtn.GetComponent<Button>().interactable = false;
        yield return new WaitForSeconds(2f);
        skipDaysBtn.GetComponent<Button>().interactable = true;
    }
}
