using UnityEngine;
using System;
using Zenject;

namespace MilkFarm
{
    /// <summary>
    /// PlayerPrefs ile save/load işlemlerini yöneten singleton manager
    /// JSON formatında kayıt yapar
    /// ✅ Config injection eklendi
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        [Inject] private GameConfig config;

        private const string SAVE_KEY = "MilkFarm_SaveData_v1";
        private MilkFarmSaveData _currentSaveData;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Oyunu kaydet
        /// </summary>
        public void SaveGame(MilkFarmSaveData data)
        {
            try
            {
                // Timestamp güncelle
                data.lastSaveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                // JSON'a çevir
                string json = JsonUtility.ToJson(data, true);

                // PlayerPrefs'e kaydet
                PlayerPrefs.SetString(SAVE_KEY, json);
                PlayerPrefs.Save();

                _currentSaveData = data;
                Debug.Log($"[SaveManager] Oyun kaydedildi. Timestamp: {data.lastSaveTimestamp}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Kayıt hatası: {e.Message}");
            }
        }

        /// <summary>
        /// Oyunu yükle
        /// ✅ Config uygulaması eklendi
        /// </summary>
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
                         
                        // ✅ Config'i stations'a uygula
                        if (config != null)
                        {
                            _currentSaveData.ApplyConfigToStations(config);
                            Debug.Log("[SaveManager] ✅ Config applied to stations");
                        }

                        Debug.Log($"[SaveManager] Oyun yüklendi. Timestamp: {data.lastSaveTimestamp}");
                        return data;
                    }
                }

                // Kayıt yoksa yeni oluştur
                Debug.Log("[SaveManager] Kayıt bulunamadı, yeni kayıt oluşturuluyor.");
                _currentSaveData = new MilkFarmSaveData();

                // ✅ Config'i yeni save'e uygula
                if (config != null)
                {
                    _currentSaveData.ApplyConfigToStations(config);
                    Debug.Log("[SaveManager] ✅ Config applied to new save");
                }

                return _currentSaveData;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Yükleme hatası: {e.Message}");
                _currentSaveData = new MilkFarmSaveData();

                // ✅ Config'i error save'e uygula
                if (config != null)
                {
                    _currentSaveData.ApplyConfigToStations(config);
                }

                return _currentSaveData;
            }
        }

        /// <summary>
        /// Mevcut save data'yı al
        /// </summary>
        public MilkFarmSaveData GetCurrentSaveData()
        {
            if (_currentSaveData == null)
            {
                _currentSaveData = LoadGame();
            }
            return _currentSaveData;
        }

        /// <summary>
        /// Kayıtları sıfırla (debug için)
        /// </summary>
        public void ResetSave()
        {
            PlayerPrefs.DeleteKey(SAVE_KEY);
            PlayerPrefs.Save();
            _currentSaveData = new MilkFarmSaveData();

            // ✅ Config'i reset save'e uygula
            if (config != null)
            {
                _currentSaveData.ApplyConfigToStations(config);
            }

            Debug.Log("[SaveManager] Kayıtlar sıfırlandı!");
        }

        /// <summary>
        /// Otomatik kayıt (belirli aralıklarla)
        /// </summary>
        private void Start()
        {
            InvokeRepeating(nameof(AutoSave), 30f, 30f); // Her 30 saniyede bir kaydet
        }

        private void AutoSave()
        {
            if (_currentSaveData != null)
            {
                SaveGame(_currentSaveData);
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                Debug.Log("[SaveManager] 🔔 Pause - Triggering save event");
                MilkFarmEvents.SaveRequested(); // ✅ Event fire

                if (_currentSaveData != null)
                {
                    SaveGame(_currentSaveData);
                }
            }
        }

        private void OnApplicationQuit()
        {
            Debug.Log("[SaveManager] 🔔 Quit - Triggering save event");
            MilkFarmEvents.SaveRequested(); // ✅ Event fire

            if (_currentSaveData != null)
            {
                SaveGame(_currentSaveData);
            }
        }
    }
}