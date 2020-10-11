using Microsoft.Practices.Prism.ViewModel;
using Microsoft.Practices.Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using StatisticsDemo.Model;
using System.Data;
using System.Windows.Threading;
using OfficeOpenXml;
using System.Threading;

namespace StatisticsDemo.ViewModel
{
    class MainWindowViewModel : NotificationObject
    {
        #region 属性绑定
        private string version;

        public string Version
        {
            get { return version; }
            set
            {
                version = value;
                this.RaisePropertyChanged("Version");
            }
        }
        private string messageStr;

        public string MessageStr
        {
            get { return messageStr; }
            set
            {
                messageStr = value;
                this.RaisePropertyChanged("MessageStr");
            }
        }
        private string hmePageVisibility;

        public string HomePageVisibility
        {
            get { return hmePageVisibility; }
            set
            {
                hmePageVisibility = value;
                this.RaisePropertyChanged("HomePageVisibility");
            }
        }
        private string alarmPageVisibility;

        public string AlarmPageVisibility
        {
            get { return alarmPageVisibility; }
            set
            {
                alarmPageVisibility = value;
                this.RaisePropertyChanged("AlarmPageVisibility");
            }
        }
        private MachineStateViewModel machineStateA;

        public MachineStateViewModel MachineStateA
        {
            get { return machineStateA; }
            set
            {
                machineStateA = value;
                this.RaisePropertyChanged("MachineStateA");
            }
        }
        private ObservableCollection<AlarmRecordViewModel> alarmRecord;

        public ObservableCollection<AlarmRecordViewModel> AlarmRecord
        {
            get { return alarmRecord; }
            set
            {
                alarmRecord = value;
                this.RaisePropertyChanged("AlarmRecord");
            }
        }

        #endregion
        #region 方法绑定
        public DelegateCommand AppLoadedEventCommand { get; set; }
        public DelegateCommand<object> OperateButtonCommand { get; set; }
        public DelegateCommand<object> MenuActionCommand { get; set; }
        #endregion
        #region 变量
        DispatcherTimer dispatcherTimer = new DispatcherTimer();
        private string iniParameterPath = System.Environment.CurrentDirectory + "\\Parameter.ini";
        string LastBanci;
        List<AlarmData> AlarmList = new List<AlarmData>();
        int D300 = -1;
        #endregion
        #region 构造函数
        public MainWindowViewModel()
        {
            #region 初始化参数
            Version = "20201011";
            MessageStr = "";
            HomePageVisibility = "Visible";
            AlarmPageVisibility = "Collapsed";
            try
            {
                using (StreamReader reader = new StreamReader(System.IO.Path.Combine(System.Environment.CurrentDirectory, "MachineStateA.json")))
                {
                    string json = reader.ReadToEnd();
                    MachineStateA = JsonConvert.DeserializeObject<MachineStateViewModel>(json);
                }
            }
            catch (Exception ex)
            {
                MachineStateA = new MachineStateViewModel()
                {
                    DaiLiao = 0,
                    YangBen = 0,
                    TesterAlarm = 0,
                    Down = 0,
                    UploaderAlarm = 0,
                    Run = 0
                };
                WriteToJson(MachineStateA, System.IO.Path.Combine(System.Environment.CurrentDirectory, "MachineStateA.json"));
                AddMessage(ex.Message);
            }
            AlarmRecord = new ObservableCollection<AlarmRecordViewModel>();
            try
            {
                if (!Directory.Exists(@"D:\报警记录"))
                {
                    Directory.CreateDirectory(@"D:\报警记录");
                }
                DataTable dt;
                Csvfile.csv2dt(System.IO.Path.Combine(@"D:\报警记录", "AlarmRecord" + GetBanci() + ".csv"), 1, out dt);
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    AlarmRecordViewModel newrow = new AlarmRecordViewModel
                    {
                        Time = Convert.ToDateTime(dt.Rows[i][0]),
                        Code = (string)dt.Rows[i][1],
                        Content = (string)dt.Rows[i][2]
                    };
                    AlarmRecord.Add(newrow);
                }
            }
            catch (Exception ex)
            {
                AddMessage(ex.Message);
            }
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Tick += DispatcherTimer_Tick;
            dispatcherTimer.Start();

            LastBanci = Inifile.INIGetStringValue(iniParameterPath, "Summary", "LastBanci", "null");
            #endregion

            // If you use EPPlus in a noncommercial context
            // according to the Polyform Noncommercial license:
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            #region 报警文档
            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                string alarmExcelPath = Path.Combine(System.Environment.CurrentDirectory, "报警.xlsx");
                if (File.Exists(alarmExcelPath))
                {

                    FileInfo existingFile = new FileInfo(alarmExcelPath);
                    using (ExcelPackage package = new ExcelPackage(existingFile))
                    {
                        // get the first worksheet in the workbook
                        ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
                        for (int i = 1; i <= worksheet.Dimension.End.Row; i++)
                        {
                            AlarmData ad = new AlarmData();
                            ad.Code = worksheet.Cells["A" + i.ToString()].Value == null ? "Null" : worksheet.Cells["A" + i.ToString()].Value.ToString();
                            ad.Content = worksheet.Cells["B" + i.ToString()].Value == null ? "Null" : worksheet.Cells["B" + i.ToString()].Value.ToString();
                            ad.Type = worksheet.Cells["C" + i.ToString()].Value == null ? "Null" : worksheet.Cells["C" + i.ToString()].Value.ToString();
                            ad.Start = DateTime.Now;
                            ad.End = DateTime.Now;
                            ad.State = false;
                            AlarmList.Add(ad);
                        }
                        AddMessage("读取到" + worksheet.Dimension.End.Row.ToString() + "条报警");
                    }
                }
                else
                {
                    AddMessage("VPP报警.xlsx 文件不存在");
                }
            }
            catch (Exception ex)
            {
                AddMessage(ex.Message);
            }
            #endregion

            AppLoadedEventCommand = new DelegateCommand(new Action(this.AppLoadedEventCommandExecute));
            OperateButtonCommand = new DelegateCommand<object>(new Action<object>(this.OperateButtonCommandExecute));
            MenuActionCommand = new DelegateCommand<object>(new Action<object>(this.MenuActionCommandExecute));
        }
        #endregion
        #region 方法绑定函数
        private void AppLoadedEventCommandExecute()
        {
            AddMessage("软件加载完成");
            Run();
        }
        private void MenuActionCommandExecute(object obj)
        {
            switch (obj.ToString())
            {
                case "0":
                    HomePageVisibility = "Visible";
                    AlarmPageVisibility = "Collapsed";
                    break;
                case "1":
                    break;
                case "2":
                    HomePageVisibility = "Collapsed";
                    AlarmPageVisibility = "Visible";
                    break;
                default:
                    break;
            }
        }
        private void OperateButtonCommandExecute(object obj)
        {
            switch (obj.ToString())
            {
                case "0":
                    //AddMessage("Start");
                    //Inifile.INIWriteValue(iniParameterPath, "AlarmCommand", "Code", "M300");
                    break;
                default:
                    break;
            }
        }


        #endregion
        #region 事件响应函数
        //每秒执行一次
        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            //            项目 说明  优先级
            //机台停机时间  待料 上料盘、下料盘传感器感应无料  0
            //    样本 样本测试    1
            //    报警 测试机 测试机报警   2
            //    故障 急停、开门、非运行流程 3
            //    报警 上料机 上料机所有报警 4
            //机台运行时间              5

            switch (D300)
            {
                case 1:
                    MachineStateA.DaiLiao += (double)1 / 60;
                    break;
                case 2:
                    MachineStateA.YangBen += (double)1 / 60;
                    break;
                case 3:
                    MachineStateA.TesterAlarm += (double)1 / 60;
                    break;
                case 4:
                    MachineStateA.Down += (double)1 / 60;
                    break;
                case 5:
                    MachineStateA.UploaderAlarm += (double)1 / 60;
                    break;
                case 6:
                    MachineStateA.Run += (double)1 / 60;
                    break;
                default:
                    break;
            }
            if (D300 > 0 && D300 < 7)
            {
                WriteToJson(MachineStateA, System.IO.Path.Combine(System.Environment.CurrentDirectory, "MachineStateA.json"));
            }
        }
        #endregion
        #region 自定义函数
        private void AddMessage(string str)
        {
            string[] s = MessageStr.Split('\n');
            if (s.Length > 1000)
            {
                MessageStr = "";
            }
            if (MessageStr != "")
            {
                MessageStr += "\n";
            }
            MessageStr += System.DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " " + str;
        }
        private void WriteToJson(object p, string path)
        {
            try
            {
                using (FileStream fs = File.Open(path, FileMode.Create))
                using (StreamWriter sw = new StreamWriter(fs))
                using (JsonWriter jw = new JsonTextWriter(sw))
                {
                    jw.Formatting = Formatting.Indented;
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(jw, p);
                }
            }
            catch (Exception ex)
            {
                AddMessage(ex.Message);
            }
        }
        private string GetBanci()
        {
            string rs = "";
            if (DateTime.Now.Hour >= 8 && DateTime.Now.Hour < 20)
            {
                rs += DateTime.Now.ToString("yyyyMMdd") + "_D";
            }
            else
            {
                if (DateTime.Now.Hour >= 0 && DateTime.Now.Hour < 8)
                {
                    rs += DateTime.Now.AddDays(-1).ToString("yyyyMMdd") + "_N";
                }
                else
                {
                    rs += DateTime.Now.ToString("yyyyMMdd") + "_N";
                }
            }
            return rs;
        }
        private void WriteStatetoExcel(string filepath)
        {
            try
            {
                using (ExcelPackage package = new ExcelPackage())
                {
                    var ws = package.Workbook.Worksheets.Add("MySheet");
                    ws.Cells[1, 1].Value = "A";
                    ws.Cells[1, 3].Value = DateTime.Now.ToString();
                    ws.Cells[2, 1].Value = "项目";
                    ws.Cells[2, 2].Value = "时间(单位min)";
                    ws.Cells[3, 1].Value = "待料";
                    ws.Cells[3, 2].Value = Math.Round(MachineStateA.DaiLiao, 1);
                    ws.Cells[4, 1].Value = "样本";
                    ws.Cells[4, 2].Value = Math.Round(MachineStateA.YangBen, 1);
                    ws.Cells[5, 1].Value = "测试机报警";
                    ws.Cells[5, 2].Value = Math.Round(MachineStateA.TesterAlarm, 1);
                    ws.Cells[6, 1].Value = "故障";
                    ws.Cells[6, 2].Value = Math.Round(MachineStateA.Down, 1);
                    ws.Cells[7, 1].Value = "上料机报警";
                    ws.Cells[7, 2].Value = Math.Round(MachineStateA.UploaderAlarm, 1);
                    ws.Cells[8, 1].Value = "机台运行";
                    ws.Cells[8, 2].Value = Math.Round(MachineStateA.Run, 1);

                    package.SaveAs(new FileInfo(filepath));
                }

            }
            catch (Exception ex)
            {
                AddMessage(ex.Message);
            }

        }
        private async void Run()
        {
            string alarmCode, _alarmCode = "-1";
            while (true)
            {
                try
                {
                    D300 = int.Parse(Inifile.INIGetStringValue(iniParameterPath, "Machine", "State", "-1"));
                    alarmCode = Inifile.INIGetStringValue(iniParameterPath, "AlarmCommand", "Code", "-1");
                    if (_alarmCode != alarmCode)
                    {
                        AlarmData alarmData = AlarmList.FirstOrDefault(s => s.Code == alarmCode);
                        if (alarmData != null)
                        {
                            AddMessage($"{alarmData.Code}:{alarmData.Content} 发生");

                            AlarmRecordViewModel newrow = new AlarmRecordViewModel
                            {
                                Time = DateTime.Now,
                                Code = alarmData.Code,
                                Content = alarmData.Content
                            };
                            AlarmRecord.Add(newrow);

                            string banci = GetBanci();
                            if (!File.Exists(System.IO.Path.Combine(@"D:\报警记录", "AlarmRecord" + banci + ".csv")))
                            {
                                string[] heads = new string[] { "时间", "报警代码", "报警内容" };
                                Csvfile.savetocsv(System.IO.Path.Combine(@"D:\报警记录", "AlarmRecord" + banci + ".csv"), heads);
                            }
                            string[] conts = new string[] { DateTime.Now.ToString(), alarmData.Code, alarmData.Content };
                            Csvfile.savetocsv(System.IO.Path.Combine(@"D:\报警记录", "AlarmRecord" + banci + ".csv"), conts);
                        }

                        _alarmCode = alarmCode;
                    }
                }
                catch (Exception ex) { AddMessage(ex.Message); }


                #region 换班
                if (LastBanci != GetBanci())
                {
                    try
                    {
                        WriteStatetoExcel(Path.Combine("D:\\报警记录", "时间统计" + LastBanci + ".xlsx"));
                        MachineStateA.Clean();
                        WriteToJson(MachineStateA, System.IO.Path.Combine(System.Environment.CurrentDirectory, "MachineStateA.json"));

                        LastBanci = GetBanci();
                        Inifile.INIWriteValue(iniParameterPath, "Summary", "LastBanci", LastBanci);
                        AddMessage(LastBanci + " 换班数据清零");
                    }
                    catch (Exception ex)
                    {
                        AddMessage(ex.Message);
                    }
                }
                #endregion
                await Task.Delay(100);
            }
        }
        #endregion
    }
}
