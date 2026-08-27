# Minimal PMX Fixture

`MinimalFixture.pmx` is a self-authored PMX 2.0 validation asset. It contains:

- UTF-8 model metadata;
- three vertices and one triangle;
- one material with no texture;
- one root bone and BDEF1 weights;
- no Morph, display-frame, rigid-body, joint, or SoftBody records.

The fixture is generated directly from the PMX field layout and is not derived from an
MMD model, character, texture, or third-party parser. It is covered by the package MIT
license.

Select the PMX asset after importing this sample. Expected results are a root
`GameObject`, one `SkinnedMeshRenderer`, one three-vertex Mesh, one material, one bone,
and one `PmxModelAsset` sub-asset. Physics remains disabled by default.
