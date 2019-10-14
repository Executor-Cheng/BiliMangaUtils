using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.Linq;
using System.Threading;

namespace BiliMangaUtils
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            RootCommand root = new RootCommand();
            root.Handler = CommandHandler.Create(typeof(Program).GetMethod(nameof(DefaultExecution), Type.EmptyTypes));
            // Options
            RequiredOption comicIdOpt = new RequiredOption("-comicId")
            {
                Argument = new Argument<int>
                {
                    Arity = ArgumentArity.ExactlyOne
                },
                Description = "漫画Id[必选]"
            };
            comicIdOpt.AddAlias("-comic_id");
            RequiredOption episodeIdOpt = new RequiredOption("-episodeId") 
            {
                Argument = new Argument<int>
                {
                    Arity = ArgumentArity.ExactlyOne
                },
                Description = "章节Id[必选]"
            };
            episodeIdOpt.AddAlias("-ep_id");
            episodeIdOpt.AddAlias("-episode_id");
            RequiredOption cookieOpt = new RequiredOption("-cookie")
            {
                Argument = new Argument<string>
                {
                    Arity = ArgumentArity.ExactlyOne
                },
                Description = "用户Cookie[必选]",
            };
            cookieOpt.AddAlias("-ck");
            Option couponIdOpt = new Option("-couponId") 
            { 
                Argument = new Argument<int>()
                {
                    Arity = ArgumentArity.ZeroOrOne
                },
                Description = "漫读券Id"
            };
            couponIdOpt.AddAlias("-cp_id");
            couponIdOpt.AddAlias("-coupon_id");
            // Commands
            Command purchaseOneCmd = new Command("purchaseOne")
            {
                Description = "购买单章漫画"
            };
            root.AddCommand(purchaseOneCmd);
            purchaseOneCmd.AddOption(episodeIdOpt);
            purchaseOneCmd.AddOption(cookieOpt);
            purchaseOneCmd.AddOption(couponIdOpt);
            purchaseOneCmd.Handler = CommandHandler.Create(typeof(Program).GetMethod(nameof(PurchaseOne)));

            Command purchaseAllCmd = new Command("purchaseAll")
            {
                Description = "购买全本漫画"
            };
            root.AddCommand(purchaseAllCmd);
            purchaseAllCmd.AddOption(comicIdOpt);
            purchaseAllCmd.AddOption(cookieOpt);
            purchaseAllCmd.AddOption(couponIdOpt);
            purchaseAllCmd.Handler = CommandHandler.Create(typeof(Program).GetMethod(nameof(PurchaseAll), new Type[] { typeof(int), typeof(string), typeof(int?) }));
            
            return new CommandLineBuilder(root).UseRequiredOptions().UseDefaults().Build().Invoke(args);
        }

        public static int PurchaseOne(int episodeId, string cookie, int? couponId)
        {
            try
            {
                Coupon coupon = couponId.HasValue ? new Coupon(couponId.Value) : MangaUtils.GetRecommendCoupon(episodeId, cookie);
                MangaUtils.BuyEpisode(episodeId, coupon.Id, cookie);
                Console.WriteLine($"Successfully Unlocked Episode:{episodeId}");
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Can't Buy Episode because:{e}");
                return -1;
            }
        }

        public static int PurchaseAll(int comicId, string cookie, int? couponId)
        {
            try
            {
                ComicInfo comic = MangaUtils.GetComicInfo(comicId, cookie);
                return PurchaseAll(comic, cookie, couponId);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unexpected Exception encountered while Getting ComicInfo:{e}");
                return -1;
            }
        }

        private static int PurchaseAll(ComicInfo comic, string cookie, int? couponId)
        {
            int count = -1;
            bool shouldSleep = false;
            if (comic.Episodes.Any(p => p.Locked && !p.UserPurchased))
            {
                foreach (EpisodeInfo episode in comic.Episodes.Where(p => p.Locked && !p.UserPurchased).Reverse()) // 获取到的是倒序
                {
                    if (PurchaseOne(episode.Id, cookie, couponId) != 0)
                    {
                        return count;
                    }
                    count++;
                    if (shouldSleep)
                    {
                        Thread.Sleep(300);
                    }
                    shouldSleep = true;
                }
            }
            return count + 1;
        }

        public static void DefaultExecution()
        {
            string cookie;
            while (true)
            {
                Console.Write("请输入你的Cookie:");
                cookie = Console.ReadLine();
                try
                {
                    if (MangaUtils.CheckLoginStatus(cookie))
                    {
                        break;
                    }
                    Console.WriteLine("给定的Cookie无效,请重新输入");
                    Console.ReadLine();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"发生了意外情况:{e}\n请重试");
                    Console.ReadLine();
                }
            }
            while (true)
            {
                Console.Clear();
                Console.Write("请输入漫画Id/漫画名(支持输入部分名称):");
                string keyword = Console.ReadLine();
                ComicInfo comic;
                try
                {
                    if (int.TryParse(keyword, out int comicId))
                    {
                        comic = MangaUtils.GetComicInfo(comicId, cookie);
                    }
                    else
                    {
                        ComicInfo[] comics = MangaUtils.SearchComic(keyword);
                        if ((comic = comics.FirstOrDefault(p => p.Name == keyword)) == null)
                        {
                            while (true)
                            {
                                Console.Clear();
                                Console.WriteLine("已搜索到下列漫画");
                                Console.WriteLine(string.Join("\r\n", comics.Select((p, q) => $"{q + 1}. {p.Name}")));
                                Console.Write("请输入对应序号,输入0取消操作:");
                                if (int.TryParse(Console.ReadLine(), out int seq) && seq <= comics.Length && seq > -1)
                                {
                                    if (seq > 0)
                                    {
                                        comic = comics[seq - 1];
                                    }
                                    break;
                                }
                                Console.WriteLine("输入无效，请重新输入");
                                Console.ReadLine();
                            }
                        }
                        if (comic == null)
                        {
                            continue;
                        }
                        comic = MangaUtils.GetComicInfo(comic.Id, cookie);
                    }
                    bool quitSig = false;
                    while (!quitSig)
                    {
                        Console.Clear();
                        Console.WriteLine($"当前漫画:{comic.Name}[{comic.Id}],你还剩{comic.Episodes.Count(p => p.Locked && !p.UserPurchased)}章未解锁");
                        Console.Write("是否全部解锁(Y/n)?");
                        switch (Console.ReadLine().ToLower())
                        {
                            case "y":
                                {
                                    int unlocked = PurchaseAll(comic, cookie, null);
                                    Console.WriteLine($"已成功解锁{unlocked}章");
                                    Console.ReadLine();
                                    quitSig = true;
                                    break;
                                }
                            case "n":
                                {
                                    quitSig = true;
                                    break;
                                }
                            default:
                                {
                                    Console.WriteLine("输入无效,请重新输入");
                                    Console.ReadLine();
                                    break;
                                }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"发生了意外情况:{e}\n请重试");
                }
            }
        }
    }
}