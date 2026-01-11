namespace CustosAC.Core.Configuration;

/// <summary>
/// External resource settings
/// </summary>
public class ExternalResourceSettings
{
    public WebsiteInfo[] WebsitesToCheck { get; set; } =
    {
        new() { Url = "https://oplata.info", Name = "Oplata.info" },
        new() { Url = "https://funpay.com", Name = "FunPay.com" }
    };

    public TelegramBotInfo[] TelegramBots { get; set; } =
    {
        new() { Username = "@MelonySolutionBot", Name = "Melony Solution Bot" },
        new() { Username = "@UndeadSellerBot", Name = "Undead Seller Bot" }
    };

    public string[] NetworkServices { get; set; } = { "netprofm", "NlaSvc", "Dhcp", "Dnscache" };
}

/// <summary>
/// Website information
/// </summary>
public class WebsiteInfo
{
    public string Url { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Telegram bot information
/// </summary>
public class TelegramBotInfo
{
    public string Username { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
