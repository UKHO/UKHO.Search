using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace UKHO.Search.Ingestion.Tests.TestSupport
{
    internal sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;

        public string ApplicationName { get; set; } = "UKHO.Search.Ingestion.Tests";

        public string ContentRootPath
        {
            get => _contentRootPath;
            set
            {
                _contentRootPath = value;
                ContentRootFileProvider = new PhysicalFileProvider(_contentRootPath);
            }
        }

        public IFileProvider ContentRootFileProvider { get; set; }

        private string _contentRootPath = Directory.GetCurrentDirectory();

        public TestHostEnvironment()
        {
            ContentRootFileProvider = new PhysicalFileProvider(_contentRootPath);
        }
    }
}
