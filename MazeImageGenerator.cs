using System;
using System.Collections.Generic;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MazeSolver
{
    internal class MazeImageGenerator
    {
        private Image<Rgba32> image = new Image<Rgba32>(122, 121);

        private Dictionary<MazeLocationInfo, Rgba32> _colors = new Dictionary<MazeLocationInfo, Rgba32>();

        public MazeImageGenerator()
        {
            _colors.Add(MazeLocationInfo.Start, new Rgba32(0,byte.MaxValue,0));
            _colors.Add(MazeLocationInfo.Wall, new Rgba32(0, 0, 0));
            _colors.Add(MazeLocationInfo.Space, new Rgba32(byte.MaxValue, byte.MaxValue, byte.MaxValue));
            _colors.Add(MazeLocationInfo.Exit, new Rgba32(byte.MaxValue, 0, 0));
            _colors.Add(MazeLocationInfo.Unknown, new Rgba32(127, 127, 127));
            _colors.Add(MazeLocationInfo.DeadEnd, new Rgba32(173, 216, 216));

            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    image[x, y] = _colors[MazeLocationInfo.Unknown];
                }
            }            
        }

        public void DrawPixel(int x, int y, MazeLocationInfo locationInfo)
        {
            image[x, y] = GetColorFromInfo(locationInfo);
        }


        private Rgba32 GetColorFromInfo(MazeLocationInfo locationInfo)
        {
            return _colors[locationInfo];
        }

        public void GenerateImage(string filepath)
        {
            image.SaveAsPng(filepath);
            image.Dispose();
        }
    }
}
