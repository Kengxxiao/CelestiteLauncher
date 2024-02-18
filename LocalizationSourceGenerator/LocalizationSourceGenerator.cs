using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace LocalizationSourceGenerator
{
    [Generator(LanguageNames.CSharp)]
    public class LocalizationSourceGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Debugger.Launch();
            var defaultLocaleFile = context.AdditionalTextsProvider.Where(static x => x.Path.EndsWith("zh-CN.json"));
            var contents = defaultLocaleFile.Select((text, cancellationToken) => text.GetText(cancellationToken)!.ToString());
            context.RegisterSourceOutput(contents, (spc, content) =>
            {
                var lines = content.Split('\n').Where(x => x.Trim().StartsWith("\"")).Select(x => x.Split(':')[0].Trim().Replace("\"", "")).ToArray();
                StringBuilder enumSourceBuilder = new();
                enumSourceBuilder.AppendLine("using System.Collections.Generic;");
                enumSourceBuilder.AppendLine("namespace Celestite.I18N;");
                enumSourceBuilder.AppendLine("public static partial class Localization");
                enumSourceBuilder.AppendLine("{");
                foreach (var line in lines)
                {
                    enumSourceBuilder.AppendLine($"    public static string {line} {{ get => LocalizationData.GetValueOrDefault(\"{line}\", \"ERR_LOCALE_NOTFOUND_{line}\")!; }}");
                }
                enumSourceBuilder.AppendLine("}");
                spc.AddSource("Localization.g.cs", enumSourceBuilder.ToString());
            });
        }
    }
}
