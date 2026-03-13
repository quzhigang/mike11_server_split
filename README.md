# hd_mike11server

## 项目概述

本项目是一个基于 `ASP.NET + .NET Framework 4.6.1` 开发的水利专业模型后台服务，用于为 B/S 架构的数字孪生系统提供模型计算与结果处理能力。

项目当前主要面向河道一维水动力学模型，同时集成 DHI MIKE 系列模型引擎，重点包括：

- `MIKE 11`
- `MIKE 21`
- `MIKE FLOOD`

系统主要负责：

- 方案管理
- 模型参数设置
- 输入文件读写
- 模型计算引擎调用
- 结果提取
- 统计分析
- GIS 化输出

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

- [Mike11](c:/Users/15257/Desktop/hd_mike11server/Mike11)：一维模型相关核心代码
- [Mike21](c:/Users/15257/Desktop/hd_mike11server/Mike21)：二维模型相关代码
- [Mike Flood](c:/Users/15257/Desktop/hd_mike11server/Mike%20Flood)：耦合模型相关代码
- [CatchMent](c:/Users/15257/Desktop/hd_mike11server/CatchMent)：产汇流相关代码
- [Common](c:/Users/15257/Desktop/hd_mike11server/Common)：公共工具、数据库、文件、GIS、语音等通用能力
- [Const_Global](c:/Users/15257/Desktop/hd_mike11server/Const_Global)：全局常量与配置
- [API](c:/Users/15257/Desktop/hd_mike11server/API)：外部依赖 DLL 与相关资源

## 核心文件

当前项目内体量较大的核心源码文件主要包括：

- [Mike11/Res11.cs](c:/Users/15257/Desktop/hd_mike11server/Mike11/Res11.cs)
- [Mike11/Nwk11.cs](c:/Users/15257/Desktop/hd_mike11server/Mike11/Nwk11.cs)
- [HydroModel.cs](c:/Users/15257/Desktop/hd_mike11server/HydroModel.cs)
- [Model_Ser.ashx.cs](c:/Users/15257/Desktop/hd_mike11server/Model_Ser.ashx.cs)

其中 `Res11` 当前已经开始按 `partial class` 方式拆分到 [Mike11/Res11](c:/Users/15257/Desktop/hd_mike11server/Mike11/Res11) 目录。

## Res11 当前拆分结构

`Res11` 目前按业务主题拆分为多个分片，主要包括：

- 结果模型与枚举
- 断面结果
- 水库结果
- 河段水量过程
- 闸门状态与闸门流量
- 风险分析
- 预警与巡查
- GIS 结果
- 时间序列辅助
- 查询与转换
- 序列化与反序列化
- 持久化加载与写入

## 开发与构建说明

当前工程文件为 [bjd_model.csproj](c:/Users/15257/Desktop/hd_mike11server/bjd_model.csproj)，目标框架是 `v4.6.1`。

本地构建前需要确认：

- 已安装 `.NET Framework 4.6.1 Developer Pack`
- `API/mike` 下的 DHI 相关 DLL 完整可用
- 数据库相关 DLL 可正常引用

如果缺少 `4.6.1 Developer Pack`，MSBuild 通常会报 `MSB3644`。

## 当前维护重点

当前代码治理重点不是新增业务，而是：

- 拆分超大源码文件
- 让业务边界更清晰
- 降低调试时跨文件跳转
- 保持原有功能、数据库行为和外部接口稳定

## 相关文档

- [项目介绍与源码拆分计划.md](c:/Users/15257/Desktop/hd_mike11server/项目介绍与源码拆分计划.md)
- [AGENTS.md](c:/Users/15257/Desktop/hd_mike11server/AGENTS.md)
