using Microsoft.EntityFrameworkCore;

namespace GDP.Persistence.Models;

public class WorkSpace
{
    /// <summary>
    /// 工作空间id,数据库Schema
    /// </summary>
    [Comment("工作空间id,数据库Schema")]
    public string Id { get; set; }

    /// <summary>
    /// 工作空间名
    /// </summary>
    [Comment("工作空间名")]
    public string Name { get; set; }

    public string? Description { get; set; }

    public ICollection<LayerTreeNode> LayerTree { get; set; }
}