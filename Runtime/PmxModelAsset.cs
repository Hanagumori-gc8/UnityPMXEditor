using System;
using UnityEngine;

namespace Hanagumori.UnityPmx
{
    [Serializable]
    public sealed class PmxAdvancedDeformRecord
    {
        [SerializeField] private int vertexIndex;
        [SerializeField] private PmxVertexWeightType deformType;
        [SerializeField] private PmxFeatureSupportStatus supportStatus;
        [SerializeField] private int[] boneIndices = Array.Empty<int>();
        [SerializeField] private float[] weights = Array.Empty<float>();
        [SerializeField] private bool hasSdefParameters;
        [SerializeField] private Vector3 rawSdefC;
        [SerializeField] private Vector3 rawSdefR0;
        [SerializeField] private Vector3 rawSdefR1;

        public int VertexIndex => vertexIndex;
        public PmxVertexWeightType DeformType => deformType;
        public PmxFeatureSupportStatus SupportStatus => supportStatus;
        public int[] BoneIndices => boneIndices;
        public float[] Weights => weights;
        public bool HasSdefParameters => hasSdefParameters;
        public Vector3 RawSdefC => rawSdefC;
        public Vector3 RawSdefR0 => rawSdefR0;
        public Vector3 RawSdefR1 => rawSdefR1;

        internal static PmxAdvancedDeformRecord Create(int sourceVertexIndex, PmxVertexDeform deform,
            PmxFeatureSupportStatus status)
        {
            var record = new PmxAdvancedDeformRecord
            {
                vertexIndex = sourceVertexIndex,
                deformType = deform.Type,
                supportStatus = status,
                boneIndices = Copy(deform.BoneIndices),
                weights = Copy(deform.Weights),
                hasSdefParameters = deform.Type == PmxVertexWeightType.Sdef
            };
            if (deform.SdefC.HasValue) record.rawSdefC = ToRawVector(deform.SdefC.Value);
            if (deform.SdefR0.HasValue) record.rawSdefR0 = ToRawVector(deform.SdefR0.Value);
            if (deform.SdefR1.HasValue) record.rawSdefR1 = ToRawVector(deform.SdefR1.Value);
            return record;
        }

        private static int[] Copy(System.Collections.Generic.IReadOnlyList<int> values)
        {
            var result = new int[values.Count];
            for (int i = 0; i < result.Length; i++) result[i] = values[i];
            return result;
        }

        private static float[] Copy(System.Collections.Generic.IReadOnlyList<float> values)
        {
            var result = new float[values.Count];
            for (int i = 0; i < result.Length; i++) result[i] = values[i];
            return result;
        }

        private static Vector3 ToRawVector(PmxVector3 value) => new Vector3(value.X, value.Y, value.Z);
    }

    public sealed class PmxModelAsset : ScriptableObject
    {
        public const int CurrentSchemaVersion = 5;

        [SerializeField] private int schemaVersion = CurrentSchemaVersion;
        [SerializeField] private string modelName;
        [SerializeField] private string englishModelName;
        [SerializeField, TextArea] private string comment;
        [SerializeField, TextArea] private string englishComment;
        [SerializeField] private float pmxVersion;
        [SerializeField] private int vertexCount;
        [SerializeField] private int surfaceCount;
        [SerializeField] private Mesh mesh;
        [SerializeField] private Material[] materials = Array.Empty<Material>();
        [SerializeField] private string[] textureAssetPaths = Array.Empty<string>();
        [SerializeField] private bool usesApproximateMaterials;
        [SerializeField] private Transform[] bones = Array.Empty<Transform>();
        [SerializeField] private Transform rootBone;
        [SerializeField] private PmxAdvancedDeformMode advancedDeformMode;
        [SerializeField] private int advancedDeformVertexCount;
        [SerializeField] private int fallbackWeightVertexCount;
        [SerializeField] private PmxAdvancedDeformRecord[] advancedDeforms =
            Array.Empty<PmxAdvancedDeformRecord>();
        [SerializeField] private int[] morphToBlendShapeIndex = Array.Empty<int>();
        [SerializeField] private PmxHeaderMetadata header;
        [SerializeField] private string[] originalTexturePaths = Array.Empty<string>();
        [SerializeField] private PmxBoneMetadata[] boneMetadata = Array.Empty<PmxBoneMetadata>();
        [SerializeField] private PmxMaterialMetadata[] materialMetadata = Array.Empty<PmxMaterialMetadata>();
        [SerializeField] private PmxMorphMetadata[] morphMetadata = Array.Empty<PmxMorphMetadata>();
        [SerializeField] private PmxDisplayFrameMetadata[] displayFrameMetadata = Array.Empty<PmxDisplayFrameMetadata>();
        [SerializeField] private PmxRigidBodyMetadata[] rigidBodyMetadata = Array.Empty<PmxRigidBodyMetadata>();
        [SerializeField] private PmxJointMetadata[] jointMetadata = Array.Empty<PmxJointMetadata>();
        [SerializeField] private PmxSoftBodyMetadata[] softBodyMetadata = Array.Empty<PmxSoftBodyMetadata>();
        [SerializeField] private PmxPhysicsImportMode physicsImportMode;
        [SerializeField] private PmxImportDiagnostic[] diagnostics = Array.Empty<PmxImportDiagnostic>();

        public int SchemaVersion => schemaVersion;
        public string ModelName => modelName;
        public string EnglishModelName => englishModelName;
        public string Comment => comment;
        public string EnglishComment => englishComment;
        public float PmxVersion => pmxVersion;
        public int VertexCount => vertexCount;
        public int SurfaceCount => surfaceCount;
        public Mesh Mesh => mesh;
        public Material[] Materials => materials;
        public string[] TextureAssetPaths => textureAssetPaths;
        public bool UsesApproximateMaterials => usesApproximateMaterials;
        public Transform[] Bones => bones;
        public Transform RootBone => rootBone;
        public PmxAdvancedDeformMode AdvancedDeformMode => advancedDeformMode;
        public int AdvancedDeformVertexCount => advancedDeformVertexCount;
        public int FallbackWeightVertexCount => fallbackWeightVertexCount;
        public PmxAdvancedDeformRecord[] AdvancedDeforms => advancedDeforms;
        public int[] MorphToBlendShapeIndex => morphToBlendShapeIndex;
        public PmxHeaderMetadata Header => header;
        public string[] OriginalTexturePaths => originalTexturePaths;
        public PmxBoneMetadata[] BoneMetadata => boneMetadata;
        public PmxMaterialMetadata[] MaterialMetadata => materialMetadata;
        public PmxMorphMetadata[] MorphMetadata => morphMetadata;
        public PmxDisplayFrameMetadata[] DisplayFrameMetadata => displayFrameMetadata;
        public PmxRigidBodyMetadata[] RigidBodyMetadata => rigidBodyMetadata;
        public PmxJointMetadata[] JointMetadata => jointMetadata;
        public PmxSoftBodyMetadata[] SoftBodyMetadata => softBodyMetadata;
        public PmxPhysicsImportMode PhysicsImportMode => physicsImportMode;
        public PmxImportDiagnostic[] Diagnostics => diagnostics;

        internal void Initialize(PmxDocument document, Mesh importedMesh,
            Material[] importedMaterials, string[] importedTexturePaths, bool approximateMaterials,
            SkeletonConversionResult skeleton, PmxAdvancedDeformMode deformMode,
            SkinningConversionResult skinning, BlendShapeConversionResult blendShapes,
            PmxPhysicsImportMode importedPhysicsMode, PmxImportDiagnostic[] importDiagnostics)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            schemaVersion = CurrentSchemaVersion;
            modelName = document.Name;
            englishModelName = document.EnglishName;
            comment = document.Comment;
            englishComment = document.EnglishComment;
            pmxVersion = document.Header.Version;
            vertexCount = document.Vertices.Count;
            surfaceCount = document.Surfaces.Count;
            mesh = importedMesh;
            materials = importedMaterials ?? Array.Empty<Material>();
            textureAssetPaths = importedTexturePaths ?? Array.Empty<string>();
            usesApproximateMaterials = approximateMaterials;
            bones = skeleton != null ? skeleton.Bones : Array.Empty<Transform>();
            rootBone = skeleton?.RootBone;
            advancedDeformMode = deformMode;
            advancedDeformVertexCount = skinning?.AdvancedDeformVertexCount ?? 0;
            fallbackWeightVertexCount = skinning?.FallbackVertexCount ?? 0;
            var preserved = new System.Collections.Generic.List<PmxAdvancedDeformRecord>();
            for (int i = 0; i < document.Vertices.Count; i++)
            {
                PmxVertexDeform deform = document.Vertices[i].Deform;
                if (deform.Type == PmxVertexWeightType.Sdef || deform.Type == PmxVertexWeightType.Qdef)
                    preserved.Add(PmxAdvancedDeformRecord.Create(i, deform,
                        deformMode == PmxAdvancedDeformMode.Approximate
                            ? PmxFeatureSupportStatus.Approximated
                            : PmxFeatureSupportStatus.Preserved));
            }
            advancedDeforms = preserved.ToArray();
            morphToBlendShapeIndex = blendShapes != null
                ? (int[])blendShapes.MorphToBlendShapeIndex.Clone()
                : Array.Empty<int>();
            header = PmxMetadataFactory.Header(document.Header);
            originalTexturePaths = new string[document.Textures.Count];
            for (int i = 0; i < originalTexturePaths.Length; i++)
                originalTexturePaths[i] = document.Textures[i].Path;
            boneMetadata = PmxMetadataFactory.Bones(document.Bones);
            materialMetadata = PmxMetadataFactory.Materials(document.Materials);
            morphMetadata = PmxMetadataFactory.Morphs(document.Morphs, morphToBlendShapeIndex);
            displayFrameMetadata = PmxMetadataFactory.DisplayFrames(document.DisplayFrames);
            rigidBodyMetadata = PmxMetadataFactory.RigidBodies(document.RigidBodies);
            jointMetadata = PmxMetadataFactory.Joints(document.Joints);
            softBodyMetadata = PmxMetadataFactory.SoftBodies(document.SoftBodies);
            physicsImportMode = importedPhysicsMode;
            diagnostics = importDiagnostics ?? Array.Empty<PmxImportDiagnostic>();
        }
    }
}
