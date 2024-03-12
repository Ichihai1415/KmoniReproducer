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
        /// <param name="fileNames">ファイル名</param>
        public Data(string[] fileNames)
        {
            ConWrite($"{DateTime.Now:HH:MM:ss.ffff} 初期化中...");
            string fileName1 = fileNames[0];
            var fileText1 = File.ReadAllLines(fileName1);
            for (var i = 0; i < 15; i++)
                fileText1[i] = fileText1[i][18..];
            OriginTime = DateTime.Parse(fileText1[0]);
            HypoLat = double.Parse(fileText1[1]);
            HypoLon = double.Parse(fileText1[2]);
            ConWrite($"{DateTime.Now:HH:MM:ss.ffff} 読み込み中...");
            KNETdatas = fileNames.Select(x => new KNET_ASCII(x)).ToArray();
            ConWrite($"{DateTime.Now:HH:MM:ss.ffff} 読み込み完了  データ数:{KNETdatas.Length} メモリ:{GC.GetTotalMemory(true) / 1024d / 1024d:.00}MB", ConsoleColor.Blue);
        }

        /// <summary>
        /// 発生時刻
        /// </summary>
        public DateTime OriginTime { get; set; } = DateTime.MinValue;

        /// <summary>
        /// 震源緯度
        /// </summary>
        public double HypoLat { get; set; } = 0;

        /// <summary>
        /// 震源経度
        /// </summary>
        public double HypoLon { get; set; } = 0;

        /// <summary>
        /// K-NET ASCIIフォーマットでのデータ
        /// </summary>
        public KNET_ASCII[]? KNETdatas { get; set; }

        /// <summary>
        /// K-NET ASCIIフォーマットでの一観測点のデータ
        /// </summary>
        public class KNET_ASCII
        {
            /// <summary>
            /// ファイル名を指定して<c>KNET_ASCII</c>を初期化します。
            /// </summary>
            /// <param name="fileName">ファイル名</param>
            public KNET_ASCII(string fileName)
            {
                //ConWrite(fileName, ConsoleColor.Green);
                var fileText = File.ReadAllLines(fileName);
                for (var i = 0; i < 15; i++)
                    fileText[i] = fileText[i][18..];
                StationCode = fileText[5];
                StationLat = double.Parse(fileText[6]);
                StationLon = double.Parse(fileText[7]);
                RecordTime = DateTime.Parse(fileText[9]);
                SamplingFreq = int.Parse(fileText[10].Replace("Hz", ""));
                DataDir = fileText[12];
                var scaleFs = fileText[13].Split("(gal)/").Select(double.Parse).ToArray();
                ScaleFactor = scaleFs[0] / scaleFs[1];

                var rawAccs = fileText.Skip(17).SelectMany(x => x.Split(' ', StringSplitOptions.RemoveEmptyEntries)).Select(int.Parse).ToArray();
                fileText = null;
                Accs = CorrectAccs(rawAccs, ScaleFactor);
                rawAccs = null;
            }

            /// <summary>
            /// 観測点コード
            /// </summary>
            public string StationCode { get; set; }

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
            DateTime _RecordTime;

            /// <summary>
            /// 記録開始時刻(<b>指定時に自動で15秒引かれます。</b><see href="https://www.kyoshin.bosai.go.jp/kyoshin/man/knetform.html">強震データのK-NET ASCIIフォーマットについて</see>を参照)
            /// </summary>
            public DateTime RecordTime
            {
                get { return _RecordTime; }
                set { _RecordTime = value.AddSeconds(-15); }//この時刻は強震計の遅延時間”１５秒”の効果を含んでいます。 したがって，真のスタート時刻を求めるためにはこの時刻から１５秒を引いて下さい。
            }

            /// <summary>
            /// サンプリング周波数
            /// </summary>
            public int SamplingFreq { get; set; }

            /// <summary>
            /// 観測チャンネル
            /// </summary>
            public string DataDir { get; set; }

            /// <summary>
            /// スケールファクタ
            /// </summary>
            public double ScaleFactor { get; set; }

            /// <summary>
            /// 観測データ
            /// </summary>
            public double[] Accs { get; set; }

            /// <summary>
            /// 生の強震データをgalに変換します。
            /// </summary>
            /// <param name="rawAccs">生の強震データ</param>
            /// <param name="scaleFacter">スケールファクタ</param>
            /// <returns></returns>
            public static double[] CorrectAccs(int[] rawAccs, double scaleFacter)
            {
                var ave = rawAccs.Average();
                return rawAccs.Select(x => (x - ave) * scaleFacter).ToArray();
            }



        }
    }
}
