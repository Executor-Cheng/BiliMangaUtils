using System.Linq;
using System.Text.Json;

namespace BiliMangaUtils
{
    public static class JsonElementExtension
    {
        public static bool HasValues(this ref JsonElement j)
            => j.ValueKind == JsonValueKind.Array ? j.EnumerateArray().Any() : j.ValueKind == JsonValueKind.Object ? j.EnumerateObject().Any() : false;
    }
}
