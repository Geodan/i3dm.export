# i3dm.export

Console tool for exporting Instanced 3D Models (i3dm's), i3dm composites (cmpt) and tileset.json from PostGIS table. 

The input table contains instance information like location (epsg:4326), binary glTF model (glb), scale, rotation and instance attributes. 

The 3D tiles created by this tool are tested in Cesium JS.


MapBox GL JS (using https://github.com/Geodan/mapbox-3dtiles) is NOT supported at the moment.

Sample of trees in Cesium viewer using instanced 3D Tiles in Lyon:

![image](https://github.com/Geodan/i3dm.export/assets/538812/f67c6126-64be-42a9-9ee5-8f64c452c4aa)


For instanced 3D Model (i3dm) specs see https://github.com/CesiumGS/3d-tiles/tree/master/specification/TileFormats/Instanced3DModel

For composite tile (cmpt) specs see https://github.com/CesiumGS/3d-tiles/blob/master/specification/TileFormats/Composite/README.md

[![NuGet Status](http://img.shields.io/nuget/v/i3dm.export.svg?style=flat
)](https://www.nuget.org/packages/i3dm.export/)

## Installation


Prerequisite: .NET 6.0 SDK is installed https://dotnet.microsoft.com/download/dotnet/6.0

```
$ dotnet tool install -g i3dm.export
```

Or update

```
$ dotnet tool update -g i3dm.export
```

## Live demo 3D instanced tiles

Trees sample with implicit tiling:

https://geodan.github.io/i3dm.export/samples/traffic_lights/cesium/

## Input database table

Input database table contains following columns: 

. geom - geometry with Point or PointZ (epsg:4326) for i3dm positions;

. scale - double with instance scale (all directions);

. rotation - double with horizontal rotation angle (0 - 360 degrees);

. tags - json with instance attribute information;

. model - byte[] or string column with glb or name of binary glTF model per instance. Should be valid path on tool runtime or valid uri on display in client; 

. (optional) scale_non_uniform - double precision[3] - for scale per instance in 3 directions.

See [testdata/create_testdata.sql](testdata/create_testdata.sql) for script creating sample table. 

## Parameters

Tool parameters:

```
-c: (required) connection string to PostGIS database

-t: (required) table with instance positions

-g: (optional - Default: 5000) Geometric error

-o: (optional - Default: ./tiles) Output directory, will be created if not exists

-q: (optional - Default: "") Query to add to the where clauses (for example: -q "id<5460019"). Be sure to check indexes when using this option.

-f, --format: (optional - default Cesium) Output format mapbox/cesium

--use_external_model: (optional - default false) Use external model instead of embedded model

--use_scale_non_uniform: (optional - default false) Use column scale_non_uniform for scaling instances

--max_features_per_tile (optional - default 1000). Maximum number of features/instances of tile

--geometrycolumn: (optional - default: geom) Geometry column name

--use_gpu_instancing (optional - default false) Use GPU instancing (only for Cesium)

--boundingvolume_heights (option - default: 0,10) - Tile boundingVolume heights (min, max) in meters. The heights will be added to the z_min and z_max values of the input geometries.
```
# Docker

See https://hub.docker.com/r/geodan/i3dm.export

## Sample running

```
$ i3dm.export -c "Host=localhost;Username=postgres;Password=postgres;Database=test;Port=5432" -t public.trees
```

## Getting started

For getting started with i3dm.export tool see [getting started](docs/getting_started.md).

## Model

By default, the instance model will be stored in the i3dm payload. In the i3dm header the value 'gltfFormat' is set to 1. 
In this case, the model should be a valid file path to the binary glTF. 
Only the i3m files should be copied to a production server.

When parameter 'use_external_model' is set to true, only the model name will be stored in the i3dm payload. 
In the i3dm header the value 'gltfFormat' is set to 0. In this case, the model should be a valid absolute or relative url to 
the binary glTF. The client is responsible for retrieving the binary glTF's. Both the i3dm's and binary glTF's should be copied to a production server.

## Composites

Starting release 2.0, for every tile there will be a composiste tile (cmpt) - even if there is only 1 model available in the tile.  
Specs see https://docs.opengeospatial.org/cs/18-053r2/18-053r2.html#249 . The composite tile contains a collection of instanced 3d tiles (i3dm), for each model there is 1 i3dm.

## Implicit tiling

Starting release 2.0, tiles are generated according to the 3D Tiles 1.1 Implicit Tiling technique. Tiles are generated in a quadtree, maximum number of features/instances is defined by parameter 'implicit_tiling_max_features'. 

Content tiles will be generated in output folder 'content', subtree files will be created in folder 'subtrees'. In the root output folder file 'tileset.json' will be created.

## Instance batch info

To add batch info to instanced tiles fill the 'tags' type json column in the input table.

For example:

```
[{"customer" : "John Doe"}, {"id" : 5454577}]
```

Note: 

. all instance records per tile should have the same properties (in the above case 'customer' and 'id'). 
The list of properties is determined from the first instance in the tile;

. only key - value is supported, not more complex structures.

Sample to update the json column in PostgreSQL:

```
postgres=# update  i3dm.my_table set tags = json_build_array(json_build_object('customer','John Doe'), json_build_object('id',id));
```

The batch table information in the i3dm tile is stored as follows (for 2 instances):

```
{"customer":["John Doe","John Doe2"], "id": [5454577, 5454579]}
```

## Bounding volume

The root bounding volume in Tileset.json is calculated from the input table in 3 dimensions (xmin, ymin, xmax, ymax, zmin, zmax). 

- The values for the boundingbox (xmin, ymin, xmax, ymax) are increased by 10%;
    
- The height values (zmin, zmax) are increased by the value of setting 'boundingvolume_heights' (default 0,10).  

## Scale non uniform

When the instance model should be scaled in three directions (x, y, z) use the --use_scale_non_uniform option (default false)

When using this option, the column 'scale_non_uniform' will be used for scaling instances.

Column 'scale_non_uniform' must be of type 'double precision[3]'.

Sample queries to create/fill this column:

Create column:

```
postgres=# ALTER TABLE traffic_signs_instances
postgres=# ADD COLUMN scale_non_uniform double precision[3]
```

Fill column:

```
postgres=# update traffic_signs_instances set scale_non_uniform = '{10.0, 20.0, 30.0}'
```

## Spatial index

When using large tables create a spatial index on the geometry column:

```
psql> CREATE INDEX ON m_table USING gist(geom)
```

## GPU Instancing

In 3D Tiles 1.1, GPU instancing is supported. This means that the same model can be used for multiple instances within a glTF (using EXT_mesh_gpu_instancing -
https://github.com/KhronosGroup/glTF/blob/main/extensions/2.0/Vendor/EXT_mesh_gpu_instancing/README.md). 
Files like I3dm/cmpt are no longer created.

There is an experimental option to create 3D Tiles 1.1 using GPU instancing: --use_gpu_instancing (default false).

This option is currently in development. 

The following features should work: Positioning, Rotation (roll, pitch, yaw) and Scaling of instances.

To use this option, the input table should contain columns 'roll', 'pitch' and 'yaw' (column 'rotation' is not used).

Sql script to create the columns:

```
alter table instances add yaw decimal default 0
alter table instances add pitch decimal default 0
alter table instances add roll decimal default 0
alter table instances drop column rotation
```

The columns should be filled with radian angles (0 - 2PI).

The following features are not yet supported when using use_gpu_instancing: 

- batch information (EXT_Mesh_Features/EXT_Structural_Metadata)

- composite tiles (formerly known as cmpt). When there are multiple models in the input table only the first one is used.

Warning: When the input glTF model has transformations, the model will be transformed twice: once in the glTF and once for the instance translations. In some 
cases it's better to remove the transformations from the input model. For example tool 'gltf-tansform' - function clearNodeTransform (https://gltf-transform.dev/modules/functions/functions/clearNodeTransform) can be 
used to clear local transformations.

## Developing

Run from source code:

```
$ git clone https://github.com/Geodan/i3dm.export.git
$ cd i3dm.export/src
$ dotnet run -- -c "Host=myserver;Username=postgres;Password=postgres;Database=test;Port=5432" -t public.trees
```

To develop in Visual Studio Code, open .vscode/launch.json and adjust the 'args' parameter to your environment

```
"args": ["-c" ,"Host=myserver;Username=postgres;Database=test;Port=5432", "-t", "my_table"],
```

Press F5 to start debugging.

To Visualize in CesiumJS, add references to:

- https://cdnjs.cloudflare.com/ajax/libs/cesium/1.96.0/Cesium.js"

- https://cdnjs.cloudflare.com/ajax/libs/cesium/1.96.0/Widgets/widgets.min.css

## History

2023-11-08: release 2.6.0: Add support for GPU instancing (experimental), removed option -r RTC_CENTER 

2023-10-18: release 2.5.0: Improved root bounding volume calculation, improved batch table handling

2023-09-27: release 2.4.5: Get boundingvolume z from input table + option boundingvolume_heights removed

2023-09-27: release 2.4.4 change default -r (relative positions) from false to true

2023-06-20: release 2.4.3 fix for generating 1 tile

2023-02-21: release 2.4.1 fix version number

2023-02-21: release 2.4 split root subtree file in multiple subtree files

2023-01-30: release 2.3.2 improve spatial index performance 

2022-10-05: release 2.3.1 bug fix for showing all instances per tile

2022-10-05: release 2.3 add support for byte[] type column (in addition to string) for glb's from database

2022-08-31: release 2.2 renamed parameter 'implicit_tiling_max_features' to 'max_features_per_tile', 
fix skewed bounding volumes

2022-08-09: release 2.1 use 1 geometric error as input parameter

2022-08-08: release 2.0 adding 3D Tiles 1.1 Implicit Tiling. 

Breaking change: 

Parameters removed: -e and -s (extent tile and super extent tile)

Parameters added: implicit_tiling_max_features (default 1000)

2021-10-04: release 1.9 adding Cesium support

2020-11-12: release 1.8 add external tileset support

2020-11-12: release 1.72. to .NET 5.0

2020-11-05: release 1.7.1 with bug fix wrong instances per i3dm when multiple models used. 

2020-11-05: release 1.7 add support for composite tiles (cmpt). Breaking change: parameter -m --model is removed. 
Model can now be defined per instance in the input table.

2020-10-28: release 1.6 add scale_non_uniform support

2020-10-21: release 1.5 add query, use_external_model parameters

2020-10-20: release 1.4 add batch info support

2020-10-20: release 1.3 adding instance rotation + rtc_Center support 

2020-10-19: release 1.2 with instance scale support

2020-10-19: add support for uri references to external glTF's.

2020-10-15: Initial coding
