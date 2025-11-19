using Lots.Domain.Entities;
using Lots.Domain.Interfaces;
using Lots.Domain.Services;
using Lots.Infrastructure.Persistence;
using MySql.Data.MySqlClient;
using System.Data;

namespace Lots.Infrastructure.Repository
{
    public class LotRepository : ILotRepository
    {
        private readonly MySqlConnectionDB _connectionDB;

        public LotRepository(MySqlConnectionDB con)
        {
            _connectionDB = con;
        }
        public async Task<List<Lot>> GetAllAsync()
        {
            var list = new List<Lot>();
            string query = @"SELECT id, medicine_id, batch_number, expiration_date,
                                    quantity, unit_cost, is_deleted,
                                    created_at, updated_at
                             FROM lots
                             WHERE is_deleted = 0
                             ORDER BY batch_number ASC";

            using var conn = _connectionDB.GetConnection();
            using var cmd = new MySqlCommand(query, conn);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new Lot
                {
                    id = reader.GetInt32("id"),
                    medicine_id = reader["medicine_id"] == DBNull.Value
                                 ? null
                                 : reader.GetInt32("medicine_id"),
                    batch_number = reader.GetString("batch_number"),
                    expiration_date = reader.GetDateTime("expiration_date"),
                    quantity = reader.GetInt32("quantity"),
                    unit_cost = reader.GetDecimal("unit_cost"),
                    is_deleted = reader.GetBoolean("is_deleted"),
                    created_at = reader.GetDateTime("created_at"),
                    updated_at = reader["updated_at"] == DBNull.Value
                                ? null
                                : reader.GetDateTime("updated_at")
                });
            }

            return list;
        }

        public async Task<Lot?> GetByIdAsync(int id)
        {
            string query = @"SELECT id, medicine_id, batch_number, expiration_date,
                                    quantity, unit_cost, is_deleted,
                                    created_at, updated_at
                             FROM lots
                             WHERE id = @id AND is_deleted = 0";

            using var conn = _connectionDB.GetConnection();
            using var cmd = new MySqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@id", id);
            await conn.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            return new Lot
            {
                id = reader.GetInt32("id"),
                medicine_id = reader["medicine_id"] == DBNull.Value
                             ? null
                             : reader.GetInt32("medicine_id"),
                batch_number = reader.GetString("batch_number"),
                expiration_date = reader.GetDateTime("expiration_date"),
                quantity = reader.GetInt32("quantity"),
                unit_cost = reader.GetDecimal("unit_cost"),
                is_deleted = reader.GetBoolean("is_deleted"),
                created_at = reader.GetDateTime("created_at"),
                updated_at = reader["updated_at"] == DBNull.Value
                            ? null
                            : reader.GetDateTime("updated_at")
            };
        }
        public async Task<int> CreateAsync(Lot lot)
        {
            string query = @"INSERT INTO lots
                             (medicine_id, batch_number, expiration_date, quantity,
                              unit_cost, created_at)
                             VALUES
                             (@medicine_id, @batch_number, @expiration_date, 
                              @quantity, @unit_cost, @created_at);
                             SELECT LAST_INSERT_ID();";

            using var conn = _connectionDB.GetConnection();
            using var cmd = new MySqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@medicine_id", lot.medicine_id ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@batch_number", lot.batch_number);
            cmd.Parameters.AddWithValue("@expiration_date", lot.expiration_date);
            cmd.Parameters.AddWithValue("@quantity", lot.quantity);
            cmd.Parameters.AddWithValue("@unit_cost", lot.unit_cost);
            cmd.Parameters.AddWithValue("@created_at", lot.created_at);

            await conn.OpenAsync();
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        public async Task UpdateAsync(Lot lot)
        {
            string query = @"UPDATE lots SET
                                medicine_id = @medicine_id,
                                batch_number = @batch_number,
                                expiration_date = @expiration_date,
                                quantity = @quantity,
                                unit_cost = @unit_cost,
                                updated_at = @updated_at
                             WHERE id = @id";

            using var conn = _connectionDB.GetConnection();
            using var cmd = new MySqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@id", lot.id);
            cmd.Parameters.AddWithValue("@medicine_id", lot.medicine_id ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@batch_number", lot.batch_number);
            cmd.Parameters.AddWithValue("@expiration_date", lot.expiration_date);
            cmd.Parameters.AddWithValue("@quantity", lot.quantity);
            cmd.Parameters.AddWithValue("@unit_cost", lot.unit_cost);
            cmd.Parameters.AddWithValue("@updated_at", lot.updated_at);

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteAsync(int id)
        {
            string query = @"UPDATE lots
                             SET is_deleted = 1,
                                 updated_at = NOW()
                             WHERE id = @id";

            using var conn = _connectionDB.GetConnection();
            using var cmd = new MySqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@id", id);

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
