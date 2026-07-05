# Unity 编译优化总结

## 问题
Unity 启动/编译时卡在 **"Compiling Scripts: ScriptCompilation: Running Backend"**，原因是 ~580 个 .cs 文件全部塞在同一个 `Assembly-CSharp` 中编译。

---

## 已执行的优化

### 1. 清理 Photon 编译符号

**位置**: `ProjectSettings/ProjectSettings.asset`  
Photon 插件代码已删，但 **16 个平台** 的 Scripting Define Symbols 中仍残留：
```
PHOTON_UNITY_NETWORKING;PUN_2_0_OR_NEWER;PUN_2_OR_NEWER;PUN_2_19_OR_NEWER
```
已全部移除。保留的符号：`DOTWEEN;MIRROR;...`

### 2. 删除残留文件

- 删除 `Library/ScriptAssemblies/` 编译缓存
- 删除 `Assets/Photon.meta`

### 3. 移出不需要的脚本

`Assets/2025生日用素材/` → `联盟/2025生日用素材/`（gitignore 已添加 `/联盟/`），减少 21 个旧的 .cs 脚本。

### 4. 创建 Assembly Definition（程序集分离）

因 Mirror 和 Battle 脚本与 Assembly-CSharp 耦合太深（引用 `PlayerBasicInfoMgr`、`NetWorkMgr`、`GameLoadPanel` 等），只创建了 **4 个纯叶子程序集**：

#### 依赖关系
```
Aoyi.FixMath    Aoyi.Messages    Aoyi.BaseClasses    Aoyi.ErrorManager
  (leaf)           (leaf)            (leaf)                (leaf)
     │                  │
     ▼                  ▼
Assembly-CSharp ────────┘
  (Mirror, Battle, NetWorkScripts, CharactersChosePages, LobbyScripts, ...)
```

#### 创建的 asmdef 文件

| asmdef 文件 | 路径 | 命名空间 | 依赖 | 脚本数 |
|---|---|---|---|---|
| `Aoyi.FixMath.asmdef` | `Battle/FixedMathBase/` | `FixMath` | 无 | ~2 |
| `Aoyi.Messages.asmdef` | `NetWorkScripts/ProtoMsg/` | `MsgFramework` | 无 | ~6 |
| `Aoyi.BaseClasses.asmdef` | `NetWorkScripts/BasicSc/` | `BaseClasses` | 无 | ~1 |
| `Aoyi.ErrorManager.asmdef` | `ErrorManager/` | `ErrorManagement` | 无 | ~2 |

#### 效果
- **~11 个脚本** 从 Assembly-CSharp 中分离为独立程序集
- 修改 FixMath 时只重新编译 2 个文件
- 修改 Messages 时只重新编译 6 个文件
- Assembly-CSharp 自动引用上述所有 asmdef，外部脚本使用 `using FixMath;` / `using MsgFramework;` 等无需改动

---

## 下一步建议

### 高优先级
1. **清理旧版脚本**：`脚本文件/`、`存储资源夹/` 共约 120 个旧脚本，如果不再使用可以移出 `Assets/`
2. **为 NetWorkScripts/Manager/ 创建 asmdef**：`PlayerBasicInfoMgr`、`UIManager`、`SceneMgr` 等 Manager 类被多处引用，独立出来后可以减少增量编译体积

### 中优先级
3. **合并废弃的 meta 文件夹**：`Battle/` 下有 `PlayerLogicScipts.meta`、`PlayerRenderScript.meta` 等空文件夹

---

## 如果下次启动仍然卡住

1. 按住 **Alt** 双击项目 → 选择 **Skip Compilation**（安全模式）
2. 进入编辑器后手动 `Ctrl+R` 刷新
3. 如果仍然不行，删除整个 `Library/` 文件夹重新导入
