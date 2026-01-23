using MilkFarm;
using System.Collections.Generic;

[System.Serializable]
public class GameSaveData
{
    // Existing fields...
    public float currentMoney;
    public List<CowSaveData> cows;
    public List<StationSaveData> stations;
    public IAPSaveData iap;

    // ✅ YENİ: Offline progress için
    public long lastPlayTime; // Unix timestamp (seconds since 1970)

    // Constructor'da initialize et:
    public GameSaveData()
    {
        // ... existing init ...
        lastPlayTime = 0; // 0 = ilk oyun
    }
}