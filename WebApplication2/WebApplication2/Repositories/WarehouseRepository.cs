using System.Data;
using System.Data.SqlClient;

namespace WebApplication2.Repositories;

public interface IWarehouseRepository
{
    public Task<int?> RegisterProductInWarehouseAsync(int idWarehouse, int idProduct, int idOrder, DateTime createdAt);
    public Task RegisterProductInWarehouseByProcedureAsync(int idWarehouse, int idProduct, DateTime createdAt);
    Task<bool> CheckIfProductExists(int dtoIdProduct);
    Task<bool> CheckIfWarehouseExists(int dtoIdWarehouse);
    Task<bool> CheckIfOrderExists(int dtoIdProduct,int dtoAmount,DateTime createdAt);
    Task<bool> CheckIfOrderCompleted(int dtoIdProduct, int dtoIdWarehouse,int dtoAmount);
}

public class WarehouseRepository : IWarehouseRepository
{
    private readonly IConfiguration _configuration;
    public WarehouseRepository(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<bool> CheckIfProductExists(int idProduct)
    {
        await using var connection = new SqlConnection(_configuration["ConnectionStrings:DefaultConnection"]);
        await connection.OpenAsync();

        var query = "SELECT COUNT(*) FROM Product WHERE IdProduct = @idProduct";
        await using var cmd = new SqlCommand(query, connection);
        cmd.Parameters.AddWithValue("@idProduct", idProduct);
        var result = (int)await cmd.ExecuteScalarAsync();

        return result > 0;
    }

    public async Task<bool> CheckIfWarehouseExists(int idWarehouse)
    {
        await using var connection = new SqlConnection(_configuration["ConnectionStrings:DefaultConnection"]);
        await connection.OpenAsync();

        var query = "SELECT COUNT(*) FROM Warehouse WHERE IdWarehouse = @idWarehouse";
        await using var cmd = new SqlCommand(query, connection);
        cmd.Parameters.AddWithValue("@idWarehouse", idWarehouse);
        var result = (int)await cmd.ExecuteScalarAsync();

        return result > 0;
    }

    public async Task<bool> CheckIfOrderExists(int idProduct, int amount, DateTime createdAt)
    {
        await using var connection = new SqlConnection(_configuration["ConnectionStrings:DefaultConnection"]);
        await connection.OpenAsync();

        var query = "SELECT COUNT(*) FROM Order WHERE IdProduct = @idProduct AND Amount = @amount AND CreatedAt < @createdAt";
        await using var cmd = new SqlCommand(query, connection);
        cmd.Parameters.AddWithValue("@idProduct", idProduct);
        cmd.Parameters.AddWithValue("@amount", amount);
        cmd.Parameters.AddWithValue("@createdAt", createdAt);
        var result = (int)await cmd.ExecuteScalarAsync();

        return result > 0;
    }

    public async Task<bool> CheckIfOrderCompleted(int idProduct, int idWarehouse, int amount)
    {
        await using var connection = new SqlConnection(_configuration["ConnectionStrings:DefaultConnection"]);
        await connection.OpenAsync();

        var query = "SELECT COUNT(*) FROM Product_Warehouse WHERE IdProduct = @idProduct AND Amount = @amount AND IdWarehouse = @idWarehouse";
        await using var cmd = new SqlCommand(query, connection);
        cmd.Parameters.AddWithValue("@idProduct", idProduct);
        cmd.Parameters.AddWithValue("@amount", amount);
        cmd.Parameters.AddWithValue("@idWarehouse", idWarehouse);
        var result = (int)await cmd.ExecuteScalarAsync();

        return result > 0;
    }

    public async Task<int?> RegisterProductInWarehouseAsync(int idWarehouse, int idProduct, int idOrder, DateTime createdAt)
    {
        await using var connection = new SqlConnection(_configuration["ConnectionStrings:DefaultConnection"]);
        await connection.OpenAsync();
        
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            var query = "UPDATE \"Order\" SET FulfilledAt = @FulfilledAt WHERE IdOrder = @IdOrder";
            await using var command = new SqlCommand(query, connection);
            command.Transaction = (SqlTransaction)transaction;
            command.Parameters.AddWithValue("@IdOrder", idOrder);
            command.Parameters.AddWithValue("@FulfilledAt", DateTime.UtcNow);
            await command.ExecuteNonQueryAsync();
            
            command.CommandText = @"
                          INSERT INTO Product_Warehouse (IdWarehouse, IdProduct, IdOrder, CreatedAt, Amount, Price)
                          OUTPUT Inserted.IdProductWarehouse
                          VALUES (@IdWarehouse, @IdProduct, @IdOrder, @CreatedAt, 0, 0);";
            command.Parameters.Clear();
            command.Parameters.AddWithValue("@IdWarehouse", idWarehouse);
            command.Parameters.AddWithValue("@IdProduct", idProduct);
            command.Parameters.AddWithValue("@IdOrder", idOrder);
            command.Parameters.AddWithValue("@CreatedAt", createdAt);
            var idProductWarehouse = (int)await command.ExecuteScalarAsync();

            await transaction.CommitAsync();
            return idProductWarehouse;
        }
        catch
        {
            await transaction.RollbackAsync();
            return null;
        }
    }
    
    public async Task RegisterProductInWarehouseByProcedureAsync(int idWarehouse, int idProduct, DateTime createdAt)
    {
        await using var connection = new SqlConnection(_configuration["ConnectionStrings:DefaultConnection"]);
        await connection.OpenAsync();
        await using var command = new SqlCommand("AddProductToWarehouse", connection);
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.AddWithValue("IdProduct", idProduct);
        command.Parameters.AddWithValue("IdWarehouse", idWarehouse);
        command.Parameters.AddWithValue("Amount", 0);
        command.Parameters.AddWithValue("CreatedAt", createdAt);
        await command.ExecuteNonQueryAsync();
    }
}