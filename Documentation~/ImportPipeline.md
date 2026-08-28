# Import pipeline

The importer converts a validated `PmxDocument` into Unity assets, then configures the
Generic skeleton, skinning, versioned metadata, runtime controllers, and optional
experimental physics. Individual conversion stages remain one-way and do not mutate the
Format document.

## Asset flow

```text
.pmx source
  -> PmxBinaryReader
  -> PmxValidator
  -> PmxCoordinateConverter
  -> MeshConverter + IMaterialConverter
  -> root GameObject
       |-- PMX Model Parts
       |    |-- PMX Part 000000 ...
       |    `-- PMX Part 000001 ...
       |-- PMX Mesh (ProxyNodes only)
       `-- PMX Skeleton (indexed source-name bones)
  -> stable Mesh, Material, and PmxModelAsset sub-assets
```

`PmxScriptedImporter` is editor-only. `PmxModelAsset`, the coordinate and mesh
converters, and `IMaterialConverter` are runtime types and do not reference
`UnityEditor`.

## Part hierarchy modes

`PmxImportSettings.PartHierarchyMode` controls the scene representation:

- `ProxyNodes` (default) creates one canonical `PMX Mesh` renderer and stable
  `PMX Part {index:D6}` proxy nodes under `PMX Model Parts`. Proxy nodes expose
  selection, material assignment and solo/show-all controls; their transforms do
  not alter the shared geometry.
- `SeparateRenderers` creates one stable part node and one `SkinnedMeshRenderer`
  per PMX material declaration. Each renderer receives a generated one-submesh
  Mesh sub-asset with the original vertex channels, skinning data and BlendShapes,
  so parts can be selected and transformed independently. This intentionally
  increases renderer and draw-object count.

Both modes retain the same PMX material order and metadata. Morph evaluation sends
vertex BlendShape, UV and MaterialPropertyBlock updates to every active part renderer;
there is no per-frame Material instantiation. Exporters inspect the active mode so
OBJ and FBX do not repeat all material ranges for each separate renderer.

An optional post-conversion physics path is available. `Physics=None` remains the default
and leaves PMX physics records as metadata only. `Physics=Experimental` builds
the documented PhysX approximation after mesh, skeleton, skinning, and runtime
controller construction. See `PhysicsCompatibility.md` for mappings and limitations.

## Stable asset identities

AssetImportContext identifiers are structural and index-based:

- root GameObject: `main/root`
- Mesh: `mesh/000000`
- PmxModelAsset: `model/metadata`
- Materials: `material/000000`, `material/000001`, and so on

PMX names remain display metadata only. Japanese, empty or duplicate names do
not participate in sub-asset identity. Tests reimport a fixture after changing
its texture and assert that Mesh, Material and PmxModelAsset local file IDs do
not change.

## Coordinates and geometry

All handedness and unit conversion is owned by `PmxCoordinateConverter`:

- positions apply the configured scale and negate Z;
- normals negate Z and normalize;
- base UV values preserve U and convert V as `1 - v` for Unity texture sampling;
- base UV Morph deltas preserve X and negate Y under the same convention;
- triangle B/C indices are swapped after the handedness change;
- scalar distances use the same configured scale.

`MeshConverter` never flips coordinates directly. It creates one submesh for
each PMX material, consuming the material surface counts in declaration order.
The counts must cover the full Surface index list exactly. Meshes with more
than 65,535 vertices use `IndexFormat.UInt32`.

## Texture paths and dependencies

Texture table entries are converted to forward-slash Unity asset paths relative
to the PMX source directory. Sources under `Assets/` remain under `Assets/`;
sources under `Packages/` remain under `Packages/`.

The importer rejects:

- `..` directory traversal segments;
- drive-letter, rooted, UNC and URI-like paths;
- NUL characters and non-portable asset-name characters;
- empty texture paths.

Existing `Texture2D` assets are loaded through `AssetDatabase` and registered
with `AssetImportContext.DependsOnSourceAsset`. Missing textures produce an
import warning and a material without that texture. The test fixture verifies
that changing a referenced texture changes the PMX dependency hash and causes
the dependent import artifact to update.

## Default material approximation

`IMaterialConverter` isolates render-pipeline-specific material policy. The
core package has no URP dependency. `DefaultMaterialConverter` uses the active
render pipeline's default material shader when available, falling back to a
built-in shader by name.

The default converter maps diffuse color, main texture, specular color and a
coarse smoothness value. It deliberately does not claim to reproduce MMD toon
lighting, sphere-map blending or PMX edge rendering. Imported material names,
`PmxModelAsset.UsesApproximateMaterials`, documentation and an import warning
all label this behavior as an approximation.
