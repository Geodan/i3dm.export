# i3dm.export

Console tool for exporting Instanced 3D Models (i3dm's), i3dm composites (cmpt) and tileset.json from PostGIS table. 

The input table contains instance information like location (epsg:4326), binary glTF model (glb), scale, rotation and instance attributes. 

The 3D tiles created by this tool are tested in:

- Cesium JS

MapBox GL JS (using https://github.com/Geodan/mapbox-3dtiles) is NOT supported at the moment.

Sample of trees in Cesium viewer using instanced (i3dm) and composite (cmpt) 3D Tiles

![Cesium trees](trees.png)

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

30k trees in instanced tiles (1000m by 1000m) with random scales/rotations and batch information:

https://bertt.github.io/mapbox_3dtiles_samples/samples/instanced/


Trees sample with implicit tiling:

Cesium: https://geodan.github.io/i3dm.export/samples/traffic_lights/cesium/

Boxes sample:

MapBox: https://geodan.github.io/i3dm.export/samples/traffic_lights/mapbox/#16.79/52.367926/4.89836/0/45


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

-r: (optional - Default: true) Use RTC_CENTER for high precision relative positions

-q: (optional - Default: "") Query to add to the where clauses (for example: -q "id<5460019"). Be sure to check indexes when using this option.

-f, --format: (optional - default Cesium) Output format mapbox/cesium

--use_external_model: (optional - default false) Use external model instead of embedded model

--use_scale_non_uniform: (optional - default false) Use column scale_non_uniform for scaling instances

--max_features_per_tile (optional - default 1000). Maximum number of features/instances of tile

--geometrycolumn: (optional - default: geom) Geometry column name
```

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

Content tiles will be generated in output folder 'content', subtree file '0_0_0.subtree' will be created in folder 'subtrees'. In the root output folder file 'tileset.json' will be created.

Limitation: 

- 1 subtree file will be created (0_0_0.subtree), there is no support for tree of subtree files (yet);

- there is no support (yet) for Implicit Tiling in the MapBox client, use 
a 1.X version of this tool to workaround this.


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
