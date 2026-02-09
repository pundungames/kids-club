using System.Collections;
using System.Collections.Generic;
using nickeltin.InternalBridge.Editor;
using nickeltin.SDF.Runtime;
using UnityEngine.SceneManagement;

namespace nickeltin.SDF.Editor
{
    internal partial class SDFFLRDebugWindow
    {
        private readonly struct SortedRenderer
        {
            public readonly int Index;

            public SortedRenderer(int index)
            {
                Index = index;
            }

            public bool TryGet(out SDFFirstLayerRenderer renderer)
            {
                return SDFFirstLayerRenderer.activeRenderers[Index].TryGetTarget(out renderer);
            }
        }

        private readonly struct SceneRenderers : IEnumerable<SortedRenderer>
        {
            public readonly Scene Scene;
            private readonly List<SortedRenderer> _sortedRenderers;
            public readonly IList<SortedRenderer> Renderers => _sortedRenderers;

            private readonly _SavedBool _expandedState;

            public bool Expanded
            {
                get => _expandedState.Value;
                set => _expandedState.Value = value;
            }

            public SceneRenderers(Scene scene, List<SortedRenderer> sortedRenderers)
            {
                Scene = scene;
                _sortedRenderers = sortedRenderers;
                _expandedState = new _SavedBool($"com.nickeltin.sdf.FSRDebugWindow.{Scene.name + Scene.path}");
            }

            public IEnumerator<SortedRenderer> GetEnumerator() => _sortedRenderers.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private class SortedRenderersCollection : IEnumerable<SceneRenderers>
        {
            private readonly Dictionary<Scene, List<SortedRenderer>> _sceneToRenderers = new();

            public bool Refreshed { get; private set; }

            public void ConsumeRefresh() => Refreshed = false;

            public void Refresh()
            {
                _sceneToRenderers.Clear();
                var i = 0;
                foreach (var activeRendererRef in SDFFirstLayerRenderer.activeRenderers)
                {
                    if (!activeRendererRef.TryGetTarget(out var renderer)) continue;

                    if (_sceneToRenderers.TryGetValue(renderer.gameObject.scene, out var list))
                    {
                        list.Add(new SortedRenderer(i));
                    }
                    else
                    {
                        _sceneToRenderers.Add(renderer.gameObject.scene, new List<SortedRenderer> { new(i) });
                    }

                    i++;
                }

                Refreshed = true;
            }

            public IEnumerator<SceneRenderers> GetEnumerator()
            {
                foreach (var (scene, list) in _sceneToRenderers)
                {
                    yield return new SceneRenderers(scene, list);
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}