namespace FactorApp.UI.Models
{
    public class StoreInfo
    {
        public int Id { get; set; }
        public string StoreName { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public string FooterText { get; set; }

        // >>>> این خط را اضافه کنید <<<<
        public bool IsDarkMode { get; set; } = false;
    }
}