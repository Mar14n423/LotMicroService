using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lots.Infrastructure.Persistence
{
    public class MySqlConnectionDB
    {
        private readonly string _connectionString;

        public MySqlConnectionDB(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public MySqlConnection GetConnection()
            => new MySqlConnection(_connectionString);
    }
}
