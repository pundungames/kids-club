using UnityEngine;

namespace nickeltin.SDF.Runtime
{
    public partial class PureSDFImage
    {
        private static Material _defaultMaterial;
        public static Material DefaultMaterial
        {
            get
            {
                if (_defaultMaterial == null)
                {
                    _defaultMaterial = new Material(Shader.Find(SDFUtil.SDFDisplayPureUIShaderName))
                    {
                        name = "Default Pure UI SDF Material",
                        hideFlags = HideFlags.NotEditable
                    };
                }

                return _defaultMaterial;
            }
        }
        
        public override Material defaultMaterial => DefaultMaterial;

        public override Texture mainTexture => LayerA.SDFTexture;
        

        protected override void UpdateMaterial()
        {
            if (!IsActive())
                return;

            
            canvasRenderer.materialCount = 1;
            canvasRenderer.SetMaterial(materialForRendering, 0);
            
            // Since we're using only sdf sprites here we can be sure that no alpha split textures is used.
            canvasRenderer.SetTexture(LayerA.SDFTexture);
            canvasRenderer.SetAlphaTexture(_lerpLayers ? LayerB.SDFTexture : null);
        }
    }
}