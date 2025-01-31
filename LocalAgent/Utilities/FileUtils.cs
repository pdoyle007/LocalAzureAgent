﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using LocalAgent.Variables;

namespace LocalAgent.Utilities
{
    public class FileUtils
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public byte[] GetMd5HashBytes(string filePath)
        {
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(filePath);
            return md5.ComputeHash(stream);
        }

        /// <summary>
        /// Helper function to calculate a MD5 hash for a file
        /// </summary>
        /// <param name="filePath">File path to hash</param>
        /// <returns>Lowercase string containing the file hash</returns>
        public virtual string GetMd5Hash(string filePath)
        {
            return BitConverter.ToString(GetMd5HashBytes(filePath))
                .Replace("-", "").ToLower();
        }

        /// <summary>
        /// Helper function to check if a file exists and has a specified extension
        /// </summary>
        /// <param name="fileInfo"></param>
        /// <param name="extension"></param>
        /// <returns></returns>
        public virtual bool CheckFileExtension(FileInfo fileInfo, string extension)
        {
            return fileInfo.Exists
                   && fileInfo.Extension.ToLower() == extension.ToLower();
        }

        /// <summary>
        /// Supports recursive searches for a file, by filename
        /// </summary>
        /// <param name="basePath">The base folder to search</param>
        /// <param name="fileName">The name of the file to seek</param>
        /// <param name="recursive">Default true</param>
        /// <returns>A list of fully qualified file paths which match the request</returns>
        public virtual IList<string> FindFiles(string basePath,string fileName, bool recursive = true)
        {
            var searchDepth = recursive 
                ? SearchOption.AllDirectories 
                : SearchOption.TopDirectoryOnly;

            if (new DirectoryInfo(basePath).Exists)
                return Directory.GetFiles(basePath,fileName, searchDepth);
            return new List<string>();
        }

        public virtual IList<string> FindFile(string path)
        {
            return new FileInfo(path).Exists
                ? new List<string> {path}
                : new List<string>();
        }

        /// <summary>
        /// Copies all the files from one folder into another, in parallel
        /// </summary>
        /// <param name="sourcePath">Folder content to copy</param>
        /// <param name="destinationPath">Folder destination to receive content</param>
        public void CloneFolder(string sourcePath, string destinationPath)
        {
            var sourceDirectory = new DirectoryInfo(sourcePath);
            if (!sourceDirectory.Exists)
            {
                throw new ArgumentException($"Folder not found: '{sourcePath}'", nameof(sourcePath));
            }

            sourceDirectory.CopyTo(destinationPath);
        }

        /// <summary>
        /// Deletes the content in a specified folder
        /// First delete the whole folder, then recreates an empty folder
        /// </summary>
        /// <param name="path"></param>
        public void DeleteFolderContent(string path)
        {
            var info = new DirectoryInfo(path);
            if (!info.Exists)
            {
                throw new ArgumentException($"Folder not found: '{path}'", nameof(path));
            }

            var paths = Directory.EnumerateDirectories(path);

            // ReadOnly flags must be cleared before the folder content is deleted
            ClearReadOnlyFlag(path);

            info.Delete(true);
            info.Create();
        }

        /// Recursive operation to clear Read Only flags from Files and Directories
        public void ClearReadOnlyFlag(string path) {
            Logger.Info($"Clearing ReadOnly flags in {path}");

            new DirectoryInfo(path).GetDirectories("*", SearchOption.AllDirectories)
                .ToList().ForEach(
                    di => {
                        di.Attributes &= ~FileAttributes.ReadOnly;
                        di.GetFiles("*", SearchOption.TopDirectoryOnly)
                            .ToList()
                            .ForEach(fi => fi.IsReadOnly = false);
                    }
                );
        }

        // Creates a folder, and subfolders
        public void CreateFolder(string path)
        {
            Directory.CreateDirectory(path);
        }

        public IList<string> FindFilesByPattern(PipelineContext context, string path, IList<string> patterns)
        {
            var buildTargets = new List<string>();

            foreach (var s in patterns)
            {
                if (s.StartsWith("**/*."))
                {
                    var searchExtension = s.Replace("**/*.", "*.");
                    buildTargets.AddRange(FindFiles(context.Variables[VariableNames.BuildSourcesDirectory], searchExtension, true));
                }
                else if (s.StartsWith("*."))
                {
                    buildTargets.AddRange(FindFiles(context.Variables[VariableNames.BuildSourcesDirectory], s, false));
                }
                else
                {
                    var searchPath = Path.Combine(context.Variables[VariableNames.BuildSourcesDirectory], s);
                    buildTargets.AddRange(FindFile(searchPath));
                }
            }

            return buildTargets;
        }
    }

    public static class ExtensionMethods
    {
        public static void CopyTo(this DirectoryInfo directoryInfo, string destinationPath, bool overwrite = true)
        {
            Parallel.ForEach(Directory.GetFileSystemEntries(directoryInfo.FullName, "*", SearchOption.AllDirectories), fileName => {
                var destFile = $"{destinationPath}{fileName[directoryInfo.FullName.Length..]}";
                Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
                if (File.Exists(fileName)) File.Copy(fileName, destFile, overwrite);
            });
        }

        public static string ToPath(this string path) {
            return path.Replace('\\',Path.DirectorySeparatorChar).Replace('/',Path.DirectorySeparatorChar);
        }
    }
}
