using ICSharpCode.SharpZipLib.Tar;
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

        static void Main(/*string[] args*/)//todo:加速度データ格納を1Data_AccにNS,EW,UD全部入れるように
        {
            ConWrite("\n" +
                "  ////////////////////////////////////////////////////////\n" +
                "  //                                                    //\n" +
                "  //  KmoniReproducer v1.0.1                            //\n" +
                "  //    https://github.com/Ichihai1415/KmoniReproducer  //\n" +
                "  //                                                    //\n" +
                "  ////////////////////////////////////////////////////////\n" +
                "");
            ConWrite($"{DateTime.Now:HH:mm:ss.ffff} 初期化中...", ConsoleColor.Blue);
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);//Encoding.GetEncoding("Shift-JIS")に必要
            if (!File.Exists("Koruri-Regular.ttf"))
                File.WriteAllBytes("Koruri-Regular.ttf", Resources.Koruri_Regular);
            if (!File.Exists("Koruri-LICENSE"))
                File.WriteAllBytes("Koruri-LICENSE", Resources.Koruri_LICENSE);
            PrivateFontCollection pfc = new();
            pfc.AddFontFile("Koruri-Regular.ttf");
            font = pfc.Families[0];
            serializeIntend.Converters.Add(new ColorConverter());

            Datas.KNETKiKnetObsPoints = Datas.KNETKiKnetObsPoints_PrefName.ToDictionary(k => k.Key, v => v.Value.Split(' ')[1]);

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
            ConWrite($"RAM:{GC.GetTotalMemory(true) / 1024d / 1024d:F2}MB", ConsoleColor.Green);

            ConWrite("【注意/お知らせ】README(https://github.com/Ichihai1415/KmoniReproducer/blob/release/KmoniReproducer/README.md)、Wiki(https://github.com/Ichihai1415/KmoniReproducer/wiki)を確認してください。\n特に設定中の中断機能やエラー対策はしていません。入力をやり直したい場合適当な文字を入れればエラーで最初に戻ります。" +
                "ソフトを再起動してもいいですが読み込んだデータ、計算済み震度等内部のデータが消えることに注意してください。\n" +
                "また、入力要求時に例や推測される値を示す場合があります。何も入力しなかった場合推測される値があればその値(緯度経度や値が未設定の場合は除く)、それ以外は例の値が自動入力されます(例が複数あるものは1つ目のもの)。\n", ConsoleColor.Yellow);
            if (!Directory.Exists("output"))
            {
                ConWrite("上記内容を確認してください。何かキーを押すと続行します。");
                Console.ReadKey();
            }
            if (Console.WindowWidth < 110)
            {
                ConWrite(new string('-', 110));
                ConWrite($"コンソールの幅が小さく、処理時一部の表示が崩れる可能性があります。↑↓の-が改行されない(1行に収まる)ようにしてください。", ConsoleColor.DarkYellow);
                ConWrite(new string('-', 110));
                if (ConAsk("サイズの変更を試行しますか？(y/n) Windows11の新しいコンソールでは非対応です。実行すると表示がおかしくなることがあります。", true, "n") == "y")
                    Console.WindowWidth = 110;
            }
            Data? data = null;
            Data_Draw? data_Draw = null;

#if false

            var files_debug = Directory.EnumerateFiles(@"C:\Ichihai1415\extract\20240101161000-a", "*", SearchOption.AllDirectories).Where(f => f.EndsWith(".NS") || f.EndsWith(".EW") || f.EndsWith(".UD") || f.EndsWith(".NS2") || f.EndsWith(".EW2") || f.EndsWith(".UD2")).ToArray();
            var data_debug = KNET_ASCII2Data(files_debug);
            Acc2JI(data_debug, out Data_Draw? drawData_debug, new DateTime(2024, 1, 1, 16, 10, 0), new DateTime(2024, 1, 1, 16, 14, 0), new TimeSpan(0, 0, 1), new TimeSpan(0, 1, 0));
            return;

#elif false
            var files_debug = Directory.EnumerateFiles(@"D:\Ichihai1415\data\kyoshin\raw\20110311144600", "*", SearchOption.AllDirectories).Where(f => f.EndsWith(".NS") || f.EndsWith(".EW") || f.EndsWith(".UD") || f.EndsWith(".NS2") || f.EndsWith(".EW2") || f.EndsWith(".UD2")).ToArray();
            var data_debug = KNET_ASCII2Data(files_debug);
            Acc2JI(data_debug, out Data_Draw? drawData_debug, new DateTime(2011, 3, 11, 14, 46, 0), new DateTime(2011, 03, 11, 14, 56, 0), new TimeSpan(0, 0, 0, 0, 100), new TimeSpan(0, 1, 0));
            return;
#endif

            while (true)
                try
                {//todo:1x:震度計算関連 2x(21,22):震度データ関連のように 12,13で加速度データを独自形式出力入力追加
                    var mode = ConAsk("\nモード(数字)を入力してください。\n" +
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
                                        data.AddObsDatas(GetDataFromKNETASCII(), true);
                                    break;
                                case "2":
                                    if (data == null)
                                        data = GetDataFromJMAcsv();
                                    else
                                        data.AddObsDatas(GetDataFromJMAcsv(), false);
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
                            var autoSave = ConAsk("計算後自動で保存しますか？(y/n) 既定:y", true, "y") == "y";
                            var startT2 = DateTime.Parse(ConAsk($"計算開始日時を入力してください。発生日時は {(data.OriginTime == DateTime.MinValue ? "----/--/-- --:--:--" : data.OriginTime)} となっています。例:2024/01/01 00:00:00", data.OriginTime != DateTime.MinValue, data.OriginTime.ToString()));
                            Acc2JI(data, out var data_Draw_tmp,
                                startT2,
                                startT2.AddSeconds(int.Parse(ConAsk($"計算開始日時から終了までの時間(秒)を入力してください。例:300", true, "300"))),
                                TimeSpan.FromSeconds(double.Parse(ConAsk($"計算間隔(秒、小数可)を入力してください。例1:1 例2:0.5", true, "1"))),
                                TimeSpan.FromSeconds(double.Parse(ConAsk($"計算秒数(秒、小数可)を入力してください。基本は1分ですが時間がかかります。リアルタイムでの揺れの再現や速く処理したい場合短くしてください。※短いほど実際の震度とずれが生まれます。例:60", true, "60"))));
                            data_Draw = data_Draw_tmp;
                            if (autoSave && data_Draw_tmp != null)//計算失敗時もここ来るため
                                ShindoSave(data_Draw);
                            break;
                        case "3":
                            ShindoSave(data_Draw);
                            break;
                        case "4":
                            var dir_in = ConAsk("出力したフォルダを入力してください。").Replace("\"", "");
                            if (!File.Exists($"{dir_in}\\_param.json"))
                            {
                                ConWrite($"パラメータファイル({dir_in}\\_param.json)が見つかりません。", ConsoleColor.Red);
                                break;
                            }
                            ConWrite($"{DateTime.Now:HH:mm:ss.ffff} 読み込み中...", ConsoleColor.Blue);
                            var files = Directory.EnumerateFiles(dir_in, "*.json").Where(x => !x.EndsWith("_param.json"));
                            var paramNode = JsonNode.Parse(File.ReadAllText($"{dir_in}\\_param.json"));
#pragma warning disable CS8619 // 値における参照型の Null 許容性が、対象の型と一致しません。
#pragma warning disable CS8602 // null 参照の可能性があるものの逆参照です。
                            data_Draw = new Data_Draw()
                            {
                                CalStartTime = DateTime.Parse(paramNode["CalStartTime"]?.ToString() ?? DateTime.MinValue.ToString()),
                                HypoLat = double.Parse(paramNode["HypoLat"]?.ToString() ?? "0"),
                                HypoLon = double.Parse(paramNode["HypoLon"]?.ToString() ?? "0"),
                                CalPeriod = TimeSpan.Parse(paramNode["CalPeriod"]?.ToString() ?? "00:00:00"),
                                TotalCalPeriodSec = int.Parse(paramNode["TotalCalPeriodSec"]?.ToString() ?? "-1"),
                                Datas_Draw = files.Select(x => JsonSerializer.Deserialize<Data_Draw.ObsDataD>(File.ReadAllText(x))).Where(x => x != null).ToDictionary(k => k.StationName, v => v)
                            };
#pragma warning restore CS8602 // null 参照の可能性があるものの逆参照です。
#pragma warning restore CS8619 // 値における参照型の Null 許容性が、対象の型と一致しません。
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
                            var startT5 = DateTime.Parse(ConAsk($"描画開始日時を入力してください。計算開始日時は {(data_Draw.CalStartTime == DateTime.MinValue ? "----/--/-- --:--:--" : data_Draw.CalStartTime)} となっています。例:2024/01/01 00:00:00", data_Draw.CalStartTime != DateTime.MinValue, data_Draw.CalStartTime.ToString()));
                            config_draw = new Config_Draw
                            {
                                StartTime = startT5,
                                EndTime = startT5.AddSeconds(double.Parse(ConAsk($"描画開始日時から終了までの時間(秒)を入力してください。震度計算では {(data_Draw.TotalCalPeriodSec == -1 ? "--" : data_Draw.TotalCalPeriodSec)} となっています。 例:300", data_Draw.TotalCalPeriodSec != -1, data_Draw.TotalCalPeriodSec.ToString()))),
                                DrawSpan = TimeSpan.FromSeconds(double.Parse(ConAsk($"描画間隔(秒、小数可)を入力してください。震度計算では {(calSpan.TotalSeconds == 0d ? "--" : calSpan.TotalSeconds)} となっています。例1:1 例2:0.5", calSpan.TotalSeconds != 0d, calSpan.TotalSeconds.ToString()))),
                                DrawObsName = ConAsk("観測点円の右に観測点名を表示しますか？(y/n) ※地図を拡大しない場合非推奨です。", true, "n") == "y",
                                DrawObsShindo = ConAsk("観測点円の右に観測点震度を表示しますか？(y/n) ※地図を拡大しない場合非推奨です。", true, "n") == "y",
                                AutoZoomMin = int.Parse(ConAsk("<自動ズームの方法は 最小震度,最大震度との差 の2通りあります。対象のものがない場合後に設定する緯度経度の始点終点で描画されます。>\n" +
                                "最小震度での自動ズームをする場合、範囲に入れる最小震度を入力してください。最大震度との差での対象のものが無ければこの範囲になります。 -9で無効、計測震度に対する値: ~-0.5は-1、-0.5~0.5は0, ... 4.5~5.0(5弱)は5, 5.0~5.5(5強)は6, ...です。例1:-9 例2:(震度4の場合):4", true, "-9")),
                                AutoZoomMinDif = int.Parse(ConAsk("(描画時刻の)最大震度との差での自動ズームをする場合、範囲に入れる最大震度との差を入力してください。 -9で無効、10以上で全表示です。例1:-9 例2:(最大震度5弱で震度3まで(5弱,4,3)の場合):3", true, "-9")),
                                Obs_UseIntColor = ConAsk("観測点円の色に強震モニタ色ではなく震度階級別色を利用しますか？(右側では両方描画します) (y/n)", true, "n") == "y"
                            };
                            config_map = new Config_Map
                            {
                                MapSize = int.Parse(ConAsk("マップサイズ(画像の高さ)を入力してください。幅は16:9になるように計算されます。例:1080", true, "1080")),
                                LatSta = double.Parse(ConAsk($"緯度の始点(地図の下端)を入力してください。例:22.5 震源緯度は {(data_Draw.HypoLat == -200d ? "--" : data_Draw.HypoLat)} となっています。", true, "22.5")),
                                LatEnd = double.Parse(ConAsk("緯度の終点(地図の上端)を入力してください。例:47.5", true, "47.5")),
                                LonSta = double.Parse(ConAsk($"経度の始点(地図の左端)を入力してください。例:122.5 震源経度は {(data_Draw.HypoLon == -200d ? "--" : data_Draw.HypoLon)} となっています。", true, "122.5")),
                                LonEnd = double.Parse(ConAsk("経度の終点(地図の右端)を入力してください。例:147.5", true, "147.5")),
                                MapType = (Config_Map.MapKind)int.Parse(ConAsk("マップの種類(数字)を入力してください。線の太さは種類ごとに固定なため範囲に応じて変えると良いです(全国なら11,12、拡大なら21,22,31,32)。例:11\n" +
                                "> 11.地震情報／都道府県等 (軽量)\n" +
                                "> 12.地震情報／都道府県等 (詳細)\n" +
                                "> 21.地震情報／都道府県等 + 地震情報／細分区域 (軽量)\n" +
                                "> 22.地震情報／都道府県等 + 地震情報／細分区域 (詳細)\n" +
                                "> 31.地震情報／都道府県等 + 地震情報／細分区域 + 市町村等（地震津波関係） (軽量)\n" +
                                "> 32.地震情報／都道府県等 + 地震情報／細分区域 + 市町村等（地震津波関係） (詳細)", true, "11"))
                            };

                            if (!File.Exists("config-color.json"))
                                File.WriteAllText("config-color.json", JsonSerializer.Serialize(config_color, serializeIntend));
                            _ = ConAsk("色をconfig-color.jsonで設定してください。エンターキーを押すと描画を開始します。", true);
                            config_color = JsonSerializer.Deserialize<Config_Color>(File.ReadAllText("config-color.json"), serializeIntend) ?? new Config_Color();

                            Draw(data_Draw);
                            ConWrite($"{DateTime.Now:HH:mm:ss.ffff} 画像出力完了", ConsoleColor.Blue);
                            break;
                        case "8":
                            OpenTar(ConAsk("展開するtarファイルのパスを入力してください。").Replace("\"", ""));
                            break;
                        case "9":
                            data = null;
                            data_Draw = null;
                            ConWrite($"RAM:{GC.GetTotalMemory(true) / 1024d / 1024d:F2}MB", ConsoleColor.Green);
                            break;
                        case "0":
                            Console.ForegroundColor = ConsoleColor.Gray;
                            Environment.Exit(0);
                            break;
                        default:
                            ConWrite("値が無効です。", ConsoleColor.Red);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    ConWrite("[Main]", ex);
                    Directory.CreateDirectory("Errorlog");
                    File.WriteAllText($"Errorlog\\{DateTime.Now:yyyyMMddHHmmss.ffff}", ex.ToString());
                }
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
                var ok = ConAsk(".tar.gzファイルが見つかりました。展開しますか？(y/n)", true, "n") == "y";
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
            DateTime? originTime_tmp = null;
            double? hypoLat_tmp = null;
            double? hypoLon_tmp = null;
            if (ConAsk("地震データ(発生日時、震源緯度経度)を設定しますか？(y/n) ※K-NET,KiK-netのものを読み込む場合あればそれが優先されます。", true, "n") == "y")
            {
                originTime_tmp = DateTime.Parse(ConAsk("発生日時を入力してください。例:2024/01/01 00:00:00", true, DateTime.MinValue.ToString()));
                hypoLat_tmp = double.Parse(ConAsk("震源の緯度を入力してください。例:35.79", true, "-200"));
                hypoLon_tmp = double.Parse(ConAsk("震源の経度を入力してください。例:135.79", true, "-200"));
            }
            return JMAcsv2Data(files, originTime_tmp == DateTime.MinValue ? null : originTime_tmp,
                hypoLat_tmp == -200d ? null : hypoLat_tmp, hypoLon_tmp == -200d ? null : hypoLon_tmp);//.Where(x => !x.EndsWith("level.csv")).ToArray()
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
                var ok = ConAsk(".tar.gzファイルが見つかりました。展開しますか？(y/n)", true, "n") == "y";
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
        /// 加速度から震度を求めます。
        /// </summary>
        /// <param name="data">加速度データ</param>
        /// <param name="drawData">描画用データ(out)</param>
        /// <param name="startTime">開始時刻</param>
        /// <param name="endTime">終了時刻</param>
        /// <param name="calSpan">計算間隔</param>
        /// <param name="calPeriod">計算時間(通常1分)</param>
        /// <param name="calTimeBefore">先に予想時間用計算をするか</param>
        public static void Acc2JI(Data? data, out Data_Draw? drawData, DateTime startTime, DateTime endTime, TimeSpan calSpan, TimeSpan calPeriod, bool calTimeBefore = true)
        {
            data ??= new Data();//nullの時下で止める
            if (data.ObsDatas == null)
            {
                ConWrite("加速度データが存在しません。", ConsoleColor.Red);
                drawData = null;
                return;
            }
            drawData = new Data_Draw(data)
            {
                CalStartTime = startTime,
                CalPeriod = calPeriod,
                TotalCalPeriodSec = (int)(endTime - startTime).TotalSeconds
            };

            if (calTimeBefore)
            {
                ConWrite($"{DateTime.Now:HH:mm:ss.ffff} データ量確認中...", ConsoleColor.Blue);
                List<int> totalValidDataCount = [];
                for (var drawTime = startTime; drawTime < endTime; drawTime += calSpan)
                {
                    var validDataCount_tmp = 0;
                    foreach (var data1 in data.ObsDatas.Where(x => x.DataDir == "N-S"))
                    {
                        var startIndex_tmp = (int)((drawTime - calPeriod + calSpan - data1.RecordTime).TotalMilliseconds * data1.SamplingFreq / 1000);//参照不可能(計算用)
                        var startIndex = Math.Max(startIndex_tmp, 0);
                        var endIndex_tmp = (int)((drawTime + calSpan - data1.RecordTime).TotalMilliseconds * data1.SamplingFreq / 1000);//参照不可能(計算用)
                        var endIndex = Math.Min(Math.Max(endIndex_tmp, 0), data1.Accs.Length) - 1;
                        var count = endIndex - startIndex + 1;
                        if (count < 0.3 * data1.SamplingFreq)
                            continue;
                        validDataCount_tmp += count;
                    }
                    totalValidDataCount.Add(validDataCount_tmp);
                }
                var validDataCountMax = totalValidDataCount.Max();
                if (validDataCountMax == 0)
                {
                    ConWrite("有効な加速度データが存在しません。設定を確認してください。", ConsoleColor.Red);
                    drawData = null;
                    return;
                }
                ConWrite("\n");//─ │ ┌ ┐ └ ┘
                Console.WindowWidth = Math.Max(Console.WindowWidth, 105);//なんか変わらない
                Console.WriteLine("100│\n 90│\n 80│\n 70│\n 60│\n 50│\n 40│\n 30│\n 20│\n 10│\n" +
                    "  0└─────────┴─────────┴─────────┴─────────┴─────────┴─────────┴─────────┴─────────┴─────────┴────────── \n" +
                    "(%)0        10        20        30        40        50        60        70        80        90        100\n");
                var endLine = Console.CursorTop - 3;//描画エリア最後の所
                Console.ForegroundColor = ConsoleColor.Green;
                for (var i = 1; i < 101; i++)
                {
                    var left = i + 3;
                    var j = Math.Max(totalValidDataCount.Count * i / 100 - 1, 0);
                    var validDataCount_ = totalValidDataCount[j];
                    var value = validDataCount_ * 100 / validDataCountMax;//最高に対する割合
                    var y = (int)Math.Round(value / 10d, MidpointRounding.AwayFromZero);
                    //var y = (int)Math.Round(i / 10d, MidpointRounding.AwayFromZero);
                    Console.SetCursorPosition(left, endLine - y);
                    if (validDataCount_ == 0)
                        Console.Write("x");
                    else
                        Console.Write("*");
                    Thread.Sleep(10);
                }
                Console.SetCursorPosition(0, endLine + 3);
                ConWrite("<データ数の分布> 縦軸が処理データ数が最大となるものを100%にしたときの割合、横軸がデータの計算の割合(100%で終了) xはデータなし\n" +
                    "計算時間はおおむねこのグラフのように変化します(縦軸の値が大きいほど時間がかかる)。参考にしてください(予想時間は直前の計算時間のまま計算したときの時間です)。");
                if (ConAsk("計算を実行してよろしいですか？(y/n) (計算時間は過不足していませんか？)") != "y")
                {
                    drawData = null;
                    return;
                }
            }

            //return;

            ConWrite($"{DateTime.Now:HH:mm:ss.ffff} 震度計算中...", ConsoleColor.Blue);
            ConWrite($"{startTime:yyyy/MM/dd  HH:mm:ss.ff} ~ {endTime:HH:mm:ss.ff}  span:{calSpan:mm\\:ss\\.ff} period:{calPeriod:mm\\:ss\\.ff}  dataCount(acc)(points):{data.ObsDatas.Length / 3}\n", ConsoleColor.Green);
            Console.SetCursorPosition(0, Console.CursorTop - 1);
            var nowP = 0;
            var total = (endTime - startTime) / calSpan;
            var total2 = data.ObsDatas.Length / 3d;
            var calStartT = DateTime.Now;
            var calStartT2 = DateTime.Now;
            var validObsCount = 0;
            var text1 = $"\r└ {startTime:HH:mm:ss.ff} ";
            var text2 = "";
            for (var drawTime = startTime; drawTime < endTime; drawTime += calSpan)
            {
                if (nowP % 10 == 0)
                    GC.Collect();
                var eta1 = (DateTime.Now - calStartT2) * (total - nowP);
                var lastCalTime = DateTime.Now - calStartT2;
                calStartT2 = DateTime.Now;
                var nowP2 = 0;
                var validObsCount_tmp = 0;
                foreach (var data1 in data.ObsDatas.Where(x => x.DataDir == "N-S"))
                {
                    var startIndex_tmp = (int)((drawTime - calPeriod + calSpan - data1.RecordTime).TotalMilliseconds * data1.SamplingFreq / 1000);//参照不可能(計算用)
                    var startIndex = Math.Max(startIndex_tmp, 0);
                    var endIndex_tmp = (int)((drawTime + calSpan - data1.RecordTime).TotalMilliseconds * data1.SamplingFreq / 1000);//参照不可能(計算用)
                    var endIndex = Math.Min(Math.Max(endIndex_tmp, 0), data1.Accs.Length) - 1;
                    var count = endIndex - startIndex + 1;
                    //st 00:00:05  span 0.25  draw 00:00:15
                    //=>  00:00:05.25 <= data < 00:00:15.25  10.25sec (*max:60sec)
                    //dataCount=sec*freq=msec*freq/1000 (100Hz:max6000)

                    var data23 = data.ObsDatas.Where(x => x.StationName == data1.StationName).ToArray();
                    var data1Ac = data1.Accs.Skip(startIndex).Take(count).ToArray();
                    var data2Ac = data23[1].Accs.Skip(startIndex).Take(count).ToArray();
                    var data3Ac = data23[2].Accs.Skip(startIndex).Take(count).ToArray();
                    if (data1Ac.Length < 0.3 * data1.SamplingFreq)
                        goto skip;
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
                    if (double.IsNormal(ji) || ji == 0)//小さすぎると-∞の時がある？ 0でもIsNormal=false
                        drawData.AddInt(data1, drawTime, ji);//計算はしてるから↓は追加しとく
                    //ConWrite($"{data1.StationName} {drawTime:HH:mm:ss.ff} : {ji}", ConsoleColor.Cyan);
                    //return;

                    validObsCount_tmp++;//有効だったものだけ合わせるから↓にはいらない
                skip:
                    nowP2++;
                    ConWrite(text1 + $"[data:{(nowP2 == total2 ? "" : " ")}{nowP2 / total2 * 100:00.00}% of {total2}]" + text2, ConsoleColor.Green, false);
                    ConsoleClearRight();
                }
                validObsCount = validObsCount_tmp;
                nowP++;
                text1 = $"\r└ {drawTime:HH:mm:ss.ff} -> {nowP}/{total} ({nowP / total * 100:F2}％) ";
                text2 = $" eta:{(eta1 >= TimeSpan.FromHours(1) ? eta1.TotalHours.ToString("0") + eta1.ToString("\\:mm\\:ss") : eta1.TotalMinutes.ToString("0") + eta1.ToString("\\:ss\\.ff"))}" +
                    $" (last:{(lastCalTime >= TimeSpan.FromSeconds(1) ? lastCalTime.TotalSeconds.ToString() : lastCalTime.TotalMilliseconds.ToString() + "m")}s validData:{validObsCount})";

                Console.SetCursorPosition(0, Console.CursorTop + 1);
                var proP = (int)(nowP / total * 100);
                ConWrite("   [", false);
                ConWrite(new string('=', proP), ConsoleColor.Green, false);
                ConWrite(new string(' ', 100 - proP), ConsoleColor.Green, false);
                ConWrite("]", false);
                Console.SetCursorPosition(0, Console.CursorTop - 1);
            }
            ConWrite(text1 + $"[data:100.00% of {total2}]" + text2, ConsoleColor.Green);

            ConWrite($"\n{DateTime.Now:HH:mm:ss.ffff} 震度計算完了", ConsoleColor.Blue);
            ConWrite($"dataCount(int):{drawData.Datas_Draw.Count}  RAM:{GC.GetTotalMemory(true) / 1024d / 1024d:F2}MB", ConsoleColor.Green);
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
        /// 震度データを保存します。
        /// </summary>
        /// <param name="data_Draw">描画用データ</param>
        public static void ShindoSave(Data_Draw? data_Draw)
        {
            ConWrite($"{DateTime.Now:HH:mm:ss.ffff} 震度データ出力中...", ConsoleColor.Blue);
            if (data_Draw == null)
            {
                ConWrite("先に震度を計算してください。", ConsoleColor.Red);
                return;
            }
            if (data_Draw.Datas_Draw.Count == 0)
            {
                ConWrite("先に震度を計算してください。", ConsoleColor.Red);
                return;
            }
            var calSpanCk_out = data_Draw.Datas_Draw.Values.First().TimeInt.Keys.ToArray();
            var calSpan_out = calSpanCk_out[1] - calSpanCk_out[0];
            var dir_out = $"output\\shindo\\{data_Draw.CalStartTime:yyyyMMddHHmmss}-{data_Draw.Datas_Draw.Count}-{calSpan_out.TotalSeconds}s-{data_Draw.CalPeriod.TotalSeconds}s";
            Directory.CreateDirectory(dir_out);
            File.WriteAllText($"{dir_out}\\_param.json",
                "{" +
                $"\"CalStartTime\":\"{data_Draw.CalStartTime}\"," +
                $"\"TotalCalPeriodSec\":{data_Draw.TotalCalPeriodSec}," +
                $"\"CalPeriod\":\"{data_Draw.CalPeriod}\"," +
                $"\"HypoLat\":{data_Draw.HypoLat}," +
                $"\"HypoLon\":{data_Draw.HypoLon}" +
                "}");
            foreach (var obsData in data_Draw.Datas_Draw)
                File.WriteAllText($"{dir_out}\\{obsData.Key}.json", JsonSerializer.Serialize(obsData.Value));//観測点名によっては失敗するかも
            ConWrite($"{dir_out} に出力しました。", ConsoleColor.Green);
        }

        /// <summary>
        /// 画像描画用に緯度・経度を補正します。余白も追加します。
        /// </summary>
        /// <param name="latSta">緯度の始点</param>
        /// <param name="latEnd">緯度の終点</param>
        /// <param name="lonSta">経度の始点</param>
        /// <param name="lonEnd">経度の終点</param>
        public static void AreaCorrect(ref double latSta, ref double latEnd, ref double lonSta, ref double lonEnd)
        {
            latSta -= (latEnd - latSta) / 20;//差の1/20余白追加
            latEnd += (latEnd - latSta) / 20;
            lonSta -= (lonEnd - lonSta) / 20;
            lonEnd += (lonEnd - lonSta) / 20;
            if (latEnd - latSta < 3)//緯度差を最小3に
            {
                var correction = (3 - (latEnd - latSta)) / 2d;
                latSta -= correction;
                latEnd += correction;
            }
            if (lonEnd - lonSta > latEnd - latSta)//大きいほうに合わせる
            {
                var correction = ((lonEnd - lonSta) - (latEnd - latSta)) / 2d;
                latSta -= correction;
                latEnd += correction;
            }
            else// if (LonEnd - LonSta < LatEnd - LatSta)
            {
                var correction = ((latEnd - latSta) - (lonEnd - lonSta)) / 2d;
                lonSta -= correction;
                lonEnd += correction;
            }
            /*//動きまくるのを避ける
            if (true)
            {//正のとき用
                latSta = (int)latSta - 1;
                latEnd = (int)latEnd + 1;
                lonSta = (int)lonSta - 1;
                lonEnd = (int)lonEnd + 1;
            }*/
        }


        /// <summary>
        /// コンソールで入力を求めます。
        /// </summary>
        /// <param name="message">表示するメッセージ</param>
        /// <param name="allowNull">空文字入力を許容するか</param>
        /// <param name="defaultValue"><paramref name="allowNull"/>がtrueの時、空文字入力の時に返す値 入力時確認用表示を行います。nullにすると何も表示しません(""を返します)。</param>
        /// <returns>入力された文字列</returns>
        public static string ConAsk(string message, bool allowNull = false, string? defaultValue = null)
        {
            ConWrite(message);
        retry:
            Console.ForegroundColor = ConsoleColor.Cyan;
            var ans = Console.ReadLine();
            if (string.IsNullOrEmpty(ans))
                if (allowNull)
                {
                    if (defaultValue != null)
                    {
                        Console.SetCursorPosition(0, Console.CursorTop - 1);
                        ConWrite($"(\"{defaultValue}\"を自動入力しました)", ConsoleColor.Yellow);
                    }
                    return string.IsNullOrEmpty(ans) ? defaultValue ?? "" : ans;
                }
                else
                {
                    ConWrite("値を入力してください。", ConsoleColor.Red);
                    ConWrite(message);
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

        /// <summary>
        /// コンソールの現在の行をクリアします。
        /// </summary>
        public static void ConsoleClear1Line()
        {
            var currentLine = Console.CursorTop;
            Console.SetCursorPosition(0, currentLine);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentLine);
        }

        /// <summary>
        /// コンソールの現在の行の右をクリアします(\rで上書きするとき用)。
        /// </summary>
        public static void ConsoleClearRight()
        {
            var currentLine = Console.CursorTop;
            var currentLeft = Console.CursorLeft;
            Console.Write(new string(' ', Console.WindowWidth - currentLeft));
            Console.SetCursorPosition(currentLeft, currentLine);
        }
    }
}
