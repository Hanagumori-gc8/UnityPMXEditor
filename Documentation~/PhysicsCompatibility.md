# Experimental Physics Compatibility

UnityPMXEditor physics import is optional and defaults to `Physics=None`. In that mode,
rigid bodies, joints, and soft bodies remain available in `PmxModelAsset` metadata, but
the importer creates no `Rigidbody`, `Collider`, `ConfigurableJoint`, or
`PmxPhysicsController`. The mesh, skeleton, skinning, morph, grant, and IK paths remain
usable without physics.

`Physics=Experimental` is an approximate mapping from PMX's Bullet-oriented data to
Unity PhysX. Bullet and PhysX use different solvers, constraint formulations, contact
generation, friction models, sleeping behavior, and integration details. The mapping is
not frame-by-frame equivalent to MMD and must not be used as evidence of MMD-compatible
physics.

## Mapping

| PMX data | Unity mapping | Status |
| --- | --- | --- |
| Sphere | `SphereCollider` | Approximated |
| Box half extents | `BoxCollider.size` | Approximated |
| Capsule radius/height | Y-axis `CapsuleCollider` | Approximated |
| Rigid body mass/damping | `Rigidbody` mass, drag, angular drag | Approximated |
| Restitution/friction | Per-body `PhysicMaterial` | Approximated |
| Spring 6DOF joint type 0 | `ConfigurableJoint` | Approximated |
| Collision group/mask | Deterministic pairwise `Physics.IgnoreCollision` | Approximated |
| SoftBody | `PmxSoftBodyMetadata` only | Unsupported |
| Other PMX 2.1 joint types | Metadata only | Unsupported |

All PMX position, scale, handedness, Euler-angle, and joint-frame conversion is owned by
the physics coordinate converter in `PmxPhysicsBuilder`. Collider and joint construction
does not perform independent Z flips.

PMX linear limits are per-axis lower and upper bounds. `ConfigurableJoint` exposes one
symmetric `linearLimit`, so the largest absolute bound is used while each axis retains
its Locked/Limited state. Angular X retains low/high limits; Unity Y and Z limits are
symmetric and use the largest absolute bound. PMX per-axis angular Y/Z spring values are
collapsed to Unity's combined `angularYZDrive`. The importer-wide joint damper setting
supplies damping because the PMX joint payload does not provide a directly equivalent
PhysX damper.

## Body Modes

- Mode 0 (`bone-follow`) is kinematic. `PmxPhysicsController.FixedUpdate` moves the body
  from its related bone while preserving the imported body-to-bone offset.
- Mode 1 (`dynamic`) is simulated and does not write its pose back to a bone.
- Mode 2 (`physics+bone`) is simulated, then writes its pose to the related bone in
  `LateUpdate` while preserving the imported offset.
- A `-1` bone index leaves the body unbound. Invalid positive references are rejected by
  Format validation before conversion.

Disabling `PmxPhysicsController` clears velocity, restores the imported rigid-body and
bone baselines, temporarily makes every body kinematic, and removes PMX pair filters.
Re-enabling restores the original kinematic modes and reapplies the same pair filters.

## URPTest Validation Settings

Stage 6 uses the existing project settings without modification:

| Setting | Value |
| --- | --- |
| Unity | 2022.3.60f1 |
| URP | 14.0.12 |
| Fixed timestep | 0.02 seconds |
| Gravity | `(0, -9.81, 0)` |
| Default solver iterations | 6 |
| Default solver velocity iterations | 1 |
| Bounce threshold | 2 |
| Default contact offset | 0.01 |
| Auto simulation | Enabled |
| Auto sync transforms | Disabled |
| Enhanced determinism | Disabled |
| Queries hit triggers | Enabled |
| Queries hit backfaces | Disabled |

The generated Play Mode fixture runs sphere bone-follow, box dynamic, and capsule
physics+bone bodies for 3000 fixed frames (60 simulated seconds at the recorded timestep).
It checks all poses and velocities for NaN/infinity, bounds the simulated positions,
reapplies collision filtering, and verifies disable/restore behavior. Passing this test
only demonstrates bounded behavior for that controlled fixture and those project
settings. It cannot prove behavior for arbitrary models, machines, Unity versions,
timesteps, solver settings, or an MMD/Bullet reference simulation.
