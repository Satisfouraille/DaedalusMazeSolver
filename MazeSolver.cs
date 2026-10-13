using System.Diagnostics;

MazeSolver.MazeSolver mazeSolver = new();
Stopwatch timer = new Stopwatch();
timer.Start();
await mazeSolver.SolveMaze();
timer.Stop();
Console.WriteLine("Time to solve : " + timer.Elapsed);

namespace MazeSolver
{
    class MazeSolver
    {
        public static bool Solved = false;
        private readonly DaedalusClient _client = new DaedalusClient();
        private readonly Grid2D _grid2D = new Grid2D();
        private string _currentTry = "";
        private Point2D _currentLocation = new Point2D(0, 0);
        private Direction _lastMoveDirection;
        private Point2D _startLocation;
        private string _startString = "";
        private Direction _startDirection = Direction.Up;
        public async Task SolveMaze()
        {
            _grid2D.AddVisitedLocation(0, 0, MazeLocationInfo.Start);

            while (!Solved)
            {
                // TODO : idée : garder le debut d'un deadend pour le marquer direct
                bool moved = false;
                MazeLocationInfo statusRight = _grid2D.GetStatusFor(_currentLocation.GetPointAfterMovingTo(_lastMoveDirection.RelativeRight));
                MazeLocationInfo statusAhead = _grid2D.GetStatusFor(_currentLocation.GetPointAfterMovingTo(_lastMoveDirection));
                MazeLocationInfo statusLeft = _grid2D.GetStatusFor(_currentLocation.GetPointAfterMovingTo(_lastMoveDirection.RelativeLeft));
                if (statusAhead == MazeLocationInfo.Unknown
                    || statusAhead == MazeLocationInfo.Space)
                {
                    moved = await MoveIfPossible(_lastMoveDirection);
                    if (moved) continue;
                }

                if (statusRight == MazeLocationInfo.Unknown
                    || statusRight == MazeLocationInfo.Space)
                {
                    moved = await MoveIfPossible(_lastMoveDirection.RelativeRight);
                    if (moved) continue;
                }

                if (statusLeft == MazeLocationInfo.Unknown
                    || statusLeft == MazeLocationInfo.Space)
                {
                    moved = await MoveIfPossible(_lastMoveDirection.RelativeLeft);
                    if (moved) continue;
                }
                
                _grid2D.MarkAsDeadEnd(_currentLocation);
                //MarkCurrentPointAsIntersection();
                Restart();
            }

            string folderPath = AppDomain.CurrentDomain.BaseDirectory;
            string txtFilePath = Path.Combine(folderPath, "all_locations.txt");
            using StreamWriter sw = new StreamWriter(txtFilePath);

            foreach (string mazeLocation in _grid2D.GetAllMazeLocationsAsStrings())
            {
                sw.WriteLine(mazeLocation);
            }
            MazeImageGenerator generator = new MazeImageGenerator();
            foreach (var (point2D, locationInfo) in _grid2D.GetAllMazeLocationsAsTuples())
            {
                generator.DrawPixel(point2D.X + 2, point2D.Y + 119, locationInfo);
            }
            string pngFilePath = Path.Combine(folderPath, "maze.png");
            generator.GenerateImage(pngFilePath);
        }

        private async Task<bool> MoveIfPossible(Direction direction)
        {
            if (await GetMoveAttemptResult(direction))
            {
                PerformMove(direction);
                _grid2D.AddVisitedLocation(_currentLocation.X, _currentLocation.Y, MazeLocationInfo.Space);
                return true;
            }
            else
            {
                _grid2D.AddVisitedLocation(
                    _currentLocation.X + direction.DeltaX,
                    _currentLocation.Y + direction.DeltaY,
                    MazeLocationInfo.Wall);
                return false;
            }
        }

        private void PerformMove(Direction direction)
        {
            _currentTry += direction.ToChar();
            _currentLocation.X += direction.DeltaX;
            _currentLocation.Y += direction.DeltaY;
            _lastMoveDirection = direction;
            Console.WriteLine($"Moved {direction.ToString()}, new position ({_currentLocation.X},{_currentLocation.Y})");
        }

        private void Restart()
        {
            if (_currentLocation == _startLocation)
            {
                Solved = true;
            }
            Console.BackgroundColor = ConsoleColor.Gray;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.WriteLine("Attempted :" + _currentTryBuffer);
            _currentLocation = _startLocation;
            _currentTry = _startString;
            _lastMoveDirection = _startDirection;
            Console.BackgroundColor = ConsoleColor.Yellow;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.WriteLine($"Restarted at {_startLocation.X},{_startLocation.Y}");
            Console.ResetColor();


        }

        private void MarkCurrentPointAsIntersection()
        {
            //_lastIntersection = _currentLocation;
        }

        private string _currentTryBuffer = "";

        private async Task<bool> GetMoveAttemptResult(Direction direction)
        {
            MazeLocationInfo moveAttemptInfo = _grid2D.GetStatusFor(_currentLocation.GetPointAfterMovingTo(direction));
            if (moveAttemptInfo != MazeLocationInfo.Unknown)
            {
                return moveAttemptInfo == MazeLocationInfo.Space;
            }

            _currentTryBuffer = _currentTry + direction.ToChar();

            long? result = await _client.MakeRequest(_currentTryBuffer);

            return result == DaedalusClient.ProgressPageContentLength;
        }
    }

    public record struct Point2D(int X, int Y)
    {

        public Point2D GetPointAfterMovingTo(Direction direction)
        {
            return new Point2D(X + direction.DeltaX, Y + direction.DeltaY);
        }
    }

    public class Grid2D
    {
        private Dictionary<Point2D, MazeLocationInfo> _visitedLocations = new Dictionary<Point2D, MazeLocationInfo>();

        public void AddVisitedLocation(int x, int y, MazeLocationInfo locationInfo)
        {
            Point2D pointToAdd = new Point2D(x, y);
            _visitedLocations.TryAdd(pointToAdd, locationInfo);
        }

        public MazeLocationInfo GetStatusFor(Point2D point2D)
        {
            if (!_visitedLocations.TryGetValue(point2D, out MazeLocationInfo value))
            {
                return MazeLocationInfo.Unknown;
            }
            return value;
        }

        public void MarkAsDeadEnd(Point2D point2D)
        {
            if (_visitedLocations.ContainsKey(point2D))
            {
                _visitedLocations[point2D] = MazeLocationInfo.DeadEnd;
            }
        }

        public IEnumerable<string> GetAllMazeLocationsAsStrings()
        {
            foreach (var (mazeLocation, value) in _visitedLocations)
            {
                yield return $"{mazeLocation.X},{mazeLocation.Y},{value}";
            }
        }

        public IEnumerable<(Point2D, MazeLocationInfo)> GetAllMazeLocationsAsTuples()
        {
            foreach (var (point2d, locationInfo) in _visitedLocations)
            {
                yield return (point2d, locationInfo);
            }
        }
    }

    public record struct MazeLocation(int X, int Y, MazeLocationInfo LocationInfo);
    public enum Direction { Up, Left, Down, Rite }
    public static class DirectionToChar
    {
        extension(Direction direction)
        {
            public char ToChar()
            {
                switch (direction)
                {
                    case Direction.Up:
                        return 'u';
                    case Direction.Left:
                        return 'l';
                    case Direction.Down:
                        return 'd';
                    case Direction.Rite:
                        return 'r';
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            public Direction RelativeRight => (Direction)(((int)direction + 3) % 4);
            public Direction RelativeOpposite => (Direction)(((int)direction + 2) % 4);
            public Direction RelativeLeft => (Direction)(((int)direction + 1) % 4);
            public int DeltaX => direction switch
            {
                Direction.Up => 0,
                Direction.Down => 0,
                Direction.Left => -1,
                Direction.Rite => 1,
                _ => throw new ArgumentOutOfRangeException(),
            };
            public int DeltaY => direction switch
            {
                Direction.Up => 1,
                Direction.Down => -1,
                Direction.Left => 0,
                Direction.Rite => 0,
                _ => throw new ArgumentOutOfRangeException(),
            };
        }
    }
    public enum MazeLocationInfo { Start, Wall, Space, Exit, Unknown, DeadEnd }

    public class DaedalusClient
    {
        public int RequestsMade { get; private set; } = 0;
        public const int HomepageContentLength = 1843;
        public const int ProgressPageContentLength = 1744;
        public const int BonkPageContentLength = 1730;

        private static HttpClient _sharedClient;
        private static readonly Uri BaseUri = new Uri("https://daedalus.defi.info.cegepmontpetit.ca");

        static DaedalusClient()
        {

            HttpClientHandler clientHandler = new HttpClientHandler()
            {
                UseCookies = false,
            };

            _sharedClient = new HttpClient(clientHandler)
            {
                BaseAddress = BaseUri,
                Timeout = TimeSpan.FromSeconds(10)
            };
        }

        public async Task<long?> MakeRequest(string attempt)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(attempt);
            string base64Value = Convert.ToBase64String(plainTextBytes);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "/move");
            request.Headers.TryAddWithoutValidation("Cookie", $"path={base64Value}");

            using HttpResponseMessage response = await _sharedClient.SendAsync(request);

            long? responseLength = response.Content.Headers.ContentLength.GetValueOrDefault();
            if (responseLength != ProgressPageContentLength
                && responseLength != BonkPageContentLength
                && responseLength != HomepageContentLength)
            {
                //MazeSolver.Solved = true;
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine("Unexpected answer : \n" + await response.Content.ReadAsStringAsync());
                Console.ResetColor();
                Console.WriteLine("Requests made : " + RequestsMade);
            }

            RequestsMade++;

            return responseLength;
        }
    }
}