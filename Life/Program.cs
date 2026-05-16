using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Drawing;
using System.Drawing.Imaging;

[assembly: InternalsVisibleTo("cli_life.Tests")]

namespace cli_life
{
    public class SimulationSettings
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int CellSize { get; set; }
        public double LiveDensity { get; set; }
        public int DelayMs { get; set; }
    }

    public class Cluster
    {
        public List<(int x, int y)> Cells { get; set; } = new List<(int x, int y)>();
        public string Classification { get; set; } = "Unknown";

        public int GetWidth()
        {
            if (Cells.Count == 0) return 0;
            int minX = Cells.Min(c => c.x);
            int maxX = Cells.Max(c => c.x);
            return maxX - minX + 1;
        }

        public int GetHeight()
        {
            if (Cells.Count == 0) return 0;
            int minY = Cells.Min(c => c.y);
            int maxY = Cells.Max(c => c.y); 
            return maxY - minY + 1;
        }

        public bool[,] GetNormalizedMatrix()
        {
            if (Cells.Count == 0) return new bool[0, 0];
            int minX = Cells.Min(c => c.x);
            int minY = Cells.Min(c => c.y);
            int width = GetWidth();
            int height = GetHeight();
            bool[,] matrix = new bool[width, height];
            foreach (var cell in Cells)
            {
                matrix[cell.x - minX, cell.y - minY] = true;
            }
            return matrix;
        }

        public bool MatchesPattern(bool[,] pattern)
        {
            var matrix = GetNormalizedMatrix();
            int w = matrix.GetLength(0);
            int h = matrix.GetLength(1);
            int pw = pattern.GetLength(0);
            int ph = pattern.GetLength(1);

            if (w != pw || h != ph) return false;

            for (int i = 0; i < w; i++)
                for (int j = 0; j < h; j++)
                    if (matrix[i, j] != pattern[i, j])
                        return false;
            return true;
        }

        public static bool[,] Rotate90(bool[,] mat)
        {
            int w = mat.GetLength(0);
            int h = mat.GetLength(1);
            bool[,] result = new bool[h, w];
            for (int i = 0; i < w; i++)
                for (int j = 0; j < h; j++)
                    result[j, w - 1 - i] = mat[i, j];
            return result;
        }

        public static bool[,] FlipHorizontal(bool[,] mat)
        {
            int w = mat.GetLength(0);
            int h = mat.GetLength(1);
            bool[,] result = new bool[w, h];
            for (int i = 0; i < w; i++)
                for (int j = 0; j < h; j++)
                    result[w - 1 - i, j] = mat[i, j];
            return result;
        }

        public bool MatchesPatternWithSymmetries(bool[,] pattern)
        {
            var variants = new List<bool[,]>();
            var current = pattern;
            for (int rot = 0; rot < 4; rot++)
            {
                variants.Add(current);
                variants.Add(FlipHorizontal(current));
                current = Rotate90(current);
            }

            foreach (var variant in variants)
            {
                if (MatchesPattern(variant))
                    return true;
            }
            return false;
        }
    }

    public class Cell
    {
        public bool IsAlive;
        public readonly List<Cell> neighbors = new List<Cell>();
        private bool IsAliveNext;
        public void DetermineNextLiveState()
        {
            int liveNeighbors = neighbors.Where(x => x.IsAlive).Count();
            if (IsAlive)
                IsAliveNext = liveNeighbors == 2 || liveNeighbors == 3;
            else
                IsAliveNext = liveNeighbors == 3;
        }
        public void Advance()
        {
            IsAlive = IsAliveNext;
        }
    }

    public class Board
    {
        public readonly Cell[,] Cells;
        public readonly int CellSize;

        public int Columns { get { return Cells.GetLength(0); } }
        public int Rows { get { return Cells.GetLength(1); } }
        public int Width { get { return Columns * CellSize; } }
        public int Height { get { return Rows * CellSize; } }

        public Board(int width, int height, int cellSize, double liveDensity = .1)
        {
            CellSize = cellSize;
            Cells = new Cell[width / cellSize, height / cellSize];
            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
                    Cells[x, y] = new Cell();

            ConnectNeighbors();
            Randomize(liveDensity);
        }

        readonly Random rand = new Random();
        public void Randomize(double liveDensity)
        {
            foreach (var cell in Cells)
                cell.IsAlive = rand.NextDouble() < liveDensity;
        }

        public void Advance()
        {
            foreach (var cell in Cells)
                cell.DetermineNextLiveState();
            foreach (var cell in Cells)
                cell.Advance();
        }

        private void ConnectNeighbors()
        {
            for (int x = 0; x < Columns; x++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    int xL = (x > 0) ? x - 1 : Columns - 1;
                    int xR = (x < Columns - 1) ? x + 1 : 0;
                    int yT = (y > 0) ? y - 1 : Rows - 1;
                    int yB = (y < Rows - 1) ? y + 1 : 0;

                    Cells[x, y].neighbors.Add(Cells[xL, yT]);
                    Cells[x, y].neighbors.Add(Cells[x, yT]);
                    Cells[x, y].neighbors.Add(Cells[xR, yT]);
                    Cells[x, y].neighbors.Add(Cells[xL, y]);
                    Cells[x, y].neighbors.Add(Cells[xR, y]);
                    Cells[x, y].neighbors.Add(Cells[xL, yB]);
                    Cells[x, y].neighbors.Add(Cells[x, yB]);
                    Cells[x, y].neighbors.Add(Cells[xR, yB]);
                }
            }
        }

        public void SaveToFile(string filename)
        {
            using (StreamWriter writer = new StreamWriter(filename))
            {
                for (int row = 0; row < Rows; row++)
                {
                    for (int col = 0; col < Columns; col++)
                    {
                        writer.Write(Cells[col, row].IsAlive ? '*' : ' ');
                    }
                    writer.WriteLine();
                }
            }
        }

        public void LoadFromFile(string filename)
        {
            string[] lines = File.ReadAllLines(filename);
            for (int row = 0; row < Rows && row < lines.Length; row++)
            {
                string line = lines[row];
                for (int col = 0; col < Columns && col < line.Length; col++)
                {
                    Cells[col, row].IsAlive = line[col] == '*';
                }
            }
        }

        public List<Cluster> FindClusters()
        {
            bool[,] visited = new bool[Columns, Rows];
            List<Cluster> clusters = new List<Cluster>();

            for (int x = 0; x < Columns; x++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    if (Cells[x, y].IsAlive && !visited[x, y])
                    {
                        Cluster cluster = new Cluster();
                        DFS(x, y, visited, cluster);
                        clusters.Add(cluster);
                    }
                }
            }
            return clusters;
        }

        private void DFS(int x, int y, bool[,] visited, Cluster cluster)
        {
            if (x < 0 || x >= Columns || y < 0 || y >= Rows) return;
            if (!Cells[x, y].IsAlive || visited[x, y]) return;

            visited[x, y] = true;
            cluster.Cells.Add((x, y));

            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    if (dx != 0 || dy != 0)
                        DFS(x + dx, y + dy, visited, cluster);
        }

        public int GetLiveCellCount()
        {
            int count = 0;
            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
                    if (Cells[x, y].IsAlive)
                        count++;
            return count;
        }

        public void PrintStatistics()
        {
            int liveCells = GetLiveCellCount();
            var clusters = FindClusters();
            Console.SetCursorPosition(0, Rows + 4);
            Console.WriteLine($"=== СТАТИСТИКА ===");
            Console.WriteLine($"Живых клеток: {liveCells}");
            Console.WriteLine($"Комбинаций (кластеров): {clusters.Count}");
        }
    }

    public static class StabilityExperiment
    {
        private const int STABILITY_WINDOW = 20;
        private const int MAX_GENERATIONS = 10000;

        public class ExperimentResult
        {
            public double Density { get; set; }
            public int GenerationsToStable { get; set; }
            public int FinalLiveCells { get; set; }
            public bool ReachedMaxGenerations { get; set; }
        }

        public static List<ExperimentResult> RunExperiments(int boardWidth, int boardHeight, int cellSize, List<double> densities, int attemptsPerDensity)
        {
            var allResults = new List<ExperimentResult>();

            Directory.CreateDirectory("Data");

            Console.WriteLine("=== НАЧАЛО ЭКСПЕРИМЕНТОВ ===");
            Console.WriteLine($"Размер поля: {boardWidth}x{boardHeight}");
            Console.WriteLine($"Проверка стабильности: {STABILITY_WINDOW} поколений без изменений");
            Console.WriteLine($"Максимум поколений: {MAX_GENERATIONS}");
            Console.WriteLine();

            foreach (var density in densities)
            {
                Console.WriteLine($"Исследование плотности: {density:F3}");

                for (int attempt = 1; attempt <= attemptsPerDensity; attempt++)
                {
                    Console.Write($"  Попытка {attempt}/{attemptsPerDensity}... ");

                    var board = new Board(boardWidth, boardHeight, cellSize, density);
                    var result = MeasureStabilityTime(board);
                    result.Density = density;

                    allResults.Add(result);
                    Console.WriteLine($"стабильность через {result.GenerationsToStable} поколений, " +
                                    $"клеток: {result.FinalLiveCells}");
                }
                Console.WriteLine();
            }

            SaveResultsToTxt(allResults);
            PlotGenerator.GeneratePlot(allResults);

            Console.WriteLine("=== ЭКСПЕРИМЕНТЫ ЗАВЕРШЕНЫ ===");
            Console.WriteLine($"Результаты сохранены в папке Data/");

            return allResults;
        }

        private static ExperimentResult MeasureStabilityTime(Board board)
        {
            int lastStableGeneration = 0;
            int lastLiveCount = board.GetLiveCellCount();
            int stableCounter = 0;
            int generation = 0;

            while (generation < MAX_GENERATIONS)
            {
                board.Advance();
                generation++;

                int currentLiveCount = board.GetLiveCellCount();

                if (currentLiveCount == lastLiveCount)
                {
                    stableCounter++;
                    if (stableCounter >= STABILITY_WINDOW)
                    {
                        lastStableGeneration = generation - STABILITY_WINDOW;
                        break;
                    }
                }
                else
                {
                    stableCounter = 0;
                    lastLiveCount = currentLiveCount;
                }
            }

            bool reachedMax = generation >= MAX_GENERATIONS;
            int finalCount = board.GetLiveCellCount();

            return new ExperimentResult
            {
                GenerationsToStable = reachedMax ? MAX_GENERATIONS : lastStableGeneration,
                FinalLiveCells = finalCount,
                ReachedMaxGenerations = reachedMax
            };
        }

        public static void SaveResultsToTxt(List<ExperimentResult> results)
        {
            string filePath = "Data/data.txt";
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.WriteLine("РЕЗУЛЬТАТЫ ЭКСПЕРИМЕНТОВ ПО ПЕРЕХОДУ В СТАБИЛЬНУЮ ФАЗУ");
                writer.WriteLine("=================================================");
                writer.WriteLine($"Дата эксперимента: {DateTime.Now}");
                writer.WriteLine($"Проверка стабильности: {STABILITY_WINDOW} поколений без изменения числа клеток");
                writer.WriteLine($"Максимальное число поколений: {MAX_GENERATIONS}");
                writer.WriteLine();
                writer.WriteLine("ФОРМАТ: № | Плотность | Поколения до стабильности | Финальное число клеток | Достигнут лимит");
                writer.WriteLine("--------------------------------------------------------------------------------");

                int index = 1;
                foreach (var result in results)
                {
                    writer.WriteLine($"{index,4} | {result.Density.ToString(CultureInfo.InvariantCulture),10} | {result.GenerationsToStable,25} | {result.FinalLiveCells,23} | {(result.ReachedMaxGenerations ? "Да" : "Нет")}");
                    index++;
                }

                writer.WriteLine();
                writer.WriteLine("СТАТИСТИКА ПО ПЛОТНОСТЯМ:");
                writer.WriteLine("--------------------------------------------------------------------------------");

                var grouped = results.GroupBy(r => r.Density);
                foreach (var group in grouped.OrderBy(g => g.Key))
                {
                    var avgGenerations = group.Average(r => r.GenerationsToStable);
                    var minGenerations = group.Min(r => r.GenerationsToStable);
                    var maxGenerations = group.Max(r => r.GenerationsToStable);
                    var stdDev = Math.Sqrt(group.Average(r => Math.Pow(r.GenerationsToStable - avgGenerations, 2)));

                    writer.WriteLine($"Плотность {group.Key.ToString(CultureInfo.InvariantCulture)}:");
                    writer.WriteLine($"  Среднее поколений: {avgGenerations:F2}");
                    writer.WriteLine($"  Мин/Макс: {minGenerations}/{maxGenerations}");
                    writer.WriteLine($"  Стандартное отклонение: {stdDev:F2}");
                    writer.WriteLine($"  Количество попыток: {group.Count()}");
                    writer.WriteLine($"  Достигли лимита: {group.Count(r => r.ReachedMaxGenerations)}");
                    writer.WriteLine();
                }
            }

            Console.WriteLine($"Сохранено TXT: {filePath}");
        }

        public static class PlotGenerator
        {
            public static void GeneratePlot(List<StabilityExperiment.ExperimentResult> results)
            {
                Directory.CreateDirectory("Data");

                string filePath = "Data/plot.png";

                int width = 1000;
                int height = 700;
                int margin = 80;

                using (Bitmap bitmap = new Bitmap(width, height))
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.Clear(Color.White);

                    Font titleFont = new Font("Arial", 16);
                    Font labelFont = new Font("Arial", 10);

                    Pen axisPen = new Pen(Color.Black, 2);
                    Pen graphPen = new Pen(Color.Blue, 2);

                    Brush pointBrush = Brushes.Red;

                    int plotWidth = width - 2 * margin;
                    int plotHeight = height - 2 * margin;

                    g.DrawLine(axisPen,
                        margin,
                        height - margin,
                        width - margin,
                        height - margin);

                    g.DrawLine(axisPen,
                        margin,
                        margin,
                        margin,
                        height - margin);

                    g.DrawString(
                        "Переход в стабильное состояние",
                        titleFont,
                        Brushes.Black,
                        width / 2 - 170,
                        20);

                    g.DrawString(
                        "Плотность заполнения",
                        labelFont,
                        Brushes.Black,
                        width / 2 - 70,
                        height - 40);

                    g.DrawString(
                        "Поколения",
                        labelFont,
                        Brushes.Black,
                        10,
                        height / 2);

                    var grouped = results
                        .GroupBy(r => r.Density)
                        .OrderBy(g => g.Key)
                        .Select(g => new
                        {
                            Density = g.Key,
                            AvgGenerations = g.Average(r => r.GenerationsToStable)
                        })
                        .ToList();

                    double maxY = grouped.Max(x => x.AvgGenerations);

                    if (maxY <= 0)
                        maxY = 1;

                    PointF[] points = new PointF[grouped.Count];

                    for (int i = 0; i < grouped.Count; i++)
                    {
                        double density = grouped[i].Density;
                        double generations = grouped[i].AvgGenerations;

                        float x = margin + (float)(density * plotWidth);

                        float y = (height - margin) -
                                  (float)((generations / maxY) * plotHeight);

                        points[i] = new PointF(x, y);
                    }

                    if (points.Length > 1)
                    {
                        g.DrawLines(graphPen, points);
                    }

                    foreach (var point in points)
                    {
                        g.FillEllipse(pointBrush,
                            point.X - 4,
                            point.Y - 4,
                            8,
                            8);
                    }

                    for (int i = 0; i <= 10; i++)
                    {
                        float x = margin + i * (plotWidth / 10f);

                        double densityLabel = i / 10.0;

                        g.DrawLine(Pens.Gray,
                            x,
                            height - margin,
                            x,
                            height - margin + 5);

                        g.DrawString(
                            densityLabel.ToString("0.0", CultureInfo.InvariantCulture),
                            labelFont,
                            Brushes.Black,
                            x - 10,
                            height - margin + 10);
                    }

                    for (int i = 0; i <= 10; i++)
                    {
                        float y = (height - margin) - i * (plotHeight / 10f);

                        double value = maxY * i / 10.0;

                        g.DrawLine(Pens.Gray,
                            margin - 5,
                            y,
                            margin,
                            y);

                        g.DrawString(
                            ((int)value).ToString(),
                            labelFont,
                            Brushes.Black,
                            20,
                            y - 7);
                    }

                    bitmap.Save(filePath, ImageFormat.Png);
                }

                Console.WriteLine($"График сохранен: {filePath}");
            }
        }



        class Program
        {
            static Board board;
            static SimulationSettings settings;
            static bool running = true;
            static bool showStats = false;
            static int currentDelay;

            static void Reset()
            {
                board = new Board(
                    width: settings.Width,
                    height: settings.Height,
                    cellSize: settings.CellSize,
                    liveDensity: settings.LiveDensity);
            }

            static void LoadSettings(string filename = "settings.json")
            {
                if (File.Exists(filename))
                {
                    string json = File.ReadAllText(filename);
                    settings = JsonSerializer.Deserialize<SimulationSettings>(json);
                }
                else
                {
                    settings = new SimulationSettings
                    {
                        Width = 50,
                        Height = 20,
                        CellSize = 1,
                        LiveDensity = 0.5,
                        DelayMs = 500
                    };
                    SaveSettings(filename);
                }
                currentDelay = settings.DelayMs;
            }

            static void SaveSettings(string filename = "settings.json")
            {
                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filename, json);
            }

            static void Render()
            {
                for (int row = 0; row < board.Rows; row++)
                {
                    for (int col = 0; col < board.Columns; col++)
                    {
                        var cell = board.Cells[col, row];
                        Console.Write(cell.IsAlive ? '*' : ' ');
                    }
                    Console.Write('\n');
                }
            }

            static void ShowMenu()
            {
                Console.SetCursorPosition(0, board.Rows + 1);
                Console.WriteLine("Commands: [S]ave | [L]oad | [R]eset | [Space] pause/run | [.] step | [I]nfo | [E]xperiment | [Q]uit | [+]/[-] speed");
            }

            static void HandleInput()
            {
                if (!Console.KeyAvailable) return;
                var key = Console.ReadKey(true).Key;
                switch (key)
                {
                    case ConsoleKey.S:
                        board.SaveToFile("saved_state.txt");
                        Console.SetCursorPosition(0, board.Rows + 2);
                        Console.WriteLine("State saved to saved_state.txt");
                        break;
                    case ConsoleKey.L:
                        if (File.Exists("saved_state.txt"))
                        {
                            board.LoadFromFile("saved_state.txt");
                            Console.SetCursorPosition(0, board.Rows + 2);
                            Console.WriteLine("State loaded from saved_state.txt");
                        }
                        break;
                    case ConsoleKey.R:
                        Reset();
                        break;
                    case ConsoleKey.Spacebar:
                        running = !running;
                        break;
                    case ConsoleKey.OemPeriod:
                        if (!running)
                        {
                            board.Advance();
                            Console.Clear();
                            Render();
                            ShowMenu();
                            if (showStats) board.PrintStatistics();
                        }
                        break;
                    case ConsoleKey.I:
                        showStats = !showStats;
                        Console.SetCursorPosition(0, board.Rows + 2);
                        Console.WriteLine($"Statistics display: {(showStats ? "ON" : "OFF")}     ");
                        if (showStats) board.PrintStatistics();
                        break;
                    case ConsoleKey.E:
                        RunExperiment();
                        break;
                    case ConsoleKey.Q:
                        Environment.Exit(0);
                        break;
                    case ConsoleKey.Add:
                    case ConsoleKey.OemPlus:
                        currentDelay = Math.Max(50, currentDelay - 50);
                        break;
                    case ConsoleKey.Subtract:
                    case ConsoleKey.OemMinus:
                        currentDelay = Math.Min(1000, currentDelay + 50);
                        break;
                }
            }

            static void RunExperiment()
            {
                Console.Clear();
                Console.WriteLine("=== ЗАПУСК ЭКСПЕРИМЕНТА ===");
                Console.WriteLine("Параметры эксперимента:");
                Console.WriteLine($"Размер поля: {settings.Width}x{settings.Height}");
                Console.WriteLine();

                List<double> densities = new List<double>();
                for (double d = 0.05; d <= 0.95 + 0.0001; d += 0.05)
                {
                    densities.Add(Math.Round(d, 2));
                }

                Console.WriteLine("Исследуемые плотности:");
                foreach (var d in densities)
                {
                    Console.Write($"{d.ToString(CultureInfo.InvariantCulture)} ");
                }
                Console.WriteLine("\n");

                Console.Write("Количество попыток для каждой плотности (рекомендуется 10-30): ");
                string input = Console.ReadLine();
                int attempts = 10;
                if (!int.TryParse(input, out attempts) || attempts < 1)
                {
                    attempts = 10;
                    Console.WriteLine($"Используем значение по умолчанию: {attempts}");
                }

                Console.WriteLine("\nНачинаю эксперименты...");
                Console.WriteLine("Это может занять несколько минут. Результаты будут сохранены в папке Data/\n");

                var results = StabilityExperiment.RunExperiments(
                    settings.Width,
                    settings.Height,
                    settings.CellSize,
                    densities,
                    attempts
                );

                Console.WriteLine("\nНажмите любую клавишу для продолжения...");
                Console.ReadKey();

                Reset();
                running = true;
                Console.Clear();
            }

            static void Main(string[] args)
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

                LoadSettings();
                Reset();

                while (true)
                {
                    if (running)
                    {
                        Console.Clear();
                        Render();
                        ShowMenu();
                        if (showStats) board.PrintStatistics();
                        board.Advance();
                        Thread.Sleep(currentDelay);
                    }
                    else
                    {
                        Console.Clear();
                        Render();
                        ShowMenu();
                        if (showStats) board.PrintStatistics();
                        Thread.Sleep(100);
                    }
                    HandleInput();
                }
            }
        }
    }
}
