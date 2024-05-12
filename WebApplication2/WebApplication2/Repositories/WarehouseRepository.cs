using System.Data;
using System.Data.SqlClient;

namespace WebApplication2.Repositories;

public interface IWarehouseRepository
{
    public Task<int?> RegisterProductInWarehouseAsync(int idWarehouse, int idProduct, int idOrder, DateTime createdAt);
    public Task RegisterProductInWarehouseByProcedureAsync(int idWarehouse, int idProduct, DateTime createdAt);
    Task<bool> CheckIfProductExists(int dtoIdProduct);
    Task<bool> CheckIfWarehouseExists(int dtoIdWarehouse);
    Task<bool> CheckIfOrderExists(int dtoIdProduct,int dtoAmount);
    Task<bool> CheckIfOrderCompleted(int dtoIdProduct, int dtoIdWarehouse,int dtoAmount);
}

public class WarehouseRepository : IWarehouseRepository
{
    private readonly IConfiguration _configuration;
    public WarehouseRepository(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<bool> CheckIfProductExists(int dtoIdProduct)
    {
        await using var connection = new SqlConnection(_configuration["ConnectionStrings:DefaultConnection"]);
        await connection.OpenAsync();
        
        await using var transaction = await connection.BeginTransactionAsync();
        try
        {
            var query = "SELECT TOP 1 IdProduct FROM Product WHERE @idProduct = IdProduct";
            await using var cmd = new SqlCommand(query, connection);
            cmd.Transaction = (SqlTransaction)transaction;
            cmd.Parameters.AddWithValue("@idProduct", dtoIdProduct);
            var result = await cmd.ExecuteNonQueryAsync();

            return result != null;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> CheckIfWarehouseExists(int dtoIdWarehouse)
    {
        await using var connection = new SqlConnection(_configuration["ConnectionStrings:DefaultConnection"]);
        await connection.OpenAsync();
        
        await using var transaction = await connection.BeginTransactionAsync();
        try
        {
            var query = "SELECT TOP 1 IdWarehouse FROM Warehouse WHERE @idWarehouse = IdWarehouse";
            await using var cmd = new SqlCommand(query, connection);
            cmd.Transaction = (SqlTransaction)transaction;
            cmd.Parameters.AddWithValue("@idWarehouse", dtoIdWarehouse);
            var result = await cmd.ExecuteNonQueryAsync();

            return result != null;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> CheckIfOrderExists(int dtoIdProduct, int dtoAmount)
    {
        await using var connection = new SqlConnection(_configuration["ConnectionStrings:DefaultConnection"]);
        await connection.OpenAsync();
        
        await using var transaction = await connection.BeginTransactionAsync();
        try
        {
            var query = "SELECT TOP 1 IdProduct FROM Order WHERE @idProduct = IdProduct AND @Amount = Amount";
            await using var cmd = new SqlCommand(query, connection);
            cmd.Transaction = (SqlTransaction)transaction;
            cmd.Parameters.AddWithValue("@idProduct", dtoIdProduct);
            cmd.Parameters.AddWithValue("@Amount", dtoAmount);
            var result = await cmd.ExecuteNonQueryAsync();

            return result != null;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> CheckIfOrderCompleted(int dtoIdProduct, int dtoIdWarehouse,int dtoAmount)
    {
        await using var connection = new SqlConnection(_configuration["ConnectionStrings:DefaultConnection"]);
        await connection.OpenAsync();
        
        await using var transaction = await connection.BeginTransactionAsync();
        try
        {
            var query = "SELECT TOP 1 IdOrder FROM Product_Warehouse WHERE @idProduct = IdProduct AND @Amount = Amount AND @idWarehouse = IdWarehouse";
            await using var cmd = new SqlCommand(query, connection);
            cmd.Transaction = (SqlTransaction)transaction;
            cmd.Parameters.AddWithValue("@idProduct", dtoIdProduct);
            cmd.Parameters.AddWithValue("@Amount", dtoAmount);
            cmd.Parameters.AddWithValue("@idWarehouse", dtoIdWarehouse);
            var result = await cmd.ExecuteNonQueryAsync();

            return result != null;
        }
        catch
        {
            return false;
        }
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
        command.Parameters.AddWithValue("IdWarehouse",idWarehouse);
        command.Parameters.AddWithValue("Amount", 0);
        command.Parameters.AddWithValue("CreatedAt", createdAt);
        await command.ExecuteNonQueryAsync();
    }
}