﻿#region License
/***
 * Copyright © 2018-2025, 张强 (943620963@qq.com).
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
using ZqUtils.Core.Extensions;
using ZqUtils.Core.WeChat.Interfaces;

namespace ZqUtils.Core.WeChat.Models
{
    /// <summary>
    /// 文本消息
    /// </summary>
    public class TextXmlMsg : IXmlMsg
    {
        /// <summary>
        /// 接收方帐号(收到的OpenID)
        /// </summary>
        public string ToUserName { get; set; }
        
        /// <summary>
        /// 开发者微信号
        /// </summary>
        public string FromUserName { get; set; }
        
        /// <summary>
        /// 消息创建时间(整型)
        /// </summary>
        public int CreateTime { get; set; } = DateTime.Now.ToUnixTime();
        
        /// <summary>
        /// 消息类型
        /// </summary>
        public string MsgType { get; set; } = "text";
        
        /// <summary>
        /// 回复的消息内容（换行：在content中能够换行，微信客户端就支持换行显示）
        /// </summary>
        public string Content { get; set; }
        
        /// <summary>
        /// 转换成xml
        /// </summary>
        /// <returns>string</returns>
        public string ToXml()
        {
            return $@"<xml>
                        <ToUserName><![CDATA[{ToUserName}]]></ToUserName>
                        <FromUserName><![CDATA[{FromUserName}]]></FromUserName>
                        <CreateTime>{CreateTime}</CreateTime>
                        <MsgType><![CDATA[{MsgType}]]></MsgType>
                        <Content><![CDATA[{Content}]]></Content>
                    </xml>";
        }
    }
}
