using System.CommandLine.Invocation;
using System.Linq;

namespace System.CommandLine
{
    public class RequiredOption : Option
    {
        public RequiredOption(string alias, string description = null) : base(alias, description) { }
        public RequiredOption(string[] aliases, string description = null) : base(aliases, description) { }
    }
}

namespace System.CommandLine.Builder
{
    public static class CommandLineExtensions
    {
        public static CommandLineBuilder UseRequiredOptions(this CommandLineBuilder value)
        {
            value.UseMiddleware(async (context, next) => {
                var options = context.ParseResult.CommandResult;
                foreach (Option item in options.Command.Children.OfType<Option>())
                {
                    if (item is RequiredOption && !options.Children.Any(s => s.Name == item.Name))
                    {
                        context.Console.Out.WriteLine($"{item.Name} is a REQUIRED option\n");
                        var help = new HelpResult();
                        help.Apply(context);
                        return;
                    }
                }
                await next(context);
            });
            return value;
        }
    }
}