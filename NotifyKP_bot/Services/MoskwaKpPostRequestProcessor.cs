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
                    SaveImageToFile("c:\\1", captchaResponse.id, captchaResponse.image, captchaResponse.kod ?? string.Empty);


                    if (!string.IsNullOrEmpty(captchaResponse.kod) && captchaResponse.kod.Length == 4)
                    {
                        var payloadSprawdz = new { kod = captchaResponse.kod, token = captchaResponse.id };

                        secondRequest = await SendPostRequest<SecondPostResponseModel>("https://api.e-konsulat.gov.pl/api/u-captcha/sprawdz", payloadSprawdz);

                        if (secondRequest.Data != null && secondRequest.Data.ok)
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

            if (secondRequest?.Data == null || !secondRequest.Data.ok)
            {
                _logger.LogError("Failed to process captcha after maximum retries.");
                return;
            }

            await Task.Delay(300);
            var payloadThird = new { token = secondRequest.Data.token };
            var thirdResponse = await SendPostRequest<ThirdPostResponseModel>(url, payloadThird);
            SaveImageToFile("c:\\1\\ok", captchaResponse?.id ?? string.Empty, captchaResponse?.image ?? string.Empty, captchaResponse?.kod ?? string.Empty);


            var dates = new HashSet<string>();

            if (thirdResponse.IsSuccess && thirdResponse?.Data?.listaTerminow != null)
            {
                foreach (var item in thirdResponse.Data.listaTerminow)
                {
                    dates.Add(item.data);
                    _logger.LogWarning($"{code}{thirdResponse.Data.token} {item.idTerminu} {item.data} {item.godzina} ");
                }
            }

            await _bezKolejkiService.ProcessingDate(dataSaved, dates.ToList(), code);

            //------4
            if (thirdResponse != null && thirdResponse.IsSuccess && thirdResponse?.Data?.listaTerminow != null)
            {
                var clients = await _clientService.GetAllAsync(u => u.Code == code && u.IsActive && !u.IsRegistered);
                if (clients != null && clients.Count > 0)
                {
                    var clientIndex = 0;
                    foreach (var item in thirdResponse.Data.listaTerminow)
                    {
                        var fourthResult = await ProcessingRezerwacje(thirdResponse.Data.token, item);
                        if (fourthResult == null) {
                            _logger.LogWarning($"{code}. ProcessingRezerwacje return null.");
                            continue;
                        }

                        var fiveResult =  await ProcessRegistration(fourthResult.bilet, clients[clientIndex]);
                        if (fiveResult == null)
                        {
                            _logger.LogWarning($"{code}. ProcessRegistration return null. Skip client");
                            continue;
                        }

                        if (fiveResult.wynik == "zapisano")
                        {
                            var pdfPath = Path.Combine("c:\\1\\M", $"{fiveResult.guid}.pdf");

                            await DownloadPdfAsync(fiveResult.guid, pdfPath);
                            var message = $"wynik: {fiveResult?.wynik}" +
                                $"guid : {fiveResult.guid}" +
                                $"kod: {fiveResult.kod}" +
                                $"numerFormularza {fiveResult.numerFormularza}";


                            await _telegramBotService.SendTextMessage(5993130676, $"description {clients[clientIndex].Email}\n{message}");
                        }
                        else
                        {
                            var message = $"{code}. {clients[clientIndex].Email} \nReg is fail: {fiveResult.wynik}";
                            await _telegramBotService.SendTextMessage(5993130676, message);
                            _logger.LogWarning(message);
                        }
                        clientIndex ++;
                        if (clientIndex > clients.Count)
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
                // Формируем URL для скачивания PDF
                var pdfUrl = $"https://api.e-konsulat.gov.pl/api/formularze/pdf-karta-polaka/{guid}";

                // Отправляем GET запрос
                var response = await _httpClient.GetAsync(pdfUrl);

                if (response.IsSuccessStatusCode)
                {
                    // Читаем JSON-ответ
                    var jsonResponse = await response.Content.ReadFromJsonAsync<PdfResponse>();

                    if (jsonResponse != null && !string.IsNullOrEmpty(jsonResponse.pdf))
                    {
                        // Декодируем Base64 в массив байтов
                        byte[] pdfBytes = Convert.FromBase64String(jsonResponse.pdf);

                        // Сохраняем массив байтов в файл
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

        // Модель для десериализации JSON-ответа
        public class PdfResponse
        {
            public string numerFormularza { get; set; }
            public string pdf { get; set; } // Base64-строка с PDF
            public bool haslo { get; set; }
        }


        private async Task<FourthRezerwacjePostResponseModel?> ProcessingRezerwacje(string token, ListaTerminow termin)
        {
            if (termin != null)
            {
                var payloadFourth = new FourthRezerwacjePostPayloadModel
                {
                    id_terminu = termin.idTerminu,
                    token = token
                };
                var fourthResponse = await SendPostRequest<FourthRezerwacjePostResponseModel>
                    ("https://api.e-konsulat.gov.pl/api/rezerwacja-wizyt-karta-polaka/rezerwacje", payloadFourth);
                if (!fourthResponse.IsSuccess)
                {
                    _logger.LogWarning($"ProcessingRezerwacje вернул ошибку: {fourthResponse.ErrorMessage}");

                }
                return fourthResponse.Data;
            }

            return null;
        }

        private async Task<FivePostDaneKartaPolakaDaneFormularzaResponseModel> ProcessRegistration(string bilet, ClientModel client)
        {
            var payloadDaneKartaPolaka = new FivePostDaneKartaPolakaPayLoadModel
            {
                bilet = bilet,
                daneFormularza = new FivePostDaneKartaPolakaDaneFormularzaPayLoadModel 
                {
                    imie1 = client.Surname.ToUpper(),
                    nazwisko1 = client.Surname.ToUpper(),
                    dataUrodzenia = client.DateOfBirth?.ToString("yyyy-MM-dd") ?? "",
                    obywatelstwo = client.Citizenship.ToUpper(),
                    obywatelstwoICAO = "RUS",
                    plec = (bool)client.Sex ? "M" : "K",
                    numerPaszportu = client.PassportNumber,
                    numerIdentyfikacyjny = client.PassportIdNumber,
                    ulica = client.Street,
                    nrDomu = client.HouseNumber,
                    kodPocztowy = client.ZipCode,
                    miejscowosc = client.City,
                    telefon = client.PhoneNumberPrefix+client.PhoneNumber,
                    email = client.Email.ToLower(),
                    opisSprawy = "Karta Polaka"
                }
            };
            var fiveResponse = await SendPostRequest<FivePostDaneKartaPolakaDaneFormularzaResponseModel>
                 ("https://api.e-konsulat.gov.pl/api/formularze/dane-karta-polaka", payloadDaneKartaPolaka);
            if (!fiveResponse.IsSuccess)
            {
                _logger.LogWarning($"ProcessRegistration return error: {fiveResponse.ErrorMessage}");
            }

            return fiveResponse.Data;
        }
        private async Task LearningML(string fullPatch, string directoryPatch)
        {
            var mlContext = new MLContext();

            var data = mlContext.Data.LoadFromTextFile<CaptchaData>(
                path: fullPatch,
                separatorChar: ',',
                hasHeader: true
            );


            // Проверяем, что данные загружены корректно
            var enumerableData = mlContext.Data.CreateEnumerable<CaptchaData>(data, reuseRowObject: false).Take(5);
            foreach (var item in enumerableData)
            {
                Console.WriteLine($"Label: {item.Label}, Features: {string.Join(", ", item.ImagePath)}");
            }

            // Data transformation pipeline
            var pipeline = mlContext.Transforms.Conversion.MapValueToKey("Label")
                .Append(mlContext.Transforms.LoadImages(outputColumnName: "Image", imageFolder: directoryPatch, inputColumnName: "ImagePath"))
                .Append(mlContext.Transforms.ResizeImages(outputColumnName: "Image", imageWidth: 200, imageHeight: 100))
                .Append(mlContext.Transforms.ExtractPixels(outputColumnName: "Image"))
                .Append(mlContext.MulticlassClassification.Trainers.LbfgsMaximumEntropy(labelColumnName: "Label", featureColumnName: "Image"))
                .Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));


            // Train model
            var model = pipeline.Fit(data);

            string modelPath = Path.Combine(directoryPatch, "CaptchaModel.zip");
            await Task.Run(() => mlContext.Model.Save(model, data.Schema, modelPath));

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
            string fileName = Path.Combine(path, $"{id}_{recognizedText}.png");
            Directory.CreateDirectory(path);
            File.WriteAllBytes(fileName, imageBytes);
        }

        private async Task<ApiResult<T>> SendPostRequest<T>(string url, object payload) where T : class
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(url, payload);
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
                // Ленивая загрузка модели
                if (!_isModelLoaded)
                {
                    LoadModel();
                }

                // Преобразование изображения в формат, подходящий для модели
                var preprocessedImage = PreprocessImageML(image);

                // Создание временного файла для изображения
                string tempImagePath = Path.GetTempFileName();
                preprocessedImage.Save(tempImagePath);

                // Создание входных данных для модели
                var input = new CaptchaData { ImagePath = tempImagePath };
                var inputData = _mlContext.Data.LoadFromEnumerable(new[] { input });

                // Применение модели для предсказания
                var predictions = _model.Transform(inputData);
                var predictedLabels = _mlContext.Data.CreateEnumerable<CaptchaPrediction>(predictions, reuseRowObject: false);

                // Удаление временного файла
                File.Delete(tempImagePath);

                // Возврат распознанного текста
                return predictedLabels.FirstOrDefault()?.PredictedLabel ?? string.Empty;
            }
            catch (Exception ex)
            {
                // Логирование ошибки
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
