using System;
using System.Collections.Generic;
using System.Text;

namespace ZuvioAutoSign
{
    public class Prompt
    {
        public const string FormatError = "輸入格式錯誤 請重新輸入";
        public const ConsoleColor queryColor = ConsoleColor.White;
        public const ConsoleColor promptColor = ConsoleColor.Cyan;
        public const ConsoleColor errorColor = ConsoleColor.Red;
        public const ConsoleColor successColor = ConsoleColor.Green;
        public const ConsoleColor inputColor = ConsoleColor.Yellow;
        public static void SplitLine() { Console.WriteLine("================================"); }
        public static void DateTimeOutput(string s)
        {
            Console.WriteLine($"{DateTime.Now.ToString("g")} {s}");
        }
        public static void Query(string result)
        {
            Console.ForegroundColor =queryColor;
            Console.WriteLine(result);
        }
        public static void _Prompt(string s)
        {
            Console.ForegroundColor = promptColor;
            Console.WriteLine(s);
            
        }
        public static void Error(string s)
        {
            Console.ForegroundColor = errorColor;
            Console.WriteLine($"[Warning]{s}");
            
        }
        public static void Success(string s)
        {
            Console.ForegroundColor = successColor;
            Console.WriteLine($"[Success]{s}");
            
        }
        public static string Input(string s)
        {
            Console.ForegroundColor = inputColor;
            Console.Write(s);
            
            return Console.ReadLine();
        }
    }
}
