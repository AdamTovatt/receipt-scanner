using OpenCvSharp;
using ReceiptScannerTests.Models;
using System.Diagnostics;
using System.Reflection;

namespace ReceiptScannerTests.Utilities
{
    public class LocalFileDebugOutputService : IDebugOutputService
    {
        private readonly string _basePath;

        public LocalFileDebugOutputService()
        {
            // Get the calling class name to use as the base path
            string callingClassName = GetCallingClassName();
            string assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            _basePath = Path.Combine(assemblyLocation, "DebugOutput", callingClassName);

            Directory.CreateDirectory(_basePath);
        }

        public void OutputImageWithPoints(Mat image, List<DebugPoint> points, string imageName)
        {
            // Create a copy of the image to draw on
            Mat outputImage = image.Clone();

            // Draw each point
            foreach (DebugPoint debugPoint in points)
            {
                // Draw a filled circle at the point
                Cv2.Circle(outputImage, new Point(debugPoint.Point.X, debugPoint.Point.Y), 5, debugPoint.Color, -1);

                // Draw a larger circle outline
                Cv2.Circle(outputImage, new Point(debugPoint.Point.X, debugPoint.Point.Y), 8, debugPoint.Color, 2);

                // Add text label if provided
                if (!string.IsNullOrEmpty(debugPoint.Text))
                {
                    Cv2.PutText(outputImage, debugPoint.Text, new Point(debugPoint.Point.X + 10, debugPoint.Point.Y - 10),
                        HersheyFonts.HersheySimplex, 0.5, debugPoint.Color, 1);
                }
            }

            // Save the image
            string outputPath = Path.Combine(_basePath, $"{imageName}.png");
            outputImage.SaveImage(outputPath);

            Console.WriteLine($"Debug image saved to: {outputPath}");

            // Cleanup
            outputImage.Dispose();
        }

        public void OutputImage(Mat image, string imageName)
        {
            // Save the image directly without any modifications
            string outputPath = Path.Combine(_basePath, $"{imageName}.png");
            image.SaveImage(outputPath);

            Console.WriteLine($"Debug image saved to: {outputPath}");
        }

        public static string CreateNameFromCaller(string suffix)
        {
            StackTrace stackTrace = new StackTrace(skipFrames: 1, fNeedFileInfo: false);
            Assembly executingAssembly = Assembly.GetExecutingAssembly();

            foreach (StackFrame frame in stackTrace.GetFrames()!)
            {
                MethodBase? method = frame.GetMethod();
                if (method == null)
                {
                    continue;
                }

                Type? declaringType = method.DeclaringType;
                if (declaringType == null)
                {
                    continue;
                }

                if (declaringType == typeof(LocalFileDebugOutputService))
                {
                    continue;
                }

                Assembly assembly = declaringType.Assembly;

                // Skip system assemblies and dynamic assemblies
                if (assembly.FullName!.StartsWith("System") ||
                    assembly.FullName.StartsWith("Microsoft") ||
                    assembly.IsDynamic)
                {
                    continue;
                }

                string? methodName = method.Name;

                // Skip async method names (they contain angle brackets and d__)
                if (methodName.Contains('<') || methodName.Contains("d__"))
                {
                    continue;
                }

                if (methodName == "MoveNext")
                {
                    continue;
                }

                // Convert PascalCase to a more readable format
                string readableName = ConvertPascalCaseToReadable(methodName);

                // Combine with suffix
                return $"{readableName}_{suffix}";
            }

            // Fallback if we can't get the method name
            return $"UnknownMethod_{suffix}";
        }

        private static string GetCallingClassName()
        {
            StackTrace stackTrace = new StackTrace(skipFrames: 1, fNeedFileInfo: false);
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            foreach (StackFrame frame in stackTrace.GetFrames()!)
            {
                MethodBase? method = frame.GetMethod();
                if (method == null)
                {
                    continue;
                }

                Type? declaringType = method.DeclaringType;
                if (declaringType == null)
                {
                    continue;
                }

                if (declaringType == typeof(LocalFileDebugOutputService))
                {
                    continue;
                }

                Assembly assembly = declaringType.Assembly;

                // Skip system assemblies and dynamic assemblies
                if (assembly.FullName!.StartsWith("System") ||
                    assembly.FullName.StartsWith("Microsoft") ||
                    assembly.IsDynamic)
                {
                    continue;
                }

                string className = declaringType.Name;
                string? methodName = method.Name;

                if (assembly == executingAssembly)
                {
                    if (className.Contains('<'))
                    {
                        if (declaringType.FullName?.Length >= className.Length)
                        {
                            string firstPart = declaringType.FullName.Substring(0, declaringType.FullName.Length - className.Length);
                            firstPart = firstPart.TrimEnd(' ', '+');
                            firstPart = firstPart.Split('.').Last();

                            Type[] assemblyTypes = assembly.GetTypes();

                            if (assemblyTypes.Any(x => x.Name == firstPart))
                            {
                                return firstPart;
                            }
                        }
                    }
                }

                // Skip async method names (they contain angle brackets and d__)
                if (methodName.Contains('<') || methodName.Contains("d__"))
                {
                    continue;
                }

                // Look for test classes (they typically end with "Tests")
                if (className.EndsWith("Tests"))
                {
                    return className;
                }

                // Also check if it's a class that contains test methods
                if (declaringType.GetCustomAttributes(typeof(TestClassAttribute), false).Length > 0)
                {
                    return className;
                }
            }

            // Final fallback if we can't get the class name
            return "UnknownClass";
        }

        private static string ConvertPascalCaseToReadable(string pascalCase)
        {
            if (string.IsNullOrEmpty(pascalCase))
                return pascalCase;

            // Add spaces before capital letters (except the first character)
            string result = pascalCase[0].ToString();

            for (int i = 1; i < pascalCase.Length; i++)
            {
                if (char.IsUpper(pascalCase[i]))
                {
                    result += " " + pascalCase[i];
                }
                else
                {
                    result += pascalCase[i];
                }
            }

            // Remove spaces and make it a valid filename
            return result.Replace(" ", "");
        }
    }
}