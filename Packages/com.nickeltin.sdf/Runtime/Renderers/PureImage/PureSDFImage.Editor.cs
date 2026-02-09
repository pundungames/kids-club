#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;

namespace nickeltin.SDF.Runtime
{
    public partial class PureSDFImage
    {
        private readonly List<PureSDFImageRenderingStack> _renderingStack = new();

        public int CountValidRenderingStacks()
        {
            return _renderingStack.Count(s => s != null && s.Contains(this));
        }
        
        internal void RegisterRenderingStack(PureSDFImageRenderingStack stack)
        {
            _renderingStack.RemoveAll(s => s == null);
            if (!_renderingStack.Contains(stack)) _renderingStack.Add(stack);
        }
    }
}

#endif