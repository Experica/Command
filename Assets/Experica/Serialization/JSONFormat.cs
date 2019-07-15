using Experica;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Experica
{
    class JSONFormat : IFormat
    {

    public T Deserialize<T>(string data)
        {
            T obj = JsonConvert.DeserializeObject<T>(data);
            return obj;
        }

        public string Serialize<T>(T obj)
        {
            string json = JsonConvert.SerializeObject(obj, Formatting.Indented, new JsonSerializerSettings()
            {
                // This handles self-referencing loops.
                //PreserveReferencesHandling = PreserveReferencesHandling.All
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                //PreserveReferencesHandling = PreserveReferencesHandling.Objects
            });
            return json;
        }
    }
}
