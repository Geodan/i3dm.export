using Dapper;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Globalization;
using Wkx;

namespace i3dm.export;

public static class InstancesRepository
{
    internal static int CountFeaturesInBox(NpgsqlConnection conn, string geometryTable, string geometryColumn, BoundingBox bbox, string where)
    {
        var fromX = bbox.XMin.ToString(CultureInfo.InvariantCulture);
        var fromY = bbox.YMin.ToString(CultureInfo.InvariantCulture);
        var toX = bbox.XMax.ToString(CultureInfo.InvariantCulture);
        var toY = bbox.YMax.ToString(CultureInfo.InvariantCulture);

        string whereStatement = GetWhere(geometryColumn, where, fromX, fromY, toX, toY);

        var sql = $"select count({geometryColumn}) from {geometryTable} where {whereStatement}";
        conn.Open();
        var cmd = new NpgsqlCommand(sql, conn);
        var reader = cmd.ExecuteReader();
        reader.Read();
        var count = reader.GetInt32(0);
        conn.Close();
        return count;
    }

    private static string GetWhere(string geometryColumn, string where, string fromX, string fromY, string toX, string toY)
    {
        return $"ST_Intersects({geometryColumn}, ST_MakeEnvelope({fromX}, {fromY}, {toX}, {toY}, 4326)) {where}";
    }

    public static List<Instance> GetInstances(NpgsqlConnection conn, string geometryTable, string geometryColumn, BoundingBox bbox, int epsg, string where = "", bool useScaleNonUniform = false)
    {
        var fromX = ToInvariantCulture(bbox.XMin);
        var fromY = ToInvariantCulture(bbox.YMin);
        var toX = ToInvariantCulture(bbox.XMax);
        var toY = ToInvariantCulture(bbox.YMax);

        var scaleNonUniform = useScaleNonUniform ? "scale_non_uniform as scalenonuniform, " : string.Empty;
        conn.Open();
        var sql = FormattableString.Invariant($"SELECT ST_ASBinary(ST_Transform(st_force3d({geometryColumn}), {epsg})) as position, scale, {scaleNonUniform} rotation, model, tags FROM {geometryTable} where {GetWhere(geometryColumn, where, fromX, fromY, toX, toY)}");
        var res = conn.Query<Instance>(sql).AsList();
        conn.Close();
        return res;
    }

    public static BoundingBox GetBoundingBoxForTable(NpgsqlConnection conn, string geometry_table, string geometry_column, string query = "")
    {
        conn.Open();
        var q = string.IsNullOrEmpty(query) ? "" : $"where {query}";
        var sql = $"SELECT st_xmin(box), ST_Ymin(box), ST_Xmax(box), ST_Ymax(box) FROM (select st_extent({geometry_column}) AS box from {geometry_table} {q}) as total";
        
        var cmd = new NpgsqlCommand(sql, conn);
        var reader = cmd.ExecuteReader();
        reader.Read();
        var xmin = reader.GetDouble(0);
        var ymin = reader.GetDouble(1);
        var xmax = reader.GetDouble(2);
        var ymax = reader.GetDouble(3);
        reader.Close();
        conn.Close();
        return new BoundingBox(xmin, ymin, xmax, ymax);
    }

    private static string ToInvariantCulture(double value) {
        return value.ToString(CultureInfo.InvariantCulture);
    }
}
