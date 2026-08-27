namespace Hanagumori.UnityPmx
{
    public readonly struct PmxVector2
    {
        public PmxVector2(float x, float y) { X = x; Y = y; }
        public float X { get; }
        public float Y { get; }
    }

    public readonly struct PmxVector3
    {
        public PmxVector3(float x, float y, float z) { X = x; Y = y; Z = z; }
        public float X { get; }
        public float Y { get; }
        public float Z { get; }
    }

    public readonly struct PmxVector4
    {
        public PmxVector4(float x, float y, float z, float w) { X = x; Y = y; Z = z; W = w; }
        public float X { get; }
        public float Y { get; }
        public float Z { get; }
        public float W { get; }
    }

    public enum PmxTextEncoding : byte
    {
        Utf16LittleEndian = 0,
        Utf8 = 1
    }

    public enum PmxVertexWeightType : byte
    {
        Bdef1 = 0,
        Bdef2 = 1,
        Bdef4 = 2,
        Sdef = 3,
        Qdef = 4
    }

    public enum PmxMorphType : byte
    {
        Group = 0,
        Vertex = 1,
        Bone = 2,
        Uv = 3,
        AdditionalUv1 = 4,
        AdditionalUv2 = 5,
        AdditionalUv3 = 6,
        AdditionalUv4 = 7,
        Material = 8,
        Flip = 9,
        Impulse = 10
    }

    [System.Flags]
    public enum PmxMaterialFlags : byte
    {
        None = 0,
        DisableCulling = 1 << 0,
        GroundShadow = 1 << 1,
        DrawShadow = 1 << 2,
        ReceiveShadow = 1 << 3,
        DrawEdge = 1 << 4,
        VertexColor = 1 << 5,
        DrawPoint = 1 << 6,
        DrawLine = 1 << 7
    }

    [System.Flags]
    public enum PmxBoneFlags : ushort
    {
        None = 0,
        IndexedTail = 0x0001,
        Rotatable = 0x0002,
        Translatable = 0x0004,
        Visible = 0x0008,
        Enabled = 0x0010,
        InverseKinematics = 0x0020,
        InheritRotation = 0x0100,
        InheritTranslation = 0x0200,
        FixedAxis = 0x0400,
        LocalCoordinates = 0x0800,
        PhysicsAfterDeform = 0x1000,
        ExternalParent = 0x2000
    }

    [System.Flags]
    public enum PmxSoftBodyFlags : byte
    {
        None = 0,
        BLink = 1 << 0,
        CreateClusters = 1 << 1,
        LinkCrossing = 1 << 2
    }
}
