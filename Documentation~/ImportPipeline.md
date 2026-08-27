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
  -> root GameObject and stable sub-assets
```

`PmxScriptedImporter` is editor-only. `PmxModelAsset`, the coordinate and mesh
converters, and `IMaterialConverter` are runtime types and do not reference
`UnityEditor`.

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
- UV values are copied without a coordinate-system edit;
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
