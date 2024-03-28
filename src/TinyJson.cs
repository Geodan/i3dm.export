using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace i3dm.export;

public static class TinyJson
{
    public static string ToJson(List<JArray> tags, List<string> properties)
    {
        var sb = new StringBuilder();
        sb.Append("{");
        foreach (var prop in properties)
        {
            sb.Append($"\"{prop}\":[");
            var values = GetValues(tags, prop);
            // todo: check types, do not always serialize to string

            var values1 = new List<string>();
            foreach(var value in values)
            {
                var val1 = ((JValue)value).Value;
                val1 = HttpUtility.HtmlEncode(val1);
                values1.Add(string.Format("\"{0}\"", val1));
            }

            sb.Append(string.Join(",",values1));

            sb.Append("]");

            if (prop != properties.Last())
            {
                sb.Append(",");
            }
        }

        sb.Append("}");
        var resres = sb.ToString();
        return resres;
    }

    public static List<object> GetValues(List<JArray> tags, string prop)
    {
        var res = new List<Object>();
        foreach (var tag in tags)
        {
            foreach (var parsedObject in tag.Children<JObject>())
            {
                foreach (JProperty parsedProperty in parsedObject.Properties())
                {
                    string propertyName = parsedProperty.Name;

                    if (propertyName == prop)
                    {
                        res.Add(parsedProperty.Value);
                    }
                }
            }
        }
        return res;

    }

    public static List<string> GetProperties(JArray tag)
    {
        var res = new List<string>();
        foreach (var parsedObject in tag.Children<JObject>())
        {
            foreach (JProperty parsedProperty in parsedObject.Properties())
            {
                // do not use the Array type, because it is not supported by the 3d-tiles spec
                if (parsedProperty.Value.Type != JTokenType.Object)
                {
                    string propertyName = parsedProperty.Name;
                    res.Add(propertyName);
                }
            }
        }
        return res;
    }
}
