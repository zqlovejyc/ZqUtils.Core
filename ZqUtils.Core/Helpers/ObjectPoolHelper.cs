#region License
/***
 * Copyright © 2018-2022, 张强 (943620963@qq.com).
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * without warranties or conditions of any kind, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
#endregion

using System;
using System.Threading;
using System.Collections.Generic;
using Microsoft.Extensions.ObjectPool;
/****************************
 * [Author] 张强
 * [Date] 2019-02-18
 * [Describe] 对象池帮助类
 * **************************/
namespace ZqUtils.Core.Helpers
{
    /// <summary>
    /// 对象池帮助类
    /// </summary>
    public class ObjectPoolHelper<T>
    {
        #region 私有字段
        private int isTaked = 0;
        private int currentResource = 0;
        private int tryNewObject = 0;
        private readonly Queue<T> queue = new Queue<T>();
        private readonly Func<T> func = null;
        private readonly int minSize = 1;
        private readonly int maxSize = 50;
        #endregion

        #region 私有方法
        /// <summary>
        /// Enter
        /// </summary>
        private void Enter()
        {
            //把1赋值给isTaked
            while (Interlocked.Exchange(ref isTaked, 1) != 0) { }
        }

        /// <summary>
        /// Leave
        /// </summary>
        private void Leave()
        {
            //把0赋值给isTaked
            Interlocked.Exchange(ref isTaked, 0);
        }
        #endregion

        #region 构造函数
        /// <summary>
        /// 构造一个对象池
        /// </summary>
        /// <param name="func">用来初始化对象的函数</param>
        /// <param name="minSize">对象池下限</param>
        /// <param name="maxSize">对象池上限</param>
        public ObjectPoolHelper(Func<T> func, int minSize = 100, int maxSize = 100)
        {
            if (minSize > 0)
                this.minSize = minSize;
            if (maxSize > 0)
                this.maxSize = maxSize;
            for (var i = 0; i < this.minSize; i++)
            {
                this.queue.Enqueue(func());
            }
            this.currentResource = this.minSize;
            this.tryNewObject = this.minSize;
            this.func = func;
        }
        #endregion

        #region 对象池获取对象
        /// <summary>
        /// 从对象池中取一个对象出来, 执行完成以后会自动将对象放回池中
        /// </summary>
        public T GetObject()
        {
            var t = default(T);
            try
            {
                if (this.tryNewObject < this.maxSize)
                {
                    Interlocked.Increment(ref this.tryNewObject);
                    t = func();
                }
                else
                {
                    this.Enter();
                    t = this.queue.Dequeue();
                    this.Leave();
                    Interlocked.Decrement(ref this.currentResource);
                }
                return t;
            }
            finally
            {
                this.Enter();
                this.queue.Enqueue(t);
                this.Leave();
                Interlocked.Increment(ref currentResource);
            }
        }
        #endregion
    }

    /// <summary>
    /// 微软对象池工具类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MicrosoftObjectPool<T> where T : class, new()
    {
        /// <summary>
        /// 默认对象池
        /// </summary>
        private readonly DefaultObjectPool<T> _pool;

        /// <summary>
        /// 是否归返
        /// </summary>
        private readonly bool _return;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="maximumRetained">对象池大小</param>
        /// <param name="creater">对象初始化委托</param>
        /// <param name="return">是否归返对象池</param>
        public MicrosoftObjectPool(int maximumRetained = 100, Func<T> creater = null, bool @return = true)
        {
            var policy = new BasePooledObjectPolicy<T> { Creater = creater, IsReturn = @return };
            this._pool = new DefaultObjectPool<T>(policy, maximumRetained);
            this._return = @return;
        }

        /// <summary>
        /// 获取对象
        /// </summary>
        /// <returns></returns>
        public T Get()
        {
            return this._pool.Get();
        }

        /// <summary>
        /// 归返对象
        /// </summary>
        /// <param name="entity"></param>
        public void Return(T entity)
        {
            if (this._return)
            {
                this._pool.Return(entity);
            }
        }
    }

    /// <summary>
    /// 基础对象池策略
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BasePooledObjectPolicy<T> : IPooledObjectPolicy<T> where T : class, new()
    {
        /// <summary>
        /// 对象创建委托
        /// </summary>
        public Func<T> Creater { get; set; }

        /// <summary>
        /// 是否归返对象池
        /// </summary>
        public bool IsReturn { get; set; } = true;

        /// <summary>
        /// 创建对象
        /// </summary>
        /// <returns></returns>
        public T Create()
        {
            if (Creater != null)
            {
                return this.Creater();
            }
            return new T();
        }

        /// <summary>
        /// 归返对象池
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool Return(T obj)
        {
            return this.IsReturn;
        }
    }
}
