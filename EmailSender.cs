using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace GoldPriceEmailAlert
{
    internal class EmailSender
    {
        private readonly AppConfig _config;

        public EmailSender(AppConfig config)
        {
            _config = config;
        }

        /// <summary>
        /// Gửi email cảnh báo qua Gmail SMTP (SSL port 587)
        /// Tham khảo: https://stackoverflow.com/questions/32260/sending-email-in-net-through-gmail
        /// </summary>
        public bool SendAlert(string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(_config.SenderEmail) ||
                string.IsNullOrWhiteSpace(_config.SenderPassword) ||
                string.IsNullOrWhiteSpace(_config.RecipientEmail))
            {
                Console.WriteLine("     Email chưa được cấu hình đầy đủ. Bỏ qua gửi mail.");
                return false;
            }

            try
            {
                using var client = new SmtpClient("smtp.gmail.com", 587)
                {
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(_config.SenderEmail, _config.SenderPassword)
                };

                using var message = new MailMessage(
                    from: _config.SenderEmail,
                    to: _config.RecipientEmail,
                    subject: subject,
                    body: body)
                {
                    IsBodyHtml = true
                };

                client.Send(message);
                Console.WriteLine($"    Đã gửi email đến {_config.RecipientEmail}");
                return true;
            }
            catch (SmtpException ex)
            {
                Console.WriteLine($"    Lỗi SMTP: {ex.Message}");
                Console.WriteLine("    Gợi ý: Đảm bảo bạn dùng 'App Password' (không phải mật khẩu Gmail thường).");
                Console.WriteLine("      Bật 2FA → https://myaccount.google.com/security → App Passwords");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    Lỗi gửi email: {ex.Message}");
                return false;
            }
        }

        public string BuildAlertHtml(decimal currentPrice, decimal threshold, string alertType, DateTime fetchedAt)
        {
            string color = alertType == "MAX" ? "#e74c3c" : "#3498db";
            string arrow = alertType == "MAX" ? "↑" : "↓";

            return $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset='UTF-8'>
  <style>
    body {{ font-family: Arial, sans-serif; background: #f5f5f5; padding: 20px; }}
    .card {{ background: white; border-radius: 8px; padding: 24px; max-width: 480px;
             margin: auto; box-shadow: 0 2px 8px rgba(0,0,0,0.1); }}
    h2 {{ color: {color}; }}
    .price {{ font-size: 2em; font-weight: bold; color: {color}; }}
    .info {{ color: #555; margin-top: 12px; font-size: 0.95em; }}
    .footer {{ margin-top: 20px; font-size: 0.8em; color: #aaa; }}
  </style>
</head>
<body>
  <div class='card'>
    <h2>{arrow} CẢNH BÁO GIÁ VÀNG VƯỢT NGƯỠNG {alertType}</h2>
    <p>Giá vàng hiện tại:</p>
    <div class='price'>${currentPrice:N2} USD/oz</div>
    <div class='info'>
      <p> Ngưỡng {alertType}: <strong>${threshold:N2} USD/oz</strong></p>
      <p> Thời điểm: {fetchedAt:dd/MM/yyyy HH:mm:ss}</p>
      <p> Chênh lệch: <strong>{Math.Abs(currentPrice - threshold):N2} USD/oz</strong>
         ({(alertType == "MAX" ? "+" : "-")}{Math.Abs((currentPrice - threshold) / threshold * 100):N2}%)</p>
    </div>
    <div class='footer'>
      Email tự động từ ứng dụng Cảnh Báo Giá Vàng &mdash; Gold Price Alert System
    </div>
  </div>
</body>
</html>";
        }
    }

}
