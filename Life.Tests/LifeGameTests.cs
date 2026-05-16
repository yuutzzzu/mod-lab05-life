using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;

[assembly: InternalsVisibleTo("cli_life.Tests")]

namespace cli_life.Tests
{
    public class LifeGameTests
    {
        // ========== ТЕСТЫ НА КЛЕТКУ И СОСЕДЕЙ ==========

        [Fact]
        public void Cell_InitiallyDead_IsAliveFalse()
        {
            var cell = new Cell();
            Assert.False(cell.IsAlive);
        }

        [Fact]
        public void Cell_SetIsAlive_WorksCorrectly()
        {
            var cell = new Cell();
            cell.IsAlive = true;
            Assert.True(cell.IsAlive);

            cell.IsAlive = false;
            Assert.False(cell.IsAlive);
        }

        [Fact]
        public void Cell_NeighborsCollection_InitiallyEmpty()
        {
            var cell = new Cell();
            Assert.Empty(cell.neighbors);
        }

        [Fact]
        public void Cell_DetermineNextLiveState_LiveCellWith2Neighbors_Survives()
        {
            var cell = new Cell { IsAlive = true };
            var neighbor1 = new Cell { IsAlive = true };
            var neighbor2 = new Cell { IsAlive = true };

            cell.neighbors.Add(neighbor1);
            cell.neighbors.Add(neighbor2);

            cell.DetermineNextLiveState();
            cell.Advance();

            Assert.True(cell.IsAlive);
        }

        [Fact]
        public void Cell_DetermineNextLiveState_LiveCellWith3Neighbors_Survives()
        {
            var cell = new Cell { IsAlive = true };
            for (int i = 0; i < 3; i++)
                cell.neighbors.Add(new Cell { IsAlive = true });

            cell.DetermineNextLiveState();
            cell.Advance();

            Assert.True(cell.IsAlive);
        }

        [Fact]
        public void Cell_DetermineNextLiveState_LiveCellWith1Neighbor_Dies()
        {
            var cell = new Cell { IsAlive = true };
            cell.neighbors.Add(new Cell { IsAlive = true });

            cell.DetermineNextLiveState();
            cell.Advance();

            Assert.False(cell.IsAlive);
        }

        [Fact]
        public void Cell_DetermineNextLiveState_LiveCellWith4Neighbors_Dies()
        {
            var cell = new Cell { IsAlive = true };
            for (int i = 0; i < 4; i++)
                cell.neighbors.Add(new Cell { IsAlive = true });

            cell.DetermineNextLiveState();
            cell.Advance();

            Assert.False(cell.IsAlive);
        }

        [Fact]
        public void Cell_DetermineNextLiveState_DeadCellWith3Neighbors_BecomesAlive()
        {
            var cell = new Cell { IsAlive = false };
            for (int i = 0; i < 3; i++)
                cell.neighbors.Add(new Cell { IsAlive = true });

            cell.DetermineNextLiveState();
            cell.Advance();

            Assert.True(cell.IsAlive);
        }

        [Fact]
        public void Cell_DetermineNextLiveState_DeadCellWith2Neighbors_StaysDead()
        {
            var cell = new Cell { IsAlive = false };
            for (int i = 0; i < 2; i++)
                cell.neighbors.Add(new Cell { IsAlive = true });

            cell.DetermineNextLiveState();
            cell.Advance();

            Assert.False(cell.IsAlive);
        }

        // ========== ТЕСТЫ НА BOARD ==========

        [Fact]
        public void Board_Constructor_CreatesCorrectDimensions()
        {
            var board = new Board(50, 30, 1, 0.0);

            var columnsProperty = board.GetType().GetProperty("Columns");
            var rowsProperty = board.GetType().GetProperty("Rows");
            var widthProperty = board.GetType().GetProperty("Width");
            var heightProperty = board.GetType().GetProperty("Height");

            int columns = (int)columnsProperty.GetValue(board);
            int rows = (int)rowsProperty.GetValue(board);
            int width = (int)widthProperty.GetValue(board);
            int height = (int)heightProperty.GetValue(board);

            Assert.Equal(50, columns);
            Assert.Equal(30, rows);
            Assert.Equal(50, width);
            Assert.Equal(30, height);
        }

        [Fact]
        public void Board_Constructor_WithZeroLiveDensity_AllCellsDead()
        {
            var board = new Board(50, 30, 1, 0.0);
            var cellsField = board.GetType().GetField("Cells", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var cells = (Cell[,])cellsField.GetValue(board);

            foreach (var cell in cells)
                Assert.False(cell.IsAlive);
        }

        [Fact]
        public void Board_Constructor_WithFullLiveDensity_AllCellsAlive()
        {
            var board = new Board(10, 10, 1, 1.0);
            var cellsField = board.GetType().GetField("Cells", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var cells = (Cell[,])cellsField.GetValue(board);

            foreach (var cell in cells)
                Assert.True(cell.IsAlive);
        }

        [Fact]
        public void Board_ConnectNeighbors_EachCellHas8Neighbors()
        {
            var board = new Board(10, 10, 1, 0.0);
            var cellsField = board.GetType().GetField("Cells", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var cells = (Cell[,])cellsField.GetValue(board);

            foreach (var cell in cells)
                Assert.Equal(8, cell.neighbors.Count);
        }

        [Fact]
        public void Board_GetLiveCellCount_ReturnsCorrectNumber()
        {
            var board = new Board(10, 10, 1, 0.0);
            var cellsField = board.GetType().GetField("Cells", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var cells = (Cell[,])cellsField.GetValue(board);

            cells[0, 0].IsAlive = true;
            cells[1, 1].IsAlive = true;
            cells[2, 2].IsAlive = true;
            cells[3, 3].IsAlive = true;
            cells[4, 4].IsAlive = true;

            var method = board.GetType().GetMethod("GetLiveCellCount", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            int liveCount = (int)method.Invoke(board, null);

            Assert.Equal(5, liveCount);
        }

        [Fact]
        public void Board_Advance_BlinkerPattern_Oscillates()
        {
            var board = new Board(5, 5, 1, 0.0);
            var cellsField = board.GetType().GetField("Cells", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var cells = (Cell[,])cellsField.GetValue(board);

            cells[1, 2].IsAlive = true;
            cells[2, 2].IsAlive = true;
            cells[3, 2].IsAlive = true;

            Assert.True(cells[1, 2].IsAlive);
            Assert.True(cells[2, 2].IsAlive);
            Assert.True(cells[3, 2].IsAlive);

            var advanceMethod = board.GetType().GetMethod("Advance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            advanceMethod.Invoke(board, null);

            Assert.True(cells[2, 1].IsAlive);
            Assert.True(cells[2, 2].IsAlive);
            Assert.True(cells[2, 3].IsAlive);
            Assert.False(cells[1, 2].IsAlive);
            Assert.False(cells[3, 2].IsAlive);

            advanceMethod.Invoke(board, null);

            Assert.True(cells[1, 2].IsAlive);
            Assert.True(cells[2, 2].IsAlive);
            Assert.True(cells[3, 2].IsAlive);
        }

        [Fact]
        public void Board_FindClusters_IdentifiesIsolatedClusters()
        {
            var board = new Board(10, 10, 1, 0.0);
            var cellsField = board.GetType().GetField("Cells", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var cells = (Cell[,])cellsField.GetValue(board);

            cells[0, 0].IsAlive = true;
            cells[1, 0].IsAlive = true;
            cells[0, 1].IsAlive = true;
            cells[1, 1].IsAlive = true;

            cells[5, 5].IsAlive = true;

            var method = board.GetType().GetMethod("FindClusters", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var clusters = (List<Cluster>)method.Invoke(board, null);

            Assert.Equal(2, clusters.Count);
            Assert.Equal(4, clusters[0].Cells.Count);
            Assert.Equal(1, clusters[1].Cells.Count);
        }

        [Fact]
        public void Board_SaveAndLoad_PreservesState()
        {
            var board = new Board(10, 10, 1, 0.5);
            string testFile = "test_save_load.txt";

            var saveMethod = board.GetType().GetMethod("SaveToFile", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            saveMethod.Invoke(board, new object[] { testFile });

            var loadedBoard = new Board(10, 10, 1, 0.0);
            var loadMethod = loadedBoard.GetType().GetMethod("LoadFromFile", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            loadMethod.Invoke(loadedBoard, new object[] { testFile });

            var cellsField = board.GetType().GetField("Cells", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var originalCells = (Cell[,])cellsField.GetValue(board);
            var loadedCells = (Cell[,])cellsField.GetValue(loadedBoard);

            for (int x = 0; x < 10; x++)
                for (int y = 0; y < 10; y++)
                    Assert.Equal(originalCells[x, y].IsAlive, loadedCells[x, y].IsAlive);

            if (File.Exists(testFile))
                File.Delete(testFile);
        }

        [Fact]
        public void Board_StableBlock_DoesNotChange()
        {
            var board = new Board(5, 5, 1, 0.0);
            var cellsField = board.GetType().GetField("Cells", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var cells = (Cell[,])cellsField.GetValue(board);

            cells[1, 1].IsAlive = true;
            cells[2, 1].IsAlive = true;
            cells[1, 2].IsAlive = true;
            cells[2, 2].IsAlive = true;

            var beforeState = GetStateCopy(board);
            var advanceMethod = board.GetType().GetMethod("Advance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            advanceMethod.Invoke(board, null);
            var afterState = GetStateCopy(board);

            Assert.True(StatesAreEqual(beforeState, afterState));
        }

        // ========== ТЕСТЫ НА CLUSTER ==========

        [Fact]
        public void Cluster_GetNormalizedMatrix_ReturnsCorrectMatrix()
        {
            var cluster = new Cluster();
            cluster.Cells.Add((5, 5));
            cluster.Cells.Add((6, 5));
            cluster.Cells.Add((5, 6));
            cluster.Cells.Add((6, 6));

            var method = cluster.GetType().GetMethod("GetNormalizedMatrix", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var matrix = (bool[,])method.Invoke(cluster, null);

            Assert.Equal(2, matrix.GetLength(0));
            Assert.Equal(2, matrix.GetLength(1));
            Assert.True(matrix[0, 0] && matrix[1, 0] && matrix[0, 1] && matrix[1, 1]);
        }

        [Fact]
        public void Cluster_MatchesPattern_BlockPattern_ReturnsTrue()
        {
            var cluster = new Cluster();
            cluster.Cells.Add((5, 5));
            cluster.Cells.Add((6, 5));
            cluster.Cells.Add((5, 6));
            cluster.Cells.Add((6, 6));

            var blockPattern = new bool[,] { { true, true }, { true, true } };
            var method = cluster.GetType().GetMethod("MatchesPattern", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            bool result = (bool)method.Invoke(cluster, new object[] { blockPattern });

            Assert.True(result);
        }

        [Fact]
        public void Cluster_MatchesPatternWithSymmetries_RotatedBlock_ReturnsTrue()
        {
            var cluster = new Cluster();
            cluster.Cells.Add((5, 5));
            cluster.Cells.Add((6, 5));
            cluster.Cells.Add((5, 6));
            cluster.Cells.Add((6, 6));

            var blockPattern = new bool[,] { { true, true }, { true, true } };
            var method = cluster.GetType().GetMethod("MatchesPatternWithSymmetries", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            bool result = (bool)method.Invoke(cluster, new object[] { blockPattern });

            Assert.True(result);
        }

        // ========== ТЕСТЫ НА ЭКСПЕРИМЕНТЫ ==========

        [Fact]
        public void StabilityExperiment_MeasureStability_ReturnsValidResult()
        {
            var board = new Board(10, 10, 1, 0.0);
            var cellsField = board.GetType().GetField("Cells", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var cells = (Cell[,])cellsField.GetValue(board);

            cells[1, 1].IsAlive = true;
            cells[2, 1].IsAlive = true;
            cells[1, 2].IsAlive = true;
            cells[2, 2].IsAlive = true;

            int initialCount = board.GetLiveCellCount();
            var advanceMethod = board.GetType().GetMethod("Advance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            for (int i = 0; i < 25; i++)
                advanceMethod.Invoke(board, null);

            Assert.Equal(initialCount, board.GetLiveCellCount());
        }

        [Fact]
        public void Board_Randomize_RespectsLiveDensityApproximately()
        {
            double density = 0.3;
            var board = new Board(100, 100, 1, density);
            int liveCount = board.GetLiveCellCount();
            double actualDensity = liveCount / (double)(100 * 100);

            Assert.InRange(actualDensity, density - 0.05, density + 0.05);
        }

        [Fact]
        public void Board_Advance_WithAllDead_StaysAllDead()
        {
            var board = new Board(10, 10, 1, 0.0);
            var advanceMethod = board.GetType().GetMethod("Advance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            advanceMethod.Invoke(board, null);
            Assert.Equal(0, board.GetLiveCellCount());
        }

        [Fact]
        public void SimulationSettings_CanBeSerialized()
        {
            var settings = new SimulationSettings
            {
                Width = 100,
                Height = 50,
                CellSize = 2,
                LiveDensity = 0.25,
                DelayMs = 100
            };

            var json = System.Text.Json.JsonSerializer.Serialize(settings);
            var deserialized = System.Text.Json.JsonSerializer.Deserialize<SimulationSettings>(json);

            Assert.Equal(settings.Width, deserialized.Width);
            Assert.Equal(settings.Height, deserialized.Height);
            Assert.Equal(settings.CellSize, deserialized.CellSize);
            Assert.Equal(settings.LiveDensity, deserialized.LiveDensity);
            Assert.Equal(settings.DelayMs, deserialized.DelayMs);
        }

        [Fact]
        public void Board_Advance_UpdatesAllCellsCorrectly()
        {
            var board = new Board(3, 3, 1, 0.0);
            var cellsField = board.GetType().GetField("Cells", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var cells = (Cell[,])cellsField.GetValue(board);

            // Создаём паттерн "жужжалка" (Beehive)
            cells[0, 1].IsAlive = true;
            cells[1, 0].IsAlive = true;
            cells[1, 2].IsAlive = true;
            cells[2, 1].IsAlive = true;

            var advanceMethod = board.GetType().GetMethod("Advance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            advanceMethod.Invoke(board, null);

            // Жужжалка стабильна
            Assert.Equal(4, board.GetLiveCellCount());
        }

        [Fact]
        public void Cluster_EmptyCluster_ReturnsZeroDimensions()
        {
            var cluster = new Cluster();
            var getWidthMethod = cluster.GetType().GetMethod("GetWidth", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var getHeightMethod = cluster.GetType().GetMethod("GetHeight", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            int width = (int)getWidthMethod.Invoke(cluster, null);
            int height = (int)getHeightMethod.Invoke(cluster, null);

            Assert.Equal(0, width);
            Assert.Equal(0, height);
        }

        [Fact]
        public void Cluster_GetNormalizedMatrix_EmptyCluster_ReturnsEmptyMatrix()
        {
            var cluster = new Cluster();
            var method = cluster.GetType().GetMethod("GetNormalizedMatrix", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var matrix = (bool[,])method.Invoke(cluster, null);

            Assert.Equal(0, matrix.GetLength(0));
            Assert.Equal(0, matrix.GetLength(1));
        }

        // ========== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ==========

        private bool[,] GetStateCopy(Board board)
        {
            var cellsField = board.GetType().GetField("Cells", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var cells = (Cell[,])cellsField.GetValue(board);

            var state = new bool[board.Columns, board.Rows];
            for (int x = 0; x < board.Columns; x++)
                for (int y = 0; y < board.Rows; y++)
                    state[x, y] = cells[x, y].IsAlive;
            return state;
        }

        private bool StatesAreEqual(bool[,] state1, bool[,] state2)
        {
            if (state1.GetLength(0) != state2.GetLength(0) ||
                state1.GetLength(1) != state2.GetLength(1))
                return false;

            for (int x = 0; x < state1.GetLength(0); x++)
                for (int y = 0; y < state1.GetLength(1); y++)
                    if (state1[x, y] != state2[x, y])
                        return false;
            return true;
        }
    }
}
