using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;

namespace TitleScreenQuickAccess {
    [BepInPlugin("com.github.end-4.titleScreenQuickAccess", "TitleScreenQuickAccess", "1.0.0")]
    public class Core : BaseUnityPlugin {

        public static string workingPath = Assembly.GetExecutingAssembly().Location;
        public static string workingDir = Path.GetDirectoryName(workingPath);

        internal static ManualLogSource Log;

        private void Awake() {
            Log = Logger;

            SceneManager.sceneLoaded += OnSceneLoaded;
            Log.LogInfo("TitleScreenQuickAccess loaded!");

            // Check if we are already in the Main Menu (in case the plugin loaded late)
            Scene currentScene = SceneManager.GetActiveScene();
            if (IsMainMenu(currentScene)) {
                Log.LogInfo("Detected Main Menu on Awake, creating button...");
                CreateQuickAccessButtons();
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
            // Log scene info to help identify scenes if hashes change
            Log.LogInfo($"Scene Loaded: {scene.name} | Path: {scene.path}");

            if (IsMainMenu(scene)) {
                CreateQuickAccessButtons();
            }
        }

        private bool IsMainMenu(Scene scene) {
            return scene.name == "Main Menu" || scene.name == "b3e7f2f8052488a45b35549efb98d902" || scene.path.Contains("Main Menu");
        }

        internal class TooltipHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, ISelectHandler, IDeselectHandler {
            public string message = "";
            private static GameObject? tooltipObj;
            private static TMP_Text? textComp;

            private void handleEnter() {
                // Create tooltip obj if doesn't exist
                if (tooltipObj == null) {
                    tooltipObj = new GameObject("QuickAccessTooltip");
                    var canvas = GameObject.Find("Canvas");
                    if (canvas != null) tooltipObj.transform.SetParent(canvas.transform, false);

                    var bgImage = tooltipObj.AddComponent<Image>();
                    bgImage.color = new Color(0, 0, 0, 0.85f);
                    bgImage.raycastTarget = false;

                    var fitter = tooltipObj.AddComponent<ContentSizeFitter>();
                    fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                    fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                    var layout = tooltipObj.AddComponent<HorizontalLayoutGroup>();
                    layout.childControlHeight = true;
                    layout.childControlWidth = true;
                    layout.padding = new RectOffset(10, 10, 5, 5);

                    var textObj = new GameObject("Text");
                    textObj.transform.SetParent(tooltipObj.transform, false);
                    textComp = textObj.AddComponent<TextMeshProUGUI>();
                    textComp.fontSize = 20;
                    textComp.alignment = TextAlignmentOptions.Center;
                    textComp.raycastTarget = false;
                }
                // Set text to that of current button
                var buttonText = GetComponentInChildren<TMP_Text>(true);
                if (buttonText != null && textComp != null) {
                    textComp.font = buttonText.font;
                }

                tooltipObj.SetActive(true);
                textComp.text = message;
            }

            private void handleExit() {
                if (tooltipObj != null) tooltipObj.SetActive(false);
                hasMouse = false;
            }

            private void positionTooltipHere() {
                if (tooltipObj == null) return;
                RectTransform rt = GetComponent<RectTransform>();
                float btnWidth = rt.rect.width;
                float btnHeight = rt.rect.height;
                tooltipObj.transform.position = transform.position + new Vector3(btnWidth / 2, btnHeight / 2, 0);
            }

            bool hasMouse = false;
            public void OnPointerEnter(PointerEventData eventData) { 
                handleEnter();
                hasMouse = true;
            }
            public void OnSelect(BaseEventData eventData) { 
                handleEnter();
                if (!hasMouse) positionTooltipHere();
            }
            public void OnPointerExit(PointerEventData eventData) { handleExit(); }
            public void OnPointerClick(PointerEventData eventData) { handleExit(); }
            public void OnDeselect(BaseEventData eventData) { handleExit(); }

            private Vector3 prevMousePos;
            private void Update() {
                if (prevMousePos == UnityEngine.Input.mousePosition) return;
                prevMousePos = UnityEngine.Input.mousePosition;
                if (tooltipObj != null && tooltipObj.activeSelf) {
                    tooltipObj.transform.position = prevMousePos + new Vector3(0, 40, 0);
                }
            }
        }

        public static Sprite? LoadSpriteFromFile(string path, float pixelsPerUnit = 100f, Vector2? pivot = null, TextureFormat textureFormat = TextureFormat.RGBA32, bool mipmap = false) {
            try {
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;

                byte[] data = File.ReadAllBytes(path);
                Texture2D tex = new Texture2D(2, 2, textureFormat, mipmap);
                if (!tex.LoadImage(data)) return null;
                tex.Apply();

                Vector2 pivotValue = pivot ?? new Vector2(0.5f, 0.5f);
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivotValue, pixelsPerUnit);
            } catch (Exception) {
                return null;
            }
        }

        private GameObject? FindNestedObject(GameObject baseObject, string path) {
            Transform t = baseObject.transform;
            string[] pathItems = path.Split("/");
            for (int i = 0; i < pathItems.Length; i++) {
                string itemStr = pathItems[i];
                t = t.transform.Find(itemStr);
                if (t == null) {
                    Log.LogWarning(itemStr + " not found for object path " + baseObject.name + "/" + path);
                    return null;
                }
            }
            return t.gameObject;
        }

        public GameObject? FindByChildText(GameObject parent, string targetText) {
            // Look through every child of the parent transform
            foreach (Transform child in parent.transform) {
                // Look for the Text component in the child or its own children
                var textComponent = child.GetComponentInChildren<TMPro.TMP_Text>();
                if (textComponent != null && textComponent.text == targetText) {
                    return child.gameObject;
                }
            }

            return null;
        }

        private GameObject CreateTitleScreenButton(string name, Vector2 offset, string tooltip = "") {
            const float optionsOffset = 75;
            Transform mainMenu = GameObject.Find("Canvas/Main Menu (1)").transform;
            GameObject leftSide = mainMenu.transform.Find("LeftSide").gameObject;
            Transform optionsButton = leftSide.transform.Find("Options");
            GameObject newButton = Instantiate(optionsButton.gameObject, leftSide.transform);
            newButton.name = name;
            RectTransform rt = newButton.GetComponent<RectTransform>();
            rt.anchoredPosition += new Vector2(offset.x, offset.y + optionsOffset);
            newButton.GetComponent<HudOpenEffect>().enabled = true;
            newButton.SetActive(true);
            Button buttonComponent = newButton.GetComponent<Button>();
            buttonComponent.onClick = new Button.ButtonClickedEvent(); // nuke old behavior

            if (!string.IsNullOrEmpty(tooltip)) {
                var th = newButton.AddComponent<TooltipHandler>();
                th.message = tooltip;
            }

            return newButton;
        }

        private GameObject CreateSquareTitleScreenButton(string name, Vector2 offset, string tooltip = "") {
            GameObject btn = CreateTitleScreenButton(name, offset, tooltip);
            RectTransform rt = btn.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(rt.sizeDelta.y, rt.sizeDelta.y);
            return btn;
        }

        private GameObject CreateSquareTitleScreenIconButton(string name, Vector2 offset, string iconPath, string tooltip = "") {
            GameObject btn = CreateSquareTitleScreenButton(name, offset, tooltip);
            // Remove text
            var text = btn.GetComponentInChildren<TMP_Text>();
            if (text != null) text.gameObject.SetActive(false);
            // Add icon
            GameObject iconObj = new GameObject(name + "Button");
            iconObj.transform.SetParent(btn.transform, false);
            Sprite? angryIconSprite = LoadSpriteFromFile(iconPath);
            Image iconImage = iconObj.AddComponent<Image>();
            iconImage.sprite = angryIconSprite;
            iconImage.raycastTarget = false; // So it doesn't block the button clicks
            RectTransform rect = iconObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(40, 40); // Set icon size
            return btn;
        }

        /// <summary>
        /// Method <c>CreateVanillaShortcutButton</c> creates a shortcut button for vanilla stuff.
        /// Vanilla buttons don't seem to have race condition issues; we can find them immediately on creation instead of finding every time the button is clicked
        /// </summary>
        private GameObject? CreateVanillaShortcutButton(string name, Vector2 offset, string iconPath, string canvasPathToClickedButton, string tooltip = "") {
            GameObject newButton = CreateSquareTitleScreenIconButton(name, offset, iconPath, tooltip);

            // Find necessary stuff
            GameObject canvas = SceneManager.GetActiveScene().GetRootGameObjects()
                .Where(obj => obj.name == "Canvas").FirstOrDefault();
            if (canvas == null) return null;
            GameObject? clickedButton = FindNestedObject(canvas, canvasPathToClickedButton);
            if (clickedButton == null) return null;

            // Add button behavior
            Button buttonComponent = newButton.GetComponent<Button>();
            buttonComponent.onClick.AddListener(() => {
                clickedButton.GetComponent<Button>().onClick.Invoke();
            });
            return newButton;
        }

        private GameObject? CreateCyberGrindButton() {
            return CreateVanillaShortcutButton("CustomLevels", new Vector2(426, 0),
                Path.Combine(workingDir, "assets/cybergrind.png"),
                "Chapter Select/Chapters/The Cyber Grind",
                "Cyber Grind");
        }

        private GameObject? CreateSandboxButton() {
            return CreateVanillaShortcutButton("Sandbox", new Vector2(426, -150),
                Path.Combine(workingDir, "assets/sandbox.png"),
                "Chapter Select/Chapters/Sandbox",
                "Sandbox");
        }

        private GameObject? CreateCustomLevelButton() {
            GameObject canvas = SceneManager.GetActiveScene().GetRootGameObjects()
                .Where(obj => obj.name == "Canvas").FirstOrDefault();
            if (canvas == null) return null;
            GameObject customLevelsButton = CreateSquareTitleScreenIconButton("CustomLevels", new Vector2(502, 0), Path.Combine(workingDir, "assets/angry.png"), "Custom levels (Angry Level Loader)");

            // Add button behavior
            Button buttonComponent = customLevelsButton.GetComponent<Button>();
            buttonComponent.onClick.AddListener(() => {
                // Menus
                GameObject mainMenu = GameObject.Find("Canvas/Main Menu (1)");
                GameObject optionsMenu = canvas.transform.Find("OptionsMenu").gameObject;

                // Find angry level loader conf btn
                Transform pages = optionsMenu.transform.Find("Pages");
                Transform pluginConf = pages.transform.Find("ConcretePanel(Clone)");
                GameObject? pluginConfPanelContent = FindNestedObject(canvas, "OptionsMenu/Pages/ConcretePanel(Clone)/Scroll Rect/Contents");
                if (pluginConfPanelContent == null) return;
                GameObject? configField = FindByChildText(pluginConfPanelContent, "Angry Level Loader");
                GameObject selectObject = configField.transform.Find("Select").gameObject;

                // Find plugin conf btn
                Transform navrail = optionsMenu.transform.Find("Navigation Rail");
                Transform pluginConfBtn = navrail.transform.Find("PluginConfiguratorButton(Clone)");

                // (De)activate stuff
                mainMenu.SetActive(false);
                optionsMenu.SetActive(true);
                foreach (Transform child in pages) {
                    child.gameObject.SetActive(false);
                }

                pluginConf.gameObject.SetActive(true);
                selectObject.GetComponent<Button>().onClick.Invoke();
            });
            return customLevelsButton;
        }

        private GameObject? CreatePluginConfigButton() {
            GameObject canvas = SceneManager.GetActiveScene().GetRootGameObjects()
                    .Where(obj => obj.name == "Canvas").FirstOrDefault();
            if (canvas == null) return null;

            GameObject pluginConfButton = CreateSquareTitleScreenIconButton("PluginConfig", new Vector2(426, -75), Path.Combine(workingDir, "assets/plugins.png"), "Plugin Configurator");
            Button buttonComponent = pluginConfButton.GetComponent<Button>();
            buttonComponent.onClick.AddListener(() => {

                // Menus
                GameObject mainMenu = GameObject.Find("Canvas/Main Menu (1)");
                GameObject optionsMenu = canvas.transform.Find("OptionsMenu").gameObject;
                GameObject pages = optionsMenu.transform.Find("Pages").gameObject;
                GameObject pluginConf = pages.transform.Find("ConcretePanel(Clone)").gameObject;

                // (De)activate stuff
                mainMenu.SetActive(false);
                optionsMenu.SetActive(true);
                foreach (Transform child in pages.transform) {
                    child.gameObject.SetActive(false);
                }
                pluginConf.gameObject.SetActive(true);
            });
            return pluginConfButton;
        }

        private void CreateQuickAccessButtons() {
            CreateCyberGrindButton();
            CreateCustomLevelButton();
            CreatePluginConfigButton();
            CreateSandboxButton();
        }
    }
}
