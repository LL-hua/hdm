using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace hdm.Core
{
    // ============================================================
    // 1. 路基宽度解析（LuJiYaoSuYinQing）
    // ============================================================
    public static class LuJiYaoSuYinQing
    {
        public static List<DuanMianShuJu> ParseFile(string filePath)
        {
            return Parse(File.ReadLines(filePath));
        }

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
                    continue;
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

        /// <summary>按桩号算左右车道（从左到右）</summary>
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

                var dianList = new List<double[]>();
                var banKuaiList = new List<double[]>();

                for (int k = 0; k < dmBefore.BanKuaiJiHe.Count; k++)
                {
                    if (k >= dmAfter.BanKuaiJiHe.Count) break;
                    double wBefore = dmBefore.BanKuaiJiHe[k][0];
                    double sBefore = dmBefore.BanKuaiJiHe[k][1];
                    double wAfter = dmAfter.BanKuaiJiHe[k][0];
                    double sAfter = dmAfter.BanKuaiJiHe[k][1];

                    double curWidth = wBefore + ratio * (wAfter - wBefore);
                    double curSlopePercent = sBefore + ratio * (sAfter - sBefore);
                    double curSlopeVal = curSlopePercent / 100.0;

                    curZuoX -= curWidth;
                    curZuoY += (curWidth * curSlopeVal);

                    dianList.Add(new double[] { curZuoX, curZuoY });
                    banKuaiList.Add(new double[] { curWidth, curSlopePercent });
                }

                // 反转 → 从左到右
                dianList.Reverse();
                banKuaiList.Reverse();

                jieGuoBao.ZuoCeCheDaoJueDui = ListToMat(dianList);
                jieGuoBao.ZuoBanKuai = ListToMat(banKuaiList);
            }
            else
            {
                jieGuoBao.ZuoCeCheDaoJueDui = new double[0, 2];
                jieGuoBao.ZuoBanKuai = new double[0, 2];
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

                var dianList = new List<double[]>();
                var banKuaiList = new List<double[]>();

                for (int k = 0; k < dmBefore.BanKuaiJiHe.Count; k++)
                {
                    if (k >= dmAfter.BanKuaiJiHe.Count) break;
                    double wBefore = dmBefore.BanKuaiJiHe[k][0];
                    double sBefore = dmBefore.BanKuaiJiHe[k][1];
                    double wAfter = dmAfter.BanKuaiJiHe[k][0];
                    double sAfter = dmAfter.BanKuaiJiHe[k][1];

                    double curWidth = wBefore + ratio * (wAfter - wBefore);
                    double curSlopePercent = sBefore + ratio * (sAfter - sBefore);
                    double curSlopeVal = curSlopePercent / 100.0;

                    curYouX += curWidth;
                    curYouY += (curWidth * curSlopeVal);

                    dianList.Add(new double[] { curYouX, curYouY });
                    banKuaiList.Add(new double[] { curWidth, curSlopePercent });
                }

                // 右侧不反转（已是从左到右）
                jieGuoBao.YouCeCheDaoJueDui = ListToMat(dianList);
                jieGuoBao.YouBanKuai = ListToMat(banKuaiList);
            }
            else
            {
                jieGuoBao.YouCeCheDaoJueDui = new double[0, 2];
                jieGuoBao.YouBanKuai = new double[0, 2];
            }

            return jieGuoBao;
        }

        /// <summary>List&lt;double[]&gt; → double[,]</summary>
        private static double[,] ListToMat(List<double[]> list)
        {
            var m = new double[list.Count, 2];
            for (int i = 0; i < list.Count; i++)
            {
                m[i, 0] = list[i][0];
                m[i, 1] = list[i][1];
            }
            return m;
        }
    }

    // ============================================================
    // 2. 边坡解析（BianPoYinQing）
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

        private static double[,] ParseSlopeLine(string line)
        {
            var list = new List<double[]>();
            string[] tokens = line.Split(',');
            for (int j = 1; j < tokens.Length; j += 2)
            {
                if (j + 1 >= tokens.Length) break;
                list.Add(new double[] { Convert.ToDouble(tokens[j]), Convert.ToDouble(tokens[j + 1]) });
            }
            var result = new double[list.Count, 2];
            for (int i = 0; i < list.Count; i++)
            {
                result[i, 0] = list[i][0];
                result[i, 1] = list[i][1];
            }
            return result;
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
            if (dangQianBianPo == null)
            {
                houxuan.ZuoTianJueDui = new double[0, 2];
                houxuan.ZuoWaJueDui = new double[0, 2];
                houxuan.YouTianJueDui = new double[0, 2];
                houxuan.YouWaJueDui = new double[0, 2];
                return houxuan;
            }

            houxuan.ZuoTianJueDui = Accumulate(dangQianBianPo.ZuoTian, zuoYuanX, zuoYuanY, true);
            houxuan.ZuoWaJueDui   = Accumulate(dangQianBianPo.ZuoWa,   zuoYuanX, zuoYuanY, true);
            houxuan.YouTianJueDui = Accumulate(dangQianBianPo.YouTian, youYuanX, youYuanY, false);
            houxuan.YouWaJueDui   = Accumulate(dangQianBianPo.YouWa,   youYuanX, youYuanY, false);

            return houxuan;
        }

        /// <summary>累加相对位移 → 绝对坐标。reverse=true 时反转（左侧用）</summary>
        private static double[,] Accumulate(double[,] steps, double startX, double startY, bool reverse)
        {
            if (steps == null || steps.GetLength(0) == 0)
                return new double[0, 2];

            int n = steps.GetLength(0);
            var result = new double[n, 2];
            double x = startX, y = startY;
            for (int i = 0; i < n; i++)
            {
                x += steps[i, 0];
                y += steps[i, 1];
                result[i, 0] = x;
                result[i, 1] = y;
            }

            if (reverse)
            {
                for (int i = 0, j = n - 1; i < j; i++, j--)
                {
                    double tx = result[i, 0], ty = result[i, 1];
                    result[i, 0] = result[j, 0]; result[i, 1] = result[j, 1];
                    result[j, 0] = tx; result[j, 1] = ty;
                }
            }
            return result;
        }
    }

    // ============================================================
    // 3. 横坡解析（LumianSlopeManager）
    // ============================================================
    public static class LumianSlopeManager
    {
        /// <summary>返回 N 行 × 2 列：[桩号, 横坡%]，按桩号升序</summary>
        public static double[,] ParseFile(string filePath)
        {
            return Parse(File.ReadLines(filePath));
        }

        public static double[,] Parse(IEnumerable<string> lines)
        {
            var list = new List<double[]>();
            bool first = true;
            foreach (var raw in lines)
            {
                if (first) { first = false; continue; }
                string line = raw.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("#") || line.StartsWith("//"))
                    continue;

                string[] parts = line.Split(',');
                list.Add(new double[] { double.Parse(parts[0].Trim()), double.Parse(parts[1].Trim()) });
            }

            var result = new double[list.Count, 2];
            for (int i = 0; i < list.Count; i++)
            {
                result[i, 0] = list[i][0];
                result[i, 1] = list[i][1];
            }
            LL.SortRowsByColumn(result, 0);
            return result;
        }

        /// <summary>横坡线性插值（二分查找）。records 为 N×2 [桩号, 横坡%]</summary>
        public static double InterpolateCrossfall(double[,] records, double currentStation)
        {
            if (records == null) return 0.0;
            int n = records.GetLength(0);
            if (n == 0) return 0.0;
            if (n == 1) return records[0, 1];

            if (currentStation <= records[0, 0]) return records[0, 1];
            if (currentStation >= records[n - 1, 0]) return records[n - 1, 1];

            int low = 0, high = n - 1;
            while (low <= high)
            {
                int mid = low + ((high - low) >> 1);
                double midStation = records[mid, 0];
                if (Math.Abs(midStation - currentStation) < 0.00001) return records[mid, 1];
                if (midStation < currentStation) low = mid + 1;
                else high = mid - 1;
            }

            double s1 = records[high, 0], v1 = records[high, 1];
            double s2 = records[low, 0], v2 = records[low, 1];
            if (Math.Abs(s2 - s1) < 0.00001) return v1;
            return v1 + (v2 - v1) * (currentStation - s1) / (s2 - s1);
        }
    }

    // ============================================================
    // 4. 左结构层解析（LeftJiegoucengManager）
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
                if (first) { first = false; continue; }
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

        /// <summary>
        /// 计算左结构层。
        /// centerPoint = [x, y] 中心点。
        /// 返回：每个层一个 polygon（double[,]）；subgradeLine 为路基线（double[,]）。
        /// </summary>
        public static List<double[,]> ComputeCoordinates(
            double station, double[] centerPoint, double crossfall,
            List<LeftJiegoucengConfig> configs,
            out double[,] subgradeLine)
        {
            var polygons = new List<double[,]>();

            var activeLayers = configs
                .Where(cfg => station >= cfg.StartStation && station <= cfg.EndStation)
                .OrderBy(cfg => cfg.LayerIndex)
                .ToList();

            if (activeLayers.Count == 0)
            {
                subgradeLine = new double[0, 2];
                return polygons;
            }

            int layerCount = activeLayers.Count;
            double k = -crossfall / 100.0;

            double[] topOutX = new double[layerCount], topOutY = new double[layerCount];
            double[] botOutX = new double[layerCount], botOutY = new double[layerCount];
            double[] topInX = new double[layerCount], topInY = new double[layerCount];
            double[] botInX = new double[layerCount], botInY = new double[layerCount];

            var firstLayer = activeLayers[0];
            double curTopInX = centerPoint[0] + firstLayer.InnerStepWidth;
            double curTopInY = centerPoint[1];

            double curTopOutX = curTopInX + firstLayer.OuterStepWidth;
            double curTopOutY = curTopInY + (curTopOutX - curTopInX) * k;

            for (int i = 0; i < layerCount; i++)
            {
                var cfg = activeLayers[i];

                if (i > 0)
                {
                    curTopOutX = botOutX[i - 1] + cfg.OuterStepWidth;
                    curTopOutY = botOutY[i - 1] + (curTopOutX - botOutX[i - 1]) * k;

                    curTopInX = botInX[i - 1] + cfg.InnerStepWidth;
                    curTopInY = botInY[i - 1] + (curTopInX - botInX[i - 1]) * k;
                }

                topOutX[i] = curTopOutX; topOutY[i] = curTopOutY;
                topInX[i] = curTopInX; topInY[i] = curTopInY;

                double outN = Math.Abs(cfg.OuterSlope);
                double dx = -(outN * cfg.Thickness) / (1.0 - outN * k);
                double botOutXv = curTopOutX + dx;
                double botOutYv = curTopOutY - cfg.Thickness + dx * k;
                botOutX[i] = botOutXv; botOutY[i] = botOutYv;

                botInX[i] = curTopInX; botInY[i] = curTopInY - cfg.Thickness;

                var poly = new double[4, 2];
                poly[0, 0] = topOutX[i]; poly[0, 1] = topOutY[i];
                poly[1, 0] = botOutX[i]; poly[1, 1] = botOutY[i];
                poly[2, 0] = botInX[i];  poly[2, 1] = botInY[i];
                poly[3, 0] = topInX[i];  poly[3, 1] = topInY[i];
                polygons.Add(poly);
            }

            // 外 + 内 拼接
            var combined = new List<double[]>();

            for (int i = 0; i < layerCount; i++)
            {
                combined.Add(new double[] { topOutX[i], topOutY[i] });
                combined.Add(new double[] { botOutX[i], botOutY[i] });
            }

            var innerList = new List<double[]>();
            innerList.Add(new double[] { topInX[0], centerPoint[1] });
            for (int i = 0; i < layerCount; i++)
            {
                innerList.Add(new double[] { topInX[i], topInY[i] });
                innerList.Add(new double[] { botInX[i], botInY[i] });
            }
            innerList.Reverse();
            combined.AddRange(innerList);

            subgradeLine = new double[combined.Count, 2];
            for (int i = 0; i < combined.Count; i++)
            {
                subgradeLine[i, 0] = combined[i][0];
                subgradeLine[i, 1] = combined[i][1];
            }

            return polygons;
        }
    }

    // ============================================================
    // 5. 右结构层解析（RightJiegoucengManager）
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

        public static List<double[,]> ComputeCoordinates(
            double station, double[] centerPoint, double crossfall,
            List<RightJiegoucengConfig> configs,
            out double[,] subgradeLine)
        {
            var polygons = new List<double[,]>();

            var activeLayers = configs
                .Where(cfg => station >= cfg.StartStation && station <= cfg.EndStation)
                .OrderBy(cfg => cfg.LayerIndex)
                .ToList();

            if (activeLayers.Count == 0)
            {
                subgradeLine = new double[0, 2];
                return polygons;
            }

            int layerCount = activeLayers.Count;
            double k = crossfall / 100.0;

            double[] topOutX = new double[layerCount], topOutY = new double[layerCount];
            double[] botOutX = new double[layerCount], botOutY = new double[layerCount];
            double[] topInX = new double[layerCount], topInY = new double[layerCount];
            double[] botInX = new double[layerCount], botInY = new double[layerCount];

            var firstLayer = activeLayers[0];
            double curTopInX = centerPoint[0] + firstLayer.InnerStepWidth;
            double curTopInY = centerPoint[1];

            double curTopOutX = curTopInX + firstLayer.OuterStepWidth;
            double curTopOutY = curTopInY + (curTopOutX - curTopInX) * k;

            for (int i = 0; i < layerCount; i++)
            {
                var cfg = activeLayers[i];

                if (i > 0)
                {
                    curTopOutX = botOutX[i - 1] + cfg.OuterStepWidth;
                    curTopOutY = botOutY[i - 1] + (curTopOutX - botOutX[i - 1]) * k;

                    curTopInX = botInX[i - 1] + cfg.InnerStepWidth;
                    curTopInY = botInY[i - 1] + (curTopInX - botInX[i - 1]) * k;
                }

                topOutX[i] = curTopOutX; topOutY[i] = curTopOutY;
                topInX[i] = curTopInX; topInY[i] = curTopInY;

                double outN = Math.Abs(cfg.OuterSlope);
                double dx = (outN * cfg.Thickness) / (1.0 + outN * k);
                double botOutXv = curTopOutX + dx;
                double botOutYv = curTopOutY - cfg.Thickness + (botOutXv - curTopOutX) * k;
                botOutX[i] = botOutXv; botOutY[i] = botOutYv;

                botInX[i] = curTopInX; botInY[i] = curTopInY - cfg.Thickness;

                var poly = new double[4, 2];
                poly[0, 0] = topOutX[i]; poly[0, 1] = topOutY[i];
                poly[1, 0] = botOutX[i]; poly[1, 1] = botOutY[i];
                poly[2, 0] = botInX[i];  poly[2, 1] = botInY[i];
                poly[3, 0] = topInX[i];  poly[3, 1] = topInY[i];
                polygons.Add(poly);
            }

            var combined = new List<double[]>();

            combined.Add(new double[] { topInX[0], centerPoint[1] });
            for (int i = 0; i < layerCount; i++)
            {
                combined.Add(new double[] { topInX[i], topInY[i] });
                combined.Add(new double[] { botInX[i], botInY[i] });
            }

            var outerList = new List<double[]>();
            for (int i = 0; i < layerCount; i++)
            {
                outerList.Add(new double[] { topOutX[i], topOutY[i] });
                outerList.Add(new double[] { botOutX[i], botOutY[i] });
            }
            outerList.Reverse();
            combined.AddRange(outerList);

            subgradeLine = new double[combined.Count, 2];
            for (int i = 0; i < combined.Count; i++)
            {
                subgradeLine[i, 0] = combined[i][0];
                subgradeLine[i, 1] = combined[i][1];
            }

            return polygons;
        }
    }

    // ============================================================
    // 6. 通用文本读取（DataReader）
    // ============================================================
    public static class DataReader
    {
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