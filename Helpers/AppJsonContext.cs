using System.Text.Json.Serialization;
using AutoPortal.Models;

namespace AutoPortal.Helpers
{
    [JsonSerializable(typeof(LoginConfig))]
    public partial class AppJsonContext : JsonSerializerContext { }
}
