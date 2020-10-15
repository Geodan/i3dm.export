# i3dm.export

Tool for exporting i3dm's from PostGIS table. This tool is intended to be used in combination with 3D Tiles support in MapBox GL JS (https://github.com/Geodan/mapbox-3dtiles)

## Input database table

Input database table contains following columns: 

. id

. geom - geometry with PointZ (4326) for i3dm positions

. scale - double 

. rotation - double with horizontal rotation angle (0 - 360 degreees)

. properties (json)

See [testdata/create_testdata.sql] for script creating sample table. 

## Parameters

Tool parameters:

```
-c: (required) connection string to PostGIS database

-t: (required) table with instance positions

-m, --model: (required) glTF model (glb)

-g: (Default: 500, 0) Geometric errors

-e, --extenttile(Default: 1000) extent per tile

-o, --output (Default: ./tiles) Output directory, will be created if not exists
```


## Sample running

```
$ i3dm.export -c "Host=localhost;Username=postgres;Password=postgres;Database=test;Port=5432" -t public.trees -m tree.glb
```








