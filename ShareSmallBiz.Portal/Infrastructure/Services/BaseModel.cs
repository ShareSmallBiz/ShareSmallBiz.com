namespace ShareSmallBiz.Portal.Infrastructure.Services
{
    public abstract class BaseModel
    {
        public int Id { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
        public string? CreatedID { get; set; }
        public string? ModifiedID { get; set; }

        // Automatically ties entity to the user who created it
        public UserModel? CreatedUser { get; set; }
    }
}
