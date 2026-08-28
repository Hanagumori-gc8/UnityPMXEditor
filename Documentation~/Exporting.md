# Model Parts, Bone Gizmos, and Export

## Inspect model parts

Select the `PmxModelAsset` sub-asset below an imported `.pmx`, or select a PMX scene
instance and inspect its `PmxRuntimeController`. The **Model Parts (Material Submeshes)**
foldout lists the stable material index, PMX display name, triangle count, and Unity
Material for every contiguous PMX material surface range.

The importer exposes two `Part Hierarchy Mode` choices. `ProxyNodes` keeps one canonical
`SkinnedMeshRenderer` and creates selectable proxy nodes; `SeparateRenderers` creates one
renderer and one generated one-submesh Mesh sub-asset per material part. The latter costs
more renderer objects but permits independent Transform edits. Both modes keep stable
indexed identities and deterministic Morph evaluation. OBJ export provides a stable `g`
group per material part in either mode.

## Inspect bones

Select a PMX model instance in a scene. Its `PmxRuntimeController` Inspector lists every
bone with source name, stable index, parent index, and deformation layer. **Select** moves
the Unity selection to that bone Transform. With the PMX root selected, the Scene view
draws cyan parent links and yellow joint Gizmos. The imported hierarchy remains Generic;
the tool does not fabricate a Humanoid Avatar.

## Export commands

Select an imported PMX main asset, its `PmxModelAsset` sub-asset, or a PMX scene instance.
Use either the Inspector buttons or:

- **Assets > UnityPMXEditor > Export Selected as FBX...**
- **Assets > UnityPMXEditor > Export Selected as OBJ...**

An output written below the current Unity project is synchronously imported. An output
outside the project is left as an external file.

## FBX boundary

FBX export uses Unity's official `com.unity.formats.fbx` 4.2.1 package, the released
version for Unity 2022.3. It exports an isolated copy at the origin, preserving the
Generic Transform hierarchy, SkinnedMeshRenderer, bone weights, materials, and other data
supported by the official exporter.

FBX does not make UnityPMXEditor runtime behavior equivalent to MMD. PMX display frames,
controller components, Group/Flip evaluation, IK/grant ordering, Bullet physics, and
preserved-only SDEF/QDEF records are not portable FBX runtime semantics. Always inspect
the exported file in the target DCC or engine.

## OBJ boundary

OBJ export bakes every `SkinnedMeshRenderer` to the current visible pose. It validates
finite vertices/normals/UVs, triangle topology, every index, and bounded total counts.
Unity left-handed coordinates are reflected to right-handed OBJ coordinates and triangle
winding is reversed consistently. Each submesh is written as a stable `g` group with a
matching `usemtl`; the adjacent `.mtl` contains approximate diffuse/specular values and a
relative main-texture reference when available.

OBJ contains no bones, skinning, BlendShapes, animation, Morph controllers, physics, or
PMX metadata. Importers may duplicate vertices at group or material boundaries; that is
normal and does not mean the exporter wrote duplicate `v` records. Unity's ModelImporter
may also weld vertices or remove degenerate triangles while reimporting FBX/OBJ. Compare
the emitted OBJ `v`/`f`/`g` records when auditing the file writer separately from Unity's
downstream import optimization.

## Licensing

The Unity FBX Exporter and Autodesk FBX SDK dependencies retain their own licenses; see
`Third Party Notices.md`. Exporting a PMX model does not change the model, texture,
character, or distribution license. The UnityPMXEditor MIT license applies only to this
repository's code and self-authored fixture.
