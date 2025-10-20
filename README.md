# Simple ECS Ability Timeline

基于 Unity DOTS 的技能系统，配有可视化时间轴编辑器。

## 简介

这是简单的ECS技能系统，提供可视化的时间轴编辑器。系统采用 Phase（阶段）+ Track（轨道）+ Key/Clip（事件点/片段）的结构，支持多种效果类型。

## 核心特性

###  编辑器功能
- **可视化时间轴编辑器** - 基于 UIElements 的直观编辑界面
- **多轨道支持** - VFX、SFX、动画、伤害判定等多种轨道类型
- **Key/Clip 系统** - 支持事件点触发和持续性效果
- **预设管理** - 可重用的 Key 配置预设
- **撤销/重做** - 完整的 Unity Undo 系统集成(TODO 暂未完善)

###  轨道类型
- **Hitbox 轨** - 伤害判定框（支持球形/锥形/胶囊体）（TODO：后续添加持续性判定）
- **VFX 轨** - 视觉特效生成
- **SFX 轨** - 音效播放
- **Animation 轨** - 动画片段控制
- **Camera 轨** - 相机震动等效果（TODO：占位后续添加）
- **Script 轨** - 脚本回调
- **Custom 轨** - 自定义扩展

## 快速开始

### 1. 创建技能配置
```
Assets → Create → SECS/Configs → Ability Config
```

### 2. 打开编辑器
```
Window → SECS → Ability Timeline Editor
```
或双击 `AbilityConfigSO` 资产。

### 3. 编辑技能
1. 点击 **"+ Phase"** 创建技能阶段
2. 点击 **"+ Track"** 在阶段中添加轨道
3. 选择轨道类型（VFX/SFX/Animation 等）
4. 在时间轴上添加 **Key**（事件点）或 **Clip**（持续片段）
5. 在检查器中配置参数

### 4. 运行时集成
在 SubScene 中添加 `AbilityDBAuthoring` 组件：
## 5.后续计划
1.完善编辑器易用性
2.添加完整的快捷键支持
3.bug修复
## 技术要求

- **Unity 版本**: 6000.0.5.7F1+
- **Entities 版本**: 1.3.0+


