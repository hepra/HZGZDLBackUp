using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace GZDL_DEV.model
{
   public class class_AnalysisShow
    {
       public class_AnalysisShow(string field_name, string Aphase_value)
       {
           Field_name = field_name;
           Bphase_Value = "0";
           Aphase_Value = Aphase_value;
           Cphase_Value = "0";
       }
       public class_AnalysisShow()
       {
       }

       public class_AnalysisShow(string field_name, string Aphase_value, string Bphase_value, string Cphase_value)
       {
           Field_name = field_name;
           Bphase_Value = Bphase_value;
           Aphase_Value = Aphase_value;
           Cphase_Value = Cphase_value;
       }
       public string Field_name { get; set; }
       public string Aphase_Value { get; set; }
       public string Bphase_Value { get; set; }
       public string Cphase_Value { get; set; }
      
    }
}
