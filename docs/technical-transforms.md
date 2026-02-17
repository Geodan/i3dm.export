# Technical notes: coordinate systems & transforms

This document explains how `i3dm.export` computes **translation**, **rotation**, and **scale** for instances in both output modes:

- `--use_gpu_instancing=true`: outputs a `.glb` that uses `EXT_mesh_gpu_instancing`.
- `--use_gpu_instancing=false`: outputs `i3dm` (or `cmpt` containing `i3dm`) using `NORMAL_UP` / `NORMAL_RIGHT`.

It also documents the coordinate-system conversions between **ECEF (EPSG:4978)** and **glTF Y-up**, and clarifies the **matrix conventions** used by glTF vs `System.Numerics`.

## 1) Coordinate systems

### 1.1 ECEF (EPSG:4978)
Internally, instance positions are converted to **ECEF** (Earth-Centered, Earth-Fixed) coordinates.

- Right-handed coordinate system.
- Units: meters.
- Axes (typical convention):
  - +X: intersection of equator and prime meridian.
  - +Y: 90° east on equator.
  - +Z: north pole.

### 1.2 Local tangent frame (ENU)
For each instance position we derive a local tangent basis:

- **E**: East (tangent)
- **N**: North (tangent)
- **U**: Up (surface normal / ellipsoid normal)

This ENU basis is right-handed:

```
E × N = U
```

Implementation note:
- `SpatialConverter.EcefToEnu(position)` computes an orthonormal E/N/U basis at that ECEF position.
- `EnuCalculator.GetLocalEnuCesium(position, heading, pitch, roll)` starts from that base frame and applies yaw/pitch/roll.

### 1.3 glTF space (Y-up)
glTF uses a **right-handed** coordinate system with:

- +X right
- +Y up
- +Z forward

The exporter outputs **Y-up** glTF.

### 1.4 ECEF → glTF Y-up swizzle
The exporter maps vectors/points from ECEF to glTF Y-up using the same swizzle in both position and orientation code:

```
ToYUp(x, y, z) = ( x,  z, -y )
```

So:
- ECEF +Z (up-ish) becomes glTF +Y.
- ECEF +Y becomes glTF -Z.

This keeps the resulting glTF basis right-handed.

## 2) Angle conventions (Yaw / Pitch / Roll)

Angles are in **degrees**.

For GPU instancing, the instance record provides:

- **Yaw**: rotation around local **Up** axis (heading)
- **Pitch**: rotation around local **East** axis
- **Roll**: rotation around local **North/Forward** axis

Important: the code uses the same convention as the legacy non-GPU rotation: **clockwise-positive** (as seen from the positive axis direction).

Implementation note:
- `Rotator.RotateVector(...)` converts clockwise-positive degrees into the standard right-hand-rule rotation by using `360 - angle` internally.

## 3) Matrix conventions: glTF vs System.Numerics

### 3.1 glTF convention
- Matrices are stored **column-major** in the file.
- Transforms conceptually use **column vectors**:

```
world = M * local
```

### 3.2 System.Numerics convention
`System.Numerics.Matrix4x4` + `Vector3.Transform(v, M)` uses **row-vector semantics**:

```
world = local * M
```

This is the single biggest source of confusion when converting between math written for glTF/Cesium (column vectors) and code using `System.Numerics`.

### 3.3 How this repo constructs rotation matrices
When we build a rotation matrix from a basis, we intentionally store the **world basis vectors in the matrix rows** so that:

- local X maps to East
- local Y maps to Up
- local Z maps to Forward

With row-vector semantics that means:

```
(1,0,0) * M = East
(0,1,0) * M = Up
(0,0,1) * M = Forward
```

This is why `GPUTileHandler.GetTransformationMatrix(...)` writes basis vectors into the **rows**.

## 4) Export mode: `--use_gpu_instancing=true` (EXT_mesh_gpu_instancing)

### 4.1 What glTF applies at runtime
In glTF, each mesh node has a node transform (TRS or matrix). With `EXT_mesh_gpu_instancing`, each instance adds its own TRS.

Conceptually (glTF / column vector notation):

```
worldVertex = NodeWorld * InstanceTRS * vertex
```

(Where `NodeWorld` includes the full scene graph above the mesh node.)

### 4.2 Preserving node transforms from the input model
Many models (including Blender exports) include **axis-correction** or other transforms in the scene graph.
If we drop them, some nodes will be misplaced or rotated.

This exporter preserves per-node transforms by collecting all nodes with meshes and using their `node.WorldMatrix`.

Code path:
- `GPUTileHandler.CollectNodesWithMeshes(...)`
- Each mesh node contributes:
  - the mesh geometry
  - the node’s `WorldMatrix`

### 4.3 Instance TRS calculation
For each instance we compute:

1) **Position** (ECEF) → **glTF Y-up** using `ToYUp(Point)`.

2) **Orientation**:
   - Compute ENU basis at the ECEF position.
   - Apply yaw/pitch/roll in ENU.
   - Swizzle each basis vector to glTF Y-up: `ToYUp(Vector3)`.
   - Re-orthonormalize to reduce numerical drift.
   - Build a rotation matrix whose rows are `{East, Up, Forward}`.
   - Convert to quaternion with `Quaternion.CreateFromRotationMatrix`.

3) **Scale**:
   - Uniform: `Scale`
   - Non-uniform: `ScaleNonUniform[3]` when `--use_scale_non_uniform=true`.

### 4.4 Combining node and instance transforms
Each output mesh node uses:

- `nodeTransform = node.WorldMatrix` (from the input model)
- `instanceTransform = TRS` computed above

And we create a combined transform:

- `combined = nodeTransform * instanceTransform`

(Exact multiplication order is handled by `AffineTransform.Multiply(...)` in SharpGLTF; the intended effect is “apply node’s authored transform, then apply instance TRS”.)

### 4.5 RTC (relative-to-center) translation
To keep numbers small, we use the first instance position as a per-tile translation anchor.

- We subtract this anchor from each instance translation.
- At the end, the anchor is applied back to nodes.

This improves numerical precision in clients.

## 5) Export mode: `--use_gpu_instancing=false` (i3dm)

In i3dm, per-instance orientation is encoded via two vectors:

- `NORMAL_RIGHT` (local +X)
- `NORMAL_UP` (local +Y)

The client derives the final orientation from these vectors.

Current behavior:
- The non-GPU path currently uses the legacy single **horizontal rotation** value (`rotation`) as a heading angle.
- It derives `NORMAL_RIGHT` and `NORMAL_UP` from the ENU basis.

(If/when full yaw/pitch/roll is enabled for i3dm, the same ENU + swizzle principles apply; only the storage format differs.)

## 6) Common pitfalls / why models can look “tilted”

1) **Mixing handedness or sign conventions**
   - Clockwise-positive vs right-hand-rule is easy to flip.

2) **Row-vectors vs column-vectors**
   - A basis written into columns will be wrong when used with `Vector3.Transform(v, M)`.

3) **Dropping input node transforms**
   - Some models rely on an axis-correction node; if ignored, the model will be sideways or parts won’t move.

4) **Non-orthonormal basis drift**
   - Small floating point errors can accumulate; we re-orthonormalize the basis before creating quaternions.

## 7) Code pointers

- ENU basis: `src\Cesium\SpatialConverter.cs` (`EcefToEnu`)
- Y-up swizzle: `src\GPUTileHandler.cs` (`ToYUp`)
- Instance TRS (GPU): `src\GPUTileHandler.cs` (`GetInstanceTransform`)
- Node transform preservation: `src\GPUTileHandler.cs` (`CollectNodesWithMeshes`)
- Yaw/pitch/roll application: `src\EnuCalculator.cs`
