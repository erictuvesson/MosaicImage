namespace CreateMosaicImage
{
    using System;

    /// <summary>
    /// Simple point value, makes it easier to send x,y.
    /// Could use <see cref="System.Tuple"/> instead, but this is nicer.
    /// </summary>
    struct Point
    {
        public int X;
        public int Y;

        public Point(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        public override string ToString()
        {
            return $"{X}, {Y}";
        }
    }

    /// <summary>
    /// Allows for custom loops, this gives us better color focus.
    /// </summary>
    class LoopPattern
    {
        public readonly int Width;
        public readonly int Height;

        protected readonly Action<Point> Action;

        public LoopPattern(int width, int height, Action<Point> action)
        {
            this.Width = width;
            this.Height = height;
            this.Action = action;

            System.Diagnostics.Debug.Assert(action != null);
        }

        public virtual void Execute()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    Action(new Point(x, y));
                }
            }
        }
    }

    /// <summary>
    /// Focus the middle.
    /// </summary>
    class SpiralLoopPattern : LoopPattern
    {
        public SpiralLoopPattern(int width, int height, Action<Point> action)
            : base(width, height, action)
        {

        }

        public override void Execute()
        {
            int x = 0, y = 0, dx = 0, dy = -1;
            int t = Math.Max(Width, Height);
            int max = t * t;

            int x2 = Width / 2;
            int y2 = Height / 2;

            for (int i = 0; i < max; i++)
            {
                if ((-Width / 2 <= x) && (x <= Width / 2) && (-Height / 2 <= y) && (y <= Height / 2))
                {
                    Action(new Point(x2 + x, y2 + y));
                }

                if ((x == y) || ((x < 0) && (x == -y)) || ((x > 0) && (x == 1 - y)))
                {
                    t = dx; dx = -dy; dy = t;
                }
                x += dx; y += dy;
            }
        }
    }
}
