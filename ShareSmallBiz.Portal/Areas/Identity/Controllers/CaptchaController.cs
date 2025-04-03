using System.Drawing;
using System.Drawing.Imaging;

namespace ShareSmallBiz.Portal.Areas.Identity.Controllers;

[Area("Identity")]
public class CaptchaController : Controller
{
    [HttpGet]
    [Route("Identity/Captcha/Generate")]
    public ActionResult GenerateCaptcha()
    {
        // Generate a simple math problem.
        Random rnd = new Random();
        int a = rnd.Next(1, 10);
        int b = rnd.Next(1, 10);
        int answer = a + b;

        // Store the answer in the session for later verification.
        HttpContext.Session.SetInt32("CaptchaAnswer", answer);

        // Prepare the captcha text.
        string captchaText = $"{a} + {b} = ?";

        // Create the image.
        using (Bitmap bitmap = new Bitmap(200, 50))
        using (Graphics g = Graphics.FromImage(bitmap))
        {
            g.Clear(System.Drawing.Color.White);

            // Draw the captcha text.
            using (Font font = new Font("Arial", 20, FontStyle.Bold))
            {
                g.DrawString(captchaText, font, Brushes.Black, new System.Drawing.PointF(10, 10));
            }

            // Add some random noise lines.
            for (int i = 0; i < 5; i++)
            {
                int x1 = rnd.Next(0, bitmap.Width);
                int y1 = rnd.Next(0, bitmap.Height);
                int x2 = rnd.Next(0, bitmap.Width);
                int y2 = rnd.Next(0, bitmap.Height);
                g.DrawLine(Pens.Gray, x1, y1, x2, y2);
            }

            // Convert the image to a byte array.
            using (MemoryStream ms = new MemoryStream())
            {
                bitmap.Save(ms, ImageFormat.Png);
                return File(ms.ToArray(), "image/png");
            }
        }
    }
}
