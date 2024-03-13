using ICSharpCode.SharpZipLib.Tar;
using MathNet.Numerics.IntegralTransforms;
using System.IO.Compression;
using System.Numerics;

namespace KmoniReproducer
{
    internal class Program
    {
        static void Main(string[] args)
        {




            var data = GetDataFromKNETASCII();
            Acc2JI(data);








            Console.WriteLine();
        }

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
                Thread.Sleep(100);//待たないと使用中
                foreach (var targzFile in targzFiles)
                    File.Delete(targzFile);
            }
        }

        public static Data? GetDataFromKNETASCII()
        {
            string dir = ConAsk("強震データがあるフォルダ名を入力してください。").Replace("\"", "");
            var targzFiles = Directory.EnumerateFiles(dir, "*.tar.gz", SearchOption.AllDirectories).ToArray();
            if (targzFiles.Length != 0)
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

        public static void Acc2JI(Data data)
        {

            DateTime startTime = data.OriginTime;
            DateTime endTime = startTime.AddSeconds(180);
            TimeSpan calSpan = TimeSpan.FromSeconds(60);

            for (DateTime drawTime = startTime; drawTime < endTime; drawTime += calSpan)
                foreach (var data1 in data.ObsDatas.Where(x => x.DataDir == "N-S"))
                {
                    int startIndex = Math.Max((int)((drawTime.AddMinutes(-1) + calSpan - startTime).TotalMilliseconds * data1.SamplingFreq / 1000), 0);
                    int endIndex = (int)((drawTime + calSpan - startTime).TotalMilliseconds * data1.SamplingFreq / 1000) - 1;
                    int count = endIndex - startIndex + 1;
                    //st 00:00:05  span 0.25  draw 00:00:15
                    //=>  00:00:05.25 <= data < 00:00:15.25  10.25sec (*max:60sec)
                    //dataCount=sec*freq=msec*freq/1000 (100Hz:max6000)

                    var data23 = data.ObsDatas.Where(x => x.StationCode == data1.StationCode).ToArray();
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

                    int nPow2 = 1;
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

                    for (int i = 0; i < nPow2; i++)
                    {
                        double f = (i + 1) / (double)nPow2 * data1.SamplingFreq;
                        double y = f * 0.1;
                        double fl = Math.Pow(1 - Math.Exp(-Math.Pow(f / 0.5, 3)), 0.5);
                        double fh = Math.Pow(1 + 0.694 * Math.Pow(y, 2) + 0.241 * Math.Pow(y, 4) + 0.0557 * Math.Pow(y, 6) + 0.009664 * Math.Pow(y, 8) + 0.00134 * Math.Pow(y, 10) + 0.000155 * Math.Pow(y, 12), -0.5);
                        double ff = Math.Pow(1 / f, 0.5);
                        double fa = fl * fh * ff;
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
                    int index03 = (int)Math.Floor(0.3 * data1.SamplingFreq) - 1;
                    double ji = Math.Floor(Math.Round(((2 * Math.Log(dataAcCR[index03], 10)) + 0.96) * 100, MidpointRounding.AwayFromZero) / 10) / 10;

                    ConWrite($"{data1.StationCode} {drawTime:HH:mm:ss.ff} : {ji}");


                    //return;
                }

            /*



            DateTime GetTime = StartTime;
            double dataSecond = 0;
            double[] AccX = new double[2048];
            double[] AccY = new double[2048];
            double[] AccZ = new double[2048];
            string newcsv = "\n";

            Console.WriteLine("データ取得中…");
            for (int i = 0; i < 2048;)
            {
                string[] data_ = File.ReadAllText($"{Directory}\\{GetTime.Year}\\{GetTime.Month}\\{GetTime.Day}\\{GetTime.Hour}\\{GetTime.Minute}\\{GetTime:yyyyMMddHHmmss}.txt").Split('\n');
                foreach (string data__ in data_)
                {
                    string[] data = data__.Split(',');
                    if (data.Length == 4)
                    {
                        newcsv += $"\n{data[0]},{data[1]},{data[2]}";
                        AccX[i] = double.Parse(data[0]);
                        AccY[i] = double.Parse(data[1]);
                        AccZ[i] = double.Parse(data[2]);
                        dataSecond += 1.0 / data_.Length;
                        i++;
                        if (i >= 2048)
                            break;
                    }
                }
                GetTime += new TimeSpan(0, 0, 1);
            }
            File.WriteAllText("output.csv", newcsv.Replace("\n\n", ""));

            Console.WriteLine("データ変換中…");
            Complex Acc = new Complex(AccX[0], 0);
            Complex[] AccXc = Array.ConvertAll(AccX, x => new Complex(x, 0));
            Complex[] AccYc = Array.ConvertAll(AccY, x => new Complex(x, 0));
            Complex[] AccZc = Array.ConvertAll(AccZ, x => new Complex(x, 0));

            //フーリエ変換
            Console.WriteLine("フーリエ変換中…");
            Fourier.Forward(AccXc);
            Fourier.Forward(AccYc);
            Fourier.Forward(AccZc);

            //フィルター
            Console.WriteLine("フィルター計算中…");
            for (int i = 0; i < 2048; i++)
            {
                double Hz = (i + 1) / dataSecond;
                double y = Hz * 0.1;
                //ローカットフィルター
                double FL = Math.Pow(1 - Math.Exp(-1 * Math.Pow(Hz / 0.5, 3)), 0.5);
                //ハイカットフィルター
                double FH = Math.Pow(1 + 0.694 * Math.Pow(y, 2) + 0.241 * Math.Pow(y, 4) + 0.0557 * Math.Pow(y, 6) + 0.009664 * Math.Pow(y, 8) + 0.00134 * Math.Pow(y, 10) + 0.000155 * Math.Pow(y, 12), -0.5);
                //周期効果フィルター
                double FF = Math.Pow(1 / Hz, 0.5);
                //フィルター合計
                double FA = FL * FH * FF;
                AccXc[i] *= FA;
                AccZc[i] *= FA;
                AccZc[i] *= FA;
            }

            //逆フーリエ変換
            Console.WriteLine("逆フーリエ変換中…");
            Fourier.Inverse(AccXc);
            Fourier.Inverse(AccYc);
            Fourier.Inverse(AccZc);

            Console.WriteLine("計算中…");
            double[] fdataX = new double[2048];
            double[] fdataY = new double[2048];
            double[] fdataZ = new double[2048];
            for (int i = 0; i < 2048; i++)
            {
                fdataX[i] = AccXc[i].Magnitude;
                fdataY[i] = AccYc[i].Magnitude;
                fdataZ[i] = AccZc[i].Magnitude;
            }
            double[] fdataA = fdataX.Zip(fdataY, (a, b) => Math.Sqrt(a * a + b * b)).Zip(fdataZ, (a, b) => Math.Sqrt(a * a + b * b)).ToArray();
            Array.Sort(fdataA);
            Array.Reverse(fdataA);
            int index = (int)Math.Floor(0.3 / dataSecond * 2048);
            double JI = Math.Round((2 * Math.Log(fdataA[index], 10)) + 0.96, 2, MidpointRounding.AwayFromZero);
            Console.WriteLine("計算終了");
            Console.WriteLine(JI);

            */




        }


        public static void Fill0(ref double[] srcArray, int nPow2)
        {
            var newArray = new double[nPow2];
            Array.Copy(srcArray, 0, newArray, 0, srcArray.Length);
            srcArray = newArray;
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
