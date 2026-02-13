using UnityEngine;

namespace MilkFarm
{
    public enum PurchaseType
    {
        // IAP Boostlar (Real Money)
        SpeedBoost50,
        SpeedBoost100,
        RichCustomer50,
        RichCustomer100,
        MilkStorage,
        AutoFeeder,
        AutoWorker,

        // Area/Slot Unlocklar (Gem)
        UnlockArea,      // Yeni area açma
        UnlockTrough,    // Yemlik/suluk açma
        UnlockSlot,      // İnek slotu açma
        UnlockCow,        // İnek satın alma
        UnlockChickenArea,
        UnlockChicken
    }

    [CreateAssetMenu(fileName = "NewPurchaseItem", menuName = "MilkFarm/IAP/Purchase Item Data")]
    public class PurchaseItemData : ScriptableObject
    {
        [Header("UI Bilgileri")]
        public string titleText;          // "İnek Satın Al"
        public Sprite icon;               // İnek ikonu
        public string productName;        // "Holstein İnek"
        public string benefitDescription; // "+10% Süt Üretimi"
        public string priceText;          // "50 💎" veya "$4.99"

        [Header("Store Bilgileri")]
        public string productId;          // "com.milkfarm.cow_slot_1" veya IAP ID
        public PurchaseType type;         // UnlockCow, SpeedBoost50, vb.
        [Header("Unlock Bilgileri (Area/Slot/Cow için)")]
        public int targetIndex;           // Hangi area/slot/cow (0, 1, 2, 3...)
        public int gemCost;               // Gem maliyeti

        [Header("IAP Bilgileri (Real money için)")]
        public float realPrice;           // Gerçek para (opsiyonel)
        public bool isRealMoney;          // IAP mi yoksa gem mi?
    }
}