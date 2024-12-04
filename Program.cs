using System.Globalization;
using System.IO.Compression;
using System.Resources;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FastAndFrustrating;

internal class Constrs {
    public required int[][] MatA { get; set; }
    public required int[] VecB { get; set; }
}

[JsonSourceGenerationOptions(
    WriteIndented = true,
    GenerationMode = JsonSourceGenerationMode.Default,
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(Constrs))]
internal partial class ConstrsJsonContext : JsonSerializerContext { }

internal class Program {
#if DEBUG
    private static readonly string Constrs = DebugResources.Constrs;
    private static readonly string Flag = DebugResources.Flag;
    private static readonly string KeyInfo = DebugResources.KeyInfo;
#else
    private static readonly string Constrs = Resources.Constrs;
    private static readonly string Flag = Resources.Flag;
    private static readonly string KeyInfo = Resources.KeyInfo;
#endif

    public static void Main(string[] args) {

#if !DEBUG
        if (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName != "vt") {
            Console.WriteLine("No way! You must be a Vidar-Team member to run this app.");
            return;
        }
#endif

        Console.Write("Give me your key:> ");
        var password = Encoding.ASCII.GetBytes(Console.ReadLine() ?? "");

        using var stream = new GZipStream(new MemoryStream(Convert.FromBase64String(Constrs)),
            CompressionMode.Decompress);
        var constrs = JsonSerializer.Deserialize(stream, ConstrsJsonContext.Default.Constrs);
#if DEBUG
            System.Diagnostics.Debug.Assert(constrs != null);
            System.Diagnostics.Debug.Assert(constrs.MatA.Length > 0 && constrs.MatA.Length == constrs.VecB.Length);
            System.Diagnostics.Debug.Assert(constrs.MatA.All(v => v.Length == constrs.MatA[0].Length));
#else
        if (!(constrs is { MatA.Length: > 0 }
            && constrs.MatA.Length == constrs.VecB.Length
            && constrs.MatA.All(v => v.Length == constrs.MatA[0].Length))) {
            Console.WriteLine("An error has occured.");
            return;
        }
#endif

        if (password.Length != constrs.VecB.Length) {
            Console.WriteLine("Try again.");
            return;
        }

        var match = constrs.MatA
            .Zip(constrs.VecB)
            .All(t => t.First.Zip(password.Take(t.First.Length))
                .Select(p => p.First * p.Second)
                .Sum() == t.Second);
        if (!match) {
#if DEBUG
                Console.WriteLine("Constraints error");
#else
            Console.WriteLine("Try again.");
            return;
#endif
        }

        using var aes = Aes.Create();
        var derivedKey = HKDF.DeriveKey(
            HashAlgorithmName.SHA256,
            password,
            (aes.KeySize + aes.BlockSize) / 8,
            info: Encoding.UTF8.GetBytes(KeyInfo));
        aes.Key = derivedKey[..(aes.KeySize / 8)];
        try {
            var result = aes.DecryptCbc(
                Convert.FromBase64String(Flag),
                derivedKey.AsSpan(aes.KeySize / 8, aes.BlockSize / 8));
            Console.WriteLine("Good job! Here is your flag: {0}", Encoding.UTF8.GetString(result));
        } catch (CryptographicException) {
#if DEBUG
                Console.WriteLine("Can't decrypt with given parameters.");
#else
            Console.WriteLine("Try again.");
#endif
        } catch {
            Console.WriteLine("An error has occurred.");
        }

#if DEBUG
            Console.WriteLine("Encrypted buffer with key \"{0}\":", Encoding.UTF8.GetString(password));
            var enc = aes.EncryptCbc(
                Encoding.UTF8.GetBytes("hgame{F4st_4nd_frustr4t1ng_A0T_compilat1on}"),
                derivedKey.AsSpan(aes.KeySize / 8, aes.BlockSize / 8),
                paddingMode: PaddingMode.PKCS7);
            Console.WriteLine(Convert.ToBase64String(enc));
#endif
    }
}