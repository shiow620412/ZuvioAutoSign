using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.IO;
using System.Collections.Specialized;
using System.Web;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics.CodeAnalysis;

namespace ZuvioAutoSign
{
    public class Zuvio
    {
        private Timer t;
        public string account;
        public string password;
        private string AccessToken;
        private string UserID;
        private string Session;
        public int DelayTime=120;
        private string[] _week = new string[] { "一", "二", "三", "四", "五", "六", "日" };
        public string[,] schedule = new string[7,10];
        public Dictionary<string, string> Location;

        public Zuvio()
        {
            if (!File.Exists("Setting.json"))
            {

                account = Prompt.Input("請輸入帳號：");
                password = Prompt.Input("請輸入密碼：");
                
                Login();
                for (int i = 0; i < schedule.GetLength(0); i++)
                {
                    for (int j = 0; j < schedule.GetLength(1); j++)
                    {
                        schedule[i, j] = string.Empty;
                    }
                }
                Location = new Dictionary<string, string>();
                Prompt.Success("登入成功");
            }
            else
            {
                JObject json = JObject.Parse(File.ReadAllText("Setting.json"));
                account = json["account"].ToString();
                password = json["password"].ToString();
                Login();
                //AccessToken = json["AccessToken"].ToString();
                //UserID = json["UserID"].ToString();
                //Session = json["Session"].ToString();
                int.TryParse(json["DelayTime"].ToString(), out DelayTime);
                Location = JsonConvert.DeserializeObject<Dictionary<string, string>>(json["Location"].ToString());
                for (int i = 0; i < schedule.GetLength(0); i++)
                {
                    for(int j = 0; j < schedule.GetLength(1); j++)
                    {
                        schedule[i,j] = json["schedule"][i][j].ToString();
                    }
                }
                Prompt.Success("設定讀取成功");
            }

        }
        public List<Course> GetCourses()
        {
            JObject s = JObject.Parse(Request($"course/listStudentCurrentCourses?user_id={UserID}&accessToken={AccessToken}"));
            return JsonConvert.DeserializeObject<List<Course>>(s["courses"].ToString());
        }
        public void Start(List<Course> courses)
        {
           
            t = new Timer((e) =>
            {
                DateTime dt = DateTime.Now;
                //default 17
                if(dt.Hour >= 8 && dt.Hour <= 17)
                {
                    //default -8
                    if(schedule[dt.DayOfWeek.GetHashCode()-1,dt.Hour-8] != "")
                    {
                        string targetCourse = schedule[dt.DayOfWeek.GetHashCode() - 1, dt.Hour - 8];
                        string courseSignHtml = Request($"student5/irs/rollcall/{courses.Find(x=>x.course_name==targetCourse).course_id}");
                        System.Threading.Thread.Sleep(250);
                        try
                        {
                            //檢查是否簽到過
                            if (Regex.IsMatch(courseSignHtml, "<div class=\"i-r-f-b-disabled-button\">已簽到</div>"))
                                return;
                            string rollcall_id = getVariable(courseSignHtml, "rollcall_id", isNum: false);

                            Prompt.DateTimeOutput($"發現有進行中的點名({targetCourse})");
                            Prompt.DateTimeOutput("開始簽到");
                            Thread.Sleep(250);
                            string lat = string.Empty;
                            string lng = string.Empty;
                            if (Location.ContainsKey(targetCourse))
                            {
                                string[] split = Location[targetCourse].Split(",");
                                lng = split[0];//經度
                                lat = split[1];//緯度
                            }

                            Sign sign = SignUp(rollcall_id, lat: lat, lng: lng);
                            if (sign.msg == "OK")
                            {
                                Console.ForegroundColor = Prompt.successColor;
                                Prompt.DateTimeOutput("簽到結果:成功");                     
                            }
                            else
                            {
                                Console.ForegroundColor = Prompt.errorColor;
                                Prompt.DateTimeOutput("簽到結果:失敗");                          
                            }
                            Console.ForegroundColor = Prompt.queryColor;
                        }
                        catch(Exception ex)
                        {
                            if(ex.Message != "Can't Find rollcall_id value")
                                Prompt.Error(ex.ToString());
                        }
                                             
                    }
                }
            }, null, 0, DelayTime*1000);

        }
        public void Stop()
        {
            ManualResetEvent timerDisposed = new ManualResetEvent(false);
            t.Dispose(timerDisposed);
            timerDisposed.WaitOne();
            timerDisposed.Dispose();

        }
        public void ModifyLocation(string CourseName)
        {
            while(true)
            {
                Prompt._Prompt("輸入格式:經度,緯度");
                string input = Prompt.Input("請輸入:");
                Match mh = Regex.Match(input, "[0-9]+.[0-9]*,[0-9]+.[0-9]*");
                if (mh.Success)
                {
                    Location[CourseName] = mh.Groups[0].Value;
                    Prompt.Success($"{CourseName}定位地點 => {input}");
                    break;
                }
                else
                {
                    Prompt.Error(Prompt.FormatError);

                }
            }
           
        }
        public void ModifySchedule(string CourseName)
        {
            PrintSchedule(CourseName);
            Prompt._Prompt("輸入格式:加入or刪除(I or D) 星期幾(1-7) 第幾節課(1-10)");
            Prompt._Prompt("註:第5節為中午12:00");
            Prompt._Prompt("註:格式錯誤將自動退出設定");
            while (true)
            {
                string input = Prompt.Input("請輸入:");
                Match mh = Regex.Match(input, "([ID]) ([1-7]) ([0-9]{1,2})");
                if (mh.Success)
                {
                    int week = int.Parse(mh.Groups[2].Value);
                    int courseTime = int.Parse(mh.Groups[3].Value);
                    string mode = mh.Groups[1].Value;
                    if (mode == "I")
                    {
                        schedule[week - 1, courseTime - 1] = CourseName;
                        Prompt.Success($"{CourseName}上課時間 => 星期{_week[week - 1]} 第{courseTime}節({courseTime + 7}:00~{courseTime + 8}:00)");
                    }
                    else if (mode == "D")
                    {
                        schedule[week - 1, courseTime - 1] = "";
                        Prompt.Success($"刪除 {CourseName}的星期{_week[week - 1]} 第{courseTime}節({courseTime + 7}:00~{courseTime + 8}:00)");
                    }

                }
                else
                {
                    Prompt.Error("格式錯誤 退出設定！");
                    break;
                }
            }
       

        }
        private void PrintSchedule(string CourseName)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Prompt.SplitLine();
            Prompt.Query(CourseName);
            Prompt.SplitLine();
            bool find = false;
            for(int i = 0; i < schedule.GetLength(0); i++)
            {
                for(int j = 0; j < schedule.GetLength(1); j++)
                {
                    if(schedule[i,j] == CourseName)
                    {
                        find = true;
                        Prompt.Query($"星期{_week[i]} 第{j+1}節({j + 8}:00~{j + 9}:00)");
                    }
                }
            }
            if (!find)
            {
                Prompt.Query("此課程尚未設定任何時間");
            }
            Prompt.SplitLine();
        }
        private string getVariable(string html,string varName,bool isNum = true)
        {
            
            string pattern = "([0-9]+)";
            if (!isNum)
                pattern = "[\"\']([a-zA-Z0-9]+)[\"\']";
            Match mh = Regex.Match(html, $"var {varName} = {pattern}");
            if (mh.Success)
            {
                return mh.Groups[1].Value;
            }
            else
            {
                throw new Exception($"Can't Find {varName} value");
            }
        }
        private void Login()
        {
            
            string url = "https://irs.zuvio.com.tw/irs/submitLogin";
            NameValueCollection param = HttpUtility.ParseQueryString(string.Empty);
            param.Add("email", account);
            param.Add("password", password);
            param.Add("current_language", "zh-TW");
            byte[] b = Encoding.ASCII.GetBytes(param.ToString());
   
            HttpWebRequest web = WebRequest.Create(url) as HttpWebRequest ;
                        
         
            web.Method = "POST";
            web.ContentType = "application/x-www-form-urlencoded";            
            //不設置cookie container 會無法登入
            web.CookieContainer = new CookieContainer();
          

            web.ContentLength = b.Length;
            using (Stream stream = web.GetRequestStream())
            {                             
                stream.Write(b, 0, b.Length);               
            }
         
          
            using (HttpWebResponse response = web.GetResponse() as HttpWebResponse) 
            using(Stream stream = response.GetResponseStream())
            using(StreamReader sr = new StreamReader(stream))
            {
                try
                {
                    string html = sr.ReadToEnd();
                    AccessToken = getVariable(html, "accessToken", isNum: false);
                    UserID = getVariable(html, "user_id");
                    Session = response.Cookies[0].Value;
                }catch(Exception ex)
                {
                    Prompt.Error("帳號or密碼錯誤 請重新輸入");
                    new Zuvio();
                }
              
            }
            

        }

        /// <summary>
        /// Execute SignUp
        /// </summary>
        /// <param name="RollcallID"></param>
        /// <param name="lat">緯度</param>
        /// <param name="lng">經度</param>
        /// <returns>Sign Object</returns>
        private Sign SignUp(string RollcallID,string lat="",string lng="")
        {
            HttpWebRequest request = WebRequest.CreateHttp("https://irs.zuvio.com.tw/app_v2/makeRollcall");
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            byte[] data = Encoding.UTF8.GetBytes($"user_id={UserID}&accessToken={AccessToken}&rollcall_id={RollcallID}&device=WEB&lat={lat}&lng={lng}");
            request.ContentLength = data.Length;
            using (Stream reqStream = request.GetRequestStream())
            {
                reqStream.Write(data, 0, data.Length);
               
            }
            
            string response = new StreamReader(request.GetResponse().GetResponseStream()).ReadToEnd();
            return JsonConvert.DeserializeObject<Sign>(response);
            
        }
        private string Request(string path, string type = "GET")
        {
            HttpWebRequest web = WebRequest.CreateHttp($"https://irs.zuvio.com.tw/{path}");
            web.Method = type;
            
            web.Headers.Add(HttpRequestHeader.Cookie, $"PHPSESSID={Session};");


            return new StreamReader(web.GetResponse().GetResponseStream()).ReadToEnd();

        }
    
    }
    public class Course
    {
        public string semester_id;
        public string semester_name;
        public string teacher_name;
        public string course_id;
        public string course_name;
        public int course_unread_num;
        public string course_created_at;
        public bool pinned;
        public string is_special_course;
        

    }
    public class Sign
    {
        public bool status;
        public JObject ad;
        public string star_suggest;
        public string msg;
    }
}
