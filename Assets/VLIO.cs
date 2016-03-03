using UnityEngine;
using System.Collections;
using System.IO;
using System.Text;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;

public class VLIO {

	public static void WriteYaml(string path, object data)
    {
        var serializer = new Serializer();
        var s = new StringBuilder();
        serializer.Serialize(new StringWriter(s), data);
        File.WriteAllText(path, s.ToString());
    }

    public static T ReadYaml<T>(string path)
    {
        using (var s = new StringReader(File.ReadAllText(path)))
        {
            var deserializer = new Deserializer();
            return deserializer.Deserialize<T>(s);
        }
    }
}
