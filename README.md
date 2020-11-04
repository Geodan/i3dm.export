# i3dm.export

Console tool for exporting Instanced 3D Models (i3dm's) and tileset.json from PostGIS table. The input table contains instance information like location (epsg:4326), scale, rotation and instance attributes. The used 3D model (binary glTF - glb) for visualizing the instances is one of input parameters.

This tool is intended to be used in combination with 3D Tiles support in MapBox GL JS (https://github.com/Geodan/mapbox-3dtiles).

For instanced 3D Model specs see https://github.com/CesiumGS/3d-tiles/tree/master/specification/TileFormats/Instanced3DModel

[![NuGet Status](http://img.shields.io/nuget/v/i3dm.export.svg?style=flat
)](https://www.nuget.org/packages/i3dm.export/)

## Installation


Prerequisite: .NET Core 3.1 SDK is installed https://dotnet.microsoft.com/download/dotnet-core/3.1

```
$ dotnet tool install -g i3dm.export
```

Or update

```
$ dotnet tool update -g i3dm.export
```

## Live demo 3D instanced tiles

30k trees in instanced tiles (1000m by 1000m) with random scales/rotations:

https://bertt.github.io/mapbox_3dtiles_samples/samples/instanced/trees/

## Input database table

Input database table contains following columns: 

. geom - geometry with PointZ (4326) for i3dm positions

. scale - double

. rotation - double with horizontal rotation angle (0 - 360 degrees)

. tags (json)

. model - string with binary glTF model

. (optional) scale_non_uniform (double precision[3])

See [testdata/create_testdata.sql](testdata/create_testdata.sql) for script creating sample table. 

## Parameters

Tool parameters:

```
-c: (required) connection string to PostGIS database

-t: (required) table with instance positions

-g: (optional - Default: 500,0) Geometric errors

-e: (optional - Default: 1000) Extent per tile

-o: (optional - Default: ./tiles) Output directory, will be created if not exists

-r: (optional - Default: false) Use RTC_CENTER for high precision relative positions

-q: (optional - Default: "") Query to add to the where clauses (for example: -q "id<5460019"). Be sure to check indexes when using this option.

--use_external_model: (optional - default false) Use external model instead of embedded model

--use_scale_non_uniform: (optional - default false) Use column scale_non_uniform for scaling instances
```

## Sample running

```
$ i3dm.export -c "Host=localhost;Username=postgres;Password=postgres;Database=test;Port=5432" -t public.trees -m tree.glb
```

## Getting started

For getting started with i3dm.export tool see [getting started](docs/getting_started.md).

## Instance batch info

To add batch info to instanced tiles fill the 'tags' type json column in the input table.

For example:

```
[{"customer" : "John Doe"}, {"id" : 5454577}]
```

In the MapBox GL JS client this attribute information can be displayed when selecting the instance.

Note: 

. all instance records per tile should have the same properties (in the above case 'customer' and 'id'). 
The list of properties is determined from the first instance in the tile;

. only key - value is supported, not more complex structures.

Sample to update the json column in PostgreSQL:

```
psql> update  i3dm.my_table set tags = json_build_array(json_build_object('customer','John Doe'), json_build_object('id',id));
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
ALTER TABLE traffic_signs_instances
ADD COLUMN scale_non_uniform double precision[3]
```

Fill column:

```
update traffic_signs_instances set scale_non_uniform = '{10.0, 20.0, 30.0}'
```

## Developing

Run from source code:

```
$ git clone https://github.com/Geodan/i3dm.export.git
$ cd i3dm.export/src
$ dotnet run -- -c "Host=myserver;Username=postgres;Password=postgres;Database=test;Port=5432" -t public.trees -m tree.glb
```

To develop in Visual Studio Code, open .vscode/launch.json and adjust the 'args' parameter to your environment

```
"args": ["-c" ,"Host=myserver;Username=postgres;Database=test;Port=5432", "-t", "my_table", "-m", "tree.glb"],
```

Press F5 to start debugging.

To visualize in MapBox GL JS, add a reference to MapBox3DTiles.js:

```
<script src="Mapbox3DTiles.js"></script>
```

and load the tileset:

```
 map.on('style.load', function() {
     let tileslayerTree = new Mapbox3DTiles.Mapbox3DTilesLayer( { 
       id: 'tree', 
       url: 'tileset.json'
      } );
      map.addLayer(tileslayerTree, 'waterway-label');
});
```

## Queries

Queries used in this tool:

1] Query bounding box of table

```
SELECT st_xmin(box), ST_Ymin(box), ST_Zmin(box), ST_Xmax(box), ST_Ymax(box), ST_Zmax(box) FROM (select ST_3DExtent(st_transform({geometry_column}, 3857)) AS box from {geometry_table} {q}) as total
````

2] Query instances per tile

```
SELECT ST_ASBinary(ST_Transform(geom, 3857)) as position, scale, rotation, tags FROM {geometry_table} WHERE {q} ST_Intersects(geom, ST_Transform(ST_MakeEnvelope({from.X}, {from.Y}, {to.X}, {to.Y}, 3857), 4326))
```

where:

- {q} option is the optional query parameter.
- {geometry_column} column with geometry (default: geom)
- {geometry_table} input geometry table
- {from.X}, {from.Y}, {to.X}, {to.Y} envelope of a tile

## Roadmap

- support lods;

## History

2020-10-28: release 1.6 add scale_non_uniform support

2020-10-21: release 1.5 add query, use_external_model parameters

2020-10-20: release 1.4 add batch info support

2020-10-20: release 1.3 adding instance rotation + rtc_Center support 

2020-10-19: release 1.2 with instance scale support

2020-10-19: add support for uri references to external glTF's.

2020-10-15: Initial coding




