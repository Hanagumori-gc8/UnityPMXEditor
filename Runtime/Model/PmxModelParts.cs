using System;
using UnityEngine;

namespace Hanagumori.UnityPmx
{
    public enum PmxPartHierarchyMode
    {
        ProxyNodes = 0,
        SeparateRenderers = 1
    }

    [DisallowMultipleComponent]
    public sealed class PmxModelPart : MonoBehaviour
    {
        [SerializeField] private int partIndex = -1;
        [SerializeField] private PmxModelPartsController owner;
        [SerializeField] private SkinnedMeshRenderer targetRenderer;

        public int PartIndex => partIndex;
        public PmxModelPartsController Owner => owner;
        public PmxPartHierarchyMode Mode => owner != null
            ? owner.Mode
            : PmxPartHierarchyMode.ProxyNodes;
        public SkinnedMeshRenderer TargetRenderer => targetRenderer;
        public Material Material => owner != null ? owner.GetPartMaterial(partIndex) : null;

        public void SetMaterial(Material material)
        {
            if (owner == null) throw new InvalidOperationException("PMX part owner is missing.");
            owner.SetPartMaterial(partIndex, material);
        }

        public void ShowOnly() => owner?.ShowOnlyPart(partIndex);
        public void ShowAll() => owner?.ShowAllParts();

        internal void Configure(int index, PmxModelPartsController partsOwner,
            SkinnedMeshRenderer renderer)
        {
            partIndex = index;
            owner = partsOwner;
            targetRenderer = renderer;
        }
    }

    [DisallowMultipleComponent]
    public sealed class PmxModelPartsController : MonoBehaviour
    {
        [SerializeField] private PmxPartHierarchyMode mode;
        [SerializeField] private Mesh sharedMesh;
        [SerializeField] private Material[] materials = Array.Empty<Material>();
        [SerializeField] private SkinnedMeshRenderer canonicalRenderer;
        [SerializeField] private SkinnedMeshRenderer[] renderers =
            Array.Empty<SkinnedMeshRenderer>();
        [SerializeField] private PmxModelPart[] parts = Array.Empty<PmxModelPart>();
        [SerializeField] private int soloPartIndex = -1;
        [NonSerialized] private Mesh soloMesh;
        [NonSerialized] private Mesh originalCanonicalMesh;

        public PmxPartHierarchyMode Mode => mode;
        public Mesh SharedMesh => sharedMesh;
        public Material[] Materials => materials;
        public SkinnedMeshRenderer CanonicalRenderer => canonicalRenderer;
        public SkinnedMeshRenderer[] Renderers => renderers;
        public PmxModelPart[] Parts => parts;
        public int SoloPartIndex => soloPartIndex;

        internal void Configure(PmxPartHierarchyMode importedMode, Mesh mesh,
            Material[] importedMaterials, SkinnedMeshRenderer combinedRenderer,
            SkinnedMeshRenderer[] importedRenderers, PmxModelPart[] importedParts)
        {
            mode = importedMode;
            sharedMesh = mesh;
            materials = importedMaterials ?? Array.Empty<Material>();
            canonicalRenderer = combinedRenderer;
            renderers = importedRenderers ?? Array.Empty<SkinnedMeshRenderer>();
            parts = importedParts ?? Array.Empty<PmxModelPart>();
            soloPartIndex = -1;
            originalCanonicalMesh = mesh;
        }

        public Material GetPartMaterial(int partIndex)
        {
            ValidatePartIndex(partIndex);
            return materials[partIndex];
        }

        public void SetPartMaterial(int partIndex, Material material)
        {
            ValidatePartIndex(partIndex);
            materials[partIndex] = material;
            if (mode == PmxPartHierarchyMode.ProxyNodes)
            {
                if (canonicalRenderer == null) return;
                if (soloPartIndex >= 0)
                    canonicalRenderer.sharedMaterials = new[] { materials[soloPartIndex] };
                else
                    canonicalRenderer.sharedMaterials = CopyMaterials();
            }
            else
            {
                SkinnedMeshRenderer renderer = FindRenderer(partIndex);
                if (renderer != null) renderer.sharedMaterial = material;
            }
        }

        public void ShowOnlyPart(int partIndex)
        {
            ValidatePartIndex(partIndex);
            soloPartIndex = partIndex;
            if (mode == PmxPartHierarchyMode.ProxyNodes)
            {
                if (canonicalRenderer == null) return;
                if (originalCanonicalMesh == null) originalCanonicalMesh = canonicalRenderer.sharedMesh;
                if (soloMesh != null) UnityEngine.Object.DestroyImmediate(soloMesh);
                soloMesh = UnityEngine.Object.Instantiate(originalCanonicalMesh);
                soloMesh.name = originalCanonicalMesh.name + $" Solo Part {partIndex:D6}";
                soloMesh.subMeshCount = 1;
                soloMesh.SetIndices(originalCanonicalMesh.GetIndices(partIndex),
                    MeshTopology.Triangles, 0, false);
                soloMesh.RecalculateBounds();
                canonicalRenderer.sharedMesh = soloMesh;
                canonicalRenderer.sharedMaterials = new[] { materials[partIndex] };
                return;
            }

            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null)
                    renderers[i].enabled = i == partIndex;
        }

        public void ShowAllParts()
        {
            soloPartIndex = -1;
            if (mode == PmxPartHierarchyMode.ProxyNodes)
            {
                if (canonicalRenderer == null) return;
                if (originalCanonicalMesh != null) canonicalRenderer.sharedMesh = originalCanonicalMesh;
                if (soloMesh != null)
                {
                    UnityEngine.Object.DestroyImmediate(soloMesh);
                    soloMesh = null;
                }
                canonicalRenderer.sharedMaterials = CopyMaterials();
                return;
            }

            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null) renderers[i].enabled = true;
        }

        private SkinnedMeshRenderer FindRenderer(int partIndex)
        {
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null && i == partIndex)
                    return renderers[i];
            return null;
        }

        private Material[] CopyMaterials()
        {
            var result = new Material[materials.Length];
            Array.Copy(materials, result, materials.Length);
            return result;
        }

        private void ValidatePartIndex(int partIndex)
        {
            if (partIndex < 0 || partIndex >= materials.Length)
                throw new ArgumentOutOfRangeException(nameof(partIndex), partIndex,
                    $"PMX part index must be in [0, {materials.Length}).");
        }
    }
}
