using NUnit.Framework;

namespace OguzhanOzdemir.BlastPuzzle.Tests
{
    public sealed class BlastBoardTests
    {
        [Test]
        public void Singleton_IsRejectedWithoutMutation()
        {
            int[,] cells = {
                { 1, 2, 3 },
                { 4, 0, 1 },
                { 2, 3, 4 }
            };
            BlastBoard board = new BlastBoard(cells);

            BlastResult result = board.TryBlast(1, 1);

            Assert.That(result.Accepted, Is.False);
            Assert.That(board.ColorAt(1, 1), Is.EqualTo(TileColor.Coral));
        }

        [Test]
        public void Blast_RemovesOnlyOrthogonallyConnectedCluster()
        {
            int[,] cells = {
                { 0, 0, 2 },
                { 0, 1, 3 },
                { 4, 2, 0 }
            };
            BlastBoard board = new BlastBoard(cells, 17);

            BlastResult result = board.TryBlast(0, 0);

            Assert.That(result.Accepted, Is.True);
            Assert.That(result.RemovedTiles, Is.EqualTo(3));
            Assert.That(result.Score, Is.EqualTo(90));
        }

        [Test]
        public void CollapseAndRefill_LeavesEveryCellPlayable()
        {
            int[,] cells = {
                { 0, 0, 1 },
                { 0, 2, 3 },
                { 4, 1, 2 }
            };
            BlastBoard board = new BlastBoard(cells, 88);

            board.TryBlast(0, 0);

            for (int y = 0; y < board.Height; y++)
            for (int x = 0; x < board.Width; x++)
                Assert.That((int)board.ColorAt(x, y), Is.InRange(0, 4));
        }

        [Test]
        public void Refill_IsDeterministicForSeedAndInput()
        {
            int[,] cells = {
                { 0, 0, 1 },
                { 0, 2, 3 },
                { 4, 1, 2 }
            };
            BlastBoard first = new BlastBoard(cells, 1234);
            BlastBoard second = new BlastBoard(cells, 1234);

            first.TryBlast(0, 0);
            second.TryBlast(0, 0);

            for (int y = 0; y < first.Height; y++)
            for (int x = 0; x < first.Width; x++)
                Assert.That(first.ColorAt(x, y), Is.EqualTo(second.ColorAt(x, y)));
        }

        [Test]
        public void LargeCluster_CreatesRocketAndPreservesTriggerTile()
        {
            int[,] cells = {
                { 0, 0, 0 },
                { 0, 0, 0 },
                { 0, 0, 0 }
            };
            BlastBoard board = new BlastBoard(cells, 5);

            BlastResult result = board.TryBlast(1, 0);

            Assert.That(result.RemovedTiles, Is.EqualTo(8));
            Assert.That(result.CreatedPowerUp, Is.EqualTo(PowerUp.HorizontalRocket));
            Assert.That(board.PowerUpAt(1, 0), Is.EqualTo(PowerUp.HorizontalRocket));
        }

        [Test]
        public void Rocket_ClearsItsEntireLine()
        {
            int[,] cells = {
                { 0, 1, 2 },
                { 3, 4, 0 },
                { 1, 2, 3 }
            };
            BlastBoard board = new BlastBoard(cells, 5);
            board.SetPowerUpForTesting(1, 1, PowerUp.VerticalRocket);

            BlastResult result = board.TryBlast(1, 1);

            Assert.That(result.Accepted, Is.True);
            Assert.That(result.RemovedTiles, Is.EqualTo(3));
        }
    }
}

