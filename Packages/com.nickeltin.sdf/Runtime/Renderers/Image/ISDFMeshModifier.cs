using UnityEngine.UI;

namespace nickeltin.SDF.Runtime
{
    public interface ISDFMeshModifier : IMeshModifier
    {
        /// <summary>
        /// Call used to modify sdf mesh.
        /// Place any custom mesh processing in this function.
        /// Called right after <see cref="IMeshModifier.ModifyMesh(VertexHelper)"/>.
        /// </summary>
        void ModifySDFMesh(VertexHelper sdfVerts);
    }
}