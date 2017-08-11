using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantLab.DataManager.Check.Factor
{
    class DataClass
    {
    }
    //class FactorInformation
    //{
    //    //public long FactorId { get; set; }
    //    public string FactorName { get; set; }
    //    public string FactorField { get; set; }
    //    public string Quarter { get; set; }
    //    public string Months { get; set; }
    //    public string Days { get; set; }
    //    public string StartTime { get; set; }
    //    public string EndTime { get; set; }
    //}
    class CodeClass
    {
        public long SecurityId { get; set; }
        public string SecurityCode { get; set; }
        public DateTime InceptionDate { get; set; }
        public DateTime ExpirationDate { get; set; }
    }
    class CodeFactor
    {
        public DateTime ValueDate { get; set; }
        public long SecurityId { get; set; }
        //public string SecurityCode { get; set; }
        //public long count { get; set; }
        //public DateTime InceptionDate { get; set; }
        //public DateTime ExpirationDate { get; set; }
    }
    class CodeRecordInformation
    {
        public string FactorName { get; set; }
        public string FactorField { get; set; }
        public string SecurityCode { get; set; }
        public string Record { get; set; }
        public string NoRecord { get; set; }
        public DateTime InceptionDate { get; set; }
        public DateTime ExpirationDate { get; set; }

        public Dictionary<string, string> ToDic()
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("FactorName", FactorName);
            dic.Add("FactorField", FactorField);
            dic.Add("SecurityCode", SecurityCode);
            dic.Add("Record", Record);
            dic.Add("NoRecord", NoRecord);
            dic.Add("InceptionDate", InceptionDate.ToString("yyyy-MM-dd"));
            dic.Add("ExpirationDate", ExpirationDate.ToString("yyyy-MM-dd"));
            return dic;

        }
    }

}
