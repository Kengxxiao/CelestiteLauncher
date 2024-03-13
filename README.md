# Celestite [![GitHub Release](https://img.shields.io/github/v/release/Kengxxiao/CelestiteLauncher)](https://github.com/Kengxxiao/CelestiteLauncher/releases)
![GitHub License](https://img.shields.io/github/license/Kengxxiao/CelestiteLauncher)
![GitHub last commit](https://img.shields.io/github/last-commit/Kengxxiao/CelestiteLauncher)
[![GitHub Downloads](https://img.shields.io/github/downloads/Kengxxiao/CelestiteLauncher/total)](https://github.com/Kengxxiao/CelestiteLauncher/releases)
[![Build Status](https://dev.azure.com/Kengxxiao/Celestite-opensource/_apis/build/status%2FKengxxiao.CelestiteLauncher?branchName=main)](https://dev.azure.com/Kengxxiao/Celestite-opensource/_build/latest?definitionId=5&branchName=main)

**Celestite** 是一款跨平台的第三方 **DMMGamePlayer** 实现，提供了基础的启动器功能和一些额外的高级特性，包括但不限于：  
* 在日本以外的国家/地区使用 **DMMGamePlayer** 的功能[^1]。
* 自定义游戏启动命令行。
* 在非 Windows 系统上使用 Proton/Wine 等 Windows 兼容层启动游戏。
* 通过模拟启动流程完成 DMM POINT GET MISSION 的任务。
* 以无 GUI 模式执行游戏启动流程。
* (Windows) 在基于 WebView2 的内置浏览器上以账号隔离的方式运行浏览器游戏。
  
[^1]: 该项功能不包含在开源代码中，需要在 [Releases](https://github.com/Kengxxiao/CelestiteLauncher/releases) 中下载以获得该项功能支持。

同时，**Celestite** 兼容 **DMMGamePlayer** 的数据格式，允许用户执行以下操作：
* 在 **Celestite** 上直接使用 **DMMGamePlayer** 的登录态。
* 共享 **DMMGamePlayer** 中已经下载完成的游戏。

## 使用
要使用 **Celestite**，建议您在 [Releases](https://github.com/Kengxxiao/CelestiteLauncher/releases) 中下载对应系统的最新版本。  
由于启动器本体不包含更新检查功能，因此建议您在 Github 上定期检查是否有针对 BUG 的软件更新。

## 高级启动
可以使用一些特定的命令行参数使 **Celestite** 自动执行某些功能。
* ```--nogui``` 启用无 GUI 模式，该模式在运行完成后自动退出。
* ```--username <username> --password <password>``` 在无 GUI 模式下定义要登录的 DMM 用户名和密码，如果不使用那么 **Celestite** 会使用上次在 GUI 模式下已经成功登录的账号（使用无 GUI 模式登录的账号不会被保存）。
* ```--productId <productId> --type <type>``` 要求 **Celestite** 直接启动某个游戏。如果 **Celestite** 未在后台运行，那么启动指令将会在登录成功后开始执行；反之，启动指令将会被传递给正在运行的 **Celestite** 实例执行。
### 示例
* ```Celestite.Desktop.exe --nogui --username example@example.com --password example --productId mementomori --type GCL``` 代表在无 GUI 模式下使用邮箱为 example@example.com 的账号登录 DMM 后启动本地的 MementoMori 游戏。
