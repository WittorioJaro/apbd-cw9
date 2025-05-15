using System.ComponentModel.DataAnnotations;

namespace Tutorial9.Model;

public class ProductWarehouseDTO
{
    [Required]
    public int IdProduct { get; set; }
    
    [Required]
    public int IdWarehouse { get; set; }
    
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public int Amount { get; set; }
    
    [Required]
    public DateTime CreatedAt { get; set; }
}

public class ProductWarehouseDetailsDTO
{
    public int IdProductWarehouse { get; set; }
    public int IdWarehouse { get; set; }
    public int IdProduct { get; set; }
    public int IdOrder { get; set; }
    public int Amount { get; set; }
    public Decimal Price { get; set; }
    public DateTime CreatedAt { get; set; }
}