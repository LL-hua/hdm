# hdm

道路横断面土方计算工具。核心算法与宿主分离，同一套计算逻辑可挂接 Excel、AutoCAD、控制台。

## 项目结构

hdm/
├── hdm.Core/          纯计算类库（无 I/O 平台依赖）
├── hdm.Console/       控制台测试宿主
├── hdm.ExcelAddIn/    Excel 加载项（.xll）
└── hdm.sln

## 三个子项目

| 项目 | 作用 | 目标框架 |
|------|------|----------|
| hdm.Core | 核心算法：线路反算、边坡、结构层、填挖面积 | net8.0 |
| hdm.Console | 控制台测试：选项目 - 输桩号 - 出面积 | net8.0 |
| hdm.ExcelAddIn | Excel UDF：=LL_FillCut("项目", 桩号) | net8.0-windows |

核心原则：hdm.Core 只依赖 .NET 标准库，不读文件、不画图、不依赖任何宿主。所有 I/O、绘图、UI 都在外层。

## 编译

### 控制台

cd hdm.Console
dotnet run

### Excel 加载项

Windows 上编译（需要 .NET 8 SDK）：

dotnet build hdm.ExcelAddIn -c Release

产物：hdm.ExcelAddIn/bin/Release/net8.0-windows/hdm-AddIn64.xll

注意：hdm.ExcelAddIn 必须用 net8.0-windows，Linux/macOS 上无法编译（缺 Windows Desktop SDK）。

## 使用

### 项目文件夹约定

每个项目是 .xll（或 .dll）同级目录下的一个文件夹，名字即项目名。

D:/hdm/
├── hdm-AddIn64.xll
├── lot1/
│   ├── lot1.pqx                 线路参数（8列）
│   ├── lot1.sqx                 竖曲线（3列）
│   ├── lot1.k                   桩号列表（1列）
│   ├── lot1.原地面              原地面点（3列，跳过首行）
│   ├── lot1.左板块              左路基宽度
│   ├── lot1.右板块              右路基宽度
│   ├── lot1.边坡                边坡段落（每5行一个段落）
│   ├── lot1.左结构层            左结构层配置
│   ├── lot1.右结构层            右结构层配置
│   ├── lot1.左结构层横坡        左横坡
│   ├── lot1.右结构层横坡        右横坡
│   └── lot1.config              可选：清表深度等配置
└── lot2/
    └── ...

### Excel UDF

| 函数 | 说明 | 示例 |
|------|------|------|
| LL_FillCut(project, station) | 返回 [填方, 挖方, 清表] 一行三列 | =LL_FillCut("lot1", 85525) |
| LL_ListProjects() | 列出所有项目 | =LL_ListProjects() |
| LL_Tips() | 返回项目列表文本 | =LL_Tips() |
| LL_RefreshCache() | 清空项目缓存 | =LL_RefreshCache() |

用法：

1. 把 .xll 和项目文件夹放在同一目录
2. Excel - 文件 - 选项 - 加载项 - 转到 - 浏览 - 选 .xll
3. 单元格输入 =LL_FillCut("lot1", 85525)，结果溢出到右侧三格：填方 | 挖方 | 清表

缓存：项目首次调用时加载（约 260 ms），后续查询 1~3 ms。改了源文件后调用 =LL_RefreshCache() 重新加载。

### 控制台

cd hdm.Console
dotnet run

## 配置文件（可选）

{项目名}.config，每行 Key = Value，分号后为注释。

| 键 | 默认值 | 说明 |
|----|--------|------|
| ClearDepth | -0.3 | 清表深度（米） |
| XyzRows | 35 | 原地面网格行数（保留） |
| XyzCols | 60 | 原地面网格列数（保留） |

示例：

ClearDepth = -0.3    ; 清表深度

## 性能

| 场景 | 耗时 |
|------|------|
| 项目加载（首次） | ~260 ms |
| 单桩号查询（首次） | ~440 ms（含 JIT） |
| 单桩号查询（稳态） | 1~3 ms |
| 1300 桩号批量 | 几乎瞬间 |

## 架构

输入文件（.pqx/.sqx/.k/.原地面/...）
        ↓
   Parsers.cs（解析 + 计算依赖）
        ↓
   SectionCalculator.Compute(station)
        ↓
   SectionResult（面积 + 几何数据）
        ↓
   Console 打印 / Excel 返回数组 / AutoCAD 绘图（待做）

核心算法（LL.cs + SectionCalculator.cs）与宿主解耦，新增宿主只需：

1. 引用 hdm.Core
2. 写薄薄一层 I/O 适配

## 待办

- [ ] AutoCAD 加载项（hdm.AutoCad）
- [ ] 批量查询 UDF（LL_FillCutBatch）
- [ ] 返回更多字段（中桩高程、清表边界、逐层面积）
- [ ] 桩号字符串输入（"K85+525"）

## 许可

git add README.md && git commit -m "添加 README" && git push