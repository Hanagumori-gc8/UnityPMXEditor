# Changelog

All notable changes to this package are documented in this file.

## [0.2.0] - 2026-08-28

### Added

- FBX export for imported PMX assets and scene instances through Unity's official FBX
  Exporter 4.2.1, preserving the Generic hierarchy, skinned mesh, materials, and data the
  official exporter can represent.
- Bounded OBJ/MTL export of the current static pose with finite-value and index
  validation, right-handed coordinates, one stable group per PMX material submesh, and
  referenced diffuse textures.
- Model-part and bone lists in PMX inspectors, bone selection controls, and Scene-view
  bone Gizmos for selected PMX instances.
- Asset-menu and Inspector export commands for FBX and OBJ.

### Dependencies

- Add Editor-only use of `com.unity.formats.fbx` 4.2.1. The Format and Runtime
  assemblies remain independent of UnityEditor and the FBX SDK.

### Known limitations

- OBJ is a static geometry format and does not contain bones, skinning, BlendShapes,
  Morph controllers, animation, physics, or PMX metadata.
- FBX output is limited to semantics supported by Unity's official exporter. PMX/MMD
  runtime controllers, IK/grant execution rules, physics, and unsupported SDEF/QDEF
  semantics are not reconstructed as equivalent MMD behavior in FBX.

## [0.1.1] - 2026-08-28

### Fixed

- Convert PMX texture V coordinates and base UV Morph Y deltas to Unity's sampling
  convention.
- Preserve SDEF/QDEF-only vertices at their rest positions with a model-space anchor in
  `PreserveOnly` mode instead of emitting zero-sum Unity bone weights.

### Changed

- Increment the PMX scripted importer version so existing assets are reimported with the
  corrected UV and skinning conversion.
- Document that `PreserveOnly` retains SDEF/QDEF source data and rest geometry but does
  not apply SDEF/QDEF deformation at runtime.

## [0.1.0] - 2026-08-28

### Added

- Root-layout UPM package with Format, Runtime, Editor, Runtime.Tests, and Editor.Tests
  assembly boundaries for Unity 2022.3.
- Pure C# bounded PMX 2.0/2.1 reader with UTF-16LE/UTF-8, dynamic indices, all required
  sections, complete reference validation, and section/byte-offset errors.
- Static mesh, submesh, texture dependency, approximate material, coordinate, skeleton,
  BDEF skinning, bindpose, and Generic hierarchy conversion.
- Stable structural sub-asset identifiers for Mesh, Materials, `PmxModelAsset`, and
  experimental physics materials.
- Vertex BlendShapes and versioned metadata for original names/comments, bones, IK,
  Morphs, display frames, materials, rigid bodies, joints, and PMX 2.1 SoftBody.
- Deterministic Group/Flip, Bone, base UV, and Material Morph runtime evaluation;
  inherit/grant and bounded IK; explicit StandardApproximate/MmdCompatible capability
  reporting.
- Optional experimental Sphere/Box/Capsule rigid bodies and PMX Spring 6DOF to Unity
  PhysX conversion, disabled by default, with SoftBody reported as Unsupported.
- Package Manager sample containing a self-authored minimal PMX fixture.
- Installation, support, format, coordinate, material, runtime, physics, licensing, and
  release-checklist documentation.

### Known limitations

- Built-in Render Pipeline and HDRP are not validated.
- MMD toon, sphere-map, edge rendering, additional UV Morph effects, Impulse Morph, PMX
  2.1 SoftBody, and non-Spring-6DOF joints do not have complete runtime backends.
- SDEF/QDEF, Material Morph, grants, IK, and experimental physics have documented
  approximation or preservation policies and are not frame-identical to MMD.
