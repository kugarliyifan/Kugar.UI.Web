var Sys = {};
var ua = navigator.userAgent.toLowerCase();
if (window.ActiveXObject)
    Sys.ie = ua.match( /msie ([\d.]+)/ )[1];
else if (document.getBoxObjectFor)
    Sys.firefox = ua.match( /firefox\/([\d.]+)/ )[1];
else if (window.MessageEvent && !document.getBoxObjectFor)
    Sys.chrome = ua.match( /chrome\/([\d.]+)/ )[1];
else if (window.opera)
    Sys.opera = ua.match( /opera.([\d.]+)/ )[1];
else if (window.openDatabase)
    Sys.safari = ua.match(/version\/([\d.]+)/)[1];




/** year : /yyyy/ */
 var y4 = "([0-9]{4})";
 /** year : /yy/ */
 var y2 = "([0-9]{2})";
 /** index year */
 var yi = -1;
 
 /** month : /MM/ */
 var M2 = "(0[1-9]|1[0-2])";
 /** month : /M/ */
 var M1 = "([1-9]|1[0-2])";
 /** index month */
 var Mi = -1;
 
 /** day : /dd/ */
 var d2 = "(0[1-9]|[1-2][0-9]|30|31)";
 /** day : /d/ */
 var d1 = "([1-9]|[1-2][0-9]|30|31)";
 /** index day */
 var di = -1;
 
 /** hour : /HH/ */
 var H2 = "([0-1][0-9]|20|21|22|23)";
 /** hour : /H/ */
 var H1 = "([0-9]|1[0-9]|20|21|22|23)";
 /** index hour */
 var Hi = -1;
 
 /** minute : /mm/ */
 var m2 = "([0-5][0-9])";
 /** minute : /m/ */
 var m1 = "([0-9]|[1-5][0-9])";
 /** index minute */
 var mi = -1;
 
 /** second : /ss/ */
 var s2 = "([0-5][0-9])";
 /** second : /s/ */
 var s1 = "([0-9]|[1-5][0-9])";
 /** index month */
 var si = -1;


 Object.prototype.GetTypeName = function() {
     var reg = /^(\w)/ ,
         regFn = function($, $1) {
             return $1.toUpperCase();
         },
         to_s = Object.prototype.toString;

         var obj = this;
         var result = (typeof obj).replace(reg, regFn);
         if (result === 'Object' || (result === 'Function' && obj.exec)) { //safari chrome中 type /i/ 为function
             if (obj === null) result = 'Null';
             else if (obj.window == obj) result = 'Window'; //返回Window的构造器名字
             else if (obj.callee) result = 'Arguments';
             else if (obj.nodeType === 9) result = 'Document';
             else if (obj.nodeName) result = (obj.nodeName + '').replace('#', ''); //处理元素节点
             else if (!obj.constructor || !(obj instanceof Object)) {
                 if ("send" in obj && "setRequestHeader" in obj) { //处理IE5-8的宿主对象与节点集合
                     result = "XMLHttpRequest"
                 } else if ("length" in obj && "item" in obj) {
                     result = "namedItem" in obj ? 'HTMLCollection' : 'NodeList';
                 } else {
                     result = 'Unknown';
                 }
             } else result = to_s.call(obj).slice(8, -1);
         }
         if (result === "Number" && isNaN(obj)) result = "NaN";
         //safari chrome中 对 HTMLCollection与NodeList的to_s都为 "NodeList",此bug暂时无解
//         if (str) {
//             return str === result;
//         }
         return result;

 };
 
 

//---------------------------------------------------  
// 判断闰年  
//---------------------------------------------------  
Date.prototype.isLeapYear = function()
{
    return (0 == this.getYear() % 4 && ((this.getYear() % 100 != 0) || (this.getYear() % 400 == 0)));
};
  
//---------------------------------------------------  
// 日期格式化  
// 格式 YYYY/yyyy/YY/yy 表示年份  
// MM/M 月份  
// W/w 星期  
// dd/DD/d/D 日期  
// hh/HH/h/H 时间  
// mm/m 分钟  
// ss/SS/s/S 秒  
//---------------------------------------------------  
Date.prototype.Format = function(formatStr)
{
    var str = formatStr;
    var Week = ['日', '一', '二', '三', '四', '五', '六'];

    str = str.replace( /yyyy|YYYY/ , this.getFullYear());
    str = str.replace( /yy|YY/ , (this.getYear() % 100) > 9 ? (this.getYear() % 100).toString() : '0' + (this.getYear() % 100));

    str = str.replace( /MM/ , this.getMonth() > 9 ? this.getMonth().toString() : '0' + this.getMonth());
    str = str.replace( /M/g , this.getMonth());

    str = str.replace( /w|W/g , Week[this.getDay()]);

    str = str.replace( /dd|DD/ , this.getDate() > 9 ? this.getDate().toString() : '0' + this.getDate());
    str = str.replace( /d|D/g , this.getDate());

    str = str.replace( /hh|HH/ , this.getHours() > 9 ? this.getHours().toString() : '0' + this.getHours());
    str = str.replace( /h|H/g , this.getHours());
    str = str.replace( /mm/ , this.getMinutes() > 9 ? this.getMinutes().toString() : '0' + this.getMinutes());
    str = str.replace( /m/g , this.getMinutes());

    str = str.replace( /ss|SS/ , this.getSeconds() > 9 ? this.getSeconds().toString() : '0' + this.getSeconds());
    str = str.replace( /s|S/g , this.getSeconds());

    return str;
};
  
//+---------------------------------------------------  
//| 求两个时间的天数差 日期格式为 YYYY-MM-dd   
//+---------------------------------------------------  
function daysBetween(DateOne,DateTwo)  
{   
    var OneMonth = DateOne.substring(5,DateOne.lastIndexOf ('-'));  
    var OneDay = DateOne.substring(DateOne.length,DateOne.lastIndexOf ('-')+1);  
    var OneYear = DateOne.substring(0,DateOne.indexOf ('-'));  
  
    var TwoMonth = DateTwo.substring(5,DateTwo.lastIndexOf ('-'));  
    var TwoDay = DateTwo.substring(DateTwo.length,DateTwo.lastIndexOf ('-')+1);  
    var TwoYear = DateTwo.substring(0,DateTwo.indexOf ('-'));  
  
    var cha=((Date.parse(OneMonth+'/'+OneDay+'/'+OneYear)- Date.parse(TwoMonth+'/'+TwoDay+'/'+TwoYear))/86400000);   
    return Math.abs(cha);  
}  ;
  
  
//+---------------------------------------------------  
//| 日期计算  
//+---------------------------------------------------  
Date.prototype.DateAdd = function(strInterval, Number) {
    var dtTmp = this;
    switch (strInterval) {
    case 's':return new Date(Date.parse(dtTmp) + (1000 * Number));
    case 'n':return new Date(Date.parse(dtTmp) + (60000 * Number));
    case 'h':return new Date(Date.parse(dtTmp) + (3600000 * Number));
    case 'd':return new Date(Date.parse(dtTmp) + (86400000 * Number));
    case 'w':return new Date(Date.parse(dtTmp) + ((86400000 * 7) * Number));
    case 'q':return new Date(dtTmp.getFullYear(), (dtTmp.getMonth()) + Number * 3, dtTmp.getDate(), dtTmp.getHours(), dtTmp.getMinutes(), dtTmp.getSeconds());
    case 'm':return new Date(dtTmp.getFullYear(), (dtTmp.getMonth()) + Number, dtTmp.getDate(), dtTmp.getHours(), dtTmp.getMinutes(), dtTmp.getSeconds());
    case 'y':return new Date((dtTmp.getFullYear() + Number), dtTmp.getMonth(), dtTmp.getDate(), dtTmp.getHours(), dtTmp.getMinutes(), dtTmp.getSeconds());
    }
};
  
//+---------------------------------------------------  
//| 比较日期差 dtEnd 格式为日期型或者 有效日期格式字符串  
//+---------------------------------------------------  
Date.prototype.DateDiff = function(strInterval, dtEnd) {
    var dtStart = this;
    if (typeof dtEnd == 'string')//如果是字符串转换为日期型  
    {
        dtEnd = StringToDate(dtEnd);
    }
    switch (strInterval) {
    case 's':return parseInt((dtEnd - dtStart) / 1000);
    case 'n':return parseInt((dtEnd - dtStart) / 60000);
    case 'h':return parseInt((dtEnd - dtStart) / 3600000);
    case 'd':return parseInt((dtEnd - dtStart) / 86400000);
    case 'w':return parseInt((dtEnd - dtStart) / (86400000 * 7));
    case 'm':return (dtEnd.getMonth() + 1) + ((dtEnd.getFullYear() - dtStart.getFullYear()) * 12) - (dtStart.getMonth() + 1);
    case 'y':return dtEnd.getFullYear() - dtStart.getFullYear();
    }
};
  
//+---------------------------------------------------  
//| 日期输出字符串，重载了系统的toString方法  
//+---------------------------------------------------  
Date.prototype.toString = function(showWeek)
{
    var myDate = this;
    var str = myDate.toLocaleDateString();
    if (showWeek)
    {
        var Week = ['日', '一', '二', '三', '四', '五', '六'];
        str += ' 星期' + Week[myDate.getDay()];
    }
    return str;
};
  
//+---------------------------------------------------  
//| 日期合法性验证  
//| 格式为：YYYY-MM-DD或YYYY/MM/DD  
//+---------------------------------------------------  
String.prototype.IsValidDate=function ()   
{   
    var sDate=this.replace(/(^\s+|\s+$)/g,''); //去两边空格;   
    if(sDate=='') return true;   
    //如果格式满足YYYY-(/)MM-(/)DD或YYYY-(/)M-(/)DD或YYYY-(/)M-(/)D或YYYY-(/)MM-(/)D就替换为''   
    //数据库中，合法日期可以是:YYYY-MM/DD(2003-3/21),数据库会自动转换为YYYY-MM-DD格式   
    var s = sDate.replace(/[\d]{ 4,4 }[\-/]{ 1 }[\d]{ 1,2 }[\-/]{ 1 }[\d]{ 1,2 }/g,'');   
    if (s=='') //说明格式满足YYYY-MM-DD或YYYY-M-DD或YYYY-M-D或YYYY-MM-D   
    {   
        var t=new Date(sDate.replace(/\-/g,'/'));   
        var ar = sDate.split(/[-/:]/);   
        if(ar[0] != t.getYear() || ar[1] != t.getMonth()+1 || ar[2] != t.getDate())   
        {   
            //alert('错误的日期格式！格式为：YYYY-MM-DD或YYYY/MM/DD。注意闰年。');   
            return false;   
        }   
    }   
    else   
    {   
        //alert('错误的日期格式！格式为：YYYY-MM-DD或YYYY/MM/DD。注意闰年。');   
        return false;   
    }   
    return true;   
}   ;

function getValidDateRegex(formatString) {
     var reg = formatString;
     reg = reg.replace( /yyyy/ , y4);
     reg = reg.replace( /yy/ , y2);
     reg = reg.replace( /MM/ , M2);
     reg = reg.replace( /M/ , M1);
     reg = reg.replace( /dd/ , d2);
     reg = reg.replace( /d/ , d1);
     reg = reg.replace( /HH/ , H2);
     reg = reg.replace( /H/ , H1);
     reg = reg.replace( /mm/ , m2);
     reg = reg.replace( /m/ , m1);
     reg = reg.replace( /ss/ , s2);
     reg = reg.replace( /s/ , s1);
     return new RegExp("^" + reg + "$");
    
}

//+---------------------------------------------------  
//| 日期合法性验证  
//| 格式为：formatString 参数指定的格式
//+---------------------------------------------------  
 String.prototype.IsValidDateByFormat = function(formatString) {
     var dateString = this.Trim;
     if (dateString == "") return false;
//     var reg = formatString;
//     reg = reg.replace( /yyyy/ , y4);
//     reg = reg.replace( /yy/ , y2);
//     reg = reg.replace( /MM/ , M2);
//     reg = reg.replace( /M/ , M1);
//     reg = reg.replace( /dd/ , d2);
//     reg = reg.replace( /d/ , d1);
//     reg = reg.replace( /HH/ , H2);
//     reg = reg.replace( /H/ , H1);
//     reg = reg.replace( /mm/ , m2);
//     reg = reg.replace( /m/ , m1);
//     reg = reg.replace( /ss/ , s2);
//     reg = reg.replace( /s/ , s1);
//     reg = new RegExp("^" + reg + "$");
     var reg = getValidDateRegex(formatString);
     return reg.test(dateString);
 };

String.prototype.Trim = function() {
    return this.replace(/(^\s+|\s+$)/g,'');
};

//+---------------------------------------------------  
//| 日期时间检查  
//| 格式为：YYYY-MM-DD HH:MM:SS  
//+---------------------------------------------------  
String.prototype.CheckDateTimeString=function ()  
{   
    var reg = /^(\d+)-(\d{ 1,2 })-(\d{ 1,2 }) (\d{ 1,2 }):(\d{ 1,2 }):(\d{ 1,2 })$/;   
    var r = this.match(reg);   
    if(r==null)return false;   
    r[2]=r[2]-1;   
    var d= new Date(r[1],r[2],r[3],r[4],r[5],r[6]);   
    if(d.getFullYear()!=r[1])return false;   
    if(d.getMonth()!=r[2])return false;   
    if(d.getDate()!=r[3])return false;   
    if(d.getHours()!=r[4])return false;   
    if(d.getMinutes()!=r[5])return false;   
    if(d.getSeconds()!=r[6])return false;   
    return true;   
};

//+---------------------------------------------------  
//| 日期检查  
//| 格式为：YYYY-MM-DD HH:MM:SS  
//+---------------------------------------------------  
String.prototype.CheckDateString=function()  
{   
    var reg = /^(\d+)-(\d{ 1,2 })-(\d{ 1,2 })$/;   
    var r = this.match(reg);   
    if(r==null)return false;   
    r[2]=r[2]-1;   
    var d= new Date(r[1],r[2],r[3]);   
    if(d.getFullYear()!=r[1])return false;   
    if(d.getMonth()!=r[2])return false;   
    if(d.getDate()!=r[3])return false;   

    return true;   
};
  
//+---------------------------------------------------  
//| 把日期分割成数组  
//+---------------------------------------------------  
Date.prototype.toArray = function()
{
    var myDate = this;
    var myArray = Array();
    myArray[0] = myDate.getFullYear();
    myArray[1] = myDate.getMonth();
    myArray[2] = myDate.getDate();
    myArray[3] = myDate.getHours();
    myArray[4] = myDate.getMinutes();
    myArray[5] = myDate.getSeconds();
    return myArray;
};
  
//+---------------------------------------------------  
//| 取得日期数据信息  
//| 参数 interval 表示数据类型  
//| y 年 m月 d日 w星期 ww周 h时 n分 s秒  
//+---------------------------------------------------  
Date.prototype.DatePart = function(interval)
{
    var myDate = this;
    var partStr = '';
    var Week = ['日', '一', '二', '三', '四', '五', '六'];
    switch (interval)
    {
    case 'y':partStr = myDate.getFullYear();break;
    case 'm':partStr = myDate.getMonth() + 1;break;
    case 'd':partStr = myDate.getDate();break;
    case 'w':partStr = Week[myDate.getDay()];break;
    case 'ww':partStr = myDate.WeekNumOfYear();break;
    case 'h':partStr = myDate.getHours();break;
    case 'n':partStr = myDate.getMinutes();break;
    case 's':partStr = myDate.getSeconds();break;
    }
    return partStr;
};
  
//+---------------------------------------------------  
//| 取得当前日期所在月的最大天数  
//+---------------------------------------------------  
Date.prototype.MaxDayOfDate = function()
{
    var myDate = this;
    var ary = myDate.toArray();
    var date1 = (new Date(ary[0], ary[1] + 1, 1));
    var date2 = date1.dateAdd(1, 'm', 1);
    var result = dateDiff(date1.Format('yyyy-MM-dd'), date2.Format('yyyy-MM-dd'));
    return result;
};
  
//+---------------------------------------------------  
//| 取得当前日期所在周是一年中的第几周  
//+---------------------------------------------------  
Date.prototype.WeekNumOfYear = function()
{
    var myDate = this;
    var ary = myDate.toArray();
    var year = ary[0];
    var month = ary[1] + 1;
    var day = ary[2];
    document.write('< script language=VBScript\> \n');
    document.write("myDate = DateValue(''+month+'-'+day+'-'+year+'') \n");
    document.write('result = DatePart("ww", myDate) \n');
    document.write(' \n');
    return result;
};
  
//+---------------------------------------------------  
//| 字符串转成日期类型   
//| 格式 MM/dd/YYYY MM-dd-YYYY YYYY/MM/dd YYYY-MM-dd  
////+---------------------------------------------------  
//String.prototype.StringToDate=function (dateFormat,splitChar)  
//{
//    splitChar = splitChar | '-';
//    
//    var srcList = this.split(splitChar);
//    var formatList = dateFormat.split(splitChar);

//    if (formatList=='yyyy') {
//        
//    }
//   
//    var converted = Date.parse(this);  
//    var myDate = new Date(converted);  
//    if (isNaN(myDate))  
//    {   
//        //var delimCahar = DateStr.indexOf('/')!=-1?'/':'-';  
//        var arys= DateStr.split('-');  
//        myDate = new Date(arys[0],--arys[1],arys[2]);  
//    }  
//    return myDate;  
//}  ;


 String.prototype.StringToDate = function(formatString) {
     var reg=getValidDateRegex(formatString);
     
     if (reg.test(this)) {
         var now = new Date();
         var vals = reg.exec(this);
         var index = validateIndex(formatString);
         var year = index[0] >= 0 ? vals[index[0] + 1] : now.getFullYear();
         var month = index[1] >= 0 ? (vals[index[1] + 1] - 1) : now.getMonth();
         var day = index[2] >= 0 ? vals[index[2] + 1] : now.getDate();
         var hour = index[3] >= 0 ? vals[index[3] + 1] : "";
         var minute = index[4] >= 0 ? vals[index[4] + 1] : "";
         var second = index[5] >= 0 ? vals[index[5] + 1] : "";

         var validate;

         if (hour == "")
             validate = new Date(year, month, day);
         else
             validate = new Date(year, month, day, hour, minute, second);

         if (validate.getDate() == day) return validate;

     }
     alert("wrong date");
 };



 
 function validateIndex(formatString){
 
 var ia = new Array();
 var i = 0;
 yi = formatString.search(/yyyy/);
 if ( yi < 0 ) yi = formatString.search(/yy/);
 if (yi >= 0) {
 ia[i] = yi;
 i++;
 }
 
 Mi = formatString.search(/MM/);
 if ( Mi < 0 ) Mi = formatString.search(/M/);
 if (Mi >= 0) {
 ia[i] = Mi;
 i++;
 }
 
 di = formatString.search(/dd/);
 if ( di < 0 ) di = formatString.search(/d/);
 if (di >= 0) {
 ia[i] = di;
 i++;
 }
 
 Hi = formatString.search(/HH/);
 if ( Hi < 0 ) Hi = formatString.search(/H/);
 if (Hi >= 0) {
 ia[i] = Hi;
 i++;
 }
 
 mi = formatString.search(/mm/);
 if ( mi < 0 ) mi = formatString.search(/m/);
 if (mi >= 0) {
 ia[i] = mi;
 i++;
 }
 
 si = formatString.search(/ss/);
 if ( si < 0 ) si = formatString.search(/s/);
 if (si >= 0) {
 ia[i] = si;
 i++;
 }
 
 var ia2 = new Array(yi, Mi, di, Hi, mi, si);
 
 for(i=0; i<ia.length-1; i++) 
 for(j=0;j<ia.length-1-i;j++) 
 if(ia[j]>ia[j+1]) {
 temp=ia[j]; 
 ia[j]=ia[j+1]; 
 ia[j+1]=temp;
 }
 
 for (i=0; i<ia.length ; i++)
 for (j=0; j<ia2.length; j++)
 if(ia[i]==ia2[j]) {
 ia2[j] = i;
 }
 
 return ia2;
 };