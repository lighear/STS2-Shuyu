# 梳雨 Mod 后续开发与构建指南

## 1. 先记住这四件事

1. 梳雨只有一套业务源码，但会构建出两套内容程序集。
2. 0.107.1 和 0.111 使用的游戏 API 与 RitsuLib 编译包不同，不能使用同一个内容 DLL。
3. 顶层 Shuyu.dll 是加载器，真正的内容 DLL 必须放在 lib/版本/Shuyu.dll。
4. 每次修改功能后，两套内容 DLL 都要重新构建，否则两个游戏版本里的功能会不一致。

最终结构：

~~~text
dist/Shuyu/
├─ Shuyu.dll                 # Shuyu.Loader
├─ Shuyu.json
├─ Shuyu.pck
└─ lib/
   ├─ 0.107.1/
   │  └─ Shuyu.dll           # 107.1 内容程序集
   └─ 0.111.0/
      └─ Shuyu.dll           # 111 内容程序集
~~~

dist 被 .gitignore 忽略，不会进入 Git。换电脑、清理目录或重新克隆仓库后，需要重新生成或从发布备份恢复。

当前 dist 中的 107 DLL 是此前验证过的 Debug 构建。正式发布前，建议切回 0.107.1，按本文命令重新生成 Release 版并再次验证。

## 2. 哪些功能修改通常只写一次

普通游戏内容继续在现有目录里修改：

~~~text
ShuyuCode/
├─ Cards/
├─ Powers/
├─ Relics/
├─ Potions/
├─ Characters/
├─ Commands/
├─ Patches/
└─ Vfx/
~~~

以下修改通常只需要写一份：

- 卡牌数值、费用、描述和升级效果。
- 能力、遗物、药水的普通逻辑。
- 本地化文本。
- 图片、场景、粒子和音效资源。
- 不涉及版本 API 差异的命令调用。

修改完成后分别构建 107 和 111，条件编译会自动选择对应 API。

不要复制出 ShuyuCode107 和 ShuyuCode111 两套业务源码。两份源码很容易在后续更新时失去同步。

## 3. 什么时候需要修改兼容层

兼容层位于：

~~~text
ShuyuCode/Compat/
├─ AttackCommandCompat.cs
├─ CreatureCmdCompat.cs
├─ CardCloneCompat.cs
└─ AsyncMethodCompat.cs
~~~

如果新增代码在 111 可以编译，但切换到 107 后出现以下情况，就可能需要兼容层：

- 同一个方法在 107 与 111 的参数数量不同。
- 返回类型不同。
- 111 新增了 107 不存在的辅助方法。
- virtual 或 override 方法签名发生变化。
- Harmony 目标方法或 async 状态机结构发生变化。

### 3.1 普通静态或实例方法

优先把差异放进 ShuyuCode/Compat，不要在很多卡牌文件里重复写条件编译。

现有例子：

~~~csharp
await CreatureCmdCompat.Damage(
    choiceContext,
    target,
    damage,
    props,
    dealer,
    cardSource,
    cardPlay);
~~~

兼容层内部：

~~~csharp
#if STS2_107
return CreatureCmd.Damage(
    choiceContext, target, damage, props, dealer, cardSource);
#else
return CreatureCmd.Damage(
    choiceContext, target, damage, props, dealer, cardSource, cardPlay);
#endif
~~~

这样 111 仍保留 CardPlay 上下文，只有 107 丢弃它无法接收的参数。

### 3.2 virtual 和 override

override 的签名必须与当前游戏 DLL 完全一致，因此通常只能在业务类里使用条件编译：

~~~csharp
#if STS2_107
public override decimal ModifyDamageMultiplicative(
    Creature? target,
    decimal amount,
    ValueProp props,
    Creature? dealer,
    CardModel? cardSource)
#else
public override decimal ModifyDamageMultiplicative(
    Creature? target,
    decimal amount,
    ValueProp props,
    Creature? dealer,
    CardModel? cardSource,
    CardPlay? cardPlay)
#endif
{
    // 两个版本共用同一段业务逻辑。
}
~~~

尽量只让方法头分版本，方法体保持共用。

### 3.3 新增攻击卡

现有攻击构建代码可以继续写：

~~~csharp
AttackCommand
    .Damage(...)
    .FromCard(this, cardPlay);
~~~

107 会自动绑定到 AttackCommandCompat 扩展方法，111 会绑定到游戏原生方法。

不要为了让 107 编译而把所有调用直接改成 FromCard(this)。这样会让 111 丢失 CardPlay 信息。

### 3.4 跨玩家复制卡牌

继续使用：

~~~csharp
CardModel clone = this.CreateCloneForPlayer(otherPlayer);
~~~

107 会使用 CardCloneCompat，111 使用游戏原生方法。

修改这部分时必须进行多人测试，重点确认：

- Owner 是否正确。
- 卡牌是否进入正确玩家的牌堆。
- 升级、动态变量和 CloneOf 是否保留。
- 两端是否得到相同模型和状态。

### 3.5 async Harmony 补丁

不要硬编码以下名称：

~~~text
<SomeAsyncMethod>d__26
<SomeAsyncMethod>d__29
~~~

这些编号会随游戏重新编译而变化。应使用 AsyncMethodCompat.GetStateMachineType 或 Harmony/RitsuLib 提供的 async 目标机制。

## 4. 哪些文件通常不要修改

普通卡牌、能力和数值更新通常不需要修改：

~~~text
Shuyu.Loader/
├─ Bootstrap.cs
├─ ReflectionHelperModTypesPatch.cs
└─ ModelIdSerializationCacheRebuildPatch.cs
~~~

只有以下情况才需要修改加载器：

- 新增正式支持的游戏 API 版本。
- 改变 lib 目录名称。
- 游戏修改了 ModManager、ReflectionHelper 或 Godot 脚本注册机制。
- 日志明确显示加载器无法关联程序集或找不到内容类型。

不要把 RitsuLib 引用加入加载器工程。加载器应保持只依赖 sts2.dll 和 0Harmony.dll。

## 5. 资源与 PCK 注意事项

Shuyu.pck 是两个游戏版本共用的资源包。

以下修改需要重新导出 PCK：

- 新增或修改图片。
- 新增或修改 tscn、tres、gdshader。
- 修改本地化 JSON。
- 修改资源路径或场景引用。
- 新增需要由场景实例化的 Godot C# 脚本类型。

只修改普通 C# 数值或逻辑时，一般不需要重新导出 PCK。

资源路径必须继续使用：

~~~text
res://Shuyu/...
~~~

不要因为程序集改成加载器结构而把资源目录改成 Shuyu.Loader。

如果新增卡牌或能力，检查：

- 注册 attribute 是否正确。
- 图片和本地化 ID 是否与模型名一致。
- 中英文文本是否都存在。
- PCK 是否包含新资源。
- Ritsu 分析器是否出现 RITSU013 等资源警告。

## 6. 构建前检查

### 6.1 确认当前游戏版本

~~~powershell
Get-Content 'D:\Steam\steamapps\common\Slay the Spire 2\release_info.json'
~~~

构建 107 内容 DLL 时，Sts2DataDir 必须指向 0.107.1 的游戏 DLL。

构建 111 内容 DLL 时，Sts2DataDir 必须指向 0.111 的游戏 DLL。

仅仅把 Sts2CompatVersion 改成 107，不会自动把本机的 111 sts2.dll 变成 107。目标版本和实际游戏 DLL 不一致时，构建结果无效或会直接报错。

### 6.2 检查 local.props

local.props 至少需要：

~~~xml
<Project>
  <PropertyGroup>
    <Sts2Dir>D:\Steam\steamapps\common\Slay the Spire 2</Sts2Dir>
    <Sts2DataDir>$(Sts2Dir)\data_sts2_windows_x86_64</Sts2DataDir>
    <Sts2CompatVersion>107</Sts2CompatVersion>
    <GodotExe>D:\你的Godot目录\Godot.exe</GodotExe>
  </PropertyGroup>
</Project>
~~~

不要把游戏 sts2.dll、RitsuLib DLL 或商业游戏资源提交到 Git。

## 7. 构建 0.107.1 内容程序集

先把 Steam 游戏切换到 0.107.1，并确认 release_info.json。

每次从 111 切换到 107 后，都先执行 restore：

~~~powershell
dotnet restore .\Shuyu.csproj -p:Sts2CompatVersion=107 -p:CopyModOnBuild=false -p:RunPckExport=false
~~~

再构建 Release：

~~~powershell
dotnet build .\Shuyu.csproj -c Release --no-restore -p:Sts2CompatVersion=107 -p:CopyModOnBuild=false -p:RunPckExport=false
~~~

输出：

~~~text
.godot\mono\temp\bin\107\Release\Shuyu.dll
~~~

复制到 dist：

~~~powershell
New-Item -ItemType Directory -Force -Path .\dist\Shuyu\lib\0.107.1 | Out-Null
Copy-Item .\.godot\mono\temp\bin\107\Release\Shuyu.dll .\dist\Shuyu\lib\0.107.1\Shuyu.dll -Force
~~~

## 8. 构建 0.111 内容程序集

先把 Steam 游戏切换到 0.111，并确认 release_info.json。

每次从 107 切换到 111 后，都先执行 restore：

~~~powershell
dotnet restore .\Shuyu.csproj -p:Sts2CompatVersion=111 -p:CopyModOnBuild=false -p:RunPckExport=false
~~~

再构建 Release：

~~~powershell
dotnet build .\Shuyu.csproj -c Release --no-restore -p:Sts2CompatVersion=111 -p:CopyModOnBuild=false -p:RunPckExport=false
~~~

输出：

~~~text
.godot\mono\temp\bin\111\Release\Shuyu.dll
~~~

复制到 dist：

~~~powershell
New-Item -ItemType Directory -Force -Path .\dist\Shuyu\lib\0.111.0 | Out-Null
Copy-Item .\.godot\mono\temp\bin\111\Release\Shuyu.dll .\dist\Shuyu\lib\0.111.0\Shuyu.dll -Force
~~~

## 9. 为什么切换版本后必须 restore

107 使用：

~~~text
STS2.RitsuLib.Compat.0.107.1 0.5.13
~~~

111 使用：

~~~text
STS2.RitsuLib 0.5.13
~~~

虽然两个版本有独立的编译输出目录，但 NuGet 的 project.assets.json 仍可能被最近一次 restore 更新。切换 Sts2CompatVersion 后直接使用 --no-restore，可能继续使用上一个版本的 RitsuLib 包。

安全规则：

- 切换 107/111 后，先按目标版本 restore。
- restore 成功后，才可以用 --no-restore build。
- 不确定当前依赖状态时，再执行一次目标版本 restore，不要猜。

## 10. 构建加载器

普通业务代码变化不需要重新构建加载器，但完整发布时重新构建一次最稳妥：

~~~powershell
dotnet build .\Shuyu.Loader\Shuyu.Loader.csproj -c Release
~~~

输出：

~~~text
.godot\mono\temp\bin\loader\Release\net9.0\Shuyu.Loader.dll
~~~

复制到 dist 顶层并重命名：

~~~powershell
Copy-Item .\.godot\mono\temp\bin\loader\Release\net9.0\Shuyu.Loader.dll .\dist\Shuyu\Shuyu.dll -Force
~~~

注意：

- 源文件叫 Shuyu.Loader.dll。
- 发布后的文件名必须叫 Shuyu.dll。
- 程序集内部名称仍必须是 Shuyu.Loader。

## 11. 复制 manifest

~~~powershell
Copy-Item .\Shuyu.json .\dist\Shuyu\Shuyu.json -Force
~~~

正常情况下应保持：

~~~json
{
  "min_game_version": "0.107.1",
  "dependencies": [
    {
      "id": "STS2-RitsuLib",
      "version": "0.5.13"
    }
  ]
}
~~~

不要把 min_game_version 改回 0.111.0，否则 107.1 会在加载器运行前被游戏拒绝。

不要把 RitsuLib 依赖降低到 0.5.12；双版本结构依赖 0.5.13。

## 12. 导出共用 PCK

如果资源发生变化，用 local.props 中配置的 Godot 4.5.1 Mono 导出：

~~~powershell
& 'D:\Godot_v4.5.1-stable_mono_win64\Godot_v4.5.1-stable_mono_win64\Godot_v4.5.1-stable_mono_win64.exe' --headless --path 'D:\sts2-shuyu' --export-pack 'Windows Desktop' 'D:\sts2-shuyu\dist\Shuyu\Shuyu.pck'
~~~

如果你的 Godot 路径不同，以 local.props 的 GodotExe 为准。

导出时不要把 PCK 放进 lib/0.107.1 或 lib/0.111.0；两个版本共用顶层 Shuyu.pck。

## 13. Rider 构建解决方案与自动部署

现在可以使用 Rider 的“构建解决方案”完成当前游戏版本的日常测试部署。

执行前必须确认：

1. 游戏进程已经关闭。
2. release_info.json 是准备测试的游戏版本。
3. local.props 的 Sts2CompatVersion 与游戏一致。
4. local.props 的 Sts2Dir、Sts2DataDir 和 GodotExe 路径正确。

解决方案已经设置为内容工程先构建、加载器工程后构建。默认 Debug 构建顺序是：

~~~text
构建当前版本 Shuyu 内容 DLL
        ↓
复制到 mods/Shuyu/lib/当前版本/Shuyu.dll
        ↓
复制 Shuyu.json 到 Mod 顶层
        ↓
Godot 重新导出 mods/Shuyu/Shuyu.pck
        ↓
构建 Shuyu.Loader
        ↓
复制并重命名为 mods/Shuyu/Shuyu.dll
~~~

当前版本为 107 时，内容 DLL 写入：

~~~text
mods/Shuyu/lib/0.107.1/Shuyu.dll
~~~

当前版本为 111 时，内容 DLL 写入：

~~~text
mods/Shuyu/lib/0.111.0/Shuyu.dll
~~~

加载器工程使用同一个 CopyModOnBuild 开关，并把 Shuyu.Loader.dll 复制为顶层 Shuyu.dll。程序集内部名称仍然是 Shuyu.Loader。

因此日常测试可以直接：

1. 在 Rider 修改代码或资源。
2. 点击“构建解决方案”。
3. 等待内容编译和 PCK 导出完成。
4. 启动游戏测试。

如果只构建 Shuyu 内容项目而不是整个解决方案，内容 DLL、manifest 和 PCK 仍会更新，但加载器工程不会重新构建。普通业务代码修改通常不影响加载器；若修改过 Shuyu.Loader，必须构建整个解决方案。

一次构建只更新当前 Sts2CompatVersion 对应的内容目录，不会重新构建另一个游戏版本。例如当前是 111 时，lib/0.107.1 不会被更新。这适合本机测试，但不等于完成双版本正式发布。

如果游戏 Mod 目录被手动删除，而 Rider 判断某个项目已经是最新状态，可以使用“重新构建解决方案”，确保所有构建后复制目标重新执行。

Godot 导出过程中可能显示粒子插件 UID 或 GDExtension 平台警告。只要最终显示构建成功且 PCK 已生成，这些已知警告不等于部署失败；出现 error 时仍需单独处理。

### 13.1 只编译、不部署

命令行检查或 CI 不希望修改游戏目录时，显式传入：

~~~text
-p:CopyModOnBuild=false
-p:RunPckExport=false
~~~

例如：

~~~powershell
dotnet build .\Shuyu.sln -c Debug --no-restore -p:CopyModOnBuild=false -p:RunPckExport=false
~~~

CopyModOnBuild=false 会同时关闭内容 DLL 和加载器的复制；RunPckExport=false 会关闭 PCK 导出。

### 13.2 日常 Debug 测试与正式 Release 的区别

Rider 默认“构建解决方案”通常生成 Debug DLL，适合本机快速测试。

正式发布仍应按第 7～12 节分别构建两套 Release 内容 DLL、Release 加载器并组装 dist。不要直接把游戏 mods/Shuyu 中的一次 Debug 自动部署目录当成最终工坊发布包。

## 14. 从 dist 安装到游戏进行测试

不要递归清空游戏 Mod 目录，因为其中可能有工坊上传配置、预览图或个人备份。

只复制发布所需文件：

~~~powershell
$target = 'D:\Steam\steamapps\common\Slay the Spire 2\mods\Shuyu'
New-Item -ItemType Directory -Force -Path "$target\lib\0.107.1", "$target\lib\0.111.0" | Out-Null
Copy-Item .\dist\Shuyu\Shuyu.dll "$target\Shuyu.dll" -Force
Copy-Item .\dist\Shuyu\Shuyu.json "$target\Shuyu.json" -Force
Copy-Item .\dist\Shuyu\Shuyu.pck "$target\Shuyu.pck" -Force
Copy-Item .\dist\Shuyu\lib\0.107.1\Shuyu.dll "$target\lib\0.107.1\Shuyu.dll" -Force
Copy-Item .\dist\Shuyu\lib\0.111.0\Shuyu.dll "$target\lib\0.111.0\Shuyu.dll" -Force
~~~

## 15. 启动后检查哪些日志

111 正常情况下应看到：

~~~text
[Shuyu.Loader] Host version 0.111.0; picked variant 0.111.0.
[Shuyu.Loader] Registered via AssociateAssemblyWithMod (v0.110.0+).
[Shuyu.Loader] Reflection bridge patch installed.
[Shuyu] [Patcher - core-patches] ... 4 applied ... 0 failed
[Shuyu] Shuyu initialized.
[AutoRegister] Processed assembly 'Shuyu':
    163 operation(s), 163 succeeded, 0 failed.
~~~

107.1 正常情况下应看到：

~~~text
[Shuyu.Loader] Host version 0.107.1; picked variant 0.107.1.
[Shuyu.Loader] Registered via direct Mod.assembly set (v0.107 fallback).
[Shuyu.Loader] Reflection bridge patch installed.
[Shuyu] [Patcher - core-patches] ... 4 applied ... 0 failed
[Shuyu] Shuyu initialized.
[AutoRegister] Processed assembly 'Shuyu':
    163 operation(s), 163 succeeded, 0 failed.
~~~

重点搜索：

~~~text
MissingMethodException
TypeLoadException
No compatible variant
Failed to load
Failed to initialize Shuyu
Patch application complete
Processed assembly 'Shuyu'
~~~

如果游戏版本高于 111，加载器会尝试 111 DLL，并写出：

~~~text
attempting the 0.111.0 variant as a forward-compatibility fallback
~~~

出现这条警告不代表一定有问题，但至少需要完成一次启动、选角和战斗验证。

## 16. 修改功能后的最低测试范围

每次修改至少完成：

1. 107 Release 构建零错误。
2. 111 Release 构建零错误。
3. 两个版本各启动一次，确认加载器选择正确 DLL。
4. 4 个梳雨补丁均为 0 failed。
5. 内容注册为 163 succeeded、0 failed；如果新增内容，成功数量应相应增加。
6. 新增或修改的卡牌、能力、遗物或 VFX 在游戏中实际使用一次。

涉及以下内容时需要额外测试：

| 修改内容 | 额外测试 |
|---|---|
| 卡牌 Owner、队友、复制 | 双人或多人联机。 |
| 动态模型、存档字段 | 新开局、保存、退出、读档。 |
| 卡牌结算牌堆 | 抽牌堆、弃牌堆、消耗堆和随机位置。 |
| CardPlay 或伤害来源 | 攻击触发能力、遗物和伤害修正。 |
| 角色动画 | attack、cast、受击、死亡和时间缩放。 |
| VFX/shader | 正常图形模式；headless 不能确认最终画面。 |
| Harmony transpiler | 两个版本都检查目标命中和实际行为。 |

## 17. 发布前检查

### 17.1 文件结构

~~~powershell
Get-ChildItem .\dist\Shuyu -Recurse -File | Select-Object FullName, Length
~~~

最终应只有发布需要的文件：

~~~text
Shuyu.dll
Shuyu.json
Shuyu.pck
lib/0.107.1/Shuyu.dll
lib/0.111.0/Shuyu.dll
~~~

不要包含：

- sts2.dll
- 0Harmony.dll
- STS2-RitsuLib.dll
- pdb
- deps.json
- runtimeconfig.json
- obj、bin 或 .godot
- 个人 local.props

### 17.2 哈希

~~~powershell
Get-FileHash -Algorithm SHA256 .\dist\Shuyu\Shuyu.dll, .\dist\Shuyu\Shuyu.json, .\dist\Shuyu\Shuyu.pck, .\dist\Shuyu\lib\0.107.1\Shuyu.dll, .\dist\Shuyu\lib\0.111.0\Shuyu.dll
~~~

保存发布版本的哈希，方便确认工坊目录、本地测试目录和压缩包是否来自同一套产物。

### 17.3 Git 检查

~~~powershell
git diff --check
git status --short
~~~

dist 不出现在 git status 中是正常的，因为它被故意忽略。

## 18. 推荐的完整发布顺序

1. 修改共享源码与资源。
2. 切换游戏到 107.1。
3. restore 107。
4. 构建 107 Release。
5. 把 107 DLL 复制到 dist/lib/0.107.1。
6. 在 107.1 安装 dist 并验证加载器与功能。
7. 切换游戏到 111。
8. restore 111。
9. 构建 111 Release。
10. 把 111 DLL 复制到 dist/lib/0.111.0。
11. 构建 Shuyu.Loader Release 并复制为 dist/Shuyu/Shuyu.dll。
12. 如果资源有变化，重新导出顶层 Shuyu.pck。
13. 复制最新 Shuyu.json。
14. 在 111 安装 dist 并验证加载器与功能。
15. 检查文件结构、程序集身份、日志与哈希。
16. 再从 dist 制作压缩包或上传工坊。

## 19. 常见错误

### 错误一：顶层 Shuyu.dll 体积突然变大

顶层加载器目前只有约 24 KB。若顶层变成数百 KB，通常是内容 DLL 覆盖了加载器。

正确做法：重新构建 Shuyu.Loader，并把 Shuyu.Loader.dll 复制到顶层后重命名为 Shuyu.dll。

### 错误二：切换版本后大量方法不存在

通常是：

- 当前游戏 DLL 与 Sts2CompatVersion 不匹配。
- 切换版本后没有重新 restore，仍在使用另一版本的 RitsuLib 包。

### 错误三：游戏只加载 Shuyu.Loader，看不到卡牌

检查日志中是否出现：

~~~text
Registered via AssociateAssemblyWithMod
~~~

或：

~~~text
Registered via direct Mod.assembly set
~~~

同时确认 lib 版本目录和 DLL 文件名完全正确。

### 错误四：代码构建成功，但图片或场景不更新

说明只更新了 DLL，没有重新导出和安装 Shuyu.pck。

### 错误五：111 正常，107 编译失败

不要直接删除 111 的新参数或 API。先判断：

- 是否可以新增 Compat 包装方法。
- 是否属于 override，必须使用 STS2_107 条件签名。
- 是否需要为 107 实现等价行为，而不是简单丢弃功能。

### 错误六：高于 111 的游戏加载失败

加载器只会尽力使用 111 DLL，无法保证未来游戏不修改 API。查看具体的 MissingMethodException 或 TypeLoadException，再决定：

- 小差异：扩展现有 111 兼容层。
- 大差异：新增独立 lib/新版本 内容程序集和加载器映射。

## 20. 游戏升级以后怎么处理

游戏升级时先不要立即删除旧 DLL，也不要直接在 107 或 111 内容 DLL 上覆盖开发。先判断新版本能否继续使用现有 111 变体。

### 20.1 升级前保留基线

如果已知 Steam 即将升级，建议先保存：

- 当前完整 dist/Shuyu。
- 当前 Shuyu.json 和版本号。
- 107、111 内容 DLL 与加载器的 SHA-256。
- 当前 release_info.json。
- 当前能正常启动的日志。

不要把 sts2.dll 或其他商业游戏 DLL 提交到 Git。需要重新构建旧版本时，应重新切换对应 Steam 分支，或者使用你自己合法保存的本机依赖目录。

### 20.2 升级后先直接测试，不要先改代码

当前加载器的规则是：

~~~text
0.107.1        -> 107 内容 DLL
0.108～0.110   -> 拒绝
0.111.x        -> 111 内容 DLL
高于 0.111.0  -> 尝试 111 内容 DLL并输出警告
~~~

因此升级到 112 或更高版本后，先安装现有 dist 并启动游戏。

日志应先看到：

~~~text
[Shuyu.Loader] Host version ... is newer than 0.111.x;
attempting the 0.111.0 variant as a forward-compatibility fallback.
~~~

然后检查：

1. RitsuLib 是否完成初始化。
2. 梳雨加载器是否选择 0.111.0。
3. 4 个梳雨补丁是否全部 applied、0 failed。
4. 内容注册是否全部 succeeded、0 failed。
5. 是否出现 MissingMethodException 或 TypeLoadException。
6. 角色选择、开局、出牌、存档和读档是否正常。
7. 本次游戏更新涉及的功能是否正常。

### 20.3 情况 A：现有 111 DLL 完全正常

如果启动、补丁、内容和实际游戏测试都正常，可以暂时继续使用 111 DLL，不必仅因为游戏版本号变化就新增一个内容目录。

但要注意：

- 这属于向前兼容验证，不代表二进制天然兼容。
- 在发布说明中写明已经实测的新游戏版本。
- 保存该版本的 release_info.json 和测试日志。
- 游戏后续再次升级时仍要重新验证。

如果只是在 0.111.0、0.111.1、0.111.2 之间更新，加载器不会输出“高于 111”警告，但仍建议进行一次快速启动和战斗测试。

### 20.4 情况 B：RitsuLib 先加载失败

如果梳雨加载器还没执行，RitsuLib 就已经失败，先检查 RitsuLib 是否支持新游戏版本。

此时不要先修改梳雨兼容层，因为梳雨依赖尚未成功加载。

处理顺序：

1. 查看 RitsuLib 日志选择了哪个内部版本。
2. 检查是否已有支持新游戏版本的 RitsuLib 发行版或兼容包。
3. 验证新的 RitsuLib 是否仍支持梳雨保留的 0.107.1 和 0.111。
4. 只有确认双旧版本策略后，才修改 Shuyu.csproj 的 RitsuLibRuntimeVersion 和 Shuyu.json 依赖版本。
5. 修改依赖版本后，107、111 和新游戏版本必须全部重新构建或验证。

当前固定的 RitsuLib 0.5.13 是为 0.107.1 与 0.111 双版本确认的基线。不要因为看到新版就直接升级依赖。

### 20.5 情况 C：RitsuLib 正常，但梳雨 111 DLL 失败

常见错误包括：

~~~text
MissingMethodException
TypeLoadException
Method not found
Harmony target not found
override 签名不匹配
~~~

先查阅该游戏版本的迁移说明并反编译新 sts2.dll，对比具体 API，不要根据错误信息盲目删除参数。

如果差异很小，可以扩展现有兼容层，让 111 和新版本仍共用一个现代分支。

如果差异较大，或者修改 111 分支会破坏已验证的 111 行为，应新增独立内容变体。

### 20.6 新增一个内容版本的步骤

下面以新增 0.112.0 为例。

第一步：扩展 Shuyu.csproj。

新增目录映射和编译常量：

~~~xml
<ContentVariantFolder
    Condition="'$(Sts2CompatVersion)' == '112'">
  0.112.0
</ContentVariantFolder>

<DefineConstants
    Condition="'$(Sts2CompatVersion)' == '112'">
  $(DefineConstants);STS2_112
</DefineConstants>
~~~

修改 Sts2CompatVersion 校验，使 112 成为允许值。

为 112 选择正确的 RitsuLib 编译包。如果 111 和 112 使用同一个主线包，可以把现有条件改为同时接受 111 和 112；如果 RitsuLib 提供专用兼容包，则为 112 单独引用该包。

不要同时添加两个条件可能一起成立的同名 PackageReference。

第二步：尝试编译新版本。

~~~powershell
dotnet restore .\Shuyu.csproj -p:Sts2CompatVersion=112 -p:CopyModOnBuild=false -p:RunPckExport=false
dotnet build .\Shuyu.csproj -c Release --no-restore -p:Sts2CompatVersion=112 -p:CopyModOnBuild=false -p:RunPckExport=false
~~~

第三步：处理 API 差异。

当前大部分版本代码采用：

~~~csharp
#if STS2_107
// 107 API
#else
// 111 及较新 API
#endif
~~~

如果 112 与 111 API 相同，无需改这些文件，STS2_112 会自动进入 else 分支。

如果 112 又修改了方法签名，应改成：

~~~csharp
#if STS2_107
// 107 API
#elif STS2_111
// 111 API
#else
// 112 API
#endif
~~~

也可以把普通调用差异继续集中到 ShuyuCode/Compat，避免业务类出现大量版本判断。

第四步：添加发布目录。

~~~text
dist/Shuyu/lib/0.112.0/Shuyu.dll
~~~

第五步：修改加载器。

在 Shuyu.Loader/Bootstrap.cs 的 KnownVersions 中加入 0.112.0，并让宿主版本优先选择新的已验证变体。

例如：

~~~csharp
if (host.Major == 0 && host.Minor == 107 && host.Build == 1)
{
    requiredTarget = "0.107.1";
}
else if (host >= new Version(0, 112, 0))
{
    requiredTarget = "0.112.0";
}
else if (host >= new Version(0, 111, 0))
{
    requiredTarget = "0.111.0";
}
~~~

这样：

~~~text
0.111.x -> 111 DLL
0.112.x -> 112 DLL
高于 112 -> 尝试最新的 112 DLL
~~~

如果不希望未来版本自动回退，应改成精确的 Major、Minor 判断。

第六步：更新文档和发布说明。

记录：

- 新游戏版本。
- 新内容 DLL 的目录和 SHA-256。
- 使用的 RitsuLib 包与版本。
- 新增了哪些条件编译。
- 哪些功能完成了实测。

### 20.7 新增版本后必须回归旧版本

新增 112 不能只测试 112。

至少重新验证：

1. 0.107.1 仍选择 lib/0.107.1。
2. 0.111 仍选择 lib/0.111.0。
3. 0.112 选择 lib/0.112.0。
4. 107 与 111 的原有条件编译没有被新分支改变。
5. 三个版本使用的 RitsuLib 均能正常加载。
6. manifest 的 min_game_version 仍为 0.107.1。
7. 共用 Shuyu.pck 在三个版本中都能加载。

不要为了支持新版本而直接替换 lib/0.111.0 中的 DLL。旧目录必须继续保存真正面向 111 编译和验证的内容程序集。

### 20.8 是否需要提高 Shuyu.json 的最低游戏版本

通常不需要。

只要仍然支持 0.107.1：

~~~json
"min_game_version": "0.107.1"
~~~

就应保持不变。

新增 112 是增加一个较新版本变体，不代表放弃 107。

只有明确决定不再支持 107.1 时，才提高 min_game_version，并同时删除或停止发布对应目录。

### 20.9 游戏更新处理速查表

| 现象 | 处理 |
|---|---|
| 新版本直接启动，全部测试正常 | 继续使用 111 DLL，记录已验证版本。 |
| 只有加载器的向前兼容警告 | 正常现象，完成快速回归即可。 |
| RitsuLib 无法加载 | 先解决或升级 RitsuLib，再检查梳雨。 |
| 个别普通 API 改签名 | 扩展 Compat 层。 |
| virtual/override 改签名 | 新增版本条件签名。 |
| 大量 API 或补丁目标变化 | 新建独立内容变体目录。 |
| 新增了独立内容变体 | 修改 csproj、加载器、dist 和文档，并回归旧版本。 |
| 只改资源格式 | 重新导出 PCK，并在所有支持版本验证。 |

## 21. Rider 出现大量红波浪线怎么办

### 21.1 典型现象

游戏切换到 107.1 后，Rider 可能显示：

~~~text
FromCard 只能有一个实参
找不到某个 111 方法
override 不匹配
CardPlay 参数不正确
~~~

这通常不是源码真的损坏，而是 Rider 的设计时构建仍选择 111 配置，同时 Shuyu.csproj 又引用了当前安装的 107 sts2.dll。

结果是：

~~~text
编译常量和 RitsuLib 包：111
游戏 sts2.dll：107
~~~

两套 API 混在一起后，IDE 就会产生大量红线。

### 21.2 设置 Rider 当前使用的版本

打开项目根目录的 local.props。

当前游戏是 107.1 时：

~~~xml
<Sts2CompatVersion>107</Sts2CompatVersion>
~~~

当前游戏是 111 时：

~~~xml
<Sts2CompatVersion>111</Sts2CompatVersion>
~~~

local.props 已被 Git 忽略，只影响本机 IDE 和不显式传版本参数的本机构建，不会进入发布包。

### 21.3 重新恢复对应 NuGet 包

107：

~~~powershell
dotnet restore .\Shuyu.csproj -p:CopyModOnBuild=false -p:RunPckExport=false
~~~

111：

~~~powershell
dotnet restore .\Shuyu.csproj -p:CopyModOnBuild=false -p:RunPckExport=false
~~~

两条命令相同，因为 Sts2CompatVersion 已从 local.props 读取。

107 应选择：

~~~text
STS2.RitsuLib.Compat.0.107.1 0.5.13
~~~

111 应选择：

~~~text
STS2.RitsuLib 0.5.13
~~~

### 21.4 让 Rider 重新读取项目

修改 local.props 并 restore 后：

1. 回到 Rider。
2. 使用 Reload All Projects，或关闭后重新打开解决方案。
3. 等待 NuGet 恢复和项目索引完成。
4. 再查看红波浪线。

如果仍然保留旧诊断：

1. 确认 release_info.json 与 local.props 的版本一致。
2. 再执行一次 restore。
3. 在 Rider 中执行 Invalidate Caches / Restart。
4. 重开项目并等待索引完成。

不要为了消除红线而修改 AttackCommandCompat 或删除 cardPlay 参数；配置正确后这些调用会自动按 107 兼容扩展方法解析。

### 21.5 用命令确认 IDE 配置是否正确

不显式传 Sts2CompatVersion，直接构建：

~~~powershell
dotnet build .\Shuyu.csproj --no-restore -p:CopyModOnBuild=false -p:RunPckExport=false
~~~

当前 local.props 为 107 时，成功输出路径应包含：

~~~text
.godot\mono\temp\bin\107\Debug\Shuyu.dll
~~~

当前 local.props 为 111 时，路径应包含：

~~~text
.godot\mono\temp\bin\111\Debug\Shuyu.dll
~~~

如果路径版本不对，说明 local.props 没有被读取，或命令行/Rider 另外传入了更高优先级的 Sts2CompatVersion。

### 21.6 哪些警告可以与本问题区分

配置正确后，当前项目仍可能显示少量既有警告，例如：

- CS8600、CS8602、CS8604 等可空引用警告。
- RITSU013 资源索引警告。

这些警告应单独 review，但与 FromCard 参数数量错误不是同一个问题。判断版本配置是否修复的关键是：

- 不再出现大量 API 签名红线。
- 命令行构建为 0 个错误。
- 输出路径是当前选择的 107 或 111。
