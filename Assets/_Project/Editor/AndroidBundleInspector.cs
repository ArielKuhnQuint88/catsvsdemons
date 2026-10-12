using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace CatsVsDemons.Editor
{
    /// <summary>Inspects the actual bundle, including native plugins, without external SDK tools.</summary>
    public static class AndroidBundleInspector
    {
        public static List<string> Inspect(string path)
        {
            List<string> errors = new();
            using ZipArchive archive = ZipFile.OpenRead(path);
            if (archive.GetEntry("BundleConfig.pb") == null ||
                archive.GetEntry("base/manifest/AndroidManifest.xml") == null)
                errors.Add("O arquivo não contém a estrutura de um Android App Bundle.");
            if (!archive.Entries.Any(entry => entry.FullName.StartsWith("META-INF/", StringComparison.Ordinal) &&
                (entry.FullName.EndsWith(".RSA", StringComparison.OrdinalIgnoreCase) ||
                 entry.FullName.EndsWith(".DSA", StringComparison.OrdinalIgnoreCase) ||
                 entry.FullName.EndsWith(".EC", StringComparison.OrdinalIgnoreCase))))
                errors.Add("O AAB não contém a assinatura de upload.");

            int libraries = 0;
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                if (!entry.FullName.Contains("/lib/") || !entry.FullName.EndsWith(".so", StringComparison.Ordinal)) continue;
                if (!entry.FullName.Contains("/lib/arm64-v8a/"))
                {
                    errors.Add("Biblioteca fora do perfil ARM64: " + entry.FullName);
                    continue;
                }
                libraries++;
                using Stream stream = entry.Open();
                string error = InspectElf(stream);
                if (error != null) errors.Add(entry.FullName + ": " + error);
            }
            if (libraries == 0) errors.Add("Nenhuma biblioteca ARM64 foi encontrada no AAB.");
            return errors;
        }

        public static string InspectElf(Stream stream)
        {
            using BinaryReader reader = new(stream, System.Text.Encoding.UTF8, true);
            byte[] header = reader.ReadBytes(64);
            if (header.Length != 64 || header[0] != 0x7f || header[1] != 'E' ||
                header[2] != 'L' || header[3] != 'F' || header[4] != 2 || header[5] != 1)
                return "Formato ELF64 little-endian inválido.";
            if (BitConverter.ToUInt16(header, 18) != 183) return "A biblioteca não é AArch64.";
            ulong offset = BitConverter.ToUInt64(header, 32);
            int recordSize = BitConverter.ToUInt16(header, 54);
            int count = BitConverter.ToUInt16(header, 56);
            if (offset < 64 || offset > 1048576 || recordSize < 56 || count < 1 || count > 4096)
                return "Tabela de segmentos ELF inválida.";
            long remaining = (long)offset - 64;
            while (remaining > 0)
            {
                int bytes = reader.ReadBytes((int)Math.Min(remaining, 4096)).Length;
                if (bytes == 0) return "Cabeçalho ELF truncado.";
                remaining -= bytes;
            }
            int loadSegments = 0;
            for (int index = 0; index < count; index++)
            {
                byte[] record = reader.ReadBytes(recordSize);
                if (record.Length != recordSize) return "Segmento ELF truncado.";
                uint type = BitConverter.ToUInt32(record, 0);
                if (type != 1) continue;
                loadSegments++;
                ulong alignment = BitConverter.ToUInt64(record, 48);
                ulong fileOffset = BitConverter.ToUInt64(record, 8);
                ulong address = BitConverter.ToUInt64(record, 16);
                if (alignment < 16384 || (alignment & (alignment - 1)) != 0 ||
                    fileOffset % 16384 != address % 16384)
                    return "Segmento LOAD sem alinhamento de 16 KB. Atualize a Unity ou o plugin nativo.";
            }
            return loadSegments == 0 ? "Nenhum segmento LOAD no ELF." : null;
        }
    }
}
