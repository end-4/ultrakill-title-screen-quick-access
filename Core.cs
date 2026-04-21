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

namespace TitleScreenQuickAccess
{
    [BepInPlugin("com.github.end-4.titleScreenQuickAccess", "TitleScreenQuickAccess", "1.0.0")]
    public class Core : BaseUnityPlugin
    {
        public const string PluginGUID = "com.github.end-4.titleScreenQuickAccess";
        public const string PluginName = "TitleScreenQuickAccess";
        public const string PluginVersion = "1.0.0";

        public static string workingPath;
        public static string workingDir;

        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = base.Logger;
            workingPath = Assembly.GetExecutingAssembly().Location;
            workingDir = Path.GetDirectoryName(workingPath);

            SceneManager.sceneLoaded += OnSceneLoaded;
            Log.LogInfo("TitleScreenQuickAccess loaded!");

            // Check if we are already in the Main Menu (in case the plugin loaded late)
            Scene currentScene = SceneManager.GetActiveScene();
            if (IsMainMenu(currentScene))
            {
                Log.LogInfo("Detected Main Menu on Awake, creating button...");
                CreateQuickAccessButtons();
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Log scene info to help identify scenes if hashes change
            Log.LogInfo($"Scene Loaded: {scene.name} | Path: {scene.path}");

            if (IsMainMenu(scene))
            {
                CreateQuickAccessButtons();
            }
        }

        private bool IsMainMenu(Scene scene)
        {
            // Matches the friendly name, the hash you observed, or the standard scene path
            return scene.name == "Main Menu" ||
                   scene.name == "b3e7f2f8052488a45b35549efb98d902" ||
                   scene.path.Contains("Main Menu");
        }

        public GameObject FindByChildText(Transform parent, string targetText)
        {
            // Look through every child of the parent transform
            foreach (Transform child in parent)
            {
                // Look for the Text component in the child or its own children
                var textComponent = child.GetComponentInChildren<TMPro.TMP_Text>();
                print("this buttons text " + textComponent.text);
                if (textComponent != null && textComponent.text == targetText)
                {
                    return child.gameObject;
                }
            }

            return null;
        }

        private GameObject CreateTitleScreenButton(string name, Vector2 offset)
        {
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
            return newButton;
        }

        private GameObject CreateSquareTitleScreenButton(string name, Vector2 offset)
        {
            GameObject btn = CreateTitleScreenButton(name, offset);
            RectTransform rt = btn.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(rt.sizeDelta.y, rt.sizeDelta.y);
            return btn;
        }

        public static Sprite LoadSpriteFromFile(string path, float pixelsPerUnit = 100f, Vector2? pivot = null, TextureFormat textureFormat = TextureFormat.RGBA32, bool mipmap = false)
        {
            try
            {
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;

                byte[] data = File.ReadAllBytes(path);
                Texture2D tex = new Texture2D(2, 2, textureFormat, mipmap);
                if (!tex.LoadImage(data)) return null;
                tex.Apply();

                Vector2 pivotValue = pivot ?? new Vector2(0.5f, 0.5f);
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivotValue, pixelsPerUnit);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void CreateCustomLevelButton()
        {
            GameObject customLevelsButton = CreateSquareTitleScreenButton("CustomLevels", new Vector2(502, 0));

            // Change appearance
            // Remove text
            var text = customLevelsButton.GetComponentInChildren<TMP_Text>();
            if (text != null) text.gameObject.SetActive(false);
            // Add icon
            GameObject iconObj = new GameObject("CustomLevelsButtonIcon");
            iconObj.transform.SetParent(customLevelsButton.transform, false);
            Sprite angryIconSprite = LoadSpriteFromFile(Path.Combine(workingDir, "assets/angry.png"));
            Image iconImage = iconObj.AddComponent<Image>();
            iconImage.sprite = angryIconSprite;
            iconImage.raycastTarget = false; // So it doesn't block the button clicks
            RectTransform rect = iconObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(40, 40); // Set icon size

            // Button behavior
            Button buttonComponent = customLevelsButton.GetComponent<Button>();
            buttonComponent.onClick = new Button.ButtonClickedEvent(); // nuke old behavior
            buttonComponent.onClick.AddListener(() => // and add new
            {
                GameObject canvas = SceneManager.GetActiveScene().GetRootGameObjects()
                    .Where(obj => obj.name == "Canvas").FirstOrDefault();
                if (canvas == null) return;

                // find main menu
                Transform mainMenu = GameObject.Find("Canvas/Main Menu (1)").transform;

                // find angry level loader conf btn
                Transform optionsMenu = canvas.transform.Find("OptionsMenu");
                Transform pages = optionsMenu.transform.Find("Pages");
                Transform pluginConf = pages.transform.Find("ConcretePanel(Clone)");
                Transform rect = pluginConf.transform.Find("Scroll Rect");
                Transform cont = rect.transform.Find("Contents");
                GameObject configField = FindByChildText(cont, "Angry Level Loader");
                GameObject selectObject = configField.transform.Find("Select").gameObject;

                // find plugin conf btn
                Transform navrail = optionsMenu.transform.Find("Navigation Rail");
                Transform pluginConfBtn = navrail.transform.Find("PluginConfiguratorButton(Clone)");

                // activate screens
                mainMenu.gameObject.SetActive(false);
                optionsMenu.gameObject.SetActive(true);
                foreach (Transform child in pages)
                {
                    child.gameObject.SetActive(false);
                }

                pluginConf.gameObject.SetActive(true);
                selectObject.gameObject.GetComponent<Button>().onClick.Invoke();
            });
        }

        private void CreateCyberGrindButton()
        {
            GameObject cyberGrindButton = CreateSquareTitleScreenButton("CyberGrind", new Vector2(426, 0));
            // Change appearance
            // Remove text
            var text = cyberGrindButton.GetComponentInChildren<TMP_Text>();
            if (text != null) text.gameObject.SetActive(false);
            // Add icon
            GameObject iconObj = new GameObject("CyberGrindButtonIcon");
            iconObj.transform.SetParent(cyberGrindButton.transform, false);
            Sprite angryIconSprite = LoadSpriteFromFile(Path.Combine(workingDir, "assets/cybergrind.png"));
            Image iconImage = iconObj.AddComponent<Image>();
            iconImage.sprite = angryIconSprite;
            iconImage.raycastTarget = false; // So it doesn't block the button clicks
            RectTransform rect = iconObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(40, 40); // Set icon size
            // Button behavior
            Button buttonComponent = cyberGrindButton.GetComponent<Button>();
            buttonComponent.onClick = new Button.ButtonClickedEvent(); // nuke old behavior
            buttonComponent.onClick.AddListener(() => // and add new
            {
                GameObject canvas = SceneManager.GetActiveScene().GetRootGameObjects()
                    .Where(obj => obj.name == "Canvas").FirstOrDefault();
                if (canvas == null) return;

                // find main menu
                Transform mainMenu = GameObject.Find("Canvas/Main Menu (1)").transform;

                // find angry level loader conf btn
                Transform chapterSel = canvas.transform.Find("Chapter Select");
                Transform chapters = chapterSel.transform.Find("Chapters");
                Transform cyberGrind = chapters.transform.Find("The Cyber Grind");

                cyberGrind.gameObject.GetComponent<Button>().onClick.Invoke();
            });
        }

        private void CreateQuickAccessButtons()
        {
            CreateCyberGrindButton();
            CreateCustomLevelButton();
        }
    }
}