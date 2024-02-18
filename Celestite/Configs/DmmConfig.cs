using System.Collections.Generic;
using System.Text.Json.Serialization;
using Celestite.Network.Models;

namespace Celestite.Configs
{
    public class OsCrypt
    {
        public string EncryptedKey { get; set; } = string.Empty;
        public bool AuditEnabled { get; set; }
    }
    public class LocalState
    {
        public OsCrypt? OsCrypt { get; set; }
    }

    public class DmmGameCnfContentDetail
    {
        public bool Installed { get; set; }
        public string Version { get; set; } = string.Empty;
        public string Shortcut { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string KeyBindSettingVer { get; set; } = string.Empty;
    }
    public class DmmGameCnfContent
    {
        public string ProductId { get; set; } = string.Empty;
        public TApiGameType GameType { get; set; }
        public DmmGameCnfContentDetail Detail { get; set; } = null!;
    }
    public class DmmGameCnf
    {
        public string DefaultInstallDir { get; set; } = string.Empty;
        public List<DmmGameCnfContent> Contents { get; set; } = [];
    }

    [JsonSerializable(typeof(LocalState))]
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
    public partial class ElectronDataJsonSerializerContext : JsonSerializerContext
    {
    }

    [JsonSerializable(typeof(DmmGameCnf))]
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    public partial class DmmGameCnfJsonSerializerContext : JsonSerializerContext
    {
    }
}
