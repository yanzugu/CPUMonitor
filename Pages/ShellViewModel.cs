using Stylet;
using System;
using System.Windows;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace CPUMonitor.Pages
{
    public class ShellViewModel : Screen
    {
        public double CpuUsage { get; set; }
        public double MemoryUsage { get; set; }
        public string AvailableMemory { get; set; }
        public string TotalMemory { get; set; }
        public string UsedMemory { get; set; }

        private readonly PerformanceCounter cpuUsageCounter;
        private readonly ulong totalPhys = GetTotalPhys();
        private Queue<double> cpuUsageQueue = new Queue<double>();
        private Queue<double> memoryUsageQueue = new Queue<double>();

        public ShellViewModel()
        {
            cpuUsageCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");

            var dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += CpuTick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();

            dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += MemoryTick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();

            TotalMemory = FormatSize(totalPhys);

            Task.Factory.StartNew(ProcessUpdateCpuUsage, TaskCreationOptions.LongRunning);
            Task.Factory.StartNew(ProcessUpdateMemoryUsage, TaskCreationOptions.LongRunning);
        }

        private void CpuTick(object sender, EventArgs e)
        {
            if (CpuUsage.AlmostEqual(0))
            {
                CpuUsage = cpuUsageCounter.NextValue();
            }
            else
            {
                cpuUsageQueue.Enqueue(cpuUsageCounter.NextValue());
            }
        }

        private void MemoryTick(object sender, EventArgs e)
        {
            double nextValue = (double)GetUsedPhys() / (double)totalPhys * 100;
            AvailableMemory = FormatSize(GetAvailPhys());
            UsedMemory = FormatSize(GetUsedPhys(), false);

            if (MemoryUsage.AlmostEqual(0))
            {
                MemoryUsage = nextValue;
            }
            else
            {
                memoryUsageQueue.Enqueue(nextValue);
            }
        }

        private void ProcessUpdateCpuUsage()
        {
            while (true)
            {
                SpinWait.SpinUntil(() => cpuUsageQueue.Count != 0);

                double nextValue = cpuUsageQueue.Dequeue();
                double interval = nextValue >= CpuUsage ? 0.05d : -0.05d;

                while (CpuUsage.AlmostEqual(nextValue) == false)
                {
                    CpuUsage += interval;
                    Thread.Sleep(5);
                }
            }
        }

        private void ProcessUpdateMemoryUsage()
        {
            while (true)
            {
                SpinWait.SpinUntil(() => memoryUsageQueue.Count != 0);

                double nextValue = memoryUsageQueue.Dequeue();
                double interval = nextValue >= MemoryUsage ? 0.05d : -0.05d;

                while (MemoryUsage.AlmostEqual(nextValue) == false)
                {
                    MemoryUsage += interval;
                    Thread.Sleep(5);
                }
            }
        }


        #region Obtain memory information API
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx(ref MEMORY_INFO mi);

        //Define the information structure of memory
        [StructLayout(LayoutKind.Sequential)]
        private struct MEMORY_INFO
        {
            public uint dwLength; //Current structure size
            public uint dwMemoryLoad; //Current memory utilization
            public ulong ullTotalPhys; //Total physical memory size
            public ulong ullAvailPhys; //Available physical memory size
            public ulong ullTotalPageFile; //Total Exchange File Size
            public ulong ullAvailPageFile; //Total Exchange File Size
            public ulong ullTotalVirtual; //Total virtual memory size
            public ulong ullAvailVirtual; //Available virtual memory size
            public ulong ullAvailExtendedVirtual; //Keep this value always zero
        }
        #endregion

        #region Formatting capacity size
        /// <summary>
        /// Formatting capacity size
        /// </summary>
        /// <param name="size">Capacity ( B)</param>
        /// <returns>Formatted capacity</returns>
        private static string FormatSize(double size, bool showUnit = true)
        {
            double d = (double)size;
            int i = 0;
            while ((d > 1024) && (i < 5))
            {
                d /= 1024;
                i++;
            }
            string[] unit = { "B", "KB", "MB", "GB", "TB" };

            if (showUnit)
            {
                return string.Format("{0:0.0} {1}", d, unit[i]);
            }
            else
            {
                return string.Format("{0:0.0}", d);
            }
        }
        #endregion

        #region Get the current memory usage
        /// <summary>
        /// Get the current memory usage
        /// </summary>
        /// <returns></returns>
        private static MEMORY_INFO GetMemoryStatus()
        {
            MEMORY_INFO mi = new MEMORY_INFO();
            mi.dwLength = (uint)System.Runtime.InteropServices.Marshal.SizeOf(mi);
            GlobalMemoryStatusEx(ref mi);
            return mi;
        }
        #endregion

        #region Get the current available physical memory size
        /// <summary>
        /// Get the current available physical memory size
        /// </summary>
        /// <returns>Current available physical memory( B)</returns>
        private static ulong GetAvailPhys()
        {
            MEMORY_INFO mi = GetMemoryStatus();
            return mi.ullAvailPhys;
        }
        #endregion

        #region Get the current memory size used
        /// <summary>
        /// Get the current memory size used
        /// </summary>
        /// <returns>Memory size used( B)</returns>
        private static ulong GetUsedPhys()
        {
            MEMORY_INFO mi = GetMemoryStatus();
            return (mi.ullTotalPhys - mi.ullAvailPhys);
        }
        #endregion

        #region Get the current total physical memory size
        /// <summary>
        /// Get the current total physical memory size
        /// </summary>
        /// <returns&amp;gt;Total physical memory size( B)&amp;lt;/returns&amp;gt;
        private static ulong GetTotalPhys()
        {
            MEMORY_INFO mi = GetMemoryStatus();
            return mi.ullTotalPhys;
        }
        #endregion
    }
}
