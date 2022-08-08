using Dapper;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Globalization;
using Wkx;

namespace i3dm.export
{
    public static class InstancesRepository
    {
        internal static int CountFeaturesInBox(NpgsqlConnection conn, string geometryTable, string geometryColumn, BoundingBox3D bbox, string where, int epsg)
        {
            var fromX = bbox.XMin.ToString(CultureInfo.InvariantCulture);
            var fromY = bbox.YMin.ToString(CultureInfo.InvariantCulture);
            var toX = bbox.XMax.ToString(CultureInfo.InvariantCulture);
            var toY = bbox.YMax.ToString(CultureInfo.InvariantCulture);

            var sql = $"select count({geometryColumn}) from {geometryTable} where ST_Intersects(st_transform({geometryColumn}, {epsg}), ST_MakeEnvelope({fromX}, {fromY}, {toX}, {toY}, {epsg})) {where}";
            conn.Open();
            var cmd = new NpgsqlCommand(sql, conn);
            var reader = cmd.ExecuteReader();
            reader.Read();
            var count = reader.GetInt32(0);
            conn.Close();
            return count;
        }

        public static List<Instance> GetInstances(NpgsqlConnection conn, string geometryTable, string geometryColumn, BoundingBox3D bbox, int epsg, string where = "", bool useScaleNonUniform = false)
        {
            var fromX = ToInvariantCulture(bbox.XMin);
            var fromY = ToInvariantCulture(bbox.YMin);
            var toX = ToInvariantCulture(bbox.XMax);
            var toY = ToInvariantCulture(bbox.YMax);

            var scaleNonUniform = useScaleNonUniform ? "scale_non_uniform as scalenonuniform, " : string.Empty;
            conn.Open();
            var sql = FormattableString.Invariant($"SELECT ST_ASBinary(ST_Transform(st_force3d({geometryColumn}), {epsg})) as position, scale, {scaleNonUniform} rotation, model, tags FROM {geometryTable} where ST_Intersects(st_transform({geometryColumn}, {epsg}), ST_MakeEnvelope({fromX}, {fromY}, {toX}, {toY}, {epsg})) {where}");
            var res = conn.Query<Instance>(sql).AsList();
            conn.Close();
            return res;
        }

        public static BoundingBox3D GetBoundingBox3DForTable(NpgsqlConnection conn, string geometry_table, string geometry_column, int epsg, string query = "")
        {
            conn.Open();
            var q = string.IsNullOrEmpty(query) ? "" : $"where {query}";
            var sql = $"SELECT st_xmin(box), ST_Ymin(box), ST_Zmin(box), ST_Xmax(box), ST_Ymax(box), ST_Zmax(box) FROM (select ST_3DExtent(st_transform(st_force3d({geometry_column}), {epsg})) AS box from {geometry_table} {q}) as total";
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

        private static string ToInvariantCulture(double value) {
            return value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
