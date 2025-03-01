using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Services.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Tesseract;
using BezKolejki_bot.Models;

namespace Services.Services
{
    public class CaptchaRecognitionService : ICaptchaRecognitionService
    {
        private readonly ILogger<CaptchaRecognitionService> _logger;
        private readonly string _tessDataPath;
        private readonly string _modelPath;
        private readonly MLContext _mlContext;
        private ITransformer _mlModel;
        private bool _isModelLoaded = false;
        public CaptchaRecognitionService(ILogger<CaptchaRecognitionService> logger)
        {
            _logger = logger;
            _modelPath = @"c:\1\ok2\CaptchaModel.zip";
            _tessDataPath = @"d:\Work\dev\Telegram_bot\tessdata";
            _mlContext = new MLContext();
        }

        public async Task<string> RecognizeCaptchaTesseract(string base64String)
        {
            string recognizedText = string.Empty;
            byte[] imageBytes = Convert.FromBase64String(base64String);
            using (var ms = new MemoryStream(imageBytes))
            {
                Image<Rgba32> preprocessedImage = PreprocessImage(ms);
                recognizedText =  RecognizeCaptcha(preprocessedImage, _tessDataPath);
                _logger.LogInformation("Распознанный текст: " + recognizedText);
                return recognizedText;
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

        public Image<Rgba32> ConvertBase64ToImage(string base64String)
        {
            byte[] imageBytes = Convert.FromBase64String(base64String);

            using (var ms = new MemoryStream(imageBytes))
            {
                return Image.Load<Rgba32>(ms);
            }
        }

        public async Task<string> RecognizeCaptchaML(Image<Rgba32> image)
        {
            try
            {
                if (!_isModelLoaded)
                {
                    LoadMlModel();
                }

                // Предобработка для ML (можно вынести в отдельный метод PreprocessImageForML)
                var preprocessedImage = image.Clone(ctx =>
                {
                    ctx.Resize(200, 100);
                    ctx.Grayscale();
                    ctx.BinaryThreshold(0.5f);
                    ctx.MedianBlur(2, true);
                });

                // Создаём временный файл для сохранения изображения
                string tempImagePath = Path.ChangeExtension(Path.GetTempFileName(), ".png");
                await preprocessedImage.SaveAsync(tempImagePath);

                // Подготовка входных данных
                var input = new CaptchaData { ImagePath = tempImagePath };
                var inputData = _mlContext.Data.LoadFromEnumerable(new[] { input });

                // Применение модели
                var predictions = _mlModel.Transform(inputData);
                var predictedLabels = _mlContext.Data.CreateEnumerable<CaptchaPrediction>(predictions, reuseRowObject: false);
                File.Delete(tempImagePath);

                return predictedLabels.FirstOrDefault()?.PredictedLabel ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка распознавания капчи ML: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Загружает обученную ML-модель.
        /// </summary>
        private void LoadMlModel()
        {
            if (!File.Exists(_modelPath))
            {
                throw new FileNotFoundException($"Model file not found: {_modelPath}");
            }
            _mlModel = _mlContext.Model.Load(_modelPath, out var modelSchema);
            _isModelLoaded = true;
        }
    }
}
