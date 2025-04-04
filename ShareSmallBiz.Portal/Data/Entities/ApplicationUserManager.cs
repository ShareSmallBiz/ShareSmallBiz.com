using Microsoft.Extensions.Options;

namespace ShareSmallBiz.Portal.Data.Entities;

public class ApplicationUserManager : UserManager<ShareSmallBizUser>
{
    public ApplicationUserManager(
        IUserStore<ShareSmallBizUser> store,
        IOptions<IdentityOptions> optionsAccessor,
        IPasswordHasher<ShareSmallBizUser> passwordHasher,
        IEnumerable<IUserValidator<ShareSmallBizUser>> userValidators,
        IEnumerable<IPasswordValidator<ShareSmallBizUser>> passwordValidators,
        ILookupNormalizer keyNormalizer,
        IdentityErrorDescriber errors,
        IServiceProvider services,
        ILogger<UserManager<ShareSmallBizUser>> logger)
        : base(store,
              optionsAccessor,
              passwordHasher,
              userValidators,
              passwordValidators,
              keyNormalizer,
              errors,
              services,
              logger)
    {
    }
}