using Microsoft.EntityFrameworkCore.Design;

namespace ShareSmallBiz.Portal.Data;

public class ShareSmallBizUserContextFactory : IDesignTimeDbContextFactory<ShareSmallBizUserContext>
{
    public ShareSmallBizUserContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();
        var connectionString = "Data Source=c:\\websites\\ShareSmallBiz\\ShareSmallBizUser.db";
        var optionsBuilder = new DbContextOptionsBuilder<ShareSmallBizUserContext>();
        optionsBuilder.UseSqlite(connectionString);
        return new ShareSmallBizUserContext(optionsBuilder.Options);
    }
}
