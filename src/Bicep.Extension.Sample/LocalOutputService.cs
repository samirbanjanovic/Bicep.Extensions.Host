using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Bicep.Extension.Sample
{
    internal class LocalOutputService
        : IBackendService
    {

        public Task<bool> CreateOrUpdate(JsonObject json)
        {
            // serialize output to json
            var jsonString = json.ToJsonString();
            // write output to disk
            using var writer = new StreamWriter("output.json", append: true);           
            writer.Write(jsonString);
            
            return Task.FromResult(true);
        }

        public Task<bool> Delete(JsonObject json)
        {
            throw new Exception("Delete method is not implemented.");
        }

        public Task<bool> Get(JsonObject json)
        {
            throw new NotImplementedException("Get method is not implemented.");
        }

        public Task<bool> Preview(JsonObject json)
        {
            throw new NotImplementedException("Preview method is not implemented.");
        }
    }
}
