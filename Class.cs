using System;
using System.IO;
using System.Runtime.InteropServices;

namespace FileMoveToBin
{
    /// <summary>
    /// Provides functionality to move files to the system Recycle Bin.
    /// Uses the Windows Shell API via P/Invoke to ensure proper shell integration.
    /// </summary>
    public static class RecycleBinHelper
    {
        #region Windows API Constants

        /// <summary>
        /// Shell file operation type: Delete (move to Recycle Bin when combined with FOF_ALLOWUNDO).
        /// </summary>
        private const int FO_DELETE = 0x0003;

        /// <summary>
        /// Shell file operation flag: Preserve undo information (enables Recycle Bin behavior).
        /// Without this flag, files are permanently deleted.
        /// </summary>
        private const int FOF_ALLOWUNDO = 0x0040;

        /// <summary>
        /// Shell file operation flag: Do not display a confirmation dialog to the user.
        /// </summary>
        private const int FOF_NOCONFIRMATION = 0x0010;

        /// <summary>
        /// Shell file operation flag: Do not display a progress dialog during the operation.
        /// </summary>
        private const int FOF_SILENT = 0x0004;

        #endregion

        #region Native Structures

        /// <summary>
        /// Contains information that the <see cref="SHFileOperation"/> function uses to perform 
        /// file operations such as copy, move, rename, or delete.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SHFILEOPSTRUCT
        {
            /// <summary>
            /// A window handle to the dialog box or window that displays the operation status.
            /// Set to <see cref="IntPtr.Zero"/> for desktop (no parent window).
            /// </summary>
            public IntPtr hwnd;

            /// <summary>
            /// The file operation to perform. Must be one of the FO_* constants.
            /// </summary>
            public int wFunc;

            /// <summary>
            /// A pointer to one or more source file paths. 
            /// Multiple paths must be separated by the null terminator character '\0'.
            /// The entire string must be terminated with two null characters ("\0\0").
            /// </summary>
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pFrom;

            /// <summary>
            /// A pointer to the destination path. 
            /// Required only for copy and move operations; set to null for delete.
            /// </summary>
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pTo;

            /// <summary>
            /// Flags that control the file operation. Combination of FOF_* constants.
            /// </summary>
            public short fFlags;

            /// <summary>
            /// Set to true by the system if the user aborted any file operations before completion.
            /// </summary>
            public bool fAnyOperationsAborted;

            /// <summary>
            /// A handle to a name mapping object containing the old and new names of the renamed files.
            /// Typically unused for Recycle Bin operations.
            /// </summary>
            public IntPtr hNameMappings;

            /// <summary>
            /// The title of a progress dialog box. Used only when FOF_SIMPLEPROGRESS is set.
            /// </summary>
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpszProgressTitle;
        }

        #endregion

        #region Native Methods

        /// <summary>
        /// Performs a shell file operation such as copy, move, rename, or delete.
        /// This is the core Windows API function that interacts with the Recycle Bin.
        /// </summary>
        /// <param name="lpFileOp">
        /// A reference to an <see cref="SHFILEOPSTRUCT"/> structure that contains information 
        /// about the file operation to perform.
        /// </param>
        /// <returns>
        /// Returns 0 (zero) if the operation is successful. 
        /// Returns a nonzero error code if the operation fails.
        /// </returns>
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHFileOperation(ref SHFILEOPSTRUCT lpFileOp);

        #endregion

        #region Public Methods

        /// <summary>
        /// Moves the specified file to the system Recycle Bin.
        /// </summary>
        /// <param name="filePath">The full path of the file to move to the Recycle Bin.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="filePath"/> is null.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the specified file does not exist.</exception>
        /// <exception cref="IOException">Thrown when the Shell API operation fails to move the file to the Recycle Bin.</exception>
        /// <remarks>
        /// This operation uses the Windows Shell API <see cref="SHFileOperation"/> with the 
        /// <see cref="FOF_ALLOWUNDO"/> flag to enable Recycle Bin behavior. 
        /// Confirmation and progress dialogs are suppressed for a seamless operation.
        /// </remarks>
        public static void MoveToRecycleBin(string filePath)
        {
            // Validate the input parameter to prevent null reference exceptions
            ArgumentNullException.ThrowIfNull(filePath);

            // Verify that the file exists before attempting to move it
            if (!File.Exists(filePath))
                throw new FileNotFoundException("The specified file does not exist.", filePath);

            // Initialize the SHFILEOPSTRUCT with the required parameters for Recycle Bin operation
            var fileOperation = new SHFILEOPSTRUCT
            {
                // No parent window handle required for silent operation
                hwnd = IntPtr.Zero,

                // Specify the delete operation (will be redirected to Recycle Bin by FOF_ALLOWUNDO)
                wFunc = FO_DELETE,

                // The source file path. 
                // CRITICAL: The Windows Shell API requires the path string to be terminated 
                // with TWO null characters. The first '\0' terminates the path, and the second 
                // '\0' terminates the entire path list (required even for single files).
                pFrom = filePath + "\0\0",

                // Not required for delete operations
                pTo = string.Empty,

                // Combine flags: Allow undo (Recycle Bin), suppress confirmation dialog, 
                // and suppress progress dialog for a seamless operation
                fFlags = (short)(FOF_ALLOWUNDO | FOF_NOCONFIRMATION | FOF_SILENT)
            };

            // Execute the shell file operation
            int result = SHFileOperation(ref fileOperation);

            // Check the return value: 0 indicates success, any non-zero value indicates failure
            if (result != 0)
            {
                throw new IOException(
                    $"The Shell API operation failed with error code {result}. " +
                    $"The file '{filePath}' may not have been moved to the Recycle Bin."
                );
            }

            // Note: fAnyOperationsAborted is typically not checked here because the FOF_SILENT
            // and FOF_NOCONFIRMATION flags prevent user interaction that could cause abortion.
            // However, in a UI-enabled scenario, you may want to check fileOperation.fAnyOperationsAborted.
        }

        /// <summary>
        /// Moves the file represented by the specified <see cref="FileInfo"/> instance to the system Recycle Bin.
        /// </summary>
        /// <param name="file">The <see cref="FileInfo"/> instance containing information about the file to move to the Recycle Bin.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="file"/> is null or its full path is null or empty.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the specified file does not exist.</exception>
        /// <exception cref="IOException">Thrown when the Shell API operation fails to move the file to the Recycle Bin.</exception>
        /// <remarks>
        /// This method delegates to the <see cref="MoveToRecycleBin(string)"/> overload after validating 
        /// the <see cref="FileInfo.FullName"/> property.
        /// </remarks>
        public static void MoveToRecycleBin(FileInfo file)
        {
            // Validate the path string before creating the FileInfo instance
            if (string.IsNullOrWhiteSpace(file.FullName))
                throw new ArgumentException("The file path cannot be null or empty.", nameof(file));

            // Delegate to the FileInfo-based overload
            MoveToRecycleBin(file.FullName);
        }

        #endregion
    }
}