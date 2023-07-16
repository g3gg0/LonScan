using CrashReporterDotNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LonScan
{
    static class Program
    {
        private static ReportCrash _reportCrash;

        [STAThread]
        static void Main()
        {
            Application.ThreadException += (sender, args) => SendReport(args.Exception);
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                SendReport((Exception)args.ExceptionObject);
            };

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            _reportCrash = new ReportCrash("LonScan@g3gg0.de")
            {
                Silent = true,
                ShowScreenshotTab = true,
                IncludeScreenshot = false,
                AnalyzeWithDoctorDump = true,
                DoctorDumpSettings = new DoctorDumpSettings
                {
                    ApplicationID = new Guid("1205bba2-0d38-4351-bba1-8ca63993f8b6"),
                    OpenReportInBrowser = true
                }
            };
            _reportCrash.RetryFailedReports();

            Application.Run(new LonScannerMain());
        }
        public static void SendReport(Exception exception, string developerMessage = "")
        {
            _reportCrash.DeveloperMessage = developerMessage;
            _reportCrash.Silent = false;
            _reportCrash.Send(exception);
        }

        public static void SendReportSilently(Exception exception, string developerMessage = "")
        {
            _reportCrash.DeveloperMessage = developerMessage;
            _reportCrash.Silent = true;
            _reportCrash.Send(exception);
        }
    }
}
