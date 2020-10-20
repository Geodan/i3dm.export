# i3dm.export

Console tool for exporting instanced 3D Tiles (i3dm's) and tileset.json from PostGIS table. The input table contains instance information like location (4326), scale, rotation and instance attributes. The used 3D model (glTF - glb) for visualizing the instances is one of input parameters.

This tool is intended to be used in combination with 3D Tiles support in MapBox GL JS (https://github.com/Geodan/mapbox-3dtiles)

## Installation

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

. properties (json) (not supported yet)

See [testdata/create_testdata.sql](testdata/create_testdata.sql) for script creating sample table. 

## Parameters

Tool parameters:

```
-c: (required) connection string to PostGIS database

-t: (required) table with instance positions

-m: (required) glTF model (glb). When an uri is given, the i3dm will contain a reference to the model, otherwise an embedded local glb is expected.

-g: (optional - Default: 500,0) Geometric errors

-e: (optional - Default: 1000) extent per tile

-o: (optional - Default: ./tiles) Output directory, will be created if not exists

-r: (optional = Default: false) Use RTC_CENTER for high precision relative positions

-q: query (not supported yet)
```

## Sample running

```
$ i3dm.export -c "Host=localhost;Username=postgres;Password=postgres;Database=test;Port=5432" -t public.trees -m tree.glb
```

## Developing

```
$ git clone https://github.com/Geodan/i3dm.export.git
$ cd i3dm.export/src
$ dotnet run
```

To develop in Visual Studio Code, open .vscode/launch.json and adjust the 'args' parameter to your environment

```
"args": ["-c" ,"Host=myserver;Username=postgres;Database=test;Port=5432", "-t", "my_table", "-m", "tree.glb"],
```

Press F5 to start debugging.

## Roadmap

- support instance properties, scale_non_uniform, lod;


## History

2020-10-20: release 1.3 adding instance rotation + rtc_Center support 

2020-10-19: release 1.2 with instance scale support

2020-10-19: add support for uri references to external glTF's.

2020-10-15: Initial coding




