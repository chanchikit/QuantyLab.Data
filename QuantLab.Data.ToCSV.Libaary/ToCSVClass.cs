using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using QuantLab.Data.DoFiliter;

namespace QuantLab.Data.ToCSV.Libaary
{
    public class ToCSVClass
    {
        public static void ToCSV(string filepath, List<Dictionary<string, string>> lisrDic, Boolean ba, Boolean fileexist)
        {
            try
            {
                if (lisrDic.First() != null)
                {
                    string head = string.Join(",", lisrDic.First().Keys.ToList());
                    string data = "";
                    FileInfo fi = new FileInfo(filepath);
                    if (!fi.Directory.Exists)
                    {
                        fi.Directory.Create();
                    }
                    using (StreamWriter sw2 = new StreamWriter(filepath, ba, Encoding.Default))
                    {
                        if (fileexist == false)
                        {
                            sw2.WriteLine(head);
                        }
                        foreach (var a in lisrDic)
                        {
                            data = string.Join(",", DoFiliterClass.DoFiliter(a.Values.ToList()));
                            sw2.WriteLine(data);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

    }

}


