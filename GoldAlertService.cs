using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoldPriceEmailAlert
{
    internal class GoldAlertService
    {
        private readonly AppConfig _config;
        private readonly GoldPriceFetcher _fetcher;
        private readonly EmailSender _emailSender;

        // Tránh spam email: chỉ gửi lại sau ít nhất 30 phút cho cùng loại cảnh báo
        private DateTime _lastMaxAlertSent = DateTime.MinValue;
        private DateTime _lastMinAlertSent = DateTime.MinValue;
        private readonly TimeSpan _alertCooldown = TimeSpan.FromMinutes(30);

        public GoldAlertService(AppConfig config)
        {
            _config = config;
            _fetcher = new GoldPriceFetcher(config.ApiKey);
            _emailSender = new EmailSender(config);
        }

        public async Task CheckAndAlertAsync()
        {
            Console.WriteLine($"\n[{DateTime.Now:HH:mm:ss}] Đang kiểm tra giá vàng...");

            GoldPriceResult? result = await _fetcher.FetchAsync();

            if (result == null)
            {
                Console.WriteLine("    Không lấy được giá vàng. Thử lại sau.");
                return;
            }

            decimal price = result.Price;
            Console.WriteLine($"    Giá vàng: ${price:N2} USD/oz  |  Nguồn: {result.Source}");
            Console.WriteLine($"    Ngưỡng MAX: ${_config.MaxPrice:N2}  |  Ngưỡng MIN: ${_config.MinPrice:N2}");

            // --- Kiểm tra vượt ngưỡng MAX ---
            if (price > _config.MaxPrice)
            {
                Console.WriteLine($"    GIÁ VƯỢT NGƯỠNG MAX! ({price:N2} > {_config.MaxPrice:N2})");

                if (DateTime.Now - _lastMaxAlertSent >= _alertCooldown)
                {
                    string subject = $" CẢNH BÁO: Giá vàng vượt ngưỡng MAX - ${price:N2}/oz";
                    string body = _emailSender.BuildAlertHtml(price, _config.MaxPrice, "MAX", result.FetchedAt);

                    if (_emailSender.SendAlert(subject, body))
                        _lastMaxAlertSent = DateTime.Now;
                }
                else
                {
                    TimeSpan remaining = _alertCooldown - (DateTime.Now - _lastMaxAlertSent);
                    Console.WriteLine($"    Cooldown: còn {remaining.Minutes} phút trước khi gửi lại.");
                }
            }
            // --- Kiểm tra xuống ngưỡng MIN ---
            else if (price < _config.MinPrice)
            {
                Console.WriteLine($"    GIÁ XUỐNG DƯỚI NGƯỠNG MIN! ({price:N2} < {_config.MinPrice:N2})");

                if (DateTime.Now - _lastMinAlertSent >= _alertCooldown)
                {
                    string subject = $" CẢNH BÁO: Giá vàng xuống dưới ngưỡng MIN - ${price:N2}/oz";
                    string body = _emailSender.BuildAlertHtml(price, _config.MinPrice, "MIN", result.FetchedAt);

                    if (_emailSender.SendAlert(subject, body))
                        _lastMinAlertSent = DateTime.Now;
                }
                else
                {
                    TimeSpan remaining = _alertCooldown - (DateTime.Now - _lastMinAlertSent);
                    Console.WriteLine($"    Cooldown: còn {remaining.Minutes} phút trước khi gửi lại.");
                }
            }
            else
            {
                Console.WriteLine($"   Giá vàng trong ngưỡng bình thường.");
            }
        }
    }
}

