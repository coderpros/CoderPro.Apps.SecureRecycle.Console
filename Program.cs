// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="coderPro.net">
//   Copyright 2024 coderPro.net. All rights reserved.
// </copyright>
// <summary>
//   This program securely deletes files from the recycle bin using various algorithms.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace CoderPro.Apps.SecureRecycle.Console
{
    #region Usings

    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Threading.Tasks;

    using Shell32;
    #endregion

    /// <summary>
    /// The main program class for securely deleting files from the recycle bin.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1503:CurlyBracketsMustNotBeOmitted", Justification = "Reviewed. Suppression is OK here.")]
    internal class Program
    {
        /// <summary>
        /// Indicates whether debug mode is enabled.
        /// </summary>
        private static bool _debug;

        /// <summary>
        /// The deletion algorithm to use.
        /// </summary>
        private static string _deletionAlgorithm = string.Empty;

        private static System.DateTime _startTime = System.DateTime.MinValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="Program"/> class.
        /// </summary>
        internal Program()
        {
        }

        #region Events
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// <param name="args">The command-line arguments.</param>
        [STAThread]
        public static void Main(string[] args)
        {
            if (args.Contains("/debug"))
            {
                _debug = true;
            }

            var algorithmIndex = Array.IndexOf(args, "/protocol");

            if (algorithmIndex != -1)
            {
                _deletionAlgorithm = args[algorithmIndex + 1];
            }

            _startTime = DateTime.Now;

            var files = GetFilesInRecycleBin();

            if (files.Count > 0)
            {
                DeleteFilesInRecycleBinAsync(files, _deletionAlgorithm).GetAwaiter().GetResult();
            }

            OnCompleted();
        }
        #endregion

        #region Helpers
        /// <summary>
        /// Gets the files in the recycle bin.
        /// </summary>
        /// <returns>A <see cref="FolderItems"/> collection of files in the recycle bin.</returns>
        private static List<string> GetFilesInRecycleBin()
        {
            var shell = new Shell();
            var recyclerFolder = shell.NameSpace(10);
            var items = recyclerFolder.Items();

            var filePaths = new List<string>();

            for (var i = 0; i < items.Count; i++)
            {
                var item = items.Item(i);
                var attributes = File.GetAttributes(item.Path);

                if (item.IsFolder && attributes != FileAttributes.Archive)
                {
                    // Recursively retrieve files from the folder
                    GetFilesFromFolder(item.GetFolder as Folder, filePaths);
                }
                else
                {
                    filePaths.Add(item.Path);
                }
            }

            return filePaths;
        }

        private static void GetFilesFromFolder(Folder folder, List<string> filePaths)
        {
            var items = folder.Items();

            for (var i = 0; i < items.Count; i++)
            {
                var item = items.Item(i);
                var attributes = File.GetAttributes(item.Path);

                if (item.IsFolder && attributes != FileAttributes.Archive)
                {
                    GetFilesFromFolder(item.GetFolder as Folder, filePaths);
                }
                else
                {
                    filePaths.Add(item.Path);
                }
            }
        }

        /// <summary>
        /// Deletes the files in the recycle bin asynchronously using the specified deletion algorithm.
        /// </summary>
        /// <param name="items">The files to delete.</param>
        /// <param name="deletionAlgorithm">The deletion algorithm to use.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private static async Task DeleteFilesInRecycleBinAsync(List<string> items, string deletionAlgorithm = "")
        {
            var tasks = items.Select(t => Task.Run(() => EncryptAndDeleteFileAsync(t, deletionAlgorithm))).ToList();

            await Task.WhenAll(tasks).ConfigureAwait(true);
        }

        /// <summary>
        /// Encrypts and deletes a file asynchronously using the specified deletion algorithm.
        /// </summary>
        /// <param name="filePath">The path of the file to delete.</param>
        /// <param name="deletionAlgorithm">The deletion algorithm to use.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private static async Task EncryptAndDeleteFileAsync(string filePath, string deletionAlgorithm)
        {
            await Task.Run(() => EncryptFile(filePath)).ConfigureAwait(true);
            await Task.Run(() => DeleteFile(filePath, deletionAlgorithm)).ConfigureAwait(false);
        }

        /// <summary>
        /// Encrypts the specified file.
        /// </summary>
        /// <param name="filePath">The path of the file to encrypt.</param>
        private static void EncryptFile(string filePath)
        {
            if (_debug) Console.WriteLine($"Started encrypting {filePath}");

            using (var aes = Aes.Create())
            {
                aes.GenerateKey();
                aes.GenerateIV();

                var fileContent = File.ReadAllBytes(filePath);

                using (var memoryStream = new MemoryStream())
                {
                    using (var cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(fileContent, 0, fileContent.Length);
                    }

                    var encryptedContent = memoryStream.ToArray();
                    File.WriteAllBytes(filePath, encryptedContent);
                }
            }

            if (_debug) Console.WriteLine($"Finished encrypting {filePath}");
        }

        /// <summary>
        /// Deletes the specified file using the specified deletion algorithm.
        /// </summary>
        /// <param name="filePath">The path of the file to delete.</param>
        /// <param name="deletionAlgorithm">The deletion algorithm to use.</param>
        private static void DeleteFile(string filePath, string deletionAlgorithm = "")
        {
            if (_debug) Console.WriteLine($"Started secure delete ({deletionAlgorithm}) of {filePath}");

            switch (deletionAlgorithm)
            {
                case "dod7":
                    DoD7Delete(filePath);
                    break;
                case "gutmann":
                    GutmannDelete(filePath);
                    break;
                case "ones":
                    WriteOnes(filePath);
                    break;
                case "zeros":
                    WriteZeros(filePath);
                    break;
                default:
                    ShredFile(filePath);
                    break;
            }

            File.Delete(filePath);

            if (_debug) Console.WriteLine($"Finished secure delete of {filePath}");
        }

        /// <summary>
        /// Called when all files have been encrypted and deleted.
        /// </summary>
        private static void OnCompleted()
        {
            // Delete any remaining directories in the Recycle Bin
            var shell = new Shell();
            var recyclerFolder = shell.NameSpace(10);
            var items = recyclerFolder.Items();

            var directoryPaths = new List<string>();

            for (var i = 0; i < items.Count; i++)
            {
                var item = items.Item(i);
                if (item.IsFolder)
                {
                    directoryPaths.Add(item.Path);
                }
            }

            directoryPaths = directoryPaths.OrderByDescending(path => path.Length).ToList();

            foreach (var dirPath in directoryPaths)
            {
                try
                {
                    Directory.Delete(dirPath, true);
                    if (_debug) Console.WriteLine($"Deleted directory: {dirPath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting directory {dirPath}: {ex.Message}");
                }
            }

            Console.WriteLine("All files have been encrypted and deleted.");
            Console.WriteLine($"Elapsed time: {DateTime.Now - _startTime}");
        }
        #endregion

        #region Delete Helpers
        /// <summary>
        /// Deletes the specified file using the DoD 5220.22-M (7-pass) deletion protocol.
        /// </summary>
        /// <param name="filePath">The path of the file to delete.</param>
        private static void DoD7Delete(string filePath)
        {
            for (var pass = 0; pass < 7; pass++)
            {
                switch (pass)
                {
                    case 0:
                    case 2:
                    case 4:
                        WriteZeros(filePath);
                        break;
                    case 1:
                    case 3:
                    case 5:
                        WriteOnes(filePath);
                        break;
                    case 6:
                        ShredFile(filePath);
                        break;
                }
            }
        }

        /// <summary>
        /// Deletes the specified file using the Gutmann (35-pass) deletion protocol.
        /// </summary>
        /// <param name="filePath">The path of the file to delete.</param>
        private static void GutmannDelete(string filePath)
        {
            for (var pass = 0; pass < 35; pass++)
            {
                if (pass < 4 || pass is >= 27 and < 31)
                {
                    ShredFile(filePath);
                }
                else if (pass % 2 == 0)
                {
                    WriteZeros(filePath);
                }
                else
                {
                    WriteOnes(filePath);
                }
            }
        }

        /// <summary>
        /// Shreds the specified file by overwriting it with random data.
        /// </summary>
        /// <param name="filePath">The path of the file to shred.</param>
        private static void ShredFile(string filePath)
        {
            var fileLength = new FileInfo(filePath).Length;
            var randomBytes = new byte[fileLength];
            RandomNumberGenerator.Fill(randomBytes);
            File.WriteAllBytes(filePath, randomBytes);
        }

        /// <summary>
        /// Overwrites the specified file with zeros.
        /// </summary>
        /// <param name="filePath">The path of the file to overwrite.</param>
        private static void WriteZeros(string filePath)
        {
            var fileLength = new FileInfo(filePath).Length;
            var zeroBytes = new byte[fileLength];
            File.WriteAllBytes(filePath, zeroBytes);
        }

        /// <summary>
        /// Overwrites the specified file with ones.
        /// </summary>
        /// <param name="filePath">The path of the file to overwrite.</param>
        private static void WriteOnes(string filePath)
        {
            var fileLength = new FileInfo(filePath).Length;
            var oneBytes = new byte[fileLength];
            for (var i = 0; i < oneBytes.Length; i++)
            {
                oneBytes[i] = 0xFF;
            }

            File.WriteAllBytes(filePath, oneBytes);
        }
        #endregion
    }
}
