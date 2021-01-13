using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Text.RegularExpressions;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Timers;

namespace ZuvioAutoSign
{

    class Program
    {
        
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.Title = "Zuvio 自動點名系統 Ver1.2 by.Wind";
            Zuvio zuvio = new Zuvio();                                 
            List<Course> courses = zuvio.GetCourses();
            string select = string.Empty;
            while (true )
            {
                Prompt._Prompt("輸入格式：編號");
                Prompt._Prompt("0.開啟自動簽到");
                Prompt._Prompt($"1.設定檢查時間(Current DelayTime:{zuvio.DelayTime}s)");
                Prompt._Prompt("↓↓以下為設定課程時間↓↓");
                Prompt._Prompt("輸入格式：編號 設定時間orGPS定位地點(T or G)");
                int num = 1;
                courses.ForEach(x =>
                {
                    num++;
                    Prompt._Prompt($"{num}.{x.course_name}");
                });
                
                select = Prompt.Input("請輸入：");
                Console.ForegroundColor = ConsoleColor.White;
                Prompt.SplitLine();
                if (select == "0")
                {
                    break;
                }
                else if (select == "1")
                {
                    while (!int.TryParse(Prompt.Input("請輸入檢查時間(單位:秒)："), out zuvio.DelayTime))
                    {
                        Prompt.Error(Prompt.FormatError);
                    }
                }
                else
                {
                    Match mh = Regex.Match(select, "([0-9]) ([TG])", RegexOptions.IgnoreCase);
                    if (mh.Success)
                    {
                        string mode = mh.Groups[2].Value.ToUpper();
                        string courseName = courses[int.Parse(mh.Groups[1].Value) - 2].course_name;
                        if (mode == "T")
                        {
                            zuvio.ModifySchedule(courseName);
                        }else if (mode == "G")
                        {
                            zuvio.ModifyLocation(courseName);
                        }
                    }
                    else
                    {
                        Prompt.Error(Prompt.FormatError);
                    }
                }
                Console.ForegroundColor = ConsoleColor.White;
                Prompt.SplitLine();
            }

            File.WriteAllText("Setting.json", JsonConvert.SerializeObject(zuvio));
            Prompt.DateTimeOutput("Zuvio 自動點名啟動");
            Prompt.DateTimeOutput("按下任意鍵結束程式");
            zuvio.Start(courses);
            Console.Read();
  
           
            
        }
    }
}
