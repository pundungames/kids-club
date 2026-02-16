using UnityEngine;
using System.Collections.Generic;
using Zenject;
using Dreamteck.Splines;
using System;

namespace MilkFarm
{
    /// <summary>
    /// AreaController - EVENT LISTENER VERSION
    /// Sadece visual (lock button kapat), spawn YOK
    /// </summary>
    public class AreaController : MonoBehaviour
    {
        [Header("Area Info")]
        [SerializeField] private AreaType areaType; // Müfettişten (Inspector) seçeceksin
        [SerializeField] private int areaIndex;
        [SerializeField] private bool isUnlockedByDefault = false;
        [SerializeField] private bool isChickenScene = false;

        [Header("Lock UI")]
        [SerializeField] private GameObject lockButton;

        [Header("Cow Slots")]
        [SerializeField] private GameObject[] slotLocks; // Cow lock button'lar

        [Header("Interactive Objects")]
        [SerializeField] private GameObject troughsParent;
        [SerializeField] private GameObject slotsParent;
        [SerializeField] private GameObject animalsLockParent;
        [SerializeField] internal SplineComputer spline;

        [Header("Troughs")]
        [SerializeField] private GameObject[] troughs;

        [Header("Dependencies")]
        [Inject] private SaveManager saveManager;

        private bool isUnlocked = false;

        public int AreaIndex => areaIndex;
        public bool IsUnlocked => isUnlocked;

        // === EVENT SUBSCRIPTION ===

        private void OnEnable()
        {
            MilkFarmEvents.OnCowUnlocked += HandleCowUnlocked;
            MilkFarmEvents.OnAreaUnlocked += HandleAreaUnlocked;
            MilkFarmEvents.OnTroughUnlocked += HandleTroughUnlocked;
        }

        private void OnDisable()
        {
            MilkFarmEvents.OnCowUnlocked -= HandleCowUnlocked;
            MilkFarmEvents.OnAreaUnlocked -= HandleAreaUnlocked;
            MilkFarmEvents.OnTroughUnlocked -= HandleTroughUnlocked;
        }

        // === INITIALIZATION ===

        private void Awake()
        {
            if (isUnlockedByDefault)
            {
                Unlock();
            }
            else
            {
                Lock();
            }
        }

        private void Start()
        {
            LoadUnlockStates();
            if(isUnlockedByDefault) lockButton.gameObject.SetActive(false);
        }

        // === EVENT HANDLERS ===

        private void HandleCowUnlocked(int globalIndex)
        {
            // Bu area'nın ineği mi?
            int cowAreaIndex = globalIndex / 3;
            if (cowAreaIndex != areaIndex) return;

            int localSlot = globalIndex % 3;

            Debug.Log($"[AreaController] 🔔 Event: Cow {globalIndex} → Area {areaIndex}, Slot {localSlot}");
            CloseLockButton(localSlot);
        }

        private void HandleAreaUnlocked(int index)
        {
            // Eğer gelen index bizimkiyle aynıysa VE biz doğru türdeysek aç
            // Not: MilkFarmEvents.AreaUnlocked() içine tür bilgisi de eklenebilir 
            // ama şimdilik save'den tekrar kontrol etmek en güvenlisi:

            var saveData = saveManager.GetCurrentSaveData();
            if (areaType == AreaType.Cow && saveData.unlockedAreas.Contains(areaIndex))
            {
                Unlock();
            }
            else if (areaType == AreaType.Chicken && saveData.unlockedChickenAreas.Contains(areaIndex))
            {
                Unlock();
            }
        }

        private void HandleTroughUnlocked(int index)
        {
            if (index != areaIndex) return;

            Debug.Log($"[AreaController] 🔔 Event: Trough {areaIndex} unlocked!");
            UnlockTroughs();
        }

        // === AREA UNLOCK ===

        public void Unlock()
        {
            if (isUnlocked) return;

            isUnlocked = true;

            if (lockButton != null) lockButton.SetActive(false);

            if (troughsParent != null) troughsParent.SetActive(true);
            if (slotsParent != null) slotsParent.SetActive(true);
            if (animalsLockParent != null) animalsLockParent.SetActive(true);

            gameObject.SetActive(true);

            Debug.Log($"[AreaController] ✅ Area {areaIndex} unlocked!");
        }

        public void Lock()
        {
            isUnlocked = false;
            if (lockButton != null) lockButton.SetActive(true);
        }

        // === LOCK BUTTON CONTROL ===

        private void CloseLockButton(int localSlot)
        {
            if (localSlot < 0 || localSlot >= slotLocks.Length)
            {
                Debug.LogError($"[AreaController] Invalid local slot: {localSlot}");
                return;
            }

            if (slotLocks[localSlot] != null)
            {
                slotLocks[localSlot].SetActive(false);
                Debug.Log($"[AreaController] ✅ Lock button {localSlot} kapandı!");
            }
        }

        // === TROUGH ===

        public void UnlockTroughs()
        {
            if (!isUnlocked) return;

            foreach (GameObject trough in troughs)
            {
                if (trough != null) trough.SetActive(true);
            }

            Debug.Log($"[AreaController] ✅ Troughs unlocked!");
        }

        // === LOAD (Sadece lock button durumları) ===

        private void LoadUnlockStates()
        {
            var saveData = saveManager.GetCurrentSaveData();
            if (saveData == null) return;

            // ✅ 1. Hangi listeye bakacağımızı seçiyoruz
            bool areaWasUnlocked = false;
            if (areaType == AreaType.Cow)
            {
                areaWasUnlocked = saveData.unlockedAreas != null && saveData.unlockedAreas.Contains(areaIndex);
            }
            else if (areaType == AreaType.Chicken)
            {
                areaWasUnlocked = saveData.unlockedChickenAreas != null && saveData.unlockedChickenAreas.Contains(areaIndex);
            }

            if (areaWasUnlocked)
            {
                Unlock();
            }
            else
            {
                Lock();
            }
            // ✅ 2. Hayvan kilitlerini de türe göre kontrol ediyoruz
            int animalsPerArea = 3;
            int startAnimalIndex = areaIndex * animalsPerArea;

            for (int i = 0; i < animalsPerArea; i++)
            {
                int globalIndex = startAnimalIndex + i;
                bool isAnimalUnlocked = false;

                if (areaType == AreaType.Cow)
                {
                    if (globalIndex < saveData.cows.Count)
                        isAnimalUnlocked = saveData.cows[globalIndex].isUnlocked;
                }
                else
                {
                    if (globalIndex < saveData.chickens.Count)
                        isAnimalUnlocked = saveData.chickens[globalIndex].isUnlocked;
                }

                if (isAnimalUnlocked)
                {
                    CloseLockButton(i);
                }
            }
        }        // === DEBUG ===

        [ContextMenu("Debug: Unlock Area")]
        public void DebugUnlock() => Unlock();

        [ContextMenu("Debug: Close All Locks")]
        public void DebugCloseAllLocks()
        {
            for (int i = 0; i < slotLocks.Length; i++)
            {
                CloseLockButton(i);
            }
        }
    }
}
[Serializable]
public enum AreaType { Cow, Chicken } // Türleri belirleyelim