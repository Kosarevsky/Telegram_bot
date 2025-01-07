using BezKolejki_bot.Interfaces;
using Microsoft.Extensions.Logging;
using Services.Interfaces;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System.Net;
using System.Net.Http.Json;
using Tesseract;
using SixLabors.ImageSharp.Processing;
using Newtonsoft.Json;

namespace BezKolejki_bot.Services
{
    public class MoskwaKpPostRequestProcessor : ISiteProcessor
    {
        private readonly ILogger<MoskwaKpPostRequestProcessor> _logger;
        private readonly IHttpClientFactory _httpClient;
        private readonly IBezKolejkiService _bezKolejkiService;

        public MoskwaKpPostRequestProcessor(ILogger<MoskwaKpPostRequestProcessor> logger, IHttpClientFactory httpClientFactory, IBezKolejkiService bezKolejkiService)
        {
            _logger = logger;
            _httpClient = httpClientFactory;
            _bezKolejkiService = bezKolejkiService;
        }
        record Error(string Message);

        public async Task ProcessSiteAsync(string url, string code)
        {
            var countByActiveUsers = await _bezKolejkiService.GetCountActiveUsersByCode(code);

            if (countByActiveUsers <= 0)
            {
                _logger.LogInformation($"{code} count subscribers = 0. skipping....");
                return;
            }

            _logger.LogInformation($"{code} count subscribers has {countByActiveUsers} {_bezKolejkiService.TruncateText(url, 40)}");
            bool dataSaved = false;

            int attempts = 0;
            int maxRetries = 5;
            FirstPostRequestModel? captchaResponse = null;
            SecondPostResponseModel? secondRequest = null;


            while (attempts < maxRetries)
            {
                captchaResponse = await FirstPostRequest(url);
                if (captchaResponse != null)
                {
                    SaveImageToFile("c:\\1", captchaResponse.id, captchaResponse.image, captchaResponse.kod ?? string.Empty);


                    if (!string.IsNullOrEmpty(captchaResponse.kod) && captchaResponse.kod.Length == 4)
                    { 
                        var payloadSprawdz = new { kod = captchaResponse.kod, token = captchaResponse.id };
                    
                        secondRequest = await SendPostRequest<SecondPostResponseModel>("https://api.e-konsulat.gov.pl/api/u-captcha/sprawdz", payloadSprawdz);

                        if (secondRequest != null && secondRequest.ok)
                        {
                            _logger.LogInformation("Successfully processed captcha and completed second request.");
                            break; 
                        }
                        else
                        {
                            _logger.LogWarning("Captcha verification failed or second request response is not OK. Retrying...");
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Failed to retrieve or recognize captcha. Retrying...");
                    }
                }

                attempts++;
                if (attempts < maxRetries)
                {
                    _logger.LogInformation($"Retrying captcha process... Attempt {attempts + 1} of {maxRetries}");
                    await Task.Delay(500);  
                }
            }


            if (secondRequest == null || !secondRequest.ok)
            {
                _logger.LogError("Failed to process captcha after maximum retries.");
                return;
            }


            var payloadThird = new { token = secondRequest.token };
            var thirdResponse = await SendPostRequest<ThirdPostResponseModel>(url, payloadThird);
            SaveImageToFile("c:\\1\\ok", captchaResponse.id, captchaResponse.image, captchaResponse.kod ?? string.Empty);


            var dates = new HashSet<string>();

            if (thirdResponse != null && thirdResponse.listaTerminow != null)
            {
                foreach (var item in thirdResponse.listaTerminow)
                {
                    dates.Add(item.data);
                    _logger.LogWarning($"{code}{thirdResponse.token} {item.idTerminu} {item.data} {item.godzina} {item}");
                }
            }

            await _bezKolejkiService.ProcessingDate(dataSaved, dates.ToList(), code);

        }

        public async Task<FirstPostRequestModel?> FirstPostRequest(string url)
        {
            var payLoad = new { imageWidth = 400, imageHeight = 200 };
            string recognizedText = string.Empty;
            var client = _httpClient.CreateClient();
            try
            {
                var response = await client.PostAsJsonAsync("https://api.e-konsulat.gov.pl/api/u-captcha/generuj", payLoad);
                await Task.Delay(1000);

                if (response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound)
                {
                    Error? error = await response.Content.ReadFromJsonAsync<Error>();
                    _logger.LogWarning($"{response.StatusCode}");
                    _logger.LogWarning(error?.Message);
                }
                else
                {
                    var content = await response.Content.ReadFromJsonAsync<FirstPostRequestModel>();
                    if (content != null)
                    {
                        byte[] imageBytes = Convert.FromBase64String(content.image);
                        using (var ms = new MemoryStream(imageBytes))
                        {
                            Image<Rgba32> preprocessedImage = PreprocessImage(ms);
                            recognizedText = RecognizeCaptcha(preprocessedImage, @"d:\Work\dev\Telegram_bot\tessdata");
                            _logger.LogInformation("Распознанный текст: " + recognizedText);
                            content.kod = recognizedText;
                            return content;
                        }
                    }
                    else
                    {
                        _logger.LogWarning("The response content is null");
                    }
                }
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError($"HTTP error occurred while processing URL {url}: {httpEx.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while processing POST request to {url}: {ex.Message}");
            }

            return null;
        }
        private void SaveImageToFile(string path, string id, string image, string recognizedText)
        {
            byte[] imageBytes = Convert.FromBase64String(image);
            string fileName = Path.Combine(path, $"{id} {recognizedText}.png");
            Directory.CreateDirectory(path);
            File.WriteAllBytes(fileName, imageBytes);
        }

        private async Task<T?> SendPostRequest<T>(string url, object payload) where T : class 
        {
            var client = _httpClient.CreateClient();
            try
            {
                var response = await client.PostAsJsonAsync(url, payload);
                return await ProcessHttpResponse<T>(response);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning($"An error occurred during the second POST request: {ex.Message}");
            }
            return default;
        }

        private async Task<T?> ProcessHttpResponse<T>(HttpResponseMessage response) where T : class
        {
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    return await response.Content.ReadFromJsonAsync<T>();
                }
                catch (JsonException ex)
                {
                    _logger.LogError($"Failed to deserialize response: {ex.Message}");
                }
            }
            else
            {
                var error = await response.Content.ReadFromJsonAsync<Error>();
                _logger.LogWarning($"HTTP Error {response.StatusCode}: {error?.Message}");
            }
            return null;
        }
        static string RecognizeCaptcha(Image<Rgba32> image, string tessDataPath)
        {
            using (var engine = new TesseractEngine(tessDataPath, "eng", EngineMode.Default))
            {
                engine.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789#=@+");

                using (var pix = PixConverter.ToPix(image))
                {
                    using (var page = engine.Process(pix, PageSegMode.SingleLine))
                    {
                        return page.GetText().Trim();
                    }
                }
            }
        }

        public static class PixConverter
        {
            public static Pix ToPix(Image<Rgba32> image)
            {
                using (var stream = new MemoryStream())
                {
                    image.SaveAsBmp(stream);
                    stream.Seek(0, SeekOrigin.Begin);
                    return Pix.LoadFromMemory(stream.ToArray());
                }
            }
        }

        static Image<Rgba32> PreprocessImage(Stream imageStream)
        {
            Image<Rgba32> image = Image.Load<Rgba32>(imageStream);

            image.Mutate(x =>
            {
                x.Grayscale();
                x.Contrast(1.5f);
                x.AdaptiveThreshold();  // Адаптивная бинаризация для более точного порога
                x.BinaryThreshold(0.4f);
                x.GaussianBlur(0.8f);
                x.Crop(new Rectangle(80, 40, image.Width - 80, image.Height - 40));
            });
            return image;
        }

        public class FirstPostRequestModel
        {
            public string id { get; set; }
            public int iloscZnakow { get; set; }
            public string image { get; set; }
            public string? kod { get; set; }
        }
        private class SecondPostResponseModel
        {
            public bool ok { get; set; }
            public string token { get; set; }
        }
        
        private class ThirdPostResponseModel
        {
            public List<ListaTerminow>? listaTerminow { get; set; }
            public string token { get; set; }
        }

        private class ListaTerminow
        {
            public int idTerminu { get; set; }
            public string data { get; set; }
            public string godzina { get; set; }
        }
    }
}
