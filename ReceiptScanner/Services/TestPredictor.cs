using OpenCvSharp;
using Sdcb.PaddleOCR.Models.Online;
using Sdcb.PaddleOCR.Models;
using Sdcb.PaddleOCR;
using System.Diagnostics;

namespace ReceiptScanner.Services
{
    public static class TestPredictor
    {
        public static async Task<string> PredictAsync(byte[] imageBytes)
        {
            // Download English OCR model
            FullOcrModel model = await OnlineFullModels.EnglishV3.DownloadAsync();
            // Set up PaddleOCR with the downloaded model
            using (PaddleOcrAll ocrEngine = new(model)
            {
                AllowRotateDetection = true,
                Enable180Classification = false, // Optimize for performance
            })
            using (Mat imgSrc = Cv2.ImDecode(imageBytes, ImreadModes.Color)) // Load the image
            {
                // Perform OCR and measure elapsed time
                Stopwatch stopWatch = Stopwatch.StartNew();
                PaddleOcrResult result = ocrEngine.Run(imgSrc);
                Console.WriteLine($"Elapsed={stopWatch.ElapsedMilliseconds} ms");
                return result.Text;
            }
        }
    }
}
