#region License
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

using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
/****************************
* [Author] 张强
* [Date] 2015-10-26
* [Describe] Email工具类
* **************************/
namespace ZqUtils.Core.Helpers
{
    /// <summary>
    /// Email工具类
    /// </summary>
    public class MailHelper
    {
        #region 发送邮件
        /// <summary> 
        /// 发送邮件
        /// </summary> 
        /// <param name="from">发送人邮件地址</param> 
        /// <param name="fromName">发送人显示名称</param> 
        /// <param name="to">发送给谁（邮件地址），多个邮件地址以逗号分割</param> 
        /// <param name="subject">标题</param> 
        /// <param name="body">内容</param> 
        /// <param name="userName">邮件登录名</param> 
        /// <param name="password">邮件密码</param> 
        /// <param name="attachment">附件</param> 
        /// <param name="host">邮件服务器（smtp.126.com）</param> 
        /// <param name="port">端口号，默认25</param> 
        /// <param name="enableSsl">是否启用SSL加密，默认false</param>
        /// <param name="useDefaultCredentials">是否使用凭据，默认true</param>
        /// <param name="timeout">超时时长，默认10000ms</param>
        /// <param name="bodyEncoding">邮件内容编码方式，默认Encoding.Default</param>
        /// <param name="deliveryMethod">推送方式，默认Network</param>
        /// <returns>bool</returns> 
        public static void SendMail(
            string from,
            string fromName,
            string to,
            string subject,
            string body,
            string userName,
            string password,
            string attachment,
            string host,
            int port = 25,
            bool enableSsl = false,
            bool useDefaultCredentials = true,
            int timeout = 10000,
            Encoding bodyEncoding = null,
            SmtpDeliveryMethod deliveryMethod = SmtpDeliveryMethod.Network)
        {
            //邮件发送类 
            using (var message = new MailMessage())
            {
                //是谁发送的邮件 
                message.From = new MailAddress(from, fromName);
                //发送给谁 
                message.To.Add(to);
                //标题 
                message.Subject = subject;
                //内容编码 
                message.BodyEncoding = bodyEncoding ?? Encoding.Default;
                //发送优先级 
                message.Priority = MailPriority.High;
                //邮件内容 
                message.Body = body;
                //是否HTML形式发送 
                message.IsBodyHtml = true;
                //附件 
                if (attachment?.Length > 0)
                {
                    message.Attachments.Add(new Attachment(attachment));
                }
                //邮件服务器和端口 
                using (var client = new SmtpClient(host, port))
                {
                    //凭据
                    client.UseDefaultCredentials = useDefaultCredentials;
                    //ssl加密
                    client.EnableSsl = enableSsl;
                    //指定发送方式 
                    client.DeliveryMethod = deliveryMethod;
                    //指定登录名和密码 
                    client.Credentials = new NetworkCredential(userName, password);
                    //超时时间 
                    client.Timeout = timeout;
                    client.Send(message);
                }
            }
        }

        /// <summary> 
        /// 发送邮件
        /// </summary> 
        /// <param name="from">发送人邮件地址</param> 
        /// <param name="fromName">发送人显示名称</param> 
        /// <param name="to">发送给谁（邮件地址），多个邮件地址以逗号分割</param> 
        /// <param name="subject">标题</param> 
        /// <param name="body">内容</param> 
        /// <param name="userName">邮件登录名</param> 
        /// <param name="password">邮件密码</param> 
        /// <param name="attachment">附件</param> 
        /// <param name="host">邮件服务器（smtp.126.com）</param> 
        /// <param name="port">端口号，默认25</param> 
        /// <param name="enableSsl">是否启用SSL加密，默认false</param>
        /// <param name="useDefaultCredentials">是否使用凭据，默认true</param>
        /// <param name="timeout">超时时长，默认10000ms</param>
        /// <param name="bodyEncoding">邮件内容编码方式，默认Encoding.Default</param>
        /// <param name="deliveryMethod">推送方式，默认Network</param>
        /// <returns>bool</returns> 
        public static async Task SendMailAsync(
            string from,
            string fromName,
            string to,
            string subject,
            string body,
            string userName,
            string password,
            string attachment,
            string host,
            int port = 25,
            bool enableSsl = false,
            bool useDefaultCredentials = true,
            int timeout = 10000,
            Encoding bodyEncoding = null,
            SmtpDeliveryMethod deliveryMethod = SmtpDeliveryMethod.Network)
        {
            //邮件发送类 
            using (var message = new MailMessage())
            {
                //是谁发送的邮件 
                message.From = new MailAddress(from, fromName);
                //发送给谁 
                message.To.Add(to);
                //标题 
                message.Subject = subject;
                //内容编码 
                message.BodyEncoding = bodyEncoding ?? Encoding.Default;
                //发送优先级 
                message.Priority = MailPriority.High;
                //邮件内容 
                message.Body = body;
                //是否HTML形式发送 
                message.IsBodyHtml = true;
                //附件 
                if (attachment?.Length > 0)
                {
                    message.Attachments.Add(new Attachment(attachment));
                }
                //邮件服务器和端口 
                using (var client = new SmtpClient(host, port))
                {
                    //凭据
                    client.UseDefaultCredentials = useDefaultCredentials;
                    //ssl加密
                    client.EnableSsl = enableSsl;
                    //指定发送方式 
                    client.DeliveryMethod = deliveryMethod;
                    //指定登录名和密码 
                    client.Credentials = new NetworkCredential(userName, password);
                    //超时时间 
                    client.Timeout = timeout;
                    await client.SendMailAsync(message);
                }
            }
        }
        #endregion
    }
}
