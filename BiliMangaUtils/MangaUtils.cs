using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace BiliMangaUtils
{
    public static class MangaUtils
    {
        /// <summary>
        /// 使用给定的关键词搜索漫画
        /// </summary>
        /// <exception cref="NotImplementedException"/>
        /// <param name="keyword">关键词</param>
        /// <returns></returns>
        public static ComicInfo[] SearchComic(string keyword)
        {
            IDictionary<string, string> headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json"
            };
            using MemoryStream ms = new MemoryStream();
            using Utf8JsonWriter writer = new Utf8JsonWriter(ms);
            writer.WriteStartObject();
            writer.WriteString("key_word", keyword);
            writer.WriteNumber("page_num", 1);
            writer.WriteNumber("page_size", 9);
            writer.WriteEndObject();
            writer.Flush();
            byte[] payload = ms.ToArray();
            string resp = HttpHelper.HttpPost("https://manga.bilibili.com/twirp/comic.v1.Comic/Search?device=pc&platform=web", payload, headers: headers);
            using JsonDocument document = JsonDocument.Parse(resp);
            JsonElement root = document.RootElement;
            if (root.GetProperty("code").GetInt32() == 0)
            {
                return root.GetProperty("data").GetProperty("list").EnumerateArray().Select(p => new ComicInfo(
                    p.GetProperty("id").GetInt32(),
                    p.GetProperty("org_title").GetString(),
                    p.GetProperty("author_name").EnumerateArray().Select(q => q.GetString()).ToArray(),
                    1,
                    p.GetProperty("styles").EnumerateArray().Select(q => q.GetString()).ToArray(),
                    null,
                    default)).ToArray();
            }
            throw new NotImplementedException($"未知的服务器返回:{root.GetRawText()}");
        }
        /// <summary>
        /// 使用给定的漫画Id获取漫画信息
        /// </summary>
        /// <exception cref="NotImplementedException"/>
        /// <param name="comicId">漫画Id</param>
        /// <param name="cookie">用户Cookie</param>
        /// <returns>一个 <see cref="ComicInfo"/> 类的新实例</returns>
        public static ComicInfo GetComicInfo(int comicId, string cookie)
        {
            IDictionary<string, string> headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json"
            };
            using MemoryStream ms = new MemoryStream();
            using Utf8JsonWriter writer = new Utf8JsonWriter(ms);
            writer.WriteStartObject();
            writer.WriteNumber("comic_id", comicId);
            writer.WriteEndObject();
            writer.Flush();
            byte[] payload = ms.ToArray();
            string resp = HttpHelper.HttpPost("https://manga.bilibili.com/twirp/comic.v2.Comic/ComicDetail?device=pc&platform=web", payload, cookie: cookie, headers: headers);
            using JsonDocument document = JsonDocument.Parse(resp);
            JsonElement root = document.RootElement;
            if (root.GetProperty("code").GetInt32() == 0)
            {
                return ComicInfo.ParseResp(in root);
            }
            throw new NotImplementedException($"未知的服务器返回:{root.GetRawText()}");
        }
        /// <summary>
        /// 购买给定章节
        /// </summary>
        /// <exception cref="NotImplementedException"/>
        /// <param name="episodeId">章节Id</param>
        /// <param name="cookie">用户Cookie</param>
        public static void BuyEpisode(int episodeId, int couponId, string cookie)
        {
            IDictionary<string, string> headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json"
            };
            using MemoryStream ms = new MemoryStream();
            using Utf8JsonWriter writer = new Utf8JsonWriter(ms);
            writer.WriteStartObject();
            writer.WriteNumber("buy_method", 2); // 定值,不需要自动购买
            writer.WriteNumber("ep_id", episodeId); // 章节Id
            writer.WriteNumber("coupon_id", couponId); // 漫读券Id
            writer.WriteNumber("auto_pay_gold_status", 2); // 定值,不需要自动购买
            writer.WriteEndObject();
            writer.Flush();
            byte[] payload = ms.ToArray();
            string resp = HttpHelper.HttpPost("https://manga.bilibili.com/twirp/comic.v1.Comic/BuyEpisode?device=pc&platform=web", payload, cookie: cookie, headers: headers);
            using JsonDocument document = JsonDocument.Parse(resp);
            JsonElement root = document.RootElement;
            if (root.GetProperty("code").GetInt32() != 0)
            {
                throw new NotImplementedException($"未知的服务器返回:{root.GetRawText()}");
            }
        }
        /// <summary>
        /// 获取适用于解锁给定章节的漫读券
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        /// <exception cref="NotImplementedException"/>
        /// <param name="episodeId">章节Id</param>
        /// <param name="cookie">用户Cookie</param>
        /// <returns>漫读券</returns>
        public static Coupon GetRecommendCoupon(int episodeId, string cookie)
        {
            IDictionary<string, string> headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json"
            };
            using MemoryStream ms = new MemoryStream();
            using Utf8JsonWriter writer = new Utf8JsonWriter(ms);
            writer.WriteStartObject();
            writer.WriteNumber("ep_id", episodeId); // 章节Id
            writer.WriteEndObject();
            writer.Flush();
            byte[] payload = ms.ToArray();
            string resp = HttpHelper.HttpPost("https://manga.bilibili.com/twirp/comic.v1.Comic/GetEpisodeBuyInfo?device=pc&platform=web", payload, cookie: cookie, headers: headers);
            using JsonDocument document = JsonDocument.Parse(resp);
            JsonElement root = document.RootElement;
            if (root.GetProperty("code").GetInt32() == 0)
            {
                int couponId = root.GetProperty("data").GetProperty("recommend_coupon_id").GetInt32();
                if (couponId > 0)
                {
                    return new Coupon(couponId);
                }
                throw new InvalidOperationException("当前账号没有漫读券");
            }
            throw new NotImplementedException($"未知的服务器返回:{root.GetRawText()}");
        }
        /// <summary>
        /// 获取用户所有的漫读券
        /// </summary>
        /// <exception cref="NotImplementedException"/>
        /// <param name="cookie">用户Cookie</param>
        /// <returns></returns>
        public static IEnumerable<Coupon> GetUserCoupons(string cookie)
        {
            static byte[] createPayload(int page, int pageSize)
            {
                using MemoryStream ms = new MemoryStream();
                using Utf8JsonWriter writer = new Utf8JsonWriter(ms);
                writer.WriteStartObject();
                writer.WriteBoolean("not_expired", true); // 定值,指示获取非过期的漫读券
                writer.WriteNumber("page_num", 1); // 页码
                writer.WriteNumber("page_size", 15); // 页大小
                writer.WriteEndObject();
                writer.Flush();
                return ms.ToArray();
            }
            IDictionary<string, string> headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json"
            };
            for (int page = 1; ; page++)
            {
                byte[] payload = createPayload(page, 15);
                string resp = HttpHelper.HttpPost("https://manga.bilibili.com/twirp/user.v1.User/GetCoupons?device=pc&platform=web", payload, cookie: cookie, headers: headers);
                using JsonDocument document = JsonDocument.Parse(resp);
                JsonElement root = document.RootElement;
                if (root.GetProperty("code").GetInt32() == 0)
                {
                    JsonElement userCoupons = root.GetProperty("data").GetProperty("user_coupons");
                    if (userCoupons.HasValues())
                    {
                        foreach (Coupon coupon in userCoupons.EnumerateArray().Select(p => Coupon.Parse(in p)))
                        {
                            yield return coupon;
                        }
                    }
                    else
                    {
                        yield break;
                    }
                }
                else
                {
                    throw new NotImplementedException($"未知的服务器返回:{root.GetRawText()}");
                }
            }
        }
        /// <summary>
        /// 获取用户Cookie是否有效
        /// </summary>
        /// <exception cref="NotImplementedException"/>
        /// <param name="cookie">用户Cookie</param>
        /// <returns>是否有效</returns>
        public static bool CheckLoginStatus(string cookie)
        {
            string resp = HttpHelper.HttpGet("https://api.bilibili.com/x/web-interface/nav", cookie: cookie);
            using JsonDocument document = JsonDocument.Parse(resp);
            JsonElement root = document.RootElement;
            int code = root.GetProperty("code").GetInt32();
            return code switch
            {
                0 => true,
                -101 => false,
                _ => throw new NotImplementedException($"未知的服务器返回:{root.GetRawText()}"),
            };
        }
    }
}