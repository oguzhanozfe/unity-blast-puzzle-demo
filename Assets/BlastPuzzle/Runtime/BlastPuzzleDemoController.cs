using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OguzhanOzdemir.BlastPuzzle
{
    public sealed class BlastPuzzleDemoController : MonoBehaviour
    {
        private static readonly Color Background = new Color32(7, 12, 27, 255);
        private static readonly Color Surface = new Color32(17, 26, 50, 255);
        private static readonly Color Raised = new Color32(25, 37, 69, 255);
        private static readonly Color Muted = new Color32(142, 158, 193, 255);
        private static readonly Color[] Palette = {
            new Color32(255, 100, 118, 255),
            new Color32(45, 210, 219, 255),
            new Color32(142, 88, 255, 255),
            new Color32(255, 190, 70, 255),
            new Color32(74, 222, 149, 255)
        };

        private readonly List<Image> _tiles = new List<Image>();
        private readonly List<Text> _tileLabels = new List<Text>();
        private BlastBoard _board;
        private Text _scoreText;
        private Text _movesText;
        private Text _statusText;
        private int _score;
        private int _moves;
        private uint _seed = 20260908;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureDemoExists()
        {
            if (FindObjectOfType<BlastPuzzleDemoController>() == null)
                new GameObject("Blast Puzzle Demo").AddComponent<BlastPuzzleDemoController>();
        }

        private void Awake()
        {
            Application.targetFrameRate = 60;
            BuildInterface();
            StartNewBoard();
            StartCoroutine(CaptureScreenshotIfRequested());
        }

        private void BuildInterface()
        {
            if (FindObjectOfType<EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                eventSystem.transform.SetParent(transform);
            }

            Canvas canvas = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))
                .GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.transform.SetParent(transform);
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1180, 720);
            scaler.matchWidthOrHeight = .5f;

            RectTransform root = Panel(canvas.transform, "Background", Background, Vector2.zero, Vector2.one);
            RectTransform content = Panel(root, "Content", Color.clear, new Vector2(.055f, .065f), new Vector2(.945f, .94f));

            Text(content, "PLAYABLE SYSTEMS DEMO", 14, Palette[1], TextAnchor.MiddleLeft,
                new Vector2(0, .92f), new Vector2(.55f, 1), FontStyle.Bold);
            Text(content, "Blast Puzzle", 42, Color.white, TextAnchor.MiddleLeft,
                new Vector2(0, .80f), new Vector2(.55f, .94f), FontStyle.Bold);
            Text(content, "Tap connected colors. Larger groups create line-clearing rockets.", 17, Muted,
                TextAnchor.UpperLeft, new Vector2(0, .73f), new Vector2(.66f, .82f));

            RectTransform boardFrame = Panel(content, "Board Frame", Surface, new Vector2(0, 0), new Vector2(.63f, .70f));
            RectTransform boardGrid = Panel(boardFrame, "Board", Color.clear, new Vector2(.06f, .055f), new Vector2(.94f, .945f));
            GridLayoutGroup grid = boardGrid.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 8;
            grid.cellSize = new Vector2(61, 52);
            grid.spacing = new Vector2(7, 7);
            grid.childAlignment = TextAnchor.MiddleCenter;

            for (int displayY = 7; displayY >= 0; displayY--)
            for (int x = 0; x < 8; x++)
            {
                int cellX = x;
                int cellY = displayY;
                GameObject tile = new GameObject($"Tile {cellX},{cellY}", typeof(RectTransform), typeof(Image), typeof(Button));
                tile.transform.SetParent(boardGrid, false);
                Image image = tile.GetComponent<Image>();
                Button button = tile.GetComponent<Button>();
                button.targetGraphic = image;
                button.onClick.AddListener(() => SelectTile(cellX, cellY));
                ColorBlock colors = button.colors;
                colors.highlightedColor = Color.white;
                colors.pressedColor = new Color(.75f, .75f, .75f, 1);
                colors.colorMultiplier = 1;
                button.colors = colors;
                Outline outline = tile.AddComponent<Outline>();
                outline.effectColor = new Color(1, 1, 1, .08f);
                outline.effectDistance = new Vector2(2, -2);
                _tiles.Add(image);
                _tileLabels.Add(Text(tile.transform, "", 17, Color.white, TextAnchor.MiddleCenter,
                    Vector2.zero, Vector2.one, FontStyle.Bold));
            }

            RectTransform side = Panel(content, "Product Panel", Raised, new Vector2(.66f, 0), new Vector2(1, 1));
            Text(side, "RUN SNAPSHOT", 13, Muted, TextAnchor.MiddleLeft,
                new Vector2(.085f, .88f), new Vector2(.92f, .97f), FontStyle.Bold);
            _scoreText = Text(side, "", 34, Color.white, TextAnchor.MiddleLeft,
                new Vector2(.085f, .71f), new Vector2(.92f, .88f), FontStyle.Bold);
            _movesText = Text(side, "", 16, Muted, TextAnchor.MiddleLeft,
                new Vector2(.085f, .63f), new Vector2(.92f, .73f));

            Stat(side, "Goal", "4,000 pts", .49f);
            Stat(side, "Rule", "2+ connected", .37f);
            Stat(side, "Power-up", "5+ tiles", .25f);
            Button(side, "New deterministic board", new Vector2(.085f, .11f), new Vector2(.915f, .20f), Palette[2], StartNewBoard);
            _statusText = Text(side, "Choose a connected group to begin.", 14, Muted, TextAnchor.UpperLeft,
                new Vector2(.085f, .015f), new Vector2(.92f, .10f));
        }

        private static void Stat(Transform parent, string label, string value, float y)
        {
            Text(parent, label.ToUpperInvariant(), 12, Muted, TextAnchor.MiddleLeft,
                new Vector2(.085f, y), new Vector2(.47f, y + .09f), FontStyle.Bold);
            Text(parent, value, 16, Color.white, TextAnchor.MiddleRight,
                new Vector2(.45f, y), new Vector2(.915f, y + .09f), FontStyle.Bold);
        }

        private void StartNewBoard()
        {
            _board = new BlastBoard(8, 8, _seed++);
            _score = 0;
            _moves = 20;
            if (_statusText != null) _statusText.text = "Choose a connected group to begin.";
            Refresh();
        }

        private void SelectTile(int x, int y)
        {
            if (_moves <= 0)
            {
                _statusText.text = "Run complete. Start a new board to continue.";
                return;
            }

            BlastResult result = _board.TryBlast(x, y);
            if (!result.Accepted)
            {
                _statusText.text = "That tile has no matching neighbor.";
                return;
            }

            _moves--;
            _score += result.Score;
            _statusText.text = result.CreatedPowerUp == PowerUp.None
                ? $"Cleared {result.RemovedTiles} tiles for {result.Score:N0} points."
                : $"Created a {RocketName(result.CreatedPowerUp)} rocket.";
            Refresh();
        }

        private void Refresh()
        {
            if (_board == null || _tiles.Count == 0) return;
            int index = 0;
            for (int y = 7; y >= 0; y--)
            for (int x = 0; x < 8; x++)
            {
                _tiles[index].color = Palette[(int)_board.ColorAt(x, y)];
                PowerUp powerUp = _board.PowerUpAt(x, y);
                _tileLabels[index].text = powerUp == PowerUp.HorizontalRocket ? "H" :
                    powerUp == PowerUp.VerticalRocket ? "V" : "";
                index++;
            }
            _scoreText.text = $"{_score:N0} pts";
            _movesText.text = $"{_moves} moves remaining";
        }

        private static string RocketName(PowerUp powerUp) =>
            powerUp == PowerUp.HorizontalRocket ? "horizontal" : "vertical";

        private static IEnumerator CaptureScreenshotIfRequested()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            int flag = Array.IndexOf(arguments, "-capturePath");
            if (flag < 0 || flag + 1 >= arguments.Length) yield break;

            string capturePath = Path.GetFullPath(arguments[flag + 1]);
            string directory = Path.GetDirectoryName(capturePath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            yield return new WaitForSecondsRealtime(1f);
            yield return new WaitForEndOfFrame();
            if (File.Exists(capturePath)) File.Delete(capturePath);
            ScreenCapture.CaptureScreenshot(capturePath);
        }

        private static RectTransform Panel(Transform parent, string name, Color color, Vector2 min, Vector2 max)
        {
            GameObject value = new GameObject(name, typeof(RectTransform), typeof(Image));
            value.transform.SetParent(parent, false);
            RectTransform rect = value.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            value.GetComponent<Image>().color = color;
            return rect;
        }

        private static Text Text(Transform parent, string value, int size, Color color, TextAnchor alignment,
            Vector2 min, Vector2 max, FontStyle style = FontStyle.Normal)
        {
            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static void Button(Transform parent, string label, Vector2 min, Vector2 max, Color color,
            UnityEngine.Events.UnityAction action)
        {
            RectTransform rect = Panel(parent, label, color, min, max);
            Button button = rect.gameObject.AddComponent<Button>();
            button.onClick.AddListener(action);
            ColorBlock colors = button.colors;
            colors.highlightedColor = Color.Lerp(color, Color.white, .14f);
            colors.pressedColor = Color.Lerp(color, Color.black, .18f);
            button.colors = colors;
            Text(rect, label, 15, Color.white, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, FontStyle.Bold);
        }
    }
}
