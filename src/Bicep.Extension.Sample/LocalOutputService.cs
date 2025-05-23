namespace Bicep.Extension.Sample
{
    internal class LocalOutputService
        : IBackendService
    {

        public Task<bool> CreateOrUpdate(string json)
        {
            // write output to disk
            using var writer = new StreamWriter("output.json", append: true);
            writer.Write(json);

            return Task.FromResult(true);
        }

        public Task<bool> Delete(string json)
        {
            throw new Exception("Delete method is not implemented.");
        }

        public Task<bool> Get(string json)
        {
            throw new NotImplementedException("Get method is not implemented.");
        }

        public Task<bool> Preview(string json)
        {
            throw new NotImplementedException("Preview method is not implemented.");
        }
    }
}
