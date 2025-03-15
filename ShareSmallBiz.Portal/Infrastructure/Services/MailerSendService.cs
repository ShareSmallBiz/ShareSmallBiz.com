using System.Text.Json;
namespace ShareSmallBiz.Portal.Infrastructure.Services;

public class MailerSendService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _apiKey;
    private readonly HttpClient _client;

    public MailerSendService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _apiKey = configuration["MAILER_SEND_API_KEY"] ?? throw new ArgumentNullException("MAILER_SEND_API_KEY is not configured");

        _client = _httpClientFactory.CreateClient();
        // Set the base address for the MailerSend API
        _client.BaseAddress = new Uri("https://api.mailersend.com/v1/");
        // Add the authorization header using the API key
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
    }

    /// <summary>
    /// Sends an email via the MailerSend API.
    /// </summary>
    /// <param name="fromEmail">Sender's email address</param>
    /// <param name="toEmail">Recipient's email address</param>
    /// <param name="subject">Subject of the email</param>
    /// <param name="htmlContent">HTML content of the email</param>
    /// <param name="textContent">Plain text content of the email</param>
    /// <returns>A string with the response from the API.</returns>
    public async Task<string> SendEmailAsync(string fromEmail, string toEmail, string subject, string htmlContent, string textContent)
    {
        // Construct the payload according to MailerSend API requirements
        var payload = new
        {
            from = new { email = fromEmail },
            to = new[] { new { email = toEmail } },
            subject = subject,
            html = htmlContent,
            text = textContent
        };

        // Serialize the payload to JSON
        var jsonPayload = JsonSerializer.Serialize(payload);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        // Post the email to the MailerSend API endpoint
        var response = await _client.PostAsync("email", content);

        // Throw an exception if the response indicates failure
        response.EnsureSuccessStatusCode();

        // Return the response content as string
        return await response.Content.ReadAsStringAsync();
    }
}
