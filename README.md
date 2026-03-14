# hd_mike11server

## 项目概述

本项目是一个基于 `ASP.NET + .NET Framework 4.6.1` 的水利专业模型后台服务，主要用于模型计算、输入文件读写、结果提取、统计分析和 GIS 输出。

当前主要面向以下模型引擎：

- `MIKE 11`
- `MIKE 21`
- `MIKE FLOOD`

## 技术栈

- `ASP.NET`
- `.NET Framework 4.6.1`
- `C#`
- `DHI.Mike1D.*`
- `GDAL`
- `Newtonsoft.Json`
- `MySqlConnector`
- `Kdbndp`

## 目录结构

- [Mike11](c:/Users/15257/Desktop/hd_mike11server/Mike11)：一维模型相关代码
- [Mike21](c:/Users/15257/Desktop/hd_mike11server/Mike21)：二维模型相关代码
- [Mike Flood](c:/Users/15257/Desktop/hd_mike11server/Mike%20Flood)：耦合模型相关代码
- [CatchMent](c:/Users/15257/Desktop/hd_mike11server/CatchMent)：产汇流相关代码
- [Common](c:/Users/15257/Desktop/hd_mike11server/Common)：公共工具、数据库、文件、GIS 等通用能力
- [Const_Global](c:/Users/15257/Desktop/hd_mike11server/Const_Global)：全局常量与配置
- [API](c:/Users/15257/Desktop/hd_mike11server/API)：外部依赖 DLL 与相关资源

## 当前维护重点

当前重点不是扩业务，而是治理大文件，主要包括：

- [Mike11/Res11.cs](c:/Users/15257/Desktop/hd_mike11server/Mike11/Res11.cs)
- [Mike11/Nwk11.cs](c:/Users/15257/Desktop/hd_mike11server/Mike11/Nwk11.cs)
- [Mike11/Reach.cs](c:/Users/15257/Desktop/hd_mike11server/Mike11/Reach.cs)
- [HydroModel.cs](c:/Users/15257/Desktop/hd_mike11server/HydroModel.cs)
- [Model_Ser.ashx.cs](c:/Users/15257/Desktop/hd_mike11server/Model_Ser.ashx.cs)

其中 `Res11` 和 `Nwk11` 已开始采用“主文件 + 专用子目录 + partial 分片”的方式治理。

## 构建说明

工程文件是 [bjd_model.csproj](c:/Users/15257/Desktop/hd_mike11server/bjd_model.csproj)，目标框架为 `v4.6.1`。

当前环境如果缺少 `.NET Framework 4.6.1 Developer Pack`，可能无法完成编译验证。大文件拆分阶段默认只做静态复核，重点包括：

- 工程文件编译项检查
- `partial class` 一致性检查
- 重复方法定义检查
- 命名空间冲突检查

## 相关文档

- [项目介绍与源码拆分计划.md](c:/Users/15257/Desktop/hd_mike11server/项目介绍与源码拆分计划.md)
- [AGENTS.md](c:/Users/15257/Desktop/hd_mike11server/AGENTS.md)
