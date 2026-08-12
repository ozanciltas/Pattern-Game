using System;
using PatternGame.Core.Randomness;
using PatternGame.Grid;

namespace PatternGame.Gameplay.Levels
{
    public sealed class LevelGenerator
    {
        public LevelData Generate(int seed, in LevelGenerationSettings settings)
        {
            var random = new DeterministicRandom(seed);

            GridMask targetMask = GrowConnectedShape(random, settings);
            GridMask normalizedShape = Normalize(targetMask, out int solutionColumn, out int solutionRow);

            ChooseSpawnPlacement(
                random,
                normalizedShape,
                solutionColumn,
                solutionRow,
                settings.RequiredSpawnDistance,
                out int spawnColumn,
                out int spawnRow);

            GridMask wallMask = ScatterWallCells(random, targetMask, settings);

            return new LevelData(
                wallMask,
                normalizedShape,
                solutionColumn,
                solutionRow,
                spawnColumn,
                spawnRow,
                seed);
        }

        static GridMask GrowConnectedShape(DeterministicRandom random, in LevelGenerationSettings settings)
        {
            ChooseGrowthBox(
                random,
                settings,
                out int boxColumn,
                out int boxRow,
                out int boxWidth,
                out int boxHeight);

            int startColumn = boxColumn + random.NextInt(boxWidth);
            int startRow = boxRow + random.NextInt(boxHeight);

            GridMask shape = GridMask.Empty.WithCell(startColumn, startRow);

            Span<int> frontierIndices = stackalloc int[GridMask.CellCount];
            Span<int> frontierNeighbourCounts = stackalloc int[GridMask.CellCount];

            while (shape.Count < settings.PatternCellCount)
            {
                int frontierCount = CollectFrontier(
                    shape,
                    boxColumn,
                    boxRow,
                    boxWidth,
                    boxHeight,
                    frontierIndices,
                    frontierNeighbourCounts);

                if (frontierCount == 0)
                {
                    break;
                }

                int chosenIndex = ChooseFrontierCell(
                    random,
                    settings.Compactness,
                    frontierIndices,
                    frontierNeighbourCounts,
                    frontierCount);

                shape = shape.WithCell(GridMask.ColumnOf(chosenIndex), GridMask.RowOf(chosenIndex));
            }

            return shape;
        }

        static GridMask ScatterWallCells(
            DeterministicRandom random,
            GridMask targetMask,
            in LevelGenerationSettings settings)
        {
            GridMask wall = targetMask;

            Span<int> candidateIndices = stackalloc int[GridMask.CellCount];
            Span<int> candidateNeighbourCounts = stackalloc int[GridMask.CellCount];

            for (int placed = 0; placed < settings.ExtraWallCellCount; placed++)
            {
                int candidateCount = random.NextChance(settings.Compactness)
                    ? CollectFrontier(
                        wall,
                        0,
                        0,
                        GridMask.Width,
                        GridMask.Height,
                        candidateIndices,
                        candidateNeighbourCounts)
                    : 0;

                if (candidateCount == 0)
                {
                    candidateCount = CollectFreeCells(wall, candidateIndices);
                }

                if (candidateCount == 0)
                {
                    break;
                }

                int chosenIndex = candidateIndices[random.NextInt(candidateCount)];

                wall = wall.WithCell(GridMask.ColumnOf(chosenIndex), GridMask.RowOf(chosenIndex));
            }

            return wall;
        }

        static void ChooseGrowthBox(
            DeterministicRandom random,
            in LevelGenerationSettings settings,
            out int boxColumn,
            out int boxRow,
            out int boxWidth,
            out int boxHeight)
        {
            int requiredDistance = settings.RequiredSpawnDistance;

            Span<int> candidateWidths = stackalloc int[GridMask.CellCount];
            Span<int> candidateHeights = stackalloc int[GridMask.CellCount];

            int candidateCount = 0;

            for (int width = 1; width <= GridMask.Width; width++)
            {
                for (int height = 1; height <= GridMask.Height; height++)
                {
                    if (width * height < settings.PatternCellCount)
                    {
                        continue;
                    }

                    if (LevelGenerationSettings.GuaranteedSpawnDistance(width, height) < requiredDistance)
                    {
                        continue;
                    }

                    candidateWidths[candidateCount] = width;
                    candidateHeights[candidateCount] = height;
                    candidateCount++;
                }
            }

            if (candidateCount == 0)
            {
                boxColumn = 0;
                boxRow = 0;
                boxWidth = GridMask.Width;
                boxHeight = GridMask.Height;
                return;
            }

            int pick = random.NextInt(candidateCount);

            boxWidth = candidateWidths[pick];
            boxHeight = candidateHeights[pick];
            boxColumn = random.NextInt(GridMask.Width - boxWidth + 1);
            boxRow = random.NextInt(GridMask.Height - boxHeight + 1);
        }

        static int CollectFrontier(
            GridMask shape,
            int boxColumn,
            int boxRow,
            int boxWidth,
            int boxHeight,
            Span<int> indices,
            Span<int> neighbourCounts)
        {
            int frontierCount = 0;

            for (int row = boxRow; row < boxRow + boxHeight; row++)
            {
                for (int column = boxColumn; column < boxColumn + boxWidth; column++)
                {
                    if (shape.IsOccupied(column, row))
                    {
                        continue;
                    }

                    int neighbours = CountOccupiedNeighbours(shape, column, row);

                    if (neighbours == 0)
                    {
                        continue;
                    }

                    indices[frontierCount] = GridMask.ToIndex(column, row);
                    neighbourCounts[frontierCount] = neighbours;
                    frontierCount++;
                }
            }

            return frontierCount;
        }

        static int CollectFreeCells(GridMask shape, Span<int> indices)
        {
            int freeCount = 0;

            for (int index = 0; index < GridMask.CellCount; index++)
            {
                if (shape.IsOccupied(GridMask.ColumnOf(index), GridMask.RowOf(index)))
                {
                    continue;
                }

                indices[freeCount] = index;
                freeCount++;
            }

            return freeCount;
        }

        static int CountOccupiedNeighbours(GridMask shape, int column, int row)
        {
            int neighbours = 0;

            if (GridMask.IsValidCell(column - 1, row) && shape.IsOccupied(column - 1, row))
            {
                neighbours++;
            }

            if (GridMask.IsValidCell(column + 1, row) && shape.IsOccupied(column + 1, row))
            {
                neighbours++;
            }

            if (GridMask.IsValidCell(column, row - 1) && shape.IsOccupied(column, row - 1))
            {
                neighbours++;
            }

            if (GridMask.IsValidCell(column, row + 1) && shape.IsOccupied(column, row + 1))
            {
                neighbours++;
            }

            return neighbours;
        }

        static int ChooseFrontierCell(
            DeterministicRandom random,
            float compactness,
            ReadOnlySpan<int> indices,
            ReadOnlySpan<int> neighbourCounts,
            int frontierCount)
        {
            bool preferCompactGrowth = random.NextChance(compactness);

            int targetNeighbourCount = preferCompactGrowth ? int.MinValue : int.MaxValue;

            for (int i = 0; i < frontierCount; i++)
            {
                int neighbours = neighbourCounts[i];

                if (preferCompactGrowth ? neighbours > targetNeighbourCount : neighbours < targetNeighbourCount)
                {
                    targetNeighbourCount = neighbours;
                }
            }

            int matchingCount = 0;

            for (int i = 0; i < frontierCount; i++)
            {
                if (neighbourCounts[i] == targetNeighbourCount)
                {
                    matchingCount++;
                }
            }

            int remainingPicks = random.NextInt(matchingCount);

            for (int i = 0; i < frontierCount; i++)
            {
                if (neighbourCounts[i] != targetNeighbourCount)
                {
                    continue;
                }

                if (remainingPicks == 0)
                {
                    return indices[i];
                }

                remainingPicks--;
            }

            return indices[0];
        }

        static GridMask Normalize(GridMask shape, out int minimumColumn, out int minimumRow)
        {
            minimumColumn = 0;
            minimumRow = 0;

            if (shape.IsEmpty)
            {
                return shape;
            }

            minimumColumn = GridMask.Width - 1;
            minimumRow = GridMask.Height - 1;

            for (int index = 0; index < GridMask.CellCount; index++)
            {
                int column = GridMask.ColumnOf(index);
                int row = GridMask.RowOf(index);

                if (!shape.IsOccupied(column, row))
                {
                    continue;
                }

                if (column < minimumColumn)
                {
                    minimumColumn = column;
                }

                if (row < minimumRow)
                {
                    minimumRow = row;
                }
            }

            if (!shape.TryTranslate(-minimumColumn, -minimumRow, out GridMask normalized))
            {
                minimumColumn = 0;
                minimumRow = 0;
                return shape;
            }

            return normalized;
        }

        static void ChooseSpawnPlacement(
            DeterministicRandom random,
            GridMask normalizedShape,
            int solutionColumn,
            int solutionRow,
            int requiredSpawnDistance,
            out int spawnColumn,
            out int spawnRow)
        {
            Span<int> validColumns = stackalloc int[GridMask.CellCount];
            Span<int> validRows = stackalloc int[GridMask.CellCount];

            int validCount = 0;
            int farthestDistance = -1;

            spawnColumn = solutionColumn;
            spawnRow = solutionRow;

            for (int row = 0; row < GridMask.Height; row++)
            {
                for (int column = 0; column < GridMask.Width; column++)
                {
                    if (!normalizedShape.TryTranslate(column, row, out _))
                    {
                        continue;
                    }

                    int distance = Math.Abs(column - solutionColumn) + Math.Abs(row - solutionRow);

                    if (distance > farthestDistance)
                    {
                        farthestDistance = distance;
                        spawnColumn = column;
                        spawnRow = row;
                    }

                    if (distance < requiredSpawnDistance)
                    {
                        continue;
                    }

                    validColumns[validCount] = column;
                    validRows[validCount] = row;
                    validCount++;
                }
            }

            if (validCount == 0)
            {
                return;
            }

            int pick = random.NextInt(validCount);
            spawnColumn = validColumns[pick];
            spawnRow = validRows[pick];
        }
    }
}
