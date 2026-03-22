using System;

namespace Scop
{
    internal static class Program
    {
        /* ── Entry point ─────────────────────────────────────────────────── */

        private static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                PrintUsage();
                return 1;
            }

            string texPath = args.Length >= 2 ? args[1] : string.Empty;

            try
            {
                using App app = new App(args[0], texPath);
                app.Run();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Fatal: {ex.Message}");
                return 1;
            }

            return 0;
        }

        /* ── Usage ───────────────────────────────────────────────────────── */

        private static void PrintUsage()
        {
            Console.Error.WriteLine("Usage: scop <model.obj> [texture.bmp|texture.ppm]");
            Console.Error.WriteLine();
            Console.Error.WriteLine("Controls:");
            Console.Error.WriteLine("  Arrow keys  — translate on X / Y");
            Console.Error.WriteLine("  Q / E       — translate on Z (closer / farther)");
            Console.Error.WriteLine("  T           — toggle texture (smooth transition)");
            Console.Error.WriteLine("  Scroll      — zoom in / out");
            Console.Error.WriteLine("  LMB drag    — rotate manually");
            Console.Error.WriteLine("  ESC         — quit");
        }
    }
}