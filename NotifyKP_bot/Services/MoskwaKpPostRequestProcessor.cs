using BezKolejki_bot.Interfaces;
using Microsoft.Extensions.Logging;
using Services.Interfaces;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System.Net.Http.Json;
using SixLabors.ImageSharp.Processing;
using Newtonsoft.Json;
using Microsoft.ML;
using Services.Models;
using BezKolejki_bot.Models;
using System.Text;


namespace BezKolejki_bot.Services
{
    public class MoskwaKpPostRequestProcessor : ISiteProcessor
    {
        private readonly ILogger<MoskwaKpPostRequestProcessor> _logger;
        private readonly HttpClient _httpClient;
        private readonly IBezKolejkiService _bezKolejkiService;
        private readonly IClientService _clientService;
        private readonly ITelegramBotService _telegramBotService;
        private readonly ICaptchaRecognitionService _captchaService;
        private readonly MLContext _mlContext;
        private ITransformer _model;
        private bool _isModelLoaded = false;
        private readonly string _modelPath;
        private readonly string _folderPath;

        public MoskwaKpPostRequestProcessor(ILogger<MoskwaKpPostRequestProcessor> logger, 
            IHttpClientFactory httpClientFactory, 
            IBezKolejkiService bezKolejkiService, 
            IClientService clientService, 
            ITelegramBotService telegramBotService,
            ICaptchaRecognitionService captchaService)
        {
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(60);
            _bezKolejkiService = bezKolejkiService;
            _clientService = clientService;
            _telegramBotService = telegramBotService;
            _captchaService = captchaService;

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
            ApiResult<FirstPostRequestModel?> captchaResponse = null;
            ApiResult<SecondPostResponseModel> secondRequest = null;


            while (attempts < maxRetries)
            {
                captchaResponse = await FirstPostRequest(url);
                if (captchaResponse != null && captchaResponse.Data != null)
                {
                    //SaveImageToFile("c:\\1", captchaResponse.Data.Id, captchaResponse.Data.Image, captchaResponse.Data.Kod ?? string.Empty);


                    if (!string.IsNullOrEmpty(captchaResponse.Data.Kod) && captchaResponse.Data.Kod.Length == 4)
                    {
                        var payloadSprawdz = new { kod = captchaResponse.Data.Kod, token = captchaResponse.Data.Id };

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
            SaveImageToFile("c:\\1\\ok", captchaResponse?.Data?.Id ?? string.Empty, captchaResponse?.Data?.Image ?? string.Empty, captchaResponse?.Data?.Kod ?? string.Empty);


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
                            await _telegramBotService.SendAdminTextMessage($"{code} fourthResult {clients[clientIndex].Email}\n{fourthResult?.ErrorMessage}");
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
                            await _telegramBotService.SendAdminTextMessage($"{code} fiveResult {clients[clientIndex].Email}\n{fiveResult.ErrorMessage}");
                        }

                        if (fiveResult != null && fiveResult.Data != null && fiveResult.Data.Wynik == "zapisano")
                        {
                            var pdfPath = Path.Combine("c:\\1", $"{fiveResult.Data.Guid}.pdf");

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
                            await _telegramBotService.SendAdminTextMessage($"{clients[clientIndex].Email}\n{message}");
                            await _clientService.SaveAsync(clients[clientIndex]);
                        }
                        else
                        {
                            var message = $"{code}. {clients[clientIndex].Email} \nReg is fail: {fiveResult?.Data?.Wynik}";
                            await _telegramBotService.SendAdminTextMessage(message);
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
            if (!File.Exists(fullPatch))
            {
                throw new FileNotFoundException($"CSV file not found: {fullPatch}");
            }

            _logger.LogInformation("Loading data from CSV file: {FilePath}", fullPatch);
            var data = _mlContext.Data.LoadFromTextFile<CaptchaData>(
                path: fullPatch,
                separatorChar: ',',
                hasHeader: true
            );

            // Загружаем данные в список для модификации
            var enumerableData = _mlContext.Data.CreateEnumerable<CaptchaData>(data, reuseRowObject: false).ToList();
            foreach (var item in enumerableData)
            {
                if (!File.Exists(item.ImagePath))
                {
                    throw new FileNotFoundException($"Image file not found: {item.ImagePath}");
                }

                using (var image = Image.Load<Rgba32>(item.ImagePath))
                {
                    var preprocessedImage = image.Clone(ctx =>
                    {
                        ctx.Resize(200, 100);
                        ctx.Grayscale();
                        ctx.BinaryThreshold(0.5f);
                        ctx.MedianBlur(2, true);
                    });

                    string processedImagePath = Path.Combine(directoryPatch, $"processed_{Path.GetFileName(item.ImagePath)}");
                    // Можно использовать SaveAsync, если требуется асинхронность
                    await preprocessedImage.SaveAsync(processedImagePath);

                    // Обновляем путь к изображению в объекте
                    item.ImagePath = processedImagePath;
                }
            }

            // Создаем новый IDataView из обновленного списка
            var updatedData = _mlContext.Data.LoadFromEnumerable(enumerableData);
            // Разделяем данные на обучающую и тестовую выборки
            var splitData = _mlContext.Data.TrainTestSplit(updatedData, testFraction: 0.2);

            _logger.LogInformation("Creating pipeline...");
            var pipeline = _mlContext.Transforms.Conversion.MapValueToKey("Label")
                .Append(_mlContext.Transforms.LoadImages(outputColumnName: "Image", imageFolder: directoryPatch, inputColumnName: "ImagePath"))
                .Append(_mlContext.Transforms.ResizeImages(outputColumnName: "Image", imageWidth: 200, imageHeight: 100))
                .Append(_mlContext.Transforms.ExtractPixels(outputColumnName: "Image"))
                .Append(_mlContext.MulticlassClassification.Trainers.LbfgsMaximumEntropy(
                    labelColumnName: "Label",
                    featureColumnName: "Image",
                    enforceNonNegativity: false,
                    l1Regularization: 0.1f,
                    l2Regularization: 0.5f
                ))
                .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

            _logger.LogInformation("Training model...");
            var model = pipeline.Fit(splitData.TrainSet);

            _logger.LogInformation("Evaluating model...");
            var predictions = model.Transform(splitData.TestSet);
            var metrics = _mlContext.MulticlassClassification.Evaluate(predictions);

            var message =
                $"Model evaluation metrics:\n" +
                $"Accuracy: {metrics.MacroAccuracy:P2}\n" +
                $"Log Loss: {metrics.LogLoss}\n" +
                $"Confusion Matrix: {metrics.ConfusionMatrix.GetFormattedConfusionTable()}";
            await _telegramBotService.SendAdminTextMessage($"{message}");

            string modelPath = Path.Combine(directoryPatch, "CaptchaModel.zip");
            _logger.LogInformation("Saving model to: {ModelPath}", modelPath);
            await Task.Run(() => _mlContext.Model.Save(model, updatedData.Schema, modelPath));

            if (!File.Exists(modelPath))
            {
                throw new InvalidOperationException($"Failed to save model: {modelPath}");
            }

            _logger.LogInformation("Model saved successfully.");
        }


        public async Task<ApiResult<FirstPostRequestModel?>> FirstPostRequest(string url)
        {
            var payLoad = new { imageWidth = 400, imageHeight = 200 };
            var response = await SendPostRequest<FirstPostRequestModel>("https://api.e-konsulat.gov.pl/api/u-captcha/generuj", payLoad);

            if (response != null && response.Data != null)
            {
                //var kodML = await _captchaService.RecognizeCaptchaML(_captchaService.ConvertBase64ToImage(response.Data.Image));
                var kodT = await _captchaService.RecognizeCaptchaTesseract(response.Data.Image);
                //_logger.LogInformation($"{kodML} - {kodT}");
                response.Data.Kod = kodT;
            }

            return response;
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
