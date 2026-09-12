using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using hdm.Core;
using System.Linq;
class Program
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        string cwd = Directory.GetCurrentDirectory();

        // ---------- 扫描项目文件夹 ----------
        var exclude = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "bin", "obj", "result", ".git", ".idea", "vendor", "packages", "node_modules"
        };

        var projects = new List<string>();
        foreach (var dir in Directory.GetDirectories(cwd))
        {
            string name = Path.GetFileName(dir);
            if (name.StartsWith(".")) continue;
            if (exclude.Contains(name)) continue;
            projects.Add(name);
        }
        projects.Sort(StringComparer.OrdinalIgnoreCase);

        if (projects.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("当前目录下没有找到任何项目文件夹。");
            Console.ResetColor();
            return;
        }

        // ---------- 列项目 ----------
        Console.WriteLine();
        Console.WriteLine("📁 可用的项目：");
        for (int i = 0; i < projects.Count; i++)
            Console.WriteLine($"  {i + 1}. {projects[i]}");

        Console.Write($"\n请输入项目序号 (1~{projects.Count})，或输入 0 退出: ");
        string input = Console.ReadLine()?.Trim();

        if (!int.TryParse(input, out int idx) || idx < 0 || idx > projects.Count)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"无效的序号，请输入 0~{projects.Count}");
            Console.ResetColor();
            return;
        }
        if (idx == 0) { Console.WriteLine("退出程序。"); return; }

        string selected = projects[idx - 1];
        string projectDir = Path.Combine(cwd, selected);
        Console.WriteLine($"\n开始处理项目: {selected}");

        // ---------- 加载项目 ----------
        ProjectData data;
        var sw = Stopwatch.StartNew();
        try
        {
            data = ProjectData.Load(selected, projectDir);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"项目加载失败: {ex.Message}");
            Console.ResetColor();
            return;
        }
        sw.Stop();
        Console.WriteLine($"加载完成，耗时 {sw.ElapsedMilliseconds} ms");
        Console.WriteLine($"桩号总数: {data.Stations.Length}");

        // ---------- 输入桩号 ----------
        while (true)
        {
            Console.Write("\n请输入桩号（米，例如 1000），或输入 q 退出: ");
            string stInput = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(stInput) || stInput.Equals("q", StringComparison.OrdinalIgnoreCase))
                break;

            if (!double.TryParse(stInput, out double station))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("桩号格式错误，请输入数字。");
                Console.ResetColor();
                continue;
            }

            var t = Stopwatch.StartNew();
            ComputeResult r;
            try
            {
                r = data.Query(station);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"计算异常: {ex.Message}");
                Console.ResetColor();
                continue;
            }
            t.Stop();

            Console.WriteLine();
            Console.WriteLine("=========================");
            Console.WriteLine($" 桩号: {LL.hua_Num2K(station)}  ({station} m)");
            if (!r.Success)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($" 失败: {r.ErrorMessage}");
                Console.ResetColor();
            }
            else
            {
                var s = r.Result;
/**
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
        public double[] Bounds;        
**/         
              Console.WriteLine(s);  
            }
            Console.WriteLine($" 耗时: {t.Elapsed.TotalMilliseconds:F2} ms");
            Console.WriteLine("=========================");
        }

        Console.WriteLine("退出程序。");
    }
}