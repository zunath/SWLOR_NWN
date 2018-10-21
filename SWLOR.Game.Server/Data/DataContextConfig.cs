using SWLOR.Game.Server.Data.Contracts;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.EntityClient;
using System.Data.SqlClient;
using System.Linq;

namespace SWLOR.Game.Server.Data
{
    public partial class DataContext: IDataContext
    {

        // I can't figure out a way to prevent the EDMX from generating a constructor.
        // So I'm using this constructor with a useless parameter so that Autofac will use this constructor instead.
        // Please fix this if you know a way to keep the EDMX from generating that annoying constructor.
        public DataContext(bool useCustom)
            : base(BuildConnectionString())
        {
        }

        private static string BuildConnectionString()
        {
            var ipAddress = Environment.GetEnvironmentVariable("SQL_SERVER_IP_ADDRESS");
            var username = Environment.GetEnvironmentVariable("SQL_SERVER_USERNAME");
            var password = Environment.GetEnvironmentVariable("SQL_SERVER_PASSWORD");
            var database = Environment.GetEnvironmentVariable("SQL_SERVER_DATABASE");
            SqlConnectionStringBuilder sqlString = new SqlConnectionStringBuilder()
            {
                DataSource = ipAddress,
                InitialCatalog = database,
                UserID = username,
                Password = password
            };

            EntityConnectionStringBuilder entityString = new EntityConnectionStringBuilder()
            {
                Provider = "System.Data.SqlClient",
                Metadata = "res://*/Data.DataContext.csdl|res://*/Data.DataContext.ssdl|res://*/Data.DataContext.msl",
                ProviderConnectionString = sqlString.ToString()

            };

            //return $"server={ipAddress};database={database};user id={username};password='{password}';Integrated Security=False;MultipleActiveResultSets=True;TrustServerCertificate=True;Encrypt=False";
            return entityString.ConnectionString;
        }

        private string BuildSQLQuery(string procedureName, params SqlParameter[] args)
        {
            string sql = procedureName;

            for (int x = 0; x < args.Length; x++)
            {
                sql += " @" + args[x].ParameterName;

                if (x + 1 < args.Length) sql += ",";
            }

            return sql;
        }

        public void StoredProcedure(string procedureName, params SqlParameter[] args)
        {
            Database.ExecuteSqlCommand(BuildSQLQuery(procedureName, args), args);
        }

        public List<T> StoredProcedure<T>(string procedureName, params SqlParameter[] args)
        {
            return Database.SqlQuery<T>(BuildSQLQuery(procedureName, args), args).ToList();
        }

        public T StoredProcedureSingle<T>(string procedureName, params SqlParameter[] args)
        {
            return Database.SqlQuery<T>(BuildSQLQuery(procedureName, args), args).SingleOrDefault();
        }
    }
}
