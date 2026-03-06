using System.Data.Common;

namespace UKHO.Aspire.Configuration.Emulator.Data
{
    public interface IDbConnectionFactory
    {
        public DbConnection Create();
    }
}