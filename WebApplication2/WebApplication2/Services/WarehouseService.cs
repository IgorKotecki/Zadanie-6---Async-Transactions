using System.ComponentModel.DataAnnotations;
using WebApplication2.Dto;
using WebApplication2.Exceptions;
using WebApplication2.Repositories;

namespace WebApplication2.Services;

public interface IWarehouseService
{
    public Task<int> RegisterProductInWarehouseAsync(RegisterProductInWarehouseRequestDTO dto);
}

public class WarehouseService : IWarehouseService
{
    private readonly IWarehouseRepository _warehouseRepository;
    
    public WarehouseService(IWarehouseRepository warehouseRepository)
    {
        _warehouseRepository = warehouseRepository;
    }
    
    public async Task<int> RegisterProductInWarehouseAsync(RegisterProductInWarehouseRequestDTO dto)
    {
        var productExists = await _warehouseRepository.CheckIfProductExists(dto.IdProduct.Value);
        if (!productExists)
            throw new NotFoundException("Product with the provided identifier does not exist.");
        
        var warehouseExists = await _warehouseRepository.CheckIfWarehouseExists(dtoIdWarehouse: dto.IdWarehouse.Value);
        if (!warehouseExists)
            throw new NotFoundException("Warehouse with the provided identifier does not exist.");
        
        if (dto.Amount <= 0)
            throw new ValidationException("Amount must be greater than 0.");

        var orderExists = await _warehouseRepository.CheckIfOrderExists(dtoIdProduct:dto.IdProduct.Value,dtoAmount: dto.Amount.Value);
        if (!orderExists)
            throw new NotFoundException("Order with the provided identifier does not exist.");

        var chceckIfInProductWarehouse =
            await _warehouseRepository.CheckIfOrderCompleted(dtoIdProduct:dto.IdProduct.Value, dtoIdWarehouse: dto.IdWarehouse.Value,
                dtoAmount:dto.Amount.Value);
        if (chceckIfInProductWarehouse)
            throw new NotFoundException("Order with procided identifier already exists");
        
        const int idOrder = 1;

        var idProductWarehouse = await _warehouseRepository.RegisterProductInWarehouseAsync(
            idWarehouse: dto.IdWarehouse!.Value,
            idProduct: dto.IdProduct!.Value,
            idOrder: idOrder,
            createdAt: DateTime.UtcNow);

        if (!idProductWarehouse.HasValue)
            throw new Exception("Failed to register product in warehouse");

        return idProductWarehouse.Value;
    }
}