using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace hdm.Core
{
    // ============================================================
    // 1. 路基宽度解析（原 LuJiYaoSuYinQing）
    // ============================================================
    public static class LuJiYaoSuYinQing
    {
        /// <summary>从文件读（跳过表头）</summary>
        public static List<DuanMianShuJu> ParseFile(string filePath)
        {
            return Parse(File.ReadLines(filePath));
        }

        /// <summary>从行集合解析（跳过表头，支持逗号/空格/制表符）</summary>
        public static List<DuanMianShuJu> Parse(IEnumerable<string> lines)
        {
            var jieGuoList = new List<DuanMianShuJu>();
            bool isFirstLine = true;
            foreach (var line in lines)
            {
                string cleanLine = line.Trim();
                if (string.IsNullOrEmpty(cleanLine) || cleanLine.StartsWith(";")) continue;

                if (isFirstLine)
                {
                    isFirstLine = false;
                    continue; // 跳过表头
                }

                string[] tokens = cleanLine.Split(
                    new char[] { ',', ' ', '\t' },
                    StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length < 3) continue;

                var dmData = new DuanMianShuJu();
                dmData.ZhuangHao = Convert.ToDouble(tokens[0]);

                for (int j = 1; j < tokens.Length; j += 2)
                {
                    if (j + 1 >= tokens.Length) break;
                    double kuanDu = Convert.ToDouble(tokens[j]);
                    double hengPo = Convert.ToDouble(tokens[j + 1]);
                    dmData.BanKuaiJiHe.Add(new double[] { kuanDu, hengPo });
                }

                jieGuoList.Add(dmData);
            }
            return jieGuoList;
        }

        /// <summary>按桩号算左右车道绝对坐标（线性插值）</summary>
        public static LuJiCheDaoJieGuoBao getcrosectonxy(
            double muBiaoZhuangHao,
            double zhongZhuangGaoCheng,
            List<DuanMianShuJu> suoYouZuoData,
            List<DuanMianShuJu> suoYouYouData)
        {
            var jieGuoBao = new LuJiCheDaoJieGuoBao();

            // -------- 左侧 --------
            if (suoYouZuoData != null && suoYouZuoData.Count > 0)
            {
                double stBefore = suoYouZuoData[0].ZhuangHao;
                double stAfter = suoYouZuoData[suoYouZuoData.Count - 1].ZhuangHao;
                DuanMianShuJu dmBefore = suoYouZuoData[0];
                DuanMianShuJu dmAfter = suoYouZuoData[suoYouZuoData.Count - 1];

                if (muBiaoZhuangHao <= stBefore)
                {
                    dmAfter = dmBefore; stAfter = stBefore;
                }
                else if (muBiaoZhuangHao >= stAfter)
                {
                    dmBefore = dmAfter; stBefore = stAfter;
                }
                else
                {
                    for (int i = 1; i < suoYouZuoData.Count; i++)
                    {
                        if (suoYouZuoData[i].ZhuangHao >= muBiaoZhuangHao)
                        {
                            stAfter = suoYouZuoData[i].ZhuangHao;
                            dmAfter = suoYouZuoData[i];
                            stBefore = suoYouZuoData[i - 1].ZhuangHao;
                            dmBefore = suoYouZuoData[i - 1];
                            break;
                        }
                    }
                }

                double ratio = (stBefore == stAfter) ? 0.0 : (muBiaoZhuangHao - stBefore) / (stAfter - stBefore);
                double curZuoX = 0.0;
                double curZuoY = zhongZhuangGaoCheng;

                for (int k = 0; k < dmBefore.BanKuaiJiHe.Count; k++)
                {
                    if (k >= dmAfter.BanKuaiJiHe.Count) break;
                    double wBefore = dmBefore.BanKuaiJiHe[k][0];
                    double sBefore = dmBefore.BanKuaiJiHe[k][1];
                    double wAfter = dmAfter.BanKuaiJiHe[k][0];
                    double sAfter = dmAfter.BanKuaiJiHe[k][1];

                    double curWidth = wBefore + ratio * (wAfter - wBefore);
                    double curSlopeVal = (sBefore + ratio * (sAfter - sBefore)) / 100.0;

                    curZuoX -= curWidth;
                    curZuoY += (curWidth * curSlopeVal);
                    jieGuoBao.ZuoCeCheDaoJueDui.Add(new JueDuiBanKuaiDian(curZuoX, curZuoY));
                }
                jieGuoBao.ZuoCeCheDaoJueDui.Reverse();
            }

            // -------- 右侧 --------
            if (suoYouYouData != null && suoYouYouData.Count > 0)
            {
                double stBefore = suoYouYouData[0].ZhuangHao;
                double stAfter = suoYouYouData[suoYouYouData.Count - 1].ZhuangHao;
                DuanMianShuJu dmBefore = suoYouYouData[0];
                DuanMianShuJu dmAfter = suoYouYouData[suoYouYouData.Count - 1];

                if (muBiaoZhuangHao <= stBefore)
                {
                    dmAfter = dmBefore; stAfter = stBefore;
                }
                else if (muBiaoZhuangHao >= stAfter)
                {
                    dmBefore = dmAfter; stBefore = stAfter;
                }
                else
                {
                    for (int i = 1; i < suoYouYouData.Count; i++)
                    {
                        if (suoYouYouData[i].ZhuangHao >= muBiaoZhuangHao)
                        {
                            stAfter = suoYouYouData[i].ZhuangHao;
                            dmAfter = suoYouYouData[i];
                            stBefore = suoYouYouData[i - 1].ZhuangHao;
                            dmBefore = suoYouYouData[i - 1];
                            break;
                        }
                    }
                }

                double ratio = (stBefore == stAfter) ? 0.0 : (muBiaoZhuangHao - stBefore) / (stAfter - stBefore);
                double curYouX = 0.0;
                double curYouY = zhongZhuangGaoCheng;

                for (int k = 0; k < dmBefore.BanKuaiJiHe.Count; k++)
                {
                    if (k >= dmAfter.BanKuaiJiHe.Count) break;
                    double wBefore = dmBefore.BanKuaiJiHe[k][0];
                    double sBefore = dmBefore.BanKuaiJiHe[k][1];
                    double wAfter = dmAfter.BanKuaiJiHe[k][0];
                    double sAfter = dmAfter.BanKuaiJiHe[k][1];
                    double curWidth = wBefore + ratio * (wAfter - wBefore);
                    double curSlopeVal = (sBefore + ratio * (sAfter - sBefore)) / 100.0;

                    curYouX += curWidth;
                    curYouY += (curWidth * curSlopeVal);
                    jieGuoBao.YouCeCheDaoJueDui.Add(new JueDuiBanKuaiDian(curYouX, curYouY));
                }
            }

            return jieGuoBao;
        }
    }

    // ============================================================
    // 2. 边坡解析（原 BianPoYinQing）
    // ============================================================
    public static class BianPoYinQing
    {
        public static List<BianPoDuanLuo> ParseFile(string filePath)
        {
            return Parse(File.ReadAllLines(filePath));
        }

        public static List<BianPoDuanLuo> Parse(string[] lines)
        {
            var jieGuoJi = new List<BianPoDuanLuo>();
            for (int i = 1; i < lines.Length; i += 5)
            {
                if (i + 4 >= lines.Length) break;

                var duanLuo = new BianPoDuanLuo();

                string[] zhuangHaoParts = lines[i].Split(',');
                duanLuo.QiShiZhuangHao = Convert.ToDouble(zhuangHaoParts[0]);
                duanLuo.JieShuZhuangHao = Convert.ToDouble(zhuangHaoParts[1]);

                duanLuo.ZuoTian = ParseSlopeLine(lines[i + 1]);
                duanLuo.ZuoWa = ParseSlopeLine(lines[i + 2]);
                duanLuo.YouTian = ParseSlopeLine(lines[i + 3]);
                duanLuo.YouWa = ParseSlopeLine(lines[i + 4]);

                jieGuoJi.Add(duanLuo);
            }
            return jieGuoJi;
        }

        private static List<double[]> ParseSlopeLine(string line)
        {
            var list = new List<double[]>();
            string[] tokens = line.Split(',');
            for (int j = 1; j < tokens.Length; j += 2)
            {
                if (j + 1 >= tokens.Length) break;
                list.Add(new double[] { Convert.ToDouble(tokens[j]), Convert.ToDouble(tokens[j + 1]) });
            }
            return list;
        }

        /// <summary>按桩号找段落（左闭右开）</summary>
        public static BianPoDuanLuo k2BianPo(List<BianPoDuanLuo> suoYouBianPo, double muBiaoZhuangHao)
        {
            foreach (var duanLuo in suoYouBianPo)
            {
                if (muBiaoZhuangHao >= duanLuo.QiShiZhuangHao && muBiaoZhuangHao < duanLuo.JieShuZhuangHao)
                    return duanLuo;
            }
            return null;
        }

        /// <summary>相对位移累加 → 4 组绝对坐标</summary>
        public static BianPoHouXuanBao getAbsolute(
            BianPoDuanLuo dangQianBianPo,
            double zuoYuanX, double zuoYuanY,
            double youYuanX, double youYuanY)
        {
            var houxuan = new BianPoHouXuanBao();
            if (dangQianBianPo == null) return houxuan;

            double ztX = zuoYuanX; double ztY = zuoYuanY;
            foreach (var step in dangQianBianPo.ZuoTian)
            {
                ztX += step[0]; ztY += step[1];
                houxuan.ZuoTianJueDui.Add(new double[] { ztX, ztY });
            }
            houxuan.ZuoTianJueDui.Reverse();

            double zwX = zuoYuanX; double zwY = zuoYuanY;
            foreach (var step in dangQianBianPo.ZuoWa)
            {
                zwX += step[0]; zwY += step[1];
                houxuan.ZuoWaJueDui.Add(new double[] { zwX, zwY });
            }
            houxuan.ZuoWaJueDui.Reverse();

            double ytX = youYuanX; double ytY = youYuanY;
            foreach (var step in dangQianBianPo.YouTian)
            {
                ytX += step[0]; ytY += step[1];
                houxuan.YouTianJueDui.Add(new double[] { ytX, ytY });
            }

            double ywX = youYuanX; double ywY = youYuanY;
            foreach (var step in dangQianBianPo.YouWa)
            {
                ywX += step[0]; ywY += step[1];
                houxuan.YouWaJueDui.Add(new double[] { ywX, ywY });
            }

            return houxuan;
        }
    }

    // ============================================================
    // 3. 横坡解析（原 LumianSlopeManager）
    // ============================================================
    public static class LumianSlopeManager
    {
        public static List<CrossfallRecord> ParseFile(string filePath)
        {
            return Parse(File.ReadLines(filePath));
        }

        public static List<CrossfallRecord> Parse(IEnumerable<string> lines)
        {
            var records = new List<CrossfallRecord>();
            bool first = true;
            foreach (var raw in lines)
            {
                if (first) { first = false; continue; } // 跳表头
                string line = raw.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("#") || line.StartsWith("//"))
                    continue;

                string[] parts = line.Split(',');
                records.Add(new CrossfallRecord
                {
                    Station = double.Parse(parts[0].Trim()),
                    Slope = double.Parse(parts[1].Trim())
                });
            }
            records.Sort((a, b) => a.Station.CompareTo(b.Station));
            return records;
        }

        /// <summary>横坡线性插值（二分查找）</summary>
        public static double InterpolateCrossfall(List<CrossfallRecord> records, double currentStation)
        {
            if (records == null || records.Count == 0) return 0.0;
            if (records.Count == 1) return records[0].Slope;

            if (currentStation <= records[0].Station) return records[0].Slope;
            if (currentStation >= records[records.Count - 1].Station) return records[records.Count - 1].Slope;

            int low = 0;
            int high = records.Count - 1;
            while (low <= high)
            {
                int mid = low + ((high - low) >> 1);
                double midStation = records[mid].Station;
                if (Math.Abs(midStation - currentStation) < 0.00001) return records[mid].Slope;
                if (midStation < currentStation) low = mid + 1;
                else high = mid - 1;
            }

            var left = records[high];
            var right = records[low];
            double stationDelta = right.Station - left.Station;
            if (Math.Abs(stationDelta) < 0.00001) return left.Slope;

            double ratio = (currentStation - left.Station) / stationDelta;
            return left.Slope + ratio * (right.Slope - left.Slope);
        }
    }

    // ============================================================
    // 4. 左结构层解析（原 LeftJiegoucengManager）
    // ============================================================
    public static class LeftJiegoucengManager
    {
        public static List<LeftJiegoucengConfig> ParseFile(string filePath)
        {
            return Parse(File.ReadLines(filePath));
        }

        public static List<LeftJiegoucengConfig> Parse(IEnumerable<string> lines)
        {
            var list = new List<LeftJiegoucengConfig>();
            bool first = true;
            foreach (var raw in lines)
            {
                if (first) { first = false; continue; } // 跳表头
                if (string.IsNullOrWhiteSpace(raw)) continue;
                string[] parts = raw.Split(',');
                list.Add(new LeftJiegoucengConfig
                {
                    StartStation = double.Parse(parts[0].Trim()),
                    EndStation = double.Parse(parts[1].Trim()),
                    LayerIndex = int.Parse(parts[2].Trim()),
                    LayerName = parts[3].Trim(),
                    Thickness = double.Parse(parts[4].Trim()),
                    InnerStepWidth = double.Parse(parts[5].Trim()),
                    InnerSlope = double.Parse(parts[6].Trim()),
                    OuterStepWidth = double.Parse(parts[7].Trim()),
                    OuterSlope = double.Parse(parts[8].Trim())
                });
            }
            return list;
        }

        public static List<LeftPoint2D[]> ComputeCoordinates(
            double station, LeftPoint2D centerPoint, double crossfall,
            List<LeftJiegoucengConfig> configs,
            out LeftPoint2D[] subgradeLine)
        {
            var polygons = new List<LeftPoint2D[]>();

            var activeLayers = configs
                .Where(cfg => station >= cfg.StartStation && station <= cfg.EndStation)
                .OrderBy(cfg => cfg.LayerIndex)
                .ToList();

            if (activeLayers.Count == 0)
            {
                subgradeLine = Array.Empty<LeftPoint2D>();
                return polygons;
            }

            int layerCount = activeLayers.Count;
            double k = -crossfall / 100.0;

            var topOutList = new LeftPoint2D[layerCount];
            var botOutList = new LeftPoint2D[layerCount];
            var topInList = new LeftPoint2D[layerCount];
            var botInList = new LeftPoint2D[layerCount];

            var firstLayer = activeLayers[0];
            double curTopInX = centerPoint.X + firstLayer.InnerStepWidth;
            double curTopInY = centerPoint.Y;

            double curTopOutX = curTopInX + firstLayer.OuterStepWidth;
            double curTopOutY = curTopInY + (curTopOutX - curTopInX) * k;

            for (int i = 0; i < layerCount; i++)
            {
                var cfg = activeLayers[i];

                if (i > 0)
                {
                    curTopOutX = botOutList[i - 1].X + cfg.OuterStepWidth;
                    curTopOutY = botOutList[i - 1].Y + (curTopOutX - botOutList[i - 1].X) * k;

                    curTopInX = botInList[i - 1].X + cfg.InnerStepWidth;
                    curTopInY = botInList[i - 1].Y + (curTopInX - botInList[i - 1].X) * k;
                }

                topOutList[i] = new LeftPoint2D(curTopOutX, curTopOutY);
                topInList[i] = new LeftPoint2D(curTopInX, curTopInY);

                double outN = Math.Abs(cfg.OuterSlope);
                double dx = -(outN * cfg.Thickness) / (1.0 - outN * k);
                double botOutX = curTopOutX + dx;
                double botOutY = curTopOutY - cfg.Thickness + dx * k;
                botOutList[i] = new LeftPoint2D(botOutX, botOutY);

                botInList[i] = new LeftPoint2D(curTopInX, curTopInY - cfg.Thickness);

                polygons.Add(new LeftPoint2D[] {
                    new LeftPoint2D(topOutList[i].X, topOutList[i].Y),
                    new LeftPoint2D(botOutList[i].X, botOutList[i].Y),
                    new LeftPoint2D(botInList[i].X, botInList[i].Y),
                    new LeftPoint2D(topInList[i].X, topInList[i].Y)
                });
            }

            var innerList = new List<LeftPoint2D>();
            var outerList = new List<LeftPoint2D>();

            for (int i = 0; i < layerCount; i++)
            {
                outerList.Add(topOutList[i]);
                outerList.Add(botOutList[i]);
            }
            for (int i = 0; i < layerCount; i++)
            {
                innerList.Add(topInList[i]);
                innerList.Add(botInList[i]);
            }
            innerList.Insert(0, new LeftPoint2D(topInList[0].X, centerPoint.Y));
            innerList.Reverse();

            var combined = new List<LeftPoint2D>();
            combined.AddRange(outerList);
            combined.AddRange(innerList);

            subgradeLine = combined.ToArray();
            return polygons;
        }
    }

    // ============================================================
    // 5. 右结构层解析（原 RightJiegoucengManager）
    // ============================================================
    public static class RightJiegoucengManager
    {
        public static List<RightJiegoucengConfig> ParseFile(string filePath)
        {
            return Parse(File.ReadLines(filePath));
        }

        public static List<RightJiegoucengConfig> Parse(IEnumerable<string> lines)
        {
            var list = new List<RightJiegoucengConfig>();
            bool first = true;
            foreach (var raw in lines)
            {
                if (first) { first = false; continue; }
                if (string.IsNullOrWhiteSpace(raw)) continue;
                string[] parts = raw.Split(',');
                list.Add(new RightJiegoucengConfig
                {
                    StartStation = double.Parse(parts[0].Trim()),
                    EndStation = double.Parse(parts[1].Trim()),
                    LayerIndex = int.Parse(parts[2].Trim()),
                    LayerName = parts[3].Trim(),
                    Thickness = double.Parse(parts[4].Trim()),
                    InnerStepWidth = double.Parse(parts[5].Trim()),
                    InnerSlope = double.Parse(parts[6].Trim()),
                    OuterStepWidth = double.Parse(parts[7].Trim()),
                    OuterSlope = double.Parse(parts[8].Trim())
                });
            }
            return list;
        }

        public static List<RightPoint2D[]> ComputeCoordinates(
            double station, RightPoint2D centerPoint, double crossfall,
            List<RightJiegoucengConfig> configs,
            out RightPoint2D[] subgradeLine)
        {
            var polygons = new List<RightPoint2D[]>();

            var activeLayers = configs
                .Where(cfg => station >= cfg.StartStation && station <= cfg.EndStation)
                .OrderBy(cfg => cfg.LayerIndex)
                .ToList();

            if (activeLayers.Count == 0)
            {
                subgradeLine = Array.Empty<RightPoint2D>();
                return polygons;
            }

            int layerCount = activeLayers.Count;
            double k = crossfall / 100.0;

            var topOutList = new RightPoint2D[layerCount];
            var botOutList = new RightPoint2D[layerCount];
            var topInList = new RightPoint2D[layerCount];
            var botInList = new RightPoint2D[layerCount];

            var firstLayer = activeLayers[0];
            double curTopInX = centerPoint.X + firstLayer.InnerStepWidth;
            double curTopInY = centerPoint.Y;

            double curTopOutX = curTopInX + firstLayer.OuterStepWidth;
            double curTopOutY = curTopInY + (curTopOutX - curTopInX) * k;

            for (int i = 0; i < layerCount; i++)
            {
                var cfg = activeLayers[i];

                if (i > 0)
                {
                    curTopOutX = botOutList[i - 1].X + cfg.OuterStepWidth;
                    curTopOutY = botOutList[i - 1].Y + (curTopOutX - botOutList[i - 1].X) * k;

                    curTopInX = botInList[i - 1].X + cfg.InnerStepWidth;
                    curTopInY = botInList[i - 1].Y + (curTopInX - botInList[i - 1].X) * k;
                }

                topOutList[i] = new RightPoint2D(curTopOutX, curTopOutY);
                topInList[i] = new RightPoint2D(curTopInX, curTopInY);

                double outN = Math.Abs(cfg.OuterSlope);
                double dx = (outN * cfg.Thickness) / (1.0 + outN * k);
                double botOutX = curTopOutX + dx;
                double botOutY = curTopOutY - cfg.Thickness + (botOutX - curTopOutX) * k;
                botOutList[i] = new RightPoint2D(botOutX, botOutY);

                botInList[i] = new RightPoint2D(curTopInX, curTopInY - cfg.Thickness);

                polygons.Add(new RightPoint2D[] {
                    new RightPoint2D(topOutList[i].X, topOutList[i].Y),
                    new RightPoint2D(botOutList[i].X, botOutList[i].Y),
                    new RightPoint2D(botInList[i].X, botInList[i].Y),
                    new RightPoint2D(topInList[i].X, topInList[i].Y)
                });
            }

            var innerList = new List<RightPoint2D>();
            var outerList = new List<RightPoint2D>();

            innerList.Add(new RightPoint2D(topInList[0].X, centerPoint.Y));
            for (int i = 0; i < layerCount; i++)
            {
                innerList.Add(topInList[i]);
                innerList.Add(botInList[i]);
            }
            for (int i = 0; i < layerCount; i++)
            {
                outerList.Add(topOutList[i]);
                outerList.Add(botOutList[i]);
            }
            outerList.Reverse();

            var combined = new List<RightPoint2D>();
            combined.AddRange(innerList);
            combined.AddRange(outerList);

            subgradeLine = combined.ToArray();
            return polygons;
        }
    }

    // ============================================================
    // 6. 通用文本读取（原 LL.ReadDataFromFile）
    // ============================================================
    public static class DataReader
    {
        /// <summary>从文件读二维数值表（支持空格/逗号/制表符/中文逗号）</summary>
        public static double[,] ReadDataFromFile(
            string filePath, int columnCount,
            Encoding encoding = null, bool skipFirstRow = false)
        {
            string fileName = Path.GetFileName(filePath);
            Encoding actualEncoding = encoding ?? Encoding.UTF8;

            string[] allLines = File.ReadAllLines(filePath, actualEncoding);
            List<double[]> validRows = new List<double[]>();

            char[] separators = { ' ', ',', '，', '\t' };

            int startIndex = skipFirstRow ? 1 : 0;
            for (int lineIndex = startIndex; lineIndex < allLines.Length; lineIndex++)
            {
                string line = allLines[lineIndex];
                string trimmedLine = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmedLine)) continue;

                string[] tokens = trimmedLine.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length < columnCount)
                    throw new FormatException(
                        $"文件 {fileName} 第 {lineIndex + 1} 行不是 {columnCount} 列数据，实际列数: {tokens.Length}");

                double[] doubleRow = new double[columnCount];
                for (int tokenIndex = 0; tokenIndex < columnCount; tokenIndex++)
                {
                    if (!double.TryParse(tokens[tokenIndex], out double value))
                        throw new FormatException(
                            $"解析失败 → 文件：{fileName} → 行号：{lineIndex + 1} → 列号：{tokenIndex + 1} → 内容：{tokens[tokenIndex]}");
                    doubleRow[tokenIndex] = value;
                }
                validRows.Add(doubleRow);
            }

            double[,] result = new double[validRows.Count, columnCount];
            for (int i = 0; i < validRows.Count; i++)
                for (int j = 0; j < columnCount; j++)
                    result[i, j] = validRows[i][j];
            return result;
        }
    }
}