using System.Security.Claims;

namespace ShareSmallBiz.Portal.Controllers.api;

/// <summary>
/// AI Assistant — Phase 1 (placeholder with hardcoded responses).
/// Phase 2 will integrate with an LLM provider (Claude, OpenAI, etc.).
/// </summary>
[Route("api/ai")]
public class AiController(ILogger<AiController> logger) : ApiControllerBase
{
    private static readonly Dictionary<string, AiResponseTemplate> _contextTemplates = new(StringComparer.OrdinalIgnoreCase)
    {
        ["retail"] = new AiResponseTemplate(
            "Great question about retail! Here are some strategies that work well for small retail businesses.",
            ["Focus on customer experience over price", "Build a loyalty program", "Leverage social media for local awareness", "Partner with complementary local businesses"],
            ["Audit your current customer touchpoints", "Set up a simple loyalty rewards program", "Create a content calendar for social media", "Identify 3 local businesses to collaborate with"]),

        ["restaurant"] = new AiResponseTemplate(
            "For restaurants, building community and repeat customers is key to long-term success.",
            ["Engage with food bloggers and local influencers", "Offer a VIP early access program", "Share behind-the-scenes content", "Respond to every review — positive and negative"],
            ["Claim your Google Business Profile", "Create a weekly email newsletter for regulars", "Set up an online reservation system", "Develop a signature dish or experience worth talking about"]),

        ["services"] = new AiResponseTemplate(
            "Service businesses thrive on trust and referrals. Here are proven strategies.",
            ["Ask for referrals systematically after successful projects", "Showcase testimonials prominently", "Offer a satisfaction guarantee", "Build partnerships with non-competing service providers"],
            ["Create a referral incentive program", "Collect and publish 5 new testimonials this month", "Define your unique value proposition in one sentence", "Join 2 local business networking groups"]),

        ["ecommerce"] = new AiResponseTemplate(
            "For e-commerce, conversion and retention are your biggest levers.",
            ["Optimize your product pages with real photos and reviews", "Reduce cart abandonment with follow-up emails", "Offer free shipping thresholds to increase average order value", "Use retargeting ads for past visitors"],
            ["Install a cart abandonment email sequence", "A/B test your top 3 product page headlines", "Add a customer reviews section to each product", "Set a free shipping threshold 15-20% above your average order value"]),
    };

    private static readonly AiResponseTemplate _defaultTemplate = new(
        "Here are some general strategies that work well for small businesses looking to grow.",
        ["Build genuine relationships with your customers", "Focus on solving one problem exceptionally well", "Use data to understand what's working", "Invest in your online presence"],
        ["Define your ideal customer in detail", "Ask your best customers what they love most about your business", "Set one specific growth goal for this quarter", "Review your online reviews and respond to all of them"]);

    /// <summary>POST /api/ai/chat — get AI business advice (authenticated)</summary>
    [HttpPost("chat")]
    public IActionResult Chat([FromBody] AiChatRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { message = "Message is required." });

        logger.LogInformation("AI chat request from user {UserId}, context: {Context}", userId, request.Context ?? "general");

        var template = (!string.IsNullOrWhiteSpace(request.Context) && _contextTemplates.TryGetValue(request.Context, out var ctx))
            ? ctx
            : _defaultTemplate;

        // Build a contextual response that acknowledges the user's message
        var userMessage = request.Message.Trim();
        var response = $"{template.BaseResponse}\n\nRegarding your question — \"{(userMessage.Length > 100 ? userMessage[..100] + "..." : userMessage)}\" — the suggestions above are a great starting point. " +
                       $"Feel free to ask follow-up questions to dive deeper into any of these areas.";

        return Ok(new AiChatResponse
        {
            Response = response,
            Suggestions = template.Suggestions,
            ActionItems = template.ActionItems
        });
    }
}

public class AiChatRequest
{
    public string Message { get; set; } = string.Empty;
    /// <summary>Optional business context: retail, restaurant, services, ecommerce</summary>
    public string? Context { get; set; }
}

public class AiChatResponse
{
    public string Response { get; set; } = string.Empty;
    public List<string> Suggestions { get; set; } = [];
    public List<string> ActionItems { get; set; } = [];
}

internal sealed record AiResponseTemplate(string BaseResponse, List<string> Suggestions, List<string> ActionItems);
