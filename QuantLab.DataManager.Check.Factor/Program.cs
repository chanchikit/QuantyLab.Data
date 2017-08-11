using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using System.Data;
using System.IO;
using System.Data.SqlClient;
using System.Configuration;
using System.Threading;
using QuantLab.Data.DoFiliter;
using QuantLab.Data.ToCSV.Libaary;
using QuantLab.Data.SeparateTime.Library;
using QuantLab.Data.TxtToList.Library;
using System.Collections.Concurrent;

namespace QuantLab.DataManager.Check.Factor
{
    class Program
    {
        //static string connectString = "Data Source=192.168.199.150;Initial Catalog=QuantLab.Data;Persist Security Info=True;User ID=sa;Password=zDpcj$qWT!d$aM7Xfg ;Connection Timeout=500";
        static string connectString = "Data Source=192.168.199.222;Initial Catalog=QuantLab.Data;Persist Security Info=True;User ID=supwintest1;Password=supwin.com888";
        //查询某股的信息FindCodes(股票securitytype, IDbConnection _conn)，返回list<Asharescode>
        public static List<CodeClass> FindCodes(string _securitytype)
        {
            List<CodeClass> codelist = new List<CodeClass>();
            using (IDbConnection conn = new SqlConnection(connectString))
            {
                codelist = conn.Query<CodeClass>("SELECT * FROM SecurityMaster WHERE SecurityTypeGroupId = (SELECT SecurityTypeGroupId FROM  SecurityTypeGroup WHERE SecurityType = @SecurityType) ORDER BY SecurityCode", new { SecurityType = _securitytype }).ToList();
                //codelist.ForEach(x => Console.WriteLine(x.SecurityCode));
            }
            return codelist;
        }


        public static async Task<List<CodeFactor>> DoQueryAsync(IDbConnection conn,string sqlString)
        {
            var result = await conn.QueryAsync<CodeFactor>(sqlString);
            return result.ToList(); 

        }

        //处理日频因子
        public static void DoAnalysConnectedDays(string ValueType, string FactorName, string FactorField, List<CodeClass> listAllCodes, Dictionary<string, List<DateTime>> allExchangeTimeDic)
        {
            using (IDbConnection conn = new SqlConnection(connectString))
            {
                conn.Open();
                foreach (var item in allExchangeTimeDic)//遍历每一年
                {
                    List<CodeRecordInformation> listResult = new List<CodeRecordInformation>();
                    List<CodeRecordInformation> listNullResult = new List<CodeRecordInformation>();

                    //ConcurrentBag<CodeRecordInformation> listResult = new ConcurrentBag<CodeRecordInformation>();
                    //ConcurrentBag<CodeRecordInformation> listNullResult = new ConcurrentBag<CodeRecordInformation>();
                    List<Dictionary<string, string>> listDic = new List<Dictionary<string, string>>();
                    try
                    {
                        string query = string.Format("SELECT SecurityId,ValueDate FROM FactorValue WHERE ValueDate>= '{0}'AND ValueDate < '{1}' AND FactorValue.FactorId = (select FactorId from factor where factorfield = '{2}') AND FactorValue.ValueType = '{3}' AND SecurityId in (SELECT SecurityId FROM SecurityMaster WHERE SecurityTypeGroupId = (SELECT SecurityTypeGroupId FROM SecurityTypeGroup WHERE SecurityType = 'ashares'))", item.Key, (Convert.ToInt32(item.Key) + 1), FactorField, ValueType);
                        //Console.WriteLine(query);
                        //var allFactorValue = conn.Query<CodeFactor>(query).GroupBy(x => x.SecurityId).ToDictionary(x => x.Key, x => x.ToList());
                        var allFactorValue=DoQueryAsync(conn,query).Result.GroupBy(x => x.SecurityId).ToDictionary(x => x.Key, x => x.ToList());
                        var allOnMarketCodes = listAllCodes.Where(x => x.InceptionDate.Date <= item.Value.Last().Date && x.ExpirationDate.Date >= item.Value.First().Date).ToList();//所有上市的股票
                        Console.WriteLine("{0}上市：{1}", item.Key, allOnMarketCodes.Count);
                        //int num = 0;
                        for(int i=0;i<allOnMarketCodes.Count;i++)
                        {                        
                           if (allFactorValue.ContainsKey(allOnMarketCodes[i].SecurityId))//上市有记录的Code
                            {
                                //num++;
                                string existStart = "";
                                string existEnd = "";
                                string notExistStart = "";
                                string notExistEnd = "";
                                Boolean basis = true;//exist代表上一个是有记录的时间
                                StringBuilder recordstr = new StringBuilder();
                                StringBuilder norecordstr = new StringBuilder();
                                var newCodesFactorValue = allFactorValue[allOnMarketCodes[i].SecurityId].ToList();//股票的factorvalue
                                var newExchangeDays = item.Value.Where(x => x.Date >= allOnMarketCodes[i].InceptionDate && x.Date <= allOnMarketCodes[i].ExpirationDate).ToList();//该股票在这一年中的交易时间（已去除非上市的交易日）
                                for (int j = 0; j < newExchangeDays.Count; j++)
                                {
                                    if (newCodesFactorValue.Where(x => x.ValueDate.Date == newExchangeDays[j].Date).Count() > 0)
                                    {
                                        if (existStart.Length == 0)
                                        {
                                            existStart = newExchangeDays[j].ToShortDateString();
                                            if (basis == false)
                                            {
                                                if (notExistEnd.Length == 0)
                                                {
                                                    norecordstr.Append(notExistStart + "|");
                                                }
                                                else
                                                {
                                                    norecordstr.Append(notExistStart + "--" + notExistEnd + "|");
                                                }
                                                notExistEnd = "";
                                                notExistStart = "";
                                            }
                                            basis = true;
                                        }
                                        else
                                        {
                                            existEnd = newExchangeDays[j].ToShortDateString();
                                            if (basis == false)
                                            {
                                                if (notExistEnd.Length <= 0 && notExistStart.Length > 0)
                                                {
                                                    norecordstr.Append(notExistStart + "|");
                                                }
                                                if (notExistStart.Length > 0 && notExistEnd.Length > 0)
                                                {
                                                    norecordstr.Append(notExistStart + "--" + notExistEnd + "|");
                                                }
                                                notExistEnd = "";
                                                notExistStart = "";
                                            }
                                            basis = true;
                                        }
                                    }
                                    else
                                    {
                                        if (notExistStart.Length == 0)
                                        {
                                            notExistStart = newExchangeDays[j].ToShortDateString();
                                            if (basis == true)
                                            {
                                                if (existEnd.Length <= 0 && existStart.Length > 0)
                                                {
                                                    recordstr.Append(existStart + "|");
                                                }
                                                if (existStart.Length > 0 && existEnd.Length > 0)
                                                {
                                                    recordstr.Append(existStart + "--" + existEnd + "|");
                                                }
                                                existStart = "";
                                                existEnd = "";
                                            }
                                            basis = false;
                                        }
                                        else
                                        {
                                            notExistEnd = newExchangeDays[j].ToShortDateString();
                                            if (basis == true)
                                            {
                                                if (existEnd.Length == 0)
                                                {
                                                    recordstr.Append(existStart + "|");
                                                }
                                                else
                                                {
                                                    recordstr.Append(existStart + "--" + existEnd + "|");
                                                }
                                                existStart = "";
                                                existEnd = "";
                                            }
                                            basis = false;
                                        }
                                    }
                                }
                                if (existStart.Length > 0 && existEnd.Length > 0)
                                {
                                    recordstr.Append(existStart + "--" + existEnd + "|");
                                }
                                if (notExistEnd.Length > 0 && notExistStart.Length > 0)
                                {
                                    norecordstr.Append(notExistStart + "--" + notExistEnd + "|");
                                }
                                //Console.ReadKey();                              

                                if (recordstr.Length > 0)
                                {
                                    recordstr.Remove(recordstr.Length - 1, 1);
                                }
                                if (norecordstr.Length > 0)
                                {
                                    norecordstr.Remove(norecordstr.Length - 1, 1);
                                    listResult.Add(new CodeRecordInformation { FactorName = FactorName, FactorField = FactorField, SecurityCode = allOnMarketCodes[i].SecurityCode, Record = recordstr.ToString(), NoRecord = norecordstr.ToString(), InceptionDate = allOnMarketCodes[i].InceptionDate, ExpirationDate = allOnMarketCodes[i].ExpirationDate });
                                }
                                if (norecordstr.Length == 0 &&recordstr.Length>0)
                                {
                                    listNullResult.Add(new CodeRecordInformation { FactorName = FactorName, FactorField = FactorField, SecurityCode = allOnMarketCodes[i].SecurityCode, Record = recordstr.ToString(), NoRecord = norecordstr.ToString(), InceptionDate = allOnMarketCodes[i].InceptionDate, ExpirationDate = allOnMarketCodes[i].ExpirationDate });
                                }
                            }
                            else//上市没记录的Code，添加到结果list中
                            {
                                DateTime start = allOnMarketCodes[i].InceptionDate;
                                DateTime end = allOnMarketCodes[i].ExpirationDate;
                                if (start.Date< item.Value.First().Date)
                                {
                                    start = item.Value.First().Date;
                                }
                                if (end.Date> item.Value.Last().Date)
                                {
                                    end = item.Value.Last().Date;
                                }
                                listResult.Add(new CodeRecordInformation { FactorName=FactorName,FactorField=FactorField,SecurityCode= allOnMarketCodes[i].SecurityCode,NoRecord=string.Format("{0}--{1}",start.ToShortDateString(),end.ToShortDateString()),InceptionDate= allOnMarketCodes[i].InceptionDate, ExpirationDate= allOnMarketCodes[i].ExpirationDate });
                            }
                        }//所有上市股票
                        if (listResult.Count == 0 && listNullResult.Count > 0)
                        {
                            listResult = listNullResult;
                        }
                        foreach (var cc in listResult)
                        {
                            Dictionary<string, string> dic = cc.ToDic();
                            listDic.Add(dic);
                        }
                        Boolean fileexist = true;
                        string filepath = string.Format(ConfigurationManager.AppSettings["csvpath"], ValueType, DoFiliterClass.DoFiliter(FactorName), item.Key);
                        if (File.Exists(filepath) == false)
                        {
                            fileexist = false;
                        }
                        ToCSVClass.ToCSV(filepath, listDic, true, fileexist);
                        Console.WriteLine("factor:{0},year:{1}--done", FactorName, item.Key);

                    }

                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message+e.StackTrace);
                    }
                }
            }
        }

        public static void DoAnalysSeparatedDays(string ValueType, string FactorName, string FactorField, List<CodeClass> listAllCodes, Dictionary<string, List<DateTime>> allExchangeTimeDic)
        {
            
            using (IDbConnection conn = new SqlConnection(connectString))
            {
                conn.Open();
                
                foreach (var item in allExchangeTimeDic)//遍历每一年
                {
                    //List<CodeRecordInformation> listResult = new List<CodeRecordInformation>();
                    ConcurrentBag<CodeRecordInformation> listResult = new ConcurrentBag<CodeRecordInformation>();
                    ConcurrentBag<CodeRecordInformation> listNullResult = new ConcurrentBag<CodeRecordInformation>();
                    //List<CodeRecordInformation> listNullResult = new List<CodeRecordInformation>();
                    List<Dictionary<string, string>> listDic = new List<Dictionary<string, string>>();
                    try
                    {
                        
                        string query = string.Format("SELECT SecurityId,ValueDate FROM FactorValue WHERE ValueDate>= '{0}'AND ValueDate < '{1}' AND FactorValue.FactorId = (select FactorId from factor where factorfield = '{2}') AND FactorValue.ValueType = '{3}' AND SecurityId in (SELECT SecurityId FROM SecurityMaster WHERE SecurityTypeGroupId = (SELECT SecurityTypeGroupId FROM SecurityTypeGroup WHERE SecurityType = 'ashares'))", item.Key, (Convert.ToInt32(item.Key) + 1), FactorField, ValueType);
                        var allFactorValue = conn.Query<CodeFactor>(query).GroupBy(x => x.SecurityId).ToDictionary(x => x.Key, x => x.ToList());
                        //Console.WriteLine(FactorField+"--"+item.Key+"--"+allFactorValue.Count+"有记录");
                        var allOnMarketCodes = listAllCodes.Where(x => x.InceptionDate.Date <= item.Value.Last().Date && x.ExpirationDate.Date >= item.Value.First().Date).ToList();//所有上市的股票
                        //Console.WriteLine("{0}上市：{1}", item.Key, allOnMarketCodes.Count);
                        //int num = 0;
                        Parallel.ForEach(allOnMarketCodes, i =>//所有上市股票
                        {
                            if (allFactorValue.ContainsKey(i.SecurityId))//上市有记录的Code
                            {

                                //num++;
                                StringBuilder recordstr = new StringBuilder();
                                StringBuilder norecordstr = new StringBuilder();
                                var newCodesFactorValue = allFactorValue[i.SecurityId].ToList();//通过key=securityid取得一只股票的factorvalue
                                var newExchangeDays = item.Value.Where(x => x.Date >= i.InceptionDate && x.Date <= i.ExpirationDate).ToList();//该股票在这一年中的交易时间（已去除非上市的交易日）
                                for (int j = 0; j < newExchangeDays.Count; j++)
                                {
                                    if (newCodesFactorValue.Where(x => x.ValueDate.Date == newExchangeDays[j].Date).Count() > 0)
                                    {
                                        recordstr.Append(newExchangeDays[j].ToShortDateString()+"|");
                                    }
                                    else
                                    {
                                        norecordstr.Append(newExchangeDays[j].ToShortDateString() + "|"); 
                                    }
                                }
                               
                                if (recordstr.Length > 0)
                                {
                                    recordstr.Remove(recordstr.Length - 1, 1);
                                }
                                if (norecordstr.Length > 0)
                                {
                                    norecordstr.Remove(norecordstr.Length - 1, 1);
                                }
                                if (norecordstr.Length > 0)
                                {
                                    listResult.Add(new CodeRecordInformation { FactorName = FactorName, FactorField = FactorField, SecurityCode = i.SecurityCode, Record = recordstr.ToString(), NoRecord = norecordstr.ToString(), InceptionDate = i.InceptionDate, ExpirationDate = i.ExpirationDate });
                                }
                                if (norecordstr.Length == 0 && recordstr.Length > 0)
                                {
                                    listNullResult.Add(new CodeRecordInformation { FactorName = FactorName, FactorField = FactorField, SecurityCode = i.SecurityCode, Record = recordstr.ToString(), NoRecord = norecordstr.ToString(), InceptionDate = i.InceptionDate, ExpirationDate = i.ExpirationDate });
                                }
                            }
                            else//上市没记录的Code，添加到结果list中
                            {

                                var listOneCodeTime = item.Value.Where(x => x.Date >= i.InceptionDate && x.Date <= i.ExpirationDate).ToList();
                                List<string> listOneCodeResult = new List<string>();
                                listOneCodeTime.ForEach(x => listOneCodeResult.Add(x.ToShortDateString()));
                                string oneCodeTimeStr = string.Join("|", listOneCodeResult);
                                listResult.Add(new CodeRecordInformation { FactorName = FactorName, FactorField = FactorField, SecurityCode = i.SecurityCode, NoRecord = oneCodeTimeStr, InceptionDate = i.InceptionDate, ExpirationDate = i.ExpirationDate });
                            }
                        });
                        if (listResult.Count == 0&&listNullResult.Count>0)
                        {
                            listResult = listNullResult;
                        }
                        
                        foreach (var cc in listResult.AsParallel().OrderBy(x=>x.SecurityCode))
                        {
                            Dictionary<string, string> dic = cc.ToDic();
                            listDic.Add(dic);
                        }
                        Boolean fileexist = true;
                        string filepath = string.Format(ConfigurationManager.AppSettings["csvpath"], ValueType, DoFiliterClass.DoFiliter(FactorName), item.Key);
                        if (File.Exists(filepath) == false)
                        {
                            fileexist = false;
                        }
                        ToCSVClass.ToCSV(filepath, listDic, true, fileexist);
                        Console.WriteLine("factor:{0},year:{1}--done", FactorName, item.Key);

                    }

                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }
        }

        static void Main(string[] args)
        {
            List<TxtListToListClass.FactorInformation> factorList = new List<TxtListToListClass.FactorInformation>();
            List<CodeClass> listAllCodes = new List<CodeClass>();
            try
            {
                factorList = TxtListToListClass.TxtToList((ConfigurationManager.AppSettings["txtpath"]));
                listAllCodes = FindCodes(ConfigurationManager.AppSettings["SecurityType"]);
                //Console.WriteLine(listAllCodes.Count);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            Console.WriteLine(factorList.Count);
            // factorList.ForEach(x => Console.WriteLine(x.EndTime));
            try
            {
                for (int i =1; i < factorList.Count; i++)
                {
                    if (factorList[i].Quarter == "1")
                    {
                        var allQuarter = SeparateTimeClass.ToQuarter(DateTime.Parse(factorList[i].StartTime), DateTime.Parse(factorList[i].EndTime)).GroupBy(x => x.Date.ToString("yyyy")).ToDictionary(x => x.Key, x => x.ToList());
                        Thread a = new Thread(() => DoAnalysSeparatedDays("q", factorList[i].FactorName, factorList[i].FactorField, listAllCodes, allQuarter));
                        a.Start();
                    }
                    if (factorList[i].Months == "1")
                    {
                        var exchangeMonths = TxtListToListClass.MonthsTxtToList(ConfigurationManager.AppSettings["AllExchangeMonths"]).GroupBy(x => x.Date.ToString("yyyy")).ToDictionary(x => x.Key, x => x.ToList());
                        Thread a = new Thread(() => DoAnalysSeparatedDays("m", factorList[i].FactorName, factorList[i].FactorField, listAllCodes, exchangeMonths));
                        a.Start();

                    }
                    if (factorList[i].Days == "1")
                    {
                        var allExchangeDays = SeparateTimeClass.ToAllExchangeDays(DateTime.Parse(factorList[i].StartTime), DateTime.Parse(factorList[i].EndTime), connectString).GroupBy(x => x.Date.ToString("yyyy")).ToDictionary(x => x.Key, x => x.ToList());
                        Thread a=new Thread(()=> DoAnalysConnectedDays("d", factorList[i].FactorName, factorList[i].FactorField, listAllCodes, allExchangeDays));
                        a.Start();
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
