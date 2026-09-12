using System;
using System.Collections.Generic;
using System.Globalization;

namespace hdm.Core
{
    public static class LL
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        //（hua_Zs 用）
        private static readonly double[] GaussRr = { 0.1739274226, 0.3260725774, 0.3260725774, 0.1739274226 };
        private static readonly double[] GaussVv = { 0.0694318442, 0.3300094782, 0.6699905218, 0.9305681558 };
public static void PrintRig(double[,] rig)
{
    int rows = rig.GetLength(0); // 行数
    int cols = rig.GetLength(1); // 列数

    for (int i = 0; i < rows; i++)
    {
        for (int j = 0; j < cols; j++)
        {
            // F3 表示保留三位小数
            Console.Write($"{rig[i, j]:F3}");
            
            // 列之间用制表符或空格分隔
            if (j < cols - 1)
                Console.Write(",");
        }
        Console.WriteLine(); // 换行
    }
}
public static void PrintRig(double[] rig)
{
    for (int i = 0; i < rig.Length; i++)
    {
        // F3 表示保留三位小数
        Console.Write($"{rig[i]:F3}");

        // 元素之间用制表符分隔
        if (i < rig.Length - 1)
            Console.Write(",");
    }
    Console.WriteLine(); // 换行
}
/// <summary>
/// 按指定列对 double[,] 原地升序排序（行跟着走）。
/// 例：SortRowsByColumn(records, 0) 按第 0 列排序。
/// </summary>
public static void SortRowsByColumn(double[,] data, int col)
{
    if (data == null) return;
    int rows = data.GetLength(0);
    int cols = data.GetLength(1);
    if (rows <= 1 || col < 0 || col >= cols) return;

    int[] idx = new int[rows];
    for (int i = 0; i < rows; i++) idx[i] = i;

    Array.Sort(idx, (a, b) => data[a, col].CompareTo(data[b, col]));

    double[] tmp = new double[cols];
    for (int i = 0; i < rows; i++)
    {
        if (idx[i] == i) continue;
        for (int c = 0; c < cols; c++) tmp[c] = data[i, c];
        int j = i;
        while (true)
        {
            int k = idx[j];
            idx[j] = j;
            if (k == i) break;
            for (int c = 0; c < cols; c++) data[j, c] = data[k, c];
            j = k;
        }
        for (int c = 0; c < cols; c++) data[j, c] = tmp[c];
    }
}
        // ============================================================
        // 1. 桩号 / 角度工具
        // ============================================================

        /// <summary>米 → K 桩号字符串</summary>
        public static string hua_Num2K(double meters)
        {
            int km = (int)Math.Floor(meters / 1000);
            double m = meters - km * 1000;
            m = Math.Round(m, 2);
            if (m == (int)m)
                return $"K{km}+{(int)m:D3}";
            else
                return $"K{km}+{m:000.00}";
        }

        /// <summary>度分秒（DDD.MMSS）→ 弧度</summary>
        public static double hua_DmsToRadians(double dms)
        {
            int degrees = (int)dms;
            double fractional = dms - degrees;
            int minutes = (int)(fractional * 100);
            double seconds = (fractional * 100 - minutes) * 100;
            double totalDegrees = degrees + minutes / 60.0 + seconds / 3600.0;
            return totalDegrees * Math.PI / 180;
        }

        /// <summary>弧度 → 度分秒（DDD.MMSS）</summary>
        public static double hua_radiansToDMS(double radians)
        {
            double degrees = radians * (180 / Math.PI);
            int d = (int)Math.Floor(degrees);
            double remaining = (degrees - d) * 60;
            int m = (int)Math.Floor(remaining);
            double s = (remaining - m) * 60;
            string mm = m < 10 ? $"0{m}" : m.ToString();
            string ss = s < 10 ? $"0{s:F1}" : $"{s:F1}";
            string dfm = $"{d}{mm}{ss}";
            return Math.Round(double.Parse(dfm) / 10000.0, 5);
        }

        /// <summary>弧度 → 度分秒字符串</summary>
        public static string hua_radiansToDMS_度分秒(double radians)
        {
            double degrees = radians * (180 / Math.PI);
            int d = (int)Math.Floor(degrees);
            double remaining = (degrees - d) * 60;
            int m = (int)Math.Floor(remaining);
            double s = (remaining - m) * 60;
            string mm = m < 10 ? $"0{m}" : m.ToString();
            string ss = s < 10 ? $"0{s:F1}" : $"{s:F1}";
            return $"{d}°{mm}′{ss}″";
        }

        // ============================================================
        // 2. 线路计算
        // ============================================================

        /// <summary>方位角 + 距离（out 版）</summary>
        public static void hua_Fwj(double x1, double y1, double x2, double y2,
                                   out double dist, out double azimuth)
        {
            double dx = x2 - x1;
            double dy = y2 - y1;
            dist = Math.Sqrt(dx * dx + dy * dy);
            azimuth = Math.Atan2(dy, dx);
            if (azimuth < 0) azimuth += 2 * Math.PI;
        }

        /// <summary>方位角 + 距离（数组返回版）</summary>
        public static double[] hua_Fwj(double x0, double y0, double x1, double y1)
        {
            double x = x1 - x0;
            double y = y1 - y0;
            double cd = Math.Sqrt(x * x + y * y);
            double hd = Math.Atan2(y, x);
            if (hd < 0) hd += 2 * Math.PI;
            return new double[] { cd, hd };
        }

        /// <summary>线路坐标 + 方位角（高斯积分，无分配版）</summary>
        public static void hua_Zs(
            double xyk, double xyx, double xyy, double xyhd, double xycd,
            double xyqdr, double xyzdr, double xyzy, double jsk,
            double jsb, double jd,
            out double x, out double y, out double angle)
        {
            jd = hua_DmsToRadians(jd);
            double w = jsk - xyk;

            if (Math.Abs(xyqdr - xyzdr) < 0.01 && xyqdr > 0)
            {
                double a = xyhd + xyzy * Math.PI / 2;
                double da = w / xyqdr * xyzy;
                double ta = xyhd + da;
                if (ta < 0) ta += 2 * Math.PI;
                x = xyx + xyqdr * Math.Cos(a) - xyqdr * Math.Cos(a + da) + jsb * Math.Cos(a + da - xyzy * Math.PI / 2 + jd);
                y = xyy + xyqdr * Math.Sin(a) - xyqdr * Math.Sin(a + da) + jsb * Math.Sin(a + da - xyzy * Math.PI / 2 + jd);
                angle = ta;
                return;
            }

            if (xyqdr < 0.01 && xyzdr < 0.01 && xyzy < 0.01)
            {
                x = xyx + w * Math.Cos(xyhd) + jsb * Math.Cos(xyhd + jd);
                y = xyy + w * Math.Sin(xyhd) + jsb * Math.Sin(xyhd + jd);
                angle = xyhd;
                return;
            }

            double qr = xyqdr < 0.001 ? 99999999 : xyqdr;
            double zr = xyzdr < 0.001 ? 99999999 : xyzdr;
            double c = 1 / qr;
            double d = (qr - zr) / (2 * xycd * qr * zr);
            double xs = 0, ys = 0;
            for (int i = 0; i < 4; i++)
            {
                double v = GaussVv[i];
                double r = GaussRr[i];
                double f = xyhd + xyzy * v * w * (c + v * w * d);
                xs += r * Math.Cos(f);
                ys += r * Math.Sin(f);
            }
            double fhz3 = xyhd + xyzy * w * (c + w * d);
            fhz3 = (fhz3 % (2 * Math.PI) + 2 * Math.PI) % (2 * Math.PI);
            x = xyx + w * xs + jsb * Math.Cos(fhz3 + jd);
            y = xyy + w * ys + jsb * Math.Sin(fhz3 + jd);
            angle = fhz3;
        }

        /// <summary>按里程定位段落并计算坐标（无分配版）</summary>
        public static void hua_Dantiaoxianludange(
            double[,] pqx, double k, double b, double z,
            out double x, out double y, out double angle)
        {
            int segCount = pqx.GetLength(0);
            for (int i = 0; i < segCount; i++)
            {
                if (k >= pqx[i, 0] && k <= pqx[i, 0] + pqx[i, 4])
                {
                    double rad = hua_DmsToRadians(pqx[i, 3]);
                    hua_Zs(
                        pqx[i, 0], pqx[i, 1], pqx[i, 2], rad,
                        pqx[i, 4], pqx[i, 5], pqx[i, 6], pqx[i, 7],
                        k, b, z,
                        out double rx, out double ry, out double ra);
                    x = Math.Round(rx, 3);
                    y = Math.Round(ry, 3);
                    angle = ra;
                    return;
                }
            }
            x = 0; y = 0; angle = 0;
        }

        /// <summary>按里程定位段落并计算坐标（数组返回版）</summary>
        public static double[] hua_Dantiaoxianludange(double[,] pqx, double k, double b, double z)
        {
            hua_Dantiaoxianludange(pqx, k, b, z, out double x, out double y, out double angle);
            return new double[] { x, y, angle };
        }

        /// <summary>单点反算：坐标 → 桩号 + 偏距</summary>
        public static double[] hua_Fs(double[,] pqx, double fsx, double fsy)
        {
            hua_Dantiaoxianludange(pqx, pqx[0, 0], 0, 0, out double cx, out double cy, out double cAngle);
            double[] jljd = hua_Fwj(cx, cy, fsx, fsy);
            double k = pqx[0, 0];
            double cz = jljd[0] * Math.Cos(jljd[1] - cAngle);
            double pj = jljd[0] * Math.Sin(jljd[1] - cAngle);

            int hang = pqx.GetLength(0) - 1;
            double qdlc = pqx[0, 0];
            double zdlc = pqx[hang, 0] + pqx[hang, 4];
            int iter = 0;

            while (Math.Abs(cz) > 0.01 && iter < 15)
            {
                k += cz;
                iter++;
                if (k < qdlc) return new double[] { -1, -1 };
                if (k > zdlc) return new double[] { -2, -2 };

                hua_Dantiaoxianludange(pqx, k, 0, 0, out cx, out cy, out cAngle);
                jljd = hua_Fwj(cx, cy, fsx, fsy);
                cz = jljd[0] * Math.Cos(jljd[1] - cAngle);
                pj = jljd[0] * Math.Sin(jljd[1] - cAngle);
            }

            return new double[] { Math.Round(k, 3), Math.Round(pj, 3) };
        }

        /// <summary>批量反算：点云 → [桩号, 偏距, 高程]，按桩号升序</summary>
        public static double[][] hua_Fs_Batch(double[,] pqx, double[,] xy)
        {
            if (pqx == null || pqx.GetLength(0) == 0 || xy == null || xy.GetLength(0) == 0)
                return new double[0][];

            int segCount = pqx.GetLength(0);
            double[] startK = new double[segCount];
            double[] endK = new double[segCount];
            double[] startX = new double[segCount];
            double[] startY = new double[segCount];
            double[] angleRad = new double[segCount];
            double[] len = new double[segCount];
            double[] A = new double[segCount], B = new double[segCount], C = new double[segCount];

            for (int i = 0; i < segCount; i++)
            {
                startK[i] = pqx[i, 0];
                len[i] = pqx[i, 4];
                endK[i] = startK[i] + len[i];
                startX[i] = pqx[i, 1];
                startY[i] = pqx[i, 2];
                angleRad[i] = hua_DmsToRadians(pqx[i, 3]);
                A[i] = pqx[i, 5];
                B[i] = pqx[i, 6];
                C[i] = pqx[i, 7];
            }

            int pointCount = xy.GetLength(0);
            double[][] results = new double[pointCount][];

            for (int i = 0; i < pointCount; i++)
            {
                double fsx = xy[i, 0];
                double fsy = xy[i, 1];
                double fsz = xy[i, 2];

                double[] row = new double[3];

                double cx, cy, cAngle;
                hua_Zs(startK[0], startX[0], startY[0], angleRad[0], len[0],
                       A[0], B[0], C[0], startK[0], 0, 0,
                       out cx, out cy, out cAngle);

                hua_Fwj(cx, cy, fsx, fsy, out double dist, out double az);
                double k = startK[0];
                double cz = dist * Math.Cos(az - cAngle);
                double pj = dist * Math.Sin(az - cAngle);

                double qdlc = startK[0];
                double zdlc = endK[segCount - 1];
                int iter = 0;
                const int maxIter = 15;

                while (Math.Abs(cz) > 0.01 && iter < maxIter)
                {
                    k += cz;
                    iter++;

                    if (k < qdlc) { row[0] = -1; row[1] = -1; row[2] = fsz; results[i] = row; goto NextPoint; }
                    if (k > zdlc) { row[0] = -2; row[1] = -2; row[2] = fsz; results[i] = row; goto NextPoint; }

                    int segIdx = 0;
                    for (int j = 0; j < segCount; j++)
                    {
                        if (k >= startK[j] && k <= endK[j])
                        {
                            segIdx = j;
                            break;
                        }
                    }

                    hua_Zs(startK[segIdx], startX[segIdx], startY[segIdx],
                           angleRad[segIdx], len[segIdx],
                           A[segIdx], B[segIdx], C[segIdx],
                           k, 0, 0,
                           out cx, out cy, out cAngle);

                    hua_Fwj(cx, cy, fsx, fsy, out dist, out az);
                    cz = dist * Math.Cos(az - cAngle);
                    pj = dist * Math.Sin(az - cAngle);
                }

                row[0] = Math.Round(k, 3);
                row[1] = Math.Round(pj, 3);
                row[2] = fsz;
                results[i] = row;

            NextPoint:;
            }

            Array.Sort(results, (a, b) => a[0].CompareTo(b[0]));
            return results;
        }

        // ============================================================
        // 3. 匹配 / 折线工具
        // ============================================================

        /// <summary>按桩号取横断面：二分定位 + 插入排序</summary>
        public static double[,] getMatchedBZ(double[][] data, double target, double tolerance = 3.0)
        {
            int rows = data.Length;
            if (rows == 0) return new double[0, 2];

            double lower = target - tolerance;
            int left = 0, right = rows;
            while (left < right)
            {
                int mid = (left + right) >> 1;
                if (data[mid][0] < lower) left = mid + 1;
                else right = mid;
            }

            double upper = target + tolerance;

            int count = 0;
            for (int i = left; i < rows && data[i][0] <= upper; i++) count++;
            if (count == 0) return new double[0, 2];

            double[,] result = new double[count, 2];
            int n = 0;
            for (int i = left; i < rows && data[i][0] <= upper; i++)
            {
                double b = data[i][1];
                double z = data[i][2];
                int pos = n - 1;
                while (pos >= 0 && result[pos, 0] > b)
                {
                    result[pos + 1, 0] = result[pos, 0];
                    result[pos + 1, 1] = result[pos, 1];
                    pos--;
                }
                result[pos + 1, 0] = b;
                result[pos + 1, 1] = z;
                n++;
            }
            return result;
        }

        /// <summary>按 X 二分 + 线性插值求 Y</summary>
        public static double FromXgetY(double[,] points, double targetX)
        {
            const double tolerance = 1e-6;
            int n = points.GetLength(0);
            if (n == 0) return 0.0;

            int low = 0;
            int high = n - 1;
            int index = -1;

            while (low <= high)
            {
                int mid = (low + high) >> 1;
                double midX = points[mid, 0];

                if (midX == targetX) { index = mid; break; }
                if (midX < targetX) low = mid + 1;
                else high = mid - 1;
            }

            if (index >= 0) return points[index, 1];

            int rightIndex = low;
            int leftIndex = rightIndex - 1;

            if (rightIndex >= n) return points[n - 1, 1];
            if (leftIndex < 0) return points[0, 1];

            double xLeft = points[leftIndex, 0];
            double xRight = points[rightIndex, 0];

            if (Math.Abs(xRight - xLeft) < tolerance)
                return Math.Max(points[leftIndex, 1], points[rightIndex, 1]);

            double yLeft = points[leftIndex, 1];
            double yRight = points[rightIndex, 1];

            return yLeft + (yRight - yLeft) * (targetX - xLeft) / (xRight - xLeft);
        }

        /// <summary>按 Y 求所有交点 X 坐标</summary>
        public static double[] FromYgetX(double[,] points, double targetY)
        {
            const double tolerance = 1e-12;
            var result = new List<double>();
            int n = points.GetLength(0);
            if (n == 0) return Array.Empty<double>();

            for (int i = 0; i < n - 1; i++)
            {
                double x1 = points[i, 0];
                double y1 = points[i, 1];
                double x2 = points[i + 1, 0];
                double y2 = points[i + 1, 1];

                bool y1Below = y1 <= targetY + tolerance;
                bool y1Above = y1 >= targetY - tolerance;
                bool y2Below = y2 <= targetY + tolerance;
                bool y2Above = y2 >= targetY - tolerance;
                bool cross = (y1Below && y2Above) || (y1Above && y2Below);
                if (!cross) continue;

                if (Math.Abs(y2 - y1) < tolerance)
                {
                    if (result.Count == 0 || Math.Abs(result[result.Count - 1] - x1) > tolerance)
                        result.Add(x1);
                    if (i == n - 2 && (result.Count == 0 || Math.Abs(result[result.Count - 1] - x2) > tolerance))
                        result.Add(x2);
                    continue;
                }

                double t = (targetY - y1) / (y2 - y1);
                double xIntersect = x1 + t * (x2 - x1);
                if (result.Count == 0 || Math.Abs(result[result.Count - 1] - xIntersect) > tolerance)
                    result.Add(xIntersect);
            }
            return result.ToArray();
        }

        /// <summary>清表偏移：把折线整体法向平移 offset</summary>
        public static double[,] hua_OffsetPolyline(double[,] points, double offset)
        {
            if (points == null) return null;
            int ptCount = points.GetLength(0);
            if (ptCount < 2) return (double[,])points.Clone();

            int segmentCount = ptCount - 1;
            double[,] segLines = new double[segmentCount, 4];
            bool[] validSeg = new bool[segmentCount];

            for (int i = 0; i < segmentCount; i++)
            {
                double dx = points[i + 1, 0] - points[i, 0];
                double dy = points[i + 1, 1] - points[i, 1];
                double len = Math.Sqrt(dx * dx + dy * dy);
                if (len < 1e-8) continue;
                validSeg[i] = true;
                double nx = -dy / len;
                double ny = dx / len;
                segLines[i, 0] = points[i, 0] + nx * offset;
                segLines[i, 1] = points[i, 1] + ny * offset;
                segLines[i, 2] = points[i + 1, 0] + nx * offset;
                segLines[i, 3] = points[i + 1, 1] + ny * offset;
            }

            int firstIdx = -1;
            for (int i = 0; i < segmentCount; i++) if (validSeg[i]) { firstIdx = i; break; }
            if (firstIdx == -1) return new double[0, 2];

            double[,] rawVertices = new double[segmentCount + 1, 2];
            int rawCount = 0;

            rawVertices[rawCount, 0] = segLines[firstIdx, 0];
            rawVertices[rawCount, 1] = segLines[firstIdx, 1];
            rawCount++;

            int prev = firstIdx;
            for (int i = firstIdx + 1; i < segmentCount; i++)
            {
                if (!validSeg[i]) continue;

                double x1 = segLines[prev, 0], y1 = segLines[prev, 1];
                double x2 = segLines[prev, 2], y2 = segLines[prev, 3];
                double x3 = segLines[i, 0], y3 = segLines[i, 1];
                double x4 = segLines[i, 2], y4 = segLines[i, 3];
                double denom = (x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4);

                if (Math.Abs(denom) > 1e-8)
                {
                    double t1 = x1 * y2 - y1 * x2;
                    double t2 = x3 * y4 - y3 * x4;
                    rawVertices[rawCount, 0] = (t1 * (x3 - x4) - (x1 - x2) * t2) / denom;
                    rawVertices[rawCount, 1] = (t1 * (y3 - y4) - (y1 - y2) * t2) / denom;
                }
                else
                {
                    rawVertices[rawCount, 0] = (x2 + x3) / 2.0;
                    rawVertices[rawCount, 1] = (y2 + y3) / 2.0;
                }
                rawCount++;
                prev = i;
            }
            rawVertices[rawCount, 0] = segLines[prev, 2];
            rawVertices[rawCount, 1] = segLines[prev, 3];
            rawCount++;

            double[,] cleanVertices = new double[rawCount, 2];
            int cleanCount = 0;
            if (rawCount > 0)
            {
                cleanVertices[0, 0] = rawVertices[0, 0];
                cleanVertices[0, 1] = rawVertices[0, 1];
                cleanCount = 1;
            }

            for (int i = 1; i < rawCount; i++)
            {
                double cx = rawVertices[i, 0];
                double cy = rawVertices[i, 1];

                while (cleanCount > 1)
                {
                    double lastX = cleanVertices[cleanCount - 1, 0];
                    if (cx < lastX - 1e-5) cleanCount--;
                    else break;
                }

                double topX = cleanVertices[cleanCount - 1, 0];
                double topY = cleanVertices[cleanCount - 1, 1];
                double ddx = cx - topX, ddy = cy - topY;
                double dist = Math.Sqrt(ddx * ddx + ddy * ddy);

                if (dist > 1e-4)
                {
                    if (cx < topX + 1e-4 && cleanCount > 1)
                    {
                        cleanVertices[cleanCount - 1, 0] = (topX + cx) / 2.0;
                        cleanVertices[cleanCount - 1, 1] = (topY + cy) / 2.0;
                    }
                    else
                    {
                        cleanVertices[cleanCount, 0] = cx;
                        cleanVertices[cleanCount, 1] = cy;
                        cleanCount++;
                    }
                }
            }

            double[,] output = new double[cleanCount, 2];
            for (int i = 0; i < cleanCount; i++)
            {
                output[i, 0] = cleanVertices[i, 0];
                output[i, 1] = cleanVertices[i, 1];
            }
            return output;
        }

        /// <summary>构建清表面积多边形（地面线 + 清表线围成的闭合区域）</summary>
        public static double[,] BuildClearPolygon(double[,] ground, double[,] cleared, double minX, double maxX)
        {
            var list = new List<double[]>();
            list.Add(new double[] { minX, FromXgetY(ground, minX) });
            for (int i = 0; i < ground.GetLength(0); i++)
                if (ground[i, 0] > minX && ground[i, 0] < maxX)
                    list.Add(new double[] { ground[i, 0], ground[i, 1] });
            list.Add(new double[] { maxX, FromXgetY(ground, maxX) });
            list.Add(new double[] { maxX, FromXgetY(cleared, maxX) });
            for (int i = cleared.GetLength(0) - 1; i >= 0; i--)
                if (cleared[i, 0] > minX && cleared[i, 0] < maxX)
                    list.Add(new double[] { cleared[i, 0], cleared[i, 1] });
            list.Add(new double[] { minX, FromXgetY(cleared, minX) });

            double[,] mat = new double[list.Count, 2];
            for (int i = 0; i < list.Count; i++)
            {
                mat[i, 0] = list[i][0];
                mat[i, 1] = list[i][1];
            }
            return mat;
        }

        // ============================================================
        // 4. 面积 / 几何工具
        // ============================================================

        /// <summary>多边形面积（鞋带公式）</summary>
        public static double hua_PolygonArea(double[,] poly)
        {
            if (poly.GetLength(0) < 3) return 0;
            double area = 0;
            int n = poly.GetLength(0);
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                area += poly[i, 0] * poly[j, 1];
                area -= poly[i, 1] * poly[j, 0];
            }
            return Math.Abs(area) / 2.0;
        }

        /// <summary>核心填挖面积：设计线 vs 地面线求交 + 鞋带累加</summary>
        public static double[] hua_CutAndFillArea(double[,] dmx, double[,] sjx, double extendDist)
        {
            if (extendDist > 0)
            {
                int n = dmx.GetLength(0);
                if (n >= 2)
                {
                    double x1 = dmx[0, 0], y1 = dmx[0, 1];
                    double x2 = dmx[1, 0], y2 = dmx[1, 1];
                    double dx = x1 - x2, dy = y1 - y2;
                    double len = Math.Sqrt(dx * dx + dy * dy);
                    if (len > 1e-12)
                    {
                        dx /= len; dy /= len;
                        dmx[0, 0] = x1 + dx * extendDist;
                        dmx[0, 1] = y1 + dy * extendDist;
                    }
                    x1 = dmx[n - 2, 0]; y1 = dmx[n - 2, 1];
                    x2 = dmx[n - 1, 0]; y2 = dmx[n - 1, 1];
                    dx = x2 - x1; dy = y2 - y1;
                    len = Math.Sqrt(dx * dx + dy * dy);
                    if (len > 1e-12)
                    {
                        dx /= len; dy /= len;
                        dmx[n - 1, 0] = x2 + dx * extendDist;
                        dmx[n - 1, 1] = y2 + dy * extendDist;
                    }
                }
            }

            List<double[]> xys = new List<double[]>();
            double fill = 0;
            double cut = 0;
            int lenA = sjx.GetLength(0);
            int lenB = dmx.GetLength(0);

            for (int i = 0; i < lenA - 1; i++)
            {
                double x1 = sjx[i, 0];
                double y1 = sjx[i, 1];
                double x2 = sjx[i + 1, 0];
                double y2 = sjx[i + 1, 1];
                for (int j = 0; j < lenB - 1; j++)
                {
                    double x3 = dmx[j, 0];
                    double y3 = dmx[j, 1];
                    double x4 = dmx[j + 1, 0];
                    double y4 = dmx[j + 1, 1];
                    double denom = (x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4);
                    if (denom != 0)
                    {
                        double t = ((x1 - x3) * (y3 - y4) - (y1 - y3) * (x3 - x4)) / denom;
                        double u = ((x1 - x3) * (y1 - y2) - (y1 - y3) * (x1 - x2)) / denom;
                        if (t >= 0 && t <= 1 && u >= 0 && u <= 1)
                        {
                            double px = x1 + t * (x2 - x1);
                            double py = y1 + t * (y2 - y1);
                            xys.Add(new double[] { px, py, (double)i, (double)j });
                        }
                    }
                }
            }

            double minX = 0.0, maxX = 0.0, minY = 0.0, maxY = 0.0;
            int leftXysIdx = 0;
            int rightXysIdx = 0;

            if (xys.Count > 0)
            {
                minX = xys[0][0]; maxX = xys[0][0];
                minY = xys[0][1]; maxY = xys[0][1];
                for (int i = 1; i < xys.Count; i++)
                {
                    double x = xys[i][0], y = xys[i][1];
                    if (x < minX) { minX = x; leftXysIdx = i; }
                    else if (x > maxX) { maxX = x; rightXysIdx = i; }
                    if (y < minY) minY = y;
                    else if (y > maxY) maxY = y;
                }
            }

            for (int idx = 0; idx < xys.Count - 1; idx++)
            {
                double[] xy0 = xys[idx];
                double[] xy1 = xys[idx + 1];
                int i0 = (int)xy0[2];
                int i1 = (int)xy1[2];
                int j0 = (int)xy0[3];
                int j1 = (int)xy1[3];

                double px = xy0[0], py = xy0[1];
                double signedArea = 0;
                double nx, ny;

                for (int k = i0 + 1; k <= i1; k++)
                {
                    nx = sjx[k, 0]; ny = sjx[k, 1];
                    signedArea += px * ny - py * nx;
                    px = nx; py = ny;
                }
                signedArea += px * xy1[1] - py * xy1[0];
                px = xy1[0]; py = xy1[1];

                for (int k = j1; k > j0; k--)
                {
                    nx = dmx[k, 0]; ny = dmx[k, 1];
                    signedArea += px * ny - py * nx;
                    px = nx; py = ny;
                }
                signedArea += px * xy0[1] - py * xy0[0];

                if (signedArea > 0) cut += signedArea / 2.0;
                else fill += signedArea / 2.0;
            }

            List<double[]> finalSjxList = new List<double[]>();
            if (xys.Count >= 2)
            {
                double[] leftIntersection = xys[leftXysIdx];
                double[] rightIntersection = xys[rightXysIdx];
                int leftSjxSegIdx = (int)leftIntersection[2];
                int rightSjxSegIdx = (int)rightIntersection[2];

                finalSjxList.Add(new double[] { leftIntersection[0], leftIntersection[1] });
                for (int k = leftSjxSegIdx + 1; k <= rightSjxSegIdx; k++)
                    finalSjxList.Add(new double[] { sjx[k, 0], sjx[k, 1] });
                finalSjxList.Add(new double[] { rightIntersection[0], rightIntersection[1] });
            }
            else
            {
                for (int k = 0; k < lenA; k++) finalSjxList.Add(new double[] { sjx[k, 0], sjx[k, 1] });
            }

            List<double> finalResults = new List<double>
            {
                Math.Round(fill, 4),
                Math.Round(cut, 4),
                minX, maxX, minY, maxY,
                (double)finalSjxList.Count
            };
            foreach (var pt in finalSjxList)
            {
                finalResults.Add(pt[0]);
                finalResults.Add(pt[1]);
            }
            return finalResults.ToArray();
        }

        // ============================================================
        // 5. 竖曲线 / 横坡
        // ============================================================

        /// <summary>设计竖曲线高程</summary>
        public static double hua_H(double[,] sqx, double k)
        {
            int n = sqx.GetLength(0);
            if (n == 0) throw new ArgumentException("设计竖曲线wrong");
            double firstZ = sqx[0, 0], lastZ = sqx[n - 1, 0];
            if (k < firstZ || k > lastZ) return -1;
            if (n == 1) return Math.Abs(k - firstZ) < 1e-9 ? sqx[0, 1] : -1;

            int idx = -1;
            for (int i = 0; i < n - 1; i++)
                if (k >= sqx[i, 0] && k <= sqx[i + 1, 0]) { idx = i; break; }
            if (idx == -1) return -1;

            double z1 = sqx[idx, 0], h1 = sqx[idx, 1];
            double z2 = sqx[idx + 1, 0], h2 = sqx[idx + 1, 1];
            double slope = (h2 - h1) / (z2 - z1);
            double result = h1 + slope * (k - z1);

            int[] points = { idx, idx + 1 };
            foreach (int p in points)
            {
                if (p <= 0 || p >= n - 1) continue;
                double r = sqx[p, 2];
                if (r <= 0) continue;
                double zv = sqx[p, 0], hv = sqx[p, 1];
                double zPrev = sqx[p - 1, 0], hPrev = sqx[p - 1, 1];
                double i1 = (hv - hPrev) / (zv - zPrev);
                double zNext = sqx[p + 1, 0], hNext = sqx[p + 1, 1];
                double i2 = (hNext - hv) / (zNext - zv);
                double omega = i2 - i1;
                double T = r * Math.Abs(omega) / 2.0;
                double start = zv - T, end = zv + T;
                if (k >= start && k <= end)
                {
                    double tanElev = (k <= zv) ? hv + i1 * (k - zv) : hv + i2 * (k - zv);
                    double x = (k <= zv) ? (k - start) : (end - k);
                    double y = x * x / (2.0 * r) * Math.Sign(omega);
                    result = tanElev + y;
                }
            }
            return Math.Round(result, 3);
        }

        /// <summary>横坡插值（0=线性，1=二次抛物线）</summary>
        public static double hua_slope(double mileage, double[,] points, int interpType = 0)
        {
            if (points == null) throw new ArgumentNullException(nameof(points));
            int n = points.GetLength(0);
            if (n == 0) throw new ArgumentException("点表不能为空。");
            if (points.GetLength(1) < 2) throw new ArgumentException("点表必须包含至少两列：里程和横坡。");
            for (int i = 1; i < n; i++)
                if (points[i, 0] <= points[i - 1, 0])
                    throw new ArgumentException($"里程必须严格递增，第{i}行里程 {points[i, 0]} <= 上一行 {points[i - 1, 0]}");

            if (mileage <= points[0, 0]) return points[0, 1];
            if (mileage >= points[n - 1, 0]) return points[n - 1, 1];

            if (interpType == 0)
            {
                for (int i = 0; i < n - 1; i++)
                {
                    double k1 = points[i, 0], k2 = points[i + 1, 0];
                    if (mileage >= k1 && mileage <= k2)
                    {
                        double s1 = points[i, 1], s2 = points[i + 1, 1];
                        if (Math.Abs(k2 - k1) < 1e-9) return s1;
                        double t = (mileage - k1) / (k2 - k1);
                        return s1 + t * (s2 - s1);
                    }
                }
            }
            else if (interpType == 1)
            {
                int i = 0;
                for (; i < n - 1; i++)
                    if (mileage <= points[i + 1, 0]) break;
                int left, mid, right;
                if (i + 2 < n) { left = i; mid = i + 1; right = i + 2; }
                else if (i - 2 >= 0) { left = i - 2; mid = i - 1; right = i; }
                else return hua_slope(mileage, points, 0);

                double x0 = points[left, 0], y0 = points[left, 1];
                double x1 = points[mid, 0], y1 = points[mid, 1];
                double x2 = points[right, 0], y2 = points[right, 1];
                double x = mileage;
                return y0 * (x - x1) * (x - x2) / ((x0 - x1) * (x0 - x2))
                     + y1 * (x - x0) * (x - x2) / ((x1 - x0) * (x1 - x2))
                     + y2 * (x - x0) * (x - x1) / ((x2 - x0) * (x2 - x1));
            }
            else
            {
                throw new ArgumentException("不支持的插值类型，当前仅支持 0=线性，1=二次抛物线。");
            }
            return points[0, 1];
        }
    }
}