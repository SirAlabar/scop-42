using System;
using System.Globalization;
using System.IO;

namespace Scop.Utils
{
    public static class FileParser
    {
        /* ── Float parsing ───────────────────────────────────────────────── */

        // Always parses with dot as decimal separator regardless of system locale
        public static float ParseFloat(string s)
        {
            return float.Parse(s, CultureInfo.InvariantCulture);
        }

        /* ── Line tokenizer ──────────────────────────────────────────────── */

        // Strips inline # comments, trims whitespace, splits on spaces and tabs
        public static string[] TokenizeLine(string rawLine)
        {
            int    commentAt = rawLine.IndexOf('#');
            string line      = commentAt >= 0 ? rawLine[..commentAt] : rawLine;

            return line.Trim().Split(
                new char[] { ' ', '\t' },
                StringSplitOptions.RemoveEmptyEntries
            );
        }

        /* ── PPM token reader ────────────────────────────────────────────── */

        // Reads one whitespace-delimited token from a binary stream.
        // Skips PPM comment lines (lines starting with #).
        public static string ReadPpmToken(BinaryReader reader)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                byte ch = reader.ReadByte();

                if (ch == '#')
                {
                    while (reader.BaseStream.Position < reader.BaseStream.Length)
                    {
                        byte c2 = reader.ReadByte();

                        if (c2 == '\n' || c2 == '\r')
                        {
                            break;
                        }
                    }
                }
                else if (ch == ' ' || ch == '\t' || ch == '\n' || ch == '\r')
                {
                    if (sb.Length > 0)
                    {
                        return sb.ToString();
                    }
                }
                else
                {
                    sb.Append((char)ch);
                }
            }

            return sb.ToString();
        }
    }
}