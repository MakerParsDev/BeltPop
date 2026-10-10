using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using BeltPop.Core;

namespace BeltPop.Tests
{
    public sealed class BeltPopSimulationTests
    {
        private static readonly int[] ExpectedCargo = { 4, 6, 8, 6, 8, 10, 8, 10, 12, 12, 14, 16 };

        private static Tile Junction(Level level)
        {
            foreach (Tile tile in level.Tiles)
                if (tile.Kind == TileKind.Junction) return tile;
            Assert.Fail("No junction in level " + level.Number);
            return null;
        }

        private static Simulation Solve(int number)
        {
            var game = new Simulation(LevelCatalog.Create(number));
            Tile junction = Junction(game.Level);
            game.Start();
            for (int step = 0; step < 500 && game.Phase == GamePhase.Running; step++)
            {
                Cargo cargo = game.CargoAt(junction.X, junction.Y);
                if (cargo != null)
                {
                    Direction expected = cargo.Color == CargoColor.Red
                        ? Direction.North : Direction.East;
                    if (game.CurrentOutput(junction.X, junction.Y) != expected)
                        Assert.That(game.QueueSwitch(junction.X, junction.Y), Is.True);
                }
                game.Tick();
            }
            return game;
        }

        [Test]
        public void TwelveAuthoredLevelsHaveCorrectManifestsAndGraphStructure()
        {
            Assert.That(LevelCatalog.Count, Is.EqualTo(12));
            for (int i = 1; i <= LevelCatalog.Count; i++)
            {
                Level level = LevelCatalog.Create(i);
                Assert.That(level.Width, Is.EqualTo(LevelCatalog.BoardSize));
                Assert.That(level.Height, Is.EqualTo(LevelCatalog.BoardSize));
                Assert.That(level.TotalCargo, Is.EqualTo(ExpectedCargo[i - 1]),
                    "Incorrect manifest size at level " + i);
                Assert.That(Junction(level).Options, Is.EqualTo(new[] { Direction.North, Direction.East }));
                int sources = 0, reds = 0, blues = 0;
                foreach (Tile tile in level.Tiles)
                {
                    if (tile.Kind == TileKind.Source) sources++;
                    if (tile.Kind != TileKind.Sink) continue;
                    if (tile.Accepts == CargoColor.Red) reds++;
                    else blues++;
                }
                Assert.That(sources, Is.EqualTo(1), "source count level " + i);
                Assert.That(reds, Is.EqualTo(1), "red output count level " + i);
                Assert.That(blues, Is.EqualTo(1), "blue output count level " + i);
            }
        }

        [Test]
        public void AllTwelveLayoutsAreDistinctNotJustRecoloredManifests()
        {
            var layouts = new HashSet<string>();
            for (int i = 1; i <= LevelCatalog.Count; i++)
            {
                Level level = LevelCatalog.Create(i);
                var signature = new StringBuilder();
                foreach (Tile tile in level.Tiles)
                {
                    signature.Append((int)tile.Kind).Append(':').Append((int)tile.Input)
                        .Append(':').Append((int)tile.Output).Append(';');
                }
                Assert.That(layouts.Add(signature.ToString()), Is.True,
                    "Duplicated geometry at level " + i);
            }
            Assert.That(layouts.Count, Is.EqualTo(12));
        }

        [Test]
        public void AllLevelsHaveDeterministicHundredPercentWinningInputTraces()
        {
            for (int i = 1; i <= LevelCatalog.Count; i++)
            {
                Simulation game = Solve(i);
                Assert.That(game.Phase, Is.EqualTo(GamePhase.Success), "Level " + i);
                Assert.That(game.Correct, Is.EqualTo(ExpectedCargo[i - 1]), "Level " + i);
                Assert.That(game.Wrong, Is.Zero, "Level " + i);
                Assert.That(game.Stars, Is.EqualTo(3), "Level " + i);
                Assert.That(game.Ticks, Is.LessThan(500), "Level " + i);
            }
        }

        [Test]
        public void EachLevelProducesIdenticalReplayScoreAndTickCount()
        {
            for (int i = 1; i <= LevelCatalog.Count; i++)
            {
                Simulation one = Solve(i), two = Solve(i);
                Assert.That(one.Ticks, Is.EqualTo(two.Ticks), "Level " + i);
                Assert.That(one.Score, Is.EqualTo(two.Score), "Level " + i);
                Assert.That(one.ManualSwitches, Is.EqualTo(two.ManualSwitches), "Level " + i);
            }
        }

        [Test]
        public void NoSwitchesCanNotAccidentallyWinFirstTutorial()
        {
            var game = new Simulation(LevelCatalog.Create(1));
            game.Start();
            for (int t = 0; t < 300 && game.Phase == GamePhase.Running; t++)
                game.Tick();
            Assert.That(game.Phase, Is.EqualTo(GamePhase.Failure));
            Assert.That(game.Correct, Is.EqualTo(2));
            Assert.That(game.Wrong, Is.EqualTo(2));
        }

        [Test]
        public void PausedSimulationDoesNotMoveAndRestartRestoresPreview()
        {
            var game = new Simulation(LevelCatalog.Create(1));
            game.Start();
            game.Tick();
            int t = game.Ticks;
            game.Pause();
            game.Tick();
            Assert.That(game.Ticks, Is.EqualTo(t));
            game.Restart();
            Assert.That(game.Phase, Is.EqualTo(GamePhase.Preview));
            Assert.That(game.Ticks, Is.Zero);
            Assert.That(game.Correct, Is.Zero);
            Assert.That(game.Wrong, Is.Zero);
            Assert.That(game.CargoAt(0, 2), Is.Null);
        }

        [Test]
        public void JunctionInputAppliesBeforeMovementAndInvalidTouchesAreRejected()
        {
            var game = new Simulation(LevelCatalog.Create(1));
            Assert.That(game.QueueSwitch(1, 2), Is.False);
            Assert.That(game.QueueSwitch(2, 2), Is.True);
            game.Start();
            game.Tick();
            Assert.That(game.CurrentOutput(2, 2), Is.EqualTo(Direction.East));
            Assert.That(game.ManualSwitches, Is.EqualTo(1));
        }

        [Test]
        public void InvalidLevelNumbersAndMalformedTileCoordinatesAreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => LevelCatalog.Create(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => LevelCatalog.Create(LevelCatalog.Count + 1));
            var tiles = LevelCatalog.Create(1).Tiles;
            tiles[0] = new Tile(2, 2, TileKind.Empty);
            Assert.Throws<ArgumentException>(() =>
                new Level(1, LevelCatalog.BoardSize, LevelCatalog.BoardSize, tiles));
        }

        [Test]
        public void ExitResultCannotMoveUntilRestart()
        {
            Simulation game = Solve(1);
            int ticks = game.Ticks;
            game.Tick();
            Assert.That(game.Ticks, Is.EqualTo(ticks));
            Assert.That(game.QueueSwitch(2, 2), Is.False);
            game.Restart();
            game.Start();
            Assert.That(game.Phase, Is.EqualTo(GamePhase.Running));
        }
    }
}
