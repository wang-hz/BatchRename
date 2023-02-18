using System.CommandLine;
using System.Runtime.InteropServices;

namespace BatchRename {
    internal class Constants {
        public const string TMP_EXTENSION = ".tmp";
    }

    internal class DllUtil {
        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
        public static extern int StrCmpLogicalW(string x, string y);
    }

    internal sealed class NaturalStringComparer : IComparer<string> {
        public int Compare(string? x, string? y) {
            if (x == null || y == null) {
                return 0;
            }
            return DllUtil.StrCmpLogicalW(x, y);
        }
    }

    internal class Program {
        internal static void SortAndRenameFilesByIndex(int start, int length) {
            string currentDirectory = Directory.GetCurrentDirectory();
            string[] fullFilePaths = Directory.GetFiles(currentDirectory);
            Array.Sort(fullFilePaths, new NaturalStringComparer());
            for (int i = 0; i < fullFilePaths.Length; ++i) {
                string oldFullFilePath = fullFilePaths[i];
                string tmpFullFilePath = Path.Join(
                    currentDirectory,
                    $"{(start + i).ToString().PadLeft(length, '0')}{Path.GetExtension(oldFullFilePath)}{Constants.TMP_EXTENSION}"
                ).ToLower();
                File.Move(oldFullFilePath, tmpFullFilePath);
                fullFilePaths[i] = tmpFullFilePath;
            }
            foreach (string tmpFullFilePath in fullFilePaths) {
                File.Move(tmpFullFilePath, tmpFullFilePath[..^Constants.TMP_EXTENSION.Length]);
            }
        }

        static Task<int> Main(string[] args) {
            RootCommand rootCommand = new("Batch rename files in directory");
            Option<int> startOption = new(
                name: "--start",
                description: "Start index of files.",
                getDefaultValue: () => 1);
            Option<int> lengthOption = new(
                name: "--length",
                description: "Length of index.",
                getDefaultValue: () => 3);
            Command sortCommand = new("sort", "Sort and rename files.") {
                startOption,
                lengthOption
            };
            sortCommand.SetHandler((start, length) => {
                SortAndRenameFilesByIndex(start, length);
            }, startOption, lengthOption);
            rootCommand.AddCommand(sortCommand);
            return rootCommand.InvokeAsync(args);
        }
    }
}
