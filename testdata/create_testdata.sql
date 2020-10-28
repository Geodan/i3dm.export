CREATE SCHEMA i3dm;
SET search_path TO i3dm, public;
CREATE EXTENSION postgis;

CREATE TABLE mydata(
  id serial PRIMARY KEY, 
  geom geometry(POINTZ, 4326),
  scale double precision,
  scale_non_uniform double precision[3],
  rotation double precision,
  tags json
);

CREATE INDEX geom_idx
  ON mydata
  USING GIST (geom);

INSERT INTO mydata(geom, scale, rotation)
VALUES (ST_GeomFromText('POINT(4.72196885 52.46340037 0)', 4326), 1, 35),
  (ST_GeomFromText('POINT(4.71922711 52.45856725 0)', 4326), 1, 239),
  (ST_GeomFromText('POINT(4.72148852 52.44559817 0)', 4326), 1, 21),
  (ST_GeomFromText('POINT(4.7211885 52.4665854 0)', 4326), 1, 140),
  (ST_GeomFromText('POINT(4.69812239 52.42269123 0)', 4326), 1, 70),
  (ST_GeomFromText('POINT(4.71430982 52.42855288 0)', 4326), 1, 290),
  (ST_GeomFromText('POINT(4.71544748 52.42913771 0)', 4326), 1, 360),
  (ST_GeomFromText('POINT(4.71571985 52.42923163 0)', 4326), 1, 0),
  (ST_GeomFromText('POINT(4.71335925 52.42806928 0)', 4326), 1, 215),
  (ST_GeomFromText('POINT(4.71544081 52.42920149 0)', 4326), 1, 149),
  (ST_GeomFromText('POINT(4.70944108 52.42653252 0)', 4326), 1, 48),
  (ST_GeomFromText('POINT(4.6995131 52.42377981 0)', 4326), 1, 47),
  (ST_GeomFromText('POINT(4.71621894 52.42954548 0)', 4326), 1, 5),
  (ST_GeomFromText('POINT(4.7163149 52.42959262 0)', 4326), 1, 310),
  (ST_GeomFromText('POINT(4.7074907 52.4823262 0)', 4326), 1, 193),
  (ST_GeomFromText('POINT(4.72062027 52.46891039 0)', 4326), 1, 336),
  (ST_GeomFromText('POINT(4.72063387 52.4688452 0)', 4326), 1, 128),
  (ST_GeomFromText('POINT(4.72064851 52.46878097 0)', 4326), 1, 97),
  (ST_GeomFromText('POINT(4.7208815 52.46783184 0)', 4326), 1, 124),
  (ST_GeomFromText('POINT(4.72089798 52.46776733 0)', 4326), 1, 241),
  (ST_GeomFromText('POINT(4.72086594 52.46789818 0)', 4326), 1, 310),
  (ST_GeomFromText('POINT(4.72084889 52.46796413 0)', 4326), 1, 54);