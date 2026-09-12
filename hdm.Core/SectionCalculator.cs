using System;
using System.Collections.Generic;

namespace hdm.Core
{
    /// <summary>
    /// 单桩号断面计算核心。
    /// 只依赖注入的纯数据 + ClearDepth，不读文件、不写文件。
    /// </summary>
    public class SectionCalculator
    {
        private readonly double[][] _mesh;
        private readonly double[,] _pqx;
        private readonly double[,] _sqx;
        private readonly List<DuanMianShuJu> _leftWidths;
        private readonly List<DuanMianShuJu> _rightWidths;
        private readonly List<BianPoDuanLuo> _slopeData;
        private readonly double[,] _leftCrossfalls;
        private readonly double[,] _rightCrossfalls;
        private readonly List<LeftJiegoucengConfig> _leftStructures;
        private readonly List<RightJiegoucengConfig> _rightStructures;
        private readonly double _clearDepth;

        public SectionCalculator(
            double[][] mesh,
            double[,] pqx,
            double[,] sqx,
            List<DuanMianShuJu> leftWidths,
            List<DuanMianShuJu> rightWidths,
            List<BianPoDuanLuo> slopeData,
            double[,] leftCrossfalls,
            double[,] rightCrossfalls,
            List<LeftJiegoucengConfig> leftStructures,
            List<RightJiegoucengConfig> rightStructures,
            double clearDepth)
        {
            _mesh = mesh;
            _pqx = pqx;
            _sqx = sqx;
            _leftWidths = leftWidths;
            _rightWidths = rightWidths;
            _slopeData = slopeData;
            _leftCrossfalls = leftCrossfalls;
            _rightCrossfalls = rightCrossfalls;
            _leftStructures = leftStructures;
            _rightStructures = rightStructures;
            _clearDepth = clearDepth;
        }

        public ComputeResult Compute(double station)
        {
            double centerY = LL.hua_H(_sqx, station);

            var dao = LuJiYaoSuYinQing.getcrosectonxy(station, centerY, _leftWidths, _rightWidths);

            // 左侧 / 右侧点（从左到右）
            var zuoDian = dao.ZuoCeCheDaoJueDui;   // N×2
            var youDian = dao.YouCeCheDaoJueDui;

            if (zuoDian == null || zuoDian.GetLength(0) == 0 ||
                youDian == null || youDian.GetLength(0) == 0)
                return new ComputeResult { Success = false, ErrorMessage = "路基宽度数据缺失" };

            // 左外缘 = 左侧第一个点（最左）
            double lOuterX = zuoDian[0, 0];
            double lOuterY = zuoDian[0, 1];

            // 右外缘 = 右侧最后一个点（最右）
            int nYou = youDian.GetLength(0);
            double rOuterX = youDian[nYou - 1, 0];
            double rOuterY = youDian[nYou - 1, 1];

            double[,] ground = LL.getMatchedBZ(_mesh, station);
            if (ground == null || ground.GetLength(0) == 0)
                return new ComputeResult { Success = false, ErrorMessage = "地面线为空" };

            double[,] cleared = LL.hua_OffsetPolyline(ground, _clearDepth);

            var bpCfg = BianPoYinQing.k2BianPo(_slopeData, station);
            if (bpCfg == null)
                return new ComputeResult { Success = false, ErrorMessage = "边坡段落为空" };

            var candidates = BianPoYinQing.getAbsolute(bpCfg, lOuterX, lOuterY, rOuterX, rOuterY);
            double lGroundY = LL.FromXgetY(ground, lOuterX);
            var finalLeft = (lOuterY > lGroundY) ? candidates.ZuoTianJueDui : candidates.ZuoWaJueDui;
            double rGroundY = LL.FromXgetY(ground, rOuterX);
            var finalRight = (rOuterY > rGroundY) ? candidates.YouTianJueDui : candidates.YouWaJueDui;

            // ---------- 拼设计线（未裁剪）----------
            var rawDesign = new List<double[]>();
            if (finalLeft != null)
                for (int i = 0; i < finalLeft.GetLength(0); i++)
                    rawDesign.Add(new[] { finalLeft[i, 0], finalLeft[i, 1] });

            for (int i = 0; i < zuoDian.GetLength(0); i++)
                rawDesign.Add(new[] { zuoDian[i, 0], zuoDian[i, 1] });

            rawDesign.Add(new[] { 0.0, centerY });

            for (int i = 0; i < youDian.GetLength(0); i++)
                rawDesign.Add(new[] { youDian[i, 0], youDian[i, 1] });

            if (finalRight != null)
                for (int i = 0; i < finalRight.GetLength(0); i++)
                    rawDesign.Add(new[] { finalRight[i, 0], finalRight[i, 1] });

            double[,] designTop = ToMat(rawDesign);

            // ---------- 左结构层 ----------
            double leftCrossfall = LumianSlopeManager.InterpolateCrossfall(_leftCrossfalls, station);
            var leftCenter = new double[] { 0.0, centerY };
            double[,] leftSubgrade;
            var leftPolygons = LeftJiegoucengManager.ComputeCoordinates(
                station, leftCenter, leftCrossfall, _leftStructures, out leftSubgrade);

            if (leftPolygons.Count == 0)
                return new ComputeResult { Success = false, ErrorMessage = "左幅结构层段落为空" };

            // 用左结构层最内侧点反查设计线高 → 修正 centerY
            double leftInnerX = leftSubgrade[leftSubgrade.GetLength(0) - 1, 0];
            double newH = LL.FromXgetY(designTop, leftInnerX);
            leftCenter = new double[] { 0.0, newH };
            leftPolygons = LeftJiegoucengManager.ComputeCoordinates(
                station, leftCenter, leftCrossfall, _leftStructures, out leftSubgrade);

            // ---------- 右结构层 ----------
            double rightCrossfall = LumianSlopeManager.InterpolateCrossfall(_rightCrossfalls, station);
            var rightCenter = new double[] { 0.0, centerY };
            double[,] rightSubgrade;
            var rightPolygons = RightJiegoucengManager.ComputeCoordinates(
                station, rightCenter, rightCrossfall, _rightStructures, out rightSubgrade);

            if (rightPolygons.Count == 0)
                return new ComputeResult { Success = false, ErrorMessage = "右幅结构层段落为空" };

            double rightInnerX = rightSubgrade[0, 0];
            newH = LL.FromXgetY(designTop, rightInnerX);
            rightCenter = new double[] { 0.0, newH };
            rightPolygons = RightJiegoucengManager.ComputeCoordinates(
                station, rightCenter, rightCrossfall, _rightStructures, out rightSubgrade);

            // ---------- 拼最终设计线 + 完工线 ----------
            double leftOuterAnchor = leftSubgrade.GetLength(0) > 0 ? leftSubgrade[0, 0] : lOuterX;
            double rightOuterAnchor = rightSubgrade.GetLength(0) > 0
                ? rightSubgrade[rightSubgrade.GetLength(0) - 1, 0] : rOuterX;

            var designList = new List<double[]>();
            var finishedList = new List<double[]>();

            AddOuterPart(rawDesign, designList, finishedList, leftOuterAnchor, -1);

            if (leftSubgrade != null)
                for (int i = 0; i < leftSubgrade.GetLength(0); i++)
                    designList.Add(new[] { leftSubgrade[i, 0], leftSubgrade[i, 1] });

            for (int i = 0; i < zuoDian.GetLength(0); i++)
                finishedList.Add(new[] { zuoDian[i, 0], zuoDian[i, 1] });

            double leftInnerAnchor = leftSubgrade[leftSubgrade.GetLength(0) - 1, 0];
            double rightInnerAnchor = rightSubgrade[0, 0];
            foreach (var p in rawDesign)
                if (p[0] >= leftInnerAnchor && p[0] <= rightInnerAnchor)
                {
                    designList.Add(p);
                    finishedList.Add(p);
                }

            if (rightSubgrade != null)
                for (int i = 0; i < rightSubgrade.GetLength(0); i++)
                    designList.Add(new[] { rightSubgrade[i, 0], rightSubgrade[i, 1] });

            for (int i = 0; i < youDian.GetLength(0); i++)
                finishedList.Add(new[] { youDian[i, 0], youDian[i, 1] });

            AddOuterPart(rawDesign, designList, finishedList, rightOuterAnchor, 1);

            double[,] designMat = ToMat(designList);

            // ---------- 算面积 ----------
            var res = LL.hua_CutAndFillArea(cleared, designMat, 8);
            double fill = Math.Abs(res[0]), cut = Math.Abs(res[1]);
            double minX = res[2], maxX = res[3], minY = res[4];
            var clearPoly = LL.BuildClearPolygon(ground, cleared, minX, maxX);
            double clearArea = LL.hua_PolygonArea(clearPoly);

            int count = (int)res[6];
            if (count == 0)
                return new ComputeResult { Success = false, ErrorMessage = "设计线与地面线无有效交点（填挖方区域为空）" };

            double[,] finalDesign = new double[count, 2];
            int idx = 7;
            for (int i = 0; i < count; i++)
            {
                finalDesign[i, 0] = res[idx++];
                finalDesign[i, 1] = res[idx++];
            }

            double minTrim = finalDesign[0, 0];
            double maxTrim = finalDesign[finalDesign.GetLength(0) - 1, 0];
            var trimmedFinished = new List<double[]>();
            foreach (var p in finishedList)
                if (p[0] >= minTrim && p[0] <= maxTrim) trimmedFinished.Add(p);
            double[,] finalFinished = ToMat(trimmedFinished);

            // ---------- 结构层面积 + 多边形 ----------
            var layerAreaTexts = new List<string>();
            var layerPolygons = new List<double[,]>();

            // 清表线裁剪（保留 [minX, maxX] 内）
            var temp = new List<double[]>();
            for (int i = 0; i < cleared.GetLength(0); i++)
            {
                double x = cleared[i, 0];
                if (x >= minX && x <= maxX)
                    temp.Add(new double[] { x, cleared[i, 1] });
            }
            int n = temp.Count;
            double[,] cleared1 = new double[n + 2, 2];
            cleared1[0, 0] = finalDesign[0, 0];
            cleared1[0, 1] = finalDesign[0, 1];
            for (int i = 0; i < n; i++)
            {
                cleared1[i + 1, 0] = temp[i][0];
                cleared1[i + 1, 1] = temp[i][1];
            }
            cleared1[n + 1, 0] = finalDesign[finalDesign.GetLength(0) - 1, 0];
            cleared1[n + 1, 1] = finalDesign[finalDesign.GetLength(0) - 1, 1];

            if (leftPolygons != null)
                for (int i = 0; i < leftPolygons.Count; i++)
                {
                    var poly = leftPolygons[i];
                    double area = Math.Abs(LL.hua_PolygonArea(poly));
                    string name = (i < _leftStructures.Count && !string.IsNullOrEmpty(_leftStructures[i].LayerName))
                        ? _leftStructures[i].LayerName : $"L_Lay{i + 1}";
                    layerAreaTexts.Add($"L:{name}:{area:F3}");
                    layerPolygons.Add(poly);
                }

            if (rightPolygons != null)
                for (int i = 0; i < rightPolygons.Count; i++)
                {
                    var poly = rightPolygons[i];
                    double area = Math.Abs(LL.hua_PolygonArea(poly));
                    string name = (i < _rightStructures.Count && !string.IsNullOrEmpty(_rightStructures[i].LayerName))
                        ? _rightStructures[i].LayerName : $"R_Lay{i + 1}";
                    layerAreaTexts.Add($"R:{name}:{area:F3}");
                    layerPolygons.Add(poly);
                }

            // ---------- 边坡裁剪（从 finalDesign 切）----------
            var leftTrimmed = new List<double[]>();
            for (int i = 0; i < finalDesign.GetLength(0); i++)
            {
                if (finalDesign[i, 0] <= leftOuterAnchor)
                    leftTrimmed.Add(new[] { finalDesign[i, 0], finalDesign[i, 1] });
                else break;
            }

            var rightTrimmed = new List<double[]>();
            for (int i = finalDesign.GetLength(0) - 1; i >= 0; i--)
            {
                if (finalDesign[i, 0] >= rightOuterAnchor)
                    rightTrimmed.Add(new[] { finalDesign[i, 0], finalDesign[i, 1] });
                else break;
            }
            rightTrimmed.Reverse();

            // ---------- 组装结果 ----------
            return new ComputeResult
            {
                Success = true,
                Result = new SectionResult
                {
                    Station = station,
                    CenterY = centerY,

                    LOuter = new[] { lOuterX, lOuterY },
                    ROuter = new[] { rOuterX, rOuterY },
                    LToe = new[] { finalDesign[0, 0], finalDesign[0, 1] },
                    RToe = new[] { finalDesign[finalDesign.GetLength(0) - 1, 0], finalDesign[finalDesign.GetLength(0) - 1, 1] },

                    Ground = ground,
                    Cleared = cleared1,
                    Design = finalDesign,
                    Finished = finalFinished,
                    LeftSubgrade = leftSubgrade,
                    RightSubgrade = rightSubgrade,
                    Layers = layerPolygons,

                    LeftSlabs = dao.ZuoBanKuai,
                    RightSlabs = dao.YouBanKuai,

                    LeftSlopeRaw = finalLeft ?? new double[0, 2],
                    LeftSlopeTrimmed = ToMat(leftTrimmed),
                    RightSlopeRaw = finalRight ?? new double[0, 2],
                    RightSlopeTrimmed = ToMat(rightTrimmed),

                    FillArea = fill,
                    CutArea = cut,
                    ClearArea = clearArea,
                    LayerAreas = layerAreaTexts,

                    Bounds = new[] { minX, minY, maxX, res[5] }
                }
            };
        }

        // ---------------------------------------------------------
        // 工具方法
        // ---------------------------------------------------------

        private static double[,] ToMat(List<double[]> list)
        {
            var m = new double[list.Count, 2];
            for (int i = 0; i < list.Count; i++) { m[i, 0] = list[i][0]; m[i, 1] = list[i][1]; }
            return m;
        }

        private static void AddOuterPart(
            List<double[]> raw, List<double[]> design, List<double[]> finished,
            double anchor, int side)
        {
            for (int i = 0; i < raw.Count; i++)
            {
                var p = raw[i];
                bool condition = side < 0 ? p[0] <= anchor : p[0] >= anchor;
                if (condition) { design.Add(p); finished.Add(p); }

                if (i > 0)
                {
                    var prev = raw[i - 1];
                    if ((side < 0 && prev[0] < anchor && p[0] > anchor) ||
                        (side > 0 && prev[0] > anchor && p[0] < anchor))
                    {
                        double x1 = prev[0], y1 = prev[1], x2 = p[0], y2 = p[1];
                        double y = y1 + (y2 - y1) * (anchor - x1) / (x2 - x1);
                        var pt = new double[] { anchor, y };
                        design.Add(pt); finished.Add(pt);
                    }
                }
            }
        }
    }
}