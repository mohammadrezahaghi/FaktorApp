using System;

namespace FactorApp.UI.Models // <--- چک کن
{
    public enum CalculationMethod
    {
        FixedQuantity,
        AreaBased
    }

    public class Service
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public CalculationMethod Method { get; set; }
        public string? Description { get; set; }
    }
}