using UnityEngine;
using System.Collections.Generic;
using Zenject;

namespace MilkFarm
{
    /// <summary>
    /// AreaController - EVENT LISTENER VERSION
    /// Sadece visual (lock button kapat), spawn YOK
    /// </summary>
    public class AreaController : MonoBehaviour
    {
        [Header("Area Info")]
        [SerializeField] private int areaIndex;
        [SerializeField] private bool isUnlockedByDefault = false;

        [Header("Lock UI")]
        [SerializeField] private GameObject lockButton;

        [Header("Cow Slots")]
        [SerializeField] private GameObject[] slotLocks; // Cow lock button'lar

        [Header("Interactive Objects")]
        [SerializeField] private GameObject troughsParent;
        [SerializeField] private GameObject slotsParent;
        [SerializeField] private GameObject animalsLockParent;

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
            if (index != areaIndex) return;

            Debug.Log($"[AreaController] 🔔 Event: Area {areaIndex} unlocked!");
            Unlock();
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
            if (saveData == null)
            {
                Debug.LogWarning("[AreaController] SaveData null!");
                return;
            }

            Debug.Log($"[AreaController] 📂 Loading unlock states for Area {areaIndex}...");

            // ✅ 1. Area unlock check
            if (saveData.unlockedAreas != null && saveData.unlockedAreas.Contains(areaIndex))
            {
                Unlock();
                Debug.Log($"[AreaController] ✅ Area {areaIndex} was unlocked (load)");
            }
            else
            {
                Debug.Log($"[AreaController] Area {areaIndex} is LOCKED");
            }

            // ✅ 2. Close lock buttons for unlocked cows
            // 3 cows per area (NOT 4!)

            int cowsPerArea = 3; // ✅ Her area'da 3 inek
            int startCowIndex = areaIndex * cowsPerArea;
            // Area 0: Cows 0-2
            // Area 1: Cows 3-5
            // Area 2: Cows 6-8
            // Area 3: Cows 9-11

            if (saveData.cows == null || saveData.cows.Count == 0)
            {
                Debug.LogWarning("[AreaController] No cow save data!");
                return;
            }

            for (int i = 0; i < cowsPerArea; i++)
            {
                int globalCowIndex = startCowIndex + i;

                // Check bounds
                if (globalCowIndex >= saveData.cows.Count)
                {
                    Debug.LogWarning($"[AreaController] Cow index {globalCowIndex} out of bounds!");
                    break;
                }

                var cowData = saveData.cows[globalCowIndex];

                if (cowData != null && cowData.isUnlocked)
                {
                    CloseLockButton(i); // Local slot: 0, 1, 2
                    Debug.Log($"[AreaController] ✅ Cow {globalCowIndex} unlocked → Slot {i} + button closed");
                }
                else
                {
                    Debug.Log($"[AreaController] Cow {globalCowIndex} LOCKED → Slot {i} + button OPEN");
                }
            }

            Debug.Log($"[AreaController] ✅ Load complete!");
        }
        // === DEBUG ===

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