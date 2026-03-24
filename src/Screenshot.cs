using System;
using System.IO;
using OpenTK.Graphics.OpenGL4;

namespace Scop
{
    // Captures the current back buffer and writes a PPM P6 file.
    // Called from App.OnRenderFrame after Renderer.Draw() and before SwapBuffers().
    //
    // PPM P6 format:
    //   P6\n
    //   width height\n
    //   255\n
    //   [binary RGB rows, top-to-bottom]
    //
    // GL.ReadPixels returns rows bottom-to-top — we flip them before writing.

    public static class Screenshot
    {
        /* ── Capture ─────────────────────────────────────────────────────── */

        public static void Capture(int width, int height)
        {
            string filename = BuildFilename();

            byte[] pixels = ReadPixels(width, height);
            byte[] flipped = FlipRows(pixels, width, height);

            WritePpm(filename, width, height, flipped);

            Console.WriteLine($"Screenshot: saved '{filename}'");
        }

        /* ── Private helpers ─────────────────────────────────────────────── */

        private static string BuildFilename()
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            return $"screenshot_{timestamp}.ppm";
        }

        private static byte[] ReadPixels(int width, int height)
        {
            byte[] pixels = new byte[width * height * 3];

            GL.ReadPixels(
                0, 0,
                width, height,
                PixelFormat.Rgb,
                PixelType.UnsignedByte,
                pixels
            );

            return pixels;
        }

        // GL returns rows in bottom-to-top order — flip to top-to-bottom for PPM.
        private static byte[] FlipRows(byte[] pixels, int width, int height)
        {
            byte[] flipped    = new byte[pixels.Length];
            int    rowBytes   = width * 3;

            for (int row = 0; row < height; row++)
            {
                int srcRow = height - 1 - row;

                Array.Copy(
                    pixels,  srcRow * rowBytes,
                    flipped, row    * rowBytes,
                    rowBytes
                );
            }

            return flipped;
        }

        private static void WritePpm(string filename, int width, int height, byte[] pixels)
        {
            using FileStream   stream = File.OpenWrite(filename);
            using BinaryWriter writer = new BinaryWriter(stream);

            /* ── ASCII header ────────────────────────────────────────────── */

            byte[] header = System.Text.Encoding.ASCII.GetBytes(
                $"P6\n{width} {height}\n255\n"
            );

            writer.Write(header);

            /* ── Binary RGB payload ──────────────────────────────────────── */

            writer.Write(pixels);
        }
    }
}