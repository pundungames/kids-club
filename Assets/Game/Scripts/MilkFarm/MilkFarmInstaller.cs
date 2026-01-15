using UnityEngine;
using Zenject;

namespace MilkFarm
{
    /// <summary>
    /// MilkFarm modülü için Zenject Scene Installer
    /// Tüm manager'ları ve config'leri DI container'a bağlar
    /// </summary>
    public class MilkFarmInstaller : MonoInstaller
    {
        [Header("Config")]
        [SerializeField] private GameConfig gameConfig;

        [Header("Managers")]
        [SerializeField] private SaveManager saveManager;
        [SerializeField] private IAPManager iapManager;
        [SerializeField] private MoneyManager moneyManager;
        [SerializeField] private CowManager cowManager;
        [SerializeField] private StationManager stationManager;
        [SerializeField] private PackageManager packageManager;
        [SerializeField] private CustomerManager customerManager;
        [SerializeField] private OfflineProgressManager offlineProgressManager;
        [SerializeField] private AutoWorkerManager autoWorkerManager;
        [SerializeField] private UpgradeManager upgradeManager;

        public override void InstallBindings()
        {
            // Config
            Container.Bind<GameConfig>().FromInstance(gameConfig).AsSingle();

            // Managers
            Container.Bind<SaveManager>().FromInstance(saveManager).AsSingle();
            Container.Bind<IAPManager>().FromInstance(iapManager).AsSingle();
            Container.Bind<MoneyManager>().FromInstance(moneyManager).AsSingle();
            Container.Bind<CowManager>().FromInstance(cowManager).AsSingle();
            Container.Bind<StationManager>().FromInstance(stationManager).AsSingle();
            Container.Bind<PackageManager>().FromInstance(packageManager).AsSingle();
            Container.Bind<CustomerManager>().FromInstance(customerManager).AsSingle();
            Container.Bind<OfflineProgressManager>().FromInstance(offlineProgressManager).AsSingle();
            Container.Bind<AutoWorkerManager>().FromInstance(autoWorkerManager).AsSingle();
            Container.Bind<UpgradeManager>().FromInstance(upgradeManager).AsSingle();

            Debug.Log("[MilkFarmInstaller] Tüm bağımlılıklar başarıyla inject edildi!");
        }

        private void OnValidate()
        {
            // Inspector'da uyarı ver
            if (gameConfig == null)
            {
                Debug.LogWarning("[MilkFarmInstaller] GameConfig referansı eksik!");
            }
        }
    }
}
