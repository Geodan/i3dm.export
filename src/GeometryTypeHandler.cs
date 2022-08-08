using Dapper;
using Newtonsoft.Json.Linq;
using System.Data;
using Wkx;

namespace i3dm.export
{
    public class GeometryTypeHandler : SqlMapper.TypeHandler<Geometry>
    {
        public override Geometry Parse(object value)
        {
            if (value == null)
                return null;
            var stream = (byte[])value;
            var g = Geometry.Deserialize<WkbSerializer>(stream);
            return g;
        }
        public override void SetValue(IDbDataParameter parameter, Geometry value)
        {
            parameter.Value = value;
        }
    }


    public class JArrayTypeHandler : SqlMapper.TypeHandler<JArray>
    {
        public override JArray Parse(object value)
        {
            return JArray.Parse(value.ToString());
        }
        public override void SetValue(IDbDataParameter parameter, JArray value)
        {
            parameter.Value = value.ToString();
        }
    }

}
