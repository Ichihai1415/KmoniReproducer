using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.Versioning;
using System.Text.Json.Nodes;
using static KmoniReproducer.Program;

namespace KmoniReproducer
{
    [SupportedOSPlatform("windows")]
    public class DrawImg
    {
        public static void Draw(Data_Draw drawDatas)
        {
            var saveDir = $"output\\images\\{config_draw.StartTime:yyyyMMddHHmmss}-{DateTime.Now:yyyyMMddHHmmss}";
            var basemap = Draw_Map();
            var textColor = new SolidBrush(config_color.Text);

            var mapS = config_map.MapSize;
            var mapD4 = mapS / 4f;
            var mapD5 = mapS / 5f;
            var mapD5p = mapS / 5f + mapS;
            var mapD8 = mapS / 8f;
            var mapD8p = mapS / 8f + mapS;
            var mapD16 = mapS / 16f;
            var mapD16p = mapS / 16f + mapS;
            var mapD20 = mapS / 20f;
            var mapD24 = mapS / 24f;
            var mapD28 = mapS / 28f;
            var mapD28i = mapS / 28;
            var mapD30 = mapS / 30f;
            var mapD36 = mapS / 36f;
            var mapD60 = mapS / 60f;
            var mapD60p = mapS / 60f + mapS;
            var mapD180 = mapS / 180f;
            var mapD1080 = mapS / 1080f;

            var obsSize = config_draw.ObsSize * mapD1080;
            var obsSizeHalf = obsSize / 2;
            var zoomW = mapS / (config_map.LonEnd - config_map.LonSta);
            var zoomH = mapS / (config_map.LatEnd - config_map.LatSta);

            var mdsize = new SizeF();
            var infotextHei = 0f;
            using (var img_tmp = new Bitmap(100, 100))
            using (var g_tmp = Graphics.FromImage(img_tmp))
            {
                mdsize = g_tmp.MeasureString("地図データ:気象庁", new Font(font, mapD28i, GraphicsUnit.Pixel));
                infotextHei = g_tmp.MeasureString("あ", new Font(font, mapD36, GraphicsUnit.Pixel)).Height;
            }

            //#if true
#if false
            saveDir = $"output\\images";
            var img = new Bitmap(basemap);
            var g = Graphics.FromImage(img);
            var drawTime = new DateTime(2024, 01, 01, 16, 10, 30);

            var sortedInts = drawDatas.Datas_Draw.Values.OrderBy(x => x.TimeInt.TryGetValue(drawTime, out double value) ? value : double.MinValue);
            var infotextS = new List<double>();
            var infotextN = new List<string>();
            var infohead = $"観測点数:{drawDatas.Datas_Draw.Count}  震度計算秒数:{(drawDatas.CalPeriod == TimeSpan.Zero ? "--" : drawDatas.CalPeriod.TotalSeconds)}秒";
            foreach (var drawData in sortedInts)
            {
                var leftupperX = (int)((drawData.StationLon - config_map.LonSta) * zoomW) - obsSizeHalf;
                var leftupperY = (int)((config_map.LatEnd - drawData.StationLat) * zoomH) - obsSizeHalf;
                var text = config_draw.DrawObsName ? drawData.StationName + " " : "";
                if (drawData.TimeInt.TryGetValue(drawTime, out double shindo))
                {
                    g.FillEllipse(Shindo2ColorBrush(shindo), leftupperX, leftupperY, obsSize, obsSize);
                    if (config_draw.DrawObsShindo)
                        text += string.Format("{0:F1}", shindo);
                    infotextS.Add(shindo);
                    infotextN.Add(drawData.StationName);
                }
                g.DrawEllipse(new Pen(config_color.Obs_Border), leftupperX, leftupperY, obsSize, obsSize);
                g.DrawString(text, new Font(font, obsSize * 3 / 4, GraphicsUnit.Pixel), textColor, leftupperX + obsSize, leftupperY);
            }
            infotextS.Reverse();
            infotextN.Reverse();

            g.FillRectangle(new SolidBrush(config_color.InfoBack), mapS, 0, img.Width - mapS, mapS);
            g.DrawString(infohead, new Font(font, mapD30, GraphicsUnit.Pixel), textColor, mapS, 0);
            var infotextI = 0;
            for (var infoy = mapD20; infoy < mapS; infoy += infotextHei)
            {
                g.DrawString((infotextS[infotextI] >= 0 ? " " : "") + infotextS[infotextI].ToString("F1"), new Font(font, mapD36, GraphicsUnit.Pixel), textColor, mapD8p, infoy);
                g.DrawString(infotextN[infotextI], new Font(font, mapD36, GraphicsUnit.Pixel), textColor, mapD5p, infoy);
                g.FillRectangle(new SolidBrush(Shindo2Color_Int(infotextS[infotextI])), mapD60p, infoy, infotextHei, infotextHei);
                g.FillRectangle(new SolidBrush(Shindo2Color_Kmoni(infotextS[infotextI])), mapD60p + mapD180 + infotextHei, infoy, infotextHei, infotextHei);
                infotextI++;
            }
            g.DrawLine(new Pen(textColor, mapD1080), mapS, mapD20, img.Width, mapD20);
            g.DrawLine(new Pen(textColor, mapD1080), mapD16p, mapD16p, img.Width, mapS);

            g.DrawString(drawTime.ToString("yyyy/MM/dd HH:mm:ss.ff"), new Font(font, mapD24, GraphicsUnit.Pixel), textColor, 0, 0);
            g.DrawString("地図データ:気象庁", new Font(font, mapD28i, GraphicsUnit.Pixel), textColor, mapS - mdsize.Width, mapS - mdsize.Height);
            var savePath = $"{saveDir}\\{DateTime.Now:yyyyMMddHHmmss}.png";
            img.Save(savePath, ImageFormat.Png);
            g.Dispose();
            img.Dispose();
#else

            Directory.CreateDirectory(saveDir);
            var nowP = 0;
            var total = (int)((config_draw.EndTime - config_draw.StartTime) / config_draw.DrawSpan);
            var calStartT = DateTime.Now;
            var calStartT2 = DateTime.Now;

            for (var drawTime = config_draw.StartTime; drawTime < config_draw.EndTime; drawTime += config_draw.DrawSpan)
            {
                nowP++;
                var eta1 = (DateTime.Now - calStartT2) * (total - nowP);
                var eta2 = (DateTime.Now - calStartT) * (total / nowP) - (DateTime.Now - calStartT);
                if (eta1 > eta2)
                    (eta1, eta2) = (eta2, eta1);
                if (nowP % 100 == 0)
                    GC.Collect();
                ConWrite($"\r now:{drawTime:HH:mm:ss.ff} -> {nowP}/{total} ({nowP / (double)total * 100:F2}％)  eta:{(int)eta1.TotalMinutes}:{eta1:ss\\.ff}~{(int)eta2.TotalMinutes}:{eta2:ss\\.ff} (last draw:{(DateTime.Now - calStartT2).TotalMilliseconds}ms) ...", ConsoleColor.Green, false);
                calStartT2 = DateTime.Now;

                var img = new Bitmap(basemap);
                var g = Graphics.FromImage(img);

                var sortedInts = drawDatas.Datas_Draw.Values.OrderBy(x => x.TimeInt.TryGetValue(drawTime, out double value) ? value : double.MinValue);
                var infotextS = new List<double>();
                var infotextN = new List<string>();
                var infohead = $"観測点数:{drawDatas.Datas_Draw.Count}  震度計算秒数:{(drawDatas.CalPeriod == TimeSpan.Zero ? "--" : drawDatas.CalPeriod.TotalSeconds)}秒";
                foreach (var drawData in sortedInts)
                {
                    var leftupperX = (int)((drawData.StationLon - config_map.LonSta) * zoomW) - obsSizeHalf;
                    var leftupperY = (int)((config_map.LatEnd - drawData.StationLat) * zoomH) - obsSizeHalf;
                    var obsNameEdited = Datas.KNETKiKnetObsPoints.TryGetValue(drawData.StationName, out string? name) ? name : drawData.StationName;
                    var text = config_draw.DrawObsName ? obsNameEdited + " " : "";
                    if (drawData.TimeInt.TryGetValue(drawTime, out double shindo))
                    {
                        g.FillEllipse(Shindo2ColorBrush(shindo), leftupperX, leftupperY, obsSize, obsSize);
                        if (config_draw.DrawObsShindo)
                            text += string.Format("{0:F1}", shindo);
                        infotextS.Add(shindo);
                        infotextN.Add(obsNameEdited);
                    }
                    g.DrawEllipse(new Pen(config_color.Obs_Border), leftupperX, leftupperY, obsSize, obsSize);
                    g.DrawString(text, new Font(font, obsSize * 3 / 4, GraphicsUnit.Pixel), textColor, leftupperX + obsSize, leftupperY);
                }
                infotextS.Reverse();
                infotextN.Reverse();

                g.FillRectangle(new SolidBrush(config_color.InfoBack), mapS, 0, img.Width - mapS, mapS);
                g.DrawString(infohead, new Font(font, mapD30, GraphicsUnit.Pixel), textColor, mapS, 0);
                var infotextI = 0;
                for (var infoy = mapD20; infoy < mapS && infotextI < infotextS.Count; infoy += infotextHei)
                {
                    g.DrawString((infotextS[infotextI] >= 0 ? " " : "") + infotextS[infotextI].ToString("F1"), new Font(font, mapD36, GraphicsUnit.Pixel), textColor, mapD8p, infoy);
                    g.DrawString(infotextN[infotextI], new Font(font, mapD36, GraphicsUnit.Pixel), textColor, mapD5p, infoy);
                    g.FillRectangle(new SolidBrush(Shindo2Color_Int(infotextS[infotextI])), mapD60p, infoy, infotextHei, infotextHei);
                    g.FillRectangle(new SolidBrush(Shindo2Color_Kmoni(infotextS[infotextI])), mapD60p + mapD180 + infotextHei, infoy, infotextHei, infotextHei);
                    infotextI++;
                }
                g.DrawLine(new Pen(textColor, mapD1080), mapS, mapD20, img.Width, mapD20);
                g.DrawLine(new Pen(textColor, mapD1080), mapD16p, mapD16p, img.Width, mapS);

                g.DrawString(drawTime.ToString("yyyy/MM/dd HH:mm:ss.ff"), new Font(font, mapD24, GraphicsUnit.Pixel), textColor, 0, 0);
                g.DrawString("地図データ:気象庁", new Font(font, mapD28i, GraphicsUnit.Pixel), textColor, mapS - mdsize.Width, mapS - mdsize.Height);
                var savePath = $"{saveDir}\\{nowP:d4}.png";
                img.Save(savePath, ImageFormat.Png);
                g.Dispose();
                img.Dispose();
            }
            ConWrite($"{saveDir} に出力しました。", ConsoleColor.Green);
#endif
        }

#pragma warning disable CS8602 // null 参照の可能性があるものの逆参照です。
#pragma warning disable CS8604 // Null 参照引数の可能性があります。
        public static Bitmap Draw_Map()
        {
            var mapImg = new Bitmap(config_map.MapSize * 16 / 9, config_map.MapSize);
            var zoomW = config_map.MapSize / (config_map.LonEnd - config_map.LonSta);
            var zoomH = config_map.MapSize / (config_map.LatEnd - config_map.LatSta);
            var mapjson = MapSelecter();
            var g = Graphics.FromImage(mapImg);
            g.Clear(config_color.Map.Sea);
            var gPath = new GraphicsPath();
            gPath.StartFigure();
            foreach (var mapjson_feature in mapjson["features"].AsArray())
            {
                if (mapjson_feature["geometry"] == null)
                    continue;
                if ((string?)mapjson_feature["geometry"]["type"] == "Polygon")
                {
                    var points = mapjson_feature["geometry"]["coordinates"][0].AsArray().Select(mapjson_coordinate => new Point((int)(((double)mapjson_coordinate[0] - config_map.LonSta) * zoomW), (int)((config_map.LatEnd - (double)mapjson_coordinate[1]) * zoomH))).ToArray();
                    if (points.Length > 2)
                        gPath.AddPolygon(points);
                }
                else
                {
                    foreach (var mapjson_coordinates in mapjson_feature["geometry"]["coordinates"].AsArray())
                    {
                        var points = mapjson_coordinates[0].AsArray().Select(mapjson_coordinate => new Point((int)(((double)mapjson_coordinate[0] - config_map.LonSta) * zoomW), (int)((config_map.LatEnd - (double)mapjson_coordinate[1]) * zoomH))).ToArray();
                        if (points.Length > 2)
                            gPath.AddPolygon(points);
                    }
                }
            }
            g.FillPath(new SolidBrush(config_color.Map.Japan), gPath);
            g.DrawPath(new Pen(config_color.Map.Japan_Border, config_map.MapSize / 1080f), gPath);
            var mdsize = g.MeasureString("地図データ:気象庁", new Font(font, config_map.MapSize / 28, GraphicsUnit.Pixel));
            g.DrawString("地図データ:気象庁", new Font(font, config_map.MapSize / 28, GraphicsUnit.Pixel), new SolidBrush(config_color.Text), config_map.MapSize - mdsize.Width, config_map.MapSize - mdsize.Height);
            g.Dispose();
            return mapImg;
        }
#pragma warning restore CS8602 // null 参照の可能性があるものの逆参照です。
#pragma warning restore CS8604 // Null 参照引数の可能性があります。

#pragma warning disable CS8603 // Null 参照戻り値である可能性があります。
        /// <summary>
        /// 設定からマップのデータを選択します。
        /// </summary>
        /// <returns>設定に対応する地図データ(JsonNode)</returns>
        /// <exception cref="Exception">マップ設定が正しくない場合</exception>
        public static JsonNode MapSelecter()
        {
            return config_map.MapType switch
            {
                Config_Map.MapKind.map_pref_min => map_pref_min,
                Config_Map.MapKind.map_pref_mid => map_pref_mid,
                Config_Map.MapKind.map_loca_min => map_loca_min,
                Config_Map.MapKind.map_loca_mid => map_loca_mid,
                Config_Map.MapKind.map_city_min => map_city_min,
                Config_Map.MapKind.map_city_mid => map_city_mid,
                _ => throw new Exception("マップ設定が正しくありません。"),
            };
        }
#pragma warning restore CS8603 // Null 参照戻り値である可能性があります。

        /// <summary>
        /// 震度から描画色を求めます。
        /// </summary>
        /// <param name="shindo">震度</param>
        /// <returns>震度に対応する色</returns>
        public static Color Shindo2Color(double? shindo)
        {
            shindo ??= double.NaN;
            return config_color.Obs_UseIntColor ? Shindo2Color_Int(shindo) : Shindo2Color_Kmoni(shindo); ;
        }

        /// <summary>
        /// 震度から描画色を求めます。
        /// </summary>
        /// <param name="shindo">震度</param>
        /// <returns>震度に対応する色</returns>
        public static Color Shindo2Color_Int(double? shindo)
        {
            shindo ??= double.NaN;
            if (shindo < 0.5)
                return config_color.IntColor.S0;
            else if (shindo < 1.5)
                return config_color.IntColor.S1;
            else if (shindo < 2.5)
                return config_color.IntColor.S2;
            else if (shindo < 3.5)
                return config_color.IntColor.S3;
            else if (shindo < 4.5)
                return config_color.IntColor.S4;
            else if (shindo < 5.0)
                return config_color.IntColor.S5;
            else if (shindo < 5.5)
                return config_color.IntColor.S6;
            else if (shindo < 6.0)
                return config_color.IntColor.S7;
            else if (shindo < 6.5)
                return config_color.IntColor.S8;
            else if (shindo >= 6.5)
                return config_color.IntColor.S9;
            else
                return Datas.Shindo2KColor[double.NaN];
        }

        /// <summary>
        /// 震度から描画色を求めます。
        /// </summary>
        /// <param name="shindo">震度</param>
        /// <returns>震度に対応する色</returns>
        public static Color Shindo2Color_Kmoni(double? shindo)
        {
            shindo ??= double.NaN;
            if (shindo > 7d)
                shindo = 7d;
            if (shindo < -3d)
                shindo = -3d;
            return Datas.Shindo2KColor[shindo ?? double.NaN];
        }

        /// <summary>
        /// 震度から描画色を求めます。
        /// </summary>
        /// <param name="shindo">震度</param>
        /// <returns>震度に対応する色</returns>
        public static SolidBrush Shindo2ColorBrush(double? shindo)
        {
            return new SolidBrush(Shindo2Color(shindo));
        }
    }


    public class Config_Draw
    {

        /// <summary>
        /// 描画開始日時
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 描画終了日時
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// 描画間隔
        /// </summary>
        public TimeSpan DrawSpan { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// 画像の高さ1080での観測点のサイズ
        /// </summary>
        public int ObsSize { get; set; } = 7;

        /// <summary>
        /// 観測点名を観測点アイコン右に描画するか
        /// </summary>
        public bool DrawObsName { get; set; } = false;

        /// <summary>
        /// 観測点震度を観測点アイコン右に描画するか
        /// </summary>
        public bool DrawObsShindo { get; set; } = false;
    }

    /// <summary>
    /// 地図描画の設定
    /// </summary>
    public class Config_Map
    {
        /// <summary>
        /// 画像の高さ
        /// </summary>
        public int MapSize { get; set; } = 1080;

        /// <summary>
        /// 緯度の始点
        /// </summary>
        public double LatSta { get; set; } = 22.5;

        /// <summary>
        /// 緯度の終点
        /// </summary>
        public double LatEnd { get; set; } = 47.5;

        /// <summary>
        /// 経度の始点
        /// </summary>
        public double LonSta { get; set; } = 122.5;

        /// <summary>
        /// 経度の終点
        /// </summary>
        public double LonEnd { get; set; } = 147.5;

        /// <summary>
        /// マップの種類
        /// </summary>
        public MapKind MapType = MapKind.map_pref_min;

        /// <summary>
        /// マップの種類
        /// </summary>
        public enum MapKind
        {
            /// <summary>
            /// AreaInformationPrefectureEarthquake_GIS_*_01
            /// </summary>
            map_pref_min = 11,

            /// <summary>
            /// AreaInformationPrefectureEarthquake_GIS_*_1
            /// </summary>
            map_pref_mid = 12,

            /// <summary>
            /// AreaForecastLocalE_GIS_*_01
            /// </summary>
            map_loca_min = 21,

            /// <summary>
            /// AreaForecastLocalE_GIS_*_1
            /// </summary>
            map_loca_mid = 22,

            /// <summary>
            /// AreaInformationCity_quake_GIS_*_01
            /// </summary>
            map_city_min = 31,

            /// <summary>
            /// AreaInformationCity_quake_GIS_*_1
            /// </summary>
            map_city_mid = 32
        }
    }

    /// <summary>
    /// 描画色の設定
    /// </summary>
    public class Config_Color
    {
        /// <summary>
        /// 地図の色
        /// </summary>
        public MapColor Map { get; set; } = new MapColor();

        /// <summary>
        /// 地図の色
        /// </summary>
        public class MapColor
        {
            /// <summary>
            /// 海洋の塗りつぶし色
            /// </summary>
            public Color Sea { get; set; } = Color.FromArgb(30, 30, 60);
            /*
            /// <summary>
            /// 世界(日本除く)の塗りつぶし色
            /// </summary>
            public Color World { get; set; } = Color.FromArgb(100, 100, 150);
            
            /// <summary>
            /// 世界(日本除く)の境界線色
            /// </summary>
            public Color World_Border { get; set; }
            */
            /// <summary>
            /// 日本の塗りつぶし色
            /// </summary>
            public Color Japan { get; set; } = Color.FromArgb(90, 90, 120);

            /// <summary>
            /// 日本の境界線色
            /// </summary>
            public Color Japan_Border { get; set; } = Color.FromArgb(127, 255, 255, 255);
        }

        /// <summary>
        /// 右側部分背景色
        /// </summary>
        public Color InfoBack { get; set; } = Color.FromArgb(30, 60, 90);

        /// <summary>
        /// 右側部分等テキスト色
        /// </summary>
        public Color Text { get; set; } = Color.FromArgb(255, 255, 255);

        /// <summary>
        /// 観測点円の色を震度別色にするか(falseで強震モニタ色)
        /// </summary>
        public bool Obs_UseIntColor { get; set; } = false;

        /// <summary>
        /// 観測点の円(塗りつぶさないほう)の色
        /// </summary>
        public Color Obs_Border { get; set; } = Color.FromArgb(0, 127, 127, 127);

        /// <summary>
        /// 震度別色
        /// </summary>
        public ShindoColor IntColor { get; set; } = new();

        /// <summary>
        /// 震度別色
        /// </summary>
        public class ShindoColor
        {
            /// <summary>
            /// 震度0
            /// </summary>
            public Color S0 { get; set; } = Color.FromArgb(80, 90, 100);

            /// <summary>
            /// 震度1
            /// </summary>
            public Color S1 { get; set; } = Color.FromArgb(60, 80, 100);

            /// <summary>
            /// 震度2
            /// </summary>
            public Color S2 { get; set; } = Color.FromArgb(45, 90, 180);

            /// <summary>
            /// 震度3
            /// </summary>
            public Color S3 { get; set; } = Color.FromArgb(50, 175, 175);

            /// <summary>
            /// 震度4
            /// </summary>
            public Color S4 { get; set; } = Color.FromArgb(240, 240, 60);

            /// <summary>
            /// 震度5-
            /// </summary>
            public Color S5 { get; set; } = Color.FromArgb(250, 150, 0);

            /// <summary>
            /// 震度5+
            /// </summary>
            public Color S6 { get; set; } = Color.FromArgb(250, 75, 0);

            /// <summary>
            /// 震度6-
            /// </summary>
            public Color S7 { get; set; } = Color.FromArgb(200, 0, 0);

            /// <summary>
            /// 震度6+
            /// </summary>
            public Color S8 { get; set; } = Color.FromArgb(100, 0, 0);

            /// <summary>
            /// 震度7
            /// </summary>
            public Color S9 { get; set; } = Color.FromArgb(100, 0, 100);
        }
    }
}
