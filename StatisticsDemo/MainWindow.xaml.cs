﻿using MahApps.Metro.Controls;
using StatisticsDemo.Model;
using StatisticsDemo.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace StatisticsDemo
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new MainWindowViewModel();
        }

        private async void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Metro metro = new Metro();
            metro.ChangeAccent("Dark.Red");
            bool r = await metro.ShowConfirm("确认", "你确定关闭软件吗?");
            if (!r)
            {
                metro.ChangeAccent("Light.Blue");
            }
            else
            {
                System.Windows.Application.Current.Shutdown();
            }
        }
    }
}
