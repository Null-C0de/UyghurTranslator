using System.Text;
using System.Text.RegularExpressions;

namespace UyghurTranslator;

public class TranslatorEngine : IDisposable
{
    private readonly HttpClient _httpClient;
    private bool _isTranslating;

    // AppWorlds 翻译 API
    private const string ApiUrl = "https://translate.appworlds.cn";

    private static readonly Dictionary<string, string> LocalDictionary = new()
    {
        ["你好"] = "ياخشىمۇسىز",
        ["ياخشىمۇسىز"] = "你好",
        ["再见"] = "خەيرلىك",
        ["谢谢"] = "رەھمەت",
        ["رەھمەت"] = "谢谢",
        ["是"] = "ھەئە",
        ["不是"] = "ياق",
        ["好的"] = "بولىدۇ",
        ["对不起"] = "كەچۈرۈڭ",
        ["我"] = "مەن",
        ["你"] = "سىز",
        ["他"] = "ئۇ",
    };

    public TranslatorEngine()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
    }

    public static string DetectLanguage(string text)
    {
        if (Regex.IsMatch(text, @"[\u0626-\u06C3\u0674-\u06D5]"))
            return "ug";
        if (Regex.IsMatch(text, @"[\u4e00-\u9fff]"))
            return "zh";
        return "auto";
    }

    public async Task<(string result, string newDirection)> TranslateAsync(string text, string direction, bool autoDetect = true)
    {
        if (_isTranslating || string.IsNullOrWhiteSpace(text))
            return ("", "");

        _isTranslating = true;

        try
        {
            var (sourceLang, targetLang) = direction == "汉语 → 维语" ? ("zh-CN", "ug") : ("ug", "zh-CN");
            string newDirection = direction;

            if (autoDetect)
            {
                var detected = DetectLanguage(text);
                if (detected == "zh") { sourceLang = "zh-CN"; targetLang = "ug"; newDirection = "汉语 → 维语"; }
                else if (detected == "ug") { sourceLang = "ug"; targetLang = "zh-CN"; newDirection = "维语 → 汉语"; }
            }

            if (LocalDictionary.TryGetValue(text.Trim(), out var localResult))
                return (localResult, newDirection);

            // AppWorlds 翻译 API
            try
            {
                var result = await TranslateAppWorldsAsync(text, sourceLang, targetLang);
                if (!string.IsNullOrEmpty(result))
                    return (result, newDirection);
            }
            catch (Exception ex)
            {
                return ($"翻译失败：{ex.Message}", newDirection);
            }

            return ("翻译失败", newDirection);
        }
        finally
        {
            _isTranslating = false;
        }
    }

    private async Task<string?> TranslateAppWorldsAsync(string text, string source, string target)
    {
        var url = $"{ApiUrl}?text={Uri.EscapeDataString(text)}&from={source}&to={target}";
        
        var response = await _httpClient.GetStringAsync(url);
        var json = Newtonsoft.Json.Linq.JObject.Parse(response);

        var code = (int?)json["code"];
        if (code == 200 && json["data"] != null)
        {
            return json["data"]!.ToString();
        }

        var errorMsg = json["msg"]?.ToString() ?? "未知错误";
        throw new Exception(errorMsg);
    }

    public void Dispose() => _httpClient.Dispose();
}
