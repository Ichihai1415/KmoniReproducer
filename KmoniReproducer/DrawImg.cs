using System.Drawing;
using System.Drawing.Drawing2D;
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
            var basemap = Draw_Map();

            for (var drawTime = config_draw.StartTime; drawTime < config_draw.EndTime; drawTime += config_draw.DrawSpan)
            {
                var img = new Bitmap(basemap);
                var g = Graphics.FromImage(img);
                foreach (var drawData in drawDatas.Datas_Draw)
                {




                }





            }


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
                Config_Map.MapKind.map_pref_min => map_city_min,
                Config_Map.MapKind.map_pref_mid => map_city_mid,
                Config_Map.MapKind.map_loca_min => map_loca_min,
                Config_Map.MapKind.map_loca_mid => map_loca_mid,
                Config_Map.MapKind.map_city_min => map_city_min,
                Config_Map.MapKind.map_city_mid => map_city_mid,
                _ => throw new Exception("マップ設定が正しくありません。"),
            };
        }
#pragma warning restore CS8603 // Null 参照戻り値である可能性があります。


        /// <summary>
        /// 震度から強震モニタ震度色に変換します。-3.0以下は-3.0,7.0以上は7.0,データなしの場合-5.0を指定してください。
        /// </summary>
        /// <remarks>データ:https://github.com/ingen084/KyoshinShindoColorMap</remarks>
        public readonly static Dictionary<double, Color> Sindo2Color = new()
        {
            { -5.0, Color.FromArgb(0, 0, 0, 0)   },
            { -3.0, Color.FromArgb(0, 0, 205)    },
            { -2.9, Color.FromArgb(0, 7, 209)    },
            { -2.8, Color.FromArgb(0, 14, 214)   },
            { -2.7, Color.FromArgb(0, 21, 218)   },
            { -2.6, Color.FromArgb(0, 28, 223)   },
            { -2.5, Color.FromArgb(0, 36, 227)   },
            { -2.4, Color.FromArgb(0, 43, 231)   },
            { -2.3, Color.FromArgb(0, 50, 236)   },
            { -2.2, Color.FromArgb(0, 57, 240)   },
            { -2.1, Color.FromArgb(0, 64, 245)   },
            { -2.0, Color.FromArgb(0, 72, 250)   },
            { -1.9, Color.FromArgb(0, 85, 238)   },
            { -1.8, Color.FromArgb(0, 99, 227)   },
            { -1.7, Color.FromArgb(0, 112, 216)  },
            { -1.6, Color.FromArgb(0, 126, 205)  },
            { -1.5, Color.FromArgb(0, 140, 194)  },
            { -1.4, Color.FromArgb(0, 153, 183)  },
            { -1.3, Color.FromArgb(0, 167, 172)  },
            { -1.2, Color.FromArgb(0, 180, 161)  },
            { -1.1, Color.FromArgb(0, 194, 150)  },
            { -1.0, Color.FromArgb(0, 208, 139)  },
            { -0.9, Color.FromArgb(6, 212, 130)  },
            { -0.8, Color.FromArgb(12, 216, 121) },
            { -0.7, Color.FromArgb(18, 220, 113) },
            { -0.6, Color.FromArgb(25, 224, 104) },
            { -0.5, Color.FromArgb(31, 228, 96)  },
            { -0.4, Color.FromArgb(37, 233, 88)  },
            { -0.3, Color.FromArgb(44, 237, 79)  },
            { -0.2, Color.FromArgb(50, 241, 71)  },
            { -0.1, Color.FromArgb(56, 245, 62)  },
            {  0.0, Color.FromArgb(63, 250, 54)  },
            {  0.1, Color.FromArgb(75, 250, 49)  },
            {  0.2, Color.FromArgb(88, 250, 45)  },
            {  0.3, Color.FromArgb(100, 251, 4)  },
            {  0.4, Color.FromArgb(113, 251, 37) },
            {  0.5, Color.FromArgb(125, 252, 33) },
            {  0.6, Color.FromArgb(138, 252, 28) },
            {  0.7, Color.FromArgb(151, 253, 24) },
            {  0.8, Color.FromArgb(163, 253, 20) },
            {  0.9, Color.FromArgb(176, 254, 16) },
            {  1.0, Color.FromArgb(189, 255, 12) },
            {  1.1, Color.FromArgb(195, 254, 10) },
            {  1.2, Color.FromArgb(202, 254, 9)  },
            {  1.3, Color.FromArgb(208, 254, 8)  },
            {  1.4, Color.FromArgb(215, 254, 7)  },
            {  1.5, Color.FromArgb(222, 255, 5)  },
            {  1.6, Color.FromArgb(228, 254, 4)  },
            {  1.7, Color.FromArgb(235, 255, 3)  },
            {  1.8, Color.FromArgb(241, 254, 2)  },
            {  1.9, Color.FromArgb(248, 255, 1)  },
            {  2.0, Color.FromArgb(255, 255, 0)  },
            {  2.1, Color.FromArgb(254, 251, 0)  },
            {  2.2, Color.FromArgb(254, 248, 0)  },
            {  2.3, Color.FromArgb(254, 244, 0)  },
            {  2.4, Color.FromArgb(254, 241, 0)  },
            {  2.5, Color.FromArgb(255, 238, 0)  },
            {  2.6, Color.FromArgb(254, 234, 0)  },
            {  2.7, Color.FromArgb(255, 231, 0)  },
            {  2.8, Color.FromArgb(254, 227, 0)  },
            {  2.9, Color.FromArgb(255, 224, 0)  },
            {  3.0, Color.FromArgb(255, 221, 0)  },
            {  3.1, Color.FromArgb(254, 213, 0)  },
            {  3.2, Color.FromArgb(254, 205, 0)  },
            {  3.3, Color.FromArgb(254, 197, 0)  },
            {  3.4, Color.FromArgb(254, 190, 0)  },
            {  3.5, Color.FromArgb(255, 182, 0)  },
            {  3.6, Color.FromArgb(254, 174, 0)  },
            {  3.7, Color.FromArgb(255, 167, 0)  },
            {  3.8, Color.FromArgb(254, 159, 0)  },
            {  3.9, Color.FromArgb(255, 151, 0)  },
            {  4.0, Color.FromArgb(255, 144, 0)  },
            {  4.1, Color.FromArgb(254, 136, 0)  },
            {  4.2, Color.FromArgb(254, 128, 0)  },
            {  4.3, Color.FromArgb(254, 121, 0)  },
            {  4.4, Color.FromArgb(254, 113, 0)  },
            {  4.5, Color.FromArgb(255, 106, 0)  },
            {  4.6, Color.FromArgb(254, 98, 0)   },
            {  4.7, Color.FromArgb(255, 90, 0)   },
            {  4.8, Color.FromArgb(254, 83, 0)   },
            {  4.9, Color.FromArgb(255, 75, 0)   },
            {  5.0, Color.FromArgb(255, 68, 0)   },
            {  5.1, Color.FromArgb(254, 61, 0)   },
            {  5.2, Color.FromArgb(253, 54, 0)   },
            {  5.3, Color.FromArgb(252, 47, 0)   },
            {  5.4, Color.FromArgb(251, 40, 0)   },
            {  5.5, Color.FromArgb(250, 33, 0)   },
            {  5.6, Color.FromArgb(249, 27, 0)   },
            {  5.7, Color.FromArgb(248, 20, 0)   },
            {  5.8, Color.FromArgb(247, 13, 0)   },
            {  5.9, Color.FromArgb(246, 6, 0)    },
            {  6.0, Color.FromArgb(245, 0, 0)    },
            {  6.1, Color.FromArgb(238, 0, 0)    },
            {  6.2, Color.FromArgb(230, 0, 0)    },
            {  6.3, Color.FromArgb(223, 0, 0)    },
            {  6.4, Color.FromArgb(215, 0, 0)    },
            {  6.5, Color.FromArgb(208, 0, 0)    },
            {  6.6, Color.FromArgb(200, 0, 0)    },
            {  6.7, Color.FromArgb(192, 0, 0)    },
            {  6.8, Color.FromArgb(185, 0, 0)    },
            {  6.9, Color.FromArgb(177, 0, 0)    },
            {  7.0, Color.FromArgb(170, 0, 0)    }
        };
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
        public TimeSpan DrawSpan { get; set; }

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
        public Color Obs_Border { get; set; } = Color.FromArgb(127, 127, 127);

        /// <summary>
        /// 震度別色
        /// </summary>
        public SindoColor IntColor { get; set; } = new();

        /// <summary>
        /// 震度別色
        /// </summary>
        public class SindoColor
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
