using ICSharpCode.SharpZipLib.Tar;
using System.IO.Compression;

namespace KmoniReproducer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var data =  GetDataFromKNETASCII();






            Console.WriteLine();
        }

        public static void OpenTarGz(string dir)
        {
            ConWrite("展開中...");
            while (true)
            {
                var targzFiles = Directory.EnumerateFiles(dir, "*.tar.gz", SearchOption.AllDirectories);
                if (!targzFiles.Any())
                    return;
                foreach (string targzFile in targzFiles)
                {
                    ConWrite(targzFile,ConsoleColor.Green);
                    using var tgzStream = File.OpenRead(targzFile);
                    using var gzStream = new GZipStream(tgzStream, CompressionMode.Decompress);
                    using var tarArchive = TarArchive.CreateInputTarArchive(gzStream, System.Text.Encoding.ASCII);
                    tarArchive.ExtractContents(targzFile.Replace(".tar.gz",""));
                }
                Thread.Sleep(100);//待たないと使用中
                foreach (string targzFile in targzFiles)
                    File.Delete(targzFile);
            }
        }

        public static Data? GetDataFromKNETASCII()
        {
            string dir = ConAsk("強震データがあるフォルダ名を入力してください。").Replace("\"", "");
            var targzFiles = Directory.EnumerateFiles(dir, "*.tar.gz", SearchOption.AllDirectories).ToArray();
            if (targzFiles.Length > 0)
            {
                bool ok = ConAsk(".tar.gzファイルが見つかりました。展開しますか？(y/n)") == "y";
                if (ok)
                    OpenTarGz(dir);
            }
            ConWrite($"{DateTime.Now:HH:MM:ss.ffff} 取得中...");
            var files = Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories).Where(f => f.EndsWith(".NS") || f.EndsWith(".EW") || f.EndsWith(".UD") || f.EndsWith(".NS2") || f.EndsWith(".EW2") || f.EndsWith(".UD2")).ToArray();
            if (files.Length == 0)
            {
                ConWrite("見つかりませんでした。");
                return null;
            }
            return new Data(files);
        }






        public static string ConAsk(string message, bool allowNull = false)
        {
            ConWrite(message);
        retry:
            string? ans = Console.ReadLine();
            if (allowNull)
                return ans ?? "";
            else if (string.IsNullOrEmpty(ans))
            {
                ConWrite("値を入力してください。" + message);
                goto retry;
            }
            else
                return ans;
        }



        /// <summary>
        /// コンソールのデフォルトの色
        /// </summary>
        public static readonly ConsoleColor defaultColor = Console.ForegroundColor;

        /// <summary>
        /// コンソールにデフォルトの色で出力します。
        /// </summary>
        /// <param name="text">出力するテキスト</param>
        /// <param name="withLine">改行するか</param>
        public static void ConWrite(string text, bool withLine = true)
        {
            ConWrite(text, defaultColor, withLine);
        }

        /// <summary>
        /// 例外のテキストを赤色で出力します。
        /// </summary>
        /// <param name="loc">場所([ConWrite]など)</param>
        /// <param name="ex">出力する例外</param>
        public static void ConWrite(string loc, Exception ex)
        {
            ConWrite(loc + ex.ToString(), ConsoleColor.Red);
        }

        /// <summary>
        /// コンソールに色付きで出力します。色は変わったままとなります。
        /// </summary>
        /// <param name="text">出力するテキスト</param>
        /// <param name="color">表示する色</param>
        /// <param name="withLine">改行するか</param>
        public static void ConWrite(string text, ConsoleColor color, bool withLine = true)
        {
            Console.ForegroundColor = color;
            //Console.Write(DateTime.Now.ToString("HH:mm:ss.ffff "));
            if (withLine)
                Console.WriteLine(text);
            else
                Console.Write(text);
        }
    }
}
