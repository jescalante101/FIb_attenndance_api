namespace Dtos.Manager
{
    public class AppUserDto
    {
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public string Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
