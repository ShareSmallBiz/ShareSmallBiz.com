using WebSpark.HttpClientUtility.StringConverter;
using ShareSmallBiz.Portal.Areas.Media.Services;
using ShareSmallBiz.Portal.Infrastructure.Services;

namespace ShareSmallBiz.Portal.Infrastructure.Extensions
{
    public static class ApplicationServicesExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddSingleton<ILogger<Program>, Logger<Program>>();
            services.AddSingleton<IStringConverter, NewtonsoftJsonStringConverter>();
            services.AddSingleton(new ApplicationStatus(Assembly.GetExecutingAssembly()));

            services.AddScoped<ShareSmallBizUserManager, ShareSmallBizUserManager>();
            services.AddScoped<DiscussionProvider, DiscussionProvider>();
            services.AddScoped<UserProvider, UserProvider>();
            services.AddScoped<CommentProvider, CommentProvider>();
            services.AddScoped<KeywordProvider, KeywordProvider>();
            services.AddScoped<AdminCommentService, AdminCommentService>();
            services.AddScoped<MailerSendService, MailerSendService>();
            services.AddScoped<StorageProviderService, StorageProviderService>();
            services.AddScoped<StatsService, StatsService>();
            services.AddScoped<SearchService, SearchService>();
            services.AddScoped<NotificationService, NotificationService>();
            services.AddScoped<MessageService, MessageService>();
            services.AddScoped<ArticleService, ArticleService>();
            services.AddScoped<EventService, EventService>();
            return services;
        }
    }
}
