using System.Collections.Generic;

namespace FactorApp.Models
{
    public class ChangelogModel
    {
        public string Version { get; set; } = ""; // مقدار پیش‌فرض
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        
        // جلوگیری از نال بودن لیست با مقداردهی اولیه
        public List<string> Features { get; set; } = new List<string>(); 
    }
}