using System.Data;
using System.Text;
using static KmoniReproducer.Program;

namespace KmoniReproducer
{
    /// <summary>
    /// 一地震での加速度データと地震データ
    /// </summary>
    public class Data
    {
        /// <summary>
        /// K-NET ASCIIのファイル名を指定して<c>Data</c>を初期化します。
        /// </summary>
        /// <remarks>地震データは最初ものを取得します。すべて同一地震で三軸すべてのファイルがあるようにしてください。</remarks>
        /// <param name="fileNames">ファイル名の配列</param>
        public static Data KNET_ASCII2Data(string[] fileNames)//https://www.kyoshin.bosai.go.jp/kyoshin/man/knetform.html
        {
            ConWrite($"{DateTime.Now:HH:MM:ss.ffff} 読み込み中...", ConsoleColor.Blue);
            string fileName1 = fileNames[0];
            var fileText1 = File.ReadAllLines(fileName1);
            for (var i = 0; i < 15; i++)
                fileText1[i] = fileText1[i][18..];

            var data = new Data
            {
                OriginTime = DateTime.Parse(fileText1[0]),
                HypoLat = double.Parse(fileText1[1]),
                HypoLon = double.Parse(fileText1[2]),
                ObsDatas = fileNames.Select(ObsData.KNET_ASCII2ObsData).ToArray()
            };
            ConWrite($"{DateTime.Now:HH:MM:ss.ffff} 読み込み完了", ConsoleColor.Blue);
            ConWrite($"dataCount:{data.ObsDatas.Length}(points:{data.ObsDatas.Length/3})  RAM:{GC.GetTotalMemory(true) / 1024d / 1024d:.00}MB", ConsoleColor.Green);
            return data;
        }

        /// <summary>
        /// 気象庁加速度データのファイル名を指定して<c>Data</c>を初期化します。
        /// </summary>
        /// <remarks>地震データは存在しません。</remarks>
        /// <param name="fileNames">ファイル名の配列</param>
        public static Data JMAcsv2Data(string[] fileNames)
        {
            ConWrite($"{DateTime.Now:HH:MM:ss.ffff} 読み込み中...", ConsoleColor.Blue);
            var fileTexts = File.ReadAllLines(fileNames[0], Encoding.GetEncoding("Shift-JIS")).ToArray();
            var initTimeInts = fileTexts[5].Split(',')[0].Split('=')[1].Replace("   ", " 19").Split(' ').Skip(1).Select(int.Parse).ToArray();//1900年代は  xx表記になってる
            var data = new Data
            {
                OriginTime = new DateTime(initTimeInts[0], initTimeInts[1], initTimeInts[2], initTimeInts[3], initTimeInts[4], initTimeInts[5]),//とりあえず最初
                ObsDatas = fileNames.Select(ObsData.JMAcsv2ObsData).SelectMany(x => x).ToArray()
            };
            ConWrite($"{DateTime.Now:HH:MM:ss.ffff} 読み込み完了", ConsoleColor.Blue);
            ConWrite($"dataCount:{data.ObsDatas.Length}(points:{data.ObsDatas.Length / 3})  RAM:{GC.GetTotalMemory(true) / 1024d / 1024d:.00}MB", ConsoleColor.Green);
            return data;
        }

        /// <summary>
        /// ObsDatasを追加します。
        /// </summary>
        /// <param name="obsDatas">追加するObsDatasを含むData</param>
        public void AddObsDatas(Data data)
        {
            AddObsDatas(data.ObsDatas);
        }

        /// <summary>
        /// ObsDatasを追加します。
        /// </summary>
        /// <param name="obsDatas">追加するObsDatas</param>
        public void AddObsDatas(ObsData[] obsDatas)
        {
            var obsDataList = ObsDatas.ToList();
            obsDataList.AddRange(obsDatas);
            ObsDatas = [.. obsDataList];
            ConWrite($"{DateTime.Now:HH:MM:ss.ffff} 追加完了", ConsoleColor.Blue);
            ConWrite($"dataCount:{ObsDatas.Length}(points:{ObsDatas.Length / 3})  RAM:{GC.GetTotalMemory(true) / 1024d / 1024d:.00}MB", ConsoleColor.Green);
        }

        /// <summary>
        /// 発生時刻
        /// </summary>
        public DateTime OriginTime { get; set; }

        /// <summary>
        /// 震源緯度
        /// </summary>
        public double HypoLat { get; set; }

        /// <summary>
        /// 震源経度
        /// </summary>
        public double HypoLon { get; set; }

        /// <summary>
        /// 観測データの配列
        /// </summary>
        public ObsData[] ObsDatas { get; set; } = [];

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
            public static ObsData KNET_ASCII2ObsData(string fileName)//https://www.kyoshin.bosai.go.jp/kyoshin/man/knetform.html
            {
                //ConWrite(fileName, ConsoleColor.Green);
                var fileTexts = File.ReadAllLines(fileName);
                for (var i = 0; i < 15; i++)
                    fileTexts[i] = fileTexts[i][18..];
                var scaleFs = fileTexts[13].Split("(gal)/").Select(double.Parse).ToArray();
                var scaleFactor = scaleFs[0] / scaleFs[1];

                var rawAccs = fileTexts.Skip(17).SelectMany(x => x.Split(' ', StringSplitOptions.RemoveEmptyEntries)).Select(int.Parse).ToArray();
                var ave = rawAccs.Average();
                return new ObsData
                {
                    StationName = fileTexts[5],
                    StationLat = double.Parse(fileTexts[6]),
                    StationLon = double.Parse(fileTexts[7]),
                    RecordTime = DateTime.Parse(fileTexts[9]).AddSeconds(-15),
                    SamplingFreq = int.Parse(fileTexts[10].Replace("Hz", "")),
                    DataDir = fileTexts[12].Replace("4", "N-S").Replace("4", "E-W").Replace("6", "U-D"),//kikは1~6(4~6が地表)
                    Accs = rawAccs.Select(rawAcc => (rawAcc - rawAccs.Average()) * scaleFactor).ToArray()//ここでも修正してるけどオプションで震度求めるとき修正
                };
            }

            /// <summary>
            /// 気象庁加速度データのファイル名を指定して<c>ObsData</c>を初期化します。
            /// </summary>
            /// <remarks>古いものは失敗する可能性があります。</remarks>
            /// <param name="fileName">ファイル名</param>
            /// <returns>指定したファイルのデータ</returns>
            public static ObsData[] JMAcsv2ObsData(string fileName)//Encoding.GetEncoding("Shift-JIS")にはEncoding.RegisterProvider(CodePagesEncodingProvider.Instance);を一度(Main()内とか)やる必要あり
            {
                var fileTexts = File.ReadAllLines(fileName, Encoding.GetEncoding("Shift-JIS")).Select(x => x.Replace(",,,,,", "")).ToArray();//,,は古いやつ用 ,が一行当たり7個ある
                for (var i = 0; i < 6; i++)
                    fileTexts[i] = fileTexts[i].Split(',')[0].Split('=')[1];//,は古いやつ用
                var initTimeInts = fileTexts[5].Replace("   ", " 19").Split(' ').Skip(1).Select(int.Parse).ToArray();//1900年代は  xx表記になってる
                var stationName = fileTexts[0].Replace(" ", "");
                var stationLat = double.Parse(fileTexts[1].Replace(" ", ""));
                var stationLon = double.Parse(fileTexts[2].Replace(" ", ""));
                var samplingFreq = int.Parse(fileTexts[3].Replace(" ", "").Replace("Hz", ""));
                var recordTime = new DateTime(initTimeInts[0], initTimeInts[1], initTimeInts[2], initTimeInts[3], initTimeInts[4], initTimeInts[5]);
                var accs = fileTexts.Skip(7).Select(x => x.Replace(" ", "").Split(',')).ToArray();
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

            /// <summary>
            /// 観測点名
            /// </summary>
            public string StationName { get; set; } = "";

            /// <summary>
            /// 観測点緯度
            /// </summary>
            public double StationLat { get; set; }

            /// <summary>
            /// 観測点経度
            /// </summary>
            public double StationLon { get; set; }

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
    public class Data_Draw
    {
        /// <summary>
        /// 震度データを追加します。
        /// </summary>
        /// <param name="obsData">観測データ</param>
        /// <param name="intTime">震度の時刻</param>
        /// <param name="jInt">震度</param>
        public void AddInt(Data.ObsData obsData, DateTime intTime, double jInt)
        {
            if (!Datas_Draw.ContainsKey(obsData.StationName))
                Datas_Draw.Add(obsData.StationName, new ObsData(obsData));
            Datas_Draw[obsData.StationName].TimeInt.Add(intTime, jInt);
        }

        /// <summary>
        /// 観測点のデータのリスト
        /// </summary>
        /// <remarks><c>StationName</c>, <c>ObsData</c></remarks>
        public Dictionary<string, ObsData> Datas_Draw { get; set; } = [];

        /// <summary>
        /// 一観測点のデータ
        /// </summary>
        /// <param name="obsData">観測データ</param>
        public class ObsData(Data.ObsData obsData)
        {
            /// <summary>
            /// 観測点名
            /// </summary>
            public string StationName { get; set; } = obsData.StationName;

            /// <summary>
            /// 観測点緯度
            /// </summary>
            public double StationLat { get; set; } = obsData.StationLat;

            /// <summary>
            /// 観測点経度
            /// </summary>
            public double StationLon { get; set; } = obsData.StationLon;

            /// <summary>
            /// 時刻ごとの震度
            /// </summary>
            public Dictionary<DateTime, double> TimeInt { get; set; } = [];
        }
    }
}
