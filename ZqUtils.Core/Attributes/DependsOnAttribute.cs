using System;

namespace ZqUtils.Core.Attributes
{
    /// <summary>
    /// 定义一个接口多个实现类时指定注入名称
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class DependsOnAttribute : Attribute
    {
        /// <summary>
        /// 继承的接口类型
        /// </summary>
        public Type DependedType { get; }

        /// <summary>
        /// 注入名称，多继承时唯一标识
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dependedType">继承的接口类型</param>
        /// <param name="name">注入名称，多继承时唯一标识</param>
        public DependsOnAttribute(Type dependedType, string name)
        {
            DependedType = dependedType;
            Name = name;
        }
    }
}