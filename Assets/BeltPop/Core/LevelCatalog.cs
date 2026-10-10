using System;
using System.Collections.Generic;

namespace BeltPop.Core
{
    /// <summary>
    /// Twelve authored routing layouts. One source, two color-coded exits, fixed manifests.
    /// Changes to geometry are level-specific; no random generation or network services.
    /// </summary>
    public static class LevelCatalog
    {
        public const int Count = 12;
        public const int BoardSize = 7;

        private enum RouteShape { Straight, RedDetour, BlueDetour, DoubleDetour }

        private sealed class Blueprint
        {
            public readonly int JunctionX, JunctionY;
            public readonly RouteShape Shape;
            public readonly CargoColor[] Manifest;
            public Blueprint(int x, int y, RouteShape shape, string manifest)
            {
                JunctionX = x; JunctionY = y; Shape = shape;
                Manifest = new CargoColor[manifest.Length];
                for (int i = 0; i < manifest.Length; i++)
                {
                    if (manifest[i] != 'R' && manifest[i] != 'B')
                        throw new ArgumentException("Unknown cargo code in level manifest.");
                    Manifest[i] = manifest[i] == 'R' ? CargoColor.Red : CargoColor.Blue;
                }
            }
        }

        // Every entry is an authored puzzle: the route family, junction position and
        // exact cargo sequence are deliberately explicit instead of procedural RNG.
        private static readonly Blueprint[] Blueprints =
        {
            new Blueprint(2, 2, RouteShape.Straight,     "RBRB"),
            new Blueprint(3, 2, RouteShape.Straight,     "RRBBRB"),
            new Blueprint(3, 3, RouteShape.Straight,     "BRBRRBBR"),
            new Blueprint(2, 3, RouteShape.RedDetour,    "BRRBBR"),
            new Blueprint(3, 2, RouteShape.RedDetour,    "RRBBRBRB"),
            new Blueprint(4, 3, RouteShape.RedDetour,    "RBBRRBBRBR"),
            new Blueprint(2, 2, RouteShape.BlueDetour,   "BBRRBRBR"),
            new Blueprint(3, 4, RouteShape.BlueDetour,   "RBBRBRRBBR"),
            new Blueprint(4, 3, RouteShape.BlueDetour,   "BRRBBRRBRBBR"),
            new Blueprint(2, 2, RouteShape.DoubleDetour, "RBRBRRBBRBBR"),
            new Blueprint(3, 3, RouteShape.DoubleDetour, "BRBBRRBRRBBRBR"),
            new Blueprint(4, 4, RouteShape.DoubleDetour, "RRBBRBRBRRBBRBRB")
        };

        private struct Cell
        {
            public readonly int X, Y;
            public Cell(int x, int y) { X = x; Y = y; }
        }

        private static Direction DirectionTo(Cell from, Cell to)
        {
            if (to.X == from.X + 1 && to.Y == from.Y) return Direction.East;
            if (to.X == from.X - 1 && to.Y == from.Y) return Direction.West;
            if (to.X == from.X && to.Y == from.Y + 1) return Direction.North;
            if (to.X == from.X && to.Y == from.Y - 1) return Direction.South;
            throw new ArgumentException("Route must consist of adjacent orthogonal tiles.");
        }

        private static void Place(Tile[] tiles, Tile newTile)
        {
            int index = newTile.Y * BoardSize + newTile.X;
            if (newTile.X < 0 || newTile.Y < 0 || newTile.X >= BoardSize || newTile.Y >= BoardSize
                || tiles[index].Kind != TileKind.Empty)
                throw new InvalidOperationException("Level paths must not intersect or leave the board.");
            tiles[index] = newTile;
        }

        private static void PlaceRoute(Tile[] tiles, List<Cell> branch,
            CargoColor color)
        {
            // The first cell is the already-created junction. Each other cell must
            // appear exactly once in only one route, with explicit incoming port.
            for (int i = 1; i < branch.Count; i++)
            {
                Cell current = branch[i];
                Direction input = DirectionTo(current, branch[i - 1]);
                if (i == branch.Count - 1)
                    Place(tiles, new Tile(current.X, current.Y, TileKind.Sink,
                        input: input, accepts: color));
                else
                    Place(tiles, new Tile(current.X, current.Y, TileKind.Belt,
                        input: input, output: DirectionTo(current, branch[i + 1])));
            }
        }

        public static Level Create(int number)
        {
            if (number < 1 || number > Count)
                throw new ArgumentOutOfRangeException("number", "Level must be between 1 and 12.");
            Blueprint blueprint = Blueprints[number - 1];
            int x = blueprint.JunctionX, y = blueprint.JunctionY;
            var tiles = new Tile[BoardSize * BoardSize];
            for (int row = 0; row < BoardSize; row++)
            for (int column = 0; column < BoardSize; column++)
                tiles[row * BoardSize + column] = new Tile(column, row, TileKind.Empty);

            // Fixed source path enters the branching junction from the west.
            Place(tiles, new Tile(0, y, TileKind.Source,
                output: Direction.East, manifest: blueprint.Manifest));
            for (int column = 1; column < x; column++)
                Place(tiles, new Tile(column, y, TileKind.Belt,
                    input: Direction.West, output: Direction.East));
            Place(tiles, new Tile(x, y, TileKind.Junction, input: Direction.West,
                options: new[] { Direction.North, Direction.East }));

            var junction = new Cell(x, y);
            var red = new List<Cell> { junction, new Cell(x, y + 1) };
            if (blueprint.Shape == RouteShape.RedDetour)
            {
                red.Add(new Cell(x + 1, y + 1));
                for (int row = y + 2; row < BoardSize; row++)
                    red.Add(new Cell(x + 1, row));
            }
            else if (blueprint.Shape == RouteShape.DoubleDetour)
            {
                red.Add(new Cell(x - 1, y + 1));
                for (int row = y + 2; row < BoardSize; row++)
                    red.Add(new Cell(x - 1, row));
            }
            else
            {
                for (int row = y + 2; row < BoardSize; row++)
                    red.Add(new Cell(x, row));
            }

            var blue = new List<Cell> { junction, new Cell(x + 1, y) };
            if (blueprint.Shape == RouteShape.BlueDetour
                || blueprint.Shape == RouteShape.DoubleDetour)
            {
                for (int row = y - 1; row >= 0; row--)
                    blue.Add(new Cell(x + 1, row));
                for (int column = x + 2; column < BoardSize; column++)
                    blue.Add(new Cell(column, 0));
            }
            else
            {
                for (int column = x + 2; column < BoardSize; column++)
                    blue.Add(new Cell(column, y));
            }

            PlaceRoute(tiles, red, CargoColor.Red);
            PlaceRoute(tiles, blue, CargoColor.Blue);
            // No fake branching destinations: every reachable path terminates at a color sink.
            return new Level(number, BoardSize, BoardSize, tiles);
        }
    }
}
