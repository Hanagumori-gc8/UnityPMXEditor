# v0.1.0 Release Checklist

This checklist records release gates. Update the validation evidence before publishing;
do not infer runtime correctness from compilation alone.

## Package

- [x] Repository root is a valid `com.hanagumori.unity-pmx-editor` UPM package.
- [x] Unity 2022.3.60f1 resolves a local file dependency in a clean URP 14.0.12 project.
- [x] Unity resolves a temporary Git URL pinned to an exact final-candidate snapshot.
- [x] The authoritative GitHub URL resolves the pushed release-candidate commit.
- [x] Upgrade from the synthetic 0.0.9 validation predecessor to 0.1.0 completes without
      missing scripts.
- [x] Package Manager sample imports, reimports, overrides, and deletes cleanly.
- [x] No runtime dependency or compile-time API reference to URP, HDRP, or Built-in RP.

## Tests and Assets

- [x] Format, Runtime PlayMode, Editor, and importer integration suites all pass.
- [x] Format assembly has no Unity references; Runtime has no UnityEditor reference.
- [x] No missing `.meta`, duplicate GUID, broken asmdef, absolute machine path, or package-
      external source reference.
- [x] Unknown binaries and files larger than 1 MiB are reviewed and excluded or licensed.
- [x] Test fixtures are code-generated or self-authored with recorded provenance.
- [x] Import, reimport, stable sub-assets, delete, and optional physics disable are tested.

## Documentation and Legal

- [x] README distinguishes Parsed, Converted, Approximate, Preserved only, and Unsupported.
- [x] Installation, format limits, material/coordinate/runtime/physics behavior, support
      matrix, CHANGELOG, license, and Third Party Notices are present.
- [x] Documentation states that PMX model licenses do not inherit the plugin MIT license.
- [x] Built-in Render Pipeline and HDRP remain explicitly marked unvalidated.

## Publishing

- [x] Release-candidate commit reviewed and intentionally created.
- [x] Clean working tree after the release-candidate commit.
- [x] GitHub URL resolves the package from the pushed commit.
- [ ] Replace the `Unreleased` CHANGELOG marker with the release date when publishing.
- [ ] `v0.1.0` tag and GitHub Release created only with explicit user authorization.

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
