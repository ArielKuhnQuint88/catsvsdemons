using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using CatsVsDemons.Editor;

internal static class BundleInspectorChecks
{
    private static int passed;

    private static int Main(string[] args)
    {
        try
        {
            if (args.Length == 1)
            {
                var errors = AndroidBundleInspector.Inspect(args[0]);
                foreach (string error in errors) Console.Error.WriteLine(error);
                return errors.Count == 0 ? 0 : 1;
            }
            Check("ELF ARM64 aligned to 16 KB", () => Inspect(Elf(16384)) == null);
            Check("ELF ARM64 aligned to 64 KB", () => Inspect(Elf(65536)) == null);
            Check("Reject old 4 KB native plugin", () => Inspect(Elf(4096)) != null);
            Check("Reject non-power-of-two alignment", () => Inspect(Elf(24576)) != null);
            Check("Reject incongruent LOAD address", () => Inspect(Elf(16384, 1)) != null);
            Check("Reject wrong CPU architecture", () => Inspect(Elf(16384, 0, 62)) != null);
            Check("Reject truncated ELF header", () => Inspect(new byte[32]) != null);
            byte[] truncated = Elf(16384);
            Array.Resize(ref truncated, 90);
            Check("Reject truncated program table", () => Inspect(truncated) != null);
            Check("Accept signed ARM64 bundle structure", () => Bundle(Elf(16384), true, true, "arm64-v8a") == 0);
            Check("Reject unsigned bundle", () => Bundle(Elf(16384), false, true, "arm64-v8a") > 0);
            Check("Reject missing manifest", () => Bundle(Elf(16384), true, false, "arm64-v8a") > 0);
            Check("Reject 32-bit-only bundle", () => Bundle(Elf(16384), true, true, "armeabi-v7a") > 0);
            Check("Reject 4 KB library inside otherwise signed bundle", () => Bundle(Elf(4096), true, true, "arm64-v8a") > 0);
            Console.WriteLine($"{passed} checks passed against the production C# inspector.");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
    }

    private static void Check(string name, Func<bool> test)
    {
        if (!test()) throw new InvalidOperationException(name);
        passed++;
        Console.WriteLine("PASS " + name);
    }

    private static string Inspect(byte[] data)
    {
        using MemoryStream stream = new(data);
        return AndroidBundleInspector.InspectElf(stream);
    }

    private static byte[] Elf(ulong alignment, ulong address = 0, ushort machine = 183)
    {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream, Encoding.UTF8, true);
        byte[] header = new byte[64];
        header[0] = 0x7f; header[1] = (byte)'E'; header[2] = (byte)'L'; header[3] = (byte)'F';
        header[4] = 2; header[5] = 1;
        Array.Copy(BitConverter.GetBytes(machine), 0, header, 18, 2);
        Array.Copy(BitConverter.GetBytes((ulong)64), 0, header, 32, 8);
        Array.Copy(BitConverter.GetBytes((ushort)56), 0, header, 54, 2);
        Array.Copy(BitConverter.GetBytes((ushort)1), 0, header, 56, 2);
        writer.Write(header);
        writer.Write((uint)1); writer.Write((uint)5);
        writer.Write((ulong)0); writer.Write(address); writer.Write((ulong)0);
        writer.Write((ulong)4096); writer.Write((ulong)4096); writer.Write(alignment);
        return stream.ToArray();
    }

    private static int Bundle(byte[] elf, bool signed, bool manifest, string abi)
    {
        string path = Path.Combine(Path.GetTempPath(), "cvd-inspector-" + Guid.NewGuid() + ".aab");
        try
        {
            using (ZipArchive archive = ZipFile.Open(path, ZipArchiveMode.Create))
            {
                archive.CreateEntry("BundleConfig.pb");
                if (manifest) archive.CreateEntry("base/manifest/AndroidManifest.xml");
                if (signed) archive.CreateEntry("META-INF/UPLOAD.RSA");
                ZipArchiveEntry entry = archive.CreateEntry("base/lib/" + abi + "/libunity.so");
                using Stream output = entry.Open();
                output.Write(elf, 0, elf.Length);
            }
            return AndroidBundleInspector.Inspect(path).Count;
        }
        finally { File.Delete(path); }
    }
}
