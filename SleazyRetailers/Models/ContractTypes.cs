namespace SleazyRetailers.Models
{
    public static class ContractTypes
    {
        public const string Supplier = "Supplier";
        public const string Customer = "Customer";
        public const string Service = "Service";
        public const string NDA = "NDA";
        public const string Purchase = "Purchase";
        public const string License = "License";
        public const string Employment = "Employment";
        public const string General = "General";

        public static string[] AllTypes => new[]
        {
            Supplier, Customer, Service, NDA, Purchase, License, Employment, General
        };
    }
}