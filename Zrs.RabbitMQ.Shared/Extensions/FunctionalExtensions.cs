using System.Text;
using System.Text.Json;

namespace Zrs.RabbitMQ.Shared.Extensions;

internal static class FunctionalExtensions
{
    public static byte[] ToUTF8Bytes(this string s) => Encoding.UTF8.GetBytes(s);
    public static string ToUTF8String(this byte[] bytes) => Encoding.UTF8.GetString(bytes);

    public static string JsonSerialize<T>(this T obj, JsonSerializerOptions? options = null) => JsonSerializer.Serialize(obj, options);
    public static T? JsonDeserialize<T>(this string json, JsonSerializerOptions? options = null) => JsonSerializer.Deserialize<T>(json, options);
}