# v0.2.2 Release Checklist

This checklist records release gates. Update the validation evidence before publishing;
do not infer runtime correctness from compilation alone.

## Package

- [x] Repository root is a valid `com.hanagumori.unity-pmx-editor` UPM package.
- [x] Unity 2022.3.60f1 resolves the local file dependency and official FBX Exporter
      4.2.1 dependency in the URP 14.0.12 integration project.
- [ ] A clean project resolves a Git URL pinned to the final 0.2.2 commit.
- [ ] The authoritative GitHub URL resolves the pushed 0.2.2 release-candidate commit.
- [x] Upgrade from the synthetic 0.0.9 validation predecessor to 0.1.0 completes without
      missing scripts.
- [x] Local 0.1.1 reimport corrects PMX UV V orientation and removes zero-weight
      SDEF/QDEF rest-pose displacement on three representative models.
- [x] Package Manager sample imports, reimports, overrides, and deletes cleanly.
- [x] No runtime dependency or compile-time API reference to URP, HDRP, or Built-in RP.
- [x] Only the Editor assembly references `Unity.Formats.Fbx.Editor`; Format and Runtime
      retain their existing boundaries.

## Tests and Assets

- [x] Full Format, Runtime PlayMode, Editor, and importer integration suites pass on the
      final local 0.2.2 tree.
- [x] New FBX/OBJ/part/bone integration tests pass 4/4 in Unity EditMode.
- [x] Format assembly has no Unity references; Runtime has no UnityEditor reference.
- [x] No missing `.meta`, duplicate GUID, broken asmdef, absolute machine path, or package-
      external source reference.
- [x] Unknown binaries and files larger than 1 MiB are reviewed and excluded or licensed.
- [x] Test fixtures are code-generated or self-authored with recorded provenance.
- [x] Import, reimport, stable sub-assets, delete, and optional physics disable are tested.
- [x] Three local PMX models export to FBX and OBJ/MTL, synchronously reimport, preserve
      FBX bone counts and OBJ material-group counts, and contain no non-finite vertices.
- [x] Three local PMX models reimport with visible mesh/skeleton hierarchy nodes, editable
      scene-instance HideFlags, indexed source bone names, and verified BakeMesh movement.

## Documentation and Legal

- [x] README distinguishes Parsed, Converted, Approximate, Preserved only, and Unsupported.
- [x] Installation, format limits, material/coordinate/runtime/physics behavior, support
      matrix, CHANGELOG, license, and Third Party Notices are present.
- [x] Documentation states that PMX model licenses do not inherit the plugin MIT license.
- [x] Built-in Render Pipeline and HDRP remain explicitly marked unvalidated.
- [x] Third Party Notices identifies Unity FBX Exporter and Autodesk FBX SDK licensing.

## Publishing

- [ ] Release-candidate commit reviewed and intentionally created.
- [ ] Clean working tree after the release-candidate commit.
- [ ] GitHub URL resolves the package from the pushed commit.
- [ ] Replace the 0.2.2 `Unreleased` marker with an upload date when publishing.
- [ ] `v0.2.2` tag and GitHub Release created only with explicit user authorization.

## Validation evidence

- A clean Unity 2022.3.60f1 project with URP 14.0.12 resolved the local file dependency
  as package version 0.1.0. Sample initial import, PMX import, stable-ID reimport, override
  import, and deletion passed. Removing the manifest dependency also removed the package
  from Unity's registered packages and `packages-lock.json`.
- A separate clean project resolved temporary local Git commits with `source=Git`, moved
  from synthetic version 0.0.9 to 0.1.0, updated its lock hash, compiled, imported and
  reimported the Sample, and deleted it.
- Final Unity suites passed 27/27 EditMode tests and 23/23 PlayMode tests. The experimental
  physics fixture completed 3000 fixed frames, 60 simulated seconds at a 0.02-second
  timestep.
- A clean Unity 2022.3.60f1 URP 14.0.12 project resolved the current GitHub `main`
  through the authoritative HTTPS Git URL with `source=Git`, imported and reimported the
  Sample with stable local IDs, and deleted it. The earlier connection reset was
  transient and the successful retry supersedes it.
- The 0.2.0 integration project resolved `com.unity.formats.fbx` 4.2.1 and
  `com.autodesk.fbx` 4.2.1. New export tests passed 4/4: OBJ/MTL grouped round-trip, FBX
  skinned hierarchy round-trip, invalid-extension rejection, and Inspector data access.
- Three real PMX models with 21,242, 50,454, and 58,242 source vertices exported to both
  formats. FBX reimport retained 363, 631, and 568 bones. OBJ wrote 23, 46, and 43 groups
  and exactly 31,046, 73,231, and 84,070 face records. Unity removed two degenerate faces
  from the smallest model during downstream FBX/OBJ reimport; the emitted OBJ retained
  all source faces.
- The final local 0.2.0 tree passed 31/31 EditMode tests in 5.86 seconds and 23/23
  PlayMode tests in 60.18 seconds, including the 60-second experimental physics run.
- The local 0.2.2 tree passed 33/33 EditMode tests in 6.38 seconds and 23/23 PlayMode
  tests in 60.20 seconds. Three real PMX scene instances exposed 362, 630, and 567 PMX
  bones, contained no hidden/not-editable scene components, and produced approximately
  0.05 units of baked vertex movement after a weighted bone was moved.
