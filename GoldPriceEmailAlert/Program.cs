using System;
using System.Net.Http;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.IO;

namespace GoldPriceEmailAlert
{
    internal class Program
    {
        static async Task Main (string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("===========================================");
            Console.WriteLine("   ỨNG DỤNG CẢNH BÁO GIÁ VÀNG QUA EMAIL  ");
            Console.WriteLine("===========================================\n");

            // Load hoặc tạo cấu hình
            AppConfig config = ConfigManager.LoadOrCreate();

            Console.WriteLine("Cấu hình hiện tại:");
            config.Display();

            Console.Write("\nBạn có muốn thay đổi cấu hình không? (y/n): ");
            if (Console.ReadLine()?.Trim().ToLower() == "y")
            {
                config = ConfigManager.SetupInteractive();
                ConfigManager.Save(config);
            }

            Console.WriteLine("\n✅ Ứng dụng bắt đầu chạy...");
            Console.WriteLine($"   Kiểm tra giá vàng mỗi {config.IntervalMinutes} phút");
            Console.WriteLine($"   Ngưỡng MAX: {config.MaxPrice:N0} USD/oz");
            Console.WriteLine($"   Ngưỡng MIN: {config.MinPrice:N0} USD/oz");
            Console.WriteLine($"   Email nhận cảnh báo: {config.RecipientEmail}");
            Console.WriteLine("\nNhấn Ctrl+C để dừng chương trình.\n");

            var alertService = new GoldAlertService(config);

            // Chạy ngay lần đầu
            await alertService.CheckAndAlertAsync();

            // Lặp định kỳ
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
                Console.WriteLine("\n Đang dừng ứng dụng...");
            };

            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromMinutes(config.IntervalMinutes), cts.Token);
                    if (!cts.Token.IsCancellationRequested)
                        await alertService.CheckAndAlertAsync();
                }
            }
            catch (TaskCanceledException) { }

            Console.WriteLine(" Ứng dụng đã dừng.");
        }
    }
}
    

