# i3dm.export getting started

In this document we run i3dm.export on a sample dataset of traffic signs (GeoJSON file). The generated instanced 3D tiles are visualized using a simple glTF 
box in a CesiumJS client.

## Prerequisites

Some open source tooling is used in this tutorial:

- wget https://www.gnu.org/software/wget/

- Docker https://www.docker.com/get-started

- ogr2ogr https://gdal.org/programs/ogr2ogr.html

- psql https://www.postgresql.org/docs/9.2/app-psql.html

## Install i3dm.export

See releases for executables for Windows, Linux and MacOS

## Download data

```
$ wget https://raw.githubusercontent.com/Amsterdam/mlvb/master/output/asset-registration/current_traffic_signs.geojson
```

## Setup PostGIS

Start PostGIS database

```
$ docker run -d -e POSTGRES_PASSWORD=postgres -p 5432:5432 mdillon/postgis
```

## Import traffic signs to PostGIS

```
$ ogr2ogr -f "PostgreSQL" PG:"host=localhost user=postgres password=postgres dbname=postgres" current_traffic_signs.geojson -nlt POINT -nln traffic_signs
```

The data contains some outliers, delete them:

```
postgres=# delete from traffic_signs where st_x(st_transform(wkb_geometry,4326)) < 4.5 or st_x(st_transform(wkb_geometry,4326))>5.0;
```

Do a count on the traffic signs:

```
$ psql -U postgres -h localhost

postgres=# select count(*) from traffic_signs;
 count
-------
 57809
(1 row)
```

## Create instances table

We create a new view, with 

- trees point geometry in Dutch projection (EPSG:28992);

- Box.glb as model;

- randomized scales;

- randomized yaw (pitch/roll = 0);

- for tags use fields 'id' and 'bevestiging' .

Create the view:

```
postgres=# CREATE view traffic_signs_instances as (
	SELECT ogc_fid as id, 
	wkb_geometry as geom,
	1 +  random() as scale,
	random()*360 as yaw,
	0 as pitch,
	0 as roll,
	'Box.glb' as model,
	json_build_array(json_build_object('id',ogc_fid), json_build_object('bevestiging',bevestiging)) as tags
	from traffic_signs
);
```

Download Box.glb from https://raw.githubusercontent.com/Geodan/i3dm.export/main/docs/Box.glb

## Run i3dm.export on instance table

```
$ i3dm.export -c "Host=localhost;Username=postgres;password=postgres;Port=5432" -t  traffic_signs_instances
```
Here we visualize the traffic lights all instances as a simple red box (box.glb), but any glTF model can be used instead.

An 'output' directory will be created with a tiles subdirectory containing tileset.json and i3dm tiles.

## Visualize in Cesium

Put the Cesium client (index.html and from directory samples\traffic_lights\cesium ) and the output folder with tiles on a webserver.

In the Cesium client do not use the terrain because in the input data there is no altitude.

Result should look like:

![screenshot](traffic_cesium.png)

Live demo: https://geodan.github.io/i3dm.export/samples/traffic_lights/cesium




