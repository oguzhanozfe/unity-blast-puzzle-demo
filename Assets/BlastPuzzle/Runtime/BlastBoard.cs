using System;
using System.Collections.Generic;

namespace OguzhanOzdemir.BlastPuzzle
{
    public enum TileColor
    {
        Coral,
        Cyan,
        Violet,
        Gold,
        Mint
    }

    public enum PowerUp
    {
        None,
        HorizontalRocket,
        VerticalRocket
    }

    public readonly struct BlastResult
    {
        public bool Accepted { get; }
        public int RemovedTiles { get; }
        public int Score { get; }
        public PowerUp CreatedPowerUp { get; }

        public BlastResult(bool accepted, int removedTiles, PowerUp createdPowerUp)
        {
            Accepted = accepted;
            RemovedTiles = removedTiles;
            Score = removedTiles * removedTiles * 10;
            CreatedPowerUp = createdPowerUp;
        }
    }

    public sealed class BlastBoard
    {
        private const int ColorCount = 5;
        private readonly int[,] _colors;
        private readonly PowerUp[,] _powerUps;
        private uint _randomState;

        public int Width { get; }
        public int Height { get; }

        public BlastBoard(int width, int height, uint seed)
        {
            if (width < 2) throw new ArgumentOutOfRangeException(nameof(width));
            if (height < 2) throw new ArgumentOutOfRangeException(nameof(height));
            Width = width;
            Height = height;
            _colors = new int[width, height];
            _powerUps = new PowerUp[width, height];
            _randomState = seed == 0 ? 0x9E3779B9u : seed;

            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                _colors[x, y] = NextColor();
        }

        public BlastBoard(int[,] colors, uint seed = 1)
        {
            if (colors == null) throw new ArgumentNullException(nameof(colors));
            Width = colors.GetLength(0);
            Height = colors.GetLength(1);
            if (Width < 2 || Height < 2) throw new ArgumentException("Board must be at least 2x2.", nameof(colors));
            _colors = (int[,])colors.Clone();
            _powerUps = new PowerUp[Width, Height];
            _randomState = seed == 0 ? 0x9E3779B9u : seed;

            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                if (_colors[x, y] < 0 || _colors[x, y] >= ColorCount)
                    throw new ArgumentException("Every cell must contain a color from 0 to 4.", nameof(colors));
        }

        public TileColor ColorAt(int x, int y)
        {
            ValidateCoordinate(x, y);
            return (TileColor)_colors[x, y];
        }

        public PowerUp PowerUpAt(int x, int y)
        {
            ValidateCoordinate(x, y);
            return _powerUps[x, y];
        }

        public BlastResult TryBlast(int x, int y)
        {
            ValidateCoordinate(x, y);
            HashSet<int> removed = _powerUps[x, y] == PowerUp.None
                ? FindCluster(x, y)
                : RocketCells(x, y, _powerUps[x, y]);

            if (_powerUps[x, y] == PowerUp.None && removed.Count < 2)
                return new BlastResult(false, 0, PowerUp.None);

            PowerUp created = PowerUp.None;
            if (_powerUps[x, y] == PowerUp.None && removed.Count >= 5)
            {
                created = removed.Count % 2 == 0 ? PowerUp.VerticalRocket : PowerUp.HorizontalRocket;
                removed.Remove(Key(x, y));
            }

            foreach (int key in removed)
            {
                int removedX = key % Width;
                int removedY = key / Width;
                _colors[removedX, removedY] = -1;
                _powerUps[removedX, removedY] = PowerUp.None;
            }

            if (created != PowerUp.None)
                _powerUps[x, y] = created;

            CollapseAndRefill();
            return new BlastResult(true, removed.Count, created);
        }

        public void SetPowerUpForTesting(int x, int y, PowerUp powerUp)
        {
            ValidateCoordinate(x, y);
            _powerUps[x, y] = powerUp;
        }

        private HashSet<int> FindCluster(int startX, int startY)
        {
            int target = _colors[startX, startY];
            HashSet<int> visited = new HashSet<int>();
            Queue<int> queue = new Queue<int>();
            queue.Enqueue(Key(startX, startY));

            while (queue.Count > 0)
            {
                int key = queue.Dequeue();
                if (!visited.Add(key)) continue;
                int x = key % Width;
                int y = key / Width;
                TryQueue(x - 1, y, target, visited, queue);
                TryQueue(x + 1, y, target, visited, queue);
                TryQueue(x, y - 1, target, visited, queue);
                TryQueue(x, y + 1, target, visited, queue);
            }

            return visited;
        }

        private void TryQueue(int x, int y, int target, HashSet<int> visited, Queue<int> queue)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return;
            int key = Key(x, y);
            if (!visited.Contains(key) && _colors[x, y] == target)
                queue.Enqueue(key);
        }

        private HashSet<int> RocketCells(int originX, int originY, PowerUp powerUp)
        {
            HashSet<int> cells = new HashSet<int>();
            if (powerUp == PowerUp.HorizontalRocket)
                for (int x = 0; x < Width; x++) cells.Add(Key(x, originY));
            else
                for (int y = 0; y < Height; y++) cells.Add(Key(originX, y));
            return cells;
        }

        private void CollapseAndRefill()
        {
            for (int x = 0; x < Width; x++)
            {
                int writeY = 0;
                for (int readY = 0; readY < Height; readY++)
                {
                    if (_colors[x, readY] < 0) continue;
                    if (writeY != readY)
                    {
                        _colors[x, writeY] = _colors[x, readY];
                        _powerUps[x, writeY] = _powerUps[x, readY];
                    }
                    writeY++;
                }

                for (int y = writeY; y < Height; y++)
                {
                    _colors[x, y] = NextColor();
                    _powerUps[x, y] = PowerUp.None;
                }
            }
        }

        private int NextColor()
        {
            _randomState ^= _randomState << 13;
            _randomState ^= _randomState >> 17;
            _randomState ^= _randomState << 5;
            return (int)(_randomState % ColorCount);
        }

        private int Key(int x, int y) => (y * Width) + x;

        private void ValidateCoordinate(int x, int y)
        {
            if (x < 0 || x >= Width) throw new ArgumentOutOfRangeException(nameof(x));
            if (y < 0 || y >= Height) throw new ArgumentOutOfRangeException(nameof(y));
        }
    }
}
