using Application.DataAccessLayer.Interface.Common;
using Application.DataAccessLayer.Interface.OrderService;
using Application.ViewModels.OrderModel.Api;
using DB.Entity;
using Microsoft.EntityFrameworkCore;

namespace Application.DataAccessLayer.Service.OrderService
{
    public class OrderTaxManagementService : IOrderTaxManagementService
    {
        private readonly IUnitOfWork _unitOfWork;

        public OrderTaxManagementService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<UpdateCodeRateResult> UpdateCodeRateAsync(UpdateCodeRateRequest request, CancellationToken ct = default)
        {
            CodeTNVD codeTNVD = null;

            if (request.Id.HasValue && request.Id.Value > 0)
                codeTNVD = await _unitOfWork.GetRepository<CodeTNVD>().GetById(request.Id.Value);

            if (codeTNVD == null)
                codeTNVD = await _unitOfWork.GetRepository<CodeTNVD>()
                    .FindFirstOrDefault(c => c.Name == request.Name && !c.IsDeleted);

            bool wasCreated = false;

            if (codeTNVD == null)
            {
                codeTNVD = new CodeTNVD
                {
                    Name = request.Name,
                    Rate = request.NewRate,
                    IsDeleted = false
                };
                await _unitOfWork.GetRepository<CodeTNVD>().Create(codeTNVD);
                wasCreated = true;
            }
            else
            {
                codeTNVD.Rate = request.NewRate;
                _unitOfWork.GetRepository<CodeTNVD>().Update(codeTNVD);
            }

            await _unitOfWork.SaveChangesAsync(ct);

            return new UpdateCodeRateResult
            {
                Success = true,
                Message = wasCreated ? "Новый код создан" : "Ставка успешно обновлена",
                CodeId = codeTNVD.Id,
                CodeName = codeTNVD.Name,
                CodeRate = codeTNVD.Rate
            };
        }

        public async Task<AddTaxToOrderResult> AddTaxToOrderAsync(int orderId, int taxTypeId, CancellationToken ct = default)
        {
            AddTaxToOrderResult result = null;

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var taxType = await _unitOfWork.GetRepository<TaxType>()
                    .GetQueryable()
                    .Include(tt => tt.MeasureUnit)
                    .FirstOrDefaultAsync(tt => tt.Id == taxTypeId && !tt.IsDeleted, ct);

                if (taxType == null)
                {
                    result = new AddTaxToOrderResult { Success = false, Message = "Тип налога не найден" };
                    return;
                }

                var existingOrderTax = await _unitOfWork.GetRepository<OrderTax>()
                    .GetQueryable(includeDeleted: true)
                    .FirstOrDefaultAsync(ot => ot.OrderId == orderId && ot.TaxTypeId == taxTypeId, ct);

                bool wasRestored = false;

                if (existingOrderTax != null)
                {
                    if (existingOrderTax.IsDeleted)
                    {
                        existingOrderTax.IsDeleted = false;
                        existingOrderTax.Cost = taxType.Cost;
                        _unitOfWork.GetRepository<OrderTax>().Update(existingOrderTax);
                        wasRestored = true;
                    }
                    else
                    {
                        result = new AddTaxToOrderResult { Success = false, Message = "Этот налог уже добавлен к заказу" };
                        return;
                    }
                }
                else
                {
                    var orderTax = new OrderTax
                    {
                        OrderId = orderId,
                        TaxTypeId = taxTypeId,
                        Cost = taxType.Cost,
                        IsCalculated = false,
                        IsDeleted = false
                    };
                    await _unitOfWork.GetRepository<OrderTax>().Create(orderTax);
                }

                var orderProducts = await _unitOfWork.GetRepository<OrderProduct>()
                    .GetQueryable()
                    .Where(op => op.OrderId == orderId)
                    .ToListAsync(ct);

                foreach (var orderProduct in orderProducts)
                {
                    var existingProductTax = await _unitOfWork.GetRepository<OrderTaxProduct>()
                        .GetQueryable(includeDeleted: true)
                        .FirstOrDefaultAsync(otp => otp.OrderId == orderId &&
                                                    otp.OrderProductId == orderProduct.Id &&
                                                    otp.TaxTypeId == taxTypeId, ct);

                    if (existingProductTax != null)
                    {
                        if (existingProductTax.IsDeleted)
                        {
                            existingProductTax.IsDeleted = false;
                            existingProductTax.Cost = taxType.Cost;
                            _unitOfWork.GetRepository<OrderTaxProduct>().Update(existingProductTax);
                        }
                    }
                    else
                    {
                        var orderTaxProduct = new OrderTaxProduct
                        {
                            OrderId = orderId,
                            OrderProductId = orderProduct.Id,
                            TaxTypeId = taxTypeId,
                            Cost = taxType.Cost,
                            IsCalculated = false,
                            IsDeleted = false
                        };
                        await _unitOfWork.GetRepository<OrderTaxProduct>().Create(orderTaxProduct);
                    }
                }

                await _unitOfWork.SaveChangesAsync(ct);

                result = new AddTaxToOrderResult
                {
                    Success = true,
                    Message = wasRestored ? "Налог восстановлен" : "Налог добавлен",
                    TaxId = taxType.Id,
                    TaxName = taxType.Name,
                    TaxCost = taxType.Cost,
                    MeasureUnit = taxType.MeasureUnit?.Name ?? "₽"
                };
            }, ct);

            return result ?? new AddTaxToOrderResult { Success = true };
        }

        public async Task<RemoveTaxFromOrderResult> RemoveTaxFromOrderAsync(int orderId, int taxTypeId, CancellationToken ct = default)
        {
            var orderTax = await _unitOfWork.GetRepository<OrderTax>()
                .GetQueryable()
                .FirstOrDefaultAsync(ot => ot.OrderId == orderId &&
                                           ot.TaxTypeId == taxTypeId &&
                                           !ot.IsDeleted, ct);

            if (orderTax == null)
                return new RemoveTaxFromOrderResult { Success = false, Message = "Налог не найден" };

            orderTax.IsDeleted = true;
            _unitOfWork.GetRepository<OrderTax>().Update(orderTax);

            var orderTaxProducts = await _unitOfWork.GetRepository<OrderTaxProduct>()
                .GetQueryable()
                .Where(otp => otp.OrderId == orderId &&
                              otp.TaxTypeId == taxTypeId &&
                              !otp.IsDeleted)
                .ToListAsync(ct);

            foreach (var taxProduct in orderTaxProducts)
            {
                taxProduct.IsDeleted = true;
                _unitOfWork.GetRepository<OrderTaxProduct>().Update(taxProduct);
            }

            await _unitOfWork.SaveChangesAsync(ct);

            return new RemoveTaxFromOrderResult { Success = true, Message = "Налог успешно удален" };
        }
    }
}
