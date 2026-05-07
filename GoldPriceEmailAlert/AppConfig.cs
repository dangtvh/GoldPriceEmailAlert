using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GoldPriceEmailAlert
{
    public class AppConfig
    {
        public decimal MaxPrice { get; set; } = 3500m;
        public decimal MinPrice { get; set; } = 3000m;
        public string RecipientEmail { get; set; } = "";
        public string SenderEmail { get; set; } = "";
        public string SenderPassword { get; set; } = "";
        public int IntervalMinutes { get; set; } = 60;
        public string ApiKey { get; set; } = "";

        public void Display()
        {
            Console.WriteLine($"  - Giá MAX cảnh báo : {MaxPrice:N0} USD/oz");
            Console.WriteLine($"  - Giá MIN cảnh báo : {MinPrice:N0} USD/oz");
            Console.WriteLine($"  - Kiểm tra mỗi    : {IntervalMinutes} phút");
            Console.WriteLine($"  - Email gửi       : {SenderEmail}");
            Console.WriteLine($"  - Email nhận      : {RecipientEmail}");
            Console.WriteLine($"  - GoldAPI Key     : {(string.IsNullOrEmpty(ApiKey) ? "(chưa đặt)" : ApiKey[..Math.Min(8, ApiKey.Length)] + "...")}");
        }
    }

    public static class ConfigManager
    {
        private static readonly string ConfigFile = "config.json";

        public static AppConfig LoadOrCreate()
        {
            if (File.Exists(ConfigFile))
            {
                try
                {
                    string json = File.ReadAllText(ConfigFile);
                    return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                }
                catch
                {
                    Console.WriteLine("⚠️  Không đọc được file cấu hình. Dùng cấu hình mặc định.");
                }
            }
            return new AppConfig();
        }

        public static void Save(AppConfig config)
        {
            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigFile, json);
            Console.WriteLine(" Đã lưu cấu hình vào config.json\n");
        }

        public static AppConfig SetupInteractive()
        {
            var cfg = LoadOrCreate();

            Console.WriteLine("\n--- CẤU HÌNH ỨNG DỤNG ---");

            cfg.MaxPrice = ReadDecimal($"Giá MAX cảnh báo (USD/oz) [{cfg.MaxPrice}]: ", cfg.MaxPrice);
            cfg.MinPrice = ReadDecimal($"Giá MIN cảnh báo (USD/oz) [{cfg.MinPrice}]: ", cfg.MinPrice);
            cfg.IntervalMinutes = ReadInt($"Kiểm tra mỗi bao nhiêu phút [{cfg.IntervalMinutes}]: ", cfg.IntervalMinutes);

            Console.Write($"Email người gửi (Gmail) [{cfg.SenderEmail}]: ");
            string? input = Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(input)) cfg.SenderEmail = input;

            Console.Write($"App Password Gmail [{(string.IsNullOrEmpty(cfg.SenderPassword) ? "chưa đặt" : "****")}]: ");
            string? pwd = Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(pwd)) cfg.SenderPassword = pwd;

            Console.Write($"Email nhận cảnh báo [{cfg.RecipientEmail}]: ");
            string? rcpt = Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(rcpt)) cfg.RecipientEmail = rcpt;

            Console.WriteLine("\n--- API Key (goldapi.io - đăng ký miễn phí tại https://www.goldapi.io) ---");
            Console.Write($"GoldAPI Key [{(string.IsNullOrEmpty(cfg.ApiKey) ? "chưa đặt" : cfg.ApiKey[..Math.Min(8, cfg.ApiKey.Length)] + "...")}]: ");
            string? key = Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(key)) cfg.ApiKey = key;

            return cfg;
        }

        private static decimal ReadDecimal(string prompt, decimal defaultVal)
        {
            Console.Write(prompt);
            string? s = Console.ReadLine()?.Trim();
            return decimal.TryParse(s, out decimal v) ? v : defaultVal;
        }

        private static int ReadInt(string prompt, int defaultVal)
        {
            Console.Write(prompt);
            string? s = Console.ReadLine()?.Trim();
            return int.TryParse(s, out int v) ? v : defaultVal;
        }
    }
}

