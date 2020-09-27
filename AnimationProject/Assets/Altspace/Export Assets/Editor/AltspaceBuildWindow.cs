#if UNITY_EDITOR
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEditor;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Text;
using Ionic.Zip;
using System.Threading;



[Serializable]
public class SpaceTemplate
{
    public string Name;

    public string Space_template_id;

    public SpaceTemplate(string name, string id)
    {
        Name = name;
        Space_template_id = id;
    }
}

[Serializable]
public class Kit
{
    public string Name;

    public string Kit_ID;

    public Kit(string name, string id)
    {
        Name = name;
        Kit_ID = id;
    }
}



public class AltspaceBuildWindow : EditorWindow
{

    private const string macAssetBundleFolder = "OSX",
                    pcAssetBundleFolder = "Windows",
                    androidAssetBundleFolder = "Android";

    // TODO: In the future, consider adjusting backend to take the full unity version string
    // (but it treats major+minor as an int, e.g., 20192)
    public static string MySimpleUnityVersion;

    private string tempSceneFile = "";
    private string outAssetBundleName = "";
    private string saveLocation = string.Empty;
    private string email = string.Empty;
    private string password = string.Empty;

    private List<GameObject> scene = new List<GameObject>();

    //private string m_teleportLayer;
    private LayerMask NavLayerMask;

    [SerializeField]
    private UserPreferences _userPrefs;

    private Texture2D logoImage;

    private Vector2 m_templateScrollPosition;
    private Vector2 m_kitScrollPosition;

    private Vector2 m_panelScrollPosition;

    private Vector3 m_rotationOverride;

    private int m_currentTemplatePage;
    private int m_currentKitPage;

    private int m_totalTemplatePages;
    private int m_totalKitPages;

    [Serializable]
    private struct KitIndexInfo
    {
        public int Page;
        public int Index;
    }

    private KitIndexInfo m_selectedKitIndexInfo;

    private int m_projectTypeIndex;

    private int m_kitShaderTypeIndex;

    private int m_templateShaderTypeIndex;

    private bool m_hasMultipleTemplatePages;

    private Rect m_templateRect = new Rect(1, 70, 249, 150);

    //private List<SpaceTemplate> spaceTemplates = new List<SpaceTemplate>();

    //private List<Kit> kits = new List<Kit>();

    //private List<string> spaceTemplateNames = new List<string>();

    private List<string> kitNames = new List<string>();

    private List<string> m_loadedPrefabDirectories = new List<string>();


    //json loader
    private JSONHelper.SpaceTemplateCollection _spaceTemplates;
    private JSONHelper.KitCollection _kits;

    private List<JSONHelper.SpaceTemplateCollection> m_spaceTemplateCollections = new List<JSONHelper.SpaceTemplateCollection>();
    private List<JSONHelper.KitCollection> m_kitCollections = new List<JSONHelper.KitCollection>();

    //private string m_progressMessage;
    //private float m_progress;
    //private float m_goal;

    private int selectedTemplateIndex;
    private int selectedKitIndex;

    private string m_prefabName = string.Empty;
    private string m_prefabFolderName = string.Empty;

    private GameObject m_selectedKitPrefab;

    private bool isUserSignedIn;

    private bool m_creatingTemplate;
    private bool m_creatingKit;
    private bool m_settings;
    private bool m_disableAutoLayers;
    private bool m_packageScreenshots = true;

    private bool BuildAndroidKit = true;
    private bool BuildWindowsKit = true;

    private Vector2 m_prefabKitsScroll;

    private bool userHasTemplates;
    private bool userHasKits;

    /// <summary>
    /// This is used to see if the user is using the uploader in offline mode or not.
    /// </summary>
    private bool isOffline;

    private bool userHasPrefabs;

    private bool nullGameObjects;
    private bool nullProjectName;
    private bool nullAssetName;
    private bool isKitConvex;

    private bool showPassword;

    private bool rememberUserLogin;

    private string ProjectName = "template";

    public bool IsAndroidBuildTarget = true, IsWindowsBuildTarget = true, IsOSXBuildTarget = true;

    [MenuItem("AltspaceVR/Build Settings")]
    public static void ShowWindow()
    {

        EditorWindow.GetWindow<AltspaceBuildWindow>(false, "AltspaceVR Build Settings");


    }

    public string CopyPasteControls(int controlID)
    {
        if (controlID == GUIUtility.keyboardControl)
        {
            if (Event.current.type == EventType.KeyUp && (Event.current.modifiers == EventModifiers.Control || Event.current.modifiers == EventModifiers.Command))
            {
                if (Event.current.keyCode == KeyCode.C)
                {
                    Event.current.Use();
                    TextEditor tEditor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                    tEditor.Copy();
                }
                else if (Event.current.keyCode == KeyCode.V)
                {
                    Event.current.Use();
                    TextEditor tEditor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                    tEditor.Paste();
                    return tEditor.text;
                }
            }
        }

        return null;
    }

    public string TextField(string value, params GUILayoutOption[] options)
    {


        int textFieldID = GUIUtility.GetControlID("TextField".GetHashCode(), FocusType.Keyboard) + 1;
        if (textFieldID == 0)
            return value;

        value = CopyPasteControls(textFieldID) ?? value;

        return GUILayout.TextField(value);
    }

    public string PasswordField(string value, params GUILayoutOption[] options)
    {

        int textFieldID = GUIUtility.GetControlID("TextField".GetHashCode(), FocusType.Keyboard) + 1;
        if (textFieldID == 0)
            return value;

        value = CopyPasteControls(textFieldID) ?? value;
        return GUILayout.PasswordField(value, '*');
    }

    private void OnGUI()
    {
        //has to be moved here. Can't be set during serialization.
        MySimpleUnityVersion = Application.unityVersion.Split('.')[0] + Application.unityVersion.Split('.')[1];

        //m_teleportLayer = LayerMask.LayerToName(14);



        if (_userPrefs == null)
        {
            _userPrefs = (UserPreferences)Resources.Load("User Preferences");

            if (_userPrefs)
            {
                IsWindowsBuildTarget = _userPrefs.IsWindowsBuild;
                IsAndroidBuildTarget = _userPrefs.IsAndroidBuild;
                ProjectName = _userPrefs.ProjectName;
                saveLocation = _userPrefs.ExportPath;
                email = _userPrefs.Email;
                password = _userPrefs.Password;
            }
        }

        if (_userPrefs)
        {
            _userPrefs.IsWindowsBuild = IsWindowsBuildTarget;
            _userPrefs.IsAndroidBuild = IsAndroidBuildTarget;
            _userPrefs.ProjectName = ProjectName;
            //_userPrefs.ExportPath = saveLocation;
        }

        if (logoImage == null)
        {
            logoImage = Resources.Load("logo", typeof(Texture2D)) as Texture2D;
        }

        this.minSize = new Vector2(350, 600);
        this.maxSize = new Vector2(350, 600);

        GUILayout.Space(10);

        GUIStyle logoStyle = new GUIStyle();

        logoStyle.fixedWidth = 250;
        logoStyle.fixedHeight = 75;

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Label(logoImage, logoStyle);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        CheckAutenticationCookie();



        if (!isUserSignedIn)
        {
            //spaceTemplateNames.Clear();
            //spaceTemplateNames.TrimExcess();

            //spaceTemplates.Clear();
            //spaceTemplates.TrimExcess();

            _spaceTemplates = null;

            userHasTemplates = false;

            GUILayout.Space(10);

            GUILayout.Label("Log in", EditorStyles.centeredGreyMiniLabel);

            GUILayout.Space(10);

            GUILayout.Label("Email", EditorStyles.boldLabel);
            email = GUILayout.TextField(email);

            GUILayout.Space(5);

            GUILayout.Label("Password", EditorStyles.boldLabel);
            password = GUILayout.PasswordField(password, '*');

            GUILayout.Space(10);

            GUILayout.Label("Remember Login Credentials?", EditorStyles.boldLabel);
            rememberUserLogin = GUILayout.Toggle(rememberUserLogin, "Yes");

            GUILayout.Space(10);

            if (GUILayout.Button("Sign In"))
            {
                SignInToAltspaceVR();
            }
            if(GUILayout.Button("Continue Offline"))
            {
                ContinueOffline();
            }

            m_creatingTemplate = true;
        }
        else
        {

            //GUILayout.Label("Tool", EditorStyles.boldLabel);

     

            //m_projectTypeIndex = EditorGUILayout.Popup(m_projectTypeIndex, new string[] { "Template", "Kit" });

           m_projectTypeIndex = GUILayout.Toolbar(m_projectTypeIndex,new string[] {"Template", "Kit","Settings"});

            if (m_projectTypeIndex == 0)
            {
                m_creatingTemplate = true;
                m_creatingKit = false;
                m_settings = false;
            }
            else if(m_projectTypeIndex == 1)
            {
                m_creatingKit = true;
                m_creatingTemplate = false;
                m_settings = false;
            }
            else if(m_projectTypeIndex == 2)
            {
                m_creatingKit = false;
                m_creatingTemplate = false;
                m_settings = true;
            }



            GUILayout.Space(20);

            m_panelScrollPosition = GUILayout.BeginScrollView(m_panelScrollPosition);

            if (m_creatingTemplate)
            {


                GUILayout.Label("Template", EditorStyles.boldLabel);

                if(!isOffline)
                {
                    if (!userHasTemplates)
                    {
                        GUILayout.Space(10);

                        GUILayout.Label("No templates loaded. Load your template(s), \nor create a new template", EditorStyles.boldLabel);

                        GUILayout.Space(10);
                    }

                    if (GUILayout.Button("Load your Templates"))
                    {
                        GetSpaceTemplates();
                    }
                }

                

                if (userHasTemplates)
                {
                    GUILayout.Space(20);



                    //BeginWindows();

                    //EditorGUILayout.BeginVertical(GUILayout.Height(500));

                    List<string> names = new List<string>();

                    _spaceTemplates.space_templates.ForEach(x => names.Add(x.name));

                    var options = names.ToArray();

                    GUILayout.Space(10);

                    GUILayout.Label("Selected Template: " + options[selectedTemplateIndex], EditorStyles.boldLabel);

                    GUILayout.Space(10);

                    

                    m_templateScrollPosition = GUILayout.BeginScrollView(m_templateScrollPosition, GUILayout.Height(300));

                    foreach (var o in options)
                    {
                        if (GUILayout.Button(o, EditorStyles.miniButton))
                        {
                            selectedTemplateIndex = options.ToList().FindIndex(x => x.Equals(o));
                        }
                    }
                    GUILayout.EndScrollView();

                    var text_Style = new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontStyle = FontStyle.Bold
                        
                    };
                    
                    EditorGUILayout.BeginHorizontal();


                    
                    if (GUILayout.Button("<"))
                    {
                        if (m_currentTemplatePage == 0)
                            return;

                        m_currentTemplatePage--;

                        _spaceTemplates = m_spaceTemplateCollections[m_currentTemplatePage];
                    }

                    EditorGUILayout.LabelField("Page: " + (m_currentTemplatePage + 1).ToString(), text_Style, GUILayout.ExpandWidth(true));

                    if (GUILayout.Button(">"))
                    {
                        if (m_currentTemplatePage == m_totalTemplatePages)
                            return;

                        m_currentTemplatePage++;

                        _spaceTemplates = m_spaceTemplateCollections[m_currentTemplatePage];
                    }
                    
                    EditorGUILayout.EndHorizontal();

                    //EditorGUILayout.EndVertical();

                    //m_templateRect = GUILayout.Window(1, m_templateRect, (windowID) =>
                    //{
                    //    //var options = spaceTemplateNames.ToArray();
                    //    List<string> names = new List<string>();

                    //    _spaceTemplates.space_templates.ForEach(x => names.Add(x.name));

                    //    var options = names.ToArray();

                    //    GUILayout.Space(10);

                    //    GUILayout.Label("Selected Template: " + options[selectedTemplateIndex], EditorStyles.boldLabel);

                    //    GUILayout.Space(10);

                    //    m_templateScrollPosition = EditorGUILayout.BeginScrollView(m_templateScrollPosition, GUILayout.Width(275), GUILayout.Height(100));

                    //    foreach (var o in options)
                    //    {
                    //        if (GUILayout.Button(o, EditorStyles.miniButton))
                    //        {
                    //            selectedTemplateIndex = options.ToList().FindIndex(x => x.Equals(o));
                    //        }
                    //    }
                    //    EditorGUILayout.EndScrollView();

                    //}, "Loaded Templates", GUILayout.Width(250));

                    //EndWindows();

                    GUILayout.Space(50);
                }
                else
                {
                    GUILayout.Space(20);
                }


                if (GUILayout.Button("Create a new Template"))
                {
                    Application.OpenURL("https://account.altvr.com/space_templates/new");
                }

                GUILayout.Space(20);

                if (userHasTemplates || isOffline)
                {
                    GUILayout.Label("Platform Options", EditorStyles.boldLabel);

                    IsWindowsBuildTarget = GUILayout.Toggle(IsWindowsBuildTarget, "Build for Windows?");

                    IsAndroidBuildTarget = GUILayout.Toggle(IsAndroidBuildTarget, "Build for Android?");

                    GUILayout.Space(20);

                    if (GUILayout.Button("Build"))
                    {
                        var filePath = EditorUtility.SaveFilePanel("Save Template", saveLocation, "template", "");

                        FileInfo fileInfo = new FileInfo(filePath);

                        ProjectName = fileInfo.Name.ToLower();

                        char[] symbols = new char[]
                        {
                            ' ',
                            '!',
                            '@',
                            '#',
                            '$',
                            '%',
                            '^',
                            '&',
                            '*',
                            '(',
                            ')',
                            '+',
                            '=',
                            '{',
                            '}',
                            '[',
                            ']',
                            '\\',
                            '|',
                            '\'',
                            '\'',
                            '<',
                            '.',
                            ',',
                            '>',
                            ';',
                            ':',
                            '/',
                            '?',
                            '`',
                            '~'
                        };

                        ProjectName = new string((from char c in ProjectName
                                                  where !char.IsWhiteSpace(c)
                                                         select c).ToArray());

                        ProjectName = new string((from char c in ProjectName
                                                  where !symbols.Contains(c)
                                                         select c).ToArray());




                        saveLocation = fileInfo.Directory.FullName;

                        if (string.IsNullOrEmpty(ProjectName))
                        {
                            Debug.LogError("Template name can't be empty. Please provide a valid name.");
                            return;
                        }

                        //check to see if there is already a zip.

                        if (File.Exists(Path.Combine(saveLocation, ProjectName + ".zip")))
                        {
                            File.Delete(Path.Combine(saveLocation, ProjectName + ".zip"));
                        }


                        //send info to environment export tool
                        
                        EnvironmentExportTool.Instance.assetBundleName = ProjectName;
                        EnvironmentExportTool.Instance.buildPCAssetBundle = IsWindowsBuildTarget;
                        EnvironmentExportTool.Instance.buildAndroidAssetBundle = IsAndroidBuildTarget;

                        outAssetBundleName = ProjectName;

                        BuildNewEnvironment(() =>
                        {
                            if (!InitializeFilePaths(ProjectName))
                            {
                                Debug.LogError("Unable to save out scene");
                            }

                            //obsolete
                            //EditorApplication.SaveScene(EditorApplication.currentScene);
                            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

                            //save out tmp scene
                            if (!SaveOutScene())
                            {
                                //Debug.LogError("Ran into error trying to save out scene.");
                                SendErrorMessageToSlack("Ran into error trying to save out scene.");
                            }
                            if (EnvironmentExportTool.Instance.buildPCAssetBundle && !SaveOutAssetBundle(EnvironmentExportTool.Platform.PC))
                            {
                                //Debug.LogError("Unable to save out asset bundle for PC (Windows).");
                                SendErrorMessageToSlack("Unable to save out asset bundle for PC (Windows).");
                            }
                            if (EnvironmentExportTool.Instance.buildMacAssetBundle && !SaveOutAssetBundle(EnvironmentExportTool.Platform.MAC))
                            {
                                //Debug.LogError("Unable to save out asset bundle for Mac.");
                                SendErrorMessageToSlack("Unable to save out asset bundle for Mac.");
                            }
                            if (EnvironmentExportTool.Instance.buildAndroidAssetBundle && !SaveOutAssetBundle(EnvironmentExportTool.Platform.ANDROID))
                            {
                                //Debug.LogError("Unable to save out asset bundle for Android.");
                                SendErrorMessageToSlack("Unable to save out asset bundle for Android.");
                            }


                            ZipAssetBundles(false);
                        });
                    }

                    GUILayout.Space(20);

                    if(!isOffline)
                    {
                        if (GUILayout.Button("Build & Upload"))
                        {
                            saveLocation = Directory.GetParent(Application.dataPath).FullName;

                            ProjectName = "template";

                            //check to see if there is already a zip.

                            if (File.Exists(Path.Combine(saveLocation, ProjectName + ".zip")))
                            {
                                File.Delete(Path.Combine(saveLocation, ProjectName + ".zip"));
                            }


                            //send info to environment export tool

                            EnvironmentExportTool.Instance.assetBundleName = ProjectName;
                            EnvironmentExportTool.Instance.buildPCAssetBundle = IsWindowsBuildTarget;
                            EnvironmentExportTool.Instance.buildAndroidAssetBundle = IsAndroidBuildTarget;

                            outAssetBundleName = ProjectName;

                            BuildNewEnvironment(() =>
                            {
                                if (!InitializeFilePaths(ProjectName))
                                {
                                    Debug.LogError("Unable to save out scene");
                                }

                                //obsolete
                                //EditorApplication.SaveScene(EditorApplication.currentScene);
                                EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

                                //save out tmp scene
                                if (!SaveOutScene())
                                {
                                    //Debug.LogError("Ran into error trying to save out scene.");
                                    SendErrorMessageToSlack("Ran into error trying to save out scene.");
                                }
                                if (EnvironmentExportTool.Instance.buildPCAssetBundle && !SaveOutAssetBundle(EnvironmentExportTool.Platform.PC))
                                {
                                    //Debug.LogError("Unable to save out asset bundle for PC (Windows).");
                                    SendErrorMessageToSlack("Unable to save out asset bundle for PC (Windows).");
                                }
                                if (EnvironmentExportTool.Instance.buildMacAssetBundle && !SaveOutAssetBundle(EnvironmentExportTool.Platform.MAC))
                                {
                                    //Debug.LogError("Unable to save out asset bundle for Mac.");
                                    SendErrorMessageToSlack("Unable to save out asset bundle for Mac.");
                                }
                                if (EnvironmentExportTool.Instance.buildAndroidAssetBundle && !SaveOutAssetBundle(EnvironmentExportTool.Platform.ANDROID))
                                {
                                    //Debug.LogError("Unable to save out asset bundle for Android.");
                                    SendErrorMessageToSlack("Unable to save out asset bundle for Android.");
                                }


                                ZipAssetBundles();
                            });

                        }

                        GUILayout.Space(20);
                    }
                   
                }

                if (GUILayout.Button("Sign Out"))
                {
                    SignOutOfAltspaceVR();
                }
            }
            else if (m_creatingKit)
            {
                GUILayout.Space(10);

                GUILayout.Label("Kit Folder Name", EditorStyles.boldLabel);

                char[] symbols = new char[]
                        {
                            ' ',
                            '!',
                            '@',
                            '#',
                            '$',
                            '%',
                            '^',
                            '&',
                            '*',
                            '(',
                            ')',
                            '+',
                            '=',
                            '{',
                            '}',
                            '[',
                            ']',
                            '\\',
                            '|',
                            '\'',
                            '\'',
                            '<',
                            '.',
                            ',',
                            '>',
                            ';',
                            ':',
                            '/',
                            '?',
                            '`',
                            '~'
                        };

                m_prefabFolderName = EditorGUILayout.TextField(m_prefabFolderName).ToLower();

                m_prefabFolderName = new string((from char c in m_prefabFolderName
                                                 where !char.IsWhiteSpace(c)
                                                 select c).ToArray());

                m_prefabFolderName = new string((from char c in m_prefabFolderName
                                                 where !symbols.Contains(c)
                                                 select c).ToArray());

                GUILayout.Space(10);

                GUILayout.Label("Kits require a special formatting before they work correctly in Worlds. " +
                    "Select one or more gameobjects before clicking this button to convert each gameobject to a Kit Prefab. " +
                    "Once the conversion is done, your new Kit Prefabs can be found in the prefabs folder under their Kit Folder Name.", EditorStyles.wordWrappedLabel);

                GUILayout.Space(10);

                GUILayout.Label("Kit Asset Name", EditorStyles.boldLabel);
                m_prefabName = EditorGUILayout.TextField(m_prefabName.ToLower()).ToLower();

                
                GUILayout.Space(10);



                //GUILayout.Label("Rotation Override", EditorStyles.boldLabel);
                m_rotationOverride = EditorGUILayout.Vector3Field("Rotation Override", m_rotationOverride);

                GUILayout.Space(20);

                GUILayout.Label("Kit Shader", EditorStyles.boldLabel);
                m_kitShaderTypeIndex = EditorGUILayout.Popup(m_kitShaderTypeIndex, new string[] { "Keep Existing Shader(*Might not be supported)", "MRE Diffuse Vertex", "MRE Unlit"});

                GUILayout.Label("Kit Collider Info", EditorStyles.boldLabel);
                isKitConvex = GUILayout.Toggle(isKitConvex,"Is the Kit(s) Convex?");

                GUILayout.Space(20);
                if (GUILayout.Button("Convert GameObject(s) to Kit Prefab"))
                {

                    nullGameObjects = Selection.gameObjects.Length == 0;

                    nullProjectName = string.IsNullOrEmpty(m_prefabFolderName);

                    nullAssetName = string.IsNullOrEmpty(m_prefabName);

                    if(nullGameObjects || nullProjectName || nullAssetName)
                    {
                        return;
                    }
                    

                    FormatGameObjectsToKit(m_prefabFolderName.ToLower(), m_prefabName.ToLower());
                }

                GUILayout.Space(5);

                if(nullGameObjects)
                {
                    GUILayout.Label("Please select one or more gameobjects and try again.", EditorStyles.helpBox);
                }

                if (nullAssetName)
                {
                    GUILayout.Label("Please provide a Kit Asset Name.", EditorStyles.helpBox);
                }

                if (nullProjectName)
                {

                    GUILayout.Label("Please provide a Kit Folder Name.", EditorStyles.helpBox);
                }



                GUILayout.Space(20);


                GUILayout.Label("Click here to create a new kit. This button will direct you to the New Kits webpage.", EditorStyles.wordWrappedLabel);
                if (GUILayout.Button("Create a new Kit"))
                {
                    Application.OpenURL("https://account.altvr.com/kits/new");
                }

                if(!isOffline)
                {
                    if (GUILayout.Button("Load Kits"))
                    {
                        GetKits();
                    }
                }

                

                GUILayout.Space(20);


                if(userHasKits && !isOffline)
                {
                    m_kitScrollPosition = GUILayout.BeginScrollView(m_kitScrollPosition, GUILayout.Height(300));

                    List<string> kitNames = new List<string>();

                    m_kitCollections[m_currentKitPage].kits.ForEach(x => kitNames.Add(x.name));

                    var options = kitNames.ToArray();

                    EditorGUILayout.LabelField("Loaded Kits:", EditorStyles.boldLabel);

                    GUILayout.Space(10);

                    foreach (var o in options)
                    {
                        EditorGUILayout.BeginHorizontal(GUILayout.Width(50));

                        EditorGUILayout.LabelField(o);


                        if(GUILayout.Button("Select", EditorStyles.miniButton))
                        {
                            m_selectedKitIndexInfo.Index = m_kitCollections[m_currentKitPage].kits.FindIndex(x => x.name == o);
                            m_selectedKitIndexInfo.Page = m_currentKitPage;
                        }

                        GUILayout.FlexibleSpace();

                        EditorGUILayout.EndHorizontal();

                        //if (GUILayout.Button(o, EditorStyles.miniButton))
                        //{
                        //    selectedKitIndex = options.ToList().FindIndex(x => x.Equals(o));
                        //}
                    }
                    GUILayout.EndScrollView();

                    var text_Style = new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontStyle = FontStyle.Bold

                    };

                    EditorGUILayout.BeginHorizontal();



                    if (GUILayout.Button("<"))
                    {
                        if (m_currentKitPage == 0)
                            return;

                        m_currentKitPage--;

                        _kits = m_kitCollections[m_currentKitPage];
                    }

                    EditorGUILayout.LabelField("Page: " + (m_currentKitPage + 1).ToString(), text_Style, GUILayout.ExpandWidth(true));

                    if (GUILayout.Button(">"))
                    {
                        if (m_currentKitPage == m_totalKitPages)
                            return;

                        m_currentKitPage++;

                        _kits = m_kitCollections[m_currentKitPage];
                    }

                    EditorGUILayout.EndHorizontal();
                }

                GUILayout.Space(20);

                GUILayout.Label("Platform Options", EditorStyles.boldLabel);

                BuildWindowsKit = GUILayout.Toggle(BuildWindowsKit, "Build Kit for Windows?");
                BuildAndroidKit = GUILayout.Toggle(BuildAndroidKit, "Build Kit for Android?");

                GUILayout.Space(10);

                GUILayout.Label("Package the generated screenshots into the Kit build.", EditorStyles.wordWrappedLabel);
                m_packageScreenshots = GUILayout.Toggle(m_packageScreenshots, "Package Generated Screenshots");


                GUILayout.Space(20);

                //reload to see if there are any prefabs in the prefab folder

                if (GUILayout.Button("Load Kit Prefab Directories"))
                {
                    string prefabPath = "Assets/Prefabs/";

                    m_loadedPrefabDirectories.Clear();
                    m_loadedPrefabDirectories.TrimExcess();

                    var directories = Directory.GetDirectories(prefabPath);

                    m_loadedPrefabDirectories = directories.ToList();

                    userHasPrefabs = directories.Length > 0;
                }


                if (userHasPrefabs)
                {
                    GUILayout.Space(20);

                    GUILayout.Label("Build Options", EditorStyles.boldLabel);

                    if (!isOffline && userHasKits)
                    {
                        GUILayout.Label("You can upload kit to existing kit: " + m_kitCollections[m_selectedKitIndexInfo.Page].kits[m_selectedKitIndexInfo.Index].name, EditorStyles.helpBox);
                    }
                    

                    GUILayout.Space(20);

                    
                    m_prefabKitsScroll = GUILayout.BeginScrollView(m_prefabKitsScroll, GUILayout.Height(100));


                    foreach (var x in m_loadedPrefabDirectories)
                    {
                        EditorGUILayout.BeginHorizontal();

                        string dName = x.Remove(0, 15);
                        //string labelName = dName.Length <= 11 ? dName : (dName.Remove(10,dName.Length) + "...").ToString();
                        GUILayout.Label(dName, EditorStyles.boldLabel);

                        if (GUILayout.Button("Build", GUILayout.Width(100)))
                        {
                            

                            BuildKit(dName, string.Empty);
                        }

                        if(!isOffline && userHasKits)
                        {
                            if (GUILayout.Button("Build & Upload"))
                            {
                                string kit_id = m_kitCollections[m_selectedKitIndexInfo.Page].kits[m_selectedKitIndexInfo.Index].kit_id;
                                BuildKit(dName,kit_id, (path) =>
                                {
                                    UploadKit(path);
                                });
                            }
                        }
                        

                        GUILayout.EndHorizontal();
                    }


                    GUILayout.EndScrollView();

                    GUILayout.Space(50);
                }

                GUILayout.Space(20);

                if (GUILayout.Button("Sign Out"))
                {
                    SignOutOfAltspaceVR();
                }

                GUILayout.Space(10);
            }
            else if(m_settings)
            {
                GUILayout.Label("Settings", EditorStyles.boldLabel);

                GUILayout.Space(10);

                m_disableAutoLayers = EditorGUILayout.Toggle("Disable Auto Layers", m_disableAutoLayers);

                if(m_disableAutoLayers)
                {
                    GUILayout.Label("Disabling this feature will remove" +
                        "\nthe auto asssigned Nav Mesh layer." +
                        "\nYou will have to assign all layers manually.", EditorStyles.helpBox);
                }
            }

            GUILayout.EndScrollView();
        }

        //curl -v -d "user[email]=myemail@gmail.com&user[password]=1234567" https://account.altvr.com/users/sign_in.json -c cookie
        GUILayout.Label("World Building Toolkit Version: 0.8.6", EditorStyles.centeredGreyMiniLabel);


    }

    private void ContinueOffline()
    {
        isOffline = true;

        // Sanity check unity version
        int versionAsInt;
        if (MySimpleUnityVersion.Length != 5 || !int.TryParse(MySimpleUnityVersion, out versionAsInt))
        {
            // TODO: See notes for MySimpleUnityVersion
            Debug.LogError("This is an unexpected version format for unity and may confuse the backend");
            return;
        }
        else if (versionAsInt != 20181)
        {
            Debug.LogWarning("This is an unsupported version of Unity. Please download version 2018.1.9f2 for best results.");
        }
    }

    private void SignInToAltspaceVR()
    {
        // Sanity check unity version
        int versionAsInt;
        if (MySimpleUnityVersion.Length != 5 || !int.TryParse(MySimpleUnityVersion, out versionAsInt))
        {
            // TODO: See notes for MySimpleUnityVersion
            Debug.LogError("This is an unexpected version format for unity and may confuse the backend");
            return;
        }
        else if (versionAsInt != 20181)
        {
            Debug.LogWarning("This is an unsupported version of Unity. Please download version 2018.1.9f2 for best results.");
        }

        if (rememberUserLogin)
        {
            _userPrefs.Email = email;
            _userPrefs.Password = password;
            _userPrefs.RememberUserLogin = true;
        }
        else
        {
            _userPrefs.Email = string.Empty;
            _userPrefs.Password = string.Empty;
            _userPrefs.RememberUserLogin = false;
        }

        //var signInCMD = "&curl - v - d \"user[email]=email@gmail.com&user[password]=passWord\" https://account.altvr.com/users/sign_in.json -c cookie";

        //var curlLocation = Application.dataPath + Path.DirectorySeparatorChar + "Plugins" + Path.DirectorySeparatorChar + "curl.exe";
        var envPath = Application.dataPath + Path.DirectorySeparatorChar + "Plugins";

        //var setExePath = "-c start cd " + envPath;

        var batchCMD = new StringBuilder();
        batchCMD.Append("curl -v -d \"user[email]=")
            .Append(email)
            .Append("&user[password]=")
            .Append(password)
            .Append("\" https://account.altvr.com/users/sign_in.json -c cookie");

        var cmdLines = new List<string>();
        //cmdLines.Add("echo off");
        cmdLines.Add("title Sign In to AltspaceVR");
        cmdLines.Add(batchCMD.ToString());

        if (File.Exists(envPath + Path.DirectorySeparatorChar + "signin.bat"))
        {
            File.Delete(envPath + Path.DirectorySeparatorChar + "signin.bat");
        }

        File.WriteAllLines(envPath + Path.DirectorySeparatorChar + "signin.bat", cmdLines.ToArray());

        var process = new System.Diagnostics.ProcessStartInfo();
        process.WorkingDirectory = @envPath;
        process.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
        process.FileName = "bash";
        process.Arguments = "-c \"source signin.bat\"";
        System.Diagnostics.Process.Start(process);

    }

    private void SignOutOfAltspaceVR()
    {
        var envPath = Application.dataPath + Path.DirectorySeparatorChar + "Plugins";

        if(File.Exists(envPath + Path.DirectorySeparatorChar + "cookie"))
        {
            File.Delete(envPath + Path.DirectorySeparatorChar + "cookie");
            File.Delete(envPath + Path.DirectorySeparatorChar + "cookie.meta");
        }
        
        if (File.Exists(envPath + Path.DirectorySeparatorChar + "signin.bat"))
        {
            File.Delete(envPath + Path.DirectorySeparatorChar + "signin.bat");

            File.Delete(envPath + Path.DirectorySeparatorChar + "signin.bat.meta");
        }

        if (File.Exists(envPath + Path.DirectorySeparatorChar + "upload.bat"))
        {
            File.Delete(envPath + Path.DirectorySeparatorChar + "upload.bat");
            File.Delete(envPath + Path.DirectorySeparatorChar + "upload.bat.meta");
        }

        if (File.Exists(envPath + Path.DirectorySeparatorChar + "space_templates"))
        {
            File.Delete(envPath + Path.DirectorySeparatorChar + "space_templates");
            File.Delete(envPath + Path.DirectorySeparatorChar + "space_templates.meta");
        }

        if (File.Exists(envPath + Path.DirectorySeparatorChar + "space_templates.json"))
        {
            File.Delete(envPath + Path.DirectorySeparatorChar + "space_templates.json");
            File.Delete(envPath + Path.DirectorySeparatorChar + "space_templates.json.meta");
        }

        if(File.Exists(Path.Combine(envPath, "uploadKit.bat")))
        {
            File.Delete(Path.Combine(envPath, "uploadKit.bat"));
            File.Delete(Path.Combine(envPath, "uploadKit.bat.meta"));
        }

        if (File.Exists(Path.Combine(envPath, "kits.json")))
        {
            File.Delete(Path.Combine(envPath, "kits.json"));
            File.Delete(Path.Combine(envPath, "kits.json.meta"));
        }

        if (File.Exists(Path.Combine(envPath,"space_template_pages.bat")))
        {
            File.Delete(Path.Combine(envPath, "space_template_pages.bat"));
            File.Delete(Path.Combine(envPath, "space_template_pages.bat.meta"));

            var files = Directory.GetFiles(envPath).ToList();
            var pages = files.FindAll(x => x.Contains("page")).ToList();

            foreach (var page in pages)
                File.Delete(page);
        }

        m_rotationOverride = Vector3.zero;
        m_loadedPrefabDirectories.Clear();

        m_prefabFolderName = string.Empty;
        m_prefabName = string.Empty;

        isUserSignedIn = false;
        isOffline = false;

        userHasTemplates = false;
        userHasKits = false;
        userHasPrefabs = false;
        m_packageScreenshots = true;
        nullAssetName = false;
        nullProjectName = false;

        m_kitShaderTypeIndex = 0;
        m_currentTemplatePage = 0;
        m_totalTemplatePages = 0;

        m_kitCollections.Clear();
        m_kitCollections.TrimExcess();

        m_spaceTemplateCollections.Clear();
        m_spaceTemplateCollections.TrimExcess();

        if (!rememberUserLogin)
        {
            _userPrefs.Email = string.Empty;
            _userPrefs.Password = string.Empty;
        }
    }

    private void CheckAutenticationCookie()
    {
        if(isOffline)
        {
            isUserSignedIn = true;
            return;
        }

        isUserSignedIn = File.Exists(Application.dataPath + Path.DirectorySeparatorChar + "Plugins" + Path.DirectorySeparatorChar + "cookie");
    }

    private void GetSpaceTemplates()
    {
        //spaceTemplateNames.Clear();
        //spaceTemplateNames.TrimExcess();

        //spaceTemplates.Clear();
        //spaceTemplates.TrimExcess();

        _spaceTemplates = null;

        var envPath = Application.dataPath + Path.DirectorySeparatorChar + "Plugins";

        //get the template cookie
        if (File.Exists(envPath + Path.DirectorySeparatorChar + "space_templates"))
        {
            File.Delete(envPath + Path.DirectorySeparatorChar + "space_templates");
        }

        var process = new System.Diagnostics.ProcessStartInfo();
        process.WorkingDirectory = envPath;
        process.FileName = "bash";
        process.Arguments = "-c \"source space_templates.bat\"";
        process.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
        var currentProcess = System.Diagnostics.Process.Start(process);

        currentProcess.EnableRaisingEvents = true;

        currentProcess.WaitForExit();

    
        OnTemplatesLoaded();
    }

    private void LoadOtherTemplatePages()
    {
        m_totalTemplatePages = _spaceTemplates.pagination.pages;

        StringBuilder sb = new StringBuilder();
        sb.Append("")
            .AppendLine("title AltspaceVR Templates")
            .AppendLine();

        for(int i = 0; i < m_totalTemplatePages; i++)
        {
            if(i == 0)
            {
                sb.AppendLine("curl -v -b cookie https://account.altvr.com/api/space_templates/my.json -o page1.json");
            }
            else
            {
                sb.AppendLine("curl -v -b cookie https://account.altvr.com/api/space_templates/my.json?page=" + (i + 1).ToString() + " -o page" + (i+ 1).ToString() + ".json");
            }
        }

        var envPath = Application.dataPath + Path.DirectorySeparatorChar + "Plugins";

        if(File.Exists(Path.Combine(envPath,"space_template_pages.bat")))
        {
            File.Delete(Path.Combine(envPath, "space_template_pages.bat"));
            File.Delete(Path.Combine(envPath, "space_template_pages.bat.meta"));
        }

        File.WriteAllText(Path.Combine(envPath, "space_template_pages.bat"), sb.ToString());

        var process = new System.Diagnostics.ProcessStartInfo();
        process.WorkingDirectory = envPath;
        process.FileName = "bash";
        process.Arguments = "-c \"source space_template_pages.bat\"";
        process.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
        var currentProcess = System.Diagnostics.Process.Start(process);

        currentProcess.EnableRaisingEvents = true;

        currentProcess.WaitForExit();

        m_spaceTemplateCollections.Clear();
        m_spaceTemplateCollections.TrimExcess();

        for(int i = 0; i < m_totalTemplatePages; i++)
        {
            if (!File.Exists(Path.Combine(envPath, "page" + (i + 1).ToString() + ".json")))
                continue;

            string jsonText = File.ReadAllText(Path.Combine(envPath, "page" + (i + 1).ToString() + ".json"));

            var page = JsonUtility.FromJson<JSONHelper.SpaceTemplateCollection>(jsonText);

            m_spaceTemplateCollections.Add(page);
        }

        m_currentTemplatePage = 0;

        _spaceTemplates = m_spaceTemplateCollections[m_currentTemplatePage];

        if (m_spaceTemplateCollections.Count > 0)
        {

            userHasTemplates = true;
        }
        else
        {
            userHasTemplates = false;
        }
    }

    private void GetKits()
    {
        _kits = null;

        var envPath = Application.dataPath + Path.DirectorySeparatorChar + "Plugins";

        //get the template cookie
        if (File.Exists(envPath + Path.DirectorySeparatorChar + "kits.json"))
        {
            File.Delete(envPath + Path.DirectorySeparatorChar + "kits.json");
        }

        var process = new System.Diagnostics.ProcessStartInfo();
        process.WorkingDirectory = envPath;
        process.FileName = "bash";
        process.Arguments = "-c \"source kits.bat\"";
        process.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
        var currentProcess = System.Diagnostics.Process.Start(process);

        currentProcess.EnableRaisingEvents = true;

        currentProcess.WaitForExit();

        if (!File.Exists(envPath + Path.DirectorySeparatorChar + "kits.json"))
            return;

        string jsonText = File.ReadAllText(envPath + Path.DirectorySeparatorChar + "kits.json");

        _kits = JsonUtility.FromJson<JSONHelper.KitCollection>(jsonText);

        var pageData = _kits.pagination;

        Debug.Log("Pages: " + pageData.pages);
        Debug.Log("Count: " + pageData.count);

        if(_kits.pagination.pages > 1)
        {
            m_totalKitPages = _kits.pagination.pages;

            StringBuilder sb = new StringBuilder();
            sb.Append("")
                .AppendLine("title AltspaceVR Kits")
                .AppendLine();

            for (int i = 0; i < m_totalKitPages; i++)
            {
                if (i == 0)
                {
                    sb.AppendLine("curl -v -b cookie https://account.altvr.com/api/kits/my.json -o page1-kits.json");
                }
                else
                {
                    sb.AppendLine("curl -v -b cookie https://account.altvr.com/api/kits/my.json?page=" + (i + 1).ToString() + " -o page" + (i + 1).ToString() + "-kits.json");
                }
            }      

            if (File.Exists(Path.Combine(envPath, "kit_pages.bat")))
            {
                File.Delete(Path.Combine(envPath, "kit_pages.bat"));
                File.Delete(Path.Combine(envPath, "kit_pages.bat.meta"));
            }

            File.WriteAllText(Path.Combine(envPath, "kit_pages.bat"), sb.ToString());

            var newProcess = new System.Diagnostics.ProcessStartInfo();
            newProcess.WorkingDirectory = envPath;
            newProcess.FileName = "bash";
            newProcess.Arguments = "-c \"source space_template_pages.bat\"";
            newProcess.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
            var newCurrentProcess = System.Diagnostics.Process.Start(newProcess);

            newCurrentProcess.EnableRaisingEvents = true;

            newCurrentProcess.WaitForExit();

            m_kitCollections.Clear();
            m_kitCollections.TrimExcess();
            

            for (int i = 0; i < m_totalKitPages; i++)
            {
                if (!File.Exists(Path.Combine(envPath, "page" + (i + 1).ToString() + ".json")))
                    continue;

                string json = File.ReadAllText(Path.Combine(envPath, "page" + (i + 1).ToString() + ".json"));

                var page = JsonUtility.FromJson<JSONHelper.KitCollection>(json);

                m_kitCollections.Add(page);
            }

            m_totalKitPages = 0;

            _kits = m_kitCollections[m_totalKitPages];

            if (m_kitCollections.Count > 0)
            {

                userHasKits = true;
            }
            else
            {
                userHasKits = false;
            }
        }
        else if(_kits.pagination.pages == 1)
        {
            m_kitCollections.Clear();
            m_kitCollections.TrimExcess();
            m_kitCollections.Add(_kits);
            userHasKits = true;
        }
        else
        {
            userHasKits = false;
        }

        Debug.Log("Kits: " + _kits.kits.Count);


    }

    private void FormatGameObjectsToKit(string folderName, string kitName)
    {
        var prefabPath = "Assets/Prefabs/" + folderName + "/";
        var prefabPreviewPath = "Assets/Prefabs/" + folderName + "/Screenshots/";

        var gos = Selection.gameObjects.ToList();
        
        Thread.Sleep(1000);

        if (gos.Count > 1)
        {

            int index = 0;

            foreach (var go in gos)
            {
                string originalName = go.name;
                

                EditorUtility.DisplayProgressBar("Converting Gameobjects to Kit Prefabs", "Please wait...", gos.Count / gos.Count);

                var parent = new GameObject(kitName + "_" + index.ToString("00"));

                go.transform.SetParent(parent.transform);


                var child = parent.transform.GetChild(0);
                child.name = "model";


                var meshFilter = child.GetComponent<MeshFilter>() ? child.GetComponent<MeshFilter>() : null;

                var collider = new GameObject("collision");

                if (!m_disableAutoLayers)
                    collider.layer = 14;

                collider.transform.SetParent(parent.transform);


                child.transform.SetAsFirstSibling();

                if (meshFilter != null)
                {
                    collider.AddComponent<MeshCollider>();
                    collider.GetComponent<MeshCollider>().sharedMesh = meshFilter.sharedMesh;
                    collider.GetComponent<MeshCollider>().convex = isKitConvex;
                }
                else
                {
                    collider.AddComponent<BoxCollider>();
                }

                if (!Directory.Exists(prefabPath))
                {
                    Directory.CreateDirectory(prefabPath);
                }

                for (int i = 0; i < parent.transform.childCount; i++)
                {
                    parent.transform.GetChild(i).transform.localPosition = new Vector3(0, 0, 0);
                    parent.transform.GetChild(i).transform.localEulerAngles = m_rotationOverride;
                    parent.transform.GetChild(i).transform.localScale = new Vector3(1, 1, 1);

                    if (m_kitShaderTypeIndex == 0)
                        continue;

                    if (m_kitShaderTypeIndex == 1)
                    {
                        if (parent.transform.GetChild(i).GetComponent<Renderer>())
                        {
                            var r = parent.transform.GetChild(i).GetComponent<Renderer>();
                            r.sharedMaterial.shader = Shader.Find("MRE/DiffuseVertex");
                        }
                    }
                    else if (m_kitShaderTypeIndex == 2)
                    {
                        if (parent.transform.GetChild(i).GetComponent<Renderer>())
                        {
                            var r = parent.transform.GetChild(i).GetComponent<Renderer>();
                            r.sharedMaterial.shader = Shader.Find("MRE/Unlit (Supports Lightmap)");
                        }
                    }
                }

                var prefab = PrefabUtility.CreatePrefab(prefabPath + kitName + "_" + index.ToString("00") + ".prefab", parent);

                AssetDatabase.Refresh();

                DestroyImmediate(parent);

                Texture2D assetPreview = null;
                int counter = 0;

                while (assetPreview == null && counter < 100)
                {
                    assetPreview = AssetPreview.GetAssetPreview(prefab);
                    counter++;
                    System.Threading.Thread.Sleep(15);
                }

                if (!Directory.Exists(prefabPreviewPath))
                {
                    Directory.CreateDirectory(prefabPreviewPath);
                }

                var referencePixel = assetPreview.GetPixel(0, 0);

                Texture2D screenshot = new Texture2D(128, 128, TextureFormat.RGBA32, false);

                for (var x = 0; x < 128; x++)
                {
                    for (var y = 0; y < 128; y++)
                    {
                        var pixel = assetPreview.GetPixel(x, y);

                        if (pixel == referencePixel)
                        {
                            screenshot.SetPixel(x, y, Color.clear);
                        }
                        else
                        {
                            screenshot.SetPixel(x, y, pixel);
                        }

                    }
                }

                screenshot.alphaIsTransparency = true;
                screenshot.Apply();

                var bytes = screenshot.EncodeToPNG();

                File.WriteAllBytes(prefabPreviewPath + kitName + "_" + index.ToString("00") + ".png", bytes);

                AssetDatabase.Refresh();

                index++;
            }

            EditorUtility.ClearProgressBar();

        }
        else if (gos.Count == 1)
        {
            string originalName = gos[0].name;

            var parent = new GameObject(kitName);

            gos[0].transform.SetParent(parent.transform);

            var child = parent.transform.GetChild(0);
            child.name = "model";

            var meshFilter = child.GetComponent<MeshFilter>() ? child.GetComponent<MeshFilter>() : null;

            var collider = new GameObject("collider");

            if (!m_disableAutoLayers)
                collider.layer = 14;

            collider.transform.SetParent(parent.transform);

            child.transform.SetAsFirstSibling();

            if (meshFilter != null)
            {
                collider.AddComponent<MeshCollider>();
                collider.GetComponent<MeshCollider>().sharedMesh = meshFilter.sharedMesh;
                collider.GetComponent<MeshCollider>().convex = isKitConvex;

            }
            else
            {
                collider.AddComponent<BoxCollider>();
            }

            if (!Directory.Exists(prefabPath))
            {
                Directory.CreateDirectory(prefabPath);
            }


            for (int i = 0; i < parent.transform.childCount; i++)
            {
                parent.transform.GetChild(i).transform.localPosition = new Vector3(0, 0, 0);
                parent.transform.GetChild(i).transform.localEulerAngles = m_rotationOverride;
                parent.transform.GetChild(i).transform.localScale = new Vector3(1, 1, 1);

                if (m_kitShaderTypeIndex == 0)
                    continue;

                if(m_kitShaderTypeIndex == 1)
                {
                    if(parent.transform.GetChild(i).GetComponent<Renderer>())
                    {
                        var r = parent.transform.GetChild(i).GetComponent<Renderer>();
                        r.sharedMaterial.shader = Shader.Find("MRE/DiffuseVertex");
                    }                   
                }
                else if(m_kitShaderTypeIndex == 2)
                {
                    if (parent.transform.GetChild(i).GetComponent<Renderer>())
                    {
                        var r = parent.transform.GetChild(i).GetComponent<Renderer>();
                        r.sharedMaterial.shader = Shader.Find("MRE/Unlit (Supports Lightmap)");
                    }
                }
            }

            var prefab = PrefabUtility.CreatePrefab(prefabPath + kitName + ".prefab", parent);

            AssetDatabase.Refresh();


            DestroyImmediate(parent);


            Texture2D assetPreview = null;
            int counter = 0;

            while(assetPreview == null && counter < 100)
            {
                assetPreview = AssetPreview.GetAssetPreview(prefab);
                counter++;
                System.Threading.Thread.Sleep(15);
            }

            

            if (!Directory.Exists(prefabPreviewPath))
            {
                Directory.CreateDirectory(prefabPreviewPath);
            }

            var referencePixel = assetPreview.GetPixel(0, 0);

            Texture2D screenshot = new Texture2D(128, 128, TextureFormat.RGBA32, false);

            for (var x = 0; x < 128; x++)
            {
                for (var y = 0; y < 128; y++)
                {
                    var pixel = assetPreview.GetPixel(x, y);

                    if (pixel == referencePixel)
                    {
                        screenshot.SetPixel(x, y, Color.clear);
                    }
                    else
                    {
                        screenshot.SetPixel(x, y, pixel);
                    }

                }
            }

            screenshot.alphaIsTransparency = true;
            screenshot.Apply();

            var bytes = screenshot.EncodeToPNG();

            File.WriteAllBytes(prefabPreviewPath + kitName + ".png", bytes);

            AssetDatabase.Refresh();
        }

        ShowNotification(new GUIContent("Kit Assets Created!"));
    }

    private void OnTemplatesLoaded()
    {
        Debug.Log("get templates");
        ParseSpaceTemplateFromJSON();
    }

    private void CurrentProcess_Exited(object sender, EventArgs e)
    {
        throw new NotImplementedException();
    }

    private void ParseSpaceTemplateFromJSON()
    {

        var envPath = Application.dataPath + Path.DirectorySeparatorChar + "Plugins";

        if (!File.Exists(envPath + Path.DirectorySeparatorChar + "space_templates.json"))
            return;

        string jsonText = File.ReadAllText(envPath + Path.DirectorySeparatorChar + "space_templates.json");

        _spaceTemplates = JsonUtility.FromJson<JSONHelper.SpaceTemplateCollection>(jsonText);

        var pageData = _spaceTemplates.pagination;

        Debug.Log("Pages: " + pageData.pages);
        Debug.Log("Count: " + pageData.count);

        if (_spaceTemplates.pagination.pages > 1)
        {
            m_hasMultipleTemplatePages = true;
            LoadOtherTemplatePages();
            return;
        }
        else
        {
            m_hasMultipleTemplatePages = false;
        }


        Debug.Log("Space Templates: " + _spaceTemplates.space_templates.Count);

        if (_spaceTemplates.space_templates.Count > 0)
        {

            userHasTemplates = true;
        }
        else
        {
            userHasTemplates = false;
        }

    }


    private string RemoveSpaces(string value)
    {
        return value.Replace(" ", string.Empty);
    }

    private void BuildNewEnvironment(Action onComplete)
    {

        if (GameObject.Find("Environment") == null)
        {
            //get all the gameobjects in the scene
            if (scene != null)
            {
                scene.Clear();
                scene.TrimExcess();
            }

            scene = FindObjectsOfType<GameObject>().ToList();

            if (scene.Count == 0)
            {
                Debug.LogError("There is nothing to export");
                return;
            }

            //remove the gameobjects that already have parents.
            var removeItems = new List<GameObject>();
            foreach (var go in scene)
            {
                if (go.transform.parent != null)
                {
                    removeItems.Add(go);
                }

            }

            foreach (var go in removeItems)
            {
                scene.Remove(go);
            }

            var environment = new GameObject("Environment");

            scene.ForEach(go => go.transform.SetParent(environment.transform));

            if(!m_disableAutoLayers)
                environment.layer = 14;

            for (int i = 0; i < environment.transform.childCount; i++)
            {
                var child = environment.transform.GetChild(i).gameObject;

                if (!m_disableAutoLayers)
                    child.gameObject.layer = 14;

                if (child.transform.childCount > 0)
                {
                    for (int x = 0; x < child.transform.childCount; x++)
                    {
                        var superChild = child.transform.GetChild(x).gameObject;

                        if (!m_disableAutoLayers)
                            superChild.layer = 14;

                    }
                }
            }
        }
        else
        {
            var unParentedObjects = FindObjectsOfType<GameObject>().ToList().FindAll(x => x.transform.parent == null);

            var environment = GameObject.Find("Environment");

            foreach (var x in unParentedObjects)
            {
                x.transform.SetParent(environment.transform);
            }

            if (!m_disableAutoLayers)
                environment.layer = 14;

            for (int i = 0; i < environment.transform.childCount; i++)
            {
                var child = environment.transform.GetChild(i).gameObject;

                if (!m_disableAutoLayers)
                    child.gameObject.layer = 14;

                if (child.transform.childCount > 0)
                {
                    for (int x = 0; x < child.transform.childCount; x++)
                    {

                        var superChild = child.transform.GetChild(x).gameObject;

                        if (!m_disableAutoLayers)
                            superChild.layer = 14;

                    }
                }
            }
        }


        var cameras = FindObjectsOfType<Camera>().ToList();

        foreach (var x in cameras)
            DestroyImmediate(x.gameObject);



        if (onComplete != null) onComplete();
    }

    private void RemoveEnvironment()
    {
        foreach (var go in scene)
        {
            go.transform.parent = null;
        }

        var environment = GameObject.Find("Environment");

        if (environment != null)
        {
            DestroyImmediate(environment);
        }
    }

    public static EnvironmentExportTool.Platform GetCurrentPlatform()
    {
        if (Application.platform == RuntimePlatform.WindowsEditor)
            return EnvironmentExportTool.Platform.PC;
        else
            return EnvironmentExportTool.Platform.MAC;
    }


    public bool InitializeFilePaths(string assetBundleName)
    {
        if (assetBundleName == "")
        {
            Debug.LogError("Must provide a name or path for unity file to be save and uploaded!");
            return false;
        }
        outAssetBundleName = assetBundleName;
        string exportDirectory = GetExportDirectory();
        if (!Directory.Exists(exportDirectory))
        {
            Directory.CreateDirectory(exportDirectory);
        }

        tempSceneFile = exportDirectory + Path.DirectorySeparatorChar + assetBundleName + ".unity";

        return true;
    }

    private string GetExportDirectory()
    {
        string[] exportDirParts = { "Assets", "Altspace", "Export" };
        return string.Join(Path.DirectorySeparatorChar.ToString(), exportDirParts);
    }

    private string AssetBundleDirForPlatform(EnvironmentExportTool.Platform platform)
    {
        var projectDirectory = Directory.GetParent(Application.dataPath).FullName;
        var tempPath = projectDirectory + Path.DirectorySeparatorChar + "AltspaceTemp";

        //create the head directory
        if (!Directory.Exists(Path.Combine(tempPath, "AssetBundles")))
        {
            Directory.CreateDirectory(Path.Combine(tempPath, "AssetBundles"));
        }

        string exportDir = string.Empty;
        if (platform == EnvironmentExportTool.Platform.MAC)
        {
            exportDir = macAssetBundleFolder;
        }
        else if (platform == EnvironmentExportTool.Platform.ANDROID)
        {
            exportDir = androidAssetBundleFolder;
        }

        return exportDir != string.Empty ? tempPath + Path.DirectorySeparatorChar + "AssetBundles" + Path.DirectorySeparatorChar + exportDir + Path.DirectorySeparatorChar :
            tempPath + Path.DirectorySeparatorChar + "AssetBundles" + Path.DirectorySeparatorChar;
    }

    public bool SaveOutScene()
    {
        var originalActiveScene = EditorSceneManager.GetActiveScene();
        string originalActiveScenePath = originalActiveScene.path;
        EditorSceneManager.SaveScene(originalActiveScene);
        bool success = false;
        success = EditorSceneManager.SaveScene(originalActiveScene, tempSceneFile);

        if (success)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(); // necessary?
                                     // EditorSceneManager.CloseScene(originalActiveScene, true); //TODO UNITYUPGRADE
            var tempScene = EditorSceneManager.OpenScene(tempSceneFile);
            EditorSceneManager.SetActiveScene(tempScene);
            DestroyImmediate(GameObject.Find("Tools"));
            EditorSceneManager.SaveScene(tempScene);
            // EditorSceneManager.CloseScene(tempScene, true); //TODO UNITYUPGRADE
            originalActiveScene = EditorSceneManager.OpenScene(originalActiveScenePath);
            EditorSceneManager.SetActiveScene(originalActiveScene);
        }
        return success;
    }

    public bool SaveOutAssetBundle(EnvironmentExportTool.Platform platform, bool shouldCompress = true)
    {
        if (tempSceneFile.Length != 0)
        {
            string[] scenes = { tempSceneFile };
            AssetBundleBuild[] buildMap = { new AssetBundleBuild() };

            
            buildMap[0].assetNames = scenes;
            buildMap[0].assetBundleName = outAssetBundleName;
            buildMap[0].assetBundleVariant = "unity2018_1";
            BuildTarget target = BuildTarget.StandaloneWindows;
            if (platform == EnvironmentExportTool.Platform.MAC)
            {
                //target = BuildTarget.StandaloneOSX;
            }
            else if (platform == EnvironmentExportTool.Platform.ANDROID)
            {
                target = BuildTarget.Android;
            }
            string outputDir = AssetBundleDirForPlatform(platform);
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            BuildAssetBundleOptions options = shouldCompress ? BuildAssetBundleOptions.None : BuildAssetBundleOptions.UncompressedAssetBundle;

            BuildPipeline.BuildAssetBundles(outputDir, buildMap, options, target);
            




            //Debug.Log("Asset Bundle exported to: " + outputDir + ".");
            return true;
        }

        return false;
    }

    private void RenameAssetBundles(Action onComplete)
    {
        var projectDirectory = Directory.GetParent(Application.dataPath).FullName;
        var tempPath = projectDirectory + Path.DirectorySeparatorChar + "AltspaceTemp";

        string[] windowFiles = new string[] { };
        string[] androidFiles = new string[] { };

        if (Directory.Exists(Path.Combine(tempPath, "AssetBundles")))
            windowFiles = Directory.GetFiles(Path.Combine(tempPath, "AssetBundles"));

        if (Directory.Exists(Path.Combine(Path.Combine(tempPath, "AssetBundles"), "Android")))
            androidFiles = Directory.GetFiles(Path.Combine(Path.Combine(tempPath, "AssetBundles"), "Android"));


        foreach (var file in windowFiles)
        {
            Debug.Log("Renaming: " + file);

            if (Path.GetFileName(file).Equals("AssetBundles"))
            {
                File.Delete(@file);
            }

            else if (Path.GetFileName(file).Equals("AssetBundles.manifest"))
            {
                File.Delete(@file);
            }

            else if (Path.GetFileName(file).Equals(ProjectName + ".unity2018_1"))
            {

                var newFileName = tempPath + Path.DirectorySeparatorChar + "AssetBundles" + Path.DirectorySeparatorChar + ProjectName;

                File.Copy(@file, @newFileName);

                File.Delete(@file);
            }

            else if (Path.GetFileName(file).Equals(ProjectName + ".unity2018_1.manifest"))
            {
                var newFileName = tempPath + Path.DirectorySeparatorChar + "AssetBundles" + Path.DirectorySeparatorChar + ProjectName + ".manifest";

                File.Copy(@file, @newFileName);
                File.Delete(@file);
            }
        }

        foreach (var file in androidFiles)
        {
            Debug.Log("Renaming: " + file);

            if (Path.GetFileName(file).Equals("Android"))
            {
                File.Delete(@file);
            }

            else if (Path.GetFileName(file).Equals("Android.manifest"))
            {
                File.Delete(@file);
            }

            else if (Path.GetFileName(file).Equals(ProjectName + ".unity2018_1"))
            {

                var newFileName = tempPath + Path.DirectorySeparatorChar + "AssetBundles" + Path.DirectorySeparatorChar + "Android" + Path.DirectorySeparatorChar + ProjectName;


                //File.Move(file, newFileName);
                File.Copy(@file, @newFileName);

                File.Delete(@file);
            }

            else if (Path.GetFileName(file).Equals(ProjectName + ".unity2018_1.manifest"))
            {
                var newFileName = tempPath + Path.DirectorySeparatorChar + "AssetBundles" + Path.DirectorySeparatorChar + "Android" + Path.DirectorySeparatorChar + ProjectName + ".manifest";

                File.Copy(@file, @newFileName);
                File.Delete(@file);
            }
        }

        if (onComplete != null) onComplete();
    }

    private void UploadKit(string path)
    {
        if(!File.Exists(path))
        {
            Debug.LogError("File in path doesn't exist!");
            return;
        }

        string envPath = Application.dataPath + Path.DirectorySeparatorChar + "Plugins";

        //example
        //string cmd = "curl - v - b cookie - X PUT - F 'space_template[zip]=@jimmycube.zip' -F 'space_template[game_engine_version]=20192' https://account.altvr.com/api/space_templates/<space_template_id>.json";
        var cmd = new StringBuilder();
        cmd.Append("curl -v -b cookie -X PUT -F ")
            .Append("\"kit[zip]=@")
            .Append(path + "\" ")
            .Append("-F \"kit[game_engine_version]=")
                    .Append(MySimpleUnityVersion)
                    .Append("\" ")
            .Append("https://account.altvr.com/api/kits/")
            .Append(m_kitCollections[m_selectedKitIndexInfo.Page].kits[m_selectedKitIndexInfo.Index].kit_id)
            .Append(".json");

        List<string> batCMDs = new List<string>();

        //batCMDs.Add("echo off");
        batCMDs.Add(cmd.ToString());
       // batCMDs.Add("cmd /K");

        File.WriteAllLines(envPath + Path.DirectorySeparatorChar + "uploadKit.bat", batCMDs.ToArray());


        var process = new System.Diagnostics.ProcessStartInfo();
        process.WorkingDirectory = envPath;
        process.FileName = "bash";
        process.Arguments = "-c \"source uploadKit.bat\"";
        process.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
        var processInfo = System.Diagnostics.Process.Start(process);
        processInfo.WaitForExit();

        ShowNotification(new GUIContent("Upload Complete!"));

        File.Delete(path);
    }

    private void BuildKit(string fileName, string kit_id, Action<string> onComplete = null)
    {

        var saveLocation = Directory.GetParent(Application.dataPath).FullName;
        

        //remove any shitty folders that could screw this crap up
        if (Directory.Exists(saveLocation + Path.DirectorySeparatorChar + "AssetBundle"))
        {
            Directory.Delete(@saveLocation + Path.DirectorySeparatorChar + "AssetBundle", true);
        }

        if(kit_id == string.Empty)
        {
            if (Directory.Exists(saveLocation + Path.DirectorySeparatorChar + fileName))
            {
                Directory.Delete(@saveLocation + Path.DirectorySeparatorChar + fileName, true);
            }
        }
        else
        {
            if (Directory.Exists(saveLocation + Path.DirectorySeparatorChar + kit_id + "_" + fileName))
            {
                Directory.Delete(@saveLocation + Path.DirectorySeparatorChar + kit_id + "_" + fileName, true);
            }
        }

        


        AssetBundleBuild[] abb = { new AssetBundleBuild() };

        if(kit_id == string.Empty)
        {
            abb[0].assetBundleName = fileName;
        }
        else
        {
            abb[0].assetBundleName = kit_id + "_" + fileName;
        }


        var fileNames = Directory.GetFiles("Assets/Prefabs/" + fileName);
        //var fileNames = Directory.GetFiles(Path.Combine(Application.dataPath, "Prefabs" + Path.DirectorySeparatorChar + fileName));

        var old = "Assets/Prefabs/" + fileName;
        var temp = old + Path.DirectorySeparatorChar + "TempPrefabs";

        if (!Directory.Exists(temp))
        {
            Directory.CreateDirectory(temp);
        }

        if(kit_id != string.Empty)
        {
            foreach (var file in fileNames)
            {
                //if (file.Equals("Screenshots.meta"))
                //    continue;

                var n = file.Remove(0, old.Length + 1);

                File.Copy(@file, @temp + Path.DirectorySeparatorChar + kit_id + "_" + n, true);

            }
        }

        AssetDatabase.Refresh();

        System.Threading.Thread.Sleep(1000);

        var newFileNames = Directory.GetFiles(temp);

        abb[0].assetNames = kit_id == string.Empty? fileNames : newFileNames;

        if (!Directory.Exists(saveLocation + Path.DirectorySeparatorChar + "AssetBundle"))
        {
            Directory.CreateDirectory(saveLocation + Path.DirectorySeparatorChar + "AssetBundle");
        }

        if (BuildWindowsKit)
        {
            BuildPipeline.BuildAssetBundles(saveLocation + Path.DirectorySeparatorChar + "AssetBundle", abb, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);
        }

        if (BuildAndroidKit)
        {
            if (!Directory.Exists(saveLocation + Path.DirectorySeparatorChar + "AssetBundle" + Path.DirectorySeparatorChar + "Android"))
            {
                Directory.CreateDirectory(saveLocation + Path.DirectorySeparatorChar + "AssetBundle" + Path.DirectorySeparatorChar + "Android");
            }

            BuildPipeline.BuildAssetBundles(saveLocation + Path.DirectorySeparatorChar + "AssetBundle" + Path.DirectorySeparatorChar + "Android", abb, BuildAssetBundleOptions.None, BuildTarget.Android);
        }


        //rename the parent folder and delete the unessessary items

        //get the screenshots folder
        var screenshotsFolderPath = "Assets/Prefabs/" + fileName + "/Screenshots";

        List<string> screenshotList = new List<string>();

        if (m_packageScreenshots)
        {
            screenshotList = Directory.GetFiles(screenshotsFolderPath).ToList();
        }

        if (!Directory.Exists(saveLocation + Path.DirectorySeparatorChar + fileName + Path.DirectorySeparatorChar + "AssetBundles"))
            Directory.CreateDirectory(saveLocation + Path.DirectorySeparatorChar + fileName + Path.DirectorySeparatorChar + "AssetBundles");

        if (m_packageScreenshots)
        {
            if (!Directory.Exists(saveLocation + Path.DirectorySeparatorChar + fileName + Path.DirectorySeparatorChar + "Screenshots"))
                Directory.CreateDirectory(saveLocation + Path.DirectorySeparatorChar + fileName + Path.DirectorySeparatorChar + "Screenshots");
        }

        if (BuildAndroidKit)
        {
            if (!Directory.Exists(saveLocation + Path.DirectorySeparatorChar + fileName + Path.DirectorySeparatorChar + "AssetBundles" + Path.DirectorySeparatorChar + "Android"))
                Directory.CreateDirectory(saveLocation + Path.DirectorySeparatorChar + fileName + Path.DirectorySeparatorChar + "AssetBundles" + Path.DirectorySeparatorChar + "Android");
        }


        var oldLocation = saveLocation + Path.DirectorySeparatorChar + "AssetBundle";
        var newLocation = saveLocation + Path.DirectorySeparatorChar + fileName + Path.DirectorySeparatorChar + "AssetBundles";
        var screenshotLocation = saveLocation + Path.DirectorySeparatorChar + fileName + Path.DirectorySeparatorChar + "Screenshots";

        if (BuildWindowsKit)
        {
            var windowFiles = Directory.GetFiles(oldLocation);

            foreach (var x in windowFiles)
            {
                var fileShortName = x.Remove(0, oldLocation.Length);

                File.Copy(@x, @newLocation + Path.DirectorySeparatorChar + fileShortName);
                

                    
                File.Delete(@x);
            }
        }

        if (m_packageScreenshots)
        {
            foreach (var x in screenshotList)
            {
                var fileShortName = x.Remove(0, screenshotsFolderPath.Length + 1);

                if(kit_id == string.Empty)
                {
                    File.Copy(@x, screenshotLocation + Path.DirectorySeparatorChar +  fileShortName);
                }
                else
                {
                    File.Copy(@x, screenshotLocation + Path.DirectorySeparatorChar + kit_id + "_" + fileShortName);
                }

            }

            var newScreenshotList = Directory.GetFiles(screenshotLocation);

            foreach (var x in newScreenshotList)
            {
                if (x.Contains(".meta"))
                {
                    File.Delete(@x);
                }
            }
        }

        if (BuildAndroidKit)
        {
            var androidFiles = Directory.GetFiles(oldLocation + Path.DirectorySeparatorChar + "Android");

            foreach (var x in androidFiles)
            {
                var fileShortName = x.Remove(0, oldLocation.Length + 8);


                File.Copy(@x, @newLocation + Path.DirectorySeparatorChar + "Android" + Path.DirectorySeparatorChar + fileShortName);
                
                File.Delete(@x);
            }
        }

        if (Directory.Exists(oldLocation + Path.DirectorySeparatorChar + "Android"))
            Directory.Delete(@oldLocation + Path.DirectorySeparatorChar + "Android", true);

        if (Directory.Exists(oldLocation))
            Directory.Delete(@oldLocation, true);

        ZipFile zipFile = new ZipFile();
        zipFile.AddDirectory(saveLocation + Path.DirectorySeparatorChar + fileName);

        if (kit_id == string.Empty)
        {
            if (File.Exists(saveLocation + Path.DirectorySeparatorChar + fileName.ToLower() + ".zip"))
            {
                File.Delete(saveLocation + Path.DirectorySeparatorChar + fileName.ToLower() + ".zip");
            }
        }
        else
        {
            if (File.Exists(saveLocation + Path.DirectorySeparatorChar + kit_id + "_" + fileName.ToLower() + ".zip"))
            {
                File.Delete(saveLocation + Path.DirectorySeparatorChar + kit_id + "_" + fileName.ToLower() + ".zip");
            }
        }
        

        string savePathFull = kit_id == string.Empty? saveLocation + Path.DirectorySeparatorChar + fileName.ToLower() + ".zip":
                                                                                saveLocation + Path.DirectorySeparatorChar + kit_id + "_" + fileName.ToLower() + ".zip";
        zipFile.Save(savePathFull);

        Directory.Delete(newLocation, true);

        System.Threading.Thread.Sleep(3000);

        Directory.Delete(saveLocation + Path.DirectorySeparatorChar + fileName, true);
        Directory.Delete(temp,true);

        if(onComplete == null)
            System.Diagnostics.Process.Start("explorer.exe", saveLocation);
        else
        {
            if (onComplete != null) onComplete(savePathFull);
        }
    }


    private void ZipAssetBundles(bool upload = true)
    {
        RenameAssetBundles(() =>
        {
            //string assetPath = Path.Combine(saveLocation, "AssetBundles");
            //string envPath = Application.dataPath + Path.DirectorySeparatorChar + "Plugins";
            var projectDirectory = Directory.GetParent(Application.dataPath).FullName;
            var tempPath = projectDirectory + Path.DirectorySeparatorChar + "AltspaceTemp";

            ZipFile zipFile = new ZipFile();
            zipFile.AddDirectory(tempPath);

            zipFile.Save(saveLocation + Path.DirectorySeparatorChar + ProjectName + ".zip");

            Directory.Delete(tempPath, true);
            //write an async method here to wait for the file to be done zipping.

            System.Threading.Thread.Sleep(3000);

            if (upload)
            {
                UploadToAltspaceVR();
            }
            else
            {
                ShowNotification(new GUIContent("Build Complete!"));

                System.Diagnostics.Process.Start("explorer.exe", saveLocation);
            }

        });
    }


    private void UploadToAltspaceVR()
    {
        //if (spaceTemplates == null || spaceTemplates.Count == 0)
        if(_spaceTemplates == null || _spaceTemplates.space_templates.Count == 0)
        {
            SendErrorMessageToSlack("You are trying to upload your space without a target template. Please load an existing template, or create a new one.");

            return;
        }

        string envPath = Application.dataPath + Path.DirectorySeparatorChar + "Plugins";

        //example
        //string cmd = "curl - v - b cookie - X PUT - F 'space_template[zip]=@jimmycube.zip' -F 'space_template[game_engine_version]=20192' https://account.altvr.com/api/space_templates/<space_template_id>.json";
        var cmd = new StringBuilder();
        cmd.Append("curl -v -b cookie -X PUT -F ")
            .Append("\"space_template[zip]=@")
            .Append(saveLocation + Path.DirectorySeparatorChar + ProjectName + ".zip\" ")
            .Append("-F \"space_template[game_engine_version]=")
                    .Append(MySimpleUnityVersion)
                    .Append("\" ")
            .Append("https://account.altvr.com/api/space_templates/")
            //.Append(spaceTemplates[selectedTemplateIndex].Space_template_id) old version
            .Append(_spaceTemplates.space_templates[selectedTemplateIndex].space_template_id) //new version
            .Append(".json");

        List<string> batCMDs = new List<string>();

        //batCMDs.Add("echo off");
        batCMDs.Add(cmd.ToString());

        File.WriteAllLines(envPath + Path.DirectorySeparatorChar + "upload.bat", batCMDs.ToArray());


        var process = new System.Diagnostics.ProcessStartInfo();
        process.WorkingDirectory = envPath;
        process.FileName = "bash";
        process.Arguments = "-c \"source upload.bat\"";
        process.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
        var processInfo = System.Diagnostics.Process.Start(process);
        processInfo.WaitForExit();

        ShowNotification(new GUIContent("Upload Complete!"));

    }

    /// <summary>
    /// Send an error message to AltspaceVR's unity-uploader-errors channel on Slack.
    /// </summary>
    /// <param name="errorMessage"></param>
    public void SendErrorMessageToSlack(string errorMessage)
    {

        ShowNotification(new GUIContent(errorMessage));

        string envPath = Application.dataPath + Path.DirectorySeparatorChar + "Plugins";

        var cmd = new StringBuilder();
        cmd.Append("curl -X POST --data-urlencode \"payload={\\\"channel\\\": \\\"#unity-uploader-errors\\\", \\\"username\\\": \\\"webhookbot\\\", \\\"text\\\": \\\"")
            .Append(errorMessage)
            .Append("\\\"}\" ")
            .Append("https://hooks.slack.com/services/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");

        List<string> batCMDs = new List<string>();

        //batCMDs.Add("echo off");
        batCMDs.Add(cmd.ToString());

        if (File.Exists(envPath + Path.DirectorySeparatorChar + "error.bat"))
        {
            File.Delete(envPath + Path.DirectorySeparatorChar + "error.bat");
        }

        File.WriteAllLines(envPath + Path.DirectorySeparatorChar + "error.bat", batCMDs.ToArray());

        var process = new System.Diagnostics.ProcessStartInfo();
        process.WorkingDirectory = envPath;
        process.FileName = "bash";
        process.Arguments = "-c \"source error.bat\"";
        System.Diagnostics.Process.Start(process);

        Debug.LogError(errorMessage);
    }
}
#endif



