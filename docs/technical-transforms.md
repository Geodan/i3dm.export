# Technical: Coordinate Systems & Transforms

This document explains how `i3dm.export` computes translation, rotation, and scale for instances, covering coordinate transformations and matrix conventions.

## Coordinate Systems

### ECEF Mode (Default)
Standard mode converts positions to **ECEF (EPSG:4978)** - Earth-Centered, Earth-Fixed coordinates:
- Right-handed, meters
- +X: equator/prime meridian, +Y: 90° east, +Z: north pole
- Derives **ENU** (East/North/Up) tangent basis at each position: `E × N = U`
- Applies yaw/pitch/roll in local frame

### Cartesian Mode (`--keep_projection=true`)
Uses local XYZ coordinates for viewers like Giro3D:
- Right-handed, meters (from source projection)
- +X: East, +Y: North, +Z: Up
- No ECEF/ENU transformations
- Model rotated: 90° around X (Z-up), 180° around Z (orientation)
- **Limitations**: i3dm only, no per-instance rotation, fixed NORMAL_RIGHT/UP

### glTF Y-up Space
Output format uses right-handed: +X right, +Y up, +Z forward

**ECEF → glTF swizzle**: `ToYUp(x, y, z) = (x, z, -y)`

## Rotation Conventions

Angles in **degrees**, **clockwise-positive**:
- **Yaw**: around Up axis (heading)
- **Pitch**: around East axis
- **Roll**: around North/Forward axis

`Rotator.RotateVector` converts clockwise to right-hand-rule using `360 - angle`.

## Matrix Conventions

**glTF**: Column-major, column vectors → `world = M * local`  
**System.Numerics**: Row-vector semantics → `world = local * M`

This repo stores world basis vectors in matrix **rows** for row-vector semantics:
- `(1,0,0) * M = East`, `(0,1,0) * M = Up`, `(0,0,1) * M = Forward`

## Transform Pipeline

**Common workflow** (ECEF mode):
1. Compute ENU basis at ECEF position
2. Apply yaw/pitch/roll (clockwise-positive)
3. Build 3×3 orientation matrix

**Encoding diverges**:
- **GPU mode**: Convert to glTF Y-up → quaternion → `EXT_mesh_gpu_instancing` TRS
- **i3dm mode**: Keep ECEF → extract `NORMAL_RIGHT`/`NORMAL_UP` vectors

## GPU Instancing Mode (`--use_gpu_instancing=true`)

Outputs `.glb` with `EXT_mesh_gpu_instancing`: `worldVertex = NodeWorld * InstanceTRS * vertex`

**Node transforms preserved**: Uses `node.WorldMatrix` from input model (e.g., Blender axis corrections)

**Instance TRS**:
1. **Translation**: ECEF → glTF Y-up via `ToYUp(Point)`
2. **Rotation**: ENU basis + yaw/pitch/roll → swizzle to Y-up → re-orthonormalize → quaternion
3. **Scale**: Uniform or non-uniform (`--use_scale_non_uniform`)

**RTC optimization**: First instance position as tile anchor, improving precision

## i3dm Mode (`--use_gpu_instancing=false`)

Encodes orientation via `NORMAL_RIGHT` (local +X) and `NORMAL_UP` (local +Y)

**ECEF mode**:
- Compute rotated ENU basis → `NORMAL_RIGHT` = East, `NORMAL_UP` = North (in ECEF)
- Legacy `rotation` field supported as yaw (with deprecation warning)

**Cartesian mode**:
- Direct XYZ positions, model rotated 90° (X-axis) + 180° (Z-axis)
- Fixed: `NORMAL_RIGHT` = (1,0,0), `NORMAL_UP` = (0,1,0)
- No per-instance rotation yet

## Common Issues

1. **Handedness/sign confusion**: Clockwise-positive vs right-hand-rule
2. **Row vs column vectors**: Basis in wrong dimension for `Vector3.Transform`
3. **Dropped node transforms**: Missing axis corrections from input model
4. **Numerical drift**: Re-orthonormalization needed before quaternion conversion
5. **Wrong projection mode**: ECEF vs Cartesian mismatch with viewer expectations

## Code References

**ECEF mode**:
- `SpatialConverter.cs`: `EcefToEnu` - ENU basis computation
- `GPUTileHandler.cs`: `ToYUp`, `GetInstanceTransform`, `CollectNodesWithMeshes`
- `EnuCalculator.cs`: Yaw/pitch/roll application

**Cartesian mode**:
- `TileHandler.cs`: `RotateModelForCartesian`, `CalculateArrays`, `GetI3dm`
- `ImplicitTiling.cs`: `CreateTile`
