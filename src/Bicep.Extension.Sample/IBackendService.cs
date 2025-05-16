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
        Task<bool> CreateOrUpdate(string json);
        Task<bool> Delete(string json);
        Task<bool> Get(string json);
        Task<bool> Preview(string json);
    }
}
