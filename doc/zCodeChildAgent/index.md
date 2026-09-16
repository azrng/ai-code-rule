
该文档存储zcode工具的子智能体索引

reviewer： 验收型子代理（跑快模型）：在文件改动交付给用户前做独立 review——从派发指令的验收标准逐条核对开始，再审 diff 找越界改动、回归、明显 bug，能复跑的验证就复跑。只读不改，输出 pass/fail verdict。写代码用 coder，蹲守用 watcher，调研用 Explore。

image-preview-recognizer： 图片预览识别型子代理（跑快模型）：专门读取图片、截图、设计稿和报错图片，提取文字与视觉事实，判断布局、状态、异常和视觉差异，输出带证据与置信度的识别结果。只读不改；需要修改用 coder，交付验收用 reviewer。
