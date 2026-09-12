using System;
using System.Globalization;
using System.IO;

namespace hdm.Core
{
    /// <summary>
    /// 项目配置（每个项目一份实例）
    /// 只保留纯计算需要的 3 个参数。
    /// </summary>
    public class AppConfig
    {
        // ---------- 默认值 ----------
        public const double DefaultClearDepth = -0.3;
        public const int DefaultXyzRows = 35;
        public const int DefaultXyzCols = 60;

        // ---------- 可配置参数 ----------
        /// <summary>清表深度（负值表示向下偏移）</summary>
        public double ClearDepth { get; set; } = DefaultClearDepth;

        /// <summary>原地面网格行数（保留字段，当前计算未使用）</summary>
        public int XyzRows { get; set; } = DefaultXyzRows;

        /// <summary>原地面网格列数（保留字段，当前计算未使用）</summary>
        public int XyzCols { get; set; } = DefaultXyzCols;

        /// <summary>
        /// 从项目文件夹加载配置文件（{projectName}.config）。
        /// 文件不存在或解析失败 → 使用默认值，不抛异常。
        /// 格式：每行 "Key = Value"，分号后为注释。
        /// </summary>
        public static AppConfig Load(string projectName, string projectDir)
        {
            var cfg = new AppConfig();
            string configPath = Path.Combine(projectDir, projectName + ".config");
            if (!File.Exists(configPath))
                return cfg;

            try
            {
                foreach (string rawLine in File.ReadAllLines(configPath))
                {
                    string line = rawLine.Trim();
                    if (line.Length == 0 || line.StartsWith(";"))
                        continue;

                    int eq = line.IndexOf('=');
                    if (eq < 0) continue;

                    string key = line.Substring(0, eq).Trim();
                    string val = line.Substring(eq + 1).Trim();

                    // 去掉行内注释
                    int comment = val.IndexOf(';');
                    if (comment >= 0) val = val.Substring(0, comment).Trim();

                    switch (key)
                    {
                        case "ClearDepth":
                            if (double.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out double cd))
                                cfg.ClearDepth = cd;
                            break;

                        case "XyzRows":
                            if (int.TryParse(val, out int rows))
                                cfg.XyzRows = rows;
                            break;

                        case "XyzCols":
                            if (int.TryParse(val, out int cols))
                                cfg.XyzCols = cols;
                            break;
                    }
                }
            }
            catch
            {
                // 配置文件解析失败 → 静默回退到默认值
            }

            return cfg;
        }
    }
}

