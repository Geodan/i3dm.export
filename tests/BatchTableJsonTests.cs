using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System.Collections.Generic;

namespace i3dm.export.tests;

public class BatchTableJsonTests
{
    [Test]
    public void FirstBatchTableJsonHandling()
    {
        var tags = new List<JArray>();
        tags.Add(JArray.Parse("[{'customer':'John Doe'},{'id': 33}]"));
        tags.Add(JArray.Parse("[{'customer':'SuperYo'},{'id': 34}]"));

        var properties = TinyJson.GetProperties(tags[0]);
        Assert.That(properties[0] == "customer");
        Assert.That(properties[1] == "id");

        var json = TinyJson.ToJson(tags, properties);
        var dynamicObject = JsonConvert.DeserializeObject<dynamic>(json)!;
        Assert.That(dynamicObject!=null);
        Assert.That(dynamicObject["customer"][0]=="John Doe");
    }

    [Test]
    public void AttributesWithQuotesHandling()
    {
        var tags = new List<JArray>();
        tags.Add(JArray.Parse("[{'name':\"Parroti'a persica Vanessa\"},{'id': 33}]"));
        tags.Add(JArray.Parse("[{'customer':'SuperYo'},{'id': 34}]"));

        var properties = TinyJson.GetProperties(tags[0]);
        Assert.That(properties[0]== "name");
        Assert.That(properties[1] == "id");

        var json = TinyJson.ToJson(tags, properties);
        var dynamicObject = JsonConvert.DeserializeObject<dynamic>(json)!;
        Assert.That(dynamicObject!=null);
    }

    [Test]
    public void AttributesWithQuotes2Handling()
    {
        var tags = new List<JArray>();
        tags.Add(JArray.Parse("[{'name2':'TV - Ecran plat:84\":3229035'},{'id': 35}]"));

        var properties = TinyJson.GetProperties(tags[0]);

        var json = TinyJson.ToJson(tags, properties);
        var dynamicObject = JsonConvert.DeserializeObject<dynamic>(json)!;
        Assert.That(dynamicObject != null);
    }


}
