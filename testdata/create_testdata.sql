CREATE SCHEMA i3dm;
SET search_path TO i3dm, public;
CREATE EXTENSION postgis;

CREATE TABLE mydata(
	id serial PRIMARY KEY, 
	geom geometry(POINTZ, 4326),
	scale double precision,
	rotation double precision,
	properties jsonb
);

CREATE INDEX geom_idx
  ON mydata
  USING GIST (geom);