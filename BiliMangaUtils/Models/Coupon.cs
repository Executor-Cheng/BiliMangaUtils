using System;
using System.Text.Json;

namespace BiliMangaUtils
{
    public class Coupon
    {
        /// <summary>
        /// Id
        /// </summary>
        public int Id { get; }
        /// <summary>
        /// 数量
        /// </summary>
        public int Count { get; set; }
        /// <summary>
        /// 过期时间
        /// </summary>
        public DateTime Expire { get; }
        /// <summary>
        /// 类型 (Todo: 做成枚举)
        /// </summary>
        public int Type { get; }
        public Coupon(int id) : this(id, 1, default, 1) { }
        /// <summary>
        /// 内部使用
        /// </summary>
        private Coupon(int id, int count, DateTime expire, int type)
        {
            Id = id;
            Count = count;
            Expire = expire;
            Type = type;
        }
        /// <summary>
        /// 使用给定的服务器回复中的漫读券节点初始化 <see cref="Coupon"/> 类的新实例
        /// </summary>
        /// <param name="coupon">user_coupons中的各个节点</param>
        /// <returns>一个 <see cref="Coupon"/> 类的新实例</returns>
        public static Coupon Parse(in JsonElement coupon)
        {
            return new Coupon(
                coupon.GetProperty("ID").GetInt32(),
                coupon.GetProperty("remain_amount").GetInt32(),
                coupon.GetProperty("expire_time").GetDateTime(),
                coupon.GetProperty("type_num").GetInt32());
        }
    }

}