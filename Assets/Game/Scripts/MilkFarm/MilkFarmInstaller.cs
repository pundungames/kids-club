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

        public override void InstallBindings()
        {

            // Config
            Container.Bind<GameConfig>().FromInstance(gameConfig).AsSingle();

            // Managers
            Container.Bind<UIManager>().FromComponentInHierarchy().AsSingle().NonLazy();
            Container.Bind<CurrencyManager>().FromComponentInHierarchy().AsSingle().NonLazy();
            Container.Bind<SaveManager>().FromComponentInHierarchy().AsSingle().NonLazy();
            Container.Bind<IAPManager>().FromComponentInHierarchy().AsSingle().NonLazy();
            Container.Bind<MoneyManager>().FromComponentInHierarchy().AsSingle().NonLazy();
            Container.Bind<CowManager>().FromComponentInHierarchy().AsSingle().NonLazy();
            Container.Bind<StationManager>().FromComponentInHierarchy().AsSingle().NonLazy();
            Container.Bind<PackageManager>().FromComponentInHierarchy().AsSingle().NonLazy();
            Container.Bind<CustomerManager>().FromComponentInHierarchy().AsSingle().NonLazy();
            //Container.Bind<OfflineProgressManager>().FromComponentInHierarchy().AsSingle().NonLazy();
            Container.Bind<AutoWorkerManager>().FromComponentInHierarchy().AsSingle().NonLazy();
            Container.Bind<StableManager>().FromComponentInHierarchy().AsSingle().NonLazy();
            //Container.Bind<UpgradeManager>().FromComponentInHierarchy().AsSingle().NonLazy();

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
