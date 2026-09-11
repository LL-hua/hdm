using System;
using System.Collections.Generic;
using System.IO;

namespace hdm.Core
{
    /// <summary>
    /// 一个项目的全部输入数据 + 计算器 + 查询入口。
    /// 加载一次，复用多次。
    /// </summary>
    public class ProjectData
    {
        public string ProjectName { get; private set; } = null!;
        public string ProjectDir { get; private set; } = null!;
        public AppConfig Config { get; private set; } = null!;

        public double[,] Pqx { get; private set; } = null!;
        public double[,] Sqx { get; private set; } = null!;
        public double[,] Kzb { get; private set; } = null!;

        public SectionCalculator Calculator { get; private set; } = null!;

        private ProjectData() { }

        /// <summary>
        /// 从项目文件夹加载全部输入文件并构造计算器。
        /// 约定：所有数据文件名 = {projectName}.后缀
        /// </summary>
        public static ProjectData Load(string projectName, string projectDir)
        {
            if (!Directory.Exists(projectDir))
                throw new DirectoryNotFoundException($"项目文件夹不存在：{projectDir}");

            var data = new ProjectData
            {
                ProjectName = projectName,
                ProjectDir = projectDir,
                Config = AppConfig.Load(projectName, projectDir)
            };

            // ---------- 路径拼接 ----------
            string xyzPath = Path.Combine(projectDir, projectName + ".原地面");
            string lt = Path.Combine(projectDir, projectName + ".左板块");
            string rt = Path.Combine(projectDir, projectName + ".右板块");
            string bp = Path.Combine(projectDir, projectName + ".边坡");
            string l_st = Path.Combine(projectDir, projectName + ".左结构层");
            string r_st = Path.Combine(projectDir, projectName + ".右结构层");
            string l_cf = Path.Combine(projectDir, projectName + ".左结构层横坡");
            string r_cf = Path.Combine(projectDir, projectName + ".右结构层横坡");
            string pqxPath = Path.Combine(projectDir, projectName + ".pqx");
            string sqxPath = Path.Combine(projectDir, projectName + ".sqx");
            string kzbPath = Path.Combine(projectDir, projectName + ".k");

            // ---------- 读 + 解析 ----------
            double[,] pqx = DataReader.ReadDataFromFile(pqxPath, 8);
            double[,] sqx = DataReader.ReadDataFromFile(sqxPath, 3);
            double[,] kzb = DataReader.ReadDataFromFile(kzbPath, 1);
            double[,] xyz1 = DataReader.ReadDataFromFile(xyzPath, 3, null, true);

            double[][] mesh = LL.hua_Fs_Batch(pqx, xyz1);

            var leftWidths = LuJiYaoSuYinQing.ParseFile(lt);
            var rightWidths = LuJiYaoSuYinQing.ParseFile(rt);
            var slopeData = BianPoYinQing.ParseFile(bp);
            var leftCrossfalls = LumianSlopeManager.ParseFile(l_cf);
            var rightCrossfalls = LumianSlopeManager.ParseFile(r_cf);
            var leftStructures = LeftJiegoucengManager.ParseFile(l_st);
            var rightStructures = RightJiegoucengManager.ParseFile(r_st);

            data.Pqx = pqx;
            data.Sqx = sqx;
            data.Kzb = kzb;

            data.Calculator = new SectionCalculator(
                mesh, pqx, sqx,
                leftWidths, rightWidths, slopeData,
                leftCrossfalls, rightCrossfalls,
                leftStructures, rightStructures,
                data.Config.ClearDepth);

            return data;
        }

        /// <summary>查询单桩号断面</summary>
        public ComputeResult Query(double station)
        {
            return Calculator.Compute(station);
        }

        /// <summary>批量查询所有桩号（.k 文件里的）</summary>
        public List<ComputeResult> QueryAll()
        {
            var list = new List<ComputeResult>(Kzb.GetLength(0));
            for (int i = 0; i < Kzb.GetLength(0); i++)
            {
                double st = Kzb[i, 0];
                list.Add(Calculator.Compute(st));
            }
            return list;
        }

        /// <summary>所有桩号（.k 文件读取结果）</summary>
        public double[] Stations
        {
            get
            {
                var arr = new double[Kzb.GetLength(0)];
                for (int i = 0; i < Kzb.GetLength(0); i++) arr[i] = Kzb[i, 0];
                return arr;
            }
        }
    }
}