# Support Matrix

The status vocabulary is part of the compatibility contract:

- **Parsed** means validated binary data reaches `PmxDocument` and versioned metadata.
- **Converted** means a Unity-native representation or intended runtime behavior exists.
- **Approximate** means the mapping works but is not semantically or numerically identical
  to MMD.
- **Preserved only** means metadata is retained without a runtime effect.
- **Unsupported** means no backend exists and diagnostics say so explicitly.

| Area | Feature | Status | Boundary |
| --- | --- | --- | --- |
| Format | PMX 2.0/2.1 little-endian | Parsed | Other versions and trailing bytes rejected |
| Format | UTF-16LE and UTF-8 | Parsed | Strict decoding and bounded lengths |
| Format | 1/2/4-byte indices | Parsed | Vertex unsigned; other categories signed with `-1` |
| Geometry | Position/normal/base UV/triangles | Converted | Handedness, winding, scale, and UV V conversion applied centrally |
| Geometry | Material surface ranges | Converted | Exact full Surface coverage required |
| Material | Diffuse/main texture/specular | Approximate | Active-pipeline default shader, no pipeline API reference |
| Material | Toon/sphere-map/edge semantics | Preserved only | No MMD shader backend |
| Skeleton | Bone hierarchy/bindposes | Converted | Generic only; cycles and degenerate parents rejected |
| Skinning | BDEF1/BDEF2/BDEF4 | Converted | Invalid/zero weights use documented deterministic policy |
| Skinning | SDEF/QDEF records | Parsed | Approximate or preserved-only import policy |
| Morph | Vertex | Converted | Stable full-length BlendShapes |
| Morph | Group/Flip | Approximate | Iterative topological expansion; cycles rejected |
| Morph | Bone/base UV/Material | Approximate | Deterministic controller with documented differences |
| Morph | Additional UV 1-4/Impulse | Preserved only | No runtime effect |
| Bone runtime | Inherit/grant/deformation layer | Approximate | Model-local deterministic order |
| Bone runtime | IK | Approximate | Bounded CCD, not MMD numerical equivalence |
| Display | Display frames | Preserved only | Inspector/model metadata only |
| Physics | Physics=None | Converted | No physics components; mesh/skeleton remain usable |
| Physics | Sphere/Box/Capsule and modes 0/1/2 | Approximate | Experimental PhysX path |
| Physics | Spring 6DOF type 0 | Approximate | `ConfigurableJoint` limit/drive differences |
| Physics | Other joint types | Unsupported | Raw metadata retained |
| Physics | SoftBody | Unsupported | Complete metadata retained; no backend |

## Render pipelines

| Pipeline | Compile dependency | Validation status |
| --- | --- | --- |
| URP 14.0.12 | None | Import/runtime tests completed on Unity 2022.3.60f1 |
| Built-in Render Pipeline | None | Not validated; material appearance unverified |
| HDRP | None | Not validated; material appearance unverified |

The absence of a render-pipeline package dependency is an architectural property. It is
not a claim that the default approximate material looks correct in every pipeline.
