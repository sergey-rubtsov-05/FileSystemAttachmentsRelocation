using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace FSAR.DataAccessLayer
{
    public abstract class BaseRepository : IDisposable
    {
        protected abstract string TableName { get; }
        protected IDbConnection Session { get; private set; }

        protected BaseRepository()
        {
            Session = new SqlConnection(GetConnectionString("Database"));
        }

        public void Dispose()
        {
            Session?.Dispose();
            Session = null;
        }

        private string GetConnectionString(string connStringName)
        {
            var connectionString = ConfigurationManager.ConnectionStrings[connStringName];
            if (connectionString == null)
            {
                throw new ApplicationException($"Connection string {connStringName} not found");
            }

            return connectionString.ConnectionString;
        }
    }
}