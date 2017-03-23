using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace AmcMtpService
{
    class LogHelper
    {
        ///支持多线程写日志需要引入Lock机制
        private static object lockTemp = new object();
        private static object lockTemp1 = new object();
        ///创建文件夹
        public static void CreateLogFile()
        {
            string filePath = @"D:\fengdong5\";
            if(!Directory.Exists(filePath))
            {
                try
                {
                    Directory.CreateDirectory(filePath);
                }
                catch(Exception ex)
                {
                    LogHelper.LogWrite(ex.Message);
                }
                    
            }
            if(!File.Exists(filePath+"LogFile.log"))
            {
                try
                {
                    FileStream file = File.Create(filePath + "LogFile.log");
                    file.Dispose();
                    file.Close();
                }
                catch(Exception ex)
                {
                    LogHelper.LogWrite(ex.Message);
                }
            }

        }
        ///向日志文件中写入日志
        public static void LogWrite(string logText)
        {
            CreateLogFile();
            lock (lockTemp)
            {
                string filePath = @"D:\fengdong5\LogFile.log";
                StreamWriter sw = new StreamWriter(filePath, true, System.Text.Encoding.UTF8);
                try
                {
                    sw.WriteLine("/**************************************/");
                    sw.WriteLine("日期：" + System.DateTime.Now.ToString());
                    sw.WriteLine(logText);
                    sw.WriteLine("/**************************************/");
                    sw.WriteLine();
                    sw.WriteLine();
                    sw.Flush();
                }
                catch (Exception ex)
                {
                    LogHelper.LogWrite(ex.Message);
                }
                finally
                {
                    sw.Dispose();
                    sw.Close();
                }
            }
            
        }

        public static void LogWrite(Exception ex)
        {
            CreateLogFile();
            lock (lockTemp1)
            {
                string filePath = @"D:\fengdong5\LogFile.log";
                StreamWriter sw = new StreamWriter(filePath, true, System.Text.Encoding.UTF8);
                try
                {
                    sw.WriteLine("/**************************************/");
                    sw.WriteLine("日期：" + System.DateTime.Now.ToString());
                    sw.WriteLine("错误源：" + ex.Source);
                    sw.WriteLine("错误信息：" + ex.Message);
                    sw.WriteLine("当前实例的运行时类型：" + ex.GetType());
                    sw.WriteLine("引发异常的方法名：" + ex.TargetSite);
                    sw.WriteLine("/**************************************/");
                    sw.WriteLine();
                    sw.WriteLine();
                    sw.Flush();
                }
                catch (Exception e)
                {
                    LogHelper.LogWrite(e);
                }
                finally
                {
                    sw.Close();
                    sw.Dispose();
                }
            }
            

        }
    }
}
