using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

public class iOSPostProcessBuildActions
{
    
    /**
     * Credits: https://stackoverflow.com/a/54370793/2706176
     */
    [PostProcessBuild]
    public static void ChangeXcodePlist(BuildTarget buildTarget, string path)
    {
        if (buildTarget != BuildTarget.iOS) return;
        var plistPath = path + "/Info.plist";
        var plist = new PlistDocument();
        plist.ReadFromFile(plistPath);

        var rootDict = plist.root;
        
        // Skip App Store Connect export compliance questionnaire
        rootDict.SetBoolean("ITSAppUsesNonExemptEncryption", false);
        // Enable iTunes file sharing
        rootDict.SetBoolean("UIFileSharingEnabled", true);
        // Enable Documents folder in the Files app
        rootDict.SetBoolean("LSSupportsOpeningDocumentsInPlace", true);
        
        File.WriteAllText(plistPath, plist.WriteToString());
    }
}