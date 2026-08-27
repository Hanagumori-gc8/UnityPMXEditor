# Runtime compatibility and deterministic order

The runtime uses independent `PmxMorphController` and `PmxBoneController`
components coordinated by one `PmxRuntimeController`. Each component references
the single `PmxModelAsset`; metadata arrays are not copied to every bone or
renderer.

## Frame order

Every evaluated frame uses this fixed model-local sequence:

1. expand Group and Flip Morph dependencies in a precomputed topological order;
2. apply Vertex BlendShape weights, base UV deltas and MaterialPropertyBlocks;
3. reset bones to the import baseline and apply Bone Morph deltas;
4. apply inherit/grant in `(deformation layer, PMX bone index)` order;
5. solve IK in the same deterministic bone order.

The controllers do not rely on recursive Morph evaluation or MonoBehaviour
execution order between themselves. `PmxRuntimeController` is the only updater
and runs at `DefaultExecutionOrder(10000)`. Repeating a frame with identical
weights starts from the same baseline and produces the same transforms,
BlendShape weights, UV values and property blocks.

## Morph semantics

Group and Flip offsets propagate `sourceWeight * offsetWeight`. Dependencies are
validated and topologically sorted during initialization. Any runtime metadata
cycle is rejected before the first frame, without recursion.

Multiple direct, Group and Flip contributions add at the target Morph. Setting
all direct weights to zero restores BlendShapes, UVs, materials and the
post-IK zero-Morph baseline.

Bone Morph translations and quaternions accumulate in PMX Morph index order.
The result is applied before grant and IK, so those stages operate on the
morphed pose instead of replacing it with the import pose.

Base UV Morph uses the X/Y offset. Additional UV 1-4 records remain Preserved
because Mesh conversion does not create those channels.

Material Morph uses one preallocated `MaterialPropertyBlock` per material slot.
Multiply and Add operations affect the approximate diffuse, specular and
smoothness properties. Edge, toon, sphere-map and texture-tint behavior remains
metadata-only where the default shader has no equivalent. No Material is
instantiated during frame evaluation.

## Bone grant and IK differences

Translation grant applies the grant parent's local-position delta from its
import baseline. Rotation grant applies a weighted local rotation delta. This
is deterministic but may differ from MMD for complex append-transform chains,
external-parent transforms, negative grant weights and physics-after-deform
bones.

IK uses bounded CCD after Bone Morph and grant. Per-link angular limits are
applied through clamped local Euler radians, and loop count is capped at 256 to
protect runtime stability. This does not claim bit-identical MMD IK behavior,
especially for knee constraints, axis conventions, singular chains or models
that depend on MMD-specific numerical quirks.

## Capability paths

`StandardApproximate` is the default and explicitly accepts the documented
linear Material, UV, grant and IK approximations.

`MmdCompatible` selects the MMD-oriented deterministic update order and strict
capability checks. It is still reported as Approximated while the differences
above remain.

There is no dedicated SDEF/QDEF backend in 0.1.1. If a model contains either
deform and requests MmdCompatible, the configured policy must:

- `Reject`, producing a compatibility error; or
- `DowngradeToStandardApproximate`, changing the active path and recording an
  explicit diagnostic.

Changing only a mode name never upgrades SDEF/QDEF or the overall compatibility
status to exact support.

## Allocation policy

Morph weights, effective weights, dependency order, bone deltas, baseline and
working UVs, material state, MaterialPropertyBlocks, deformation order and IK
state are allocated during initialization. The UV Mesh clone is also created
once per runtime model instance.

The frame path contains no LINQ, collection construction, string formatting,
Material creation or array-returning renderer property access. The acceptance
test warms the controller and measures `GC.GetAllocatedBytesForCurrentThread`
across 1,000 full evaluations; the required result is exactly zero managed
bytes, while Mesh and Material instance IDs remain unchanged.
