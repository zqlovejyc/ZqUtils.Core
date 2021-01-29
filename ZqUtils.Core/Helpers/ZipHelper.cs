#region License
/***
 * Copyright © 2018-2021, 张强 (943620963@qq.com).
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

using ICSharpCode.SharpZipLib.Checksum;
using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using ZqUtils.Core.Extensions;
/****************************
* [Author] 张强
* [Date] 2015-10-26
* [Describe] Zip工具类
* **************************/
namespace ZqUtils.Core.Helpers
{
    /// <summary>
    /// Zip解压缩帮助类
    /// </summary>
    public class ZipHelper
    {
        #region Zip压缩
        /// <summary>   
        /// 递归压缩文件夹的内部方法   
        /// </summary>   
        /// <param name="folderToZip">要压缩的文件夹路径</param>   
        /// <param name="zipStream">压缩输出流</param>   
        /// <param name="parentFolderName">此文件夹的上级文件夹</param>   
        /// <returns></returns>   
        private static bool ZipDirectory(string folderToZip, ZipOutputStream zipStream, string parentFolderName)
        {
            string[] folders, files;
            var crc = new Crc32();
            var ent = new ZipEntry(Path.Combine(parentFolderName, Path.GetFileName(folderToZip) + "/")) { IsUnicodeText = true };
            zipStream.PutNextEntry(ent);
            zipStream.Flush();
            files = Directory.GetFiles(folderToZip);
            foreach (var file in files)
            {
                using var fs = File.OpenRead(file);
                var buffer = new byte[fs.Length];
                fs.Read(buffer, 0, buffer.Length);
                ent = new ZipEntry(Path.Combine(parentFolderName, Path.GetFileName(folderToZip) + "/" + Path.GetFileName(file))) { IsUnicodeText = true };
                ent.DateTime = DateTime.Now;
                ent.Size = fs.Length;
                crc.Reset();
                crc.Update(buffer);
                ent.Crc = crc.Value;
                zipStream.PutNextEntry(ent);
                zipStream.Write(buffer, 0, buffer.Length);
            }
            folders = Directory.GetDirectories(folderToZip);
            foreach (var folder in folders)
            {
                if (!ZipDirectory(folder, zipStream, folderToZip))
                    return false;
            }
            return true;
        }

        /// <summary>   
        /// Zip压缩文件夹    
        /// </summary>   
        /// <param name="folderToZip">要压缩的文件夹路径</param>   
        /// <param name="zipedFile">压缩文件完整路径</param>   
        /// <param name="password">密码，默认：null</param>   
        /// <returns>是否压缩成功</returns>   
        public static bool ZipDirectory(string folderToZip, string zipedFile, string password = null)
        {
            if (!Directory.Exists(folderToZip))
                return false;

            using var zipStream = new ZipOutputStream(File.Create(zipedFile));
            zipStream.SetLevel(6);

            if (password.IsNotNullOrEmpty())
                zipStream.Password = password;

            return ZipDirectory(folderToZip, zipStream, "");
        }

        /// <summary>
        /// 压缩文件的内部方法
        /// </summary>
        /// <param name="fileToZip">要压缩的文件全名</param>
        /// <param name="zipStream">压缩输出流</param>
        /// <param name="password">密码，默认：null</param>
        /// <returns>压缩结果</returns>
        private static bool ZipFile(string fileToZip, ZipOutputStream zipStream, string password = null)
        {
            if (!File.Exists(fileToZip))
                return false;

            using var fs = File.OpenRead(fileToZip);
            var buffer = new byte[fs.Length];
            fs.Read(buffer, 0, buffer.Length);

            if (password.IsNotNullOrEmpty())
                zipStream.Password = password;

            var ent = new ZipEntry(Path.GetFileName(fileToZip)) { IsUnicodeText = true };
            zipStream.PutNextEntry(ent);
            zipStream.SetLevel(6);
            zipStream.Write(buffer, 0, buffer.Length);
            return true;
        }

        /// <summary>   
        /// Zip压缩文件   
        /// </summary>   
        /// <param name="fileToZip">要压缩的文件全名</param>   
        /// <param name="zipedFile">压缩后的文件名</param>   
        /// <param name="password">密码，默认：null</param>   
        /// <returns>压缩结果</returns>   
        public static bool ZipFile(string fileToZip, string zipedFile, string password = null)
        {
            if (!File.Exists(fileToZip))
                return false;

            using var fs = File.OpenRead(fileToZip);
            var buffer = new byte[fs.Length];
            fs.Read(buffer, 0, buffer.Length);
            using var f = File.Create(zipedFile);
            using var zipStream = new ZipOutputStream(f);

            if (password.IsNotNullOrEmpty())
                zipStream.Password = password;

            var ent = new ZipEntry(Path.GetFileName(fileToZip)) { IsUnicodeText = true };
            zipStream.PutNextEntry(ent);
            zipStream.SetLevel(6);
            zipStream.Write(buffer, 0, buffer.Length);
            return true;
        }

        /// <summary>   
        /// Zip压缩文件或文件夹   
        /// </summary>   
        /// <param name="fileToZip">要压缩的路径</param>   
        /// <param name="zipedFile">压缩后的文件名</param>   
        /// <param name="password">密码，默认：null</param>   
        /// <returns>压缩结果</returns>   
        public static bool Zip(string fileToZip, string zipedFile, string password = null)
        {
            var result = false;

            if (Directory.Exists(fileToZip))
                result = ZipDirectory(fileToZip, zipedFile, password);
            else if (File.Exists(fileToZip))
                result = ZipFile(fileToZip, zipedFile, password);

            return result;
        }

        /// <summary>
        /// Zip压缩文件或文件夹
        /// </summary>
        /// <param name="filesToZip">要批量压缩的路径或者文件夹</param>
        /// <param name="zipedFile">压缩后的文件名</param>
        /// <param name="password">密码，默认：null</param>
        /// <returns>压缩结果</returns>
        public static bool Zip(List<string> filesToZip, string zipedFile, string password = null)
        {
            var result = true;
            using var zipStream = new ZipOutputStream(File.Create(zipedFile));
            zipStream.SetLevel(6);

            if (password.IsNotNullOrEmpty())
                zipStream.Password = password;

            filesToZip.ForEach(o =>
            {
                if (Directory.Exists(o))
                {
                    if (!ZipDirectory(o, zipStream, ""))
                        result = false;
                }
                else if (File.Exists(o))
                {
                    if (!ZipFile(o, zipStream, password))
                        result = false;
                }
            });

            return result;
        }
        #endregion

        #region Zip解压
        /// <summary>   
        /// Zip解压功能(解压压缩文件到指定目录)   
        /// </summary>   
        /// <param name="fileToUnZip">待解压的文件</param>   
        /// <param name="zipedFolder">指定解压目标目录</param>   
        /// <param name="password">密码，默认：null</param>   
        /// <returns>解压结果</returns>   
        public static bool UnZip(string fileToUnZip, string zipedFolder, string password = null)
        {
            if (!File.Exists(fileToUnZip))
                return false;

            if (!Directory.Exists(zipedFolder))
                Directory.CreateDirectory(zipedFolder);

            using var zipStream = new ZipInputStream(File.OpenRead(fileToUnZip));

            if (password.IsNotNullOrEmpty())
                zipStream.Password = password;

            ZipEntry ent;
            while ((ent = zipStream.GetNextEntry()) != null)
            {
                if (ent.Name.IsNotNullOrEmpty())
                {
                    var fileName = Path.Combine(zipedFolder, ent.Name);
                    fileName = PathHelper.ConvertToCurrentOsPath(fileName);
                    if (fileName.EndsWith(PathHelper.CurrentOsDirectorySeparator.ToString()))
                    {
                        Directory.CreateDirectory(fileName);
                        continue;
                    }
                    using var fs = File.Create(fileName);
                    var buffer = new byte[2048];
                    var bytesRead = 0;
                    //每次读取2kb数据，然后写入文件
                    while ((bytesRead = zipStream.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        fs.Write(buffer, 0, bytesRead);
                    }
                }
            }
            return true;
        }
        #endregion
    }
}
