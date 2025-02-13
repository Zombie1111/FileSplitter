//Made by David Westberg, https://github.com/Zombie1111/FileSplitter
using zombSplit;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static System.Numerics.BitOperations;
using System.Text;

//FileSplitter.SplitFiles();
FileSplitter.MergeFiles();//Uncomment either SplitFiles or MergeFiles and build

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace zombSplit
#pragma warning restore IDE0130 // Namespace does not match folder structure
{
    public class FileSplitter
    {
        #region Config
        private static class SplitConfig
        {
            public const bool alwaysMergeBeforeSplit = false;//If true, DoMergeFiles() will be called in SplitFiles() before splitting,
                                                             //always the case if deleteSplittedFiles is true

            public const bool deleteSplittedFiles = false;//If true, after a file has been splitted the orginal file will be deleted

            public const bool dontModifyGitIgnore = false;//If true, splitted files paths wont be added to the .gitignore file

            public const bool NoMessagePausing = true;//If false, waits for key input before continueing when print message, useful to be able to actually read the messages

            public const bool NoErrorPausing = false;//Same as above but for errors

            //Files that are in or in any sub folder of a folder that has any of these names will never be splitted
            public static readonly string[] dictorariesToExclude = [".vs", "Build", ".git", "Library", "Temp", "xSplittedFiles_TEMPONLY_hf4n~"];

            //Files with these extensions will never be splitted
            public static readonly HashSet<string> fileExtensionsToExclude = [".meta", ".VC.db", ".VC.opendb"];
                                                                                                                       
            public const int splitFilesLargerThanMB = 99;//If a file is larger that this it gets splitted into files that are smaller than this

            public const int maxThreadCount = 4;
            public const int maxAttempts = 16;
            public const int newAttemptDelayMS = 200;
        }
        #endregion Config

        private List<SplitFile> splittedFiles = [];

        [Serializable]
        public class SplitFile
        {
            public List<string> splitFileNames = [];
            /// <summary>
            /// Relative to project base dir
            /// </summary>
            public string sourceRelativePath = string.Empty;
            public long sourceByteSize = 0;

            public static void WriteToFile(string filePath, List<SplitFile> splitFiles)
            {
                int attempts = SplitConfig.maxAttempts;

                while (attempts > 0)
                {
                    try
                    {
                        using FileStream steam = File.Open(filePath, FileMode.Create);
                        using BinaryWriter writer = new(steam);

                        writer.Write(splitFiles.Count);
                        foreach (var splitFile in splitFiles)
                        {
                            writer.Write(splitFile.splitFileNames.Count);
                            foreach (var name in splitFile.splitFileNames)
                            {
                                writer.Write(name);
                            }

                            writer.Write(splitFile.sourceRelativePath);
                            writer.Write(splitFile.sourceByteSize);
                        }

                        break;
                    }
                    catch
                    {
                        attempts--;
                        Thread.Sleep(SplitConfig.newAttemptDelayMS);
                    }
                }

                if (attempts == 0)
                {
                    Debug.LogError("Error writing to splitted data file: " + filePath);
                }
            }

            public static List<SplitFile> ReadFromFile(string filePath)
            {
                int attempts = SplitConfig.maxAttempts;
                List<SplitFile> splitFiles = [];
                bool probNoFile = false;

                while (attempts > 0)
                {
                    try
                    {
                        splitFiles.Clear();
                        if (File.Exists(filePath) == false)
                        {
                            probNoFile = true;

                            if (attempts > 1)
                            {
                                attempts = 1;//99.9% not splitted yet, still 1 extra attempt just in case
                                Thread.Sleep(SplitConfig.newAttemptDelayMS);
                            }
                            else attempts--;

                            continue;
                        }

                        probNoFile = false;
                        using FileStream steam = File.Open(filePath, FileMode.Open);
                        using BinaryReader reader = new(steam);
                        int count = reader.ReadInt32();

                        for (int i = 0; i < count; i++)
                        {
                            SplitFile splitFile = new();

                            int fileNamesCount = reader.ReadInt32();
                            for (int j = 0; j < fileNamesCount; j++)
                            {
                                splitFile.splitFileNames.Add(reader.ReadString());
                            }

                            splitFile.sourceRelativePath = reader.ReadString();
                            splitFile.sourceByteSize = reader.ReadInt64();

                            splitFiles.Add(splitFile);
                        }

                        break;
                    }
                    catch
                    {
                        attempts--;
                        Thread.Sleep(SplitConfig.newAttemptDelayMS);
                    }
                }

                if (attempts == 0 && probNoFile == false)
                {
                    Debug.LogError("Error reading splitted data file: " + filePath);
                }
                
                return splitFiles;
            }
        }

        /// <summary>
        /// Merges splitted files
        /// </summary>
        public static void MergeFiles()
        {
            GetSplitSaveFile().DoMergeFiles(true);
        }

        /// <summary>
        /// Restores the .gitignore and returns splitFolderPath
        /// </summary>
        private static string RestoreGitIgnore()
        {
            //Restore .gitIgnore
            string ignoreFullPath = TryGetGitIgnoreFullPath(out string splitFolderPath);
            if (ignoreFullPath == null) return splitFolderPath;

            string ogIgnoreFullPath = GetFullOgGitIgnorePath(ignoreFullPath);

            try
            {
                if (File.Exists(ogIgnoreFullPath) == true)
                {
                    File.WriteAllBytes(ignoreFullPath, File.ReadAllBytes(ogIgnoreFullPath));
                    File.Delete(ogIgnoreFullPath);
                }
            }
            catch
            {
                Debug.LogError("Error restoring orginal .gitignore: " + ignoreFullPath + " ogIg: " + ogIgnoreFullPath);
            }

            return splitFolderPath;
        }

        private void DoMergeFiles(bool logIfNoFiles = true)
        {
            //Check if we have files to merge
            if (splittedFiles.Count == 0)
            {
                if (logIfNoFiles == true) Debug.Log("Got no files to merge");
                return;
            }

            //Restore .gitIgnore
            string splitFolderPath = RestoreGitIgnore();
            string appPath = GetProjectBasePath();
            if (splitFolderPath == null) return;

            //Merge the files
            ParallelOptions options = new() { MaxDegreeOfParallelism = SplitConfig.maxThreadCount };
            object lockObj = new();

            Parallel.ForEach(splittedFiles, options, file =>
            {
                DoMergeFile(file);
            });

            Debug.Log("Merged " + splittedFiles.Count + " files");
            DeleteSplittedFiles(splitFolderPath);

            void DoMergeFile(SplitFile file)
            {
                //Verify all split files exist and get their data
                int attempts = SplitConfig.maxAttempts;
                lock (lockObj)
                {
                    Debug.Log("Merging: " + file.sourceRelativePath, true);
                }

                while (attempts > 0)
                {
                    try
                    {
                        string outputFilePath = Path.GetFullPath(file.sourceRelativePath, appPath);

#pragma warning disable CS8602 // Dereference of a possibly null reference.
                        string sourceDir = new FileInfo(outputFilePath).Directory.FullName;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                        Directory.CreateDirectory(sourceDir);

                        using FileStream outputStream = new(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920);
                        foreach (string splitPathRel in file.splitFileNames)
                        {
                            string splitPath = Path.Combine(splitFolderPath, splitPathRel);
                            if (File.Exists(splitPath) == false)
                            {
                                lock (lockObj)
                                {
                                    Debug.LogError("Missing split file at " + splitPath + " for " + file.sourceRelativePath);
                                }

                                return;
                            }

                            using FileStream inputStream = new(splitPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 81920);
                            inputStream.CopyTo(outputStream);
                            inputStream.Close();
                        }

                        outputStream.Close();
                        break;
                    }
                    catch
                    {
                        attempts--;
                        Thread.Sleep(SplitConfig.newAttemptDelayMS);
                    }
                }

                if (attempts == 0)
                {
                    lock (lockObj)
                    {
                        Debug.LogError("Error merging: " + file.sourceRelativePath);
                    }
                }
            }
        }

        /// <summary>
        /// Splits the files
        /// </summary>
        public static void SplitFiles()
        {
#pragma warning disable CS0162 // Unreachable code detected
            if (SplitConfig.deleteSplittedFiles == true || SplitConfig.alwaysMergeBeforeSplit == true)
                MergeFiles();//Running DoSplitFiles twice in a row without running MergeFiles inbetween will
                             //permanently delete splitted files, this to prevent that!! (Only if deleteSplittedFiles)
#pragma warning restore CS0162 // Unreachable code detected

            GetSplitSaveFile().DoSplitFiles();
        }

        private void DeleteSplittedFiles(string splitFolderPath)
        {
            int attempts = SplitConfig.maxAttempts;
            Task? task = null;

            while (attempts > 0)
            {
                try
                {
                    //Delete old split files
                    task = Task.Run(() =>
                    {
                        splittedFiles.Clear();
                        SaveChanges();
                    });

                    Directory.CreateDirectory(@splitFolderPath);
                    DirectoryInfo dic = new(splitFolderPath);

                    foreach (FileInfo file in dic.GetFiles())
                    {
                        file.Delete();
                    }

                    task.Wait();
                    break;
                }
                catch
                {
                    attempts--;
                    task?.Wait();
                    Thread.Sleep(SplitConfig.newAttemptDelayMS);
                }
            }

            if (attempts == 0)
            {
                Debug.LogError("Unable to delete old splitted files: " + splitFolderPath);
            }
        }

        private static string projPath = string.Empty;

        private static string GetProjectBasePath()
        {
            if (projPath != null && projPath.Length >= 5) return new(projPath); //Make sure its not a reference

            Assembly assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            string? path;
            
            if (assembly == null || assembly.Location.Length < 5) path = System.Environment.ProcessPath;
            else path = assembly.Location;

            string? basePath = Path.GetDirectoryName(path);
            if (basePath == null || basePath.Length < 5)
            {
                Debug.LogError("CRITICAL: Unable to get valid ProjectBasePath", true);
                throw new Exception("CRITICAL: Unable to get valid ProjectBasePath");
            }

            basePath = basePath.Replace("\\", "/");
            projPath = basePath;
            return basePath;
        }

        /// <summary>
        /// Returns the full path to the .gitignore file, returns null if no .gitignore file exist (splitFolderFullPath is never null)
        /// </summary>
        private static string TryGetGitIgnoreFullPath(out string splitFolderFullPath)
        {
            string appPath = GetProjectBasePath();
            string fullIgnorePath = appPath + "/.gitignore";
            splitFolderFullPath = appPath + "/xSplittedFiles_TEMPONLY_hf4n~";
#pragma warning disable CS0162 // Unreachable code detected
            if (SplitConfig.dontModifyGitIgnore == true) return null;
#pragma warning restore CS0162 // Unreachable code detected

            if (File.Exists(fullIgnorePath) == false)
            {
                //No .gitignore is found
                Debug.Log("Found no .gitignore at " + fullIgnorePath, true);
                return null;
            }
            
            return fullIgnorePath;
        }

        private static class Debug
        {
            public static void LogError(string msg, bool forcePause = false)
            {
                Console.WriteLine(msg);
                if (forcePause == false && SplitConfig.NoErrorPausing == true) return;

                Console.WriteLine(string.Empty);
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();

                Console.Clear();
            }

            public static void Log(string msg, bool noPause = false)
            {
                Console.WriteLine(msg);
                if (noPause == true || SplitConfig.NoMessagePausing == true) return;

                Console.WriteLine(string.Empty);
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();

                Console.Clear();
            }
        }

        private static string GetFullOgGitIgnorePath(string fullGitIgnorePath)
        {
            return fullGitIgnorePath.Replace("/.gitignore", "/ogGitIgnore.zombIgnore~");
        }

        private void DoSplitFiles()
        {
            string fullIgnorePath = string.Empty;
            string splitFolderPath = string.Empty;

            Task task = Task.Run(() =>
            {
                //Restore .gitIgnore
                RestoreGitIgnore();

                //Backup the .gitignore file
                fullIgnorePath = TryGetGitIgnoreFullPath(out splitFolderPath);

                if (fullIgnorePath != null)
                {
                    try
                    {
                        File.WriteAllBytes(GetFullOgGitIgnorePath(fullIgnorePath), File.ReadAllBytes(fullIgnorePath));
                    }
                    catch
                    {
                        Debug.LogError("Error creating .gitignore copy: " + fullIgnorePath);
                    }
                }

                //Delete previous temp split files
                DeleteSplittedFiles(splitFolderPath);
            });

            //Split files
            long minSizeInBytes = SplitConfig.splitFilesLargerThanMB * 1000000;
            string appPath = GetProjectBasePath();
            List<string> gitIgnorePaths = [];

            var fPaths = GetPathToAllFiles(out gitIgnorePaths);
            int count = fPaths.Count;
            object lockObj = new();

            ParallelOptions options = new() { MaxDegreeOfParallelism = SplitConfig.maxThreadCount };
            task.Wait();

            Parallel.ForEach(fPaths, options, filePath =>
            {
                lock (lockObj)
                {
                    Debug.Log("Splitting: " + filePath, true);
                }

                DoSplitFile(filePath);
            });

            task = Task.Run(() => SaveChanges());

            //Did we split anything?
            if (splittedFiles.Count == 0)
            {
                RestoreGitIgnore();
                Debug.Log("Got no files to split");
                task.Wait();
                return;
            }

            Debug.Log("Splitted " + splittedFiles.Count + " files");

            //Add splitted files to .gitignore
            if (fullIgnorePath != null)
            {
                gitIgnorePaths.Insert(0, "# Auto generated by FileSplitter. NOTE, changes made to this .gitignore will be overwritten when you merge or split files");

                string textToAppend = Environment.NewLine + string.Join(Environment.NewLine, gitIgnorePaths);

                try
                {
                    File.AppendAllText(fullIgnorePath, textToAppend);
                }
                catch
                {
                    Debug.LogError("Error updating .gitignore: " + fullIgnorePath);
                }
            }

            task.Wait();

            void DoSplitFile(string filePath)
            {
                int maxAttempts = SplitConfig.maxAttempts;
                List<string> splitFullPaths = new(2);
                string relPath = Path.GetRelativePath(appPath, filePath);
                long sourceByteSize = 0;

                while (maxAttempts > 0)
                {
                    try
                    {
                        splitFullPaths.Clear();
                        FileInfo fInfo = new(filePath);

#pragma warning disable CS8602 // Dereference of a possibly null reference.
                        ReadOnlySpan<byte> pathBytes = Encoding.UTF8.GetBytes(Path.GetRelativePath(appPath, fInfo.Directory.FullName)).AsSpan();
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                        uint pathHash = Hash32(ref pathBytes, 420);
                        sourceByteSize = fInfo.Length;
                        string splitFileNameBase = Path.GetFileName(filePath) + "__";
                        int splitNumber = 0;

                        using (FileStream fs = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            byte[] buffer = new byte[minSizeInBytes];
                            int bytesRead;

                            while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                string splitFileName = splitFileNameBase + pathHash + splitNumber + ".zombSplit";
                                string splitFilePath = Path.Combine(splitFolderPath, splitFileName);

                                File.WriteAllBytes(splitFilePath, buffer[..bytesRead]); //Trim buffer if needed
                                splitFullPaths.Add(splitFileName);
                                splitNumber++;
                            }
                        }

                        if (SplitConfig.deleteSplittedFiles == true)
                        {
#pragma warning disable CS0162 // Unreachable code detected
                            File.Delete(filePath);
#pragma warning restore CS0162 // Unreachable code detected
                        }

                        break;
                    }
                    catch
                    {
                        maxAttempts--;
                        Thread.Sleep(SplitConfig.newAttemptDelayMS);
                    }
                }

                lock (lockObj)
                {
                    if (maxAttempts == 0 || sourceByteSize == 0)
                    {
                        Debug.LogError("Failed to split (Bytes " + sourceByteSize + "): " + filePath);
                        return;
                    }

                    splittedFiles.Add(new()
                    {
                        splitFileNames = splitFullPaths,
                        sourceRelativePath = relPath,
                        sourceByteSize = sourceByteSize
                    });
                }
            }
        }

        private void SaveChanges()
        {
            SplitFile.WriteToFile(GetSplittedFilesDataPath(), splittedFiles);
        }

        private static string GetSplittedFilesDataPath()
        {
            return GetProjectBasePath() + "/xSplittedFilesData_8hlk.zombSplitData~";
        }

        private static List<string> GetPathToAllFiles(out List<string> gitIgnoreFilePaths)
        {
            gitIgnoreFilePaths = new(16);
            var gitPaths = gitIgnoreFilePaths;
            List<string> filePaths = new(16);
            long minSizeInBytes = SplitConfig.splitFilesLargerThanMB * 1000000;
            object lockObj = new();
            string searchFolderPath = GetProjectBasePath();

            try
            {
                GetFilesRecursive(searchFolderPath);
            }
            catch
            {
                //This has never failed so doing attempts is not needed
                gitIgnoreFilePaths.Clear();
                filePaths.Clear();
                Debug.LogError("Error getting files to split in: " + searchFolderPath);
            }

            return filePaths;

            void GetFilesRecursive(string sDir)
            {
                List<Task> tasks = new(4);

                foreach (string dicPath in Directory.GetDirectories(sDir))
                {
                   //Ignore dictoraries
                   bool ignore = false;

                   foreach (string dic in SplitConfig.dictorariesToExclude)
                   {
                       if (dicPath.Contains(dic) == true)
                       {
                           ignore = true;
                           break;
                       }
                   }

                   if (ignore == true) continue;

                   tasks.Add(Task.Run(() => GetFilesRecursive(dicPath)));
                }

                foreach (string filePath in Directory.GetFiles(sDir))
                {
                   //Ignore file extensions
                   if (SplitConfig.fileExtensionsToExclude.Contains(Path.GetExtension(filePath)) == true) continue;
                                       
                   //Ignore too small files
                   string fullPath = Path.GetFullPath(filePath, searchFolderPath);
                   var fData = new FileInfo(fullPath);
                   
                   if (fData.Length < minSizeInBytes) continue;
                   if (fData.Exists == false) continue;
                   
                   //Add path to list
                   string theFilePath = filePath.Replace("\\", "/");
                   lock (lockObj)
                   {
                       gitPaths.Add(theFilePath.Replace(searchFolderPath + "/", string.Empty));
                       filePaths.Add(theFilePath);
                   }
                }

                foreach (Task task in tasks)
                {
                    task.Wait();
                }
            }
        }

        /// <summary>
        /// Returns the Splitter asset, returns null if it has been deleted
        /// </summary>
        public static FileSplitter GetSplitSaveFile()
        {
            FileSplitter splitter = new()
            {
                splittedFiles = SplitFile.ReadFromFile(GetSplittedFilesDataPath())
            };

            return splitter;
        }

        #region MurmurHash
        //From https://github.com/JeremyEspresso/MurmurHash?tab=readme-ov-file,
        //MIT License
        //
        //Copyright(c) 2022 JeremyEspresso
        //
        //Permission is hereby granted, free of charge, to any person obtaining a copy
        //of this software and associated documentation files(the "Software"), to deal
        //in the Software without restriction, including without limitation the rights
        //to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
        //copies of the Software, and to permit persons to whom the Software is
        //furnished to do so, subject to the following conditions:
        //
        //The above copyright notice and this permission notice shall be included in all
        //copies or substantial portions of the Software.
        //
        //THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
        //IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
        //FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
        //AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
        //LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
        //OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
        //SOFTWARE.
        /// <summary>
        /// Hashes the <paramref name="bytes"/> into a MurmurHash3 as a <see cref="uint"/>.
        /// </summary>
        /// <param name="bytes">The span.</param>
        /// <param name="seed">The seed for this algorithm.</param>
        /// <returns>The MurmurHash3 as a <see cref="uint"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint Hash32(ref ReadOnlySpan<byte> bytes, uint seed)
        {
            ref byte bp = ref MemoryMarshal.GetReference(bytes);
            ref uint endPoint = ref Unsafe.Add(ref Unsafe.As<byte, uint>(ref bp), bytes.Length >> 2);
            if (bytes.Length >= 4)
            {
                do
                {
                    seed = RotateLeft(seed ^ RotateLeft(Unsafe.ReadUnaligned<uint>(ref bp) * 3432918353U, 15) * 461845907U, 13) * 5 - 430675100;
                    bp = ref Unsafe.Add(ref bp, 4);
                } while (Unsafe.IsAddressLessThan(ref Unsafe.As<byte, uint>(ref bp), ref endPoint));
            }

            var remainder = bytes.Length & 3;
            if (remainder > 0)
            {
                uint num = 0;
                if (remainder > 2) num ^= Unsafe.Add(ref endPoint, 2) << 16;
                if (remainder > 1) num ^= Unsafe.Add(ref endPoint, 1) << 8;
                num ^= endPoint;

                seed ^= RotateLeft(num * 3432918353U, 15) * 461845907U;
            }

            seed ^= (uint)bytes.Length;
            seed = (uint)((seed ^ (seed >> 16)) * -2048144789);
            seed = (uint)((seed ^ (seed >> 13)) * -1028477387);
            return seed ^ seed >> 16;
        }
        #endregion
    }
}
