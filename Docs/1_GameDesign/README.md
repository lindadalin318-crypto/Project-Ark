# Game Design — Project Ark

## 职责

`1_GameDesign/` 只放**策划可直接阅读、直接维护**的设计正文。

这里不放：

- 运行时技术真相源
- workflow / authority / validator 规则
- 结构化 `.csv` / `.json` 数据
- 实现日志
- 诊断报告

## 当前主入口

- [`Ark_MasterDesign.md`](./Ark_MasterDesign.md): 唯一总设计入口
- [`World_Bible.md`](./World_Bible.md): 世界设定专题
- [`Sheba_Planet_Bible.md`](./Sheba_Planet_Bible.md): 示巴星设定专题
- [`Sheba_FeatureRequirements.md`](./Sheba_FeatureRequirements.md): 示巴星现役需求主稿
- [`Sheba_RoomGrammar.md`](./Sheba_RoomGrammar.md): 示巴星房间语法
- [`StarChart_UI_Design.md`](./StarChart_UI_Design.md): 星图 UI 设计

## 维护规则

- 现役设计主稿只保留一份
- 编号版本稿确定为主稿后，应去编号并拿稳定文件名
- 结构化数据统一迁入 [`../4_GameData/`](../4_GameData/README.md)
