using System.Runtime.Versioning;
using System.Text;
using static KmoniReproducer.Program;

namespace KmoniReproducer
{
    /// <summary>
    /// 一地震での加速度データと地震データ
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class Data
    {
        /// <summary>
        /// K-NET ASCIIのファイル名を指定して<c>Data</c>を初期化します。
        /// </summary>
        /// <remarks>地震データは最初ものを取得します。すべて同一地震で三軸すべてのファイルがあるようにしてください。</remarks>
        /// <param name="fileNames">ファイル名の配列</param>
        public static Data? KNET_ASCII2Data(string[] fileNames)//https://www.kyoshin.bosai.go.jp/kyoshin/man/knetform.html
        {
            try
            {
                ConWrite($"{DateTime.Now:HH:mm:ss.ffff} 読み込み中...", ConsoleColor.Blue);
                string fileName1 = fileNames[0];
                var fileText1 = File.ReadAllLines(fileName1);
                for (var i = 0; i < 15; i++)
                    fileText1[i] = fileText1[i][18..];

#pragma warning disable CS8619 // 値における参照型の Null 許容性が、対象の型と一致しません。
                var data = new Data
                {
                    OriginTime = fileText1[0] == "" ? DateTime.MinValue : DateTime.Parse(fileText1[0]),//即時公開データ等震源データがない
                    HypoLat = fileText1[1] == "" ? -200d : double.Parse(fileText1[1]),
                    HypoLon = fileText1[2] == "" ? -200d : double.Parse(fileText1[2]),
                    Depth = fileText1[3] == "" ? -200d : double.Parse(fileText1[3]),
                    ObsDatas = fileNames.Select(ObsData.KNET_ASCII2ObsData).Where(x => x != null).ToArray()
                };
#pragma warning restore CS8619 // 値における参照型の Null 許容性が、対象の型と一致しません。
                ConWrite($"{DateTime.Now:HH:mm:ss.ffff} 読み込み完了", ConsoleColor.Blue);
                ConWrite($"dataCount(acc):{data.ObsDatas.Length}(points:{data.ObsDatas.Length / 3})  RAM:{GC.GetTotalMemory(true) / 1024d / 1024d:F2}MB", ConsoleColor.Green);
                return data;
            }
            catch (Exception ex)
            {
#if DEBUG
                ConWrite("[KNET_ASCII2Data]", ex);
#endif
                return null;
            }
        }

        /// <summary>
        /// 気象庁加速度データのファイル名を指定して<c>Data</c>を初期化します。
        /// </summary>
        /// <remarks>地震データは存在しません。ユーザー入力で指定することができます(<c>Data.AddObsDatas()</c>で追加時にK-NET,KiK-netのものになるように調整されます)。</remarks>
        /// <param name="fileNames">ファイル名の配列</param>
        /// <param name="originTime">(発生日時)</param>
        /// <param name="hypoLat">(震源緯度)</param>
        /// <param name="hypoLon">(震源経度)</param>
        public static Data? JMACSV2Data(string[] fileNames, DateTime? originTime = null, double? hypoLat = null, double? hypoLon = null, double? depth = null)
        {
            try
            {
                ConWrite($"{DateTime.Now:HH:mm:ss.ffff} 読み込み中...", ConsoleColor.Blue);
                var fileTexts = File.ReadAllLines(fileNames[0], Encoding.GetEncoding("Shift-JIS")).ToArray();
                var initTimeInts = fileTexts[5].Split(',')[0].Split('=')[1].Replace("   ", " 19").Split(' ').Skip(1).Select(int.Parse).ToArray();//1900年代は  xx表記になってる
#pragma warning disable CS8603 // Null 参照戻り値である可能性があります。
                var data = new Data
                {
                    //OriginTime = new DateTime(initTimeInts[0], initTimeInts[1], initTimeInts[2], initTimeInts[3], initTimeInts[4], initTimeInts[5]),//とりあえず最初//やっぱりやめる
                    ObsDatas = fileNames.Select(ObsData.JMAcsv2ObsData).Where(x => x != null).SelectMany(x => x).ToArray()
                };
#pragma warning restore CS8603 // Null 参照戻り値である可能性があります。
                if (originTime != null)
                    data.OriginTime = (DateTime)originTime;
                if (hypoLat != null)
                    data.HypoLat = hypoLat.Value;
                if (hypoLon != null)
                    data.HypoLon = hypoLon.Value;
                if (depth != null)
                    data.Depth = depth.Value;
                ConWrite($"{DateTime.Now:HH:mm:ss.ffff} 読み込み完了", ConsoleColor.Blue);
                ConWrite($"dataCount(acc):{data.ObsDatas.Length}(points:{data.ObsDatas.Length / 3})  RAM:{GC.GetTotalMemory(true) / 1024d / 1024d:F2}MB", ConsoleColor.Green);
                return data;
            }
            catch (Exception ex)
            {
#if DEBUG
                ConWrite("[JMACSV2Data]", ex);
#endif
                return null;
            }
        }

        /// <summary>
        /// ObsDatasを追加します。
        /// </summary>
        /// <param name="data">追加するObsDatasを含むData</param>
        /// <param name="changeEqinfo">地震データを上書きするか(K-NET,KiK-netのみ)　<paramref name="data"/>に無いまたはすでにある場合上書きしません。</param>
        public void AddObsDatas(Data? data, bool changeEqinfo)//v1.0.2(仮)実装で changeEqinfoは不要になった
        {
            if (data == null)
                return;
            if (data.ObsDatas == null)
                return;
            if (changeEqinfo)//一部だけない状態だと混ざる可能性
            {
                if (data.OriginTime != DateTime.MinValue && OriginTime == DateTime.MinValue)
                    OriginTime = data.OriginTime;
                if (data.HypoLat != -200d && HypoLat == -200d)
                    HypoLat = data.HypoLat;
                if (data.HypoLon != -200d && HypoLon == -200d)
                    HypoLon = data.HypoLon;
                if (data.Depth != -200d && Depth == -200d)
                    Depth = data.Depth;
            }
            AddObsDatas(data.ObsDatas);
        }

        /// <summary>
        /// ObsDatasを追加します。
        /// </summary>
        /// <param name="obsDatas">追加するObsDatas</param>
        public void AddObsDatas(ObsData[] obsDatas)
        {
            if (ObsDatas == null)
            {
                ObsDatas = obsDatas;
                return;
            }
            var obsDataList = ObsDatas.ToList();
            obsDataList.AddRange(obsDatas);
            ObsDatas = [.. obsDataList];
            ConWrite($"{DateTime.Now:HH:mm:ss.ffff} 追加完了", ConsoleColor.Blue);
            ConWrite($"dataCount(acc):{ObsDatas.Length}(points:{ObsDatas.Length / 3})  RAM:{GC.GetTotalMemory(true) / 1024d / 1024d:F2}MB", ConsoleColor.Green);
        }

        /// <summary>
        /// 発生時刻
        /// </summary>
        public DateTime OriginTime { get; set; } = DateTime.MinValue;

        /// <summary>
        /// 震源緯度
        /// </summary>
        public double HypoLat { get; set; } = -200d;

        /// <summary>
        /// 震源経度
        /// </summary>
        public double HypoLon { get; set; } = -200d;

        /// <summary>
        /// 震源深さ
        /// </summary>
        public double Depth { get; set; } = -200d;

        /// <summary>
        /// 観測データの配列
        /// </summary>
        public ObsData[]? ObsDatas { get; set; } = null;

        /// <summary>
        /// 観測データ
        /// </summary>
        public class ObsData
        {
            /// <summary>
            /// K-NET ASCIIファイル名を指定して<c>ObsData</c>を初期化します。
            /// </summary>
            /// <param name="fileName">ファイル名</param>
            /// <returns>指定したファイルのデータ(加速度はgal変換・補正済み)</returns>
            public static ObsData? KNET_ASCII2ObsData(string fileName)
            {
                try
                {
                    //ConWrite(fileName, ConsoleColor.Green);
                    var fileTexts = File.ReadAllLines(fileName);
                    for (var i = 0; i < 15; i++)
                        fileTexts[i] = fileTexts[i][18..];
                    var scaleFs = fileTexts[13].Split("(gal)/").Select(double.Parse).ToArray();
                    var scaleFactor = scaleFs[0] / scaleFs[1];

                    var rawAccs = fileTexts.Skip(17).SelectMany(x => x.Split(' ', StringSplitOptions.RemoveEmptyEntries)).Select(int.Parse).ToArray();
                    return new ObsData
                    {
                        StationName = fileTexts[5],
                        StationLat = double.Parse(fileTexts[6]),
                        StationLon = double.Parse(fileTexts[7]),
                        RecordTime = DateTime.Parse(fileTexts[9]).AddSeconds(-15),//https://www.kyoshin.bosai.go.jp/kyoshin/man/knetform.html
                        SamplingFreq = int.Parse(fileTexts[10].Replace("Hz", "")),
                        DataDir = fileTexts[12].Replace("4", "N-S").Replace("4", "E-W").Replace("6", "U-D"),//kikは1~6(4~6が地表)
                        Accs = rawAccs.Select(rawAcc => rawAcc * scaleFactor).ToArray()//震度求めるとき修正
                    };
                }
                catch (Exception ex)
                {
                    ConWrite($"読み込み失敗:{fileName}", ConsoleColor.Red);
#if DEBUG
                    ConWrite("[KNET_ASCII2ObsData]", ex);
#endif
                    return null;
                }
            }

            /// <summary>
            /// 気象庁加速度データのファイル名を指定して<c>ObsData</c>を初期化します。
            /// </summary>
            /// <remarks>古いものは失敗する可能性があります。</remarks>
            /// <param name="fileName">ファイル名</param>
            /// <returns>指定したファイルのデータ</returns>
            public static ObsData[]? JMAcsv2ObsData(string fileName)//Encoding.GetEncoding("Shift-JIS")にはEncoding.RegisterProvider(CodePagesEncodingProvider.Instance);を一度(Main()内とか)やる必要あり
            {
                try
                {
                    var fileTexts = File.ReadAllLines(fileName, Encoding.GetEncoding("Shift-JIS")).Select(x => x.Replace(",,,,,", "")).ToArray();//,,は古いやつ用 ,が一行当たり7個ある
                    for (var i = 0; i < 6; i++)
                        fileTexts[i] = fileTexts[i].Split(',')[0].Split('=')[1];//,は古いやつ用
                    var initTimeInts = fileTexts[5].Replace("   ", " 19").Split(' ').Skip(1).Select(int.Parse).ToArray();//1900年代は  xx表記になってる
                    var stationName = fileTexts[0].Replace(" ", "");
                    if (string.IsNullOrEmpty(stationName))
                        return null;
                    var stationLatSt = fileTexts[1].Replace(" ", "");
                    if (string.IsNullOrEmpty(stationLatSt))//緯度経度なしのを除外しない場合[return null; <--> stationLatSt = "-200";]変える
                        return null;/*stationLatSt = "-200";*/
                    var stationLat = double.Parse(stationLatSt);
                    var stationLonSt = fileTexts[2].Replace(" ", "");
                    if (string.IsNullOrEmpty(stationLonSt))
                        return null; /*stationLonSt = "-200";*/
                    var stationLon = double.Parse(stationLonSt);
                    var samplingFreq = int.Parse(fileTexts[3].Replace(" ", "").Replace("Hz", ""));
                    var recordTime = new DateTime(initTimeInts[0], initTimeInts[1], initTimeInts[2], initTimeInts[3], initTimeInts[4], initTimeInts[5]);
                    var accs = fileTexts.Skip(7).Select(x => x.Replace(" ", "").Split(',', StringSplitOptions.RemoveEmptyEntries)).Where(x => x.Length != 0).ToArray();
                    var obsData0 = new ObsData
                    {
                        StationName = stationName,
                        StationLat = stationLat,
                        StationLon = stationLon,
                        SamplingFreq = samplingFreq,
                        RecordTime = recordTime,
                        DataDir = "N-S",
                        Accs = accs.Select(x => double.Parse(x[0])).ToArray()
                    };
                    var obsData1 = new ObsData
                    {
                        StationName = stationName,
                        StationLat = stationLat,
                        StationLon = stationLon,
                        SamplingFreq = samplingFreq,
                        RecordTime = recordTime,
                        DataDir = "E-W",
                        Accs = accs.Select(x => double.Parse(x[1])).ToArray()
                    };
                    var obsData2 = new ObsData
                    {
                        StationName = stationName,
                        StationLat = stationLat,
                        StationLon = stationLon,
                        SamplingFreq = samplingFreq,
                        RecordTime = recordTime,
                        DataDir = "U-D",
                        Accs = accs.Select(x => double.Parse(x[2])).ToArray()
                    };
                    return [obsData0, obsData1, obsData2];
                }
                catch (Exception ex)
                {
                    if (fileName.EndsWith("level.csv"))
                        ConWrite($"無視:{fileName}", ConsoleColor.Green);
                    else
                        ConWrite($"読み込み失敗:{fileName}", ConsoleColor.Red);
#if DEBUG
                    if (!fileName.EndsWith("level.csv"))
                        ConWrite("[JMAcsv2ObsData]", ex);
#endif
                    return null;
                }
            }

            /// <summary>
            /// 観測点名
            /// </summary>
            public string StationName { get; set; } = "";

            /// <summary>
            /// 観測点緯度
            /// </summary>
            public double StationLat { get; set; } = -200d;

            /// <summary>
            /// 観測点経度
            /// </summary>
            public double StationLon { get; set; } = -200d;

            /// <summary>
            /// 記録開始時刻
            /// </summary>
            public DateTime RecordTime { get; set; }

            /// <summary>
            /// サンプリング周波数
            /// </summary>
            public int SamplingFreq { get; set; }

            /// <summary>
            /// 観測チャンネル
            /// </summary>
            public string DataDir { get; set; } = "";

            /// <summary>
            /// 加速度データ
            /// </summary>
            public double[] Accs { get; set; } = [];
        }
    }

    /// <summary>
    /// 描画用データ
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class Data_Draw
    {
        /// <summary>
        /// Data_Drawを初期化します。
        /// </summary>
        public Data_Draw() { }

        /// <summary>
        /// DataをData_Drawに変換します。
        /// </summary>
        /// <remarks>震度は<c>AddInt</c>を使用して追加する必要があります。</remarks>
        /// <param name="data">一地震での加速度データと地震データ</param>
        public Data_Draw(Data data)
        {
            OriginTime = data.OriginTime;
            HypoLat = data.HypoLat;
            HypoLon = data.HypoLon;
            Depth = data.Depth;
        }

        /// <summary>
        /// 震度データを追加します。
        /// </summary>
        /// <param name="obsData">観測データ</param>
        /// <param name="intTime">震度の時刻</param>
        /// <param name="jInt">震度</param>
        public void AddInt(Data.ObsData obsData, DateTime intTime, double jInt)
        {
            if (!Datas_Draw.ContainsKey(obsData.StationName))
                Datas_Draw[obsData.StationName] = new ObsDataD(obsData);
            Datas_Draw[obsData.StationName].TimeInt[intTime] = jInt;
        }

        /// <summary>
        /// 計算開始時刻
        /// </summary>
        public DateTime CalStartTime { get; set; } = DateTime.MinValue;

        /// <summary>
        /// 発生時刻
        /// </summary>
        public DateTime OriginTime { get; set; } = DateTime.MinValue;

        /// <summary>
        /// 震源緯度
        /// </summary>
        public double HypoLat { get; set; } = -200d;

        /// <summary>
        /// 震源経度
        /// </summary>
        public double HypoLon { get; set; } = -200d;

        /// <summary>
        /// 震源深さ
        /// </summary>
        public double Depth { get; set; } = -200d;

        /// <summary>
        /// 震度計算時間(通常1分)
        /// </summary>
        public TimeSpan CalPeriod { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// 全計算期間(終了時刻-開始時刻)
        /// </summary>
        public int TotalCalPeriodSec { get; set; } = -1;

        /// <summary>
        /// 観測点のデータのリスト
        /// </summary>
        /// <remarks><c>StationName</c>, <c>ObsDataD</c></remarks>
        public Dictionary<string, ObsDataD> Datas_Draw { get; set; } = [];

        /// <summary>
        /// 一観測点のデータ
        /// </summary>
        public class ObsDataD
        {
            /// <summary>
            /// 観測データ(<see cref="Data.ObsData"/>)からObsDataDを初期化します。
            /// </summary>
            /// <param name="obsData">観測データ</param>
            public ObsDataD(Data.ObsData obsData)
            {
                StationName = obsData.StationName;
                StationLat = obsData.StationLat;
                StationLon = obsData.StationLon;
            }

            /// <summary>
            /// <b>JSON変換時用コンストラクタです。これで初期化しないでください(<see cref="ObsDataD(Data.ObsData)"/>を使用してください)。</b>
            /// </summary>
            public ObsDataD() { }

            /// <summary>
            /// 観測点名
            /// </summary>
            public string StationName { get; set; } = "";

            /// <summary>
            /// 観測点緯度
            /// </summary>
            public double StationLat { get; set; } = -200d;

            /// <summary>
            /// 観測点経度
            /// </summary>
            public double StationLon { get; set; } = -200d;

            /// <summary>
            /// 時刻ごとの震度
            /// </summary>
            public Dictionary<DateTime, double> TimeInt { get; set; } = [];
        }
    }
}
