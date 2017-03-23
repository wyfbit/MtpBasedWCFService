using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.ServiceProcess;
using System.Text;
using System.Xml;
using System.Threading.Tasks;
using PortableDeviceLib;
using PortableDeviceLib.Model;
using System.Collections.ObjectModel;
using AmcMtpService.ServiceReference1;
using System.Threading;
using System.Runtime.Remoting.Messaging;

namespace AmcMtpService
{
    public partial class Service1 : ServiceBase
    {
        /// <summary>
        /// 定义需与服务器端同步更新的文件在Pad端中的位置
        /// </summary>
        private string padSourcePath = @"amc\AndroidIETM\projects\五所风洞交互式电子手册\uploadRefresh";
        private string pcTargetFolder = @"D:\fengdong5\amcPad_505Up";

        private string padTargetFolder = @"amc\AndroidIETM\projects\五所风洞交互式电子手册\download";
        //private string pcSourceFolder = @"D:\fengdong5\amcPad_505Down";

        /// <summary>
        /// 新建接入Pad的实例
        /// </summary>
        private string AppName = "PortableDeviceExplorer";
        private  int AppMajorVersionNumber = 1;
        private int AppMinorVersionNumber = 0;
        private ObservableCollection<PortableDevice> portableDevices;

        /// <summary>
        /// 只允许接入Pad的DeviceId为此两个的才能进行数据的更新和同步
        /// </summary>
        //private static string padOne = @"\\?\usb#vid_04e8&pid_6860&ms_comp_mtp&samsung_android#6&6e1987f&0&0000#{6ac27878-a6fa-4155-ba85-f98f491d4f33}";
        private static string padOne = @"\\?\usb#vid_04e8&pid_6860&mi_00#7&1bd42319&0&0000#{6ac27878-a6fa-4155-ba85-f98f491d4f33}";
        //private static string padTwo = @"\\?\usb#vid_04e8&pid_6860&ms_comp_mtp&samsung_android#6&1bd42319&1&0000#{6ac27878-a6fa-4155-ba85-f98f491d4f33}";
        private static string padTwo = @"\\?\usb#vid_04e8&pid_6860&mi_00#7&6e1987f&0&0000#{6ac27878-a6fa-4155-ba85-f98f491d4f33}";

        /// <summary>
        /// 采用异步委托方法调用服务，防止阻塞，提高传输资源效率
        /// </summary>
        /// <param name="DirToZip">待压缩源文件夹路径</param>
        /// <param name="ZipedFile">压缩后文件名称及路径</param>
        /// <param name="CompressionLevel">压缩等级，越大压缩后的量越小（0，9）</param>
        private delegate void AsycMyZipZipDir(string DirToZip, string ZipedFile, int CompressionLevel);
        private  AsycMyZipZipDir myZip;
        
        /// <summary>
        /// 异步方式下载设备台账信息
        /// </summary>
        private delegate byte[] AsycEquipInfo();
        private  AsycEquipInfo myEquipInfo;

        /// <summary>
        /// 异步方式上传upload压缩文件
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <returns></returns>
        private delegate bool AsycUploadFile(FileStream fs);
        private AsycUploadFile myUploadFile;

        /// <summary>
        /// 异步方式下载应急计划
        /// </summary>
        /// <returns></returns>
        private delegate byte[] AsycEmergencyPlan(string strYear, int departMentName);
        private AsycEmergencyPlan myEmergencyPlan;

        /// <summary>
        /// 异步方式下载备份备件信息
        /// </summary>
        /// <returns></returns>
        private delegate byte[] AsycBackupEquip();
        private AsycBackupEquip myBackupEquip;

        /// <summary>
        /// 以流方式上传Upload压缩文件
        /// </summary>
        FileStream fsUpload;
        /// <summary>
        /// 新建WCF服务实例
        /// </summary>
        DeskTopServiceClient wcfclient;
        /// <summary>
        /// 定义从服务器端下载文件到本地所放置的路径
        /// </summary>
        string wcfdownloadpath = @"D:\fengdong5\amcPad_505Down\download\";

        /// <summary>
        /// 同步历史记录XML
        /// </summary>
        string historyRecordXmlOne = @"D:\fengdong5\amcPad_505Down\backup\updateHistoryrecordOne.xml";
        string historyRecordXmlTwo = @"D:\fengdong5\amcPad_505Down\backup\updateHistoryrecordTwo.xml";
        ///压缩从Pad端拷贝出来的文件,首先删除文件中的带有Small_名称的无效缩略图片资源,减小体积
        string DirToZip = @"D:\fengdong5\amcPad_505Up\uploadRefresh";

        ///压缩后的Upload文件的存储位置
        string ZipedFile = @"D:\fengdong5\amcPad_505Up\upload.zip";


        /// <summary>
        /// 定义n确保每个过程都执行成功
        /// </summary>
        int n = 0;

        /// <summary>
        /// 定义从pad向PC复制的文件路径
        /// </summary>
        PortableDeviceContainerObject lastObjDown;
        /// <summary>
        /// 是否更新成功标志位
        /// </summary>
        bool isUpdateComplete = false;

        Thread MtpThread = null;

        public Service1()
        {
            InitializeComponent();
            base.CanStop = true;
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                MtpThread = new Thread(LoopDetection);
                MtpThread.Start();
            }
            catch (Exception ex)
            {
                LogHelper.LogWrite(ex.Message);
            }
        }

        protected override void OnStop()
        {
            try
            {
                if (MtpThread != null || MtpThread.IsAlive )
                    MtpThread.Abort();
            }
            catch (Exception ex)
            {
                LogHelper.LogWrite("结束循环线程失败：" + ex.Message);
            }
            LogHelper.LogWrite("MTP服务结束！"+DateTime.Now.ToString());
        }

        ///循环监测是否有Pad插入
        private void LoopDetection()
        {
            //LogHelper.LogWrite("MTP服务开启！");
            //创建同步历史记录XML
            

            while (true)
            {
                ///获取接入的Pad信息
                portableDevices = new ObservableCollection<PortableDevice>();
                
                if (PortableDeviceCollection.Instance == null)
                {
                    try
                    {
                        PortableDeviceCollection.CreateInstance(AppName, AppMajorVersionNumber, AppMinorVersionNumber);
                        PortableDeviceCollection.Instance.AutoConnectToPortableDevice = false;
                    }
                    catch(Exception ex)
                    {
                        LogHelper.LogWrite(ex.Message);
                    }
                }
                ///添加Pad设备,一次只能接入一个Pad设备
                foreach (var device in PortableDeviceCollection.Instance.Devices)
                {
                    if (device.DeviceId == padOne || device.DeviceId == padTwo)
                        portableDevices.Add(device);
                }
                if (portableDevices.Count >= 1)//需增加判断，是否远程WCF服务已开启？？
                {
                    if ((portableDevices[0].DeviceId == padOne || portableDevices[0].DeviceId == padTwo) && isUpdateComplete == false)
                    {
                        foreach (var portableDevice in portableDevices)
                        {
                            string[] paths = padSourcePath.Split('\\');
                            try
                            {
                                portableDevice.ConnectToDevice(AppName, AppMajorVersionNumber, AppMinorVersionNumber);
                            }
                            catch//(Exception ex)
                            {
                                //LogHelper.LogWrite(ex.Message);
                                continue;
                            }
                            portableDevice.ScanContent(paths);


                            PortableDeviceContainerObject lastObj = portableDevice.GetLast(portableDevice.Content);

                            if (lastObj.Name == "uploadRefresh")
                            {
                                portableDevice.ScanFolderEnumerate(lastObj.ID, lastObj);

                                //从Pad向PC复制文件
                                try
                                {
                                    portableDevice.CopyFolderToPC(lastObj, pcTargetFolder);
                                }
                                catch (Exception ex)
                                {
                                    LogHelper.LogWrite(ex);
                                }


                                ///查找Pad端指定位置
                                string[] paths1 = padTargetFolder.Split('\\');

                                portableDevice.ScanContent(paths1);

                                lastObjDown = portableDevice.GetLast(portableDevice.Content);

                                //find the all files in the last folder
                                portableDevice.ScanFolderEnumerate(lastObjDown.ID, lastObjDown);

                                //copy folder from pc to pad
                                //portableDevice.CopyFolderToPad(lastObjDown, pcSourceFolder);
                                if (Directory.Exists(pcTargetFolder + "\\uploadRefresh"))
                                {
                                    ///执行数据更新工作
                                    StartUpdateDate();
                                }


                                bool complete = true;
                                ///判断是否更新成功
                                do
                                {

                                    if (n == 6)
                                    {
                                        ///上传同步记录
                                        if (portableDevices[0].DeviceId == padOne)
                                        {
                                            portableDevices[0].CopyFolderToPad(lastObjDown, historyRecordXmlOne);
                                        }
                                        if (portableDevices[0].DeviceId == padTwo)
                                        {
                                            portableDevices[0].CopyFolderToPad(lastObjDown, historyRecordXmlTwo);
                                        }

                                        ///上传结束标志文件
                                        using (FileStream wcfFile = new FileStream(wcfdownloadpath + "time.xml", FileMode.Create))
                                        {
                                            byte[] bytes = new byte[0];
                                            wcfFile.Write(bytes, 0, bytes.Length);
                                            wcfFile.Flush();
                                        }
                                        try
                                        {
                                            portableDevices[0].CopyFolderToPad(lastObjDown, wcfdownloadpath + "time.xml");
                                        }
                                        catch (Exception ex)
                                        {
                                            LogHelper.LogWrite(ex.Message);
                                        }
                                        complete = false;
                                        n = 0;
                                        if (wcfclient != null)
                                        {
                                            try
                                            {
                                                wcfclient.Close();
                                            }
                                            catch (Exception ex)
                                            {
                                                wcfclient.Abort();
                                                LogHelper.LogWrite(ex);
                                            }
                                        }
                                    }
                                    Thread.Sleep(100);
                                }
                                while (complete);

                                isUpdateComplete = true;
                            }
                        }

                    }
                }
                if (portableDevices.Count == 0)
                {
                    isUpdateComplete = false;
                }
                if (portableDevices != null)
                    portableDevices = null;

                GC.Collect();
                Thread.Sleep(100);
            }
        }
        /// <summary>
        /// 异步方式进行数据的更新
        /// </summary>
        private void StartUpdateDate()
        {
            ///创建同步历史记录
            createUpdateRecordXml();
            /// <summary>
            /// 新建WCF服务实例
            /// </summary>
            try
            {
                wcfclient = new DeskTopServiceClient();
            }
            catch (Exception ex)
            {
                LogHelper.LogWrite(ex.Message);
            }
             ///下载维护保养计划
            try
            {
                byte[] bytes = wcfclient.GetPlanList();
                if (!Directory.Exists(wcfdownloadpath))
                {
                    Directory.CreateDirectory(wcfdownloadpath);
                    using (FileStream wcfFile = new FileStream(wcfdownloadpath + "planList.xml", FileMode.Create))
                    {
                        wcfFile.Write(bytes, 0, bytes.Length);
                        wcfFile.Flush();
                    }
                }
                else
                {
                    //File.Delete(wcfdownloadpath + "planList.xml");
                    using (FileStream wcfFile = new FileStream(wcfdownloadpath + "planList.xml", FileMode.Create))
                    {
                        wcfFile.Write(bytes, 0, bytes.Length);
                        wcfFile.Flush();
                    }
                }
                Thread.Sleep(100);
                try
                {
                    portableDevices[0].CopyFolderToPad(lastObjDown, wcfdownloadpath + "planList.xml");
                }
                catch (Exception ex)
                {
                    LogHelper.LogWrite(ex);
                }
                n = n + 1;
                Thread.Sleep(100);
                //MessageBox.Show("下载维护保养计划成功！");
            }
            catch(Exception ex)
            {
                LogHelper.LogWrite(ex);
            }


            ///下载应急预案
            //int a=int.Parse(DateTime.now)
            try
            {
                byte[] bytes = wcfclient.EmergencyDetail(DateTime.Now.ToString("yyyy"), 505);
                if (!Directory.Exists(wcfdownloadpath))
                {
                    Directory.CreateDirectory(wcfdownloadpath);
                    using (FileStream wcfFile = new FileStream(wcfdownloadpath + "应急预案.pdf", FileMode.Create))
                    {
                        wcfFile.Write(bytes, 0, bytes.Length);
                        wcfFile.Flush();
                    }
                }
                else
                {
                    //File.Delete(wcfdownloadpath + "planList.xml");
                    using (FileStream wcfFile = new FileStream(wcfdownloadpath + "应急预案.pdf", FileMode.Create))
                    {
                        wcfFile.Write(bytes, 0, bytes.Length);
                        wcfFile.Flush();
                    }
                }
                Thread.Sleep(100);
                try
                {
                    portableDevices[0].CopyFolderToPad(lastObjDown, wcfdownloadpath + "应急预案.pdf");
                }
                catch (Exception ex)
                {
                    LogHelper.LogWrite(ex);
                }
                n = n + 1;
                Thread.Sleep(100);
            }
            catch (Exception ex)
            {
                LogHelper.LogWrite(ex);
            }
            
            ///异步方式下载备份备件信息
            try
            {
                myBackupEquip = new AsycBackupEquip(wcfclient.GetProduct);
                myBackupEquip.BeginInvoke(new AsyncCallback(IsDownBackupEquipCom), null);
            }
            catch (Exception ex)
            {
                LogHelper.LogWrite(ex);
            }

            //异步方式下载应急计划
            try
            {
                string currentYear = DateTime.Now.ToString("yyyy");
                int departMentName = 505;
                myEmergencyPlan = new AsycEmergencyPlan(wcfclient.EmergencyDetail);
                myEmergencyPlan.BeginInvoke(currentYear,departMentName,new AsyncCallback(IsEmergencyPlanCom), null);
            }
            catch (Exception ex)
            {
                LogHelper.LogWrite(ex);
            }

            int CompressionLevel = 9;
            try
            {
                myZip = new AsycMyZipZipDir(new ICharpZip().ZipDir);
                myZip.BeginInvoke(DirToZip, ZipedFile, CompressionLevel, new AsyncCallback(IsZipComplete), null);
            }
            catch (Exception ex)
            {
                LogHelper.LogWrite(ex);
            }

            ///异步方法下载设备台账信息
            try
            {
                myEquipInfo = new AsycEquipInfo(wcfclient.GetTZList);
                myEquipInfo.BeginInvoke(new AsyncCallback(IsDownEquipInfoComplete), null);
            }
            catch(Exception ex)
            {
                LogHelper.LogWrite(ex);
            }
        }
        /// <summary>
        /// 删除Upload文件夹中的带有small_的缩略图片
        /// </summary>
        /// <param name="dirpath"></param>
        public void FindFileAndDelete(string dirpath)
        {
            DirectoryInfo Dir = new DirectoryInfo(dirpath);
            try
            {
                foreach (DirectoryInfo d in Dir.GetDirectories())
                {
                    FindFileAndDelete(Dir + @"\" + d.ToString());
                }
                foreach (FileInfo f in Dir.GetFiles("*.png"))
                {
                    if (f.Name.Contains("small_"))
                    {
                        f.Delete();
                    }
                }
            }
            catch (Exception e)
            {
                //MessageBox.Show(e.Message);
                //throw new Exception 
                LogHelper.LogWrite(e);
            }
        }
        /// <summary>
        /// 删除无效记录文件
        /// </summary>
        /// <param name="pcTargetFolder">指定路径</param>
        private void DeleteInvalidFile(string pcTargetFolder)
        {

            if (!Directory.Exists(pcTargetFolder))
            {
                return;
            }
            try
            {
                DirectoryInfo Dir = new DirectoryInfo(pcTargetFolder);
                if (Dir.GetDirectories().Length > 0 && Dir.GetFiles().Length == 0)
                {
                    foreach (DirectoryInfo dI in Dir.GetDirectories())
                    {
                        DeleteInvalidFile(Dir + dI.ToString() + @"\");
                    }

                }
                else
                {
                    string[] strDir = Directory.GetFiles(Dir.FullName, "*.xml");
                    if (strDir.Length == 0)
                        Directory.Delete(Dir.FullName, true);
                }

            }
            catch (Exception ex)
            {
                LogHelper.LogWrite(ex);
            }
        }
        /// <summary>
        /// 压缩上传前删除已经上传的文件夹
        /// </summary>
        private void DeleteCompleteUpdateFile()
        {
            XmlDocument xmlDoc = new XmlDocument();
            if (portableDevices[0].DeviceId == padOne)
            {
                xmlDoc.Load(historyRecordXmlOne);
            }
            if (portableDevices[0].DeviceId == padTwo)
            {
                xmlDoc.Load(historyRecordXmlTwo);
            }
            XmlNode xmlWeihu = xmlDoc.SelectSingleNode("//system[@name='维护保养']");
            XmlNode xmlGuzhang = xmlDoc.SelectSingleNode("//system[@name='故障记录']");
            XmlNode xmlZhuangbei = xmlDoc.SelectSingleNode("//system[@name='装备检查']");
            XmlNode xmlYingji = xmlDoc.SelectSingleNode("//system[@name='应急预案']");

            DirectoryInfo Dir = new DirectoryInfo(@"D:\fengdong5\amcPad_505Up\upload");
            foreach (DirectoryInfo dI in Dir.GetDirectories())
            {
                if (dI.Name == "应急预案")
                {
                    foreach (DirectoryInfo dIF in new DirectoryInfo((Dir + @"\" + dI.Name)).GetDirectories())
                    {
                        foreach (XmlNode nodes in xmlYingji.ChildNodes)
                        {
                            if (nodes.Attributes["contentdate"].Value == dIF.Name)
                            {
                                Directory.Delete(dIF.FullName, true);
                            }
                        }
                    }
                }
                else if (dI.Name == "装备检查")
                {
                    foreach (DirectoryInfo dIF in new DirectoryInfo((Dir + @"\" + dI.Name)).GetDirectories())
                    {
                        foreach (XmlNode nodes in xmlZhuangbei.ChildNodes)
                        {
                            if (nodes.Attributes["contentdate"].Value == dIF.Name)
                            {
                                Directory.Delete(dIF.FullName, true);
                            }
                        }
                    }
                }
                else if (dI.Name == "故障记录")
                {
                    foreach (DirectoryInfo dIF in new DirectoryInfo((Dir + @"\" + dI.Name)).GetDirectories())
                    {
                        foreach (XmlNode nodes in xmlGuzhang.ChildNodes)
                        {
                            if (nodes.Attributes["contentdate"].Value == dIF.Name)
                            {
                                if (dIF.Exists)
                                {
                                    Directory.Delete(dIF.FullName, true);
                                }
                                //Directory.Delete(dIF.FullName, true);
                            }
                        }
                    }
                }
                else if (dI.Name == "维护保养")
                {
                    foreach (DirectoryInfo dIF in new DirectoryInfo((Dir + @"\" + dI.Name)).GetDirectories())
                    {
                        
                        foreach (DirectoryInfo dIF1 in new DirectoryInfo((Dir + @"\" + dI.Name + @"\" + dIF.Name)).GetDirectories())
                        {

                            foreach (DirectoryInfo dIF2 in new DirectoryInfo((Dir + @"\" + dI.Name + @"\" + dIF.Name + @"\" + dIF1.Name)).GetDirectories())
                            {
                                foreach (XmlNode nodes in xmlWeihu.ChildNodes)
                                {
                                    if (nodes.Attributes["contentdate"].Value == dIF2.Name && nodes.Attributes["name"].Value == dIF.Name && nodes.Attributes["ruleclass"].Value == dIF1.Name)
                                    {
                                        Directory.Delete(dIF2.FullName, true);
                                    }
                                }
                            }
                        }
                    }
                }
                //DeleteInvalidFile(Dir + dI.ToString() + @"\");
            }
            if (portableDevices[0].DeviceId == padOne)
            {
                xmlDoc.Save(historyRecordXmlOne);
            }
            if (portableDevices[0].DeviceId == padTwo)
            {
                xmlDoc.Save(historyRecordXmlTwo);
            }
        }
        /// <summary>
        /// 异步方式下载备份备件信息回调函数
        /// </summary>
        /// <param name="iar"></param>
        private void IsDownBackupEquipCom(IAsyncResult iar)
        {
            byte[] bytes;
            //string wcfdownloadpath = @"D:\download\";
            try
            {
                AsyncResult ar = (AsyncResult)iar;
                myBackupEquip = (AsycBackupEquip)ar.AsyncDelegate;
                bytes = myBackupEquip.EndInvoke(iar);
                if (!Directory.Exists(wcfdownloadpath))
                {
                    Directory.CreateDirectory(wcfdownloadpath);
                    using (FileStream wcfFile = new FileStream(wcfdownloadpath + "BackupEquip.xml", FileMode.Create))
                    {
                        wcfFile.Write(bytes, 0, bytes.Length);
                        wcfFile.Flush();
                    }
                }
                else
                {
                    if (File.Exists(wcfdownloadpath + "BackupEquip.xml"))
                    {
                        File.Delete(wcfdownloadpath + "BackupEquip.xml");
                    }
                    
                    using (FileStream wcfFile = new FileStream(wcfdownloadpath + "BackupEquip.xml", FileMode.Create))
                    {
                        wcfFile.Write(bytes, 0, bytes.Length);
                        wcfFile.Flush();
                    }
                }
                Thread.Sleep(100);
            }
            catch (Exception ex)
            {
                LogHelper.LogWrite(ex);
            }
            try
            {
                portableDevices[0].CopyFolderToPad(lastObjDown, wcfdownloadpath + "BackupEquip.xml");
            }
            catch (Exception ex)
            {
                LogHelper.LogWrite(ex);
                //continue;
            }            
			n = n + 1;
            Thread.Sleep(100);
            iar.AsyncWaitHandle.Close();
        }
        /// <summary>
        /// 下载应急计划回调函数
        /// </summary>
        /// <param name="iar"></param>
        private void IsEmergencyPlanCom(IAsyncResult iar)
        {
            byte[] bytes;
            //string wcfdownloadpath = @"D:\download\";
            try
            {
                AsyncResult ar = (AsyncResult)iar;
                myEmergencyPlan = (AsycEmergencyPlan)ar.AsyncDelegate;
                bytes = myEmergencyPlan.EndInvoke(iar);
                if (!Directory.Exists(wcfdownloadpath))
                {
                    Directory.CreateDirectory(wcfdownloadpath);
                }
                if(File.Exists(wcfdownloadpath + "EmergencyPlan.xml"))
                {
                    File.Delete(wcfdownloadpath + "EmergencyPlan.xml");
                }
                FileStream wcfFile = null;
                try
                {
                    wcfFile = new FileStream(wcfdownloadpath + "EmergencyPlan.xml", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                      wcfFile.Write(bytes, 0, bytes.Length);
                      wcfFile.Flush();
                }
                catch(Exception ex)
                {
                     LogHelper.LogWrite(ex);
                }
                finally
                {
                         wcfFile.Close();
                         wcfFile.Dispose();
                }
                Thread.Sleep(100);
            }
            catch (Exception ex)
            {
                LogHelper.LogWrite(ex);
            }
            try
            {
                portableDevices[0].CopyFolderToPad(lastObjDown, wcfdownloadpath + "EmergencyPlan.xml");
            }
            catch (Exception ex)
            {
                LogHelper.LogWrite(ex);
            }
            n = n + 1;
            Thread.Sleep(100);
            iar.AsyncWaitHandle.Close();
        }
        /// <summary>
        /// BeginInvoke方法执行完成后调用该异步委托回调方法
        /// </summary>
        /// <param name="iar">用于监视异步调用进度</param>
        private void IsZipComplete(IAsyncResult iar)
        {
            AsyncResult ar = (AsyncResult)iar;
            myZip = (AsycMyZipZipDir)ar.AsyncDelegate;
            myZip.EndInvoke(iar);
            Thread.Sleep(1000);
            //MessageBox.Show("压缩upload文件夹成功！");
            //isZipCompleteMark = true;
            fsUpload = new FileStream(ZipedFile, FileMode.Open, FileAccess.Read);

            myUploadFile = new AsycUploadFile(wcfclient.UpLoadFile);
            myUploadFile.BeginInvoke(fsUpload,new AsyncCallback(isUploadComplete),null);

            
            /*
            //bool a = wcfclient.getBytes(bytes1);
            fs.Read(bytes1, 0, bytes1.Length);
            fs.Close();
            fs.Dispose();
            bool b = wcfclient.GetUpLoadBytes(bytes1, DateTime.Now, "upload");*/
        }
        /// <summary>
        /// 判断是否下载设备台账信息成功
        /// </summary>
        /// <param name="iar"></param>
        private void IsDownEquipInfoComplete(IAsyncResult iar)
        {         
            //string wcfdownloadpath = @"D:\download\";
            byte[] bytes;
            try
            {
                AsyncResult ar = (AsyncResult)iar;
                myEquipInfo = (AsycEquipInfo)ar.AsyncDelegate;
                bytes = myEquipInfo.EndInvoke(iar);
                if (!Directory.Exists(wcfdownloadpath))
                {
                    Directory.CreateDirectory(wcfdownloadpath);
                    FileStream wcfFile = new FileStream(wcfdownloadpath + "EquipInfo.xml", FileMode.Create);
                    wcfFile.Write(bytes, 0, bytes.Length);
                    wcfFile.Flush();
                    wcfFile.Close();
                    wcfFile.Dispose();
                }
                else
                {
                    if (File.Exists(wcfdownloadpath + "EquipInfo.xml"))
                    {
                        File.Delete(wcfdownloadpath + "EquipInfo.xml");
                    }
                    FileStream wcfFile = null;
                    try
                    {
                        wcfFile = new FileStream(wcfdownloadpath + "EquipInfo.xml", FileMode.Create);
                        wcfFile.Write(bytes, 0, bytes.Length);
                        wcfFile.Flush();
                    }
                    catch (Exception ex)
                    {
                        LogHelper.LogWrite(ex);
                    }
                    finally
                    {
                        wcfFile.Close();
                        wcfFile.Dispose();
                    }
                }
                try
                {
                    portableDevices[0].CopyFolderToPad(lastObjDown, wcfdownloadpath + "EquipInfo.xml");
                }
                catch (Exception ex)
                {
                    LogHelper.LogWrite(ex);
                }
                n = n + 1;
                Thread.Sleep(100);
                //MessageBox.Show("下载设备台账信息XML成功！");
            }
            catch(Exception ex)
            {
                LogHelper.LogWrite(ex);
            }
        }
        /// <summary>
        /// 异步回调方法判断是否上传Upload压缩文件成功
        /// </summary>
        /// <param name="iar"></param>
        private void isUploadComplete(IAsyncResult iar)
        {
            try
            {
                AsyncResult ar = (AsyncResult)iar;
                myUploadFile = (AsycUploadFile)ar.AsyncDelegate;
                if (myUploadFile.EndInvoke(iar))
                {
                    fsUpload.Close();
                    fsUpload.Dispose();
                    ///上传成功后记录在XML中
                    UpdateHisRecord();
                    //portableDevices[0].CopyFolderToPad(lastObjDown, wcfdownloadpath + @"\EquipInfo.xml");
                    n = n + 1;
                    //删除已上传文件夹uploadRefesh
                    DeleteDirectory(DirToZip);
                }
            }
            catch(Exception ex)
            {
                LogHelper.LogWrite(ex);
            }
            iar.AsyncWaitHandle.Close();
        }
        /// <summary>
        /// 删除文件夹
        /// </summary>
        /// <param name="path">所要删除的文件夹路径</param>
        private static void DeleteDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                DirectoryInfo di = new DirectoryInfo(path);
                di.Delete(true);
            }
        }
        /// <summary>
        /// 在本机上存储同步历史记录的XML，并维护
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void createUpdateRecordXml()
        {
            if (!Directory.Exists(@"D:\fengdong5\amcPad_505Down\backup"))
            {
                Directory.CreateDirectory(@"D:\fengdong5\amcPad_505Down\backup");
                XmlDocument xdc = new XmlDocument();
                XmlNode node = xdc.CreateXmlDeclaration("1.0", "UTF-8", "");
                xdc.AppendChild(node);
                XmlNode root = xdc.CreateElement("root");
                xdc.AppendChild(root);
                XmlElement systemNode = xdc.CreateElement("system");
                systemNode.SetAttribute("name", "维护保养");
                root.AppendChild(systemNode);

                XmlElement systemNode1 = xdc.CreateElement("system");
                systemNode1.SetAttribute("name", "故障记录");
                root.AppendChild(systemNode1);

                XmlElement systemNode2 = xdc.CreateElement("system");
                systemNode2.SetAttribute("name", "装备检查");
                root.AppendChild(systemNode2);
                XmlElement systemNode3 = xdc.CreateElement("system");
                systemNode3.SetAttribute("name", "应急预案");
                root.AppendChild(systemNode3);
                if (portableDevices[0].DeviceId == padOne)
                {
                    xdc.Save(@"D:\fengdong5\amcPad_505Down\backup\" + "updateHistoryrecordOne.xml");
                }
                if (portableDevices[0].DeviceId == padTwo)
                {
                    xdc.Save(@"D:\fengdong5\amcPad_505Down\backup\" + "updateHistoryrecordTwo.xml");
                }
            }
            if ((!File.Exists(@"D:\fengdong5\amcPad_505Down\backup\" + "updateHistoryrecordOne.xml")) && (portableDevices[0].DeviceId == padOne))
            {
                XmlDocument xdc = new XmlDocument();
                XmlNode node = xdc.CreateXmlDeclaration("1.0", "UTF-8", "");
                xdc.AppendChild(node);
                XmlNode root = xdc.CreateElement("root");
                xdc.AppendChild(root);
                XmlElement systemNode = xdc.CreateElement("system");
                systemNode.SetAttribute("name", "维护保养");
                root.AppendChild(systemNode);

                XmlElement systemNode1 = xdc.CreateElement("system");
                systemNode1.SetAttribute("name", "故障记录");
                root.AppendChild(systemNode1);
                XmlElement systemNode2 = xdc.CreateElement("system");
                systemNode2.SetAttribute("name", "装备检查");
                root.AppendChild(systemNode2);
                XmlElement systemNode3 = xdc.CreateElement("system");
                systemNode3.SetAttribute("name", "应急预案");
                root.AppendChild(systemNode3);
                xdc.Save(@"D:\fengdong5\amcPad_505Down\backup\" + "updateHistoryrecordOne.xml");
            }
            if ((!File.Exists(@"D:\fengdong5\amcPad_505Down\backup\" + "updateHistoryrecordTwo.xml")) && (portableDevices[0].DeviceId == padTwo))
            {
                XmlDocument xdc = new XmlDocument();
                XmlNode node = xdc.CreateXmlDeclaration("1.0", "UTF-8", "");
                xdc.AppendChild(node);
                XmlNode root = xdc.CreateElement("root");
                xdc.AppendChild(root);
                XmlElement systemNode = xdc.CreateElement("system");
                systemNode.SetAttribute("name", "维护保养");
                root.AppendChild(systemNode);
                XmlElement systemNode1 = xdc.CreateElement("system");
                systemNode1.SetAttribute("name", "故障记录");
                root.AppendChild(systemNode1);
                XmlElement systemNode2 = xdc.CreateElement("system");
                systemNode2.SetAttribute("name", "装备检查");
                root.AppendChild(systemNode2);
                XmlElement systemNode3 = xdc.CreateElement("system");
                systemNode3.SetAttribute("name", "应急预案");
                root.AppendChild(systemNode3);

                xdc.Save(@"D:\fengdong5\amcPad_505Down\backup\" + "updateHistoryrecordTwo.xml");
            }
        }
        /// <summary>
        /// 记录同步历史并上传至Pad端进行展示
        /// </summary>
        private void UpdateHisRecord()
        {
            XmlDocument xmlDoc = new XmlDocument();
            if (portableDevices[0].DeviceId == padOne)
            {
                xmlDoc.Load(historyRecordXmlOne);
            }
            if (portableDevices[0].DeviceId == padTwo)
            {
                xmlDoc.Load(historyRecordXmlTwo);
            }
            
            XmlNode xmlWeihu = xmlDoc.SelectSingleNode("//system[@name='维护保养']");
            XmlNode xmlGuzhang = xmlDoc.SelectSingleNode("//system[@name='故障记录']");
            XmlNode xmlZhuangbei = xmlDoc.SelectSingleNode("//system[@name='装备检查']");
            XmlNode xmlYingji = xmlDoc.SelectSingleNode("//system[@name='应急预案']");

            DirectoryInfo Dir = new DirectoryInfo(@"D:\fengdong5\amcPad_505Up\uploadRefresh");
            foreach (DirectoryInfo dI in Dir.GetDirectories())
            {
                if (dI.Name == "应急预案")
                {
                    //判断是否含有子节点，若含有则全部删除，即只记录本次上传文件
                    if (xmlYingji.HasChildNodes)
                    {
                        xmlYingji.RemoveAll();
                        xmlYingji.Attributes.Append(CreateAttribute(xmlYingji, "name", dI.Name));
                    }
                    foreach (DirectoryInfo dIF in new DirectoryInfo((Dir + @"\" + dI.Name)).GetDirectories())
                    {
                        XmlNode xmlYing = xmlDoc.CreateElement("updateList");
                        xmlYing.Attributes.Append(CreateAttribute(xmlYing, "contentdate", dIF.Name));
                        xmlYing.Attributes.Append(CreateAttribute(xmlYing, "updatedate", DateTime.Now.ToString()));
                        xmlYingji.AppendChild(xmlYing);
                    }
                }
                if (dI.Name == "装备检查")
                {
                    if (xmlZhuangbei.HasChildNodes)
                    {
                        xmlZhuangbei.RemoveAll();
                        xmlZhuangbei.Attributes.Append(CreateAttribute(xmlZhuangbei, "name", dI.Name));
                    }
                    foreach (DirectoryInfo dIF in new DirectoryInfo((Dir + @"\" + dI.Name)).GetDirectories())
                    {
                        XmlNode xmlZhuang = xmlDoc.CreateElement("updateList");
                        xmlZhuang.Attributes.Append(CreateAttribute(xmlZhuang, "contentdate", dIF.Name));
                        xmlZhuang.Attributes.Append(CreateAttribute(xmlZhuang, "updatedate", DateTime.Now.ToString()));
                        xmlZhuangbei.AppendChild(xmlZhuang);
                    }
                }
                if (dI.Name == "故障记录")
                {
                    if(xmlGuzhang.HasChildNodes)
                    {
                        xmlGuzhang.RemoveAll();
                        xmlGuzhang.Attributes.Append(CreateAttribute(xmlGuzhang, "name", dI.Name));
                    }
                    foreach (DirectoryInfo dIF in new DirectoryInfo((Dir + @"\" + dI.Name)).GetDirectories())
                    {
                        XmlNode xmlGu = xmlDoc.CreateElement("updateList");
                        xmlGu.Attributes.Append(CreateAttribute(xmlGu, "contentdate", dIF.Name));
                        xmlGu.Attributes.Append(CreateAttribute(xmlGu, "updatedate", DateTime.Now.ToString()));
                        xmlGuzhang.AppendChild(xmlGu);
                    }
                }
                if (dI.Name == "维护保养")
                {
                    if (xmlWeihu.HasChildNodes)
                    {
                        xmlWeihu.RemoveAll();
                        xmlWeihu.Attributes.Append(CreateAttribute(xmlWeihu, "name", dI.Name));
                    }
                    foreach (DirectoryInfo dIF in new DirectoryInfo((Dir + @"\" + dI.Name)).GetDirectories())
                    {
                        string systemName = dIF.Name;

                        foreach (DirectoryInfo dIF1 in new DirectoryInfo((Dir + @"\" + dI.Name + @"\" + systemName)).GetDirectories())
                        {
                            string ruleClass = dIF1.Name;
                            foreach (DirectoryInfo dIF2 in new DirectoryInfo((Dir + @"\" + dI.Name + @"\" + systemName + @"\" + ruleClass)).GetDirectories())
                            {
                                XmlNode xmlWei = xmlDoc.CreateElement("updateList");
                                xmlWei.Attributes.Append(CreateAttribute(xmlWei, "name", dIF.Name));
                                xmlWei.Attributes.Append(CreateAttribute(xmlWei, "ruleclass", dIF1.Name));
                                xmlWei.Attributes.Append(CreateAttribute(xmlWei, "contentdate", dIF2.Name));
                                xmlWei.Attributes.Append(CreateAttribute(xmlWei, "updatedate", DateTime.Now.ToString()));
                                xmlWeihu.AppendChild(xmlWei);
                            }
                        }
                    }
                }
                //DeleteInvalidFile(Dir + dI.ToString() + @"\");
            }
            if (portableDevices[0].DeviceId == padOne)
            {
                xmlDoc.Save(historyRecordXmlOne);
            }
            if (portableDevices[0].DeviceId == padTwo)
            {
                xmlDoc.Save(historyRecordXmlTwo);
            }
            

        }

        /// <summary>
        /// 添加节点属性方法
        /// </summary>
        /// <param name="node">要添加的节点</param>
        /// <param name="attributeName">属性名称</param>
        /// <param name="value">属性值</param>
        /// <returns></returns>
        private XmlAttribute CreateAttribute(XmlNode node, string attributeName, string value)
        {
            try
            {
                XmlDocument doc = node.OwnerDocument;
                XmlAttribute attr = null;
                attr = doc.CreateAttribute(attributeName);
                attr.Value = value;
                node.Attributes.SetNamedItem(attr);
                return attr;
            }
            catch (Exception ex)
            {
                LogHelper.LogWrite(ex);
                return null;
            }
        }  

    }
}
