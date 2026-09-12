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

    /// <summary>路基车道结果包（左右两侧板块 + 点）</summary>
    public class LuJiCheDaoJieGuoBao
    {
        /// <summary>左侧点（从左到右）N×2：[x, y]</summary>
        public double[,] ZuoCeCheDaoJueDui { get; set; }

        /// <summary>右侧点（从左到右）N×2：[x, y]</summary>
        public double[,] YouCeCheDaoJueDui { get; set; }

        /// <summary>左侧板块（与左侧点一一对应）N×2：[宽度, 横坡%]</summary>
        public double[,] ZuoBanKuai { get; set; }

        /// <summary>右侧板块（与右侧点一一对应）N×2：[宽度, 横坡%]</summary>
        public double[,] YouBanKuai { get; set; }
    }

    // ============================================================
    // 2. 边坡相关
    // ============================================================

    /// <summary>边坡段落：起始/结束桩号 + 4 组相对位移（每行 [dX, dY]）</summary>
    public class BianPoDuanLuo
    {
        public double QiShiZhuangHao { get; set; }
        public double JieShuZhuangHao { get; set; }
        public double[,] ZuoTian { get; set; }
        public double[,] ZuoWa { get; set; }
        public double[,] YouTian { get; set; }
        public double[,] YouWa { get; set; }
    }

    /// <summary>边坡候选包：4 个绝对坐标序列（每行 [x, y]）</summary>
    public class BianPoHouXuanBao
    {
        public double[,] ZuoTianJueDui { get; set; }
        public double[,] ZuoWaJueDui { get; set; }
        public double[,] YouTianJueDui { get; set; }
        public double[,] YouWaJueDui { get; set; }
    }

    // ============================================================
    // 3. 结构层配置
    // ============================================================

    public class LeftJiegoucengConfig
    {
        public double StartStation { get; set; }
        public double EndStation { get; set; }
        public int LayerIndex { get; set; }
        public string LayerName { get; set; } = "";
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
        public string LayerName { get; set; } = "";
        public double Thickness { get; set; }
        public double InnerStepWidth { get; set; }
        public double InnerSlope { get; set; }
        public double OuterStepWidth { get; set; }
        public double OuterSlope { get; set; }
    }

    // ============================================================
    // 4. 计算结果
    // ============================================================

    /// <summary>单桩号计算结果（保留全部几何数据，便于后续导出 CAD）</summary>
    public class SectionResult
    {
        // ---- 位置 ----
        public double Station;       // 桩号
        public double CenterY;       // 中桩设计高程

        // ---- 关键点（单个点用 double[]）----
        public double[] LOuter;      // 左路基外缘 [x, y]
        public double[] ROuter;      // 右路基外缘 [x, y]
        public double[] LToe;        // 左坡脚 [x, y]
        public double[] RToe;        // 右坡脚 [x, y]

        // ---- 折线 ----
        public double[,] Ground;         // 地面线
        public double[,] Cleared;        // 清表线
        public double[,] Design;         // 设计线（裁剪后）
        public double[,] Finished;       // 完工线
        public double[,] LeftSubgrade;   // 左路基线
        public double[,] RightSubgrade;  // 右路基线
        public List<double[,]> Layers;   // 结构层多边形

        // ---- 顶面板块（与车道点一一对应）----
        public double[,] LeftSlabs;      // N×2：[宽度, 横坡%]
        public double[,] RightSlabs;

        // ---- 边坡 ----
        public double[,] LeftSlopeRaw;       // 左边坡（裁剪前，完整）
        public double[,] LeftSlopeTrimmed;   // 左边坡（裁剪后）
        public double[,] RightSlopeRaw;      // 右边坡（裁剪前，完整）
        public double[,] RightSlopeTrimmed;  // 右边坡（裁剪后）

        // ---- 面积 ----
        public double FillArea, CutArea, ClearArea;
        public List<string> LayerAreas;      // 结构层面积文本

        // ---- 包围盒 ----
        public double[] Bounds;              // [MinX, MinY, MaxX, MaxY]
    }

    /// <summary>计算结果的包装（成功/失败/错误信息）</summary>
    public class ComputeResult
    {
        public bool Success { get; set; }
        public SectionResult Result { get; set; } = null!;
        public string ErrorMessage { get; set; } = null!;
    }
}