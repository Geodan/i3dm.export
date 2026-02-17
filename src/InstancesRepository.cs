using Dapper;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Globalization;
using Wkx;

namespace i3dm.export;

public static class InstancesRepository
{
    internal static int CountFeaturesInBox(NpgsqlConnection conn, string geometryTable, string geometryColumn, BoundingBox bbox, string where, int source_epsg, bool keepProjection= false)
    {
        var fromX = bbox.XMin.ToString(CultureInfo.InvariantCulture);
        var fromY = bbox.YMin.ToString(CultureInfo.InvariantCulture);
        var toX = bbox.XMax.ToString(CultureInfo.InvariantCulture);
        var toY = bbox.YMax.ToString(CultureInfo.InvariantCulture);

        string whereStatement = GetWhere(geometryColumn, where, fromX, fromY, toX, toY, source_epsg, keepProjection);

        var sql = $"select count({geometryColumn}) from {geometryTable} where {whereStatement}";
        conn.Open();
        var cmd = new NpgsqlCommand(sql, conn);
        var reader = cmd.ExecuteReader();
        reader.Read();
        var count = reader.GetInt32(0);
        conn.Close();
        return count;
    }

    private static string GetWhere(string geometryColumn, string where, string fromX, string fromY, string toX, string toY, int source_epsg, bool keepProjection = false)
    {
        var result = keepProjection?
            $"ST_Intersects({geometryColumn}, ST_MakeEnvelope({fromX}, {fromY}, {toX}, {toY}, {source_epsg})) {where}":
            $"ST_Intersects({geometryColumn}, ST_Transform(ST_MakeEnvelope({fromX}, {fromY}, {toX}, {toY}, 4326), {source_epsg})) {where}";
        return result;
    }

    public static List<Instance> GetInstances(NpgsqlConnection conn, string geometryTable, string geometryColumn, BoundingBox bbox, int source_epsg, string where = "", bool useScaleNonUniform = false, bool useGpuInstancing = false, bool keepProjection = false)
    {
        var target_epsg = 4978;
        var fromX = ToInvariantCulture(bbox.XMin);
        var fromY = ToInvariantCulture(bbox.YMin);
        var toX = ToInvariantCulture(bbox.XMax);
        var toY = ToInvariantCulture(bbox.YMax);

        var scaleNonUniform = useScaleNonUniform ? "scale_non_uniform as scalenonuniform, " : string.Empty;
        conn.Open();
        var select = keepProjection?
            $"SELECT ST_ASBinary(st_force3d({geometryColumn})) as position, scale, {scaleNonUniform} model, tags":
            $"SELECT ST_ASBinary(ST_Transform(st_force3d({geometryColumn}), {target_epsg})) as position, scale, {scaleNonUniform} model, tags";

        // Breaking change: both GPU and non-GPU paths use yaw/pitch/roll.
        select += ", yaw, pitch, roll";

        var sql = FormattableString.Invariant($"{select} FROM {geometryTable} where {GetWhere(geometryColumn, where, fromX, fromY, toX, toY, source_epsg, keepProjection)}");
        var res = conn.Query<Instance>(sql).AsList();
        conn.Close();
        return res;
    }

    public static (BoundingBox bbox, double zmin, double zmax) GetBoundingBoxForTable(NpgsqlConnection conn, string geometry_table, string geometry_column, double[] heights, bool keepProjection = false, string query = "")
    {
        conn.Open();
        var q = string.IsNullOrEmpty(query) ? "" : $"where {query}";

        var geom = keepProjection ?
            $"(select ST_3DExtent({geometry_column})" :
            $"(select st_transform(ST_3DExtent({geometry_column}), 4979)";

        var sql = $"SELECT st_xmin(box), ST_Ymin(box), ST_Xmax(box), ST_Ymax(box), ST_Zmin(box), ST_Zmax(box) FROM " +
            $"{geom} AS box from {geometry_table} {q}) as total";

        var cmd = new NpgsqlCommand(sql, conn);

        var reader = cmd.ExecuteReader();
        reader.Read();
        var xmin = reader.GetDouble(0);
        var ymin = reader.GetDouble(1);
        var xmax = reader.GetDouble(2);
        var ymax = reader.GetDouble(3);
        var zmin = reader.GetDouble(4) + heights[0];
        var zmax = reader.GetDouble(5) + heights[1];

        reader.Close();
        conn.Close();

        // make 10% larger
        xmin = xmin - (xmax - xmin) * 0.1;
        ymin = ymin - (ymax - ymin) * 0.1;
        xmax = xmax + (xmax - xmin) * 0.1;
        ymax = ymax + (ymax - ymin) * 0.1;

        if(!keepProjection)
        {
            xmin = xmin < -180 ? -180 : xmin;
            xmax = xmax > 180 ? 180 : xmax;
            ymin = ymin < -90 ? -90 : ymin;
            ymax = ymax > 90 ? 90 : ymax;
        }

        var bbox = new BoundingBox(xmin, ymin, xmax, ymax);
        return (bbox, zmin, zmax);
    }

    private static string ToInvariantCulture(double value) {
        return value.ToString(CultureInfo.InvariantCulture);
    }
}
