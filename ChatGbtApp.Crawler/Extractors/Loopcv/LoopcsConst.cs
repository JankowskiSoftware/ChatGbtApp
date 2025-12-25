namespace ChatGgtApp.Crawler.Extractors.Loopcv;

public static  class LoopcvConst
{
    public const string LoginUrl = "https://app.loopcv.pro/login";
    public static readonly string Email = Environment.GetEnvironmentVariable("LOOP_EMAIL");
    public static readonly string  Password = Environment.GetEnvironmentVariable("LOOP_PASS");
}