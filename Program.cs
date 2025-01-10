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
    using System.Runtime.InteropServices;
    using System.Security.Cryptography;
    using System.Threading.Tasks;

    using Shell32;
    #endregion

    /// <summary>
    /// The main program class for securely deleting files from the recycle bin.
    /// </summary>
    [SuppressMessage(
        "StyleCop.CSharp.LayoutRules",
        "SA1503:CurlyBracketsMustNotBeOmitted",
        Justification = "Reviewed. Suppression is OK here.")]
    internal class Program
    {
        /// <summary>
        /// Indicates whether debug mode is enabled.
        /// </summary>
        private static bool _debug;

        /// <summary>
        /// Indicates whether the file should be encrypted prior to deletion.
        /// </summary>
        private static bool _encrypt;

        /// <summary>
        /// The deletion algorithm to use.
        /// </summary>
        private static string _deletionAlgorithm = string.Empty;

        private static long _deletionCount;

        /// <summary>
        /// The object used to lock the console for thread safety.
        /// </summary>
        private static readonly object ConsoleLock = new();

        /// <summary>
        /// The start time of the last deletion.
        /// </summary>
        private static DateTime _startTime = DateTime.MinValue;


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
            _debug = args.Contains("/debug");
            _encrypt = args.Contains("/encrypt");

            var algorithmIndex = Array.IndexOf(args, "/protocol");

            if (algorithmIndex != -1)
            {
                _deletionAlgorithm = args[algorithmIndex + 1];
            }

            _startTime = DateTime.Now;

            var files = GetFilesInRecycleBin();

            if (files.Count > 0)
            {
                DeleteFilesInRecycleBinAsync(files, files.Count, _deletionAlgorithm).GetAwaiter().GetResult();
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

                Marshal.ReleaseComObject(item);
            }

            Marshal.ReleaseComObject(items);
            Marshal.ReleaseComObject(recyclerFolder);
            Marshal.ReleaseComObject(shell);

            return filePaths;
        }

        private static void GetFilesFromFolder(Folder? folder, List<string> filePaths)
        {
            if (folder == null)
            {
                return;
            }

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

                Marshal.ReleaseComObject(item);
            }

            Marshal.ReleaseComObject(items);
            Marshal.ReleaseComObject(folder);
        }

        /// <summary>
        /// Deletes the files in the recycle bin asynchronously using the specified deletion algorithm.
        /// </summary>
        /// <param name="items">The files to delete.</param>
        /// <param name="totalFiles">The total number of files being deleted.</param>
        /// <param name="deletionAlgorithm">The deletion algorithm to use.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private static async Task DeleteFilesInRecycleBinAsync(List<string> items, long totalFiles, string deletionAlgorithm = "")
        {
            var tasks = items.Select(t => Task.Run(() => EncryptAndDeleteFileAsync(t, totalFiles, deletionAlgorithm))).ToList();

            // ReSharper disable once AsyncApostle.AsyncAwaitMayBeElidedHighlighting
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        /// <summary>
        /// Encrypts and deletes a file asynchronously using the specified deletion algorithm.
        /// </summary>
        /// <param name="filePath">The path of the file to delete.</param>
        /// <param name="totalFiles">The total number of files being deleted.</param>
        /// <param name="deletionAlgorithm">The deletion algorithm to use.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private static async Task EncryptAndDeleteFileAsync(string filePath, long totalFiles, string deletionAlgorithm)
        {
            if (_encrypt)
            {
                await Task.Run(() => EncryptFile(filePath)).ConfigureAwait(true);
            }

            await Task.Run(() =>
                DeleteFile(filePath, totalFiles, deletionAlgorithm))
                .ConfigureAwait(false);
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

                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
                using (var cryptoStream = new CryptoStream(fileStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    var buffer = new byte[4096];
                    int bytesRead;
                    while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        cryptoStream.Write(buffer, 0, bytesRead);
                    }
                }
            }

            if (_debug) Console.WriteLine($"Finished encrypting {filePath}");
        }

        /// <summary>
        /// Deletes the specified file using the specified deletion algorithm.
        /// </summary>
        /// <param name="filePath">The path of the file to delete.</param>
        /// <param name="totalFiles">The total number of files being deleted.</param>
        /// <param name="deletionProtocol">The deletion algorithm to use.</param>
        private static void DeleteFile(string filePath, long totalFiles, string deletionProtocol = "")
        {
            if (_debug) Console.WriteLine($"Started secure delete ({deletionProtocol}) of {filePath}");

            switch (deletionProtocol)
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
                case "none":
                    break;
                default:
                    ShredFile(filePath);
                    break;
            }

            File.Delete(filePath);
            
            lock (ConsoleLock)
            {
                _deletionCount++;
                DisplayProgress(_deletionCount, totalFiles);
            }

            if (_debug) Console.WriteLine($"Finished secure delete of {filePath}");
        }

        /// <summary>
        /// Called when all files have been encrypted and deleted.
        /// </summary>
        private static void OnCompleted()
        {
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

                Marshal.ReleaseComObject(item);
            }

            Marshal.ReleaseComObject(items);
            Marshal.ReleaseComObject(recyclerFolder);
            Marshal.ReleaseComObject(shell);

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

            Console.WriteLine(
                _deletionCount > 0
                    ? $"\nAll {_deletionCount:N0} files have been {(_deletionAlgorithm != "none" || _encrypt ? "encrypted and" : string.Empty)} deleted from your recycling bin."
                    : "\nNo files were found to be deleted.");
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
            var randomBytes = new byte[4096];
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Write);
            for (long i = 0; i < fileLength; i += randomBytes.Length)
            {
                RandomNumberGenerator.Fill(randomBytes);
                fileStream.Write(randomBytes, 0, (int)Math.Min(randomBytes.Length, fileLength - i));
            }
        }

        /// <summary>
        /// Overwrites the specified file with zeros.
        /// </summary>
        /// <param name="filePath">The path of the file to overwrite.</param>
        private static void WriteZeros(string filePath)
        {
            var fileLength = new FileInfo(filePath).Length;
            var zeroBytes = new byte[4096];
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Write);
            for (long i = 0; i < fileLength; i += zeroBytes.Length)
            {
                fileStream.Write(zeroBytes, 0, (int)Math.Min(zeroBytes.Length, fileLength - i));
            }
        }

        /// <summary>
        /// Overwrites the specified file with ones.
        /// </summary>
        /// <param name="filePath">The path of the file to overwrite.</param>
        private static void WriteOnes(string filePath)
        {
            var fileLength = new FileInfo(filePath).Length;
            var oneBytes = new byte[4096];
            for (var i = 0; i < oneBytes.Length; i++)
            {
                oneBytes[i] = 0xFF;
            }

            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Write);
            for (long i = 0; i < fileLength; i += oneBytes.Length)
            {
                fileStream.Write(oneBytes, 0, (int)Math.Min(oneBytes.Length, fileLength - i));
            }
        }

        /// <summary>
        /// Displays a progress bar in the console.
        /// </summary>
        /// <param name="current">The current progress count.</param>
        /// <param name="total">The total count for completion.</param>
        private static void DisplayProgress(long current, long total)
        {
            // TODO: Colorize the progress bar.
            var progress = (double)current / total;
            var progressBarWidth = 50;
            var progressWidth = (int)(progress * progressBarWidth);

            lock (ConsoleLock)
            {
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write("[");
                Console.Write(new string('#', progressWidth));
                Console.Write(new string(' ', progressBarWidth - progressWidth));
                Console.Write($"] {current}/{total} ({progress:P0})");
            }
        }
        #endregion
    }
}
