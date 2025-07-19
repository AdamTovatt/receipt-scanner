namespace ReceiptScanner.Models
{
    public struct Point
    {
        public readonly int X { get; init; }
        public readonly int Y { get; init; }

        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }
    }
}
