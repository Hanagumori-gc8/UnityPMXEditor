# Generic skeleton and skinning

The importer creates a PMX skeleton as a Generic Transform hierarchy and binds the
static mesh to `SkinnedMeshRenderer`. It does not create an `Animator`, an
`Avatar`, or a Humanoid mapping.

## Stable hierarchy

`SkeletonConverter` validates the parent graph without recursion, creates every
bone first, and assigns parents in a second pass. This supports a child whose
parent has a larger PMX index. Cycles, self-parenting, indices below -1 and
indices beyond the Bone section are rejected before hierarchy construction.

Hierarchy names and creation order are index-based:

- container: `PMX Skeleton`
- bone N: `PMX Bone 000000`, `PMX Bone 000001`, and so on

PMX Japanese or duplicate display names do not control hierarchy identity.
With exactly one top-level PMX bone, that Transform is `rootBone`. Multiple
top-level bones use the stable `PMX Skeleton` container as `rootBone`.

PMX bone positions are model-space positions. A child local position is its
converted model position minus its converted parent position. Top-level bones
use their converted PMX model position relative to the skeleton container.

Each bindpose is:

```text
bone.worldToLocalMatrix * modelRoot.localToWorldMatrix
```

At the import rest pose, `modelRoot.worldToLocalMatrix * bone.localToWorldMatrix
* bindpose` is identity.

## BDEF conversion

`SkinningConverter` converts BDEF1, BDEF2 and BDEF4 to Unity `BoneWeight`:

- -1 bone indices are ignored;
- zero influences are skipped;
- duplicate bone indices are merged;
- negative, NaN and infinite weights are rejected;
- remaining positive weights are normalized even when the PMX sum is not 1;
- if no positive influence remains, the first valid source bone is used with
  weight 1, otherwise bone 0 is used as a deterministic fallback;
- fallback vertices are counted in `PmxModelAsset`.

The same weights and bindposes apply when the mesh uses `IndexFormat.UInt32`.

## SDEF and QDEF policy

`PmxImportSettings.AdvancedDeformMode` has three explicit modes:

- `Strict`: aborts import when SDEF or QDEF is encountered because 0.1.1 has
  no exact implementation.
- `Approximate`: applies the stored linear bone indices and weights as BDEF and
  emits an explicit warning that the result is not exact SDEF/QDEF support.
- `PreserveOnly` (default): serializes the original deform data and binds those vertices
  to a model-space preservation anchor. This keeps the rest mesh intact without
  pretending to evaluate SDEF/QDEF; the preserved vertices remain static relative to
  the model while PMX bones animate.

Both Approximate and PreserveOnly save `PmxAdvancedDeformRecord` entries in the
`PmxModelAsset`, including vertex index, deform type, original bone indices,
weights and raw SDEF C/R0/R1 vectors. Approximation never destroys the original
parameters or changes the support label to exact.

## Validation evidence

The generated tests cover a parent listed after its child, rest-pose bindpose
identity, local positions, BDEF1/2/4, -1 indices, zero weights, non-normalized
weights, deterministic fallback, cycle rejection, a no-bone degenerate model,
65,536 vertices, stable bone local file IDs, stable metadata references, a rest-pose
preservation anchor and all three advanced-deform modes.

The visual test imports a generated skinned fixture, instantiates its root,
rotates a child bone, calls `SkinnedMeshRenderer.BakeMesh`, renders rest and
rotated images through an actual Unity graphics device, and requires a material
pixel difference before passing.
