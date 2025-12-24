using System.ComponentModel.DataAnnotations;

namespace FactorApp.UI.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; }

        [Required]
        public string PasswordHash { get; set; } // رمز هش شده
        
        [Required]
        public string PasswordSalt { get; set; } // نمک برای امنیت بیشتر

        public string FullName { get; set; }
        public bool IsActive { get; set; } = true;
    }
}