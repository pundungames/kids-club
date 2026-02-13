using AssetKits.ParticleImage;
using UnityEngine;
using Zenject;

namespace MilkFarm
{
    /// <summary>
    /// MoneySlot Clicker - ProductionAreaClicker mantığıyla
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class MoneySlotClicker : MonoBehaviour
    {
        [Inject] private MoneyManager moneyManager;
        [Inject] AudioManager audioManager;
        [Header("Visual Feedback")]
        [SerializeField] private GameObject clickEffect;
        [SerializeField] ParticleImage coinAttraction;

        private void Awake()
        {
            Collider clickCollider = GetComponent<Collider>();
            if (clickCollider != null)
            {
                clickCollider.isTrigger = true;
            }
        }

        private void OnMouseDown()
        {
            if (moneyManager != null)
            {
                if (moneyManager.HasCoins())
                {
                    audioManager.Play("CoinTap");
                    moneyManager.CollectCoins();
                    if (coinAttraction) coinAttraction.Play();
                    if (clickEffect != null)
                    {
                        Instantiate(clickEffect, transform.position, Quaternion.identity);
                    }

                    Debug.Log("[MoneySlotClicker] 💰 Coins collected!");
                }
            }
        }
    }
}