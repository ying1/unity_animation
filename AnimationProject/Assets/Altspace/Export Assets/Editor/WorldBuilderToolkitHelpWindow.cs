using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


public class WorldBuilderToolkitHelpWindow : EditorWindow {

    static WorldBuilderToolkitHelpWindow()
    {
        ShowWindow();
    }

    [MenuItem("AltspaceVR/Help")]
    public static void ShowWindow()
    {

        EditorWindow.GetWindow<WorldBuilderToolkitHelpWindow>(true, "World Builder Toolkit Help Window");


    }

    Texture2D BG;

    private void OnGUI()
    {
        this.minSize = new Vector2(600, 429);
        this.maxSize = new Vector2(600, 429);
        //position = new Rect(600, 300, 600, 500);

        if(BG == null)
        {
            BG = Resources.Load("bg", typeof(Texture2D)) as Texture2D;
        }

        GUIStyle windowStyle = new GUIStyle();
        windowStyle.normal.textColor = Color.white;
        windowStyle.fontSize = 30;

        GUIStyle contentStyle = new GUIStyle();
        contentStyle.normal.textColor = Color.white;
        contentStyle.fontSize = 15;
        contentStyle.margin = new RectOffset(5, 0, 0, 0);

       

        GUI.DrawTexture(new Rect(0, 0, 600, 429), BG);

        
        GUILayout.Label("What do you want to do?",windowStyle);

        GUILayout.Space(50);

        GUILayout.Label("Getting Started",contentStyle);

        //GUI.backgroundColor = Color.grey;
        GUI.color = Color.white;

        if(GUILayout.Button("How do I get started with the World Building Toolkit"))
        {
            Application.OpenURL("https://help.altvr.com/hc/en-us/articles/360015520014-How-do-I-get-started-with-the-World-Building-Toolkit-Unity-Uploader-");
        }

        if (GUILayout.Button("World Building Toolkit FAQs"))
        {
            Application.OpenURL("https://help.altvr.com/hc/en-us/articles/360015560614-Unity-Uploader-FAQ");
        }

        GUILayout.Space(50);

        GUILayout.Label("Essensial Skills Videos (Coming Soon!)", contentStyle);
    }
}
