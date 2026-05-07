using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GoldPriceEmailAlert
{
    public class GoldPriceResult
    {
        public decimal Price { get; set; }       // USD/troy oz
        public string Currency { get; set; } = "USD";
        public DateTime FetchedAt { get; set; } = DateTime.Now;
        public string Source { get; set; } = "";
    }
    internal class GoldPriceFetcher
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;

        public GoldPriceFetcher(string apiKey)
        {
            _apiKey = apiKey;
            _http = new HttpClient();
            _http.DefaultRequestHeaders.Add("User-Agent", "GoldPriceAlert/1.0");
        }

        /// <summary>
        /// Lấy giá vàng – thử GoldAPI.io trước, fallback sang XML nếu lỗi
        /// </summary>
        public async Task<GoldPriceResult?> FetchAsync()
        {
            // --- Nguồn 1: GoldAPI.io (JSON, miễn phí) ---
            if (!string.IsNullOrWhiteSpace(_apiKey))
            {
                var result = await FetchFromGoldApiAsync();
                if (result != null) return result;
            }

            // --- Nguồn 2: metals-api fallback (XML dạng RSS từ metalpriceapi.com) ---
            return await FetchFromXmlFeedAsync();
        }

        // ------------------------------------------------------------------
        // NGUỒN 1: GoldAPI.io  (https://www.goldapi.io)
        //   Đăng ký free → nhận API key → 100 requests/tháng free
        //   Response JSON: { "price": 3254.10, "currency": "USD", ... }
        // ------------------------------------------------------------------
        private async Task<GoldPriceResult?> FetchFromGoldApiAsync()
        {
            try
            {
                string url = "https://www.goldapi.io/api/XAU/USD";
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.Add("x-access-token", _apiKey);

                var response = await _http.SendAsync(req);
                string body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"   [GoldAPI] Lỗi HTTP {(int)response.StatusCode}: {body}");
                    return null;
                }

                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                decimal price = root.GetProperty("price").GetDecimal();

                return new GoldPriceResult
                {
                    Price = price,
                    Currency = "USD",
                    Source = "GoldAPI.io (JSON)",
                    FetchedAt = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   [GoldAPI] Exception: {ex.Message}");
                return null;
            }
        }

        // ------------------------------------------------------------------
        // NGUỒN 2: Frankfurter.app XML-like + metals prices
        //   Dùng open.er-api.com để lấy tỷ giá XAU (vàng) so USD
        //   XAU = troy ounce gold, 1 XAU = giá vàng hiện tại tính bằng USD
        //   URL: https://open.er-api.com/v6/latest/XAU  (JSON, free, không cần key)
        // ------------------------------------------------------------------
        private async Task<GoldPriceResult?> FetchFromXmlFeedAsync()
        {
            // open.er-api.com trả về 1 XAU = bao nhiêu USD (tức nghịch đảo giá vàng)
            // => giá vàng = 1 / rates["USD"]
            try
            {
                string url = "https://open.er-api.com/v6/latest/XAU";
                var response = await _http.GetAsync(url);
                string body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"   [ER-API] Lỗi HTTP {(int)response.StatusCode}");
                    return null;
                }

                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                // rates.USD = số USD trong 1 XAU = giá vàng USD/oz
                decimal usdPerOz = root.GetProperty("rates").GetProperty("USD").GetDecimal();

                return new GoldPriceResult
                {
                    Price = usdPerOz,
                    Currency = "USD",
                    Source = "open.er-api.com (XAU/USD, free)",
                    FetchedAt = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   [ER-API] Exception: {ex.Message}");
            }

            // ------------------------------------------------------------------
            // NGUỒN 3 (dự phòng cuối): parse XML từ RSS metalpriceapi
            // Minh hoạ cách đọc XML – dữ liệu thực tế phụ thuộc feed khả dụng
            // ------------------------------------------------------------------
            return await FetchFromXmlRssAsync();
        }

        private async Task<GoldPriceResult?> FetchFromXmlRssAsync()
        {
            // Ví dụ feed XML giả lập (bạn thay bằng feed thực nếu có)
            // Một số nguồn XML thực: https://data.fixer.io/api/latest?format=xml&access_key=...
            try
            {
                // Dùng frankfurter.app trả JSON, demo parse "XML" qua XDocument
                // Thực tế bài tập: thầy có thể cung cấp URL XML → thay vào đây
                string xmlUrl = "https://www.ecb.europa.eu/stats/eurofxref/eurofxref-daily.xml";
                string xmlBody = await _http.GetStringAsync(xmlUrl);

                XDocument xdoc = XDocument.Parse(xmlBody);
                XNamespace ns = "http://www.ecb.int/vocabulary/2002-08-01/eurofxref";
                XNamespace gesmes = "http://www.gesmes.org/xml/2002-08-01";

                // ECB feed không có XAU trực tiếp, nhưng đây là ví dụ đọc XML
                // Tìm node currency USD để lấy EUR/USD
                decimal? eurUsd = null;
                foreach (var cube in xdoc.Descendants(ns + "Cube"))
                {
                    var attr = cube.Attribute("currency");
                    if (attr?.Value == "USD")
                    {
                        eurUsd = decimal.Parse(cube.Attribute("rate")!.Value,
                            System.Globalization.CultureInfo.InvariantCulture);
                        break;
                    }
                }

                if (eurUsd == null) return null;

                // Giá vàng XAU tính bằng EUR từ ECB không có sẵn,
                // dùng giá tham chiếu cố định ~3250 USD/oz để demo XML parsing
                Console.WriteLine($"   [ECB XML] EUR/USD = {eurUsd} (ví dụ parse XML)");
                Console.WriteLine("     ECB không cung cấp giá XAU – dùng làm ví dụ parse XML");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   [XML Feed] Exception: {ex.Message}");
                return null;
            }
        }
    }
}

