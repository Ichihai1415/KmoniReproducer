﻿using ICSharpCode.SharpZipLib.Tar;
using KmoniReproducer.Properties;
using MathNet.Numerics.IntegralTransforms;
using System.Drawing;
using System.Drawing.Text;
using System.IO.Compression;
using System.Numerics;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using static KmoniReproducer.Data;
using static KmoniReproducer.DrawImg;

namespace KmoniReproducer
{
    [SupportedOSPlatform("windows")]
    internal class Program
    {
        /// <summary>
        /// AreaInformationPrefectureEarthquake_GIS_20190125_01
        /// </summary>
        public static JsonNode? map_pref_min;
        /// <summary>
        /// AreaInformationPrefectureEarthquake_GIS_20190125_1
        /// </summary>
        public static JsonNode? map_pref_mid;
        /// <summary>
        /// AreaForecastLocalE_GIS_20190125_01
        /// </summary>
        public static JsonNode? map_loca_min;
        /// <summary>
        /// AreaForecastLocalE_GIS_20190125_1
        /// </summary>
        public static JsonNode? map_loca_mid;
        /// <summary>
        /// AreaInformationCity_quake_GIS_20240229_01
        /// </summary>
        public static JsonNode? map_city_min;
        /// <summary>
        /// AreaInformationCity_quake_GIS_20240229_1
        /// </summary>
        public static JsonNode? map_city_mid;

        /// <summary>
        /// 描画用フォント
        /// </summary>
#pragma warning disable CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。
        public static FontFamily font;
#pragma warning restore CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。

        public static Config_Color config_color = new();
        public static Config_Map config_map = new();
        public static Config_Draw config_draw = new();

        public static JsonSerializerOptions serializeIntend = new() { WriteIndented = true };

        static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);//Encoding.GetEncoding("Shift-JIS")に必要
            if (!File.Exists("Koruri-Regular.ttf"))
                File.WriteAllBytes("Koruri-Regular.ttf", Resources.Koruri_Regular);
            if (!File.Exists("Koruri-LICENSE"))
                File.WriteAllBytes("Koruri-LICENSE", Resources.Koruri_LICENSE);
            PrivateFontCollection pfc = new();
            pfc.AddFontFile("Koruri-Regular.ttf");
            font = pfc.Families[0];
            serializeIntend.Converters.Add(new ColorConverter());

            if (File.Exists("config-color.json"))
                config_color = JsonSerializer.Deserialize<Config_Color>(File.ReadAllText("config-color.json"), serializeIntend) ?? new Config_Color();
            else
                File.WriteAllText("config-color.json", JsonSerializer.Serialize(config_color, serializeIntend));

            if (!Directory.Exists("mapdata"))
            {
                ConWrite("地図の描画には地図データが必要です。(https://github.com/Ichihai1415/KmoniReproducer/releases/download/mapdata/mapdata.zip)");
                ConWrite($"{DateTime.Now:HH:mm:ss.ffff} 地図データダウンロード中...", ConsoleColor.Blue);
                if (File.Exists("mapdata.zip"))
                    File.Delete("mapdata.zip");
                using (var client = new HttpClient())
                {
                    var res = client.GetAsync("https://github.com/Ichihai1415/KmoniReproducer/releases/download/mapdata/mapdata.zip").Result;
                    using var fileStream = new FileStream("mapdata.zip", FileMode.Create, FileAccess.Write, FileShare.None);
                    res.Content.ReadAsStream().CopyTo(fileStream);
                }
                ConWrite($"{DateTime.Now:HH:mm:ss.ffff} 地図データをダウンロードしました。", ConsoleColor.Blue);
                ZipFile.ExtractToDirectory("mapdata.zip", "mapdata");
                Thread.Sleep(100);
                File.Delete("mapdata.zip");
            }
            ConWrite($"{DateTime.Now:HH:mm:ss.ffff} 地図データ読み込み中...", ConsoleColor.Blue);
            try
            {
                map_pref_min = JsonNode.Parse(File.ReadAllText("mapdata\\AreaInformationPrefectureEarthquake_GIS_20190125_01.geojson"));
                map_pref_mid = JsonNode.Parse(File.ReadAllText("mapdata\\AreaInformationPrefectureEarthquake_GIS_20190125_1.geojson"));
                map_loca_min = JsonNode.Parse(File.ReadAllText("mapdata\\AreaForecastLocalE_GIS_20190125_01.geojson"));
                map_loca_mid = JsonNode.Parse(File.ReadAllText("mapdata\\AreaForecastLocalE_GIS_20190125_1.geojson"));
                map_city_min = JsonNode.Parse(File.ReadAllText("mapdata\\AreaInformationCity_quake_GIS_20240229_01.geojson"));
                map_city_mid = JsonNode.Parse(File.ReadAllText("mapdata\\AreaInformationCity_quake_GIS_20240229_1.geojson"));
            }
            catch (Exception ex)
            {
                ConWrite("[Main]地図データの読み込みに失敗しました。mapdataフォルダを削除して再起動してください。\n", ex);
                return;
            }
            ConWrite($"{DateTime.Now:HH:mm:ss.ffff} 地図データを読み込みました。", ConsoleColor.Blue);

            ConWrite("【お知らせ】特に設定中の中断機能やエラー対策はしていません。入力をやり直したい場合適当な文字を入れればエラーで最初に戻ります。" +
                "ソフトを再起動してもいいですが読み込んだデータ、計算済み震度等内部のデータが消えることに注意してください。");
            Data? data = null;
            Data_Draw? data_Draw = null;
            while (true)
                try
                {
                    var mode = ConAsk("モード(数字)を入力してください。\n" +
                        "> 1.加速度データ読み込み(新規/追加)\n" +
                        "> 2.震度計算\n" +
                        "> 3.震度データ(独自形式)出力\n" +
                        "> 4.震度データ(独自形式)読み込み\n" +
                        "> 5.描画\n" +
                        "> 8.tarファイルの展開\n" +
                        "> 9.データのクリア\n" +
                        "> 0.終了");
                    switch (mode)
                    {
                        case "1":
                            var dataSrc1 = ConAsk("データの機関(数字)を入力してください。\n" +
                                "> 1.K-NET,KiK-net(.NS/.EW/.UD/.NS2/.EW2/.UD2)\n" +
                                "> 2.気象庁(.csv)");
                            switch (dataSrc1)
                            {
                                case "1":
                                    if (data == null)
                                        data = GetDataFromKNETASCII();
                                    else
                                        data.AddObsDatas(GetDataFromKNETASCII());
                                    break;
                                case "2":
                                    if (data == null)
                                        data = GetDataFromJMAcsv();
                                    else
                                        data.AddObsDatas(GetDataFromJMAcsv());
                                    break;
                                default:
                                    ConWrite("値が不正です。", ConsoleColor.Red);
                                    break;
                            }
                            break;
                        case "2":
                            if (data == null)
                            {
                                ConWrite("先に加速度データを読み込んでください。", ConsoleColor.Red);
                                break;
                            }
                            data_Draw = new Data_Draw(data);

                            var startT2 = DateTime.Parse(ConAsk($"計算開始日時を入力してください。発生日時は {data.OriginTime} となっています。例:2024/01/01 00:00:00"));
                            Acc2JI(data, ref data_Draw,
                                startT2,
                                startT2.AddSeconds(double.Parse(ConAsk($"計算開始日時から終了までの時間(秒)を入力してください。例:300"))),
                                TimeSpan.FromSeconds(double.Parse(ConAsk($"計算間隔(秒、小数可)を入力してください。例1:1 例2:0.5"))),
                                TimeSpan.FromSeconds(double.Parse(ConAsk($"計算秒数(秒、小数可)を入力してください。基本は1分です。リアルタイムでの揺れの再現や速く処理したい場合短くしてください。※短いほど実際の震度とずれが生まれます。例:60"))));
                            break;
                        case "3":
                            if (data_Draw == null)
                            {
                                ConWrite("先に震度を計算してください。", ConsoleColor.Red);
                                break;
                            }
                            if (data_Draw.Datas_Draw.Count == 0)
                            {
                                ConWrite("先に震度を計算してください。", ConsoleColor.Red);
                                break;
                            }
                            var dir_out = $"output\\shindo\\{data_Draw.OriginTime:yyyyMMddHHmmss}-{data_Draw.Datas_Draw.Count}-{data_Draw.CalPeriod.TotalSeconds}s";
                            Directory.CreateDirectory(dir_out);
                            File.WriteAllText($"{dir_out}\\_param.json", $"{{\"OriginTime\":\"{data_Draw.OriginTime}\",\"HypoLat\":{data_Draw.HypoLat},\"HypoLon\":{data_Draw.HypoLon},\"CalPeriod\":\"{data_Draw.CalPeriod}\"}}");
                            foreach (var obsData in data_Draw.Datas_Draw)
                                File.WriteAllText($"{dir_out}\\{obsData.Key}.json", JsonSerializer.Serialize(obsData.Value));
                            ConWrite($"{dir_out} に出力しました。", ConsoleColor.Green);
                            break;
                        case "4":
                            var dir_in = ConAsk("出力したフォルダを入力してください。").Replace("\"", "");
                            ConWrite($"{DateTime.Now:HH:mm:ss.ffff} 読み込み中...", ConsoleColor.Blue);
                            var files = Directory.EnumerateFiles(dir_in, "*.json").Where(x => !x.EndsWith("_param.json"));
                            var paramNode = JsonNode.Parse(File.ReadAllText($"{dir_in}\\_param.json"));
#pragma warning disable CS8602 // null 参照の可能性があるものの逆参照です。
#pragma warning disable CS8619 // 値における参照型の Null 許容性が、対象の型と一致しません。
                            data_Draw = new Data_Draw(DateTime.Parse(paramNode["OriginTime"].ToString()), double.Parse(paramNode["HypoLat"].ToString()), double.Parse(paramNode["HypoLon"].ToString()), TimeSpan.Parse((string?)paramNode["CalPeriod"] ?? "00:00:00"))
                            {
                                Datas_Draw = files.Select(x => JsonSerializer.Deserialize<Data_Draw.ObsDataD>(File.ReadAllText(x))).Where(x => x != null).ToDictionary(x => x.StationName, y => y)
                            };
#pragma warning restore CS8619 // 値における参照型の Null 許容性が、対象の型と一致しません。
#pragma warning restore CS8602 // null 参照の可能性があるものの逆参照です。
                            ConWrite($"{DateTime.Now:HH:mm:ss.ffff} 読み込み完了", ConsoleColor.Blue);
                            break;
                        case "5":
                            if (data_Draw == null)
                            {
                                ConWrite("先に震度を計算してください。", ConsoleColor.Red);
                                break;
                            }
                            if (data_Draw.Datas_Draw.Count == 0)
                            {
                                ConWrite("先に震度を計算してください。", ConsoleColor.Red);
                                break;
                            }
                            var calSpanCk = data_Draw.Datas_Draw.Values.First().TimeInt.Keys.ToArray();
                            var calSpan = calSpanCk[1] - calSpanCk[0];

                            config_map = new Config_Map
                            {
                                MapSize = int.Parse(ConAsk("マップサイズ(画像の高さ)を入力してください。幅は16:9になるように計算されます。例:1080")),
                                LatSta = double.Parse(ConAsk("緯度の始点(地図の下端)を入力してください。例:22.5")),
                                LatEnd = double.Parse(ConAsk("緯度の終点(地図の上端)を入力してください。例:47.5")),
                                LonSta = double.Parse(ConAsk("経度の始点(地図の左端)を入力してください。例:122.5")),
                                LonEnd = double.Parse(ConAsk("経度の終点(地図の右端)を入力してください。例:147.5")),
                                MapType = (Config_Map.MapKind)int.Parse(ConAsk("マップの種類(数字)を入力してください。例:11\n" +
                                "> 11.地震情報／都道府県等(軽量)\n" +
                                "> 12.地震情報／都道府県等(詳細)\n" +
                                "> 21.地震情報／細分区域(軽量)\n" +
                                "> 22.地震情報／細分区域(詳細)\n" +
                                "> 31.市町村等（地震津波関係）(軽量)\n" +
                                "> 32.市町村等（地震津波関係）(詳細)"))
                            };
                            var startT5 = DateTime.Parse(ConAsk($"描画開始日時を入力してください。発生日時は {data_Draw.OriginTime} となっています。例:2024/01/01 00:00:00"));
                            config_draw = new Config_Draw
                            {
                                StartTime = startT5,
                                EndTime = startT5.AddSeconds(double.Parse(ConAsk($"描画開始日時から終了までの時間(秒)を入力してください。例:300"))),
                                DrawSpan = TimeSpan.FromSeconds(double.Parse(ConAsk($"描画間隔(秒、小数可)を入力してください。震度計算では {calSpan.TotalSeconds} のようです。例1:1 例2:0.5"))),
                                ObsSize = int.Parse(ConAsk("観測点サイズを入力してください。例:7")),
                                DrawObsName = ConAsk("観測点円の右に観測点名を表示する場合 y と入力してください。※地図を拡大しない場合非推奨です。", true) == "y",
                                DrawObsShindo = ConAsk("観測点円の右に観測点震度を表示する場合 y と入力してください。※地図を拡大しない場合非推奨です。", true) == "y"
                            };

                            if (!File.Exists("config-color.json"))
                                File.WriteAllText("config-color.json", JsonSerializer.Serialize(config_color/*, serializeIntend*/));
                            _ = ConAsk("色をconfig-color.jsonで設定してください。エンターキーを押すと描画を開始します。", true);
                            config_color = JsonSerializer.Deserialize<Config_Color>(File.ReadAllText("config-color.json"), serializeIntend) ?? new Config_Color();

                            Draw(data_Draw);
                            ConWrite($"{DateTime.Now:HH:mm:ss.ffff} 画像出力完了\n動画化(画像ファイルがあるフォルダで、ffmpeg.exeのパスが通っている場合): \n" +
                                "1fps: ffmpeg -framerate 1 -i %04d.png -vcodec libx264 -pix_fmt yuv420p -r 1 _output_1.mp4\n" +
                                "3fps: ffmpeg -framerate 3 -i %04d.png -vcodec libx264 -pix_fmt yuv420p -r 3 _output_3.mp4\n" +
                                "5fps: ffmpeg -framerate 5 -i %04d.png -vcodec libx264 -pix_fmt yuv420p -r 5 _output_5.mp4\n" +
                                "10fps: ffmpeg -framerate 10 -i %04d.png -vcodec libx264 -pix_fmt yuv420p -r 10 _output_10.mp4\n" +
                                "30fps: ffmpeg -framerate 30 -i %04d.png -vcodec libx264 -pix_fmt yuv420p -r 30 _output_30.mp4\n" +
                                "60fps: ffmpeg -framerate 60 -i %04d.png -vcodec libx264 -pix_fmt yuv420p -r 60 _output_60.mp4", ConsoleColor.Blue);
                            break;
                        case "8":
                            OpenTar(ConAsk("展開するtarファイルのパスを入力してください。").Replace("\"", ""));
                            break;
                        case "9":
                            data = null;
                            data_Draw = null;
                            break;
                        case "0":
                            Console.ForegroundColor = ConsoleColor.Gray;
                            Environment.Exit(0);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    ConWrite("[Main]", ex);
                }

            //var data2 = GetDataFromJMAcsv();
            //var drawData = new Data_Draw();
            //Acc2JI(data2, ref drawData);

            /*

            var data = GetDataFromKNETASCII();
            // var data2 = GetDataFromJMAcsv();
            //data.AddObsDatas(data2);

            var drawData = new Data_Draw();
            Acc2JI(data, ref drawData);
            */

            Console.WriteLine();
        }

        /// <summary>
        /// K-NET ASCII形式のデータをDataに格納します。
        /// </summary>
        /// <returns>読み込んだデータ</returns>
        public static Data? GetDataFromKNETASCII()
        {
            var dir = ConAsk("K-NET,KiK-net(地表のみ)の強震データ(.NS/.EW/.UD/.NS2/.EW2/.UD2)があるフォルダ名を入力してください。.tar.gzがある場合後で展開の確認が出ます。").Replace("\"", "");
            var targzFiles = Directory.EnumerateFiles(dir, "*.tar.gz", SearchOption.AllDirectories).ToArray();
            if (targzFiles.Length != 0)
            {
                var ok = ConAsk(".tar.gzファイルが見つかりました。展開しますか？(y/n)", true) == "y";
                if (ok)
                    OpenTarGz(dir);
            }
            ConWrite($"{DateTime.Now:HH:mm:ss.ffff} 取得中...", ConsoleColor.Blue);
            var files = Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories).Where(f => f.EndsWith(".NS") || f.EndsWith(".EW") || f.EndsWith(".UD") || f.EndsWith(".NS2") || f.EndsWith(".EW2") || f.EndsWith(".UD2")).ToArray();
            if (files.Length == 0)
            {
                ConWrite("見つかりませんでした。", ConsoleColor.Red);
                return null;
            }
            return KNET_ASCII2Data(files);
        }

        /// <summary>
        /// 気象庁加速度データをDataに格納します。
        /// </summary>
        /// <returns>読み込んだデータ</returns>
        public static Data? GetDataFromJMAcsv()
        {
            var dir = ConAsk("気象庁の強震データ(.csv)があるフォルダ名を入力してください。指定したフォルダのすべてのcsvファイルを読み込みます。").Replace("\"", "");
            ConWrite($"{DateTime.Now:HH:mm:ss.ffff} 取得中...", ConsoleColor.Blue);
            var files = Directory.EnumerateFiles(dir, "*.csv", SearchOption.AllDirectories).ToArray();
            if (files.Length == 0)
            {
                ConWrite("見つかりませんでした。", ConsoleColor.Red);
                return null;
            }
            return JMAcsv2Data(files);//.Where(x => !x.EndsWith("level.csv")).ToArray()
        }

        /// <summary>
        /// .tarファイルを展開します。
        /// </summary>
        /// <param name="file">tarファイルのパス</param>
        public static void OpenTar(string file)
        {
            ConWrite($"{DateTime.Now:HH:mm:ss.ffff} 展開中...", ConsoleColor.Blue);
            var dir = file.Replace(".tar", "");
            using (var fStream = File.OpenRead(file))
            using (var tarArchive = TarArchive.CreateInputTarArchive(fStream, Encoding.ASCII))
                tarArchive.ExtractContents(dir);
            Thread.Sleep(100);//待たないと使用中例外
            File.Delete(file);
            ConWrite($"{DateTime.Now:HH:mm:ss.ffff} 展開完了", ConsoleColor.Blue);
            var targzFiles = Directory.EnumerateFiles(dir, "*.tar.gz", SearchOption.AllDirectories).ToArray();
            if (targzFiles.Length != 0)
            {
                var ok = ConAsk(".tar.gzファイルが見つかりました。展開しますか？(y/n)", true) == "y";
                if (ok)
                    OpenTarGz(dir);
            }
        }

        /// <summary>
        /// .tar.gzファイルを展開します。
        /// </summary>
        /// <remarks>展開後元のファイルは削除されます。展開後に.tar.gzファイルがある場合再実行します。</remarks>
        /// <param name="file">ファイルがあるディレクトリ</param>
        public static void OpenTarGz(string dir)
        {
            var targzFiles = Directory.EnumerateFiles(dir, "*.tar.gz", SearchOption.AllDirectories);
            if (!targzFiles.Any())
            {
                ConWrite("見つかりませんでした。", ConsoleColor.Red);
                return;
            }
            ConWrite($"{DateTime.Now:HH:mm:ss.ffff} 展開中...", ConsoleColor.Blue);
            while (targzFiles.Any())
            {
                foreach (var targzFile in targzFiles)
                {
                    ConWrite(targzFile, ConsoleColor.Green);
                    using var fStream = File.OpenRead(targzFile);
                    using var gzStream = new GZipStream(fStream, CompressionMode.Decompress);
                    using var tarArchive = TarArchive.CreateInputTarArchive(gzStream, Encoding.ASCII);
                    tarArchive.ExtractContents(targzFile.Replace(".tar.gz", ""));
                }
                Thread.Sleep(100);//待たないと使用中例外
                ConWrite($"{DateTime.Now:HH:mm:ss.ffff} 削除中...", ConsoleColor.Blue);
                foreach (var targzFile in targzFiles)
                    File.Delete(targzFile);
                Thread.Sleep(100);
                targzFiles = Directory.EnumerateFiles(dir, "*.tar.gz", SearchOption.AllDirectories);
            }
            ConWrite($"{DateTime.Now:HH:mm:ss.ffff} 展開完了", ConsoleColor.Blue);
        }

        /// <summary>
        /// 加速度から震度を求めます。<paramref name="drawData"/>内部に保存されます。
        /// </summary>
        /// <remarks>開始時刻:発生時刻　終了時刻:発生時刻+3分　描画間隔:1秒</remarks>
        /// <param name="data">加速度データ</param>
        /// <param name="drawData">描画用データ(ref)</param>
        public static void Acc2JI(Data data, ref Data_Draw drawData)
        {
            Acc2JI(data, ref drawData, data.OriginTime, data.OriginTime.AddMinutes(3), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(60));
        }

        /// <summary>
        /// 加速度から震度を求めます。<paramref name="drawData"/>内部に保存されます。
        /// </summary>
        /// <param name="data">加速度データ</param>
        /// <param name="drawData">描画用データ(ref)</param>
        /// <param name="startTime">開始時刻</param>
        /// <param name="endTime">終了時刻</param>
        /// <param name="calSpan">計算間隔</param>
        /// <param name="calPeriod">計算時間(通常1分)</param>
        public static void Acc2JI(Data data, ref Data_Draw drawData, DateTime startTime, DateTime endTime, TimeSpan calSpan, TimeSpan calPeriod)
        {
            if (data.ObsDatas == null)
            {
                ConWrite("加速度データが存在しません。", ConsoleColor.Red);
                return;
            }

            ConWrite($"{DateTime.Now:HH:mm:ss.ffff} 震度計算中...", ConsoleColor.Blue);
            ConWrite($"{startTime:yyyy/MM/dd  HH:mm:ss.ff} ~ {endTime:HH:mm:ss.ff}  span:{calSpan:mm\\:ss\\.ff}   dataCount:{data.ObsDatas.Length / 3}", ConsoleColor.Green);
            drawData.CalPeriod = calPeriod;
            var nowP = 0;
            var total = (endTime - startTime) / calSpan;
            var total2 = (double)data.ObsDatas.Length / 3;
            var calStartT = DateTime.Now;
            var calStartT2 = DateTime.Now;
            for (var drawTime = startTime; drawTime < endTime; drawTime += calSpan)
            {
                nowP++;
                if (nowP % 10 == 0)
                    GC.Collect();
                var eta1 = (DateTime.Now - calStartT2) * (total - nowP);
                var eta2 = (DateTime.Now - calStartT) * (total / nowP) - (DateTime.Now - calStartT);
                if (eta1 > eta2)
                    (eta1, eta2) = (eta2, eta1);
                var text1 = $"\r now:{drawTime:HH:mm:ss.ff} -> {nowP}/{total} ({nowP / total * 100:F2}％) ";
                var text2 = $"  eta:{(int)eta1.TotalMinutes}:{eta1:ss\\.ff}~{(int)eta2.TotalMinutes}:{eta2:ss\\.ff} (last cal:{(DateTime.Now - calStartT2).TotalMilliseconds}ms) ...";
                calStartT2 = DateTime.Now;
                var nowP2 = 0;
                foreach (var data1 in data.ObsDatas.Where(x => x.DataDir == "N-S"))
                {
                    nowP2++;
                    ConWrite(text1 + $"[data:{Math.Min(nowP2 / total2 * 100, 99.99):00.00}% of {total2}]" + text2, ConsoleColor.Green, false);

                    var startIndex = Math.Max((int)((drawTime - calPeriod + calSpan - data1.RecordTime).TotalMilliseconds * data1.SamplingFreq / 1000), 0);
                    var endIndex = (int)((drawTime + calSpan - data1.RecordTime).TotalMilliseconds * data1.SamplingFreq / 1000) - 1;
                    var count = endIndex - startIndex + 1;
                    //st 00:00:05  span 0.25  draw 00:00:15
                    //=>  00:00:05.25 <= data < 00:00:15.25  10.25sec (*max:60sec)
                    //dataCount=sec*freq=msec*freq/1000 (100Hz:max6000)

                    var data23 = data.ObsDatas.Where(x => x.StationName == data1.StationName).ToArray();
                    var data1Ac = data1.Accs.Skip(startIndex).Take(count).ToArray();
                    var data2Ac = data23[1].Accs.Skip(startIndex).Take(count).ToArray();
                    var data3Ac = data23[2].Accs.Skip(startIndex).Take(count).ToArray();
                    if (data1Ac.Length < 0.3 * data1.SamplingFreq)
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
            ConWrite($"\n{DateTime.Now:HH:mm:ss.ffff} 震度計算完了", ConsoleColor.Blue);
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
        /// <returns>入力された文字列</returns>
        public static string ConAsk(string message, bool allowNull = false)
        {
            ConWrite(message);
        retry:
            Console.ForegroundColor = ConsoleColor.Yellow;
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
        /// コンソールの色を既定色に変えます。
        /// </summary>
        public static void ConWrite()
        {
            Console.ForegroundColor = defaultColor;
        }

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
        public static void ConWrite(string? loc, Exception ex)
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
            _ = defaultColor;//最初に色指定するとその色が既定色になるため
            Console.ForegroundColor = color;
            //Console.Write(DateTime.Now.ToString("HH:mm:ss.ffff "));//タイムスタンプが不要な場合コメントアウト(使うときは`$"{DateTime.Now:HH:mm:ss.ffff} "`)
            if (withLine)
                Console.WriteLine(text);
            else
                Console.Write(text);
        }
    }
}
