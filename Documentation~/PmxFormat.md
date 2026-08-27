# PMX format layer

The `Hanagumori.UnityPmx.Format` assembly reads PMX 2.0 and 2.1 without any
`UnityEngine` or `UnityEditor` dependency. It is the first stage of the import
pipeline and produces a `PmxDocument`; it does not create Unity assets.

## Format sources

The binary field order and value meanings are based on Felix Jones' community
English description of the PMX 2.1 format:

- <https://gist.github.com/felixjones/f8a06bd48f9da9a4539f>

That description identifies itself as a work in progress. UnityPMXEditor does
not copy or link a third-party parser. The reader and generated fixtures are
implemented independently from the documented binary layout. The project
keeps raw flag and discriminator values wherever their payload size is known,
so later corrections to semantic names do not destroy source data.

## Supported layout

- PMX versions 2.0 and 2.1
- Little-endian numeric fields
- UTF-16LE and UTF-8 text
- Unsigned 1, 2, and 4 byte vertex indices
- Signed 1, 2, and 4 byte texture, material, bone, morph, and rigid-body indices
- BDEF1, BDEF2, BDEF4, SDEF, and PMX 2.1 QDEF
- Vertex, Surface, Texture, Material, Bone, Morph, DisplayFrame, RigidBody,
  Joint, and PMX 2.1 SoftBody sections
- Group, Vertex, Bone, UV, Additional UV 1-4, Material, Flip, and Impulse morphs

PMX 2.0 ends after the Joint section. PMX 2.1 contains a SoftBody count even
when the count is zero.

## Index policy

Vertex indices are decoded as unsigned values. In particular, `0xFF` in a
one-byte vertex index is 255, not -1. A four-byte vertex value greater than
`Int32.MaxValue` is rejected because all in-memory collection counts are
bounded signed integers.

All other dynamic index categories are decoded as signed values. Their common
nil value -1 is preserved and accepted; values below -1 or at/above the target
section count are rejected during document validation.

## Validation and errors

`PmxBinaryReader` validates before allocating or returning a document:

- signature, version, global count and each global value;
- strict text decoding, non-negative byte lengths, UTF-16 alignment and string limits;
- non-negative section/nested counts, per-section limits and a total item budget;
- conservative remaining-byte requirements before collection construction;
- triangle alignment and the material-to-surface total;
- every vertex and signed cross-section reference;
- bone-parent and group/flip-morph cycles without recursive traversal;
- PMX-version restrictions for QDEF, Flip, Impulse and SoftBody;
- exact end-of-stream consumption, rejecting both truncation and trailing bytes.

Malformed input raises `PmxFormatException`. Its message and properties always
include the logical section and a byte offset, for example `Surface` at byte
offset `0x1234`.

Unknown bits in material, bone and soft-body flags remain available through
their `RawFlags` properties. Other raw discriminator values are also retained
when the record size remains deterministic. An unknown value that changes the
payload layout is rejected instead of being skipped or guessed.

## Configurable limits

`PmxReadLimits` controls file bytes, string bytes, every top-level section,
IK links, morph offsets, display elements, soft-body anchors/pins and a total
collection-item budget. Counts are checked before creating lists. Initial list
capacity is capped even for a count that is below its configured maximum, so a
hostile count cannot force one immediate count-sized allocation.

Applications processing untrusted uploads should lower the defaults to match
their expected model sizes.

## Fixture provenance

`Tests/Runtime/PmxFixtureBuilder.cs` writes its PMX bytes directly with
`BinaryWriter` from the field order above. It does not embed, transform or
redistribute an MMD model. Tests generate both encodings, all index widths,
all supported deforms and morphs, and the PMX 2.1 SoftBody payload. Corrupt
cases patch recorded byte offsets for signature, length, count and index tests.

The repository-local `Resources` model is excluded from Git. It may be used
for local read-only integration checks, but it is not a committed fixture and
its content or license is not attributed to this package.
