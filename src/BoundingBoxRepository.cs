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
            var tileBound = $"POLYGON(({from.X} {from.Y}, {to.X} {from.Y}, {to.X} {to.Y}, {from.X} {to.Y}, {from.X} {from.Y}))";
            var sql = $"SELECT ST_ASBinary(ST_Transform(geom, 3857)) as position, scale, rotation FROM {geometry_table} WHERE ST_Intersects(ST_Force2D(geom), ST_Transform(ST_GeomfromText('{tileBound}', 3857), 4326))";
            var res = conn.Query<Instance>(sql).AsList();
            conn.Close();
            return res;
        }

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
    }
}
