using ApiTestGenerator.Services;
using HttpClientUtility.ClientService;
using HttpClientUtility.StringConverter;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();

// Add services
builder.Services.AddScoped<IOpenApiParser, OpenApiParser>();
builder.Services.AddScoped<IApiTestService, ApiTestService>();
builder.Services.AddScoped<ITestDataService, TestDataService>();

// HTTP Client
builder.Services.AddHttpClient();
builder.Services.AddScoped<IStringConverter, SystemJsonStringConverter>();
builder.Services.AddScoped<IHttpClientService, HttpClientService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
