using Dapper;
using Npgsql;
using System.Collections.Generic;
using Wkx;

namespace i3dm.export
{
    public static class BoundingBoxRepository
    {
        public static List<Instance> GetTileInstances(NpgsqlConnection conn, string geometry_table, Point from, Point to)
        {
            conn.Open();

            // todo: add intersects from, to
            var sql = $"SELECT ST_ASBinary(geom) as position, scale, rotation FROM {geometry_table}";
            var cmd = new NpgsqlCommand(sql, conn);
            var res = conn.Query<Instance>(sql).AsList();
            conn.Close();
            return res;
        }


        public static BoundingBox3D GetBoundingBox3DForTable(NpgsqlConnection conn, string geometry_table, string geometry_column)
        {
            conn.Open();
            // todo: add conversion to 3857 in database?
            var sql = $"SELECT st_xmin(geom1), st_ymin(geom1), st_zmin(geom1), st_xmax(geom1), st_ymax(geom1), st_zmax(geom1) FROM (select ST_3DExtent({geometry_column}) as geom1 from {geometry_table}";
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

        public static bool HasFeaturesInBox(NpgsqlConnection conn, string geometry_table, string geometry_column, Point from, Point to, int epsg)
        {
            var sql = $"select exists(select {geometry_column} from {geometry_table} where ST_Intersects(ST_Centroid(ST_Envelope({geometry_column})), ST_MakeEnvelope({from.X}, {from.Y}, {to.X}, {to.Y}, {epsg})))";
            conn.Open();
            var cmd = new NpgsqlCommand(sql, conn);
            var reader = cmd.ExecuteReader();
            reader.Read();
            var exists = reader.GetBoolean(0);
            reader.Close();
            conn.Close();
            return exists;
        }
    }
}
