using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace i3dm.export.tests
{

    public class CustomItem
    {
        public string PropertyName { get; set; }
        public List<object> Values { get; set; }

    }

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
        }
    }

}
