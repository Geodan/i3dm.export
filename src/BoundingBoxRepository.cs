using Npgsql;
using System;
using Wkx;

namespace i3dm.export
{
    public static class BoundingBoxRepository
    {
        public static BoundingBox3D GetBoundingBox3DForTable(NpgsqlConnection conn, string geometry_table, string geometry_column)
        {
            conn.Open();            
            var sql = $"SELECT st_xmin(box), ST_Ymin(box), ST_Zmin(box), ST_Xmax(box), ST_Ymax(box), ST_Zmax(box) FROM (select ST_3DExtent(st_transform({geometry_column}, 3857)) AS box from {geometry_table}) as total";
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
