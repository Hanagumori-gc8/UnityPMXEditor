using System.Collections.Generic;

namespace Hanagumori.UnityPmx
{
    public sealed class PmxSoftBodyConfig
    {
        internal PmxSoftBodyConfig(float vcf, float dp, float dg, float lf, float pr, float vc,
            float df, float mt, float chr, float khr, float shr, float ahr)
        { Vcf = vcf; Dp = dp; Dg = dg; Lf = lf; Pr = pr; Vc = vc; Df = df; Mt = mt; Chr = chr; Khr = khr; Shr = shr; Ahr = ahr; }
        public float Vcf { get; }
        public float Dp { get; }
        public float Dg { get; }
        public float Lf { get; }
        public float Pr { get; }
        public float Vc { get; }
        public float Df { get; }
        public float Mt { get; }
        public float Chr { get; }
        public float Khr { get; }
        public float Shr { get; }
        public float Ahr { get; }
    }

    public sealed class PmxSoftBodyCluster
    {
        internal PmxSoftBodyCluster(float srhr, float skhr, float sshr, float srSplit, float skSplit, float ssSplit)
        { Srhr = srhr; Skhr = skhr; Sshr = sshr; SrSplit = srSplit; SkSplit = skSplit; SsSplit = ssSplit; }
        public float Srhr { get; }
        public float Skhr { get; }
        public float Sshr { get; }
        public float SrSplit { get; }
        public float SkSplit { get; }
        public float SsSplit { get; }
    }

    public sealed class PmxSoftBodyIteration
    {
        internal PmxSoftBodyIteration(int velocity, int position, int drift, int cluster)
        { Velocity = velocity; Position = position; Drift = drift; Cluster = cluster; }
        public int Velocity { get; }
        public int Position { get; }
        public int Drift { get; }
        public int Cluster { get; }
    }

    public sealed class PmxSoftBodyMaterial
    {
        internal PmxSoftBodyMaterial(float linearStiffness, float angularStiffness, float volumeStiffness)
        { LinearStiffness = linearStiffness; AngularStiffness = angularStiffness; VolumeStiffness = volumeStiffness; }
        public float LinearStiffness { get; }
        public float AngularStiffness { get; }
        public float VolumeStiffness { get; }
    }

    public sealed class PmxSoftBodyAnchor
    {
        internal PmxSoftBodyAnchor(long sourceOffset, int rigidBodyIndex, int vertexIndex, byte rawNearMode)
        { SourceOffset = sourceOffset; RigidBodyIndex = rigidBodyIndex; VertexIndex = vertexIndex; RawNearMode = rawNearMode; }
        internal long SourceOffset { get; }
        public int RigidBodyIndex { get; }
        public int VertexIndex { get; }
        public byte RawNearMode { get; }
    }

    public sealed class PmxSoftBody
    {
        internal PmxSoftBody(long sourceOffset, string name, string englishName, byte rawShape,
            int materialIndex, byte rawCollisionGroup, ushort rawNonCollisionMask, byte rawFlags,
            int bLinkDistance, int clusterCount, float totalMass, float collisionMargin,
            int rawAerodynamicsModel, PmxSoftBodyConfig config, PmxSoftBodyCluster cluster,
            PmxSoftBodyIteration iteration, PmxSoftBodyMaterial material,
            List<PmxSoftBodyAnchor> anchors, List<int> pinnedVertexIndices, List<long> pinOffsets)
        {
            SourceOffset = sourceOffset;
            Name = name;
            EnglishName = englishName;
            RawShape = rawShape;
            MaterialIndex = materialIndex;
            RawCollisionGroup = rawCollisionGroup;
            RawNonCollisionMask = rawNonCollisionMask;
            RawFlags = rawFlags;
            BLinkDistance = bLinkDistance;
            ClusterCount = clusterCount;
            TotalMass = totalMass;
            CollisionMargin = collisionMargin;
            RawAerodynamicsModel = rawAerodynamicsModel;
            Config = config;
            Cluster = cluster;
            Iteration = iteration;
            Material = material;
            Anchors = anchors;
            PinnedVertexIndices = pinnedVertexIndices;
            PinOffsets = pinOffsets;
        }

        internal long SourceOffset { get; }
        public string Name { get; }
        public string EnglishName { get; }
        public byte RawShape { get; }
        public int MaterialIndex { get; }
        public byte RawCollisionGroup { get; }
        public ushort RawNonCollisionMask { get; }
        public byte RawFlags { get; }
        public PmxSoftBodyFlags Flags => (PmxSoftBodyFlags)RawFlags;
        public int BLinkDistance { get; }
        public int ClusterCount { get; }
        public float TotalMass { get; }
        public float CollisionMargin { get; }
        public int RawAerodynamicsModel { get; }
        public PmxSoftBodyConfig Config { get; }
        public PmxSoftBodyCluster Cluster { get; }
        public PmxSoftBodyIteration Iteration { get; }
        public PmxSoftBodyMaterial Material { get; }
        public IReadOnlyList<PmxSoftBodyAnchor> Anchors { get; }
        public IReadOnlyList<int> PinnedVertexIndices { get; }
        internal IReadOnlyList<long> PinOffsets { get; }
    }
}
