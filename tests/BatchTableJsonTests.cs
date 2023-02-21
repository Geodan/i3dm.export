using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System.Collections.Generic;

namespace i3dm.export.tests
{
    public class BatchTableJsonTests
    {
        [Test]
        public void FirstBatchTableJsonHandling()
        {
            var tags = new List<JArray>();
            tags.Add(JArray.Parse("[{'customer':'John Doe'},{'id': 33}]"));
            tags.Add(JArray.Parse("[{'customer':'SuperYo'},{'id': 34}]"));

            var properties = TinyJson.GetProperties(tags[0]);
            Assert.IsTrue(properties[0] == "customer");
            Assert.IsTrue(properties[1] == "id");

            var json = TinyJson.ToJson(tags, properties);
            var dynamicObject = JsonConvert.DeserializeObject<dynamic>(json)!;
            Assert.IsNotNull(dynamicObject);
            Assert.IsTrue(dynamicObject["customer"][0]=="John Doe");
        }

        [Test]
        public void AttributesWithQuotesHandling()
        {
            var tags = new List<JArray>();
            tags.Add(JArray.Parse("[{'name':\"Parroti'a persica Vanessa\"},{'id': 33}]"));
            tags.Add(JArray.Parse("[{'customer':'SuperYo'},{'id': 34}]"));

            var properties = TinyJson.GetProperties(tags[0]);
            Assert.IsTrue(properties[0]== "name");
            Assert.IsTrue(properties[1] == "id");

            var json = TinyJson.ToJson(tags, properties);
            var dynamicObject = JsonConvert.DeserializeObject<dynamic>(json)!;
            Assert.IsNotNull(dynamicObject);
            //Assert.IsTrue(dynamicObject["customer"][0] == "John Doe");
        }

    }

}
