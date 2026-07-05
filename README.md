# 一键下班

轻量级 Windows 后台工具。点击“一键下班”后，应用会显示下班前注意事项确认窗口；用户确认或倒计时结束后，仅调用 Windows 显示器息屏 API，不关机、不注销、不睡眠。

## 技术栈

- C# / .NET 8
- WPF
- MVVM 轻量实现
- 本地 JSON 存储
- Windows Forms NotifyIcon 系统托盘
- Win32 `SendMessage(WM_SYSCOMMAND + SC_MONITORPOWER)` 息屏

## 项目结构

```text
OneClickOffWork/
  OneClickOffWork.sln
  src/
    OneClickOffWork.App/
      App.xaml
      MainWindow.xaml
      Views/
      ViewModels/
      Models/
      Services/
      Commands/
      Utils/
      Assets/
      Styles/
```

## 本地运行

当前机器需要安装 .NET 8 SDK。只有 .NET Runtime 不足以构建项目。

```powershell
cd OneClickOffWork
dotnet build
dotnet run --project src\OneClickOffWork.App
```

## 发布普通 exe

推荐直接双击运行：

```text
publish-exe.bat
```

发布完成后会生成：

```text
publish/win-x64/OneClickOffWork.exe
```

这个发布目录是压缩单文件自包含版本，已经携带 .NET 8 桌面运行时所需内容。目标电脑不需要额外安装 .NET 8 Desktop Runtime 或 .NET 8 SDK。

也可以手动执行：

```powershell
cd OneClickOffWork
dotnet publish src\OneClickOffWork.App -c Release -r win-x64 --self-contained true -p:UseAppHost=true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false -p:SatelliteResourceLanguages=zh-Hans -o publish\win-x64
```

输出目录通常位于：

```text
src/OneClickOffWork.App/bin/Release/net8.0-windows/win-x64/publish/
```

如需未压缩单文件自包含发布：

```powershell
dotnet publish src\OneClickOffWork.App -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## 数据位置

应用数据仅保存在本地，不上传服务器。

```text
%AppData%/OneClickOffWork/settings.json
%AppData%/OneClickOffWork/reminders.json
%AppData%/OneClickOffWork/logs.json
```

如果 JSON 损坏，程序会备份损坏文件并生成默认配置。

## 已实现功能

- 后台常驻与系统托盘
- 托盘菜单：打开主界面、一键下班、注意事项、设置、退出
- 关闭窗口时默认隐藏到托盘
- 首次关闭托盘气泡提示
- 首页状态与今日提醒摘要
- 注意事项新增、编辑、删除、启用/禁用、上下排序、导入、导出、恢复默认
- 设置保存、备份、恢复、恢复默认
- 开机自启动开关
- 倒计时默认 120 秒
- 首次使用引导
- 浅色、深色、跟随系统主题
- 应用内 toast 与托盘气泡
- Ctrl + Alt + D 快捷键触发一键下班
- 防重复触发下班流程
- 确认窗口置顶
- 倒计时最后 10 秒轻微视觉提示
- 单实例运行
- 本地日志记录

## 息屏说明

`PowerService` 使用 Windows API：

```text
SendMessage(HWND_BROADCAST, WM_SYSCOMMAND, SC_MONITORPOWER, 2)
```

该调用仅请求显示器进入低功耗/关闭状态，不执行系统关机、注销或睡眠。

## 打包建议

普通安装包可以使用：

- Inno Setup
- WiX Toolset
- MSIX Packaging Tool

如果后续需要 MSIX，建议补充：

- 正式应用图标
- `Package.appxmanifest`
- 代码签名证书
- 开机自启改用 MSIX startup task

## 后续扩展

- 增加真正的 Acrylic/Mica 背景效果
- 增加 Windows App SDK 通知，替代轻量 app toast
- 增加提醒拖拽排序
- 增加多套下班清单模板
- 增加企业策略：禁用自动倒计时、统一默认提醒
- 增加日志查看页面
- 增加自动更新能力
