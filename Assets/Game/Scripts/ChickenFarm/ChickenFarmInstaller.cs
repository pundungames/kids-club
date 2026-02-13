using UnityEngine;
using Zenject;
using MilkFarm;

namespace ChickenFarm
{
    public class ChickenFarmInstaller : MonoInstaller
    {
        [Header("Config")]
        [SerializeField] private GameConfig gameConfig;

        public override void InstallBindings()
        {
            // Config
            Container.Bind<GameConfig>().FromInstance(gameConfig).AsSingle();

            // Ortak Manager'lar (MilkFarmInstaller ile aynı)
            Container.Bind<UIManager>().FromComponentInHierarchy().AsSingle().NonLazy();
            Container.Bind<CurrencyManager>().FromComponentInHierarchy().AsSingle().NonLazy();
            Container.Bind<SaveManager>().FromComponentInHierarchy().AsSingle().NonLazy();
            Container.Bind<IAPManager>().FromComponentInHierarchy().AsSingle().NonLazy();
            Container.Bind<MoneyManager>().FromComponentInHierarchy().AsSingle().NonLazy();
            Container.Bind<StationManager>().FromComponentInHierarchy().AsSingle().NonLazy();
            Container.Bind<PackageManager>().FromComponentInHierarchy().AsSingle().NonLazy();
            Container.Bind<CustomerManager>().FromComponentInHierarchy().AsSingle().NonLazy();
            Container.Bind<AutoWorkerManager>().FromComponentInHierarchy().AsSingle().NonLazy();
            Container.Bind<StableManager>().FromComponentInHierarchy().AsSingle().NonLazy();

            // ✅ IAnimalManager → ChickenManager olarak bind
            // StableManager, AutoWorkerManager, IAPManager, UpgradeManager
            // hepsi IAnimalManager üzerinden çalışacak
            // Chicken Manager tek binding
            Container.BindInterfacesAndSelfTo<ChickenManager>()
                .FromComponentInHierarchy()
                .AsSingle()
                .NonLazy();


            Debug.Log("[ChickenFarmInstaller] ✅ Tüm bağımlılıklar inject edildi!");
        }
    }
}