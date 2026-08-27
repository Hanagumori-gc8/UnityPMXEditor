# Changelog

All notable changes to this package are documented in this file.

## [0.1.0] - Unreleased

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
