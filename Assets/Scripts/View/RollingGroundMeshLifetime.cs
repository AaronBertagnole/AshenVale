using UnityEngine;

namespace AshenVale
{
    public sealed class RollingGroundMeshLifetime : MonoBehaviour
    {
        public Mesh OwnedMesh { private get; set; }

        public Material OwnedMaterial { private get; set; }
        public Sprite OwnedSprite { private get; set; }

        void OnDestroy()
        {
            if (OwnedMesh != null) Destroy(OwnedMesh);
            if (OwnedMaterial != null) Destroy(OwnedMaterial);
            if (OwnedSprite != null) Destroy(OwnedSprite);
        }
    }
}
