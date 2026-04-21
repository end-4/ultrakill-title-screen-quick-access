using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace TitleScreenQuickAccess {
    [BepInPlugin("com.github.end-4.titleScreenQuickAccess", "TitleScreenQuickAccess", "1.0.0")]
    public class Core : BaseUnityPlugin {
        public const string PluginGUID = "com.github.end-4.titleScreenQuickAccess";
        public const string PluginName = "TitleScreenQuickAccess";
        public const string PluginVersion = "1.0.0";

        public static string workingPath = Assembly.GetExecutingAssembly().Location;
        public static string workingDir = Path.GetDirectoryName(workingPath);

        internal static ManualLogSource Log;

        private void Awake() {
            Log = base.Logger;

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

        public static Sprite LoadSpriteFromFile(string path, float pixelsPerUnit = 100f, Vector2? pivot = null, TextureFormat textureFormat = TextureFormat.RGBA32, bool mipmap = false) {
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

        private GameObject FindNestedObject(GameObject baseObject, string path) {
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

        public GameObject FindByChildText(GameObject parent, string targetText) {
            // Look through every child of the parent transform
            foreach (Transform child in parent.transform) {
                // Look for the Text component in the child or its own children
                var textComponent = child.GetComponentInChildren<TMPro.TMP_Text>();
                print("this buttons text " + textComponent.text);
                if (textComponent != null && textComponent.text == targetText) {
                    return child.gameObject;
                }
            }

            return null;
        }

        private GameObject CreateTitleScreenButton(string name, Vector2 offset) {
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
            return newButton;
        }

        private GameObject CreateSquareTitleScreenButton(string name, Vector2 offset) {
            GameObject btn = CreateTitleScreenButton(name, offset);
            RectTransform rt = btn.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(rt.sizeDelta.y, rt.sizeDelta.y);
            return btn;
        }

        private GameObject CreateSquareTitleScreenIconButton(string name, Vector2 offset, string iconPath) {
            GameObject btn = CreateSquareTitleScreenButton(name, offset);
            // Remove text
            var text = btn.GetComponentInChildren<TMP_Text>();
            if (text != null) text.gameObject.SetActive(false);
            // Add icon
            GameObject iconObj = new GameObject(name + "Button");
            iconObj.transform.SetParent(btn.transform, false);
            Sprite angryIconSprite = LoadSpriteFromFile(iconPath);
            Image iconImage = iconObj.AddComponent<Image>();
            iconImage.sprite = angryIconSprite;
            iconImage.raycastTarget = false; // So it doesn't block the button clicks
            RectTransform rect = iconObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(40, 40); // Set icon size
            return btn;
        }

        private void CreateCyberGrindButton() {
            GameObject cyberGrindButton = CreateSquareTitleScreenIconButton("CustomLevels", new Vector2(426, 0), Path.Combine(workingDir, "assets/cybergrind.png"));

            // Find necessary stuff
            GameObject canvas = SceneManager.GetActiveScene().GetRootGameObjects()
                .Where(obj => obj.name == "Canvas").FirstOrDefault();
            if (canvas == null) return;
            GameObject originalCyberGrindButton = FindNestedObject(canvas, "Chapter Select/Chapters/The Cyber Grind");

            // Add button behavior
            Button buttonComponent = cyberGrindButton.GetComponent<Button>();
            buttonComponent.onClick.AddListener(() => {
                originalCyberGrindButton.GetComponent<Button>().onClick.Invoke();
            });
        }

        private void CreateCustomLevelButton() {
            GameObject customLevelsButton = CreateSquareTitleScreenIconButton("CustomLevels", new Vector2(502, 0), Path.Combine(workingDir, "assets/angry.png"));
            GameObject canvas = SceneManager.GetActiveScene().GetRootGameObjects()
                .Where(obj => obj.name == "Canvas").FirstOrDefault();
            if (canvas == null) return;

            // Add button behavior
            Button buttonComponent = customLevelsButton.GetComponent<Button>();
            buttonComponent.onClick.AddListener(() => {
                // Menus
                GameObject mainMenu = GameObject.Find("Canvas/Main Menu (1)");
                GameObject optionsMenu = canvas.transform.Find("OptionsMenu").gameObject;

                // find angry level loader conf btn
                Transform pages = optionsMenu.transform.Find("Pages");
                Transform pluginConf = pages.transform.Find("ConcretePanel(Clone)");
                GameObject pluginConfPanelContent = FindNestedObject(canvas, "OptionsMenu/Pages/ConcretePanel(Clone)/Scroll Rect/Contents");
                GameObject configField = FindByChildText(pluginConfPanelContent, "Angry Level Loader");
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
        }

        private void CreatePluginConfigButton() {
            GameObject pluginConfButton = CreateSquareTitleScreenIconButton("PluginConfig", new Vector2(426, -75), Path.Combine(workingDir, "assets/plugins.png"));
            Button buttonComponent = pluginConfButton.GetComponent<Button>();
            buttonComponent.onClick.AddListener(() => {
                GameObject canvas = SceneManager.GetActiveScene().GetRootGameObjects()
                    .Where(obj => obj.name == "Canvas").FirstOrDefault();
                if (canvas == null) return;

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
        }

        private void CreateSandboxButton() {
            GameObject cyberGrindButton = CreateSquareTitleScreenIconButton("Sandbox", new Vector2(426, -150), Path.Combine(workingDir, "assets/sandbox.png"));

            // Find necessary stuff
            GameObject canvas = SceneManager.GetActiveScene().GetRootGameObjects()
                .Where(obj => obj.name == "Canvas").FirstOrDefault();
            if (canvas == null) return;
            GameObject originalCyberGrindButton = FindNestedObject(canvas, "Chapter Select/Chapters/Sandbox");

            // Add button behavior
            Button buttonComponent = cyberGrindButton.GetComponent<Button>();
            buttonComponent.onClick.AddListener(() => {
                originalCyberGrindButton.GetComponent<Button>().onClick.Invoke();
            });
        }

        private void CreateQuickAccessButtons() {
            CreateCyberGrindButton();
            CreateCustomLevelButton();
            CreatePluginConfigButton();
            CreateSandboxButton();
        }
    }
}
