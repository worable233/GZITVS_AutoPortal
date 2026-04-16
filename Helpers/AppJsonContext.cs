using System.Text.Json.Serialization;
using AutoPortal.Models;
using AutoPortal.Services;

namespace AutoPortal.Helpers
{
    [JsonSerializable(typeof(LoginConfig))]
    [JsonSerializable(typeof(AppSettings))]
    public partial class AppJsonContext : JsonSerializerContext { }
}
