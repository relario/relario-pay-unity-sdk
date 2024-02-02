# Relario Pay Unity SDK

## Installation

Installation of this package requires Unity 2021.3 or higher.

Under `Packages/manifest.json`, add the following lines, then open the Unity Editor. Note: this will add a scoped registry to your project.

```json
{
  "dependencies": {
    "com.google.external-dependency-manager": "1.2.178"
  },
  "scopedRegistries": [
    {
      "name": "package.openupm.com",
      "url": "https://package.openupm.com",
      "scopes": [
        "com.google.external-dependency-manager"
      ]
    }
  ]
}
```

Then you need to go to Package Manager (`Assets/View in Package Manager`) and import via git url.


## Build
**This SDK works only if the platform selected is Android.**
In order to build, you need to do a few changes:
- In player settings:
  - you need to set MinSDK to `24`
  - scripting backend to `IL2CPP`
  - target architecture : `ARMv7` and `ARM64`
- If android resolver asks to enable auto-import, press `NO`
  - in case you enabled it, you can disable it by going to `Assets -> External Package Dependency -> Android Resolver -> Settings` and disable auto-import.


## API

### Single payment
`Pay(int smsCount, string productId, string productName, string customerId, Action<Exception, Transaction> callback)` 

Example:     
```csharp
    void Start()
    {
        RelarioPay relarioPay = gameObject.AddComponent<RelarioPay>();
        relarioPay.OnPartialPay = transaction => Debug.Log("Partial Pay");
        relarioPay.OnFailedPay = (exception, transaction) => Debug.Log("Failed Pay");
        relarioPay.OnSuccessfulPay = transaction => Debug.Log("Successful Pay");
        relarioPay.Pay(2, "test", "Test", null, (exception, transaction) =>
        {
            Debug.Log("Transaction started");
        });
    }
```

### Subscribe
```csharp
    void Start()
    {
        RelarioPay relarioPay = gameObject.AddComponent<RelarioPay>();

        SubscriptionOptions options = new SubscriptionOptions(
            productId: "test",
            smsCount: 2,
            productName: "Example Product - Subscription",
            customerId: Guid.NewGuid().ToString(),
            intervalRate: 24,
            timeUnit: TimeUnit.DAYS
        );

        relarioPay.Subscribe(options);
    }
```

### Cancel subscription
```csharp
    void Start()
    {
        RelarioPay relarioPay = gameObject.AddComponent<RelarioPay>();

        relarioPay.CancelSubscription(productId: "test");
    }
```