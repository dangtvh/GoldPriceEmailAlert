# GoldPriceEmailAlert

Ứng dụng console C#/.NET dùng để theo dõi giá vàng XAU/USD và gửi email cảnh báo khi giá vượt ngưỡng **MAX** hoặc xuống dưới ngưỡng **MIN**.

## Chức năng chính

- Đọc hoặc tạo cấu hình từ file `config.json`.
- Lấy giá vàng từ GoldAPI.io nếu có API key.
- Tự động fallback sang nguồn miễn phí `open.er-api.com` nếu không cấu hình GoldAPI key hoặc GoldAPI lỗi.
- Gửi email cảnh báo qua Gmail SMTP khi giá vượt ngưỡng.
- Có cơ chế cooldown 30 phút để tránh spam email cùng loại cảnh báo.
- Hỗ trợ cấu hình tương tác ngay trên terminal/console.

## Yêu cầu môi trường

- .NET SDK 8.0 trở lên.
- Tài khoản Gmail có bật xác thực 2 bước nếu muốn gửi email thật.
- Gmail App Password, không dùng mật khẩu Gmail thông thường.
- GoldAPI key là tùy chọn. Nếu không có, app vẫn thử dùng nguồn fallback miễn phí.

Kiểm tra .NET SDK:

```bash
dotnet --version
```

## Cấu trúc project

```text
GoldPriceEmailAlert/
├── GoldPriceEmailAlert.sln
├── README.md
└── GoldPriceEmailAlert/
    ├── AppConfig.cs          # Model cấu hình và đọc/ghi config.json
    ├── EmailSender.cs        # Tạo nội dung HTML và gửi email SMTP
    ├── GoldAlertService.cs   # Kiểm tra ngưỡng MAX/MIN và cooldown gửi mail
    ├── GoldPriceFetcher.cs   # Lấy giá vàng từ API chính/fallback
    ├── Program.cs            # Entry point và vòng lặp chạy định kỳ
    └── GoldPriceEmailAlert.csproj
```

## Cách chạy nhanh

### 1. Restore project

```bash
dotnet restore
```

### 2. Build project

```bash
dotnet build
```

### 3. Chạy ứng dụng

```bash
dotnet run --project GoldPriceEmailAlert/GoldPriceEmailAlert.csproj
```

Khi app hỏi:

```text
Bạn có muốn thay đổi cấu hình không? (y/n):
```

Nhập `y` để cấu hình lần đầu.

## File cấu hình `config.json`

Sau khi cấu hình, app sẽ lưu thông tin vào `config.json` tại thư mục chạy ứng dụng.

Ví dụ:

```json
{
  "MaxPrice": 3500,
  "MinPrice": 3000,
  "RecipientEmail": "receiver@example.com",
  "SenderEmail": "sender@gmail.com",
  "SenderPassword": "gmail-app-password",
  "IntervalMinutes": 60,
  "ApiKey": "goldapi-key-neu-co"
}
```

Ý nghĩa các trường:

| Trường | Ý nghĩa |
| --- | --- |
| `MaxPrice` | Gửi cảnh báo khi giá vàng lớn hơn ngưỡng này. |
| `MinPrice` | Gửi cảnh báo khi giá vàng nhỏ hơn ngưỡng này. |
| `RecipientEmail` | Email nhận cảnh báo. |
| `SenderEmail` | Gmail dùng để gửi cảnh báo. |
| `SenderPassword` | Gmail App Password. Không dùng mật khẩu Gmail thường. |
| `IntervalMinutes` | Khoảng thời gian giữa 2 lần kiểm tra giá. |
| `ApiKey` | GoldAPI.io API key. Có thể để trống để dùng fallback. |

> Lưu ý bảo mật: `config.json` có thể chứa Gmail App Password và API key. Không commit file này lên GitHub hoặc gửi cho người khác.

## Cách tạo Gmail App Password

1. Vào trang quản lý tài khoản Google.
2. Bật xác thực 2 bước cho tài khoản Gmail.
3. Tạo **App Password** cho ứng dụng gửi mail.
4. Dùng App Password đó nhập vào trường `SenderPassword`.

Nếu nhập mật khẩu Gmail thông thường, Gmail SMTP thường sẽ từ chối đăng nhập.

## Luồng hoạt động

1. `Program.cs` load cấu hình bằng `ConfigManager.LoadOrCreate()`.
2. Người dùng có thể chỉnh cấu hình bằng chế độ interactive.
3. `GoldAlertService` gọi `GoldPriceFetcher` để lấy giá vàng.
4. App so sánh giá hiện tại với `MaxPrice` và `MinPrice`.
5. Nếu vượt ngưỡng, `EmailSender` tạo nội dung HTML và gửi email.
6. App chờ theo `IntervalMinutes` rồi kiểm tra lại.
7. Nhấn `Ctrl + C` để dừng chương trình.

## Test thủ công

### Test build

```bash
dotnet build
```

Kỳ vọng: build thành công, không còn warning analyzer liên quan đến:

- `CA1869`: cache và tái sử dụng `JsonSerializerOptions`.
- `CA1822`: method tạo HTML không dùng state instance đã được chuyển thành static.
- `CS8618`: các field non-nullable đã có initializer hoặc guard trong constructor.
- `IDE0044`: trạng thái cooldown đã được quản lý qua readonly container.

### Test chạy app không gửi email

Để trống các trường email, chạy:

```bash
dotnet run --project GoldPriceEmailAlert/GoldPriceEmailAlert.csproj
```

Kỳ vọng:

- App vẫn lấy giá vàng.
- Nếu vượt ngưỡng nhưng thiếu cấu hình email, app bỏ qua gửi mail và in thông báo cấu hình email chưa đầy đủ.

### Test cảnh báo MAX

Cấu hình tạm thời:

```json
{
  "MaxPrice": 1,
  "MinPrice": 0,
  "IntervalMinutes": 1
}
```

Kỳ vọng: giá vàng hiện tại lớn hơn `MaxPrice`, app đi vào nhánh cảnh báo MAX.

### Test cảnh báo MIN

Cấu hình tạm thời:

```json
{
  "MaxPrice": 999999,
  "MinPrice": 999998,
  "IntervalMinutes": 1
}
```

Kỳ vọng: giá vàng hiện tại nhỏ hơn `MinPrice`, app đi vào nhánh cảnh báo MIN.

### Test giá trong ngưỡng bình thường

Cấu hình tạm thời:

```json
{
  "MaxPrice": 999999,
  "MinPrice": 1,
  "IntervalMinutes": 1
}
```

Kỳ vọng: app thông báo giá vàng trong ngưỡng bình thường và không gửi email.

### Test cooldown chống spam

1. Cấu hình để app luôn vượt MAX hoặc luôn dưới MIN.
2. Đặt `IntervalMinutes` là `1`.
3. Chạy app và chờ kiểm tra nhiều lần.

Kỳ vọng: sau lần gửi mail đầu tiên, app không gửi lại cùng loại cảnh báo trong vòng 30 phút.

## Troubleshooting

### `dotnet` không được nhận diện

Cài .NET SDK 8.0 rồi mở terminal mới:

```bash
dotnet --version
```

### Không gửi được Gmail

Kiểm tra lại:

- Đã bật xác thực 2 bước chưa.
- Đang dùng Gmail App Password chưa.
- `SenderEmail`, `SenderPassword`, `RecipientEmail` có bị trống không.
- Mạng có chặn cổng SMTP `587` không.

### Không lấy được giá vàng

Kiểm tra lại:

- Máy có internet không.
- GoldAPI key có đúng không nếu đang dùng GoldAPI.
- Nếu không dùng GoldAPI, nguồn fallback miễn phí có đang phản hồi không.

## Gợi ý cải thiện tiếp theo

- Dùng biến môi trường hoặc .NET User Secrets thay cho việc lưu Gmail App Password/API key trong `config.json`.
- Thêm validate cấu hình: `MaxPrice > MinPrice`, `IntervalMinutes > 0`.
- Thêm project unit test với xUnit hoặc NUnit.
- Đổi gửi email sang async bằng `SendMailAsync`.
- Tách interface cho `GoldPriceFetcher` và `EmailSender` để dễ mock khi test.
