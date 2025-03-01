using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;

namespace Services.Interfaces
{
    public interface ICaptchaRecognitionService
    {
        Task<string> RecognizeCaptchaTesseract(string base64String);
        Image<Rgba32> ConvertBase64ToImage(string base64String);
        Task<string> RecognizeCaptchaML(Image<Rgba32> image);
    }
}
