using Lots.Domain.Entities;
using Lots.Domain.Interfaces;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Lots.Infrastructure.Repository
{
    public class LotRepository : ILotRepository
    {
        private readonly MySqlConnection _connection;
        private readonly MySqlTransaction? _transaction;

        // Constructor para UnitOfWork
        public LotRepository(MySqlConnection connection, MySqlTransaction? transaction)
        {
            _connection = connection;
            _transaction = transaction;
        }

        // --- IMPLEMENTACIÓN DEL MÉTODO FALTANTE (Para las Sagas) ---
        public async Task<List<Lot>> GetByMedicineIdWithStockAsync(int medicineId)
        {
            var list = new List<Lot>();
            // Buscamos lotes de esa medicina, con stock positivo, no borrados
            // y ordenados por fecha de vencimiento (FEFO)
            string query = @"SELECT * FROM lots 
                             WHERE medicine_id = @medId 
                               AND quantity > 0 
                               AND is_deleted = 0 
                             ORDER BY expiration_date ASC";

            // Gestión de conexión segura para UnitOfWork
            if (_connection.State != ConnectionState.Open) await _connection.OpenAsync();

            using var cmd = new MySqlCommand(query, _connection, _transaction);
            cmd.Parameters.AddWithValue("@medId", medicineId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(MapReaderToLot(reader));
            }
            return list;
        }
        // -----------------------------------------------------------

        public async Task<List<Lot>> GetAllAsync()
        {
            var list = new List<Lot>();
            string query = @"SELECT * FROM lots WHERE is_deleted = 0 ORDER BY batch_number ASC";

            if (_connection.State != ConnectionState.Open) await _connection.OpenAsync();

            using var cmd = new MySqlCommand(query, _connection, _transaction);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(MapReaderToLot(reader));
            }
            return list;
        }

        public async Task<Lot?> GetByIdAsync(int id)
        {
            string query = "SELECT * FROM lots WHERE id = @id AND is_deleted = 0";
            if (_connection.State != ConnectionState.Open) await _connection.OpenAsync();

            using var cmd = new MySqlCommand(query, _connection, _transaction);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync()) return MapReaderToLot(reader);
            return null;
        }

        public async Task<int> CreateAsync(Lot lot)
        {
            string query = @"INSERT INTO lots (medicine_id, batch_number, expiration_date, quantity, unit_cost, created_at, is_deleted)
                             VALUES (@medicine_id, @batch_number, @expiration_date, @quantity, @unit_cost, @created_at, 0);
                             SELECT LAST_INSERT_ID();";

            if (_connection.State != ConnectionState.Open) await _connection.OpenAsync();

            using var cmd = new MySqlCommand(query, _connection, _transaction);
            cmd.Parameters.AddWithValue("@medicine_id", lot.medicine_id ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@batch_number", lot.batch_number);
            cmd.Parameters.AddWithValue("@expiration_date", lot.expiration_date);
            cmd.Parameters.AddWithValue("@quantity", lot.quantity);
            cmd.Parameters.AddWithValue("@unit_cost", lot.unit_cost);
            cmd.Parameters.AddWithValue("@created_at", lot.created_at);

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

            if (_connection.State != ConnectionState.Open) await _connection.OpenAsync();

            using var cmd = new MySqlCommand(query, _connection, _transaction);
            cmd.Parameters.AddWithValue("@id", lot.id);
            cmd.Parameters.AddWithValue("@medicine_id", lot.medicine_id ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@batch_number", lot.batch_number);
            cmd.Parameters.AddWithValue("@expiration_date", lot.expiration_date);
            cmd.Parameters.AddWithValue("@quantity", lot.quantity);
            cmd.Parameters.AddWithValue("@unit_cost", lot.unit_cost);
            cmd.Parameters.AddWithValue("@updated_at", DateTime.Now);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteAsync(int id)
        {
            string query = "UPDATE lots SET is_deleted = 1, updated_at = NOW() WHERE id = @id";
            if (_connection.State != ConnectionState.Open) await _connection.OpenAsync();

            using var cmd = new MySqlCommand(query, _connection, _transaction);
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<Lot>> GetByIdsAsync(List<int> ids)
        {
            if (ids == null || !ids.Any()) return new List<Lot>();

            var paramsNames = ids.Select((id, index) => $"@p{index}").ToList();
            string query = $"SELECT * FROM lots WHERE is_deleted = 0 AND id IN ({string.Join(",", paramsNames)})";

            if (_connection.State != ConnectionState.Open) await _connection.OpenAsync();

            using var cmd = new MySqlCommand(query, _connection, _transaction);
            for (int i = 0; i < ids.Count; i++) cmd.Parameters.AddWithValue($"@p{i}", ids[i]);

            var list = new List<Lot>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync()) list.Add(MapReaderToLot(reader));

            return list;
        }

        // Helper para mapear (Evita repetir código y errores)
        private Lot MapReaderToLot(IDataReader reader)
        {
            return new Lot
            {
                id = reader.GetInt32(reader.GetOrdinal("id")),
                medicine_id = reader.IsDBNull(reader.GetOrdinal("medicine_id")) ? null : reader.GetInt32(reader.GetOrdinal("medicine_id")),
                batch_number = reader.GetString(reader.GetOrdinal("batch_number")),
                expiration_date = reader.GetDateTime(reader.GetOrdinal("expiration_date")),
                quantity = reader.GetInt32(reader.GetOrdinal("quantity")),
                unit_cost = reader.GetDecimal(reader.GetOrdinal("unit_cost")),
                is_deleted = reader.GetBoolean(reader.GetOrdinal("is_deleted")),
                created_at = reader.GetDateTime(reader.GetOrdinal("created_at")),
                updated_at = reader.IsDBNull(reader.GetOrdinal("updated_at")) ? null : reader.GetDateTime(reader.GetOrdinal("updated_at"))
            };
        }
    }
}