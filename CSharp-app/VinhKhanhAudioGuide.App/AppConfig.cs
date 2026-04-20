namespace VinhKhanhAudioGuide.App;

public static class AppConfig
{
    // Đổi IP này khi chạy trên điện thoại thật
    // Ví dụ: "http://192.168.1.100:5140/api/v1/"
    public const string ApiBaseUrl = "http://localhost:5140/api/v1/";

    // Để lấy IP máy tính Windows: mở CMD → gõ "ipconfig" → tìm IPv4 Address
    // Để lấy IP máy tính Mac/Linux: mở Terminal → gõ "ifconfig" → tìm inet
}
