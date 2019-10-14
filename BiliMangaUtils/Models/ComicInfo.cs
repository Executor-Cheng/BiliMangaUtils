using System;
using System.Linq;
using System.Text.Json;

namespace BiliMangaUtils
{
    public class ComicInfo
    {
        /// <summary>
        /// 漫画Id
        /// </summary>
        public int Id { get; }
        /// <summary>
        /// 漫画名称
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// 漫画作者
        /// </summary>
        public string[] Authors { get; }
        /// <summary>
        /// 漫画作者,以逗号分隔
        /// </summary>
        public string AuthorsString => string.Join(",", Authors ?? Array.Empty<string>());
        /// <summary>
        /// 漫画类型,Todo: 定义为枚举
        /// </summary>
        public int Type { get; }
        /// <summary>
        /// 漫画标签
        /// </summary>
        public string[] Styles { get; }
        /// <summary>
        /// 漫画标签,以逗号分隔
        /// </summary>
        public string StylesString => string.Join(",", Styles ?? Array.Empty<string>());
        /// <summary>
        /// 漫画章节
        /// </summary>
        public EpisodeInfo[] Episodes { get; }
        /// <summary>
        /// 漫画发布时间
        /// </summary>
        public DateTime ReleaseTime { get; }
        /// <summary>
        /// 使用给定的参数初始化 <see cref="ComicInfo"/> 类的新实例
        /// </summary>
        /// <param name="id">漫画Id</param>
        /// <param name="name">漫画名称</param>
        /// <param name="authors">漫画作者</param>
        /// <param name="type">漫画类型</param>
        /// <param name="styles">漫画标签</param>
        /// <param name="episodes">漫画章节</param>
        /// <param name="releaseTime">漫画发布时间</param>
        public ComicInfo(int id, string name, string[] authors, int type, string[] styles, EpisodeInfo[] episodes, DateTime releaseTime)
        {
            Id = id;
            Name = name;
            Authors = authors;
            Type = type;
            Styles = styles;
            Episodes = episodes;
            ReleaseTime = releaseTime;
        }
        /// <summary>
        /// 使用给定的服务器完整回复初始化 <see cref="ComicInfo"/> 类的新实例
        /// </summary>
        /// <param name="root">服务器完整回复的根节点</param>
        /// <returns>一个 <see cref="ComicInfo"/> 类的新实例</returns>
        public static ComicInfo ParseResp(in JsonElement root)
        {
            JsonElement data = root.GetProperty("data");
            EpisodeInfo[] episodes = data.GetProperty("ep_list").EnumerateArray().Select(p => EpisodeInfo.Parse(in p)).ToArray();
            return new ComicInfo(
                data.GetProperty("id").GetInt32(),
                data.GetProperty("title").GetString(),
                data.GetProperty("author_name").EnumerateArray().Select(p => p.GetString()).ToArray(),
                data.GetProperty("comic_type").GetInt32(),
                data.GetProperty("styles").EnumerateArray().Select(p => p.GetString()).ToArray(), 
                episodes,
                episodes.Min(p => p.ReleaseTime));
        }
    }

}