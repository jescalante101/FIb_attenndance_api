namespace Dtos.Manager
{
    public class UserPermissionDto
    {
        public int UserId { get; set; }
        public int PermissionId { get; set; }
        public string? UserName { get; set; } // To include user name in response
        public string? PermissionKey { get; set; } // To include permission key in response
        public string? PermissionName { get; set; } // To include permission name in response
    }
}