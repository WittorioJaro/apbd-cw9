using System.Data;
using Microsoft.Data.SqlClient;
using Tutorial9.Model;

namespace Tutorial9.Services;

public class WarehouseService : IWarehouseService
{
    private readonly IDbService _dbService;
    private readonly IConfiguration _configuration;

    public WarehouseService(IDbService dbService, IConfiguration configuration)
    {
        _dbService = dbService;
        _configuration = configuration;
    }
    
    public async Task<ProductWarehouseDetailsDTO> GetProductWarehouseDetailsById(int idProductWarehouse)
    {
        const string sql = @"SELECT IdProductWarehouse, IdOrder, IdProduct, IdWarehouse, Amount, Price, CreatedAt
                            FROM Product_Warehouse
                            WHERE IdProductWarehouse = @IdProductWarehouse";
        
        using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default")))
        {
            await connection.OpenAsync();
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@IdProductWarehouse", idProductWarehouse);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (!reader.Read())
                        throw new Exception("Nie znaleziono produktu w magazynie");
                    
                    return new ProductWarehouseDetailsDTO
                    {
                        IdProductWarehouse = reader.GetInt32(0),
                        IdOrder = reader.GetInt32(1),
                        IdProduct = reader.GetInt32(2),
                        IdWarehouse = reader.GetInt32(3),
                        Amount = reader.GetInt32(4),
                        Price = reader.GetDecimal(5),
                        CreatedAt = reader.GetDateTime(6)
                    };
                }
            }
        }
    }
    
    public async Task<int> AddProductToWarehouseUsingStoredProcedure(int idProduct, int idWarehouse, int amount, DateTime createdAt)
    {
        using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default")))
        {
            await connection.OpenAsync();
            using (SqlCommand command = new SqlCommand("AddProductToWarehouse", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
            
                command.Parameters.AddWithValue("@IdProduct", idProduct);
                command.Parameters.AddWithValue("@IdWarehouse", idWarehouse);
                command.Parameters.AddWithValue("@Amount", amount);
                command.Parameters.AddWithValue("@CreatedAt", createdAt);
            
                var result = await command.ExecuteScalarAsync();
                if (result == null || result == DBNull.Value)
                    throw new Exception("Failed to add product to warehouse");
                
                return Convert.ToInt32(result);
            }
        }
    }

    public async Task<int> AddProductToWarehouse(int IdProduct, int IdWarehouse, int Amount, DateTime CreatedAt)
    {
        const string checkProduct = "SELECT IdProduct FROM Product WHERE IdProduct = @IdProduct";
        const string checkWarehouse = "SELECT IdWarehouse FROM Warehouse WHERE IdWarehouse = @IdWarehouse";
        const string checkOrder     = @"SELECT IdOrder, Amount, CreatedAt 
                                    FROM [Order] 
                                    WHERE IdProduct = @IdProduct 
                                      AND Amount = @Amount 
                                      AND CreatedAt < @RequestCreatedAt";
        const string checkCompleted = "SELECT IdOrder FROM Product_Warehouse WHERE IdOrder = @IdOrder";
        const string updateOrder    = "UPDATE [Order] SET FulfilledAt = @Now WHERE IdOrder = @IdOrder";
        const string insertStock    = @"INSERT INTO Product_Warehouse
                                    (IdOrder, IdProduct, IdWarehouse, Amount, Price, CreatedAt)
                                    VALUES (@IdOrder, @IdProduct, @IdWarehouse, @Amount, @Price, @Now);
                                    SELECT SCOPE_IDENTITY();";
        const string getUnitPrice = 
            "SELECT Price FROM Product WHERE IdProduct = @IdProduct";
        
        using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default")))
        {
            await connection.OpenAsync();
            using (var transaction = connection.BeginTransaction())
            {
                try
                {   
                    using (SqlCommand command = new SqlCommand(checkProduct, connection, transaction))
                    {
                        command.Parameters.AddWithValue("@IdProduct", IdProduct);
                        var productExists = await command.ExecuteScalarAsync();
                
                        if (productExists == null)
                        {
                            throw new Exception("Product does not exist");
                        }
                    }
                    decimal unitPrice;
                    using (var cmd = new SqlCommand(getUnitPrice, connection, transaction))
                    {
                        cmd.Parameters.AddWithValue("@IdProduct", IdProduct);
                        var result = await cmd.ExecuteScalarAsync();
                        if (result == null)
                            throw new Exception("Nie udało się pobrać ceny produktu");
                        unitPrice = (decimal)result;
                    }

                    using (SqlCommand command = new SqlCommand(checkWarehouse, connection, transaction))
                    {
                        command.Parameters.AddWithValue("@IdWarehouse", IdWarehouse);
                        var warehouseExists = await command.ExecuteScalarAsync();
                        if (warehouseExists == null)
                        {
                            throw new Exception("Warehouse does not exist");
                        }
                    }
                    
                    int orderId, amount;
                    DateTime createdAt;
                    using (SqlCommand command = new SqlCommand(checkOrder, connection, transaction))
                    {
                        command.Parameters.AddWithValue("@IdProduct", IdProduct);
                        command.Parameters.AddWithValue("@Amount", Amount);
                        command.Parameters.AddWithValue("@RequestCreatedAt", CreatedAt);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (!reader.Read())
                                throw new Exception("Brak pasującego zamówienia");
                        
                            orderId = reader.GetInt32(0);
                            amount = reader.GetInt32(1);
                            createdAt = reader.GetDateTime(2);
                    
                        }
                    }

                    using (SqlCommand command = new SqlCommand(checkCompleted, connection, transaction))
                    {
                        command.Parameters.AddWithValue("@IdOrder", orderId);
                        var fulfilled = await command.ExecuteScalarAsync();
                        if (fulfilled != null)
                        {
                            throw new Exception("Zamówienie zostało już zrealizowane");
                        }
                    }

                    using (SqlCommand command = new SqlCommand(updateOrder, connection, transaction))
                    {
                        command.Parameters.AddWithValue("@Now", DateTime.UtcNow);
                        command.Parameters.AddWithValue("@IdOrder", orderId);
                        await command.ExecuteNonQueryAsync();
                    }

                    decimal price = Amount * unitPrice;
                    int newId;
                    using (var cmd = new SqlCommand(insertStock, connection, transaction))
                    {
                        cmd.Parameters.AddWithValue("@IdOrder",      orderId);
                        cmd.Parameters.AddWithValue("@IdProduct",    IdProduct);
                        cmd.Parameters.AddWithValue("@IdWarehouse",  IdWarehouse);
                        cmd.Parameters.AddWithValue("@Amount",       amount);
                        cmd.Parameters.AddWithValue("@Price",        price);
                        cmd.Parameters.AddWithValue("@Now",          DateTime.UtcNow);
                        newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    }

                    transaction.Commit();
                    return newId;
                    
                }
                catch 
                {
                    transaction.Rollback();
                    throw ;
                }
                
            }
            
        }
    }
    
}