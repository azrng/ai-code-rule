### 设计系统
- 使用 NG-ZORRO 主题体系（主题色、圆角、字号对齐 `design-system.yaml` token），所有视觉规范在 `design-system.yaml` 中定义
- 优先使用 NG-ZORRO 组件及其内置样式 class，禁止 inline style，禁止硬编码颜色 / 间距 / 圆角；覆盖组件库样式走主题 token 或全局样式类，禁止新增 `::ng-deep` 与 `!important`

<!-- include: shared/application/tech-choice.md -->
