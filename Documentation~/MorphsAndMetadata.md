# Morphs and versioned metadata

The importer converts PMX Vertex Morph records to Unity BlendShapes and preserves the
complete Morph source data in one versioned `PmxModelAsset` sub-asset. Runtime
controllers provide documented approximate effects for selected non-Vertex types.

## BlendShape conversion

Every PMX Vertex Morph produces one Unity BlendShape frame, including an empty
morph. Names are structural and independent of PMX display names:

```text
PMX Vertex Morph 000000
PMX Vertex Morph 000001
...
```

The number is the original PMX morph index. Japanese, English, empty and
duplicate names are preserved in metadata but cannot change BlendShape order
or identity.

Each frame allocates delta-vertex, delta-normal and delta-tangent arrays with
exactly `mesh.vertexCount` entries. Sparse offsets leave untouched entries at
zero. Multiple offsets targeting the same vertex are accumulated before the
frame is added. Position deltas use `PmxCoordinateConverter`, including import
scale and handedness conversion. An out-of-range vertex index aborts before a
frame is written.

`PmxModelAsset.MorphToBlendShapeIndex` and each `PmxMorphMetadata` record map
the original morph index to its Unity BlendShape index. Non-Vertex Morphs have
index -1.

## Support status

Runtime metadata and the custom Inspector use `PmxFeatureSupportStatus`:

- `Supported`: a direct converted effect exists, currently Vertex Morph BlendShapes.
- `Approximated`: a deliberately non-exact conversion exists, such as default
  materials or SDEF/QDEF in Approximate mode.
- `Preserved`: source data is serialized without a runtime effect, currently Additional
  UV 1-4 and Impulse Morphs.
- `Rejected`: input cannot be imported safely or under the selected policy.
- `Unsupported`: source data is preserved, but this package has no runtime backend for
  it, currently including PMX 2.1 SoftBody physics.

`PmxModelAssetInspector` always displays all five categories, status counts,
per-morph support and import diagnostics. A failed import has no model sub-asset
to inspect; its Rejected reason remains an Asset Import error.

## Metadata schema

`PmxModelAsset.CurrentSchemaVersion` is 5. The single `model/metadata` sub-asset
stores:

- original PMX version, encoding and index globals;
- original local/English model names and comments;
- original and normalized texture paths;
- bone names, model positions, parent, layer, raw flags, tail/inherit/fixed/local
  axis/external-parent fields, IK targets, loops and links;
- complete material colors, flags, texture/toon/environment semantics, memo and
  surface counts;
- every Morph, original names/panel/type, all typed offset values, support state
  and BlendShape mapping;
- DisplayFrame names, flags and elements;
- RigidBody, Joint and PMX 2.1 SoftBody fields, including anchors and pins;
- exact preserved SDEF/QDEF source values;
- structured import diagnostics with severity, status, code, section and offset.

This data is not copied to each bone or renderer. Runtime GameObjects contain the
Transform hierarchy, `SkinnedMeshRenderer`, and model-level controllers; editor
inspection and converters read the single `PmxModelAsset`.

## Runtime and preserved Morphs

Group, Flip, Bone, base UV, and Material Morphs have approximate deterministic runtime
effects and report `Approximated`. Additional UV 1-4 and Impulse Morphs remain metadata-
only and report `Preserved`. All original values and relationships remain available in
`PmxMorphMetadata`; the importer does not create placeholder effects for unsupported
semantics.
