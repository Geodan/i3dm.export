# i3dm.export

Tool for exporting i3dm's from PostGIS table. This tool is intended to be used in combination with 3D Tiles support in MapBox GL JS (https://github.com/Geodan/mapbox-3dtiles)

## Input database table

Input database table contains following columns: 

. id

. geom - geometry with PointZ (4326) for i3dm positions

. scale - double 

. rotation - double with horizontal rotation angle (0 - 360 degreees)

. properties (json)





