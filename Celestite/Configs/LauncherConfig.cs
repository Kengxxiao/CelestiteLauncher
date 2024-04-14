using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Hashing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using MemoryPack;
using MemoryPack.Internal;

namespace Celestite.Configs
{
    [Preserve]
    public sealed class EncryptedDictionaryFormatter<TValue> : MemoryPackFormatter<Dictionary<string, TValue?>>
    {
        public static readonly EncryptedDictionaryFormatter<TValue> Formatter = new();
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref Dictionary<string, TValue?>? value)
        {
            var cipher1 = EncryptedInternStringFormatter.GetCipher1();
            if (value == null)
            {
                writer.WriteCollectionHeader(-1 ^ cipher1);
                return;
            }

            var count = value.Count;
            writer.WriteCollectionHeader(count ^ cipher1);
            using var enumerator = value.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var (key, value1) = enumerator.Current;
                EncryptedInternStringFormatter.DefaultFormatter.Serialize(ref writer, ref key);
                writer.WriteValue(value1);
            }
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref Dictionary<string, TValue?>? value)
        {
            var cipher1 = EncryptedInternStringFormatter.GetCipher1();
            var count = Unsafe.ReadUnaligned<int>(ref reader.GetSpanReference(4)) ^ cipher1;
            reader.Advance(4);
            if (count == -1)
            {
                value = null;
                return;
            }

            value = new Dictionary<string, TValue?>(count);
            for (var i = 0; i < count; i++)
            {
                var key = string.Empty;
                EncryptedInternStringFormatter.DefaultFormatter.Deserialize(ref reader, ref key);
                value.Add(key!, reader.ReadValue<TValue?>());
            }
        }
    }

    public sealed class EncryptedDictionaryFormatterAttribute<TValue> : MemoryPackCustomFormatterAttribute<EncryptedDictionaryFormatter<TValue>, Dictionary<string, TValue?>>
    {
        public override EncryptedDictionaryFormatter<TValue> GetFormatter() => EncryptedDictionaryFormatter<TValue>.Formatter;
    }

    [Preserve]
    public sealed class EncryptedInternStringFormatter : MemoryPackFormatter<string>
    {
        private const int EncryptedDataVersion = 1;

        public static readonly Dictionary<string, EncryptedInternStringFormatter> Pool = [];

        public static readonly EncryptedInternStringFormatter DefaultFormatter =
            new("9bcef9c13af2f883413426d64f622ade"u8);

        public EncryptedInternStringFormatter(string typeName)
        {
            _hashCode = (int)XxHash32.HashToUInt32(Encoding.UTF8.GetBytes(typeName).AsSpan());
        }

        public EncryptedInternStringFormatter(ReadOnlySpan<byte> typeName)
        {
            _hashCode = (int)XxHash32.HashToUInt32(typeName);
        }

        private static bool _initiated;
        private static readonly byte[] AllBytes = GC.AllocateUninitializedArray<byte>(256, true);
        private static int _currentCipherBookOffset;
        private readonly int _hashCode;
        internal static void InitBytesObfuscator(int randomSeed)
        {
            unsafe
            {
                fixed (byte* allBytes = AllBytes)
                {
                    for (var i = 0; i < 256; i++)
                        allBytes[i] = (byte)(i & 0xff);
                }
            }
            var random = new Random(randomSeed);
            random.Shuffle(AllBytes);
            _initiated = true;
            _currentCipherBookOffset = 0;
        }

        [Preserve]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetCipher1()
        {
            unsafe
            {
                var bytes = stackalloc byte[4];
                for (var i = 0; i < 4; i++)
                {
                    bytes[i] = AllBytes[_currentCipherBookOffset++ % AllBytes.Length];
                }
                return *(int*)bytes;
            }
        }

        [Preserve]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref string? value)
        {
            if (!_initiated)
                throw new ApplicationException("obfuscator is not initiated");

            var cipher1 = GetCipher1();

            if (string.IsNullOrEmpty(value))
            {
                writer.WriteCollectionHeader(-1 ^ _hashCode ^ cipher1);
                return;
            }

            var dataBytes = Encoding.UTF8.GetBytes(value);
            for (var i = 0; i < dataBytes.Length; i++)
            {
                dataBytes[i] ^= AllBytes[_currentCipherBookOffset++ % AllBytes.Length];
            }
            var header = (EncryptedDataVersion << 16) | (dataBytes.Length) & 0xffff;

            writer.WriteCollectionHeader(header ^ _hashCode ^ cipher1);
            writer.WriteSpanWithoutLengthHeader<byte>(dataBytes);
        }

        [Preserve]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Deserialize(ref MemoryPackReader reader, scoped ref string? value)
        {
            if (!_initiated)
                throw new ApplicationException("obfuscator is not initiated");

            var header = Unsafe.ReadUnaligned<int>(ref reader.GetSpanReference(4));
            reader.Advance(4);
            var cipher1 = GetCipher1();
            header = header ^ _hashCode ^ cipher1;
            if (header == -1)
            {
                value = null;
                return;
            }

            var encryptedDataVersion = (header >> 16) & 0xffff;

            switch (encryptedDataVersion)
            {
                case 1:
                    {
                        var bytesLength = header & 0xffff;
                        if (bytesLength == 0)
                        {
                            value = string.Empty;
                            return;
                        }
                        var span = new Span<byte>();
                        reader.ReadSpanWithoutReadLengthHeader(bytesLength, ref span);
                        for (var i = 0; i < span.Length; i++)
                        {
                            span[i] ^= AllBytes[_currentCipherBookOffset++ % AllBytes.Length];
                        }
                        value = string.Intern(Encoding.UTF8.GetString(span));
                    }
                    break;
                default: throw new NotImplementedException();
            }

        }

    }

    public sealed class EncryptedInternStringFormatterAttribute
        ([CallerMemberName] string? typeNameHash = null) : MemoryPackCustomFormatterAttribute<EncryptedInternStringFormatter, string>
    {
        public override EncryptedInternStringFormatter GetFormatter()
        {
            if (string.IsNullOrEmpty(typeNameHash))
                return EncryptedInternStringFormatter.DefaultFormatter;
            if (EncryptedInternStringFormatter.Pool.TryGetValue(typeNameHash, out var formatter))
                return formatter;
            formatter = new EncryptedInternStringFormatter(typeNameHash);
            EncryptedInternStringFormatter.Pool[typeNameHash] = formatter;
            return formatter;
        }
    }

    [MemoryPackable(SerializeLayout.Explicit)]
    public partial class NonWindowsExclusiveConfig
    {
        [MemoryPackOrder(0)]
        [EncryptedInternStringFormatter]
        public string WinePath { get; set; } = string.Empty;
    }

    [MemoryPackable(SerializeLayout.Explicit)]
    public partial class LauncherConfig : IJsonOnDeserialized
    {
        private const int ConfigVersion = 1;
        private const int PreservedBytes = 32 - 24;

        [MemoryPackOrder(0)]
        public BaseSection BaseSection { get; set; } = new();
        [MemoryPackOrder(1)]
        public Dictionary<Guid, AccountObject> Accounts { get; set; } = [];
        [MemoryPackOrder(2)] public NonWindowsExclusiveConfig NonWindowsConfig { get; set; } = new();
        [MemoryPackOrder(3)]
        [EncryptedDictionaryFormatter<GameSettingsForLauncher>]
        public Dictionary<string, GameSettingsForLauncher> GameSettings { get; set; } = [];

        [JsonIgnore]
        [MemoryPackIgnore]
        internal Dictionary<string, Guid> FastAccountsIndex { get; } = [];

        [MemoryPackOnSerializing]
        private static void WriteHeader<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, ref LauncherConfig? _)
            where TBufferWriter : IBufferWriter<byte> // .NET Standard 2.1, use where TBufferWriter : class, IBufferWriter<byte>
        {
            writer.WriteSpanWithoutLengthHeader("KCNF"u8);
            writer.WriteValue(ConfigVersion); // 4
            var randomSeed = RandomNumberGenerator.GetInt32(0, int.MaxValue);
            EncryptedInternStringFormatter.InitBytesObfuscator(randomSeed);
            writer.WriteValue(randomSeed);
            unsafe
            {
                var randomSeedHash1 = ~randomSeed ^ 0x78549bef;
                var span = new ReadOnlySpan<byte>(Unsafe.AsPointer(ref randomSeedHash1), 4);
                writer.WriteValue(Crc32.HashToUInt32(span));
            }
            writer.WriteValue(DateTime.Now);
            writer.Advance(PreservedBytes);
        }

        [MemoryPackOnDeserializing]
        private static void ReadHeader(ref MemoryPackReader reader, ref LauncherConfig? _)
        {
            Span<byte> span = stackalloc byte[4];
            unsafe
            {
                reader.ReadSpanWithoutReadLengthHeader(4, ref span);
            }
            if (!span.SequenceEqual("KCNF"u8))
                throw new ApplicationException("Invalid config file");
            var configVersion = reader.ReadValue<int>();
            if (configVersion > ConfigVersion)
                throw new ApplicationException("Config version is too high");
            var randomSeed = reader.ReadValue<int>();
            unsafe
            {
                var randomSeedChecksum = reader.ReadValue<uint>();
                var randomSeedHash1 = ~randomSeed ^ 0x78549bef;
                var randomSeedChecksumSpan = new ReadOnlySpan<byte>(Unsafe.AsPointer(ref randomSeedHash1), 4);
                var randomSeedChecksumCrc = Crc32.HashToUInt32(randomSeedChecksumSpan);
                if (randomSeedChecksumCrc != randomSeedChecksum)
                    throw new ApplicationException("Invalid config header");
            }
            reader.ReadValue<DateTime>();
            EncryptedInternStringFormatter.InitBytesObfuscator(randomSeed);
            reader.Advance(PreservedBytes);
        }

        [MemoryPackOnDeserialized]
        public void OnDeserialized()
        {
            foreach (var v in Accounts.Values.Where(v => v.SaveEmail))
            {
                FastAccountsIndex.Add(v.Email, v.Id);
            }
        }
    }
    [JsonSerializable(typeof(LauncherConfig))]
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower, WriteIndented = true, GenerationMode = JsonSourceGenerationMode.Metadata)]
    public partial class LauncherConfigJsonSerializerContext : JsonSerializerContext { }

    [JsonSerializable(typeof(IdToken))]
    public partial class IdTokenJsonSerializerContext : JsonSerializerContext
    {
    }

    [MemoryPackable(SerializeLayout.Explicit)]
    public partial class BaseSection
    {
        [MemoryPackOrder(0)]
        [EncryptedInternStringFormatter]
        public string Locale { get; set; } = "zh-CN";
        [MemoryPackOrder(1)]
        public Guid? LastLoginId { get; set; } = null;
        [MemoryPackOrder(2)] public bool UseMica { get; set; } = true;
        [MemoryPackOrder(3)] public bool UseAcrylic { get; set; } = false;
        [MemoryPackOrder(4)] public bool MinimizeToTray { get; set; } = false;
        [MemoryPackOrder(5)] public bool EnableHimariProxy { get; set; } = false;
        [MemoryPackOrder(6)] public bool BypassSystemProxy { get; set; } = false;
        [MemoryPackOrder(7)] public bool EnableEmbeddedWebView { get; set; } = false;
        [MemoryPackOrder(8)] public bool DisableIFrameEx { get; set; } = false;
        [MemoryPackOrder(9)] public bool SafeFanzaIcon { get; set; } = false;
    }

    [MemoryPackable(SerializeLayout.Explicit)]
    public partial class AccountObject
    {
        [MemoryPackOrder(0)]
        public Guid Id { get; set; } = Guid.NewGuid();
        [MemoryPackOrder(1)]
        [EncryptedInternStringFormatter]
        public string Email { get; set; } = string.Empty;
        [MemoryPackOrder(2)]
        [EncryptedInternStringFormatter]
        public string Password { get; set; } = string.Empty;
        [MemoryPackOrder(3)]
        public bool SaveEmail { get; set; } = false;
        [MemoryPackOrder(4)]
        public bool SavePassword { get; set; } = false;
        [MemoryPackOrder(5)]
        public bool AutoLogin { get; set; } = false;
        [MemoryPackOrder(6)]
        [EncryptedInternStringFormatter]
        public string UserId { get; set; } = string.Empty;
        [MemoryPackOrder(7)]
        [EncryptedInternStringFormatter]
        public string RefreshToken { get; set; } = string.Empty;

        [MemoryPackOrder(8)][EncryptedInternStringFormatter] public string NickName { get; set; } = string.Empty;
    }

    [MemoryPackable(SerializeLayout.Explicit)]
    public partial class ThemeSection
    {
    }

    [MemoryPackable(SerializeLayout.Explicit)]
    public partial class IdToken
    {
        [MemoryPackOrder(0)]
        [JsonPropertyName("aud")]
        [EncryptedInternStringFormatter]
        public string? Audience { get; set; }
        [MemoryPackOrder(1)]
        [JsonPropertyName("exp")]
        public long ExpirationTime { get; set; }
        [MemoryPackOrder(2)]
        [JsonPropertyName("iat")]
        public long IssuedAt { get; set; }
        [MemoryPackOrder(3)]
        [JsonPropertyName("iss")]
        [EncryptedInternStringFormatter]
        public string? Issuer { get; set; }
        [MemoryPackOrder(4)]
        [JsonPropertyName("nonce")]
        public string? Nonce { get; set; }
        [MemoryPackOrder(5)]
        [JsonPropertyName("user_id")]
        [EncryptedInternStringFormatter]
        public string? UserId { get; set; }
    }

    [MemoryPackable(SerializeLayout.Explicit)]
    public partial class GameSettingsForLauncher
    {
        [MemoryPackOrder(0)]
        [EncryptedInternStringFormatter]
        public string ExtraCommandLine { get; set; } = string.Empty;

        [MemoryPackOrder(1)]
        public bool ForceSkipFileCheck { get; set; }

        [MemoryPackOrder(2)]
        public bool ForcePin { get; set; }
    }
}
