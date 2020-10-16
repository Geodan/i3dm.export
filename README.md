# i3dm.export

Console tool for exporting instanced 3D Tiles (i3dm's) and tileset.json from PostGIS table. The input table contains instance information like location (4326), scale, rotation and instance attributes. The used 3D model (glTF - glb) for visualizing the instances is one of input parameters.

This tool is intended to be used in combination with 3D Tiles support in MapBox GL JS (https://github.com/Geodan/mapbox-3dtiles)

## Installation

```
$ dotnet tool install -g i3dm.export
```

Or update

````
$ dotnet tool update -g i3dm.export
```

## Input database table

Input database table contains following columns: 

. geom - geometry with PointZ (4326) for i3dm positions

. scale - double (not supported yet)

. rotation - double with horizontal rotation angle (0 - 360 degrees) (not supported yet)

. properties (json) (not supported yet)

See [testdata/create_testdata.sql](testdata/create_testdata.sql) for script creating sample table. 

## Parameters

Tool parameters:

```
-c: (required) connection string to PostGIS database

-t: (required) table with instance positions

-m: (required) glTF model (glb)

-g: (Default: 500, 50) Geometric errors

-e: (Default: 1000) extent per tile

-o: (Default: ./tiles) Output directory, will be created if not exists

-q: query (not supported yet)
```

## Demo

30000 trees in instanced tiles

https://bertt.github.io/mapbox_3dtiles_samples/samples/instanced/trees/


## Sample running

```
$ i3dm.export -c "Host=localhost;Username=postgres;Password=postgres;Database=test;Port=5432" -t public.trees -m tree.glb
```

## Roadmap

- support instance scale, rotation, properties, scale_non_uniform;

- add support for uri references to external glTF's.






