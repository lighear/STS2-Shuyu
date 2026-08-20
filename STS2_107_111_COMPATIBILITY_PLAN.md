# 梳雨 Mod 同时支持 STS2 0.107.1 与 0.111 的实施方案

## 1. 结论

建议采用与参考 Mod 相同的总体架构：发布包顶层只放一个轻量加载器，真正的梳雨程序集分别按游戏版本放入 `lib` 子目录；加载器在运行时识别游戏版本，只加载对应的程序集。资源继续共用一个 `Shuyu.pck`。

推荐的最终发布结构：

```text
Shuyu/
├─ Shuyu.dll                 # 轻量加载器；程序集名应为 Shuyu.Loader
├─ Shuyu.json                # 单一 manifest
├─ Shuyu.pck                 # 两个版本共用的资源包
└─ lib/
   ├─ 0.107.1/
   │  └─ Shuyu.dll           # 用 STS2 0.107.1 + RitsuLib 0.5.13 的 107.1 兼容包编译
   └─ 0.111.0/
      └─ Shuyu.dll           # 用 STS2 0.111.x + RitsuLib 0.5.13 主包编译
```

不建议维护两份完整源码目录。应保留一套共享源码，只在确实发生签名差异的少数位置使用条件编译或兼容适配器，然后由同一个工程按两个构建配置产出两个变体 DLL。这样后续修改卡牌、能力和数值时不会出现两个版本长期漂移。

本方案把“107”明确限定为 `0.107.1`，正式验证目标是 `0.107.1` 和 `0.111.x`。精确的 `0.107.0` 与中间的 108～110 不支持；高于 111 的版本按用户决定尝试加载 111 变体，并明确记录为向前兼容回退，而不是已验证支持。

## 2. 本次分析范围和依据

方案最初通过只读分析和临时副本编译探测形成；随后已把梳雨的 manifest 与运行时依赖下限更新为 RitsuLib 0.5.13，并完成第一套 0.107.1 内容程序集。依据包括：

- 当前分支：`v107`。
- 当前本机游戏已从 `v0.107.1` 切换到 `v0.111.0`；两个版本的构建和验证记录均保留在本文中。
- 当前梳雨源码：可分别用 `Sts2CompatVersion=107` 和 `Sts2CompatVersion=111` 构建两套内容程序集；`Shuyu.json` 的最低游戏版本为 `0.107.1`，RitsuLib 依赖固定为 `0.5.13`。
- 参考 Mod：`D:\Steam\steamapps\workshop\content\2868840\3747679239`。
- 迁移资料：[正式版至测试版迁移说明](https://tutorials.sts2modding.com/docs/07-migration-99-100/)。
- 对参考加载器、参考的 107/109/110/111 变体 DLL、本机 107 的 `sts2.dll`，以及 RitsuLib 0.4.46、0.5.12、0.5.13 进行了反编译核对。
- 在仓库临时副本内用 `STS2 0.107.1 + RitsuLib 0.4.46` 对当前源码做了分阶段编译探测；临时副本已清理。
- 工坊 RitsuLib 0.5.13：`D:\Steam\steamapps\workshop\content\2868840\3747602295`。它自身已采用加载器加 107.1/109/110/111 变体 DLL 的结构。
- 实机日志确认 RitsuLib 0.5.13 在 107.1 选择 `0.107.1` 变体、在 111 选择 `0.111.0` 变体；两次均完成框架初始化，各补丁组均为 0 失败。

## 3. 参考 Mod 的实际机制

### 3.1 文件和程序集身份

参考包顶层的文件名是 `PengoTarot.dll`，但它的程序集名实际是 `PengoTarot.Loader`；`lib/<版本>/PengoTarot.dll` 的程序集名才是 `PengoTarot`。这一步很重要：如果内外两个 DLL 的程序集身份相同，.NET 默认加载上下文可能直接复用已经加载的外层程序集，导致真正的版本 DLL 无法载入。

梳雨应照此处理：

- 顶层文件名保持游戏要求的 `Shuyu.dll`。
- 顶层 DLL 的程序集名设为 `Shuyu.Loader`。
- 两个版本目录里的程序集名都保持 `Shuyu`。

### 3.2 加载流程

反编译得到的参考加载器按以下顺序工作：

1. 从加载器自身路径定位 `lib`。
2. 从 `ReleaseInfoManager.Instance.ReleaseInfo.Version` 读取宿主版本并去掉开头的 `v`。
3. 选择版本目录并通过当前 `AssemblyLoadContext.LoadFromAssemblyPath` 加载真实 DLL。
4. 把真实程序集重新关联到当前 Mod：
   - 0.110+ 反射调用 `ModManager.AssociateAssemblyWithMod(modId, assembly)`；
   - 0.107 回退为找到对应的 `Mod`，直接设置其公开字段 `assembly`。
5. 给 `ReflectionHelper.ModTypes` 安装 Harmony 后置补丁，把变体程序集中的类型并入游戏的 Mod 类型扫描结果。
6. 在变体程序集内寻找 `[ModInitializer]` 并反射调用真实初始化方法。
7. 补做 Godot C# 脚本注册。
8. 重建 `ModelIdSerializationCache`，用于动态模型和联机序列化。

其中第 4～7 步不是可省略的装饰。只做 `LoadFromAssemblyPath` 会让 DLL 虽然进入进程，但游戏的模型发现、RitsuLib 自动注册、Godot 脚本查找或 Mod 归属仍然指向外层加载器。

### 3.3 参考实现中不应原样照搬的部分

参考目录里虽然有 107、109、110、111 四套 DLL，但当前外层加载器的 `KnownVersions` 实际只列出 107 和 111，109/110 目录不会被选中。

参考的选择策略是“选取不高于宿主的最新变体”；宿主版本未知时用最新变体；连宿主低于所有已知版本时也回退到最新变体。这会让 108～110 静默选择 107 DLL。梳雨改为显式规则：

- 只有精确的 `0.107.1` 映射到 107 变体；
- `0.111.*` 映射到 111 变体；
- 高于 `0.111.0` 的版本尝试加载 111 变体，并输出向前兼容警告；
- 107.0、108～110 或无法识别版本时停止初始化并输出清楚的错误日志。

参考加载器自带的 `ModelIdSerializationCache` 重建补丁已按参考保留，但它只补充缓存中缺失的类别或条目；如果 RitsuLib 已正确完成缓存，补丁会直接返回，不重排也不重算。111 实机日志中没有出现梳雨加载器的重建消息，说明本次由 RitsuLib/游戏正常完成，参考补丁没有介入。

## 4. RitsuLib 版本策略

这是梳雨与参考 Mod 最大的不同：参考 Mod 没有外部依赖，而梳雨大量依赖 RitsuLib。

RitsuLib 0.5.12 只有面向 111 的单一 DLL，其 manifest 明确写着 `min_game_version: 0.111.0`，二进制也直接引用了 107.1 不存在或签名不同的接口，因此不能作为梳雨双版本共用依赖。

RitsuLib 0.5.13 已把运行时发行包改造成加载器加多个版本 DLL。两个梳雨变体应使用相同的 RitsuLib 版本号，但在编译时选择不同的 NuGet 包：

| 梳雨变体 | 编译期 NuGet 包 | 包版本 | 对应游戏 API |
|---|---|---:|---:|
| 107.1 | `STS2.RitsuLib.Compat.0.107.1` | 0.5.13 | 0.107.1 |
| 111 | `STS2.RitsuLib` | 0.5.13 | 0.111.x |

玩家运行时只需安装一份工坊 RitsuLib 0.5.13；RitsuLib 自己的顶层加载器会选择与游戏匹配的内部 DLL。梳雨发布包不得私自携带 RitsuLib DLL。

顶层 `Shuyu.json` 建议设置：

```json
{
  "min_game_version": "0.107.1",
  "dependencies": [
    {
      "id": "STS2-RitsuLib",
      "version": "0.5.13"
    }
  ]
}
```

上面的 `min_game_version` 已写入当前 `Shuyu.json`。0.107.1 变体已完成编译和直装加载验证，因此无需再临时维持 `0.111.0`；最终仍由加载器严格拒绝 108～110 等未支持版本。

游戏的依赖检查把这里的版本当作最低版本而非精确版本，因此梳雨应把依赖下限固定为 0.5.13。107.1 和 111 用户安装同一份 RitsuLib 0.5.13 工坊发行包即可。

当前 `SyncManifestDependencies` 根据主线 `STS2.RitsuLib` 包路径同步 manifest。改造成条件包引用后，107.1 包的 MSBuild 属性名会不同，因此不应继续从具体包路径推导运行时依赖版本。建议新增固定属性（例如 `RitsuLibRuntimeVersion=0.5.13`），让 manifest 同步和发布校验都读取该属性；两个变体必须使用相同版本号 0.5.13。

## 5. 当前源码的 107 兼容影响面

分阶段编译探测共得到 46 条编译诊断（首轮 6 条、修正首轮签名后的后续 40 条），对应 45 处实际兼容改造点。另有 1 处能编译但会在 107 下静默失效的运行时补丁。

| 类别 | 位置/数量 | 107 与 111 的处理方向 |
|---|---:|---|
| `ModifyDamageMultiplicative` | 3 处：`BingWuPower`、`FragilePower`、`NingShuangJuXiangPower` | 107 无 `CardPlay?` 参数，111 有；用条件编译保留两个 override 签名，内部共享同一逻辑函数。 |
| `CharacterModel.GenerateAnimator` | 1 处：`ShuyuCharacter` | 107 为 `(MegaSprite)`，111 为 `(MegaSprite, Creature)`；分别 override，公共的动画事件连接逻辑抽成私有方法。 |
| 卡牌结算位置 | 1 处：`YuZhiLiChang` | 111 使用 `CardLocation GetResultLocationForCardPlay()`；107 使用 `PileType GetResultPileTypeForCardPlay()`，随机位置需同时通过 107 的 `ModifyCardPlayResultPileTypeAndPosition` 保持原行为。 |
| `AttackCommand.FromCard` | 33 处 | 107 只接收 `CardModel`，111 还接收 `CardPlay?`；新增一个 `FromCardCompat(command, card, cardPlay)` 适配器，统一替换调用点，避免散布 33 组 `#if`。 |
| `CreatureCmd.LoseBlock` | 1 处：`FragilePower` | 107 为 `(Creature, decimal)`，新版本带选择上下文和移除者；由兼容适配器分发。 |
| `CreatureCmd.Damage` 的 `CardPlay`/来源参数 | 3 处：`EYun`、`NingYu`、`ShuyuMechanismCmd` | 为 107 调用无 `CardPlay` 的重载，同时明确保留 dealer 与 cardSource；111 继续传递 `CardPlay`。 |
| `CreateCloneForPlayer` | 2 处：`JiuChanZhiShu`、`JiuChanXuYuanShu` | 此 API 在 108 才加入。不能只删参数，需要为 107 实现“复制卡牌状态并归属到另一玩家”的等价路径，重点验证多人所有权、`CloneOf`、升级和动态变量。 |
| `MegaTrackEntry` 生命周期 | 1 处：`ShuyuCharacter` | 107 类型不实现 `IDisposable`，111 当前代码使用 `using`；按版本分别管理。 |
| 弃牌发光异步补丁 | 1 处：`FrozenGlowWhenDiscardPatch` | 当前硬编码 `&lt;FromHandForDiscard&gt;d__29`，107 实际是 `d__26`。应从 `AsyncStateMachineAttribute`/Harmony 异步方法工具动态取得状态机类型，不再硬编码编译器生成编号。 |

迁移文档中以下变化在当前代码里不需要专门改动，但仍应纳入回归测试：

- `CardCmd.Exhaust` 在 111 改为 `Task<CardPileAddResult?>`；当前调用都只 `await` 且不使用结果。
- `CardPileCmd.Draw` 在中间版本去掉 `async` 实现细节，但公开返回类型仍可按 `Task<IEnumerable<CardModel>>` 使用，现有手牌上限补丁的目标签名在 107 存在。
- 当前未发现需要迁移的 RNG seed/counter、`PeerVersionInfo`、`MegaInput` 或自定义 `PlayerChoiceContext` override。
- `CardDescriptionPatch`、`HookHandFullPatch`、`ModifyDamageFinalPatch` 的目标方法在 107 中存在；仍需在运行时确认 Harmony 实际命中。

## 6. 推荐的工程改造

### 6.1 项目划分

新增独立加载器工程，例如：

```text
Shuyu.Loader/
├─ Shuyu.Loader.csproj
├─ Bootstrap.cs
└─ ReflectionHelperModTypesPatch.cs
```

加载器应：

- 以最老支持版本 0.107.1 的 `sts2.dll` 编译，确保只使用两个版本共有的静态 API；
- 不引用 RitsuLib；
- 只引用 `sts2` 和 Harmony；
- 输出程序集名 `Shuyu.Loader`；
- 由发布目标复制/重命名为顶层 `Shuyu.dll`。

现有 `Shuyu.csproj` 作为真实内容程序集工程，增加 `Sts2CompatVersion` 配置，例如 `107`/`111`：

- 条件选择对应 `Sts2DataDir`；
- 107.1 条件引用 `STS2.RitsuLib.Compat.0.107.1` 0.5.13，111 条件引用 `STS2.RitsuLib` 0.5.13；
- 定义 `STS2_107` 或 `STS2_111`；
- 给两个配置设置独立的 `obj`/`bin` 路径，避免 Godot 源生成文件和 Publicizer 输出互相污染；
- 两个变体的程序集名都固定为 `Shuyu`。

### 6.2 兼容层

建议新增 `ShuyuCode/Compat`，把高频签名差异集中在少数文件：

```text
ShuyuCode/Compat/
├─ AttackCommandCompat.cs
├─ CreatureCmdCompat.cs
├─ CardCloneCompat.cs
└─ AsyncMethodCompat.cs
```

原则是：

- 兼容层是在编译时选择 API，不是在运行时用一个内容 DLL兼容两个游戏版本；同一套源码仍会产出两套 `Shuyu.dll`；
- 业务类保留统一调用形式；
- 只有无法用普通适配器表达的 virtual override 才在业务文件内写 `#if STS2_107`；
- 适配器必须保持 111 的 `CardPlay` 上下文，不可为了编译方便让 111 也退化成 107 行为；
- `AttackCommandCompat` 集中处理 `FromCard` 参数差异，`CreatureCmdCompat` 集中处理 `Damage` 和 `LoseBlock` 参数差异；
- `CardCloneCompat` 为 107.1 实现 `CreateCloneForPlayer` 的等价行为，并单独进行多人测试；
- `AsyncMethodCompat` 通过 `AsyncStateMachineAttribute` 或 Harmony 工具查找状态机类型，不再硬编码 `d__26`/`d__29`。

### 6.3 加载器注册顺序

梳雨加载器建议采用以下顺序：

1. 检测宿主版本并按上面的显式规则映射；高于 111 时记录向前兼容警告。
2. 加载唯一匹配的变体 DLL。
3. 在调用真实 `Entry.Initialize` 之前，把变体程序集关联到当前 Mod。
4. 安装 `ReflectionHelper.ModTypes` 桥接补丁，且防止重复安装。
5. 调用变体中的 `[ModInitializer]`。
6. 检查 Godot 脚本是否已由 `RitsuLibFramework.EnsureGodotScriptsRegistered` 注册；仅在缺失时补注册。
7. 写出宿主版本、选择的变体、变体程序集名/路径、关联方式和初始化结果。

真实 `Entry.Initialize` 里现有的以下两句应保留：

- `RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger)`；
- `ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly)`。

加载器负责让游戏认识变体程序集，真实入口负责让对应版本的 RitsuLib 扫描和注册内容，两层职责不同。

### 6.4 发布构建

新增一个统一发布目标或 PowerShell 脚本，固定执行顺序：

1. 构建 107 变体到暂存目录 `lib/0.107.1/Shuyu.dll`。
2. 构建 111 变体到暂存目录 `lib/0.111.0/Shuyu.dll`。
3. 构建加载器并放到暂存目录顶层 `Shuyu.dll`。
4. 只导出一次 `Shuyu.pck`。
5. 复制固定的 `Shuyu.json`。
6. 校验顶层 DLL 与两个变体 DLL 的程序集名、SHA-256 和路径。
7. 校验包中没有误带 `sts2.dll`、RitsuLib DLL、`obj`、`bin` 或开发期配置。
8. 生成最终 zip；本地安装目标与最终 zip 必须来自同一个暂存目录。

现有 `CopyMod` 会把当前构建的真实 `Shuyu.dll` 直接覆盖到 Mod 顶层，必须在多版本模式下替换或禁用。`ExportPCK` 也应从“每次 Build 后执行”改为发布流程只执行一次。

构建机需要同时能访问两个版本的游戏依赖。不要把商业游戏 DLL 提交到仓库；可在不跟踪的 `local.props` 中配置 `Sts2DataDir107` 和 `Sts2DataDir111`，或保存两套本机 Steam 安装副本。每个变体必须对正确版本的 `sts2.dll` 编译，不能只改文件夹名。

## 7. 实施顺序

当前进度：阶段 A、B、C 和 D 已完成；111 已通过加载器实机装载验证。阶段 E 尚需在切回 107.1 后验证加载器选择 107 变体，并继续图形特效、多人跨玩家复制和联机回归。

### 阶段 A：建立可复现基线

- 保存当前能工作的 111 构建产物、游戏 `release_info.json`、RitsuLib 0.5.13 的 111 变体信息和日志。
- 在当前 107.1 环境保存同类信息。
- 记录两边 `sts2.dll` 的 SHA-256，防止后续拿错依赖。
- 保存已验证的 RitsuLib 0.5.13 双版本加载日志作为基线。

### 阶段 B：先完成 107 内容程序集

- 在当前 `v107` 分支引入构建常量和 `STS2.RitsuLib.Compat.0.107.1` 0.5.13 配置。
- 按第 5 节逐类解决编译错误。
- 优先处理三个有行为差异的点：跨玩家卡牌克隆、御之力场回到抽牌堆的随机位置、带 `CardPlay` 的伤害来源。
- 让 107 变体独立构建通过，并在没有加载器的临时直装模式下完成基础内容测试。

当前产物已保存到 `dist/Shuyu/lib/0.107.1/Shuyu.dll`。0.107.1 实机日志确认：RitsuLib 0.5.13 选择 107.1 兼容变体，梳雨 4 个 Harmony 补丁全部应用，初始化完成，自动注册 163 项且 0 失败。无图形界面的启动测试会对部分屏幕采样着色器输出 Godot 编译提示，因此 VFX 视觉效果仍需在正常图形模式下抽查。

### 阶段 C：恢复并锁定 111 变体

- 使用正确的 111 游戏 DLL 和 `STS2.RitsuLib` 0.5.13 构建 `STS2_111`。
- 确认兼容层没有降低 111 原有行为。
- 两个变体都通过后，再进入加载器阶段。

当前 111 内容程序集已使用本机 `v0.111.0` 与 `STS2.RitsuLib` 0.5.13 构建成功，0 个错误、13 个既有警告。最终 Release 产物已保存到 `dist/Shuyu/lib/0.111.0/Shuyu.dll`，SHA-256 为 `770205E8F53A75CA8C1196FFDBA803F02A8E1794EBF058FBD2FA1398A313CA93`。随后已通过顶层加载器完成 111 实机装载验证。

### 阶段 D：加入加载器与发布暂存

- 实现独立 `Shuyu.Loader`。
- 实现程序集归属、类型发现和 Godot 脚本注册桥接。
- 改造 `CopyMod`、manifest 同步和 PCK 导出逻辑。
- 生成与参考 Mod 同型的最终目录。

以上加载器、程序集桥接和 `dist` 结构已经完成；111 实机确认加载器选择 0.111.0 变体、4 个梳雨补丁全部成功、163 项内容全部注册成功。

### 阶段 E：双版本运行验证

- 在 107.1 启动，确认日志只选择 107 变体。
- 切换 111 启动，确认日志只选择 111 变体。
- 再分别做完整功能和联机验证。

## 8. 验证清单

### 8.1 构建和装载

- 两个变体均零编译错误、零关键分析器警告。
- 顶层加载器能在 107 和 111 加载，且不会提前触发新版本 API 的类型加载异常。
- 每次进程只加载一个 `Shuyu` 真实程序集，只执行一次 `Entry.Initialize`。
- `ModManager`、`ReflectionHelper.ModTypes` 和 RitsuLib 均能看到真实变体类型而非仅看到加载器。
- 所有 Godot `[ScriptPath]` 类型能从共用 PCK 实例化。
- 不支持的 108～110 版本得到明确错误，不静默选错 DLL。

### 8.2 内容和资源

- 梳雨角色可选择，初始牌组、遗物、药水、卡池数量正确。
- 所有卡牌、能力、遗物、药水、附魔、异常状态能从 `ModelDb` 取得。
- 中英文本地化和动态变量正常。
- 角色场景、能量 UI、卡图、能力图标和所有自定义 VFX 正常。
- 新开局、保存、退出、读档均正常；旧梳雨存档的模型 ID 不丢失。

### 8.3 重点行为回归

- 33 个攻击调用在 107 正常造成伤害，在 111 仍携带正确 `CardPlay` 上下文。
- `BingWuPower`、`FragilePower`、`NingShuangJuXiangPower` 的伤害倍率在两版一致。
- 易碎转化时清格挡、施加易伤和后续伤害顺序一致。
- `YuZhiLiChang` 首回合自动打出后回到抽牌堆随机位置，手动打出逻辑不变。
- `EYun`、`NingYu`、冰霜伤害的 dealer/cardSource 归属正确。
- `JiuChanZhiShu`、`JiuChanXuYuanShu` 的跨玩家复制在多人模式中拥有正确 Owner、牌堆、升级和动态变量。
- 封冻牌在弃牌选择界面仍正确发光；日志中不能出现找不到异步状态机的错误。
- 四个自定义 Patch 在两个版本都报告目标匹配；关键 Patch 失败时按现有策略阻止初始化。

### 8.4 依赖和联机

- 107.1 + RitsuLib 0.5.13，日志确认 RitsuLib 选择 `0.107.1` 变体。
- 111.x + RitsuLib 0.5.13，日志确认 RitsuLib 选择 `0.111.0` 变体。
- manifest 最低依赖 0.5.13 在两边均通过依赖检查。
- 同游戏版本、同梳雨版本、同 RitsuLib 版本的两端能够握手并进入战斗。
- 模型 ID 序列化哈希一致，卡牌/能力同步不出现未知 ID。
- 不测试也不承诺 107 客户端与 111 客户端互联。

## 9. 发布与工坊注意事项

- `min_game_version` 只能表达最低版本，不能表达“仅支持 107 和 111、不支持中间版本”；真正的限制必须由加载器完成。
- Steam 工坊的 supported game versions 如果支持多段范围，应分别声明 107 和 111，而不是声明一个连续的 107～111 范围，否则 108～110 用户会被工坊界面误导。
- zip、工坊目录和本地测试目录必须保持相同结构。
- 更新时应先完整替换旧目录，避免遗留已经不再支持的 `lib/0.109.0` 等文件。
- 发布说明应明确写明两个游戏版本都要求 RitsuLib 0.5.13；用户无需手动选择 RitsuLib 的内部版本 DLL。

## 10. 主要风险与决策点

1. **版本边界**：107.1 和 111 是已验证目标；107.0、108～110 明确拒绝；高于 111 的版本按用户决定尝试 111 变体并输出警告，因此属于尽力向前兼容，不等于保证兼容。
2. **跨玩家卡牌克隆**：这是当前最需要行为设计而不只是改签名的地方，必须用多人实测确定 107.1 的等价实现。
3. **两套游戏依赖的保存方式**：发布构建需要同时访问 107.1 和 111 的 SDK/DLL；应确定本机双目录或 CI 私有依赖方案。
4. **RitsuLib 编译包选择**：两个梳雨变体都使用 0.5.13，但 NuGet 包 ID 不同；发布校验必须防止把主线 111 包误用于 107.1 构建。
5. **未来版本策略**：112+ 自动尝试 111 变体可减少小更新后的重发需求，但游戏若出现破坏性 API 变化仍可能加载失败；应根据日志和实测决定是否新增内容变体。

支持边界和 RitsuLib 版本策略已经确定，可按本方案直接进入实现；当前本机环境适合先完成和验证 0.107.1 梳雨变体。

## 11. 本次实际修改内容（Review 用）

本节最初记录“107 内容程序集”相关改动；加载器的实际代码、参考对应关系与运行逻辑见第 12 节。

### 11.1 文件清单

| 文件 | 修改性质 | 简单说明 |
|---|---|---|
| .gitignore | 修改 | 忽略本地发布暂存目录 dist/。 |
| Shuyu.csproj | 修改 | 增加 107/111 条件构建、RitsuLib 条件包、独立输出路径和版本校验。 |
| Shuyu.json | 修改 | 最低游戏版本改为 0.107.1，RitsuLib 最低依赖改为 0.5.13。 |
| ShuyuCode/GlobalUsings.cs | 新增 | 让全部业务代码自动可见兼容扩展方法。 |
| ShuyuCode/Compat/AttackCommandCompat.cs | 新增 | 兼容 AttackCommand.FromCard 的参数差异。 |
| ShuyuCode/Compat/CreatureCmdCompat.cs | 新增 | 兼容 CreatureCmd.Damage 和 LoseBlock 的参数差异。 |
| ShuyuCode/Compat/CardCloneCompat.cs | 新增 | 在 107.1 补出跨玩家复制卡牌的等价方法。 |
| ShuyuCode/Compat/AsyncMethodCompat.cs | 新增 | 动态查找 async 状态机类型，去除编译器编号硬编码。 |
| ShuyuCode/Powers/BingWuPower.cs | 修改 | 分别覆盖 107/111 的伤害倍率虚方法签名。 |
| ShuyuCode/Powers/FragilePower.cs | 修改 | 同上，并通过兼容层调用清除格挡。 |
| ShuyuCode/Powers/NingShuangJuXiangPower.cs | 修改 | 分别覆盖 107/111 的伤害倍率虚方法签名。 |
| ShuyuCode/Characters/ShuyuCharacter.cs | 修改 | 兼容角色动画器和 MegaTrackEntry 生命周期差异。 |
| ShuyuCode/Cards/Rare/YuZhiLiChang.cs | 修改 | 在 107 保持自动打出后回到抽牌堆随机位置。 |
| ShuyuCode/Patches/FrozenGlowWhenDiscardPatch.cs | 修改 | 兼容两个版本不同的 async 状态机编号。 |
| ShuyuCode/Cards/EYun.cs | 修改 | 伤害调用改走兼容层。 |
| ShuyuCode/Cards/Rare/NingYu.cs | 修改 | 伤害调用改走兼容层。 |
| ShuyuCode/Commands/ShuyuMechanismCmd.cs | 修改 | 群体伤害调用改走兼容层。 |
| ShuyuCode/Cards/Rare/JiuChanZhiShu.cs | 修改 | 跨玩家克隆显式改为扩展方法调用。 |
| ShuyuCode/Cards/Rare/JiuChanXuYuanShu.cs | 修改 | 跨玩家克隆显式改为扩展方法调用。 |

dist/Shuyu 是被 Git 忽略的二进制暂存目录，不属于源码 diff。当前 107 DLL 位于 dist/Shuyu/lib/0.107.1/Shuyu.dll，SHA-256 为 C023F295D5A96CC0EFB5370299D1E96E827C0B915B45F040F178FEAE39FD7F5F。

### 11.2 构建与 manifest

.gitignore 新增：

~~~gitignore
dist/
~~~

Shuyu.csproj 的版本选择和独立输出：

~~~xml
<Sts2CompatVersion Condition="'$(Sts2CompatVersion)' == ''">111</Sts2CompatVersion>
<RitsuLibRuntimeVersion>0.5.13</RitsuLibRuntimeVersion>
<DefineConstants Condition="'$(Sts2CompatVersion)' == '107'">$(DefineConstants);STS2_107</DefineConstants>
<DefineConstants Condition="'$(Sts2CompatVersion)' == '111'">$(DefineConstants);STS2_111</DefineConstants>
<IntermediateOutputPath>$(GodotProjectDir).godot\mono\temp\obj\$(Sts2CompatVersion)\$(Configuration)\</IntermediateOutputPath>
<OutputPath>$(GodotProjectDir).godot\mono\temp\bin\$(Sts2CompatVersion)\$(Configuration)\</OutputPath>
~~~

这里默认仍为 111，避免改变原工程“不传参数时构建 111”的习惯。构建 107 时显式传入：

~~~powershell
dotnet build .\Shuyu.csproj -p:Sts2CompatVersion=107
~~~

两个版本选择不同的编译期包，但运行时依赖版本相同：

~~~xml
<PackageReference Include="STS2.RitsuLib.Compat.0.107.1"
                  Version="$(RitsuLibRuntimeVersion)"
                  Condition="'$(Sts2CompatVersion)' == '107'" />
<PackageReference Include="STS2.RitsuLib"
                  Version="$(RitsuLibRuntimeVersion)"
                  Condition="'$(Sts2CompatVersion)' == '111'" />
~~~

游戏 DLL 路径改为使用已经存在的可配置属性，避免继续硬编码 Steam 路径：

~~~xml
<Reference Include="sts2">
  <HintPath>$(Sts2DataDir)\sts2.dll</HintPath>
</Reference>
~~~

非法版本会直接终止构建：

~~~xml
<Error Condition="'$(Sts2CompatVersion)' != '107'
                  and '$(Sts2CompatVersion)' != '111'"
       Text="Sts2CompatVersion must be either '107' or '111', but was '$(Sts2CompatVersion)'." />
~~~

manifest 同步不再依赖某一个 NuGet 包生成的路径变量，而是读取固定运行时版本：

~~~xml
<Target Name="SyncManifestDependencies"
        AfterTargets="Build"
        BeforeTargets="CopyMod"
        Condition="'$(DesignTimeBuild)' != 'true'">
  <PropertyGroup>
    <RitsuLibVersion>$(RitsuLibRuntimeVersion)</RitsuLibVersion>
  </PropertyGroup>
</Target>
~~~

Shuyu.json 的实际改动：

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

### 11.3 新增兼容层完整内容

ShuyuCode/GlobalUsings.cs：

~~~csharp
global using Shuyu.Compat;
~~~

这样业务文件可以直接调用兼容扩展方法，不需要在三十多个卡牌文件里分别加入 using。

ShuyuCode/Compat/AttackCommandCompat.cs：

~~~csharp
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace Shuyu.Compat;

public static class AttackCommandCompat
{
#if STS2_107
    public static AttackCommand FromCard(
        this AttackCommand command,
        CardModel card,
        CardPlay? cardPlay)
    {
        return command.FromCard(card);
    }
#endif
}
~~~

107 的原生方法只有一个 card 参数；这个扩展方法接收业务代码原有的两个参数并丢弃 107 不支持的 CardPlay。111 不编译该扩展方法，因此原代码会继续绑定到 111 的原生实例方法，不会损失 111 的出牌上下文。

ShuyuCode/Compat/CreatureCmdCompat.cs：

~~~csharp
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Shuyu.Compat;

public static class CreatureCmdCompat
{
    public static Task<IEnumerable<DamageResult>> Damage(
        PlayerChoiceContext choiceContext,
        Creature target,
        decimal amount,
        ValueProp props,
        CardModel cardSource,
        CardPlay? cardPlay)
    {
#if STS2_107
        return CreatureCmd.Damage(choiceContext, target, amount, props, cardSource);
#else
        return CreatureCmd.Damage(choiceContext, target, amount, props, cardSource, cardPlay);
#endif
    }

    public static Task<IEnumerable<DamageResult>> Damage(
        PlayerChoiceContext choiceContext,
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
#if STS2_107
        return CreatureCmd.Damage(choiceContext, target, amount, props, dealer, cardSource);
#else
        return CreatureCmd.Damage(choiceContext, target, amount, props, dealer, cardSource, cardPlay);
#endif
    }

    public static Task<IEnumerable<DamageResult>> Damage(
        PlayerChoiceContext choiceContext,
        IEnumerable<Creature> targets,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
#if STS2_107
        return CreatureCmd.Damage(choiceContext, targets, amount, props, dealer, cardSource);
#else
        return CreatureCmd.Damage(choiceContext, targets, amount, props, dealer, cardSource, cardPlay);
#endif
    }

    public static Task LoseBlock(
        PlayerChoiceContext choiceContext,
        Creature creature,
        decimal amount,
        Creature? remover)
    {
#if STS2_107
        return CreatureCmd.LoseBlock(creature, amount);
#else
        return CreatureCmd.LoseBlock(choiceContext, creature, amount, remover);
#endif
    }
}
~~~

这里没有把 111 也降级到旧重载：111 仍会传入 CardPlay、选择上下文和移除者；只有 107 分支丢弃宿主 API 本身无法接收的参数。

ShuyuCode/Compat/CardCloneCompat.cs：

~~~csharp
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace Shuyu.Compat;

public static class CardCloneCompat
{
#if STS2_107
    public static CardModel CreateCloneForPlayer(this CardModel card, Player newOwner)
    {
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(newOwner);

        CardModel clone = card.CreateClone();
        if (ReferenceEquals(clone.Owner, newOwner))
        {
            return clone;
        }

        clone.Owner = null!;
        clone.Owner = newOwner;
        return clone;
    }
#endif
}
~~~

107.1 已有 CreateClone()，它会保留升级、动态变量和 CloneOf 等克隆状态，但没有 108 新增的 CreateCloneForPlayer()。107 的 Owner setter 不允许从一个非空 Owner 直接改到另一个非空 Owner，所以必须先清空，再设置为目标玩家。此路径已经完成编译和装载验证，但仍需要多人实战确认牌堆同步。

ShuyuCode/Compat/AsyncMethodCompat.cs：

~~~csharp
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Shuyu.Compat;

public static class AsyncMethodCompat
{
    public static Type? GetStateMachineType(MethodBase method)
    {
        Type? declaringType = method.DeclaringType;
        if (method.Name == nameof(IAsyncStateMachine.MoveNext)
            && declaringType != null
            && typeof(IAsyncStateMachine).IsAssignableFrom(declaringType))
        {
            return declaringType;
        }

        return method.GetCustomAttribute<AsyncStateMachineAttribute>()?.StateMachineType;
    }
}
~~~

RitsuLib/Harmony 有时把原始 async 方法传给 transpiler，有时传入状态机的 MoveNext。这个方法同时支持两种情况，因此不再依赖 d__26 或 d__29 这种随编译版本变化的名字。

### 11.4 必须直接写在业务类里的版本分支

三个 Power 的虚方法采用相同模式。以 BingWuPower 为例：

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
    // 原有倍率逻辑保持不变。
}
~~~

实际使用该模式的文件是 BingWuPower.cs、FragilePower.cs 和 NingShuangJuXiangPower.cs。原因是 virtual override 的方法签名必须与当前游戏 DLL 完全一致，无法像普通静态调用那样完全隐藏到适配器里。

ShuyuCharacter.GenerateAnimator：

~~~csharp
#if STS2_107
public override CreatureAnimator GenerateAnimator(MegaSprite controller)
{
    CreatureAnimator animator = base.GenerateAnimator(controller);
    ConnectAnimationEvents(controller);
    return animator;
}
#else
public override CreatureAnimator GenerateAnimator(MegaSprite controller, Creature creature)
{
    CreatureAnimator animator = base.GenerateAnimator(controller, creature);
    ConnectAnimationEvents(controller);
    return animator;
}
#endif
~~~

事件连接提取为公共逻辑，避免两个版本复制整段行为：

~~~csharp
private static void ConnectAnimationEvents(MegaSprite controller)
{
    controller.ConnectAnimationStarted(
        Callable.From<GodotObject, GodotObject, GodotObject>(OnSpineAnimationStarted));
}
~~~

MegaTrackEntry 在 107 不实现 IDisposable，111 则需要释放：

~~~csharp
#if STS2_107
MegaTrackEntry entry = new(trackEntry);
SpeedUpAttackAnimation(entry);
#else
using MegaTrackEntry entry = new(trackEntry);
SpeedUpAttackAnimation(entry);
#endif
~~~

YuZhiLiChang 在 107 的结算位置处理：

~~~csharp
#if STS2_107
protected override PileType GetResultPileTypeForCardPlay()
{
    return firstTurnAutoPlay
        ? PileType.Draw
        : base.GetResultPileTypeForCardPlay();
}

public override (PileType, CardPilePosition)
    ModifyCardPlayResultPileTypeAndPosition(
        CardModel card,
        bool isAutoPlay,
        ResourceInfo resources,
        PileType pileType,
        CardPilePosition position)
{
    (PileType resultPileType, CardPilePosition resultPosition) =
        base.ModifyCardPlayResultPileTypeAndPosition(
            card, isAutoPlay, resources, pileType, position);

    return firstTurnAutoPlay && ReferenceEquals(card, this)
        ? (PileType.Draw, CardPilePosition.Random)
        : (resultPileType, resultPosition);
}
#else
// 保留原有 111 GetResultLocationForCardPlay() 实现。
#endif
~~~

107 把“目标牌堆”和“牌堆内位置”拆成两个 API，因此需要同时指定 PileType.Draw 和 CardPilePosition.Random，才能保持 111 原有行为。

### 11.5 异步 Harmony 补丁改动

原代码硬编码 FromHandForDiscard 的 d__29 状态机名称；107.1 的编号实际是 d__26，所以改为让 Harmony 提供原方法，再动态解析状态机：

~~~csharp
public static IEnumerable<CodeInstruction> Transpiler(
    IEnumerable<CodeInstruction> instructions,
    MethodBase original)
{
    Type? asyncMethod = AsyncMethodCompat.GetStateMachineType(original);
    if (asyncMethod == null)
    {
        Entry.Logger.Error(
            "[Shuyu][FrozenGlowWhenDiscardPatch] Failed to get async method CardSelectCmd.FromHandForDiscard.");
        return instructions;
    }

    FieldInfo? prefsField = AccessTools.Field(asyncMethod, "prefs");
    MethodInfo? setShouldGlow = AccessTools.PropertySetter(
        typeof(CardSelectorPrefs),
        nameof(CardSelectorPrefs.ShouldGlowGold));
    MethodInfo? addCondition = AccessTools.Method(
        typeof(FrozenGlowWhenDiscardPatch),
        nameof(AddFrozenGlowCondition));

    if (prefsField == null || setShouldGlow == null || addCondition == null)
    {
        Entry.Logger.Error(
            "[Shuyu][FrozenGlowWhenDiscardPatch] Failed to resolve transpiler members.");
        return instructions;
    }

    // 后面的原有 CodeMatcher 修改逻辑保持不变。
}
~~~

同时增加了字段和方法查找失败检查，避免 null 被继续传给 IL 匹配器后出现不容易定位的异常。

### 11.6 普通业务调用点

以下三处伤害调用只把入口从游戏 API 换成兼容层，伤害数值、属性、dealer 和 cardSource 均保持原值：

~~~csharp
// EYun.cs
await CreatureCmdCompat.Damage(
    choiceContext, Owner.Creature, DynamicVars.HpLoss.BaseValue,
    ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move,
    this, null);

// NingYu.cs
await CreatureCmdCompat.Damage(
    choiceContext, cardPlay.Target, damage,
    ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move,
    Owner.Creature, this, cardPlay);

// ShuyuMechanismCmd.cs
await CreatureCmdCompat.Damage(
    choiceContext, targets, damage, ValueProp.Move,
    cardSource.Owner.Creature, cardSource, null);
~~~

FragilePower 清格挡：

~~~csharp
await CreatureCmdCompat.LoseBlock(
    choiceContext, Owner, Owner.Block, applier);
~~~

跨玩家克隆的两个调用点：

~~~csharp
// JiuChanZhiShu.cs 与 JiuChanXuYuanShu.cs
CardModel card = this.CreateCloneForPlayer(ally.Player!);
~~~

显式写 this. 是为了让 107 编译器选择扩展方法；在 111 中扩展方法不存在，因此同一句代码会绑定到游戏原生的 CreateCloneForPlayer。

原有 33 个攻击构建调用没有逐个修改。例如：

~~~csharp
AttackCommand
    .Damage(...)
    .FromCard(this, cardPlay);
~~~

在 107 中它会绑定到 AttackCommandCompat.FromCard，在 111 中绑定到游戏原生实例方法。这正是加入全局 using 和 107 专用扩展方法的目的。

### 11.7 当前 Review 边界

- 以上源码已使用 0.107.1 的 sts2.dll 和 STS2.RitsuLib.Compat.0.107.1 0.5.13 构建成功。
- 0.107.1 实机直装确认梳雨初始化、4 个补丁和 163 项内容注册全部成功。
- 以上共享源码也已使用 0.111.0 的 sts2.dll 和 STS2.RitsuLib 0.5.13 构建成功，并已通过顶层加载器完成 111 实机装载验证。
- 跨玩家克隆的 Owner 转移逻辑来自 107.1 CardModel 的实际实现，但仍需多人实战验证网络同步和最终入牌堆行为。
- Headless 模式无法验证 VFX 的最终画面；部分屏幕采样 shader 会输出非阻断提示，需要正常图形模式抽查。
- 顶层加载器与完整 dist 结构已经生成；107 通过加载器的实机验证仍待切回 0.107.1 后完成。

## 12. 加载器实际代码与逻辑（Review 用）

### 12.1 来源与文件

加载器以用户指定的参考目录为直接来源：

~~~text
D:\Steam\steamapps\workshop\content\2868840\3747679239
~~~

使用 ILSpy 反编译参考顶层 PengoTarot.dll 后得到三个核心文件：

| 参考文件 | 梳雨对应文件 | 处理 |
|---|---|---|
| PengoTarot.Loader/Bootstrap.cs | Shuyu.Loader/Bootstrap.cs | 保留整体加载顺序和反射实现，替换 Mod ID、DLL 名与版本选择规则。 |
| ReflectionHelperModTypesPatch.cs | Shuyu.Loader/ReflectionHelperModTypesPatch.cs | 逻辑等价保留，只替换 namespace。 |
| ModelIdSerializationCacheRebuildPatch.cs | Shuyu.Loader/ModelIdSerializationCacheRebuildPatch.cs | 逻辑等价保留，只调整命名、日志和可空标注。 |

新增工程文件：

~~~text
Shuyu.Loader/
├─ Shuyu.Loader.csproj
├─ Bootstrap.cs
├─ ReflectionHelperModTypesPatch.cs
└─ ModelIdSerializationCacheRebuildPatch.cs
~~~

Shuyu.Loader 不引用 RitsuLib，只引用游戏 sts2.dll 和游戏自带的 0Harmony.dll。这一点和参考加载器一致，目的是让 RitsuLib 自己先按宿主版本完成选择，再由梳雨加载器加载相应内容 DLL。

### 12.2 三个 DLL 的程序集身份

最终文件名虽然都叫 Shuyu.dll，但程序集内部名称不同：

| 发布位置 | 文件名 | 程序集名 | 职责 |
|---|---|---|---|
| dist/Shuyu/Shuyu.dll | Shuyu.dll | Shuyu.Loader | 游戏最先加载的轻量入口。 |
| dist/Shuyu/lib/0.107.1/Shuyu.dll | Shuyu.dll | Shuyu | 107.1 内容程序集。 |
| dist/Shuyu/lib/0.111.0/Shuyu.dll | Shuyu.dll | Shuyu | 111 内容程序集。 |

顶层加载器当前 SHA-256：

~~~text
36A4B0B77A27CE87C94DD258E9DDF31F26F55DF7705C695F86FF9979F43E1796
~~~

必须让顶层程序集名为 Shuyu.Loader。如果顶层也叫 Shuyu，默认 AssemblyLoadContext 可能认为真实内容程序集已经加载，从而返回顶层加载器自身。

### 12.3 完整运行顺序

Bootstrap.Initialize 的实际顺序与参考一致：

1. 从加载器程序集路径取得 Mod 顶层目录。
2. 检查顶层 lib 目录是否存在。
3. 从 ReleaseInfoManager 读取并解析宿主游戏版本。
4. 按明确规则选择 0.107.1 或 0.111.0 内容 DLL。
5. 使用加载器所在的 AssemblyLoadContext 加载真实 Shuyu 程序集。
6. 把真实程序集关联回当前 Mod。
7. 安装 ReflectionHelper.ModTypes 桥接和模型 ID 缓存补丁。
8. 在真实程序集中寻找 ModInitializerAttribute 并调用 Entry.Initialize。
9. 检查 Godot C# 脚本是否已经注册，仅在缺失时补注册。
10. 尝试补充 ModelIdSerializationCache 中仍然缺失的条目。

每一步都单独写日志；加载文件或执行真实入口失败时不会继续假装成功。

### 12.4 版本选择规则

打包的已知变体只有：

~~~csharp
private static readonly string[] KnownVersions =
[
    "0.111.0",
    "0.107.1"
];
~~~

实际选择代码：

~~~csharp
string? requiredTarget = null;
if (host.Major == 0 && host.Minor == 107 && host.Build == 1)
{
    requiredTarget = "0.107.1";
}
else if (host >= new Version(0, 111, 0))
{
    requiredTarget = "0.111.0";
    if (host.Major != 0 || host.Minor != 111)
    {
        Log.Warn(
            $"[Shuyu.Loader] Host version {host} is newer than 0.111.x; "
            + "attempting the 0.111.0 variant as a forward-compatibility fallback.");
    }
}
~~~

结果如下：

| 宿主版本 | 行为 |
|---|---|
| 0.107.1 | 加载 lib/0.107.1/Shuyu.dll。 |
| 0.107.0 | 拒绝加载。 |
| 0.108.x～0.110.x | 拒绝加载，避免错误使用 107 DLL。 |
| 0.111.x | 加载 lib/0.111.0/Shuyu.dll。 |
| 高于 0.111.0 | 尝试加载 111 DLL，并输出向前兼容警告。 |
| 无法取得或解析版本 | 拒绝猜测。 |

参考加载器原本会选择“不高于宿主的最新变体”，而且宿主未知或过旧时会回退到最新变体。梳雨没有照搬这一点，因为它会让 108～110 静默选择错误 DLL。高于 111 时尝试 111 DLL 是用户后续明确指定的策略。

### 12.5 程序集加载与 Mod 归属

真实 DLL 使用参考的当前 AssemblyLoadContext 加载：

~~~csharp
AssemblyLoadContext loadContext =
    AssemblyLoadContext.GetLoadContext(typeof(Bootstrap).Assembly)
    ?? AssemblyLoadContext.Default;

Assembly assembly = loadContext.LoadFromAssemblyPath(dllPath);
RegisterVariantAssembly(assembly);
~~~

关联方式保留参考的双路径：

- 0.110+：反射查找 ModManager.AssociateAssemblyWithMod(string, Assembly) 并调用。
- 0.107：如果上面的方法不存在，则寻找当前 Mod 对象并反射写入公开的 Mod.assembly 字段。

这样游戏后续通过程序集查询 Mod 时，看到的是内容程序集 Shuyu，而不是空壳 Shuyu.Loader。111 实机日志已经确认走新版 AssociateAssemblyWithMod 路径；107 回退路径仍待切换版本后实机确认。

### 12.6 ReflectionHelper 类型桥

参考补丁基本原样保留：

~~~csharp
[HarmonyPatch(typeof(ReflectionHelper), "ModTypes", MethodType.Getter)]
internal static class ReflectionHelperModTypesPatch
{
    private static void Postfix(ref Type[] __result)
    {
        Type[] variantModTypes = Bootstrap.GetVariantModTypes();
        if (variantModTypes.Length != 0)
        {
            __result = __result
                .Concat(variantModTypes)
                .Distinct()
                .ToArray();
        }
    }
}
~~~

原因是 ReflectionHelper 原本只知道游戏直接加载的顶层 Shuyu.Loader。后置补丁把真实内容程序集中的类型合并进去，使模型扫描、attribute 扫描和其他反射调用能看到卡牌、能力、角色和 Godot 脚本类型。

加载器用 Lock 保护已加载程序集列表，也保留了对 ReflectionTypeLoadException 的部分类型恢复；即使某一个类型无法载入，也尽量返回其余有效类型。

### 12.7 调用真实梳雨入口

真实入口调用方式与参考一致，不硬编码 Shuyu.Entry：

~~~csharp
foreach (Type type in realAssembly.GetTypes())
{
    ModInitializerAttribute? initializer =
        type.GetCustomAttribute<ModInitializerAttribute>();
    if (initializer == null)
    {
        continue;
    }

    MethodInfo? method = type.GetMethod(
        initializer.initializerMethod,
        BindingFlags.Static
        | BindingFlags.Public
        | BindingFlags.NonPublic);
    if (method == null)
    {
        continue;
    }

    method.Invoke(null, null);
    return;
}
~~~

因此内容程序集仍由它自己的 ModInitializerAttribute 决定入口。现有 Entry.Initialize 内的 RitsuLib Godot 注册、类型发现、4 个补丁和内容映射逻辑全部保持原样。

### 12.8 Godot 脚本补注册

真实 Entry 会先调用 RitsuLibFramework.EnsureGodotScriptsRegistered。加载器随后沿用参考逻辑检查 Godot 内部的路径到类型映射：

- 如果内容程序集的全部 ScriptPath 已存在，记录 already registered 并跳过。
- 如果仍有脚本未注册，反射调用 ScriptManagerBridge.LookupScriptsInAssembly。
- 如果当前 Godot 版本找不到该内部 API，只输出警告，不影响其他初始化。

111 实机日志显示：

~~~text
[Shuyu.Loader] Godot scripts already registered, skipping.
~~~

说明 RitsuLib 已先完成注册，加载器没有重复执行。

### 12.9 模型 ID 缓存补建

ModelIdSerializationCacheRebuildPatch 来自参考加载器。它在缓存初始化后检查 ModelDb 中是否还有缓存未包含的 category 或 entry：

- 没有缺项时直接返回。
- 有缺项时按 Ordinal 排序后追加。
- 追加后更新位宽和稳定 FNV-1a 哈希。
- 访问不到游戏私有字段时只警告并跳过。

111 实机没有出现 Rebuilt ModelIdSerializationCache 日志，最终由游戏正常报告缓存初始化完成。这表示 RitsuLib 已正确处理梳雨内容，参考补丁没有重复修改缓存。保留它只是为了覆盖变体程序集扫描时序异常的情况。

### 12.10 构建后复制行为

原 Shuyu.csproj 的 CopyMod 会把内容 DLL 直接复制到 Mod 顶层。加入加载器后这样会覆盖 Shuyu.Loader，所以已改为：

~~~xml
<ContentVariantFolder
    Condition="'$(Sts2CompatVersion)' == '107'">
  0.107.1
</ContentVariantFolder>
<ContentVariantFolder
    Condition="'$(Sts2CompatVersion)' == '111'">
  0.111.0
</ContentVariantFolder>

<ModVariantOutputDir>
  $(ModOutputDir)lib\$(ContentVariantFolder)\
</ModVariantOutputDir>
~~~

CopyMod 现在把内容 DLL 放入对应 lib 目录，只把 manifest 放在顶层。PCK 仍然导出到顶层。顶层 Shuyu.Loader 由加载器工程构建后放置。

主内容工程同时排除了 Shuyu.Loader/**，防止默认的递归 C# 文件包含规则把加载器源码误编进内容 DLL。

### 12.11 111 实机验证证据

当前 0.111.0 无界面启动日志确认：

~~~text
[Shuyu.Loader] ReleaseInfo.Version raw: 'v0.111.0'
[Shuyu.Loader] Parsed version: 0.111.0
[Shuyu.Loader] Host version 0.111.0; picked variant 0.111.0.
[Shuyu.Loader] Registered via AssociateAssemblyWithMod (v0.110.0+).
[Shuyu.Loader] Reflection bridge patch installed.
[Shuyu] [Patcher - core-patches] Patch application complete:
    4 applied, 0 ignored, 0 failed, 4 total
[Shuyu] Shuyu initialized.
[Shuyu.Loader] Godot scripts already registered, skipping.
[AutoRegister] Processed assembly 'Shuyu':
    163 operation(s), 163 succeeded, 0 failed.
~~~

未发现 Shuyu 相关 MissingMethodException、TypeLoadException、加载失败或初始化失败。

### 12.12 与参考代码的实际差异

为方便 review，参考代码之外的实质变化只有：

1. PengoTarot.Loader、PengoTarot 和 PengoTarot.dll 替换为 Shuyu.Loader、Shuyu 和 Shuyu.dll。
2. 参考的 0.107.0 目录改为梳雨实际支持的 0.107.1。
3. 版本选择从“最新的不高于宿主版本”改成第 12.4 节的显式映射。
4. 高于 111 时按用户决定尝试 111 变体并写警告。
5. 使用当前项目的可空标注、集合表达式和变量命名整理反编译输出；反射目标、调用顺序和异常边界保持参考逻辑。
6. 内容工程的 CopyMod 改为写入版本目录，避免覆盖顶层加载器。

没有重新设计程序集关联、类型桥、Godot 注册或模型 ID 补建算法。
