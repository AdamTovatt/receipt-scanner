using Microsoft.ML.OnnxRuntime.Tensors;
using ReceiptScanner.Models;
using System.Text;

namespace ReceiptScanner.Services
{
    public class PGNetPostProcessor
    {
        private readonly List<string> _characterDict;
        private readonly float _scoreThreshold;
        private readonly string _validSet;

        public PGNetPostProcessor(List<string> characterDict, float scoreThreshold = 0.5f, string validSet = "totaltext")
        {
            _characterDict = characterDict;
            _scoreThreshold = scoreThreshold;
            _validSet = validSet;
        }

        public PGNetPostProcessResult Postprocess(Dictionary<string, Tensor<float>> predictions, int[] shapeList)
        {
            try
            {
                // Extract predictions
                Tensor<float> fBorder = predictions["f_border"];
                Tensor<float> fChar = predictions["f_char"];
                Tensor<float> fDirection = predictions["f_direction"];
                Tensor<float> fScore = predictions["f_score"];

                // Get dimensions
                int batchSize = fChar.Dimensions[0];
                int maxLength = fChar.Dimensions[1];
                int numClasses = fChar.Dimensions[2];

                List<Point[]> allBoundingBoxes = new List<Point[]>();
                List<string> allTexts = new List<string>();

                for (int b = 0; b < batchSize; b++)
                {
                    // Decode characters
                    List<string> texts = DecodeCharacters(fChar, b, maxLength, numClasses);
                    
                    // Get scores
                    float[] scores = GetScores(fScore, b, maxLength);
                    
                    // Get directions
                    float[] directions = GetDirections(fDirection, b, maxLength);
                    
                    // Get borders
                    Point[][] borders = GetBorders(fBorder, b, maxLength);

                    // Filter by score threshold
                    for (int i = 0; i < texts.Count; i++)
                    {
                        if (scores[i] >= _scoreThreshold && !string.IsNullOrWhiteSpace(texts[i]))
                        {
                            allTexts.Add(texts[i]);
                            allBoundingBoxes.Add(borders[i]);
                        }
                    }
                }

                return new PGNetPostProcessResult
                {
                    BoundingBoxes = allBoundingBoxes,
                    Texts = allTexts
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"PGNet postprocessing failed: {ex.Message}", ex);
            }
        }

        private List<string> DecodeCharacters(Tensor<float> fChar, int batchIndex, int maxLength, int numClasses)
        {
            List<string> texts = new List<string>();

            for (int i = 0; i < maxLength; i++)
            {
                // Get character probabilities for this position
                float[] charProbs = new float[numClasses];
                for (int j = 0; j < numClasses; j++)
                {
                    charProbs[j] = fChar[batchIndex, i, j];
                }

                // Find the most likely character
                int maxIndex = 0;
                float maxProb = charProbs[0];
                for (int j = 1; j < numClasses; j++)
                {
                    if (charProbs[j] > maxProb)
                    {
                        maxProb = charProbs[j];
                        maxIndex = j;
                    }
                }

                // Convert to character (skip if it's a padding/special token)
                if (maxIndex < _characterDict.Count && maxIndex > 0) // Assuming 0 is padding
                {
                    texts.Add(_characterDict[maxIndex]);
                }
                else if (maxIndex == 0)
                {
                    texts.Add(""); // Padding
                }
            }

            // Join characters into text
            string fullText = string.Join("", texts).Trim();
            return new List<string> { fullText };
        }

        private float[] GetScores(Tensor<float> fScore, int batchIndex, int maxLength)
        {
            float[] scores = new float[maxLength];
            for (int i = 0; i < maxLength; i++)
            {
                scores[i] = fScore[batchIndex, i];
            }
            return scores;
        }

        private float[] GetDirections(Tensor<float> fDirection, int batchIndex, int maxLength)
        {
            float[] directions = new float[maxLength];
            for (int i = 0; i < maxLength; i++)
            {
                directions[i] = fDirection[batchIndex, i];
            }
            return directions;
        }

        private Point[][] GetBorders(Tensor<float> fBorder, int batchIndex, int maxLength)
        {
            Point[][] borders = new Point[maxLength][];
            
            // Assuming fBorder has shape [batch, maxLength, 4, 2] for 4 points with x,y coordinates
            for (int i = 0; i < maxLength; i++)
            {
                borders[i] = new Point[4];
                for (int j = 0; j < 4; j++)
                {
                    borders[i][j] = new Point
                    {
                        X = (int)fBorder[batchIndex, i, j, 0],
                        Y = (int)fBorder[batchIndex, i, j, 1]
                    };
                }
            }
            
            return borders;
        }
    }
} 