using System.Collections.Generic;
using System.Text;
using System;

namespace Application.ViewModels.OrderModel.Api
{
    // --- Lookup ---

    public class CodeLookupDto
    {
        public int? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Rate { get; set; }
    }

    public class MetrologicalLookupDto
    {
        public bool Success { get; set; }
        public int? Id { get; set; }
        public string? RegistrationNumber { get; set; }
        public string? ExpiryDate { get; set; }
    }

    public class TaxTypeLookupDto
    {
        public int? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Cost { get; set; }
        public string MeasureUnit { get; set; } = "₽";
        public bool IsPercentage { get; set; }
    }

    public class CompanyLookupDto
    {
        public int? Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class CustomerLookupDto
    {
        public int? Id { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
    }

    // --- Tax management ---

    public class UpdateCodeRateRequest
    {
        public int? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal NewRate { get; set; }
    }

    public class UpdateCodeRateResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? CodeId { get; set; }
        public string? CodeName { get; set; }
        public decimal CodeRate { get; set; }
    }

    public class AddTaxToOrderResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? TaxId { get; set; }
        public string? TaxName { get; set; }
        public decimal TaxCost { get; set; }
        public string? MeasureUnit { get; set; }
    }

    public class RemoveTaxFromOrderResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    // --- Status ---

    public class UpdateOrderStatusResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? OldStatus { get; set; }
        public string? NewStatus { get; set; }
        public DateTime ChangeDate { get; set; }
        public string? ChangedBy { get; set; }
    }

    public class StatusHistoryItemDto
    {
        public int? Id { get; set; }
        public string OldStatus { get; set; } = string.Empty;
        public string OldStatusDisplay { get; set; } = string.Empty;
        public string NewStatus { get; set; } = string.Empty;
        public string NewStatusDisplay { get; set; } = string.Empty;
        public string ChangeDate { get; set; } = string.Empty;
        public string ChangedBy { get; set; } = string.Empty;
    }

    // --- Quick create ---

    public class CreateCompanyRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    public class CreateCustomerRequest
    {
        public int? CompanyId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }

    public class CreateManufacturerRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    public class CreatedCompanyDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? Id { get; set; }
        public string? Name { get; set; }
    }

    public class CreatedCustomerDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? Id { get; set; }
        public string? FullName { get; set; }
    }

    public class CreatedManufacturerDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? Id { get; set; }
        public string? Name { get; set; }
    }
}
