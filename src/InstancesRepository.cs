using Dapper;
using Npgsql;
using System;
using System.Collections.Generic;
using Wkx;

namespace i3dm.export
{
    public static class InstancesRepository
    {

        public static BoundingBox3D ConvertTileBounds(NpgsqlConnection conn, bool cesium, BoundingBox3D bounds) {
            conn.Open();
            var epsg = cesium ? "4979" : "3857";
            var sql = $"SELECT ST_Xmin(box), ST_Ymin(box), ST_Zmin(box), ST_Xmax(box), ST_Ymax(box), ST_Zmax(box) FROM ST_Transform(ST_3DMakeBox(st_setsrid(ST_MakePoint({bounds.XMin}, {bounds.YMin}, 0), 3857), st_setsrid(ST_MakePoint({bounds.XMax}, {bounds.YMax}, 0), 3857)), {epsg}) AS box";
            var cmd = new NpgsqlCommand(sql, conn);
            var reader = cmd.ExecuteReader();
            reader.Read();
            var xmin = reader.GetDouble(0);
            var ymin = reader.GetDouble(1);
            var zmin = reader.GetDouble(2);
            var xmax = reader.GetDouble(3);
            var ymax = reader.GetDouble(4);
            var zmax = reader.GetDouble(5);
            reader.Close();
            conn.Close();

            return new BoundingBox3D() { XMin = xmin, YMin = ymin, ZMin = zmin, XMax = xmax, YMax = ymax, ZMax = zmax };
        }

        public static List<Instance> GetInstances(NpgsqlConnection conn, string geometry_table, Point from, Point to, bool cesium, string query = "", bool useScaleNonUniform = false)
        {
            var epsg = cesium ? "4978" : "3857";
            var q = string.IsNullOrEmpty(query) ? "" : $"{query} and";
            var scaleNonUniform = useScaleNonUniform ? "scale_non_uniform as scalenonuniform, " : string.Empty;
            conn.Open();
            var sql = FormattableString.Invariant($"SELECT ST_ASBinary(ST_Transform(st_force3d(geom), {epsg})) as position, scale, {scaleNonUniform} rotation, model, tags FROM {geometry_table} WHERE {q} ST_Intersects(geom, ST_Transform(ST_MakeEnvelope({from.X}, {from.Y}, {to.X}, {to.Y}, 3857), 4326))");
            var res = conn.Query<Instance>(sql).AsList();
            conn.Close();
            return res;
        }

        public static BoundingBox3D GetBoundingBox3DForTable(NpgsqlConnection conn, string geometry_table, string geometry_column, string query = "")
        {
            conn.Open();
            var q = string.IsNullOrEmpty(query) ? "" : $"where {query}";
            var sql = $"SELECT st_xmin(box), ST_Ymin(box), ST_Zmin(box), ST_Xmax(box), ST_Ymax(box), ST_Zmax(box) FROM (select ST_3DExtent(st_transform({geometry_column}, 3857)) AS box from {geometry_table} {q}) as total";
            var cmd = new NpgsqlCommand(sql, conn);
            var reader = cmd.ExecuteReader();
            reader.Read();
            var xmin = reader.GetDouble(0);
            var ymin = reader.GetDouble(1);
            var zmin = reader.GetDouble(2);
            var xmax = reader.GetDouble(3);
            var ymax = reader.GetDouble(4);
            var zmax = reader.GetDouble(5);
            reader.Close();
            conn.Close();
            return new BoundingBox3D() { XMin = xmin, YMin = ymin, ZMin = zmin, XMax = xmax, YMax = ymax, ZMax = zmax };
        }
    }
}
