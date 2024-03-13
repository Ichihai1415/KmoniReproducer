using ICSharpCode.SharpZipLib.Tar;
using MathNet.Numerics.IntegralTransforms;
using System.IO.Compression;
using System.Numerics;
using static KmoniReproducer.Data;

namespace KmoniReproducer
{
    internal class Program
    {
        static void Main(string[] args)
        {




            var data = GetDataFromKNETASCII();
            var drawData = new Data_Draw();
            Acc2JI(data, ref drawData);







            Console.WriteLine();
        }

        /// <summary>
        /// .tar.gzファイルを展開します。
        /// </summary>
        /// <remarks>展開後元のファイルは削除されます。展開後に.tar.gzファイルがある場合再実行します。</remarks>
        /// <param name="dir">ファイルがあるディレクトリ</param>
        public static void OpenTarGz(string dir)
        {
            ConWrite("展開中...");
            while (true)
            {
                var targzFiles = Directory.EnumerateFiles(dir, "*.tar.gz", SearchOption.AllDirectories);
                if (targzFiles.Any())
                    return;
                foreach (var targzFile in targzFiles)
                {
                    ConWrite(targzFile, ConsoleColor.Green);
                    using var tgzStream = File.OpenRead(targzFile);
                    using var gzStream = new GZipStream(tgzStream, CompressionMode.Decompress);
                    using var tarArchive = TarArchive.CreateInputTarArchive(gzStream, System.Text.Encoding.ASCII);
                    tarArchive.ExtractContents(targzFile.Replace(".tar.gz", ""));
                }
                Thread.Sleep(100);//待たないと使用中例外
                foreach (var targzFile in targzFiles)
                    File.Delete(targzFile);
            }
        }

        /// <summary>
        /// K-NET ASCII形式のデータをDataに格納します。
        /// </summary>
        /// <returns>読み込んだデータ</returns>
        public static Data? GetDataFromKNETASCII()
        {
            var dir = ConAsk("強震データがあるフォルダ名を入力してください。").Replace("\"", "");
            var targzFiles = Directory.EnumerateFiles(dir, "*.tar.gz", SearchOption.AllDirectories).ToArray();
            if (targzFiles.Length != 0)
            {
                var ok = ConAsk(".tar.gzファイルが見つかりました。展開しますか？(y/n)") == "y";
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

            return KNET_ASCII2Data(files);
        }

        /// <summary>
        /// 加速度から震度を求めます。<c>Data</c>内部に保存されます。
        /// </summary>
        /// <remarks>開始時刻:発生時刻　終了時刻:発生時刻+3分　描画間隔:1秒</remarks>
        /// <param name="data">加速度データ</param>
        /// <param name="drawData">描画用データ(ref)</param>
        public static void Acc2JI(Data data, ref Data_Draw drawData)
        {
            Acc2JI(data, ref drawData, data.OriginTime, data.OriginTime.AddMinutes(3), TimeSpan.FromSeconds(1));
        }

        /// <summary>
        /// 加速度から震度を求めます。<c>Data</c>内部に保存されます。
        /// </summary>
        /// <param name="data">加速度データ</param>
        /// <param name="drawData">描画用データ(ref)</param>
        /// <param name="startTime">開始時刻</param>
        /// <param name="endTime">終了時刻</param>
        /// <param name="calSpan">描画間隔</param>
        public static void Acc2JI(Data data, ref Data_Draw drawData, DateTime startTime, DateTime endTime, TimeSpan calSpan)
        {
            for (var drawTime = startTime; drawTime < endTime; drawTime += calSpan)
                foreach (var data1 in data.ObsDatas.Where(x => x.DataDir == "N-S"))
                {
                    var startIndex = Math.Max((int)((drawTime.AddMinutes(-1) + calSpan - startTime).TotalMilliseconds * data1.SamplingFreq / 1000), 0);
                    var endIndex = (int)((drawTime + calSpan - startTime).TotalMilliseconds * data1.SamplingFreq / 1000) - 1;
                    var count = endIndex - startIndex + 1;
                    //st 00:00:05  span 0.25  draw 00:00:15
                    //=>  00:00:05.25 <= data < 00:00:15.25  10.25sec (*max:60sec)
                    //dataCount=sec*freq=msec*freq/1000 (100Hz:max6000)

                    var data23 = data.ObsDatas.Where(x => x.StationName == data1.StationName).ToArray();
                    var data1Ac = data1.Accs.Skip(startIndex).Take(count).ToArray();
                    var data2Ac = data23[1].Accs.Skip(startIndex).Take(count).ToArray();
                    var data3Ac = data23[2].Accs.Skip(startIndex).Take(count).ToArray();
                    if (data1Ac.Length == 0)
                        continue;
                    //File.WriteAllText("data1Ac-all.txt", string.Join('\n', data1.Accs));
                    data1Ac = data1Ac.Select(rawAcc => rawAcc - data1Ac.Average()).ToArray();
                    data2Ac = data2Ac.Select(rawAcc => rawAcc - data2Ac.Average()).ToArray();
                    data3Ac = data3Ac.Select(rawAcc => rawAcc - data3Ac.Average()).ToArray();
                    //File.WriteAllText("data1Ac.txt", string.Join('\n', data1Ac));

                    var nPow2 = 1;
                    while (nPow2 < count)
                        nPow2 *= 2;
                    Fill0(ref data1Ac, nPow2);
                    Fill0(ref data2Ac, nPow2);
                    Fill0(ref data3Ac, nPow2);
                    var data1AcC = data1Ac.Select(x => new Complex(x, 0)).ToArray();
                    var data2AcC = data1Ac.Select(x => new Complex(x, 0)).ToArray();
                    var data3AcC = data1Ac.Select(x => new Complex(x, 0)).ToArray();
                    //File.WriteAllText("data1AcC.txt", string.Join('\n', data1AcC.Select(x => $"{x.Real},{x.Imaginary}")));

                    Fourier.Forward(data1AcC);
                    Fourier.Forward(data2AcC);
                    Fourier.Forward(data3AcC);
                    //File.WriteAllText("data1AcC-fou.txt", string.Join('\n', data1AcC.Select(x => $"{x.Real},{x.Imaginary}")));

                    for (var i = 0; i < nPow2; i++)
                    {
                        var f = (i + 1) / (double)nPow2 * data1.SamplingFreq;
                        var y = f * 0.1;
                        var fl = Math.Pow(1 - Math.Exp(-Math.Pow(f / 0.5, 3)), 0.5);
                        var fh = Math.Pow(1 + (0.694 * Math.Pow(y, 2)) + (0.241 * Math.Pow(y, 4)) + (0.0557 * Math.Pow(y, 6)) + (0.009664 * Math.Pow(y, 8)) + (0.00134 * Math.Pow(y, 10)) + (0.000155 * Math.Pow(y, 12)), -0.5);
                        var ff = Math.Pow(1 / f, 0.5);
                        var fa = fl * fh * ff;
                        data1AcC[i] *= fa;
                        data2AcC[i] *= fa;
                        data3AcC[i] *= fa;
                    }

                    Fourier.Inverse(data1AcC);
                    Fourier.Inverse(data2AcC);
                    Fourier.Inverse(data3AcC);
                    //File.WriteAllText("data1AcC-fou-inv.txt", string.Join('\n', data1AcC.Select(x => $"{x.Real},{x.Imaginary}")));

                    var dataAcCR = data1AcC.Select((data1AcC, i) => Math.Sqrt(data1AcC.Magnitude * data1AcC.Magnitude + data2AcC[i].Magnitude * data2AcC[i].Magnitude + data3AcC[i].Magnitude * data3AcC[i].Magnitude)).ToArray();
                    //File.WriteAllText("dataAcCR.txt", string.Join('\n', dataAcCR));

                    Array.Sort(dataAcCR);
                    Array.Reverse(dataAcCR);
                    //File.WriteAllText("dataAcCR-sort.txt", string.Join('\n', dataAcCR));
                    var index03 = (int)Math.Floor(0.3 * data1.SamplingFreq) - 1;
                    var ji = Math.Floor(Math.Round(((2 * Math.Log(dataAcCR[index03], 10)) + 0.96) * 100, MidpointRounding.AwayFromZero) / 10) / 10;

                    drawData.AddInt(data1, drawTime, ji);
                    //ConWrite($"{data1.StationName} {drawTime:HH:mm:ss.ff} : {ji}", ConsoleColor.Cyan);
                    //return;
                }
        }

        /// <summary>
        /// double配列を0埋めします。
        /// </summary>
        /// <param name="srcArray">元の配列(ref)</param>
        /// <param name="length">埋めるサイズ</param>
        public static void Fill0(ref double[] srcArray, int length)
        {
            var newArray = new double[length];
            Array.Copy(srcArray, 0, newArray, 0, srcArray.Length);
            srcArray = newArray;
        }

        /// <summary>
        /// コンソールで入力を求めます。
        /// </summary>
        /// <param name="message">表示するメッセージ</param>
        /// <param name="allowNull">空文字入力を許容するか</param>
        /// <returns></returns>
        public static string ConAsk(string message, bool allowNull = false)
        {
            ConWrite(message);
        retry:
            var ans = Console.ReadLine();
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
