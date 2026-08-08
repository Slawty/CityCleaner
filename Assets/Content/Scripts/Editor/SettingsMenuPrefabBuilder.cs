#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public static class SettingsMenuPrefabBuilder
{
    const string SettingsFolder = "Assets/Content/Cleaning/Prefabs/UI/Settings";
    const string ButtonPrefabPath = SettingsFolder + "/UI_SettingsButton.prefab";
    const string SliderRowPrefabPath = SettingsFolder + "/UI_SettingsSliderRow.prefab";
    const string MenuPrefabPath = SettingsFolder + "/Settings Menu.prefab";
    const string FontAssetPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/prstartk SDF.asset";
    const string PanelSpritePath = "Assets/Content/UI/Sprites/Box.png";

    static TMP_FontAsset FontAsset => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
    static Sprite PanelSprite => AssetDatabase.LoadAssetAtPath<Sprite>(PanelSpritePath);

    [MenuItem("City Cleaner/Build Settings UI Prefabs")]
    public static void BuildAll()
    {
        EnsureFolder(SettingsFolder);

        GameObject buttonPrefab = BuildButtonPrefab();
        GameObject sliderRowPrefab = BuildSliderRowPrefab();
        BuildSettingsMenuPrefab(buttonPrefab, sliderRowPrefab);
        WireToManagerScene();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Settings UI prefabs built in " + SettingsFolder);
    }

    [InitializeOnLoadMethod]
    static void BuildOnLoadIfMissing()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (File.Exists(MenuPrefabPath))
                return;

            BuildAll();
        };
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
        string folderName = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, folderName);
    }

    static GameObject BuildButtonPrefab()
    {
        GameObject root = CreateUiObject("UI_SettingsButton", typeof(RectTransform), typeof(Image), typeof(SettingsMenuButton), typeof(Button));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(220f, 44f);

        Image image = root.GetComponent<Image>();
        image.sprite = PanelSprite;
        image.type = Image.Type.Sliced;
        image.color = new Color(1f, 0.92f, 0.67f, 1f);

        Button button = root.GetComponent<Button>();
        button.targetGraphic = image;

        GameObject labelObject = CreateUiObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(root.transform, false);
        Stretch(labelObject.GetComponent<RectTransform>(), 12f, 8f);
        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        SetupText(label, "Button", 24f, TextAlignmentOptions.Center);

        SettingsMenuButton menuButton = root.GetComponent<SettingsMenuButton>();
        SerializedObject serializedButton = new SerializedObject(menuButton);
        serializedButton.FindProperty("button").objectReferenceValue = button;
        serializedButton.FindProperty("labelText").objectReferenceValue = label;
        serializedButton.ApplyModifiedPropertiesWithoutUndo();

        return SavePrefab(root, ButtonPrefabPath);
    }

    static GameObject BuildSliderRowPrefab()
    {
        GameObject root = CreateUiObject("UI_SettingsSliderRow", typeof(RectTransform), typeof(SettingsSliderRow));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(520f, 56f);

        GameObject labelObject = CreateUiObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(root.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0.5f);
        labelRect.anchorMax = new Vector2(0f, 0.5f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.anchoredPosition = new Vector2(0f, 0f);
        labelRect.sizeDelta = new Vector2(180f, 40f);
        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        SetupText(label, "Setting", 22f, TextAlignmentOptions.MidlineLeft);

        GameObject sliderObject = DefaultControls.CreateSlider(new DefaultControls.Resources
        {
            standard = PanelSprite,
            background = PanelSprite,
            knob = PanelSprite
        });
        sliderObject.name = "Slider";
        sliderObject.transform.SetParent(root.transform, false);
        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0f, 0.5f);
        sliderRect.anchorMax = new Vector2(1f, 0.5f);
        sliderRect.pivot = new Vector2(0.5f, 0.5f);
        sliderRect.offsetMin = new Vector2(190f, -12f);
        sliderRect.offsetMax = new Vector2(-70f, 12f);

        GameObject valueObject = CreateUiObject("Value", typeof(RectTransform), typeof(TextMeshProUGUI));
        valueObject.transform.SetParent(root.transform, false);
        RectTransform valueRect = valueObject.GetComponent<RectTransform>();
        valueRect.anchorMin = new Vector2(1f, 0.5f);
        valueRect.anchorMax = new Vector2(1f, 0.5f);
        valueRect.pivot = new Vector2(1f, 0.5f);
        valueRect.anchoredPosition = new Vector2(0f, 0f);
        valueRect.sizeDelta = new Vector2(60f, 40f);
        TextMeshProUGUI valueText = valueObject.GetComponent<TextMeshProUGUI>();
        SetupText(valueText, "100%", 20f, TextAlignmentOptions.MidlineRight);

        SettingsSliderRow row = root.GetComponent<SettingsSliderRow>();
        SerializedObject serializedRow = new SerializedObject(row);
        serializedRow.FindProperty("labelText").objectReferenceValue = label;
        serializedRow.FindProperty("slider").objectReferenceValue = sliderObject.GetComponent<Slider>();
        serializedRow.FindProperty("valueText").objectReferenceValue = valueText;
        serializedRow.FindProperty("showAsPercent").boolValue = true;
        serializedRow.ApplyModifiedPropertiesWithoutUndo();

        return SavePrefab(root, SliderRowPrefabPath);
    }

    static void BuildSettingsMenuPrefab(GameObject buttonPrefab, GameObject sliderRowPrefab)
    {
        GameObject root = CreateUiObject("Settings Menu", typeof(RectTransform), typeof(SettingsMenuController));
        Stretch(root.GetComponent<RectTransform>(), 0f, 0f);

        GameObject panelRoot = CreateUiObject("Panel Root", typeof(RectTransform), typeof(CanvasGroup));
        panelRoot.transform.SetParent(root.transform, false);
        Stretch(panelRoot.GetComponent<RectTransform>(), 0f, 0f);

        GameObject dimmer = CreateUiObject("Dimmer", typeof(RectTransform), typeof(Image));
        dimmer.transform.SetParent(panelRoot.transform, false);
        Stretch(dimmer.GetComponent<RectTransform>(), 0f, 0f);
        Image dimmerImage = dimmer.GetComponent<Image>();
        dimmerImage.color = new Color(0f, 0f, 0f, 0.55f);
        dimmerImage.raycastTarget = true;

        GameObject panel = CreateUiObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        panel.transform.SetParent(panelRoot.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(560f, 0f);
        Image panelImage = panel.GetComponent<Image>();
        panelImage.sprite = PanelSprite;
        panelImage.type = Image.Type.Sliced;
        panelImage.color = new Color(0.18f, 0.16f, 0.14f, 0.95f);

        VerticalLayoutGroup layout = panel.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(24, 24, 24, 24);
        layout.spacing = 16f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = panel.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject titleObject = CreateUiObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        titleObject.transform.SetParent(panel.transform, false);
        LayoutElement titleLayout = titleObject.GetComponent<LayoutElement>();
        titleLayout.preferredHeight = 48f;
        TextMeshProUGUI title = titleObject.GetComponent<TextMeshProUGUI>();
        SetupText(title, "Settings", 34f, TextAlignmentOptions.Center);

        SettingsSliderRow volumeRow = ((GameObject)PrefabUtility.InstantiatePrefab(sliderRowPrefab, panel.transform)).GetComponent<SettingsSliderRow>();
        volumeRow.gameObject.name = "Master Volume Row";
        volumeRow.SetLabel("Master Volume");

        GameObject sensitivityPrefab = (GameObject)PrefabUtility.InstantiatePrefab(sliderRowPrefab, panel.transform);
        sensitivityPrefab.name = "Mouse Sensitivity Row";
        SettingsSliderRow sensitivityRow = sensitivityPrefab.GetComponent<SettingsSliderRow>();
        SerializedObject sensitivitySerialized = new SerializedObject(sensitivityRow);
        sensitivitySerialized.FindProperty("showAsPercent").boolValue = false;
        sensitivitySerialized.ApplyModifiedPropertiesWithoutUndo();
        sensitivityRow.SetLabel("Mouse Sensitivity");

        SettingsMenuButton resumeButton = ((GameObject)PrefabUtility.InstantiatePrefab(buttonPrefab, panel.transform)).GetComponent<SettingsMenuButton>();
        resumeButton.gameObject.name = "Resume Button";
        LayoutElement resumeLayout = resumeButton.gameObject.AddComponent<LayoutElement>();
        resumeLayout.preferredHeight = 44f;
        resumeButton.SetLabel("Resume");

        SettingsMenuController controller = root.GetComponent<SettingsMenuController>();
        SerializedObject serializedController = new SerializedObject(controller);
        serializedController.FindProperty("panelRoot").objectReferenceValue = panelRoot;
        serializedController.FindProperty("masterVolumeRow").objectReferenceValue = volumeRow;
        serializedController.FindProperty("mouseSensitivityRow").objectReferenceValue = sensitivityRow;
        serializedController.FindProperty("resumeButton").objectReferenceValue = resumeButton;
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        panelRoot.SetActive(false);
        SavePrefab(root, MenuPrefabPath);
    }

    static void WireToManagerScene()
    {
        const string scenePath = "Assets/Content/Scenes/Manager Scene.unity";
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        SettingsMenuController settingsMenu = Object.FindFirstObjectByType<SettingsMenuController>(FindObjectsInactive.Include);
        if (settingsMenu == null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MenuPrefabPath);
            Transform uiParent = GameObject.Find("HUD Canvas") != null
                ? GameObject.Find("HUD Canvas").transform
                : GameObject.Find("UI")?.transform;

            if (uiParent == null)
            {
                Debug.LogError("Could not find HUD Canvas or UI root in Manager Scene.");
                return;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, uiParent);
            instance.name = "Settings Menu";
            instance.transform.SetAsLastSibling();
            settingsMenu = instance.GetComponent<SettingsMenuController>();
        }

        Managers managers = Object.FindFirstObjectByType<Managers>();
        if (managers == null)
        {
            Debug.LogError("Could not find Managers in Manager Scene.");
            return;
        }

        SerializedObject serializedManagers = new SerializedObject(managers);
        serializedManagers.FindProperty("settingsMenuController").objectReferenceValue = settingsMenu;
        serializedManagers.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    static GameObject SavePrefab(GameObject root, string path)
    {
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    static GameObject CreateUiObject(string name, params System.Type[] components)
    {
        GameObject gameObject = new GameObject(name, components);
        gameObject.layer = LayerMask.NameToLayer("UI");
        return gameObject;
    }

    static void Stretch(RectTransform rectTransform, float left, float right)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = new Vector2(left, left);
        rectTransform.offsetMax = new Vector2(-right, -right);
    }

    static void SetupText(TextMeshProUGUI text, string content, float size, TextAlignmentOptions alignment)
    {
        text.text = content;
        text.font = FontAsset;
        text.fontSize = size;
        text.color = Color.white;
        text.alignment = alignment;
        text.raycastTarget = false;
    }
}
#endif
