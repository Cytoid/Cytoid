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
        // Disable Documents folder in the Files app; this interferes with "Copy to Cytoid" feature
        rootDict.SetBoolean("LSSupportsOpeningDocumentsInPlace", false);

        var documentTypes = rootDict.CreateArray("CFBundleDocumentTypes");
        var dict = documentTypes.AddDict();
        dict.CreateArray("CFBundleTypeIconFiles");
        dict.SetString("CFBundleTypeName", "Cytoid Level Document");
        dict.SetString("CFBundleTypeRole", "Viewer");
        dict.SetString("LSHandlerRank", "Owner");
        dict.CreateArray("LSItemContentTypes").AddString("io.cytoid.cytoidlevel");

        var typeDec = rootDict.CreateArray("UTExportedTypeDeclarations");
        dict = typeDec.AddDict();
        dict.CreateArray("UTTypeConformsTo").AddString("public.data");
        dict.SetString("UTTypeDescription", "Cytoid Level Document");
        dict.CreateArray("UTTypeIconFiles");
        dict.SetString("UTTypeIdentifier", "io.cytoid.cytoidlevel");
        var subDict = dict.CreateDict("UTTypeTagSpecification");
        subDict.SetString("public.filename-extension", "cytoidlevel");
        subDict.SetString("public.mime-type", "application/cytoid");

        File.WriteAllText(plistPath, plist.WriteToString());
    }
}