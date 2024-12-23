using System.ComponentModel.DataAnnotations;

namespace UserManagementAPI.Models
{
    public class User
    {
        public int Id { get; set; }
        
        [StringLength(100, MinimumLength = 1)]
        required public string Name { get; set; }
        required public string Email { get; set; }
    }
}