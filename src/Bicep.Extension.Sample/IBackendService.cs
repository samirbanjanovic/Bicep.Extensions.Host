using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Bicep.Extension.Sample
{
    public interface IBackendService
    {
        Task<bool> CreateOrUpdate(JsonObject json);
        Task<bool> Delete(JsonObject json);
        Task<bool> Get(JsonObject json);
        Task<bool> Preview(JsonObject json);
    }
}
