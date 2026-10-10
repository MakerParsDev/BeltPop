using System;
using System.Collections.Generic;

namespace BeltPop.Core
{
    public enum Direction { North, East, South, West }
    public enum CargoColor { Red, Blue }
    public enum TileKind { Empty, Wall, Source, Belt, Junction, Sink }
    public enum GamePhase { Preview, Running, Paused, Success, Failure, Stalled }

    /// <summary>A level is immutable after construction. Coordinates grow to the right and upwards.</summary>
    public sealed class Tile
    {
        public readonly int X;
        public readonly int Y;
        public readonly TileKind Kind;
        public readonly Direction Input;
        public readonly Direction Output;
        public readonly Direction[] Options;
        public readonly CargoColor[] Manifest;
        public readonly CargoColor Accepts;

        public Tile(int x, int y, TileKind kind, Direction input = Direction.West,
            Direction output = Direction.East, Direction[] options = null,
            CargoColor[] manifest = null, CargoColor accepts = CargoColor.Red)
        {
            X = x; Y = y; Kind = kind; Input = input; Output = output;
            Options = options == null ? new Direction[0] : (Direction[])options.Clone();
            Manifest = manifest == null ? new CargoColor[0] : (CargoColor[])manifest.Clone();
            Accepts = accepts;
        }
    }

    public sealed class Level
    {
        public readonly int Number;
        public readonly int Width;
        public readonly int Height;
        public readonly Tile[] Tiles;
        public int TotalCargo { get; private set; }

        public Level(int number, int width, int height, Tile[] tiles)
        {
            if (width < 3 || height < 3 || tiles == null || tiles.Length != width * height)
                throw new ArgumentException("Level dimensions and tile count must agree.");
            Number = number; Width = width; Height = height;
            Tiles = (Tile[])tiles.Clone();
            int sources = 0, sinks = 0;
            for (int i = 0; i < Tiles.Length; i++)
            {
                Tile tile = Tiles[i];
                if (tile == null || tile.X != i % width || tile.Y != i / width)
                    throw new ArgumentException("Tiles must match their stable row-major grid index.");
                if (tile.Kind == TileKind.Source)
                {
                    sources++;
                    TotalCargo += tile.Manifest.Length;
                }
                if (tile.Kind == TileKind.Sink) sinks++;
                if (tile.Kind == TileKind.Junction && (tile.Options.Length < 2 || tile.Options.Length > 3))
                    throw new ArgumentException("A junction needs two or three output directions.");
            }
            if (sources < 1 || sinks < 1 || TotalCargo < 1)
                throw new ArgumentException("Level needs cargo, source and destination.");
        }
        public int At(int x, int y) { return x < 0 || y < 0 || x >= Width || y >= Height ? -1 : y * Width + x; }
    }

    public sealed class Cargo
    {
        public readonly int Id;
        public readonly CargoColor Color;
        public Cargo(int id, CargoColor color) { Id = id; Color = color; }
    }

    public sealed class TickReport
    {
        public int Moved;
        public int Spawned;
        public int Delivered;
        public int Wrong;
        public bool Jammed;
        public GamePhase Phase;
    }

    /// <summary>Deterministic rules engine: no MonoBehaviour, time, renderer or random inputs.</summary>
    public sealed class Simulation
    {
        private struct Move
        {
            public int From;
            public int To;
            public Cargo Cargo;
        }

        public Level Level { get; private set; }
        public GamePhase Phase { get; private set; }
        public int Correct { get; private set; }
        public int Wrong { get; private set; }
        public int Delivered { get { return Correct + Wrong; } }
        public int ManualSwitches { get; private set; }
        public int Ticks { get; private set; }
        public int Score { get { return Math.Max(0, 100 * Correct - 30 * Wrong - 2 * ManualSwitches); } }
        public int Stars
        {
            get
            {
                if (Phase != GamePhase.Success) return 0;
                if (Correct == Level.TotalCargo) return 3;
                if (Correct >= (int)Math.Ceiling(Level.TotalCargo * 0.90)) return 2;
                return 1;
            }
        }

        private Cargo[] occupants;
        private int[] sourceCursors;
        private int[] selected;
        private readonly Queue<int> pendingSwitches = new Queue<int>();
        private int nextCargoId;
        private int noProgressTicks;

        public Simulation(Level level)
        {
            Level = level ?? throw new ArgumentNullException("level");
            Restart();
        }

        public void Restart()
        {
            occupants = new Cargo[Level.Tiles.Length];
            sourceCursors = new int[Level.Tiles.Length];
            selected = new int[Level.Tiles.Length];
            pendingSwitches.Clear();
            nextCargoId = 1;
            noProgressTicks = 0;
            Correct = Wrong = ManualSwitches = Ticks = 0;
            Phase = GamePhase.Preview;
        }

        public void Start()
        {
            if (Phase == GamePhase.Preview) Phase = GamePhase.Running;
            else if (Phase == GamePhase.Paused) Phase = GamePhase.Running;
        }
        public void Pause() { if (Phase == GamePhase.Running) Phase = GamePhase.Paused; }

        public bool QueueSwitch(int x, int y)
        {
            int index = Level.At(x, y);
            if (index < 0 || Level.Tiles[index].Kind != TileKind.Junction ||
                (Phase != GamePhase.Preview && Phase != GamePhase.Running)) return false;
            pendingSwitches.Enqueue(index);
            return true;
        }

        public int JunctionSelection(int x, int y)
        {
            int index = Level.At(x, y);
            return index < 0 ? -1 : selected[index];
        }

        public Direction CurrentOutput(int x, int y)
        {
            int index = Level.At(x, y);
            if (index < 0) throw new ArgumentOutOfRangeException("x");
            Tile tile = Level.Tiles[index];
            return tile.Kind == TileKind.Junction ? tile.Options[selected[index]] : tile.Output;
        }

        public Cargo CargoAt(int x, int y)
        {
            int index = Level.At(x, y);
            return index < 0 ? null : occupants[index];
        }

        private static Direction Opposite(Direction direction)
        {
            return (Direction)(((int)direction + 2) % 4);
        }

        private int Downstream(int index, Direction direction)
        {
            Tile tile = Level.Tiles[index];
            switch (direction)
            {
                case Direction.North: return Level.At(tile.X, tile.Y + 1);
                case Direction.East: return Level.At(tile.X + 1, tile.Y);
                case Direction.South: return Level.At(tile.X, tile.Y - 1);
                default: return Level.At(tile.X - 1, tile.Y);
            }
        }

        private bool CanEnter(int from, int to, Direction outgoing)
        {
            if (to < 0) return false;
            Tile receiver = Level.Tiles[to];
            if (receiver.Kind != TileKind.Sink &&
                receiver.Kind != TileKind.Belt && receiver.Kind != TileKind.Junction) return false;
            return receiver.Input == Opposite(outgoing);
        }

        private bool CanSpawn(int index)
        {
            Direction outDir = Level.Tiles[index].Output;
            int to = Downstream(index, outDir);
            return CanEnter(index, to, outDir) && occupants[to] == null;
        }

        public TickReport Tick()
        {
            TickReport report = new TickReport { Phase = Phase };
            if (Phase != GamePhase.Running) return report;
            Ticks++;

            while (pendingSwitches.Count > 0)
            {
                int index = pendingSwitches.Dequeue();
                selected[index] = (selected[index] + 1) % Level.Tiles[index].Options.Length;
                ManualSwitches++;
            }

            // Movement intentions read the START-of-tick slots; vacated slots remain blocked.
            var intents = new List<Move>();
            var claimed = new HashSet<int>();
            for (int from = 0; from < occupants.Length; from++)
            {
                Cargo cargo = occupants[from];
                if (cargo == null) continue;
                Tile tile = Level.Tiles[from];
                Direction output = tile.Kind == TileKind.Junction ? tile.Options[selected[from]] : tile.Output;
                int to = Downstream(from, output);
                if (!CanEnter(from, to, output) ||
                    (Level.Tiles[to].Kind != TileKind.Sink && occupants[to] != null) ||
                    !claimed.Add(to))
                {
                    report.Jammed = true;
                    continue;
                }
                intents.Add(new Move { From = from, To = to, Cargo = cargo });
            }

            foreach (Move move in intents)
            {
                occupants[move.From] = null;
                Tile destination = Level.Tiles[move.To];
                if (destination.Kind == TileKind.Sink)
                {
                    if (move.Cargo.Color == destination.Accepts) { Correct++; report.Delivered++; }
                    else { Wrong++; report.Wrong++; }
                }
                else occupants[move.To] = move.Cargo;
                report.Moved++;
            }

            // Spawn after commit; newly spawned cargo does not move until the next tick.
            for (int index = 0; index < Level.Tiles.Length; index++)
            {
                Tile tile = Level.Tiles[index];
                if (tile.Kind != TileKind.Source || occupants[index] != null ||
                    sourceCursors[index] >= tile.Manifest.Length || !CanSpawn(index)) continue;
                occupants[index] = new Cargo(nextCargoId++, tile.Manifest[sourceCursors[index]++]);
                report.Spawned++;
            }

            bool finished = true;
            for (int i = 0; i < Level.Tiles.Length; i++)
            {
                if (occupants[i] != null ||
                    (Level.Tiles[i].Kind == TileKind.Source && sourceCursors[i] < Level.Tiles[i].Manifest.Length))
                {
                    finished = false;
                    break;
                }
            }
            if (finished)
            {
                // First three tutorial levels require a perfect route.
                bool success = Correct >= (int)Math.Ceiling(Level.TotalCargo * .75)
                    && (Level.Number > 3 || Correct == Level.TotalCargo);
                Phase = success ? GamePhase.Success : GamePhase.Failure;
            }
            else
            {
                if (report.Moved + report.Spawned > 0) noProgressTicks = 0;
                else if (++noProgressTicks >= 12) Phase = GamePhase.Stalled;
            }
            report.Phase = Phase;
            return report;
        }
    }
}
