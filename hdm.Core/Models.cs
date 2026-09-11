using System.Collections.Generic;

namespace hdm.Core
{
    // ============================================================
    // 1. 路基宽度相关
    // ============================================================

    /// <summary>断面数据：一个桩号 + 若干板块（宽度, 横坡%）</summary>
    public class DuanMianShuJu
    {
        public double ZhuangHao { get; set; }
        public List<double[]> BanKuaiJiHe { get; set; } = new List<double[]>();
    }

    /// <summary>绝对板块点：中桩为 0，左负右正</summary>
    public class JueDuiBanKuaiDian
    {
        public double WidthX { get; set; }
        public double GaoChengY { get; set; }

        public JueDuiBanKuaiDian(double x, double y) { WidthX = x; GaoChengY = y; }
    }

    /// <summary>路基车道结果包：左右两侧拐点</summary>
    public class LuJiCheDaoJieGuoBao
    {
        public List<JueDuiBanKuaiDian> ZuoCeCheDaoJueDui { get; set; } = new List<JueDuiBanKuaiDian>();
        public List<JueDuiBanKuaiDian> YouCeCheDaoJueDui { get; set; } = new List<JueDuiBanKuaiDian>();
    }

    // ============================================================
    // 2. 边坡相关
    // ============================================================

    /// <summary>边坡段落：起始/结束桩号 + 4 组相对位移</summary>
    public class BianPoDuanLuo
    {
        public double QiShiZhuangHao { get; set; }
        public double JieShuZhuangHao { get; set; }
        public List<double[]> ZuoTian { get; set; }
        public List<double[]> ZuoWa { get; set; }
        public List<double[]> YouTian { get; set; }
        public List<double[]> YouWa { get; set; }
    }

    /// <summary>边坡候选包：4 个绝对坐标序列</summary>
    public class BianPoHouXuanBao
    {
        public List<double[]> ZuoTianJueDui { get; set; } = new List<double[]>();
        public List<double[]> ZuoWaJueDui { get; set; } = new List<double[]>();
        public List<double[]> YouTianJueDui { get; set; } = new List<double[]>();
        public List<double[]> YouWaJueDui { get; set; } = new List<double[]>();
    }

    // ============================================================
    // 3. 横坡
    // ============================================================

    public struct CrossfallRecord
    {
        public double Station;
        public double Slope;   // 绝对横坡(%)，例如 -2.5 表示 -2.5%
    }

    // ============================================================
    // 4. 结构层
    // ============================================================

    public class LeftPoint2D
    {
        public double X { get; set; }
        public double Y { get; set; }
        public LeftPoint2D(double x, double y) { X = x; Y = y; }
    }

    public class RightPoint2D
    {
        public double X { get; set; }
        public double Y { get; set; }
        public RightPoint2D(double x, double y) { X = x; Y = y; }
    }

    public class LeftJiegoucengConfig
    {
        public double StartStation { get; set; }
        public double EndStation { get; set; }
        public int LayerIndex { get; set; }
        public string LayerName { get; set; }
        public double Thickness { get; set; }
        public double InnerStepWidth { get; set; }
        public double InnerSlope { get; set; }
        public double OuterStepWidth { get; set; }
        public double OuterSlope { get; set; }
    }

    public class RightJiegoucengConfig
    {
        public double StartStation { get; set; }
        public double EndStation { get; set; }
        public int LayerIndex { get; set; }
        public string LayerName { get; set; }
        public double Thickness { get; set; }
        public double InnerStepWidth { get; set; }
        public double InnerSlope { get; set; }
        public double OuterStepWidth { get; set; }
        public double OuterSlope { get; set; }
    }

    // ============================================================
    // 5. 计算结果
    // ============================================================

    /// <summary>单桩号计算结果（保留全部几何数据，便于后续导出 CAD）</summary>
    public class SectionResult
    {
        public double Station;
        public double CenterY;
        public double LOuterX, LOuterY;
        public double ROuterX, ROuterY;
        public double LeftCrossfall, RightCrossfall;
        public double[,] FinalDesign;
        public double[,] FinalFinished;
        public double[,] Ground;
        public double[,] Cleared;
        public double[,] LeftSubgrade;
        public double[,] RightSubgrade;
        public List<double[,]> LayerPolygons;
        public List<string> LayerAreaTexts;
        public double FillArea, CutArea, ClearArea;
        public double MinX, MaxX, MinY;
        public List<double[]> LeftSlopePoints;
        public List<double[]> RightSlopePoints;
        public double LeftToeX, LeftToeY, RightToeX, RightToeY;
    }

    /// <summary>计算结果的包装（成功/失败/错误信息）</summary>
    public class ComputeResult
    {
        public bool Success { get; set; }
        public SectionResult Result { get; set; }
        public string ErrorMessage { get; set; }
    }
}