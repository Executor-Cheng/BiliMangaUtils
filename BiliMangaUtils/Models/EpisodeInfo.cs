using System;
using System.Text.Json;

namespace BiliMangaUtils
{
    public class EpisodeInfo
    {
        /// <summary>
        /// 章节Id
        /// </summary>
        public int Id { get; }
        /// <summary>
        /// 章节标题
        /// </summary>
        public string Title { get; }
        /// <summary>
        /// 章节评论数量
        /// </summary>
        public int CommentCount { get; }
        /// <summary>
        /// 用户是否已购买此章节
        /// </summary>
        public bool UserPurchased { get; }
        /// <summary>
        /// 是否为付费章节
        /// </summary>
        public bool Locked { get; }
        /// <summary>
        /// 购买章节所需漫币
        /// </summary>
        public int Price { get; }
        /// <summary>
        /// 章节发布时间
        /// </summary>
        public DateTime ReleaseTime { get; }
        /// <summary>
        /// 内部使用
        /// </summary>
        private EpisodeInfo(int id, string title, int commentCount, bool userPaid, bool locked, int price, DateTime releaseTime)
        {
            Id = id;
            Title = title;
            CommentCount = commentCount;
            UserPurchased = userPaid;
            Locked = locked;
            Price = price;
            ReleaseTime = releaseTime;
        }
        /// <summary>
        /// 使用给定的服务器回复中的章节节点初始化 <see cref="EpisodeInfo"/> 类的新实例
        /// </summary>
        /// <param name="episode">ep_id中的各个节点</param>
        /// <returns>一个 <see cref="EpisodeInfo"/> 类的新实例</returns>
        public static EpisodeInfo Parse(in JsonElement episode)
        {
            return new EpisodeInfo(
                episode.GetProperty("id").GetInt32(),
                episode.GetProperty("title").GetString(),
                episode.GetProperty("comments").GetInt32(),
                !episode.GetProperty("is_locked").GetBoolean(),
                episode.GetProperty("pay_mode").GetInt32() == 1,
                episode.GetProperty("pay_gold").GetInt32(),
                DateTime.Parse(episode.GetProperty("pub_time").GetString())); // Fuck M$, episode.GetProperty("pub_time").GetDateTime() will cause FormatException
        }
    }

}