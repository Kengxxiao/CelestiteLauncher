using System.ComponentModel.DataAnnotations.Schema;
using Dapper;
using Microsoft.Data.Sqlite;

namespace Celestite.Network.Models
{
    [DapperAot]
    public class ElectronCookie
    {
        public static ElectronCookie? GetCookieByName(SqliteConnection connection, string name) =>
            connection.QueryFirstOrDefault<ElectronCookie>(
                "select encrypted_value, expires_utc, name, host_key from cookies where name = @name and host_key = '.dmm.com'", new { name });

        public static void UpdateCookie(SqliteConnection connection, ElectronCookie cookie) =>
            connection.Execute(
                "update cookies set encrypted_value = @EncryptedValue, expires_utc = @ExpiresUtc where name = @Name and host_key = @HostKey",
                new { cookie.EncryptedValue, cookie.ExpiresUtc, cookie.Name, cookie.HostKey });

        [UseColumnAttribute]
        [Column("creation_utc")]
        public long CreationUtc { get; set; }
        [UseColumnAttribute]
        [Column("host_key")]
        public string HostKey { get; set; } = string.Empty;
        [UseColumnAttribute]
        [Column("name")]
        public string Name { get; set; } = string.Empty;
        [UseColumnAttribute]
        [Column("value")]
        public string Value { get; set; } = string.Empty;
        [UseColumnAttribute]
        [Column("path")]
        public string Path { get; set; } = string.Empty;
        [UseColumnAttribute]
        [Column("expires_utc")]
        public long ExpiresUtc { get; set; }
        [UseColumnAttribute]
        [Column("is_secure")]
        public long IsSecure { get; set; }
        [UseColumnAttribute]
        [Column("is_httponly")]
        public long IsHttpOnly { get; set; }
        [UseColumnAttribute]
        [Column("last_access_utc")]
        public long LastAccessUtc { get; set; }
        [UseColumnAttribute]
        [Column("has_expires")]
        public long HasExpires { get; set; }
        [UseColumnAttribute]
        [Column("is_persistent")]
        public long IsPersistent { get; set; }
        [UseColumnAttribute]
        [Column("priority")]
        public long Priority { get; set; }
        [UseColumnAttribute]
        [Column("encrypted_value")]
        public byte[] EncryptedValue { get; set; } = [];
        [UseColumnAttribute]
        [Column("samesite")]
        public long SameSite { get; set; }
        [UseColumnAttribute]
        [Column("source_scheme")]
        public long SourceScheme { get; set; }
    }
}
