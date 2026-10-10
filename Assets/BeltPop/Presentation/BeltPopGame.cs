using System;
using BeltPop.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace BeltPop.Presentation
{
    /// <summary>A lightweight, fully offline, portrait-first display for the pure C# puzzle engine.</summary>
    public sealed class BeltPopGame : MonoBehaviour
    {
        private const float TickSeconds = 0.70f;
        private const int BoardSize = LevelCatalog.BoardSize;
        private static readonly Color Ink = Hex("#23304D");
        private static readonly Color Primary = Hex("#6F5CFF");
        private static readonly Color Secondary = Hex("#4931BA");
        private static readonly Color Background = Hex("#F5F8FC");
        private static readonly Color Green = Hex("#33C88B");
        private static readonly Color Coral = Hex("#F66B68");
        private static readonly Color Blue = Hex("#53A9F9");
        private static readonly Color TileBase = Hex("#E7EBF5");
        private static readonly Color Belt = Hex("#63708D");

        private readonly Image[,] cells = new Image[BoardSize, BoardSize];
        private readonly Text[,] symbols = new Text[BoardSize, BoardSize];
        private readonly Text[,] captions = new Text[BoardSize, BoardSize];
        private readonly Button[,] tapTargets = new Button[BoardSize, BoardSize];
        private readonly Image[,] cargoIcons = new Image[BoardSize, BoardSize];
        private readonly Text[,] cargoLabels = new Text[BoardSize, BoardSize];
        private Text statusText, progressText, summaryText, hintText, levelText, runLabel;
        private Button runButton, nextButton;
        private Font font;
        private Sprite roundedSprite;
        private Simulation simulation;
        private float accumulator;
        private int currentLevel = 1;

        private static Color Hex(string html)
        {
            ColorUtility.TryParseHtmlString(html, out Color value);
            return value;
        }

        private void Awake()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            roundedSprite = CreateRoundRectSprite();
            Application.targetFrameRate = 60;
#if UNITY_ANDROID
            Screen.orientation = ScreenOrientation.Portrait;
#endif
            EnsureEventSystem();
            BuildUi();
            LoadLevel(1);
        }

        private void OnDestroy()
        {
            if (roundedSprite == null) return;
            Texture2D texture = roundedSprite.texture;
            Destroy(roundedSprite);
            if (texture != null) Destroy(texture);
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            GameObject events = new GameObject("BeltPop EventSystem",
                typeof(EventSystem), typeof(InputSystemUIInputModule));
            DontDestroyOnLoad(events);
        }

        private static Sprite CreateRoundRectSprite()
        {
            const int dim = 64, radius = 14;
            var texture = new Texture2D(dim, dim, TextureFormat.RGBA32, false)
            { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < dim; y++)
            for (int x = 0; x < dim; x++)
            {
                int cx = Math.Max(radius, Math.Min(dim - 1 - radius, x));
                int cy = Math.Max(radius, Math.Min(dim - 1 - radius, y));
                int dx = x - cx, dy = y - cy;
                texture.SetPixel(x, y, new Color(1, 1, 1,
                    dx * dx + dy * dy <= radius * radius ? 1f : 0f));
            }
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, dim, dim), new Vector2(.5f, .5f),
                100f, 0, SpriteMeshType.FullRect, new Vector4(14, 14, 14, 14));
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 size, Vector2 pos)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            RectTransform rt = gameObject.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(.5f, .5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            return rt;
        }

        private Image AddImage(string name, Transform parent, Vector2 anchor, Vector2 size,
            Vector2 pos, Color color)
        {
            RectTransform rt = Rect(name, parent, anchor, size, pos);
            Image image = rt.gameObject.AddComponent<Image>();
            image.sprite = roundedSprite;
            image.type = UnityEngine.UI.Image.Type.Sliced;
            image.color = color;
            return image;
        }

        private Text AddText(string name, Transform parent, string value, int size, Color color,
            Vector2 anchor, Vector2 bounds, Vector2 pos, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            RectTransform rt = Rect(name, parent, anchor, bounds, pos);
            Text text = rt.gameObject.AddComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private Button AddButton(string name, Transform parent, string title,
            Color color, Vector2 anchor, Vector2 size, Vector2 pos, Action onClick)
        {
            Image image = AddImage(name, parent, anchor, size, pos, color);
            Button button = image.gameObject.AddComponent<Button>();
            ColorBlock states = button.colors;
            states.normalColor = Color.white;
            states.highlightedColor = Hex("#E3DCFF");
            states.pressedColor = Hex("#BCB3FC");
            states.selectedColor = Color.white;
            button.colors = states;
            AddText(name + " Label", image.transform, title, 29, Color.white,
                new Vector2(.5f, .5f), size - new Vector2(12, 12), Vector2.zero);
            button.onClick.AddListener(() => onClick());
            return button;
        }

        private void BuildUi()
        {
            var canvasObj = new GameObject("BeltPop UI",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObj.transform.SetParent(transform, false);
            Canvas canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            AddImage("Background", canvasObj.transform, new Vector2(.5f, .5f),
                new Vector2(2400, 3200), Vector2.zero, Background);
            AddImage("Top Ribbon", canvasObj.transform, new Vector2(.5f, 1f),
                new Vector2(1040, 320), new Vector2(0, -165), Color.white);
            AddText("Logo", canvasObj.transform, "BELTPOP", 76, Primary,
                new Vector2(.5f, 1f), new Vector2(850, 104), new Vector2(0, -105));
            AddText("Subtitle", canvasObj.transform, "SORT  •  ROUTE  •  POP", 24, Ink,
                new Vector2(.5f, 1f), new Vector2(850, 52), new Vector2(0, -182));
            levelText = AddText("Level", canvasObj.transform, "", 32, Secondary,
                new Vector2(.5f, 1f), new Vector2(870, 56), new Vector2(0, -256));

            AddImage("Board Frame", canvasObj.transform, new Vector2(.5f, .5f),
                new Vector2(960, 1020), new Vector2(0, 140), Color.white);
            AddText("Board Hint", canvasObj.transform, "TAP THE PURPLE SWITCH TO ROUTE PARCELS",
                22, Ink, new Vector2(.5f, .5f), new Vector2(920, 65), new Vector2(0, 615));

            // View follows the currently loaded LevelDefinition: never cache one map's tile types.
            for (int y = 0; y < BoardSize; y++)
            for (int x = 0; x < BoardSize; x++)
            {
                int px = x, py = y;
                Vector2 position = new Vector2((x - 3) * 120, (y - 3) * 120 + 135);
                Image cell = AddImage("Tile " + x + "," + y, canvasObj.transform,
                    new Vector2(.5f, .5f), new Vector2(110, 110), position, TileBase);
                Button tap = cell.gameObject.AddComponent<Button>();
                tap.onClick.AddListener(() => Switch(px, py));
                tapTargets[x, y] = tap;
                cells[x, y] = cell;
                symbols[x, y] = AddText("Symbol", cell.transform, "", 44, Belt,
                    new Vector2(.5f, .5f), new Vector2(105, 83), new Vector2(0, 10));
                captions[x, y] = AddText("Caption", cell.transform, "", 17, Ink,
                    new Vector2(.5f, .5f), new Vector2(105, 32), new Vector2(0, -39));
                Image cargo = AddImage("Cargo", cell.transform, new Vector2(.5f, .5f),
                    new Vector2(73, 72), Vector2.zero, Coral);
                cargoIcons[x, y] = cargo;
                cargoLabels[x, y] = AddText("Cargo Letter", cargo.transform, "R", 38, Color.white,
                    new Vector2(.5f, .5f), new Vector2(69, 70), Vector2.zero);
                cargo.gameObject.SetActive(false);
            }

            statusText = AddText("Status", canvasObj.transform, "READY", 43, Ink,
                new Vector2(.5f, 0), new Vector2(920, 72), new Vector2(0, 530));
            progressText = AddText("Progress", canvasObj.transform, "", 28, Ink,
                new Vector2(.5f, 0), new Vector2(920, 59), new Vector2(0, 465));
            summaryText = AddText("Summary", canvasObj.transform, "", 28, Green,
                new Vector2(.5f, 0), new Vector2(920, 70), new Vector2(0, 402));

            runButton = AddButton("Run", canvasObj.transform, "RUN", Primary,
                new Vector2(.5f, 0), new Vector2(400, 110), new Vector2(-220, 282), ToggleRun);
            runLabel = runButton.GetComponentInChildren<Text>();
            AddButton("Retry", canvasObj.transform, "RETRY", Belt,
                new Vector2(.5f, 0), new Vector2(400, 110), new Vector2(220, 282), RestartLevel);
            nextButton = AddButton("Next", canvasObj.transform, "NEXT LEVEL  >", Green,
                new Vector2(.5f, 0), new Vector2(840, 105), new Vector2(0, 151), NextLevel);
            hintText = AddText("Hint", canvasObj.transform,
                "Tap switch • Press RUN • Route RED and BLUE", 25, Ink,
                new Vector2(.5f, 0), new Vector2(910, 60), new Vector2(0, 66));
        }

        private void SetupTiles(Level level)
        {
            for (int y = 0; y < BoardSize; y++)
            for (int x = 0; x < BoardSize; x++)
            {
                Tile tile = level.Tiles[level.At(x, y)];
                Image bg = cells[x, y];
                Text main = symbols[x, y], label = captions[x, y];
                Button tap = tapTargets[x, y];
                tap.interactable = tile.Kind == TileKind.Junction;
                label.text = "";
                main.text = "";
                main.color = Belt;
                switch (tile.Kind)
                {
                    case TileKind.Source:
                        bg.color = Hex("#FFE9AE");
                        main.text = "IN";
                        main.fontSize = 33;
                        main.color = Ink;
                        break;
                    case TileKind.Belt:
                        bg.color = TileBase;
                        main.text = Arrow(tile.Output);
                        main.fontSize = 50;
                        break;
                    case TileKind.Junction:
                        bg.color = Primary;
                        main.color = Color.white;
                        main.fontSize = 51;
                        label.color = Color.white;
                        label.text = "TAP";
                        break;
                    case TileKind.Sink:
                        bg.color = tile.Accepts == CargoColor.Red ? Hex("#FFE1E4") : Hex("#DBEDFF");
                        main.color = tile.Accepts == CargoColor.Red ? Coral : Blue;
                        main.text = tile.Accepts == CargoColor.Red ? "RED" : "BLUE";
                        main.fontSize = 29;
                        label.color = Ink;
                        label.text = "EXIT";
                        break;
                    case TileKind.Wall:
                        bg.color = Hex("#B0B9CA");
                        break;
                    default:
                        bg.color = Hex("#F2F4F9");
                        break;
                }
            }
        }

        private static string Arrow(Direction direction)
        {
            switch (direction)
            {
                case Direction.North: return "^";
                case Direction.East: return ">";
                case Direction.South: return "v";
                default: return "<";
            }
        }

        private void LoadLevel(int number)
        {
            currentLevel = number;
            simulation = new Simulation(LevelCatalog.Create(number));
            accumulator = 0f;
            levelText.text = "LEVEL " + number + " / " + LevelCatalog.Count;
            hintText.text = "Watch each parcel and tap the purple switch.";
            SetupTiles(simulation.Level);
            Refresh();
        }

        private void ToggleRun()
        {
            if (simulation.Phase == GamePhase.Running) simulation.Pause();
            else simulation.Start();
            Refresh();
        }

        private void Switch(int x, int y)
        {
            if (simulation.QueueSwitch(x, y))
                hintText.text = "Switch queued for the next tick.";
            Refresh();
        }

        private void RestartLevel()
        {
            simulation.Restart();
            accumulator = 0f;
            hintText.text = "Fresh attempt. Route every parcel to its color.";
            Refresh();
        }

        private void NextLevel()
        {
            if (simulation.Phase != GamePhase.Success || currentLevel >= LevelCatalog.Count) return;
            LoadLevel(currentLevel + 1);
        }

        private void Update()
        {
            if (simulation == null || simulation.Phase != GamePhase.Running) return;
            accumulator += Time.deltaTime;
            int safety = 0;
            while (accumulator >= TickSeconds && safety++ < 5 &&
                   simulation.Phase == GamePhase.Running)
            {
                accumulator -= TickSeconds;
                TickReport report = simulation.Tick();
                if (report.Wrong > 0) hintText.text = "Wrong door. Retry to improve your route.";
                if (report.Phase == GamePhase.Stalled) hintText.text = "Belt jammed. Retry your routing.";
            }
            Refresh();
        }

        private void Refresh()
        {
            if (simulation == null) return;
            Level level = simulation.Level;
            for (int y = 0; y < level.Height; y++)
            for (int x = 0; x < level.Width; x++)
            {
                Cargo cargo = simulation.CargoAt(x, y);
                Image icon = cargoIcons[x, y];
                icon.gameObject.SetActive(cargo != null);
                if (cargo != null)
                {
                    bool isRed = cargo.Color == CargoColor.Red;
                    icon.color = isRed ? Coral : Blue;
                    cargoLabels[x, y].text = isRed ? "R" : "B";
                }
                if (level.Tiles[level.At(x, y)].Kind == TileKind.Junction)
                    symbols[x, y].text = Arrow(simulation.CurrentOutput(x, y));
            }

            progressText.text = "DELIVERED  " + simulation.Delivered + " / " + level.TotalCargo
                                + "     RIGHT  " + simulation.Correct + "     WRONG  " + simulation.Wrong;
            nextButton.interactable = simulation.Phase == GamePhase.Success &&
                                      currentLevel < LevelCatalog.Count;
            nextButton.GetComponent<Image>().color = nextButton.interactable ? Green : TileBase;
            nextButton.GetComponentInChildren<Text>().color =
                nextButton.interactable ? Color.white : Belt;
            runButton.interactable = simulation.Phase == GamePhase.Preview ||
                                     simulation.Phase == GamePhase.Paused ||
                                     simulation.Phase == GamePhase.Running;
            runLabel.text = simulation.Phase == GamePhase.Running ? "PAUSE" :
                            simulation.Phase == GamePhase.Paused ? "RESUME" : "RUN";
            switch (simulation.Phase)
            {
                case GamePhase.Preview:
                    statusText.text = "READY TO ROUTE?";
                    summaryText.text = "RED goes UP   •   BLUE goes RIGHT";
                    break;
                case GamePhase.Running:
                    statusText.text = "FACTORY RUNNING";
                    summaryText.text = "Tap the switch before each parcel leaves";
                    break;
                case GamePhase.Paused:
                    statusText.text = "PAUSED";
                    summaryText.text = "Take your time. No penalty for waiting.";
                    break;
                case GamePhase.Success:
                    statusText.text = "FACTORY COMPLETE!";
                    summaryText.text = simulation.Stars + " STARS  •  SCORE " + simulation.Score;
                    hintText.text = currentLevel < LevelCatalog.Count ?
                        "Great work! Next puzzle unlocked." : "All 12 puzzles completed!";
                    break;
                case GamePhase.Failure:
                    statusText.text = "TRY A NEW ROUTE";
                    summaryText.text = "Score " + simulation.Score + " • Tap RETRY";
                    break;
                default:
                    statusText.text = "BELT JAMMED";
                    summaryText.text = "Try a different route";
                    break;
            }
        }
    }
}
