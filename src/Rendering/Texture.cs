using System;
using System.IO;
using Scop.Rendering.Interfaces;
using Scop.Utils;
using OpenTK.Graphics.OpenGL4;

namespace Scop.Rendering
{
    public class Texture : ITexture, IDisposable
    {
        /* ── Fields ──────────────────────────────────────────────────────── */

        private int     _id;
        private bool    _disposed;

        /* ── Properties ──────────────────────────────────────────────────── */

        public int  Width    { get; private set; }
        public int  Height   { get; private set; }
        public bool IsLoaded => _id != 0;


        /* ── Factory ─────────────────────────────────────────────────────── */

        // Returns a real Texture on success or a NullTexture on failure.
        // Detects format by file extension (.bmp / .ppm).
        // App.cs never needs to check the result — NullTexture handles failure silently.
        public static ITexture FromFile(string filepath)
        {
            Texture texture = new Texture();
            string  ext     = System.IO.Path.GetExtension(filepath).ToLowerInvariant();
            bool    ok      = false;

            if (ext == ".bmp")
            {
                ok = texture.LoadBMP(filepath);
            }
            else if (ext == ".ppm")
            {
                ok = texture.LoadPPM(filepath);
            }
            else
            {
                Console.Error.WriteLine($"Texture: unsupported extension '{ext}'");
            }

            if (!ok)
            {
                return new NullTexture();
            }

            return texture;
        }

        /* ── Bind / Unbind ───────────────────────────────────────────────── */

        public void Bind(int unit = 0)
        {
            GL.ActiveTexture(TextureUnit.Texture0 + unit);
            GL.BindTexture(TextureTarget.Texture2D, _id);
        }

        public void Unbind()
        {
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        /* ── BMP loader ──────────────────────────────────────────────────── */

        // Supports: 24-bit and 32-bit, uncompressed (BI_RGB)
        // Handles: bottom-up (positive height) and top-down (negative height)

        public bool LoadBMP(string filepath)
        {
            if (!FileValidator.Validate(filepath, "Texture"))
            {
                return false;
            }

            try
            {
                using BinaryReader reader = new BinaryReader(File.OpenRead(filepath));

                if (!ReadBmpFileHeader(reader, out uint dataOffset))
                {
                    return false;
                }

                if (!ReadBmpDibHeader(reader, out int width, out int height,
                    out ushort bpp, out uint compression))
                {
                    return false;
                }

                if (compression != 0)
                {
                    Console.Error.WriteLine("Texture: compressed BMP not supported");
                    return false;
                }

                if (bpp != 24 && bpp != 32)
                {
                    Console.Error.WriteLine($"Texture: only 24/32-bit BMP supported (got {bpp})");
                    return false;
                }

                byte[] pixels = DecodeBmpPixels(reader, dataOffset, width, height, bpp);
                UploadToGPU(pixels, Math.Abs(width), Math.Abs(height));

                Console.WriteLine($"Texture: BMP {Math.Abs(width)}x{Math.Abs(height)}" +
                    $" loaded from '{filepath}'");
                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Texture: failed to load BMP '{filepath}': {ex.Message}");
                return false;
            }
        }

        /* ── PPM loader ──────────────────────────────────────────────────── */

        // Supports: P3 (ASCII) and P6 (binary)
        // Handles: 8-bit and 16-bit maxval, comment lines

        public bool LoadPPM(string filepath)
        {
            if (!FileValidator.Validate(filepath, "Texture"))
            {
                return false;
            }

            try
            {
                using BinaryReader reader = new BinaryReader(File.OpenRead(filepath));

                string magic = ReadPpmToken(reader);

                if (magic != "P3" && magic != "P6")
                {
                    Console.Error.WriteLine($"Texture: unsupported PPM type '{magic}'");
                    return false;
                }

                int width  = int.Parse(ReadPpmToken(reader));
                int height = int.Parse(ReadPpmToken(reader));
                int maxval = int.Parse(ReadPpmToken(reader));

                // Consume single mandatory whitespace after maxval
                reader.ReadByte();

                byte[] pixels = DecodePpmPixels(reader, magic, width, height, maxval);
                UploadToGPU(pixels, width, height);

                Console.WriteLine($"Texture: PPM {width}x{height} loaded from '{filepath}'");
                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Texture: failed to load PPM '{filepath}': {ex.Message}");
                return false;
            }
        }

        /* ── BMP helpers ─────────────────────────────────────────────────── */

        private static bool ReadBmpFileHeader(BinaryReader reader, out uint dataOffset)
        {
            dataOffset = 0;

            byte b0 = reader.ReadByte();
            byte b1 = reader.ReadByte();

            if (b0 != 'B' || b1 != 'M')
            {
                Console.Error.WriteLine("Texture: not a valid BMP file");
                return false;
            }

            reader.ReadUInt32();            // file size (unused)
            reader.ReadUInt32();            // reserved  (unused)
            dataOffset = reader.ReadUInt32();
            return true;
        }

        private static bool ReadBmpDibHeader(
            BinaryReader    reader,
            out int         width,
            out int         height,
            out ushort      bpp,
            out uint        compression)
        {
            reader.ReadUInt32();            // header size (unused)
            width       = reader.ReadInt32();
            height      = reader.ReadInt32();
            reader.ReadUInt16();            // color planes (unused)
            bpp         = reader.ReadUInt16();
            compression = reader.ReadUInt32();
            return true;
        }

        private static byte[] DecodeBmpPixels(
            BinaryReader    reader,
            uint            dataOffset,
            int             width,
            int             height,
            ushort          bpp)
        {
            bool    bottomUp    = height > 0;
            int     absHeight   = Math.Abs(height);
            int     bppBytes    = bpp / 8;
            int     rowStride   = ((width * bppBytes + 3) / 4) * 4;

            reader.BaseStream.Seek(dataOffset, SeekOrigin.Begin);
            byte[] raw    = reader.ReadBytes(rowStride * absHeight);
            byte[] pixels = new byte[width * absHeight * 3];

            for (int y = 0; y < absHeight; y++)
            {
                int srcRow = bottomUp ? (absHeight - 1 - y) : y;

                for (int x = 0; x < width; x++)
                {
                    int srcIdx = srcRow * rowStride + x * bppBytes;
                    int dstIdx = (y * width + x) * 3;

                    // BMP stores BGR — convert to RGB
                    pixels[dstIdx + 0] = raw[srcIdx + 2];
                    pixels[dstIdx + 1] = raw[srcIdx + 1];
                    pixels[dstIdx + 2] = raw[srcIdx + 0];
                }
            }

            return pixels;
        }

        /* ── PPM helpers ─────────────────────────────────────────────────── */

        private static byte[] DecodePpmPixels(
            BinaryReader    reader,
            string          magic,
            int             width,
            int             height,
            int             maxval)
        {
            byte[] pixels = new byte[width * height * 3];

            if (magic == "P6")
            {
                DecodePpmBinary(reader, pixels, maxval);
            }
            else
            {
                DecodePpmAscii(reader, pixels, maxval);
            }

            return pixels;
        }

        private static void DecodePpmBinary(BinaryReader reader, byte[] pixels, int maxval)
        {
            if (maxval < 256)
            {
                byte[] raw = reader.ReadBytes(pixels.Length);

                if (maxval == 255)
                {
                    Buffer.BlockCopy(raw, 0, pixels, 0, raw.Length);
                }
                else
                {
                    for (int i = 0; i < raw.Length; i++)
                    {
                        pixels[i] = (byte)(raw[i] * 255 / maxval);
                    }
                }
            }
            else
            {
                // 16-bit big-endian per channel
                for (int i = 0; i < pixels.Length; i++)
                {
                    int hi  = reader.ReadByte();
                    int lo  = reader.ReadByte();
                    int val = (hi << 8) | lo;
                    pixels[i] = (byte)(val * 255 / maxval);
                }
            }
        }

        private static void DecodePpmAscii(BinaryReader reader, byte[] pixels, int maxval)
        {
            int idx = 0;

            while (idx < pixels.Length)
            {
                string tok = ReadPpmToken(reader);

                if (tok.Length == 0)
                {
                    break;
                }

                if (int.TryParse(tok, out int val))
                {
                    pixels[idx] = (byte)(val * 255 / maxval);
                    idx++;
                }
            }
        }

        // Read one whitespace-delimited token, skipping PPM comment lines
        private static string ReadPpmToken(BinaryReader reader)
        {
            return FileParser.ReadPpmToken(reader);
        }

        /* ── GPU upload ──────────────────────────────────────────────────── */

        private void UploadToGPU(byte[] data, int width, int height)
        {
            Width  = width;
            Height = height;

            _id = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, _id);

            GL.TexParameter(TextureTarget.Texture2D,
                TextureParameterName.TextureWrapS,     (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D,
                TextureParameterName.TextureWrapT,     (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D,
                TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(TextureTarget.Texture2D,
                TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            GL.TexImage2D(
                TextureTarget.Texture2D, 0,
                PixelInternalFormat.Rgb,
                width, height, 0,
                PixelFormat.Rgb, PixelType.UnsignedByte,
                data
            );

            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        /* ── Disposal ────────────────────────────────────────────────────── */

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_id != 0)
                {
                    GL.DeleteTexture(_id);
                }

                _disposed = true;
            }
        }
    }
}