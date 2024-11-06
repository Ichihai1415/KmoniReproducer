﻿using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using static KmoniReproducer.Program;

namespace KmoniReproducer
{
    /// <summary>
    /// 描画関係
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class DrawImg
    {
        /// <summary>
        /// 描画します。
        /// </summary>
        /// <param name="drawDatas">描画するデータ</param>
        public static void Draw(Data_Draw drawDatas)
        {
            var saveDir = $"output\\images\\{config_draw.StartTime:yyyyMMddHHmmss}\\{config_draw.StartTime:yyyyMMddHHmmss}-{DateTime.Now:yyyyMMddHHmmss}";
            var baseMap = Draw_Map();
            var textColor = new SolidBrush(config_color.Text);

            var saveConfigDir = saveDir + "\\config";
            Directory.CreateDirectory(saveConfigDir);
            File.WriteAllText(saveConfigDir + "\\config_draw.json", JsonSerializer.Serialize(config_draw, serializeIntend));
            File.WriteAllText(saveConfigDir + "\\config_map.json", JsonSerializer.Serialize(config_map, serializeIntend));
            File.WriteAllText(saveConfigDir + "\\config_color.json", JsonSerializer.Serialize(config_color, serializeIntend));

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

            var mdSize = new SizeF();
            var infoTextHei = 0f;
            using (var img_tmp = new Bitmap(100, 100))
            using (var g_tmp = Graphics.FromImage(img_tmp))
            {
                mdSize = g_tmp.MeasureString("地図データ:気象庁", new Font(font!, mapD28i, GraphicsUnit.Pixel));
                infoTextHei = g_tmp.MeasureString("あ", new Font(font!, mapD36, GraphicsUnit.Pixel)).Height;
            }
            var mapWmS = baseMap.Width - mapS;
            var mapSmMH = mapS - mdSize.Height;

            var nowP = 0;
            var total = (int)((config_draw.EndTime - config_draw.StartTime) / config_draw.DrawSpan);
            var calStartT = DateTime.Now;
            var calStartT2 = DateTime.Now;
            foreach (var drawData in drawDatas.Datas_Draw.Values)
                drawData.StationName = Datas.KNETKiKnetObsPoints.TryGetValue(drawData.StationName, out string? name) ? $"{name} ({drawData.StationName})" : drawData.StationName;

            ConWrite($"{DateTime.Now:HH:mm:ss.ffff} 描画中...", ConsoleColor.Blue);
            ConWrite($"{config_draw.StartTime:yyyy/MM/dd  HH:mm:ss.ff} ~ {config_draw.EndTime:HH:mm:ss.ff}  span:{config_draw.DrawSpan:mm\\:ss\\.ff}   dataCount(int):{drawDatas.Datas_Draw.Count}", ConsoleColor.Green);
            for (var drawTime = config_draw.StartTime; drawTime < config_draw.EndTime; drawTime += config_draw.DrawSpan)
            {
                nowP++;
                if (nowP % 10 == 0)
                {
                    var eta = (DateTime.Now - calStartT2) / 10d * (total - nowP);
                    ConWrite($"\r└ {drawTime:HH:mm:ss.ff} -> {nowP}/{total} ({nowP / (double)total * 100:F2}％)  ETA:{(int)eta.TotalMinutes}:{eta:ss\\.ff} (last 10draw:{(DateTime.Now - calStartT2).TotalMilliseconds}ms)", ConsoleColor.Green, false);
                    ConsoleClearRight();
                    calStartT2 = DateTime.Now;
                    if (nowP % 100 == 0)
                        GC.Collect();
                }

                var sortedInts = drawDatas.Datas_Draw.Values.OrderBy(x => x.TimeInt.TryGetValue(drawTime, out double value) ? value : double.MinValue);
                var validInts = drawDatas.Datas_Draw.Values.Where(x => x.TimeInt.TryGetValue(drawTime, out double value));
                var mapLatSta = config_map.LatSta;
                var mapLatEnd = config_map.LatEnd;
                var mapLonSta = config_map.LonSta;
                var mapLonEnd = config_map.LonEnd;

                if (sortedInts.Last().TimeInt.TryGetValue(drawTime, out double maxInt))
                    if (config_draw.AutoZoomMin != -9)//min有効
                    {
                        var viewAreaIntsM = validInts.Where(x => Shindo2Int(x.TimeInt[drawTime]) >= config_draw.AutoZoomMin);
                        if (viewAreaIntsM.Any())//minある difまだ
                        {
                            if (config_draw.AutoZoomMinDif != -9)//minある dif有効
                            {
                                var maxIntMDiff = Shindo2Int(maxInt) - config_draw.AutoZoomMinDif;//5,1=>4 (4以上)
                                var viewAreaIntsD = viewAreaIntsM.Where(x => Shindo2Int(x.TimeInt[drawTime]) >= maxIntMDiff);
                                if (viewAreaIntsD.Any())//minある difある
                                {
                                    mapLatSta = viewAreaIntsD.Min(x => x.StationLat);
                                    mapLatEnd = viewAreaIntsD.Max(x => x.StationLat);
                                    mapLonSta = viewAreaIntsD.Min(x => x.StationLon);
                                    mapLonEnd = viewAreaIntsD.Max(x => x.StationLon);
                                }
                            }
                            else//minある difない(そのまま)
                            {
                                mapLatSta = viewAreaIntsM.Min(x => x.StationLat);
                                mapLatEnd = viewAreaIntsM.Max(x => x.StationLat);
                                mapLonSta = viewAreaIntsM.Min(x => x.StationLon);
                                mapLonEnd = viewAreaIntsM.Max(x => x.StationLon);
                            }
                            AreaCorrect(ref mapLatSta, ref mapLatEnd, ref mapLonSta, ref mapLonEnd);
                            baseMap = Draw_Map(mapLatSta, mapLatEnd, mapLonSta, mapLonEnd);
                        }
                        else if (config_draw.AutoZoomMinDif != -9)//minない dif有効
                        {
                            var maxIntMDiff = Shindo2Int(maxInt) - config_draw.AutoZoomMinDif;
                            var viewAreaIntsD = validInts.Where(x => Shindo2Int(x.TimeInt[drawTime]) >= config_draw.AutoZoomMin).Where(x => Shindo2Int(x.TimeInt[drawTime]) >= maxIntMDiff);
                            if (viewAreaIntsD.Any())//minない difある
                            {
                                mapLatSta = viewAreaIntsD.Min(x => x.StationLat);
                                mapLatEnd = viewAreaIntsD.Max(x => x.StationLat);
                                mapLonSta = viewAreaIntsD.Min(x => x.StationLon);
                                mapLonEnd = viewAreaIntsD.Max(x => x.StationLon);
                                AreaCorrect(ref mapLatSta, ref mapLatEnd, ref mapLonSta, ref mapLonEnd);
                                baseMap = Draw_Map(mapLatSta, mapLatEnd, mapLonSta, mapLonEnd);
                            }
                            else//minない difない
                                baseMap = Draw_Map();
                        }
                        else//minない dif無効
                            baseMap = Draw_Map();
                    }
                    else if (config_draw.AutoZoomMinDif != -9)//min無効 dif有効
                    {
                        var maxIntMDiff = Shindo2Int(maxInt) - config_draw.AutoZoomMinDif;
                        var viewAreaIntD = validInts.Where(x => Shindo2Int(x.TimeInt.TryGetValue(drawTime, out double value) ? value : null) >= maxIntMDiff);
                        if (viewAreaIntD.Any())//min無効+difある
                        {
                            mapLatSta = viewAreaIntD.Min(x => x.StationLat);
                            mapLatEnd = viewAreaIntD.Max(x => x.StationLat);
                            mapLonSta = viewAreaIntD.Min(x => x.StationLon);
                            mapLonEnd = viewAreaIntD.Max(x => x.StationLon);
                            AreaCorrect(ref mapLatSta, ref mapLatEnd, ref mapLonSta, ref mapLonEnd);
                            baseMap = Draw_Map(mapLatSta, mapLatEnd, mapLonSta, mapLonEnd);
                        }
                        else//min無効+ない
                            baseMap = Draw_Map();
                    }

                var zoomW = mapS / (mapLonEnd - mapLonSta);
                var zoomH = mapS / (mapLatEnd - mapLatSta);//既定は43.2
                var zoom = Math.Min(zoomW, zoomH);

                var obsSize = (float)(Math.Pow(zoom / 43.2, 0.5) * 7);
                var obsSizeHalf = obsSize / 2;

                using var img = new Bitmap(baseMap);
                using var g = Graphics.FromImage(img);

                var infoTextS = new List<double>();
                var infoTextN = new List<string>();
                var infoHead = $"観測点数:{drawDatas.Datas_Draw.Count}  震度計算秒数:{(drawDatas.CalPeriod == TimeSpan.Zero ? "--" : drawDatas.CalPeriod.TotalSeconds)}秒";
                foreach (var drawData in sortedInts)
                {
                    var leftUpperX = (float)((drawData.StationLon - mapLonSta) * zoomW) - obsSizeHalf;
                    var leftUpperY = (float)((mapLatEnd - drawData.StationLat) * zoomH) - obsSizeHalf;
                    var obsNameEdited = drawData.StationName;
                    var text = config_draw.DrawObsName ? obsNameEdited + " " : "";
                    if (drawData.TimeInt.TryGetValue(drawTime, out double shindo))
                    {
                        g.FillEllipse(Shindo2ColorBrush(shindo), leftUpperX, leftUpperY, obsSize, obsSize);
                        if (config_draw.DrawObsShindo)
                            text += string.Format("{0:F1}", shindo);
                        infoTextS.Add(shindo);
                        infoTextN.Add(obsNameEdited);
                    }
                    g.DrawEllipse(new Pen(config_color.Obs_Border), leftUpperX, leftUpperY, obsSize, obsSize);
                    g.DrawString(text, new Font(font!, obsSize * 3 / 4, GraphicsUnit.Pixel), textColor, leftUpperX + obsSize, leftUpperY);
                }


                if (config_draw.DrawPSWave)
                {
                    var eqs = drawDatas.Earthquakes.Length == 0 ? [drawDatas.MainEq] : drawDatas.Earthquakes;

                    foreach (var eq in eqs)
                    {
                        var seconds = (drawTime - eq.OriginTime).TotalSeconds;
                        if (eq.OriginTime != DateTime.MinValue && seconds > 0)
                        {
                            var (pLatLon, sLatLon) = psd!.GetLatLonList(eq.Depth, seconds, eq.HypoLat, eq.HypoLon, 360);
                            if (pLatLon.Count > 2)//基本360、失敗時0か1
                            {
                                var pPts = pLatLon.Select(x => new Point((int)((x.Lon - mapLonSta) * zoomW), (int)((mapLatEnd - x.Lat) * zoomH))).ToList()!;
                                pPts.Add(pPts[0]);
                                g.DrawPolygon(new Pen(config_color.PSWaveColor.PDrawColor, config_color.PSWaveColor.PWidth), pPts.ToArray());
                                //if (seconds == 20)
                                //throw new Exception();
                            }
                            if (sLatLon.Count > 2)
                            {
                                var sPts = sLatLon.Select(x => new Point((int)((x.Lon - mapLonSta) * zoomW), (int)((mapLatEnd - x.Lat) * zoomH))).ToList()!;
                                sPts.Add(sPts[0]);
                                g.DrawPolygon(new Pen(config_color.PSWaveColor.SDrawColor, config_color.PSWaveColor.SWidth), sPts.ToArray());
                                if (config_color.PSWaveColor.SFillColor.A != 0)
                                    g.FillPolygon(new SolidBrush(config_color.PSWaveColor.SFillColor), sPts.ToArray());
                            }
                            var hypoLength = config_color.HypoLength / 2;
                            var hypoPt = new Point((int)((eq.HypoLon - mapLonSta) * zoomW), (int)((mapLatEnd - eq.HypoLat) * zoomH));
                            g.DrawLine(new Pen(config_color.HypoColor, config_color.HypoWidth), hypoPt.X - hypoLength, hypoPt.Y - hypoLength, hypoPt.X + hypoLength, hypoPt.Y + hypoLength);
                            g.DrawLine(new Pen(config_color.HypoColor, config_color.HypoWidth), hypoPt.X + hypoLength, hypoPt.Y - hypoLength, hypoPt.X - hypoLength, hypoPt.Y + hypoLength);
                        }
                    }
                }

                infoTextS.Reverse();
                infoTextN.Reverse();

                g.FillRectangle(new SolidBrush(config_color.InfoBack), mapS, 0, mapWmS, mapS);
                g.DrawString(infoHead, new Font(font!, mapD30, GraphicsUnit.Pixel), textColor, mapS, 0);
                var infoTextI = 0;
                for (var infoY = mapD20; infoY < mapS && infoTextI < infoTextS.Count; infoY += infoTextHei)
                {
                    g.DrawString((infoTextS[infoTextI] >= 0 ? " " : "") + infoTextS[infoTextI].ToString("F1"), new Font(font!, mapD36, GraphicsUnit.Pixel), textColor, mapD8p, infoY);
                    g.DrawString(infoTextN[infoTextI], new Font(font!, mapD36, GraphicsUnit.Pixel), textColor, mapD5p, infoY);
                    g.FillRectangle(new SolidBrush(Shindo2Color_Int(infoTextS[infoTextI])), mapD60p, infoY, infoTextHei, infoTextHei);
                    g.FillRectangle(new SolidBrush(Shindo2Color_Kmoni(infoTextS[infoTextI])), mapD60p + mapD180 + infoTextHei, infoY, infoTextHei, infoTextHei);
                    infoTextI++;
                }
                g.DrawLine(new Pen(textColor, mapD1080), mapS, mapD20, img.Width, mapD20);
                g.DrawLine(new Pen(textColor, mapD1080), mapD16p, mapD16p, img.Width, mapS);

                g.FillRectangle(new SolidBrush(config_color.InfoBack), mapS, mapSmMH, mapWmS, mapS);
                g.DrawLine(new Pen(textColor, mapD1080), mapS, mapSmMH, img.Width, mapSmMH);
                g.DrawString(drawTime.ToString("yyyy/MM/dd HH:mm:ss.ff"), new Font(font!, mapD28i, GraphicsUnit.Pixel), textColor, mapS, mapSmMH);
                g.DrawString("地図データ:気象庁", new Font(font!, mapD28i, GraphicsUnit.Pixel), textColor, img.Width - mdSize.Width, mapSmMH);
                var savePath = $"{saveDir}\\{nowP:d4}.png";
                img.Save(savePath, ImageFormat.Png);
            }
            ConWrite($"\n{DateTime.Now:HH:mm:ss.ffff} 描画完了", ConsoleColor.Blue);
            ConWrite($"{saveDir} に出力しました。", ConsoleColor.Green);

        remake:
            if (int.TryParse(ConAsk("ffmpegで動画を作成する場合、fps(フレームレート)を入力してください。複数作成したい場合は作成後またこの表示が出るので毎回入力してください。ffmpeg.exeのパスが通っている必要があります。\n数値への変換に失敗したら終了します。ミス防止のため空文字入力はできません。数字以外を何か入力してください。"), out int f))
            {
                ConWrite($"ffmpeg -framerate {f} -i \"{saveDir}\\%04d.png\" -vcodec libx264 -pix_fmt yuv420p -r {f} _output_{f}.mp4", ConsoleColor.Green);
                ConWrite();
                using var pro = Process.Start("ffmpeg", $"-framerate {f} -i \"{saveDir}\\%04d.png\" -vcodec libx264 -pix_fmt yuv420p -r {f} \"{saveDir}\\_output_{f}.mp4\"");
                pro.WaitForExit();
                goto remake;
            }
        }

#pragma warning disable CS8602 // null 参照の可能性があるものの逆参照です。
#pragma warning disable CS8604 // Null 参照引数の可能性があります。
        /// <summary>
        /// 地図を描画します。緯度経度を指定することができます(指定しない場合設定を参照します)。
        /// </summary>
        /// <remarks>事前に補正してください。</remarks>
        /// <param name="latSta">緯度の始点</param>
        /// <param name="latEnd">緯度の終点</param>
        /// <param name="lonSta">経度の始点</param>
        /// <param name="lonEnd">経度の終点</param>
        /// <returns>描画した地図(右側情報部分はそのまま)</returns>
        public static Bitmap Draw_Map(double latSta = -200, double latEnd = -200, double lonSta = -200, double lonEnd = -200)
        {
            if (latSta == -200)
                latSta = config_map.LatSta;
            if (latEnd == -200)
                latEnd = config_map.LatEnd;
            if (lonSta == -200)
                lonSta = config_map.LonSta;
            if (lonEnd == -200)
                lonEnd = config_map.LonEnd;

            var mapImg = new Bitmap(config_map.MapSize * 16 / 9, config_map.MapSize);
            var zoomW = config_map.MapSize / (lonEnd - lonSta);
            var zoomH = config_map.MapSize / (latEnd - latSta);
            using var g = Graphics.FromImage(mapImg);
            g.Clear(config_color.Map.Sea);
            var mapType = (int)config_map.MapType / 10;
            var mapDetail = (int)config_map.MapType % 10;

            for (var i = 1; i <= mapType; i++)
            {
                var mapjson = MapSelector((Config_Map.MapKind)(i * 10 + mapDetail));
                using var gPath = new GraphicsPath();
                gPath.StartFigure();
                foreach (var mapjson_feature in mapjson["features"].AsArray())
                {
                    if (mapjson_feature["geometry"] == null)
                        continue;
                    if ((string?)mapjson_feature["geometry"]["type"] == "Polygon")
                    {
                        var points = mapjson_feature["geometry"]["coordinates"][0].AsArray().Select(mapjson_coordinate => new Point((int)(((double)mapjson_coordinate[0] - lonSta) * zoomW), (int)((latEnd - (double)mapjson_coordinate[1]) * zoomH))).ToArray();
                        if (points.Length > 2)
                            gPath.AddPolygon(points);
                    }
                    else
                    {
                        foreach (var mapjson_coordinates in mapjson_feature["geometry"]["coordinates"].AsArray())
                        {
                            var points = mapjson_coordinates[0].AsArray().Select(mapjson_coordinate => new Point((int)(((double)mapjson_coordinate[0] - lonSta) * zoomW), (int)((latEnd - (double)mapjson_coordinate[1]) * zoomH))).ToArray();
                            if (points.Length > 2)
                                gPath.AddPolygon(points);
                        }
                    }
                }
                if (i == 1)
                    g.FillPath(new SolidBrush(config_color.Map.Japan), gPath);
                g.DrawPath(new Pen(config_color.Map.Japan_Border, config_map.MapSize / 1080f * (mapType + 1 - i)), gPath);
            }
            //var mdSize = g.MeasureString("地図データ:気象庁", new Font(font, config_map.MapSize / 28, GraphicsUnit.Pixel));
            //g.DrawString("地図データ:気象庁", new Font(font, config_map.MapSize / 28, GraphicsUnit.Pixel), new SolidBrush(config_color.Text), config_map.MapSize - mdSize.Width, config_map.MapSize - mdSize.Height);
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
        public static JsonNode MapSelector()
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

#pragma warning disable CS8603 // Null 参照戻り値である可能性があります。
        /// <summary>
        /// マップのデータを選択します。
        /// </summary>
        /// <param name="mapKind">マップの種類</param>
        /// <returns>引数に対応する地図データ(JsonNode)</returns>
        /// <exception cref="Exception">マップ設定が正しくない場合</exception>
        public static JsonNode MapSelector(Config_Map.MapKind mapKind)
        {
            return mapKind switch
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
            return config_draw.Obs_UseIntColor ? Shindo2Color_Int(shindo) : Shindo2Color_Kmoni(shindo); ;
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
                return config_color.ShindoColor.S0;
            else if (shindo < 1.5)
                return config_color.ShindoColor.S1;
            else if (shindo < 2.5)
                return config_color.ShindoColor.S2;
            else if (shindo < 3.5)
                return config_color.ShindoColor.S3;
            else if (shindo < 4.5)
                return config_color.ShindoColor.S4;
            else if (shindo < 5.0)
                return config_color.ShindoColor.S5;
            else if (shindo < 5.5)
                return config_color.ShindoColor.S6;
            else if (shindo < 6.0)
                return config_color.ShindoColor.S7;
            else if (shindo < 6.5)
                return config_color.ShindoColor.S8;
            else if (shindo >= 6.5)
                return config_color.ShindoColor.S9;
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

        /// <summary>
        /// doubleの震度を intに変換します。-0.5以下で-1, 5弱で5, 5強で6, 失敗時 int.MinValueです。
        /// </summary>
        /// <param name="shindo">変換する震度</param>
        /// <returns>震度(int.MinValue,-1~9)</returns>
        public static int Shindo2Int(double? shindo)
        {
            shindo ??= double.NaN;
            if (shindo <= -0.5)
                return -1;
            else if (shindo < 0.5)
                return 0;
            else if (shindo < 1.5)
                return 1;
            else if (shindo < 2.5)
                return 2;
            else if (shindo < 3.5)
                return 3;
            else if (shindo < 4.5)
                return 4;
            else if (shindo < 5.0)
                return 5;
            else if (shindo < 5.5)
                return 6;
            else if (shindo < 6.0)
                return 7;
            else if (shindo < 6.5)
                return 8;
            else if (shindo >= 6.5)
                return 9;
            else
                return int.MinValue;
        }
    }

    /// <summary>
    /// 描画設定
    /// </summary>
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
        /// 観測点名を観測点アイコン右に描画するか
        /// </summary>
        public bool DrawObsName { get; set; } = false;

        /// <summary>
        /// 観測点震度を観測点アイコン右に描画するか
        /// </summary>
        public bool DrawObsShindo { get; set; } = false;

        /// <summary>
        /// 自動ズームの対象震度
        /// </summary>
        /// <remarks>-9:自動ズーム無効</remarks>
        public int AutoZoomMin { get; set; } = -9;

        /// <summary>
        /// 自動ズームの対象震度と最大震度の差
        /// </summary>
        /// <remarks>-9:自動ズーム無効 0:最大震度部分ズーム 3:最大震度から3階級下までズーム</remarks>
        public int AutoZoomMinDif { get; set; } = -9;

        /// <summary>
        /// 観測点円の色を震度別色にするか(falseで強震モニタ色)
        /// </summary>
        public bool Obs_UseIntColor { get; set; } = false;

        /// <summary>
        /// PS波到達円を描画するか
        /// </summary>
        public bool DrawPSWave { get; set; } = true;
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
        public MapKind MapType { get; set; } = MapKind.map_pref_min;

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
        /// 観測点の円(塗りつぶさないほう)の色
        /// </summary>
        public Color Obs_Border { get; set; } = Color.FromArgb(0, 127, 127, 127);

        /// <summary>
        /// 震度別色
        /// </summary>
        public IntColor ShindoColor { get; set; } = new();

        /// <summary>
        /// 震度別色
        /// </summary>
        public class IntColor
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

        /// <summary>
        /// 震央マークの色
        /// </summary>
        public Color HypoColor { get; set; } = Color.FromArgb(191, 255, 0, 0);

        /// <summary>
        /// 震央マークの長さ
        /// </summary>
        public float HypoLength { get; set; } = 20f;

        /// <summary>
        /// 震央マークの太さ
        /// </summary>
        public float HypoWidth { get; set; } = 4f;

        /// <summary>
        /// P波S波の色
        /// </summary>
        public PSWColor PSWaveColor { get; set; } = new();

        /// <summary>
        /// P波S波の色
        /// </summary>
        public class PSWColor
        {
            /// <summary>
            /// P波の色
            /// </summary>
            public Color PDrawColor { get; set; } = Color.FromArgb(191, 0, 0, 255);

            /// <summary>
            /// P波の太さ
            /// </summary>
            public float PWidth { get; set; } = 2f;

            /// <summary>
            /// P波の色
            /// </summary>
            public Color SDrawColor { get; set; } = Color.FromArgb(191, 255, 0, 0);

            /// <summary>
            /// P波の塗りつぶし色
            /// </summary>
            public Color SFillColor { get; set; } = Color.FromArgb(31, 255, 0, 0);

            /// <summary>
            /// P波の太さ
            /// </summary>
            public float SWidth { get; set; } = 2f;
        }
    }

    /// <summary>
    /// ColorをJSONシリアライズ/デシアライズできるようにします。
    /// </summary>
    public class ColorConverter : JsonConverter<Color>
    {
        public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)//オーバーライド  ▼・ӡ・▼    ▼・v・▼    ▼・_・▼
        {
            var colorString = reader.GetString() ?? throw new ArgumentException("値が正しくありません。");
            var argbValues = colorString.Split(',');
            if (argbValues.Length == 3)
                return Color.FromArgb(int.Parse(argbValues[0]), int.Parse(argbValues[1]), int.Parse(argbValues[2]));
            else if (argbValues.Length == 4)
                return Color.FromArgb(int.Parse(argbValues[0]), int.Parse(argbValues[1]), int.Parse(argbValues[2]), int.Parse(argbValues[3]));
            else
                throw new ArgumentException("値が正しくありません。");
        }

        public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
        {
            writer.WriteStringValue($"{value.A},{value.R},{value.G},{value.B}");
        }
    }
}
