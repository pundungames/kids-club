using System;
using System.Linq;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace nickeltin.Core.Runtime
{
    /// <summary>
    /// Overrides current player loop and adds some more sub-systems to update and fixed update systems
    /// </summary>
    public static class StaticPlayerLoop
    {
        public struct RunBeforeUpdate { }
        
        public struct RunAfterUpdate { }
        
        public struct RunBeforeFixedUpdate { }
        
        public struct RunAfterFixedUpdate { }
        
        
        public static event Action BeforeUpdate;
        public static event Action AfterUpdate;
        public static event Action BeforeFixedUpdate;
        public static event Action AfterFixedUpdate;
        
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Init()
        {
            PlayerLoopSystem CreateSystem(PlayerLoopSystem.UpdateFunction updateDelegate, Type type)
            {
                var sys = new PlayerLoopSystem();
                sys.type = type;
                sys.updateDelegate = updateDelegate;
                return sys;
            }
            
            void PrependAndAppend(ref PlayerLoopSystem lRoot, PlayerLoopSystem.UpdateFunction prepend, Type prependType,
                PlayerLoopSystem.UpdateFunction append, Type appendType)
            {
                var pre = CreateSystem(prepend, prependType);
                var app = CreateSystem(append, appendType); 
                lRoot.subSystemList = lRoot.subSystemList.Prepend(pre).Append(app).ToArray();
            }
            
            var root = PlayerLoop.GetCurrentPlayerLoop();
            var systemsFound = 0;
            for (var i = 0; i < root.subSystemList.Length; i++)
            {
                var system = root.subSystemList[i];
                
                if (system.type == typeof(Update))
                {
                    PrependAndAppend(ref system, 
                        OnBeforeUpdate, typeof(RunBeforeUpdate), 
                        OnAfterUpdate, typeof(RunAfterUpdate));
                    systemsFound++;
                }

                if (system.type == typeof(FixedUpdate))
                {
                    PrependAndAppend(ref system, 
                        OnBeforeFixedUpdate, typeof(RunBeforeFixedUpdate), 
                        OnAfterFixedUpdate, typeof(RunAfterFixedUpdate));
                    systemsFound++;
                }
                
                root.subSystemList[i] = system;

                if (systemsFound == 2)
                {
                    break;
                }
            }

            PlayerLoop.SetPlayerLoop(root);
        }

        private static void OnBeforeUpdate()
        {
            BeforeUpdate?.Invoke();   
        }
        
        private static void OnAfterUpdate()
        {
            AfterUpdate?.Invoke();
        }

        
        private static void OnBeforeFixedUpdate()
        {
            BeforeFixedUpdate?.Invoke();
        }
        
        private static void OnAfterFixedUpdate()
        {
            AfterFixedUpdate?.Invoke();
        }
        
    }
}