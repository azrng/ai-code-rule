### Model 层规则
- GORM 实体使用 struct tag 定义字段映射
- 表名使用 snake_case
- 软删除使用 `gorm.DeletedAt`
- 公共字段（ID、创建时间、更新时间）提取为 BaseModel
