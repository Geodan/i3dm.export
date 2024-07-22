# i3dm.export

Console tool for exporting Instanced 3D Models (i3dm's or glb's with EXT_mesh_gpu_instancing), i3dm composites (cmpt), subtree files and tileset.json from PostGIS table. 

The input table contains instance information like location (epsg:4326), binary glTF model (glb), scale, rotation and instance attributes. 

The 3D tiles created by this tool are tested in Cesium JS.

Sample of trees in Cesium viewer using instanced 3D Tiles in Lyon:

![image](https://github.com/Geodan/i3dm.export/assets/538812/f67c6126-64be-42a9-9ee5-8f64c452c4aa)


For instanced 3D Model (i3dm) specs see https://github.com/CesiumGS/3d-tiles/tree/master/specification/TileFormats/Instanced3DModel

For composite tile (cmpt) specs see https://github.com/CesiumGS/3d-tiles/blob/master/specification/TileFormats/Composite/README.md

[![NuGet Status](http://img.shields.io/nuget/v/i3dm.export.svg?style=flat
)](https://www.nuget.org/packages/i3dm.export/)

## Installation


Prerequisite: .NET 8.0 SDK is installed https://dotnet.microsoft.com/download/dotnet/8.0

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

. geom - geometry with Point or PointZ (for example epsg:4326) for i3dm positions;

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

--use_external_model: (optional - default false) Use external model instead of embedded model

--use_scale_non_uniform: (optional - default false) Use column scale_non_uniform for scaling instances

--max_features_per_tile (optional - default 1000). Maximum number of features/instances of tile

--geometrycolumn: (optional - default: geom) Geometry column name

--use_gpu_instancing (optional - default false) Use GPU instancing (only for Cesium)

--boundingvolume_heights (option - default: 0,10) - Tile boundingVolume heights (min, max) in meters. The heights will be added to the z_min and z_max values of the input geometries.
```
# Docker

See https://hub.docker.com/r/geodan/i3dm.export

To run in Docker mount drives:

- /app/output: mount for the result tileset.json, subtree files and content files;

- /glb: mount when using file based input glb's - remember to add the /glb path to the model in the database. For example  '/glb/my_model.glb'.

 Example:

```
$ docker run -it -v $(pwd)/output:/app/output -v $(pwd)/glb:/glb geodan/i3dm.export -c "database_connection_string" -t table 
```

## Sample running

```
$ i3dm.export -c "Host=localhost;Username=postgres;Password=postgres;Database=test;Port=5432" -t public.trees
```

## Getting started

For getting started with i3dm.export tool see [getting started](docs/getting_started.md).

## Benchmarking

1] Benchmark on Daily Digital Obstacles files

Source: https://www.faa.gov/air_traffic/flight_info/aeronav/digital_products/dailydof/

588094 instances worldwide, Content tiles generated: 2345, Subtree files generated: 372

Time generating I3dm --use_gpu_instancing false: 0h 0m 43s 799ms

Time generating GLB --use_gpu_instancing true: 0h 0m 47s 879ms

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

Note: Positions outside xmin, ymin, xmax, ymax -180, -90, 180, 90 in wgs84 are not supported.  

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

Attribute information can be added to the instances using glTF 2.0 extensions 

- EXT_instance_features - https://github.com/CesiumGS/glTF/tree/3d-tiles-next/extensions/2.0/Vendor/EXT_instance_features

- EXT_structural_metadata - https://github.com/CesiumGS/glTF/tree/3d-tiles-next/extensions/2.0/Vendor/EXT_structural_metadata

There is an experimental option to create 3D Tiles 1.1 using GPU instancing: --use_gpu_instancing (default false).

This option is currently in development. 

Live demo trees in Grenoble with GPU instancing and metadata:

https://bertt.github.io/cesium_3dtiles_samples/samples/1.1/grenoble_trees/

![image](https://github.com/Geodan/i3dm.export/assets/538812/038ee19d-7f52-4102-a60c-4ece0672a6a4)

The following features should work: 

- Attribute information from tags; 

- Positioning, Rotation (roll, pitch, yaw) and Scaling of instances.

To use this option, the input table should contain columns 'roll', 'pitch' and 'yaw' (column 'rotation' is not used).

Sql script to create the columns:

```
alter table instances add yaw decimal default 0
alter table instances add pitch decimal default 0
alter table instances add roll decimal default 0
alter table instances drop column rotation
```

The columns should be filled with radian angles (0 - 2PI).

Known limits:

- When using GPU instancing, for the attributes the 'string' type is used (so no support for other types yet);

- No support for multiple meshes/nodes in the input model;

- composite tiles (formerly known as cmpt). Support is added for multiple models in a tile (like multiple models in a composite cmpt file). There is a known issue with showing the attribute information in Cesium. See https://github.com/CesiumGS/cesium/issues/11683

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

2024-06-20: release 2.7.4: fix for composite tiles when using GPU instancing

2024-06-13: release 2.7.3: add support for other input source EPSG codes than 4326

2024-06-10: release 2.7.2: fix gpu instancing yaw, pitch, roll, remove mapbox code (parameter -f --format)

2024-06-08: release 2.7.1: fix for disappearing instances 

2023-03-28: release 2.7.0: add support for EXT_structural_metadata

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
