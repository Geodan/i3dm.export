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

Options:

1] Use pre-built binaries from release

2] Use .NET Tool

Prerequisite: .NET 8.0 SDK is installed https://dotnet.microsoft.com/download/dotnet/8.0

```
$ dotnet tool install -g i3dm.export
```

Or update

```
$ dotnet tool update -g i3dm.export
```

3] Use Docker

```
$  docker run geodan/i3dm.export
```

## Live demo 3D instanced tiles

Trees sample with implicit tiling:

https://geodan.github.io/i3dm.export/samples/traffic_lights/cesium/

## Input database table

Input database table contains following columns: 

. geom - geometry with Point or PointZ for instance positions;

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

-g: (optional - Default: 1000) Geometric error

-o: (optional - Default: ./tiles) Output directory, will be created if not exists

-q: (optional - Default: "") Query to add to the where clauses (for example: -q "id<5460019"). Be sure to check indexes when using this option.

--use_scale_non_uniform: (optional - default false) Use column scale_non_uniform for scaling instances

--max_features_per_tile (optional - default 1000). Maximum number of features/instances of tile

--geometrycolumn: (optional - default: geom) Geometry column name

--use_gpu_instancing (optional - default false) Use GPU instancing (only for Cesium)

--boundingvolume_heights (option - default: 0,10) - Tile boundingVolume heights (min, max) in meters. The heights will be added to the z_min and z_max values of the input geometries.

--tileset_version (optional - default "") - Tileset version

--use_i3dm (optional - default false) Use I3dm format  - only first model will be processed (false creates Cmpt - Only when creating I3dm's)

--use_external_model: (optional - default false) Use external model instead of embedded model (Only when creating I3dm's)

--use_clustering: (optional - default false) If tile contains more than max_features_per_tile instances, its number of instances will be reduced to max_features_per_tile by clustering

--keep_projection: (optional - default false) Keep the original projection of the input table. 
```

## Projection support

The input table can defined in

1] Global coordinates 

WGS84 longitude, latitude in degrees + height in meters with ellipsoid as reference (EPSG:4979)

2] Local coordinates (for release 2.12.0 and higher)

Any projected coordinate system height in meters with geoid as reference

For example in the Netherlands use composite EPSG:7415 (Amersfoort / RD New (EPSG:28992) + NAP height in meters (EPSG:5709)).

On runtime the tool will reproject the instance positions to EPSG:4978 (ECEF coordinates) for creating the 3D tiles.

When option keep_projection is set to true, the original projection of the input table is preserved and used for creating the 3D tiles. Note that when using 
this option, the client application must support the original projection of the input table for correctly 
displaying the Instanced 3D Tiles, like 3DTilesRenderer/Giro3D/ITowns/QGIS Web Client (QWC). 

## Docker

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

By default, the instance model is embedded directly in the i3dm payload. In the i3dm header, the value gltfFormat is set to 1. In this case, the model must be provided as a valid file path to a binary glTF (.glb) file. Only the i3dm files need to be copied to the production server.

When the parameter use_external_model is set to true, the i3dm payload contains only the model name instead of the embedded binary glTF. In the i3dm header, the value gltfFormat is set to 0. In this case, the model must be specified as a valid absolute or relative URL pointing to a binary glTF (.glb) file. The client application is responsible for downloading the binary glTF files. Both the i3dm files and the referenced binary glTF files must be copied to the production server.

The use_external_model option is only available when --use_gpu_instancing is set to false.

## Composites

Starting with release 2.0, every tile is generated as a composite tile (.cmpt), even if the tile contains only one model.

According to the OGC 3D Tiles specification (https://docs.opengeospatial.org/cs/18-053r2/18-053r2.html#249
), a composite tile (cmpt) can contain multiple tile formats. In this implementation, each composite tile contains a collection of instanced 3D model tiles (i3dm).

For each unique model in a tile, one i3dm file is created. The resulting .cmpt file bundles all i3dm tiles for that tile. Even if there is only a single model, it is still wrapped inside a .cmpt file.

Multiple models are supported. You can specify different models in the model column (for example, a deciduous tree model and a conifer tree model). For each unique model, a separate i3dm tile is generated. If multiple models are present in the same tile, the composite tile (.cmpt) will contain multiple i3dm files, one for each unique model.

If the option --use_i3dm=true is set, only i3dm tiles are created and no composite (.cmpt) tile is generated. When multiple models are present in a tile and --use_i3dm=true is used, only the first model is processed.

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

### External textures
When the input model (GLB/GLTF) references external image textures (for example `*.png`), GPU instancing tiles are written as GLB files that reference those textures externally instead of embedding them in every tile.

On export, textures are written once to:

- `output\\content\\textures\\<modelName>\\<textureFile>`

and each generated tile GLB references them via a relative URI:

- `textures/<modelName>/<textureFile>`

This significantly reduces dataset size when many tiles share the same model + textures.

If the input model has embedded images, they remain embedded in each exported tile GLB.

### Known limits

- When using GPU instancing, for the attributes the 'string' type is used (so no support for other types yet);

- No support for multiple meshes/nodes in the input model;

- composite tiles (formerly known as cmpt). Support is added for multiple models in a tile (like multiple models in a composite cmpt file). There is a known issue with showing the attribute information in Cesium. See https://github.com/CesiumGS/cesium/issues/11683

Warning: When the input glTF model has transformations, the model will be transformed twice: once in the glTF and once for the instance translations. In some 
cases it's better to remove the transformations from the input model. For example tool 'gltf-tansform' - function clearNodeTransform (https://gltf-transform.dev/modules/functions/functions/clearNodeTransform) can be 
used to clear local transformations.

### Known issues GPU Instancing

- https://github.com/Geodan/i3dm.export/issues/81: Trees rotation/ z placement wrong 

- Getting attributes in Cesium does not work when there are multiple input models
https://community.cesium.com/t/upgrade-3d-tileset-with-composite-cmpt-tile-to-1-1-attribute-data-missing/33177/2

## Clustering

There is an experimental option to create 3D Tiles using clustering: --use_clustering (default false).

When this option is off, dense tiles with number of instances exceeding `max_features_per_tile` aren't rendered. With this option such tiles are rendered with number of instances that is exactly equal to `max_features_per_tile`. Number of instances is reduced in the following way:

- tile instances are clustered with MiniBatchKMeans algorithm with number of clusters equal to `max_features_per_tile`;
- from each cluster single instance is picked randomly.

### Performance benchmark
number of instances: 2500<br>
max_features_per_tile: 100<br>

tileset generation time:
- without clustering : 0h 0m 0s 539ms
- with clustering: 0h 0m 1s 238ms
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

## History

2026-02-13: release 2.14.0: add support for keep_projection and add support for external textures when using GPU instancing

2026-02-05: release 2.13.0: fix clustering status messages

2024-10-30: release 2.12.0: add support for cartesian projected input coordinates 

2024-12-20: release 2.11.0: add clustering + change ellipsoid 

2024-10-31: release 2.10.0: add multiple mesh support in input model + gpu instancing tags null checking + vertical precision

2024-10-15: release 2.9.0: add tileset version option

2024-08-01: release 2.8.3: fix release

2024-08-01: release 2.8.2: fix for I3dm using rtc_center for high precision positions

2024-07-22: release 2.8.1: fix release

2024-07-22: release 2.8.0: to .NET 8.0

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
