using Microsoft.AspNetCore.Mvc;
using Tutorial9.Model;
using Tutorial9.Services;

namespace Tutorial9.Controllers;

[Route("api/[controller]")]
[ApiController]
public class WarehouseController : ControllerBase
{
    private readonly IWarehouseService _warehouseService;
    
    public WarehouseController(IWarehouseService warehouseService)
    {
        _warehouseService = warehouseService;
    }
    
    [HttpPost]
    public async Task<IActionResult> AddProductToWarehouse([FromBody] ProductWarehouseDTO dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var newId = await _warehouseService.AddProductToWarehouse(
                dto.IdProduct,
                dto.IdWarehouse,
                dto.Amount,
                dto.CreatedAt
            );

            return CreatedAtAction(
                nameof(GetProductWarehouseById),
                new { id = newId },
                newId
            );
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetProductWarehouseById(int id)
    {
        var model = await _warehouseService.GetProductWarehouseDetailsById(id);
        if (model == null)
            return NotFound($"Entry with Id = {id} not found");
        return Ok(model);
    }
    
    [HttpPost("stored-procedure")]
    public async Task<IActionResult> AddProductToWarehouseUsingStoredProcedure([FromBody] ProductWarehouseDTO dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var newId = await _warehouseService.AddProductToWarehouseUsingStoredProcedure(
                dto.IdProduct,
                dto.IdWarehouse,
                dto.Amount,
                dto.CreatedAt
            );

            return CreatedAtAction(
                nameof(GetProductWarehouseById),
                new { id = newId },
                newId
            );
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
    
}