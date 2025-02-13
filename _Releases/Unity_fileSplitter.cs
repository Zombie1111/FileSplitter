//Made by David Westberg, https://github.com/Zombie1111/FileSplitter
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Diagnostics;

namespace zombFiles
{
    public class Unity_fileSplitter : AssetPostprocessor
    {
        private const string splitBasePath = "";//Should be relative to project root directory (Not assets folder)
        private static bool isClosing = false;

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            if (isClosing == true) return;

            //Setup auto split and merge
            EditorApplication.quitting -= OnEditorClose;
            EditorApplication.quitting += OnEditorClose;

            if (SessionState.GetBool("xSplitterHasMerged_fkxr", false) == true) return;
            OnEditorStart();
            SessionState.SetBool("xSplitterHasMerged_fkxr", true);
        }

        [MenuItem("Tools/File Splitting/Merge Files")]
        public static void OnEditorStart()
        {
            string path = Application.dataPath + splitBasePath + "xMergeFiles.exe";
            path = path.Replace("Assets", string.Empty);

            Process proc = Process.Start(path);
            proc.WaitForExit();

            AssetDatabase.Refresh();//If merged anything make sure the merged files gets imported
        }

        [MenuItem("Tools/File Splitting/Split Files")]
        public static void SplitFiles()
        {
            OnEditorClose();
            isClosing = false;
            AssetDatabase.Refresh();//If splitted anything and delete splitted is true, make sure the deleted files disepear
        }

        public static void OnEditorClose()
        {
            isClosing = true;
            string path = Application.dataPath + splitBasePath + "xSplitFiles.exe";
            path = path.Replace("Assets", string.Empty);

            Process proc = Process.Start(path);
            proc.WaitForExit();
        }
    }
}
#endif
