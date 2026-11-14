using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shared;
using Shared.Attributes;
using Shared.Models.Templates;
using Shared.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace AniSkip;

public class AniSkipController : BaseController
{
    // ponytail: без лимита размера и без TTL, достаточно для личного сервера; ограничить, если вырастет
    static readonly ConcurrentDictionary<string, int> malCache = new();

    /// <summary>
    /// Метки заставки/титров из AniSkip в формате SegmentTpl ({"ad":[],"skip":[{"start","end"}]}).
    /// title + year → MAL ID через Shikimori (id Shikimori == MAL id), либо mal_id напрямую (для отладки).
    /// Ошибки сети отдают 503 (не кэшируется), «меток нет» отдаёт {} (кэшируется).
    /// </summary>
    [HttpGet]
    [Staticache(cacheMinutes: 720, always: true, queryKeys: new[] { "title", "year", "e", "mal_id" })]
    [Route("aniskip")]
    async public Task<ActionResult> Index(string title, int year, int e, int mal_id)
    {
        if (e <= 0 || (mal_id <= 0 && string.IsNullOrWhiteSpace(title)))
            return BadRequest();

        int mal = mal_id;

        if (mal <= 0)
        {
            string key = $"{title}:{year}";

            if (!malCache.TryGetValue(key, out mal))
            {
                string shiki = await Http.Get($"https://shikimori.io/api/animes?limit=5&search={HttpUtility.UrlEncode(title)}", timeoutSeconds: 8);
                if (shiki == null)
                    return StatusCode(503);

                try
                {
                    // только tv; при известном годе требуем совпадение года выхода
                    var anime = JArray.Parse(shiki).FirstOrDefault(a =>
                        (string)a["kind"] == "tv" &&
                        (year == 0 || ((string)a["aired_on"])?.StartsWith(year.ToString()) == true));

                    mal = anime == null ? 0 : (int)anime["id"];
                }
                catch (Exception)
                {
                    return StatusCode(503);
                }

                if (mal > 0)
                    malCache[key] = mal;
            }
        }

        var segments = new SegmentTpl();

        if (mal > 0)
        {
            // statusCodeOK: false, потому что «меток нет» AniSkip отдаёт как 404 с телом
            string json = await Http.Get($"https://api.aniskip.com/v2/skip-times/{mal}/{e}?types=op&types=ed&types=recap&episodeLength=0", timeoutSeconds: 8, statusCodeOK: false);
            if (json == null)
                return StatusCode(503);

            try
            {
                var results = JObject.Parse(json)["results"] as JArray;

                // episodeLength=0 возвращает метки от разных рипов; берём первую каждого типа
                foreach (string type in new[] { "op", "ed", "recap" })
                {
                    var r = results?.FirstOrDefault(i => (string)i["skipType"] == type);
                    if (r != null)
                        segments.skip((int)(double)r["interval"]["startTime"], (int)Math.Ceiling((double)r["interval"]["endTime"]));
                }
            }
            catch (Exception)
            {
                return StatusCode(503);
            }
        }

        return ContentTo(JsonConvert.SerializeObject(segments.ToObject() ?? new Dictionary<string, IReadOnlyList<SegmentDto>>()), "application/json; charset=utf-8");
    }
}
