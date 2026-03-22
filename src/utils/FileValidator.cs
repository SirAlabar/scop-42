using System;
using System.IO;

namespace Scop.Utils
{
    public static class FileValidator
    {
        /* ── Public entry point ──────────────────────────────────────────── */

        // Returns true if the file exists and is readable.
        // Prints a specific error message to stderr for each failure case.
        public static bool Validate(string filepath, string caller)
        {
            if (!File.Exists(filepath))
            {
                Console.Error.WriteLine($"{caller}: file not found '{filepath}'");
                return false;
            }

            try
            {
                using FileStream fs = File.Open(filepath, FileMode.Open, FileAccess.Read);
            }
            catch (UnauthorizedAccessException)
            {
                Console.Error.WriteLine($"{caller}: no read permission for '{filepath}'");
                return false;
            }
            catch (IOException ex)
            {
                Console.Error.WriteLine($"{caller}: cannot open '{filepath}': {ex.Message}");
                return false;
            }

            return true;
        }
    }
}