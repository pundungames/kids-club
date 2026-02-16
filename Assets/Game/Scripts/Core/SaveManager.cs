using UnityEngine;
using System;
using Zenject;

namespace MilkFarm
{
    public class SaveManager : MonoBehaviour
    {
        [Inject] private GameConfig config;

        private const string SAVE_KEY = "MilkFarm_SaveData_v1";
        private MilkFarmSaveData _currentSaveData;

        public void SaveGame(MilkFarmSaveData data)
        {
            try
            {
                data.lastSaveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                string json = JsonUtility.ToJson(data, true);
                PlayerPrefs.SetString(SAVE_KEY, json);
                PlayerPrefs.Save();
                _currentSaveData = data;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Kayıt hatası: {e.Message}");
            }
        }

        public MilkFarmSaveData LoadGame()
        {
            try
            {
                if (PlayerPrefs.HasKey(SAVE_KEY))
                {
                    string json = PlayerPrefs.GetString(SAVE_KEY);
                    MilkFarmSaveData data = JsonUtility.FromJson<MilkFarmSaveData>(json);

                    if (data != null)
                    {
                        _currentSaveData = data;

                        // ✅ MIGRATION: Eski save'de yeni field'lar yoksa oluştur
                        MigrateIfNeeded(_currentSaveData);

                        if (config != null)
                            _currentSaveData.ApplyConfigToStations(config);

                        return _currentSaveData;
                    }
                }

                _currentSaveData = new MilkFarmSaveData();
                if (config != null)
                    _currentSaveData.ApplyConfigToStations(config);
                return _currentSaveData;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Yükleme hatası: {e.Message}");
                _currentSaveData = new MilkFarmSaveData();
                if (config != null)
                    _currentSaveData.ApplyConfigToStations(config);
                return _currentSaveData;
            }
        }

        /// <summary>
        /// Eski save'leri yeni field'larla uyumlu hale getir.
        /// JsonUtility eski save'de olmayan field'ları default yapar:
        /// - List → null (boş liste DEĞİL!)
        /// - float → 0f
        /// - int → 0
        /// Bu yüzden null kontrol yapıp initialize etmemiz lazım.
        /// </summary>
        private void MigrateIfNeeded(MilkFarmSaveData data)
        {
            bool migrated = false;

            // --- Chicken save data ---
            if (data.chickens == null || data.chickens.Count == 0)
            {
                data.chickens = new System.Collections.Generic.List<ChickenSaveData>();
                for (int i = 0; i < 12; i++)
                    data.chickens.Add(new ChickenSaveData(i, i == 0));
                migrated = true;
                Debug.Log("[SaveManager] 🔄 Migration: chickens list oluşturuldu");
            }

            // --- Chicken stations ---
            if (data.chickenStations == null || data.chickenStations.Count == 0)
            {
                data.chickenStations = new System.Collections.Generic.List<StationSaveData>();
                for (int i = 0; i < 4; i++)
                    data.chickenStations.Add(new StationSaveData());
                migrated = true;
                Debug.Log("[SaveManager] 🔄 Migration: chickenStations oluşturuldu");
            }

            // --- Chicken packaging ---
            if (data.chickenPackaging == null)
            {
                data.chickenPackaging = new PackageSaveData();
                migrated = true;
                Debug.Log("[SaveManager] 🔄 Migration: chickenPackaging oluşturuldu");
            }

            // --- Unlock lists ---
            if (data.unlockedChickens == null)
            {
                data.unlockedChickens = new System.Collections.Generic.List<int>();
                migrated = true;
            }
            if (data.unlockedChickenAreas == null)
            {
                data.unlockedChickenAreas = new System.Collections.Generic.List<int>();
                migrated = true;
            }

            // --- IAP chicken lists ---
            if (data.iap != null)
            {
                if (data.iap.unlockedChickens == null)
                    data.iap.unlockedChickens = new System.Collections.Generic.List<int>();
                if (data.iap.unlockedChickenAreas == null)
                    data.iap.unlockedChickenAreas = new System.Collections.Generic.List<int>();
            }

            // Migration yaptıysa hemen kaydet (bir kerelik)
            if (migrated)
            {
                Debug.Log("[SaveManager] ✅ Migration tamamlandı, kaydediliyor...");
                SaveGame(data);
            }
        }

        public MilkFarmSaveData GetCurrentSaveData()
        {
            if (_currentSaveData == null)
                _currentSaveData = LoadGame();
            return _currentSaveData;
        }

        public void ResetSave()
        {
            PlayerPrefs.DeleteKey(SAVE_KEY);
            PlayerPrefs.Save();
            _currentSaveData = new MilkFarmSaveData();
            if (config != null)
                _currentSaveData.ApplyConfigToStations(config);
            Debug.Log("[SaveManager] Kayıtlar sıfırlandı!");
        }

        private void Start()
        {
            InvokeRepeating(nameof(AutoSave), 30f, 30f);
        }

        private void AutoSave()
        {
            if (_currentSaveData != null)
                SaveGame(_currentSaveData);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                MilkFarmEvents.SaveRequested();
                if (_currentSaveData != null)
                    SaveGame(_currentSaveData);
            }
        }

        private void OnApplicationQuit()
        {
            MilkFarmEvents.SaveRequested();
            if (_currentSaveData != null)
                SaveGame(_currentSaveData);
        }
    }
}