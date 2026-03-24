using System;
using System.Collections.Generic;

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

            /* ── Strip flags from positional arguments ───────────────────── */

            bool         bonusMode  = false;
            List<string> positional = new List<string>();

            foreach (string arg in args)
            {
                if (arg == "--bonus")
                {
                    bonusMode = true;
                }
                else
                {
                    positional.Add(arg);
                }
            }

            /* ── Single mode: scop model.obj [texture] ───────────────────── */

            if (positional.Count <= 2)
            {
                string objPath = positional[0];
                string texPath = positional.Count == 2 ? positional[1] : string.Empty;

                return Run(objPath, texPath, string.Empty, string.Empty, bonusMode);
            }

            /* ── Dual mode: scop model1.obj tex1 model2.obj tex2 ─────────── */

            if (positional.Count == 4)
            {
                return Run(positional[0], positional[1], positional[2], positional[3], bonusMode);
            }

            PrintUsage();
            return 1;
        }

        /* ── Run helper ──────────────────────────────────────────────────── */

        private static int Run(
            string objPathA,
            string texPathA,
            string objPathB,
            string texPathB,
            bool   bonusMode)
        {
            try
            {
                using App app = new App(objPathA, texPathA, objPathB, texPathB, bonusMode);
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
                "Usage: scop <model.obj> [texture] [--bonus]"
            );
            Console.Error.WriteLine(
                "       scop <model1.obj> <tex1> <model2.obj> <tex2> [--bonus]"
            );
            Console.Error.WriteLine();
            Console.Error.WriteLine("Flags:");
            Console.Error.WriteLine("  --bonus     ear-clipping triangulation + face-normal UV mapping");
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