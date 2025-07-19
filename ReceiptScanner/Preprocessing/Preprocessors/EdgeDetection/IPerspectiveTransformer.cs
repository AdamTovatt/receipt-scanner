using OpenCvSharp;

namespace ReceiptScanner.Preprocessing.Preprocessors.EdgeDetection
{
    public interface IPerspectiveTransformer
    {
        Mat TransformPerspective(Mat image, Point[] corners);
    }
}