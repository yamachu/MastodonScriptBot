using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.IO;
using Mastodot;
using Mastodot.Entities;
using System.Reflection;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Threading.Tasks;

namespace SampleMastodon
{
    class Program
    {
        static void Main(string[] args)
        {
            new ScriptEvaluateBot().StreamEvaluate("mastodon.cloud", "AccessToken~~~~").Wait();
        }
    }

    // http://tech.guitarrapc.com/entry/2016/05/04/150011 を参考にした
    class ScriptEvaluateBot
    {
        private Dictionary<string, IDisposable> EvaluateScriptCollection;
        private MastodonClient Client;
        private IObservable<IStreamEntity> Stream;

        private static readonly string[] DefaultImports =
        {
            "System",
            "System.IO",
            "System.Linq",
            "System.Collections.Generic",
            "System.Text",
            "System.Text.RegularExpressions",
            "System.Net",
            "System.Net.Http",
            "System.Threading",
            "System.Threading.Tasks",
            "System.Reactive.Linq",
            "Mastodot",
            "Mastodot.Entities",
        };

        private static readonly Assembly[] DefaultReferences =
        {
            typeof(Enumerable).GetTypeInfo().Assembly,
            typeof(List<string>).GetTypeInfo().Assembly,
            typeof(System.Net.Http.HttpClient).GetTypeInfo().Assembly,
            typeof(IStreamEntity).GetTypeInfo().Assembly
        };

        public ScriptEvaluateBot()
        {
            EvaluateScriptCollection = new Dictionary<string, IDisposable>();
        }

        public async Task StreamEvaluate(string host, string accessToken)
        {
            Console.WriteLine("Start StreamEvaluateBot");
            var scriptDir = "Script";

            Client = new MastodonClient(host, accessToken);
            Stream = Client.GetObservablePublicTimeline();

            var scriptFiles = Directory.GetFiles(scriptDir, "*.csx");

            Parallel.ForEach(scriptFiles, async (name) =>
            {
                await Eval(scriptDir, Path.GetFileName(name));
            });

            var fileChangeWatcher = new FileSystemWatcher
            {
                Path = scriptDir,
                Filter = "*.csx*",
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };

            var fileCreatedOrChanged = new FileSystemEventHandler(async (sender, e) =>
            {
                if (!File.Exists(Path.Combine(scriptDir, e.Name))) return;
                if (e.Name.Split('.').Last() != "csx") return;

                await Eval(scriptDir, e.Name);
            });

            var fileDeleted = new FileSystemEventHandler((sender, e) =>
            {
                IDisposable willDelete;

                if (!EvaluateScriptCollection.TryGetValue(e.Name, out willDelete)) return;
                Console.WriteLine($"Delete {e.Name}");
                EvaluateScriptCollection.Remove(e.Name);
                willDelete.Dispose();
            });

            fileChangeWatcher.Changed += fileCreatedOrChanged;
            fileChangeWatcher.Deleted += fileDeleted;
            fileChangeWatcher.Renamed += (sender, e) =>
            {
                if (e.Name.Split('.').Last() != "csx")
                {
                    var ev = new FileSystemEventArgs(e.ChangeType, "", e.OldName);
                    fileDeleted(sender, ev);
                }
                fileCreatedOrChanged(sender, new FileSystemEventArgs(WatcherChangeTypes.Created, "", e.Name));
            };

            Console.WriteLine("Read key, then finish");
            Console.ReadKey();
            fileChangeWatcher.EnableRaisingEvents = false;
            foreach (var v in EvaluateScriptCollection)
            {
                v.Value.Dispose();
            }
        }

        private async Task Eval(string path, string name)
        {
            var fullPath = Path.Combine(path, name);
            var code = await ReadCodeFile(fullPath);

            var evalResult = await CSharpCodeEvaluate(code);
            if (evalResult == null)
            {
                Console.WriteLine($"Evaluation Failed: {name}");
                return;
            }

            IDisposable old;
            var isUpdate = EvaluateScriptCollection.TryGetValue(name, out old);
            if (isUpdate)
            {
                EvaluateScriptCollection.Remove(name);
                old.Dispose();
            }
            Console.WriteLine($"{(isUpdate ? "Update" : "Add")} {name}");
            EvaluateScriptCollection.Add(name, evalResult);
        }

        private async Task<IDisposable> CSharpCodeEvaluate(string code)
        {
            try
            {
                return (IDisposable)await CSharpScript
                    .EvaluateAsync(code,
                                   ScriptOptions.Default
                                   .WithImports(DefaultImports)
                                   .WithReferences(new[]
                {
                    "System",
                    "System.Core",
                    "System.Xml",
                    "System.Xml.Linq",
                })
                                   .WithReferences(DefaultReferences),
                                   globals: new MSParams(Stream, Client),
                                   globalsType: typeof(MSParams));
            }
            catch (Exception ex) // Evaluate failed
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        private async Task<string> ReadCodeFile(string path)
        {
            using (var fs = new FileStream(path, FileMode.Open))
            {
                var codeByte = new byte[fs.Length];
                await fs.ReadAsync(codeByte, 0, codeByte.Length);
                return System.Text.Encoding.UTF8.GetString(codeByte, 0, codeByte.Length);
            }
        }
    }

    public class MSParams
    {
        public IObservable<IStreamEntity> MSStream { get; private set; }
        public MastodonClient Client { get; private set; }

        public MSParams(IObservable<IStreamEntity> stream, MastodonClient client)
        {
            MSStream = stream;
            Client = client;
        }
    }
}
