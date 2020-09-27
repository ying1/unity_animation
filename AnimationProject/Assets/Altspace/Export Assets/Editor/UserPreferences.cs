using UnityEngine;

[CreateAssetMenu]
public class UserPreferences : ScriptableObject {

    public string ProjectName;
    public string ExportPath;
    public bool IsWindowsBuild;
    public bool IsAndroidBuild;
    public string Email;
    public string Password;
    public bool RememberUserLogin;

}
