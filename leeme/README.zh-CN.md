# Laurel

*成就靠实力赢得，绝不白送。*

**其他语言：** [Español](../LEEME.md) · [English](README.en.md) · [Português](README.pt-BR.md) · [Français](README.fr.md) · [Deutsch](README.de.md) · [Русский](README.ru.md) · [日本語](README.ja.md)

为你的游戏做好准备，让 **Hydra Launcher 在你玩游戏获得成就时记录下来**。
本应用从不自行解锁成就。

## 使用方法
1. 打开 `App\Laurel.exe` 并**选择语言**。共 8 种：西班牙语、英语、葡萄牙语、法语、德语、俄语、中文和日语。如果勾选“启动时不再询问”，之后可通过 **语言** 按钮更改。
2. 首次运行时，应用会自动查找 Hydra 的下载文件夹（只读取 Hydra 的数据，不做修改）。如果游戏在其他位置，请点击 **添加…**。
3. 输入你的 **Steam API 密钥**。密钥免费，可通过“获取密钥”链接获得。没有密钥时，应用可以扫描游戏，但无法准备游戏。**密钥永远不会保存：**它只存在于内存中，因此每次打开应用都需要重新输入。
4. 点击 **准备可修复的游戏**。
5. 让应用保持打开或将其最小化（它会留在时钟旁边）。安装游戏后，它会等待安装完成并自动准备该游戏。

**提示：**列表上方的彩色卡片显示各状态的游戏数量；点击某张卡片即可只显示这些游戏。搜索框可按名称、模拟器或 AppID 筛选。

## 各状态的含义
| 状态 | 含义 |
|---|---|
| OK | Hydra 已能找到该游戏的成就文件 |
| 就绪 | 已配置完成；获得第一个成就后会生成文件 |
| STEAM | `steam_api.dll` 是 Valve 原版（签名已验证）：Hydra 从你的 Steam 账户读取成就 |
| 可修复 | 使用 Goldberg，但缺少成就列表；应用可以创建它 |
| 无 APPID | 右键点击游戏 →“设置 AppID” |
| 不支持 | 该 DLL 不是 Valve 的，也不含已知模拟器的特征：其模拟器不会把成就保存到 Hydra 查找的位置 |
| 非 STEAM | 不使用 Steam。应用会显示其使用的平台：GOG（有或没有成就）、Epic、Ubisoft、Microsoft Store 或无 |

## 如何保护你的文件
- **从不覆盖**已有文件：只创建缺失的文件。
- **从不自行删除**任何内容。
- 将创建的每个文件记录在 `cambios.json` 中。
- **撤销应用的更改** 只会把本应用创建且之后无人修改过的文件移到回收站。如果你或游戏修改过它们，则会保留。
- **模拟模式** 会显示将执行的操作，但不写入任何内容。

## DLL 识别
当游戏有 `steam_api.dll` 但没有已知的配置文件时，应用会以**只读方式**检查该 DLL：
1. 使用 Windows 验证数字签名，无需联网。如果签名来自 Valve 且有效，则该游戏是 Steam 副本。
2. 在文件中搜索已知模拟器的特征：Goldberg、CODEX、RUNE、EMPRESS、TENOKE、OnlineFix、CreamAPI、SmartSteamEmu、3DM、ALI213、SKIDROW、RLD! 和 Razor1911。

结果显示在“查看详情”中（右键点击游戏）。

## 成就桥接
有些游戏确实会记录成就，但保存在 Hydra 不读取的位置。开启 **成就桥接** 后，应用会把这些**已获得**的成就复制成 Hydra 能识别的格式（`%AppData%\GSE Saves\<AppID>\achievements.json`）。它从不凭空生成成就。
- **GOG Galaxy → Hydra：**适用于有成就的 GOG 游戏。请为其设置 Steam AppID（右键 →“设置 AppID”）。应用读取的是 GOG Galaxy 数据库的*临时副本*，从不读取原文件。如果你输入了 API 密钥，只会复制名称与 Steam 一致的成就。
- **RLE → Hydra：**Hydra 知道 RLE 的文件夹（`%AppData%\RLE`）但不会扫描它；桥接弥补了这一点。
- 如果目标文件已存在且不是本应用创建的（例如游戏使用 Goldberg），则**保持原样**。
- 桥接创建的所有内容都会记录在 `cambios.json` 中，并可撤销。

## “不支持”游戏的指南
本应用**不会安装模拟器**。对于这类游戏，右键 →**“成就追踪指南…”**会打开分步指南：
1. **备份**当前 DLL（保存为 `.hydralogros-original`，不覆盖任何文件）。
2. gbe_fork 官方仓库的链接。
3. 打开 DLL 所在文件夹，由**你自己**替换。
4. **检查：**如果检测到 gbe_fork，会自动准备游戏。

仅适用于 DLL 已不是 Valve 原版的副本。

## 隐私与 Steam API 的使用
- **保存到磁盘的内容：**仅包括游戏库、选项（包括语言）、你设置的 AppID 以及已创建文件的列表（用于撤销）。从不保存 API 密钥。如果旧的 `config.json` 中含有密钥，应用会在启动时将其删除。
- **日志不含个人数据：**在 `registro.txt` 和界面中，你的用户文件夹显示为 `%USERPROFILE%`，用户名显示为 `<用户>`，邮箱和密钥会被隐藏。
- **应用会谨慎地查询 Steam**，让密钥的使用看起来正常：
  - 每次只发送一个请求，间隔至少 3 秒；
  - 每次会话最多 150 个请求；
  - 每个游戏每次会话只查询一次；
  - 如果 Steam 拒绝密钥，在你输入其他密钥之前不会再使用；
  - 如果 Steam 要求降低频率，请求会暂停 15 分钟。
- 成就列表会以你选择的语言从 Steam 下载。

## 安全与扫描
**扫描范围：**只扫描游戏库的子文件夹，并且只查找能识别模拟器和平台的文件。不会进入音频、视频、资源或安装程序文件夹，并会隐藏不含游戏文件的文件夹。会同时检查多个游戏，每个游戏都有大小和时间上限。

**安全锁：**
- **游戏库：**不能使用磁盘根目录、Windows、Program Files、ProgramData、Users、个人资料文件夹、AppData 或临时文件夹作为游戏库，也不能使用符号链接。如果 `config.json` 中有这些路径，会被忽略。
- **链接：**从不跟随符号链接或 *junction*，也不读取仅存于云端的 OneDrive 文件（不会触发下载）。
- **写入：**只能创建 `steam_settings\achievements.json`、`steam_settings\steam_appid.txt`、`.hydralogros-original` 备份以及 `GSE Saves\<AppID>\achievements.json`。其他任何路径都会被阻止。
- **撤销：**即使有人修改 `cambios.json`，撤销也只作用于上述文件。
- **网络：**只通过 HTTPS 连接 `api.steampowered.com`，不跟随重定向，因此你的密钥不会被发送到其他服务器。只会打开两个受信任的网站。
- **不执行任何程序：**从不启动游戏文件夹中的程序。文件夹用资源管理器打开，Windows DLL 只从 `System32` 加载。
- **输入数据：**AppID 只能是数字，超过 1 MB 的文本文件会被忽略，文本搜索有时间上限。
- **日志：**`registro.txt` 达到 2 MB 时轮换。
- 以管理员身份运行时会**发出提醒**（没有必要）。

## 文件夹中的文件
- `App\Laurel.exe`：应用程序。需要 .NET 10 Desktop Runtime。
- `App\config.json`、`cambios.json`、`estado.json` 和 `registro.txt`：使用过程中由应用创建。
- `leeme\`：本文档的其他语言版本。
- `src\`：源代码。重新编译：`dotnet publish src -c Release -o App`。
- `tools\IconGen\`：生成图标（`src\app.ico`）。重新生成：`dotnet run --project tools/IconGen -- src/app.ico`。
- `Portable\Laurel.exe`：便携版。内置 .NET（无需安装任何东西），数据保存在自己的文件夹中。重新编译：`dotnet publish src -c Release -p:Portable=true -o Portable`。
- 应用中的 **说明** 按钮会以你的语言显示本文档。它使用内置在 `.exe` 中的副本：修改这些 `.md` 文件不会改变应用显示的内容（需要重新编译）。

## 许可证
Laurel 是自由软件：你可以依据 [GNU 通用公共许可证 v3.0](../LICENSE)（GPL-3.0）的条款使用、研究、修改和分享它。如果你分发修改后的版本，它也必须是自由软件并附带源代码。

*** 本工具可自由使用 ***
