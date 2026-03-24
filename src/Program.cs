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

            /* ── Single mode: scop model.obj [texture] ───────────────────── */

            if (args.Length <= 2)
            {
                string objPath = args[0];
                string texPath = args.Length == 2 ? args[1] : string.Empty;

                return RunSingle(objPath, texPath);
            }

            /* ── Dual mode: scop model1.obj tex1 model2.obj tex2 ─────────── */

            if (args.Length == 4)
            {
                return RunDual(args[0], args[1], args[2], args[3]);
            }

            PrintUsage();
            return 1;
        }

        /* ── Run helpers ─────────────────────────────────────────────────── */

        private static int RunSingle(string objPath, string texPath)
        {
            try
            {
                using App app = new App(objPath, texPath, string.Empty, string.Empty);
                app.Run();
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Fatal: {ex.Message}");
                return 1;
            }
        }

        private static int RunDual(
            string objPathA,
            string texPathA,
            string objPathB,
            string texPathB)
        {
            try
            {
                using App app = new App(objPathA, texPathA, objPathB, texPathB);
                app.Run();
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Fatal: {ex.Message}");
                return 1;
            }
        }

        /* ── Usage ───────────────────────────────────────────────────────── */

        private static void PrintUsage()
        {
            Console.Error.WriteLine(
                "Usage: scop <model.obj> [texture]                         (single mode)"
            );
            Console.Error.WriteLine(
                "       scop <model1.obj> <tex1> <model2.obj> <tex2>       (dual mode)"
            );
            Console.Error.WriteLine();
            Console.Error.WriteLine("Controls:");
            Console.Error.WriteLine("  Arrow keys  -- translate on X / Y");
            Console.Error.WriteLine("  Q / E       -- translate on Z");
            Console.Error.WriteLine("  T           -- toggle texture");
            Console.Error.WriteLine("  W           -- wireframe toggle");
            Console.Error.WriteLine("  C           -- backface culling toggle");
            Console.Error.WriteLine("  S           -- flat / smooth shading");
            Console.Error.WriteLine("  L           -- lighting toggle");
            Console.Error.WriteLine("  P           -- screenshot (PPM)");
            Console.Error.WriteLine("  Tab         -- switch active model (dual mode)");
            Console.Error.WriteLine("  Scroll      -- zoom in / out");
            Console.Error.WriteLine("  LMB drag    -- rotate model");
            Console.Error.WriteLine("  RMB hold    -- move light source");
            Console.Error.WriteLine("  ESC         -- quit");
        }
    }
}