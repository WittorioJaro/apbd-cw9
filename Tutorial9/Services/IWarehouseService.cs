using Tutorial9.Model;

namespace Tutorial9.Services;

public interface IWarehouseService
{
    Task<int> AddProductToWarehouse(int IdProduct, int IdWarehouse, int Amount, DateTime CreatedAt);
    Task<ProductWarehouseDetailsDTO> GetProductWarehouseDetailsById(int idProductWarehouse);
    Task<int> AddProductToWarehouseUsingStoredProcedure(int IdProduct, int IdWarehouse, int Amount, DateTime CreatedAt);
}