using static KmoniReproducer.Program;
namespace KmoniReproducer
{
    /// <summary>
    /// 一地震での加速度データ
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
            string fileName1 = fileNames[0];
            var fileText1 = File.ReadAllLines(fileName1);
            for (var i = 0; i < 15; i++)
                fileText1[i] = fileText1[i][18..];

            ConWrite($"{DateTime.Now:HH:MM:ss.ffff} 読み込み中...");
            var data = new Data
            {
                OriginTime = DateTime.Parse(fileText1[0]),
                HypoLat = double.Parse(fileText1[1]),
                HypoLon = double.Parse(fileText1[2]),
                ObsDatas = fileNames.Select(ObsData.KNET_ASCII2ObsData).ToArray()
            };
            ConWrite($"{DateTime.Now:HH:MM:ss.ffff} 読み込み完了  データ数:{data.ObsDatas.Length} メモリ:{GC.GetTotalMemory(true) / 1024d / 1024d:.00}MB", ConsoleColor.Blue);
            return data;
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
            /// ファイル名を指定してObsDataを初期化します。
            /// </summary>
            /// <param name="fileName">ファイル名</param>
            /// <returns>指定したファイルのデータ(加速度はgal変換・補正済み())</returns>
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
            /// 観測データ
            /// </summary>
            public double[] Accs { get; set; } = [];
        }
    }

    /// <summary>
    /// 描画用データ
    /// </summary>
    public class Data_Draw
    {

        public static Data_Draw Data2Data_Draw(Data data)
        {







        }

        /// <summary>
        /// 観測点のデータのリスト
        /// </summary>
        public Dictionary<string, ObsData>? Datas_Draw { get; set; }

        /// <summary>
        /// 一観測点のデータ
        /// </summary>
        /// <param name="obsData"></param>
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
