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
using Microsoft.ML.Data;
using Microsoft.ML;
using Services.Models;
using BezKolejki_bot.Models;


namespace BezKolejki_bot.Services
{
    public class MoskwaKpPostRequestProcessor : ISiteProcessor
    {
        private readonly ILogger<MoskwaKpPostRequestProcessor> _logger;
        private readonly HttpClient _httpClient;
        private readonly IBezKolejkiService _bezKolejkiService;
        private readonly IClientService _clientService;
        private readonly ITelegramBotService _telegramBotService;

        private readonly MLContext _mlContext;
        private ITransformer _model;
        private bool _isModelLoaded = false;
        private readonly string _modelPath;
        private readonly string _folderPath;

        public MoskwaKpPostRequestProcessor(ILogger<MoskwaKpPostRequestProcessor> logger, IHttpClientFactory httpClientFactory, IBezKolejkiService bezKolejkiService, IClientService clientService, ITelegramBotService telegramBotService)
        {
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(60);
            _bezKolejkiService = bezKolejkiService;
            _clientService = clientService;
            _telegramBotService = telegramBotService;

            _mlContext = new MLContext();
            _folderPath = "c:\\1\\ok2";
            _modelPath = Path.Combine(_folderPath, "CaptchaModel.zip");
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

            var fileName = "list.csv";
            var fullPatch = Path.Combine(_folderPath, fileName);

            if (!File.Exists(fullPatch))
            {
                var fileList = GenerateCSVFile(_folderPath);
                
                using (StreamWriter writer = new StreamWriter(fullPatch, false))
                {
                    foreach (var item in fileList)
                    {
                        var line = $"{item.ImagePath},{item.Label}\n";
                        await writer.WriteAsync(line);
                    }
                }

                await LearningML(fullPatch, _folderPath);
            }

            bool dataSaved = false;

            int attempts = 0;
            int maxRetries = 5;
            FirstPostRequestModel? captchaResponse = null;
            ApiResult<SecondPostResponseModel> secondRequest = null;


            while (attempts < maxRetries)
            {
                captchaResponse = await FirstPostRequest(url);
                if (captchaResponse != null)
                {
                    SaveImageToFile("c:\\1", captchaResponse.Id, captchaResponse.Image, captchaResponse.Kod ?? string.Empty);


                    if (!string.IsNullOrEmpty(captchaResponse.Kod) && captchaResponse.Kod.Length == 4)
                    {
                        var payloadSprawdz = new { kod = captchaResponse.Kod, token = captchaResponse.Id };

                        secondRequest = await SendPostRequest<SecondPostResponseModel>("https://api.e-konsulat.gov.pl/api/u-captcha/sprawdz", payloadSprawdz);

                        if (secondRequest.Data != null && secondRequest.Data.Ok)
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

            if (secondRequest?.Data == null || !secondRequest.Data.Ok)
            {
                _logger.LogError("Failed to process captcha after maximum retries.");
                return;
            }

            await Task.Delay(300);
            var payloadThird = new { token = secondRequest.Data.Token };
            var thirdResponse = await SendPostRequest<ThirdPostResponseModel>(url, payloadThird);
            SaveImageToFile("c:\\1\\ok", captchaResponse?.Id ?? string.Empty, captchaResponse?.Image ?? string.Empty, captchaResponse?.Kod ?? string.Empty);


            var dates = new HashSet<string>();

            if (thirdResponse.IsSuccess && thirdResponse?.Data?.ListaTerminow != null)
            {
                foreach (var item in thirdResponse.Data.ListaTerminow)
                {
                    dates.Add(item.Data);
                    _logger.LogWarning($"{code}{thirdResponse.Data.Token} {item.IdTerminu} {item.Data} {item.Godzina} ");
                }
            }

            await _bezKolejkiService.ProcessingDate(dataSaved, dates.ToList(), code);

            //------4
            if (thirdResponse != null && thirdResponse.IsSuccess && thirdResponse?.Data?.ListaTerminow != null)
            {
                var clients = await _clientService.GetAllAsync(u => u.Code == code && u.IsActive && !u.IsRegistered);
                if (clients != null && clients.Count > 0)
                {
                    var clientIndex = 0;
                    foreach (var item in thirdResponse.Data.ListaTerminow)
                    {
                        var fourthResult = await ProcessingRezerwacje(thirdResponse.Data.Token, item);
                        if (fourthResult?.ErrorMessage != null)
                        {
                            await _telegramBotService.SendTextMessage(5993130676, $"{code} fourthResult {clients[clientIndex].Email}\n{fourthResult?.ErrorMessage}");
                            continue;
                        }

                        if (fourthResult == null) {
                            _logger.LogWarning($"{code}. ProcessingRezerwacje return null.");
                            continue;
                        }

                        var fiveResult =  await ProcessRegistration(fourthResult.Data.Bilet, clients[clientIndex]);
                        if (fiveResult == null)
                        {
                            _logger.LogWarning($"{code}. ProcessRegistration return null. Skip client");
                            continue;
                        }

                        if (fiveResult.ErrorMessage != null)
                        {
                            await _telegramBotService.SendTextMessage(5993130676, $"{code} fiveResult {clients[clientIndex].Email}\n{fiveResult.ErrorMessage}");
                        }

                        if (fiveResult != null && fiveResult.Data != null && fiveResult.Data.Wynik == "zapisano")
                        {
                            var pdfPath = Path.Combine("c:\\1\\M", $"{fiveResult.Data.Guid}.pdf");

                            await DownloadPdfAsync(fiveResult.Data.Guid, pdfPath);
                            var message = $"{code} "+
                                $"wynik: {fiveResult?.Data.Wynik}\n" +
                                $"guid : {fiveResult?.Data.Guid}\n" +
                                $"kod: {fiveResult?.Data.Kod}\n" +
                            $"numerFormularza {fiveResult?.Data.NumerFormularza}";


                            clients[clientIndex].Result = message;
                            clients[clientIndex].DateRegistration = DateTime.Now;
                            clients[clientIndex].IsRegistered = true;
                            clients[clientIndex].IsActive = false;
                            await _telegramBotService.SendTextMessage(5993130676, $"{clients[clientIndex].Email}\n{message}");
                            await _clientService.SaveAsync(clients[clientIndex]);
                        }
                        else
                        {
                            var message = $"{code}. {clients[clientIndex].Email} \nReg is fail: {fiveResult?.Data?.Wynik}";
                            await _telegramBotService.SendTextMessage(5993130676, message);
                            _logger.LogWarning(message);
                        }
                        clientIndex ++;
                        if (clientIndex >= clients.Count)
                        {
                            break;
                        }
                    }
                }
            }

            //------end 4

        }

        private async Task DownloadPdfAsync(string guid, string outputPath)
        {
            try
            {
                var pdfUrl = $"https://api.e-konsulat.gov.pl/api/formularze/pdf-karta-polaka/{guid}";

                var response = await _httpClient.GetAsync(pdfUrl);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadFromJsonAsync<PdfResponse>();

                    if (jsonResponse != null && !string.IsNullOrEmpty(jsonResponse.Pdf))
                    {
                        byte[] pdfBytes = Convert.FromBase64String(jsonResponse.Pdf);

                        await File.WriteAllBytesAsync(outputPath, pdfBytes);

                        _logger.LogInformation($"PDF успешно сохранен по пути: {outputPath}");
                    }
                    else
                    {
                        _logger.LogWarning("Поле 'pdf' в ответе пустое. PDF-файл недоступен.");
                    }
                }
                else
                {
                    _logger.LogError($"Ошибка при выполнении GET-запроса: {response.StatusCode}");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"Ошибка HTTP при скачивании PDF: {ex.Message}");
            }
            catch (FormatException ex)
            {
                _logger.LogError($"Ошибка декодирования Base64: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка при скачивании PDF: {ex.Message}");
            }
        }

        private async Task<ApiResult<FourthRezerwacjePostResponseModel>?> ProcessingRezerwacje(string token, ListaTerminow termin)
        {
            if (termin != null)
            {
                var payloadFourth = new FourthRezerwacjePostPayloadModel
                {
                    IdTerminu = termin.IdTerminu,
                    Token = token
                };

                var fourthResponse = await SendPostRequest<FourthRezerwacjePostResponseModel>
                    ("https://api.e-konsulat.gov.pl/api/rezerwacja-wizyt-karta-polaka/rezerwacje", payloadFourth);
                if (!fourthResponse.IsSuccess)
                {
                    _logger.LogWarning($"ProcessingRezerwacje вернул ошибку: {fourthResponse.ErrorMessage}");

                }
                return fourthResponse;
            }

            return null;
        }

        private async Task<ApiResult<FivePostDaneKartaPolakaDaneFormularzaResponseModel>> ProcessRegistration(string bilet, ClientModel client)
        {
            var payloadDaneKartaPolaka = new FivePostDaneKartaPolakaPayLoadModel
            {
                Bilet = bilet,
                DaneFormularza = new FivePostDaneKartaPolakaDaneFormularzaPayLoadModel 
                {
                    Imie1 = client.Surname.ToUpper(),
                    Nazwisko1 = client.Surname.ToUpper(),
                    DataUrodzenia = client.DateOfBirth?.ToString("yyyy-MM-dd") ?? "",
                    Obywatelstwo = client.Citizenship.ToUpper(),
                    ObywatelstwoICAO = "RUS",
                    Plec = (bool)client.Sex ? "M" : "K",
                    NumerPaszportu = client?.PassportNumber ?? "number",
                    NumerIdentyfikacyjny = client?.PassportIdNumber ?? "0",
                    Ulica = client?.Street ?? "street",
                    NrDomu = client?.HouseNumber ?? "-",
                    KodPocztowy = client?.ZipCode ?? "0",
                    Miejscowosc = client?.City ?? "sity",
                    Telefon = (client?.PhoneNumberPrefix ?? "+") + (client?.PhoneNumber ?? "00000000"),
                    Email = client?.Email.ToLower() ?? "-",
                    OpisSprawy = "Karta Polaka"
                }
            };
            var fiveResponse = await SendPostRequest<FivePostDaneKartaPolakaDaneFormularzaResponseModel>
                 ("https://api.e-konsulat.gov.pl/api/formularze/dane-karta-polaka", payloadDaneKartaPolaka);
            if (!fiveResponse.IsSuccess)
            {
                _logger.LogWarning($"ProcessRegistration return error: {fiveResponse.ErrorMessage}");
            }

            return fiveResponse;
        }
        private async Task LearningML(string fullPatch, string directoryPatch)
        {
            //var mlContext = new MLContext();

            var data = _mlContext.Data.LoadFromTextFile<CaptchaData>(
                path: fullPatch,
                separatorChar: ',',
                hasHeader: true
            );


            var enumerableData = _mlContext.Data.CreateEnumerable<CaptchaData>(data, reuseRowObject: false).Take(5);
            foreach (var item in enumerableData)
            {
                Console.WriteLine($"Label: {item.Label}, Features: {string.Join(", ", item.ImagePath)}");
            }

            var pipeline = _mlContext.Transforms.Conversion.MapValueToKey("Label")
                .Append(_mlContext.Transforms.LoadImages(outputColumnName: "Image", imageFolder: directoryPatch, inputColumnName: "ImagePath"))
                .Append(_mlContext.Transforms.ResizeImages(outputColumnName: "Image", imageWidth: 200, imageHeight: 100))
                .Append(_mlContext.Transforms.ExtractPixels(outputColumnName: "Image"))
                .Append(_mlContext.MulticlassClassification.Trainers.LbfgsMaximumEntropy(labelColumnName: "Label", featureColumnName: "Image"))
                .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));


            // Train model
            var model = pipeline.Fit(data);

            string modelPath = Path.Combine(directoryPatch, "CaptchaModel.zip");
            await Task.Run(() => _mlContext.Model.Save(model, data.Schema, modelPath));

            Console.WriteLine("Model training complete.");
        }

        public async Task<FirstPostRequestModel?> FirstPostRequest(string url)
        {
            var payLoad = new { imageWidth = 400, imageHeight = 200 };
            string recognizedText = string.Empty;
            try
            {
                var response = await _httpClient.PostAsJsonAsync("https://api.e-konsulat.gov.pl/api/u-captcha/generuj", payLoad);
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
                        byte[] imageBytes = Convert.FromBase64String(content.Image);
                        using (var ms = new MemoryStream(imageBytes))
                        {
                            Image<Rgba32> preprocessedImage = PreprocessImage(ms);
                            recognizedText = RecognizeCaptcha(preprocessedImage, @"d:\Work\dev\Telegram_bot\tessdata");
                            _logger.LogInformation("Распознанный текст: " + recognizedText);
                            content.Kod = recognizedText;
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
            string fileName = Path.Combine(path, $"{id}_{recognizedText}.png");
            Directory.CreateDirectory(path);
            File.WriteAllBytes(fileName, imageBytes);
        }

        private async Task<ApiResult<T>> SendPostRequest<T>(string url, object payload) where T : class
        {
            try
            {
                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);
                return await ProcessHttpResponse<T>(response);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning($"An error occurred during the second POST request: {ex.Message}");
                return new ApiResult<T>
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                };
            }
        }

        private async Task<ApiResult<T>> ProcessHttpResponse<T>(HttpResponseMessage response) where T : class
        {
            var result = new ApiResult<T>();
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var data = await response.Content.ReadFromJsonAsync<T>();
                    result.Data = data;
                    result.IsSuccess = true;
                }
                catch (JsonException ex)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = ($"Failed to deserialize response: {ex.Message}");
                    _logger.LogError(result.ErrorMessage);
                }
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync();
                try
                {
                    var error = JsonConvert.DeserializeObject<ApiErrorResponse>(content);
                    result.ErrorMessage = error?.reason ?? content;
                }
                catch (Exception ex)
                {
                    result.ErrorMessage = content;
                    _logger.LogWarning($"Failed to deserialize error response: {ex.Message}");

                }
                result.IsSuccess = false;
                _logger.LogWarning($"HTTP Error {response.StatusCode}: {result?.ErrorMessage}");
            }
            return result;
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
                x.AdaptiveThreshold();  
                x.BinaryThreshold(0.4f);
                x.GaussianBlur(0.8f);
                x.Crop(new Rectangle(80, 40, image.Width - 80, image.Height - 40));
            });
            return image;
        }

        private Image<Rgba32> PreprocessImageML(Image<Rgba32> image)
        {
            // Применение тех же преобразований, что и при обучении
            image.Mutate(x =>
            {
                x.Resize(200, 100); // Изменение размера до 28x28
                x.Grayscale(); // Преобразование в оттенки серого
                x.BinaryThreshold(0.5f); // Бинаризация
                x.MedianBlur(2, true);
            });

            return image;
        }

        private IEnumerable<CaptchaData> GenerateCSVFile(string folderPath)
        {
            var data = new List<CaptchaData>
            {
                new CaptchaData { ImagePath = "ImagePath",  Label = "CaptchaText" }
            };

            var files = Directory.GetFiles(folderPath, "*.png");
            foreach (var file in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                var parts = fileName.Split('_');
                if (parts.Length == 2) { 
                    var captcha = parts[1];
                    data.Add(new CaptchaData
                    {
                        ImagePath = file,
                        Label = captcha,
                    });
                }
            }
            return data;
        }


        public string RecognizeCaptchaML(Image<Rgba32> image)
        {
            try
            {
                if (!_isModelLoaded)
                {
                    LoadModel();
                }

                var preprocessedImage = PreprocessImageML(image);

                string tempImagePath = Path.GetTempFileName();
                preprocessedImage.Save(tempImagePath);

                var input = new CaptchaData { ImagePath = tempImagePath };
                var inputData = _mlContext.Data.LoadFromEnumerable(new[] { input });

                var predictions = _model.Transform(inputData);
                var predictedLabels = _mlContext.Data.CreateEnumerable<CaptchaPrediction>(predictions, reuseRowObject: false);

                File.Delete(tempImagePath);

                return predictedLabels.FirstOrDefault()?.PredictedLabel ?? string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error recognizing captcha: {ex.Message}");
                return string.Empty;
            }
        }
        private void LoadModel()
        {
            if (!File.Exists(_modelPath))
            {
                throw new FileNotFoundException($"Model file not found: {_modelPath}");
            }

            _model = _mlContext.Model.Load(_modelPath, out var modelSchema);
            _isModelLoaded = true;
        }

     
        public class CaptchaData
        {
            [LoadColumn(0)]
            public string ImagePath { get; set; } = string.Empty;
            [LoadColumn(1)]
            public string Label { get; set; } = string.Empty;
        }
        public class CaptchaPrediction
        {
            [ColumnName("PredictedLabel")]
            public string PredictedLabel { get; set; } = string.Empty;
        }

        public class ApiResult<T>
        {
            public bool IsSuccess { get; set; }      
            public T? Data { get; set; }   
            public string? ErrorMessage { get; set; }
        }
        public class ApiErrorResponse
        {
            public string reason { get; set; } = string.Empty;
        }
    }
}
