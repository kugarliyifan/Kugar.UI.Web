using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

  using System.Web.Mvc;   



namespace Kugar.Core.Web
{
    public class ValidateCodeResult: ActionResult
    {
        private ImageResult _imageResult=null;
        private string _code = "";
        private int _width = 100;
        private int _height = 40;

        public ValidateCodeResult(string code,int width=100,int height=40)
        {
            _code = code;
            _width = width;
            _height = height;
        }

        public override void ExecuteResult(ControllerContext context)
        {
            using (var img = CreateValidateGraphic(_width, _height, 20))
            {
                _imageResult=new ImageResult(img);

                _imageResult.ExecuteResult(context);                
            }
        }

        public Bitmap CreateValidateGraphic(int Width, int Height, int FontSize)
        {
     
            //顏色列表，用於驗證碼、噪線、噪點
            Color[] oColors ={
             System.Drawing.Color.Black,
             System.Drawing.Color.Red,
             System.Drawing.Color.Blue,
             System.Drawing.Color.Green,
             System.Drawing.Color.OrangeRed,
             System.Drawing.Color.Brown,
             System.Drawing.Color.Brown,
             System.Drawing.Color.DarkBlue
            };
            //字體列表，用於驗證碼
            string[] oFontNames = { "Times New Roman", "MS Mincho", "Book Antiqua", "Gungsuh", "PMingLiU", "Impact" };
 
            Random oRnd = new Random();
            Bitmap oBmp = null;
            Graphics oGraphics = null;
            int N1 = 0;
            System.Drawing.Point oPoint1 = default(System.Drawing.Point);
            System.Drawing.Point oPoint2 = default(System.Drawing.Point);
            string sFontName = null;
            Font oFont = null;
            Color oColor = default(Color);

            oBmp = new Bitmap(Width, Height);
            oGraphics = Graphics.FromImage(oBmp);
            oGraphics.Clear(System.Drawing.Color.White);
            try
            {
                //for (N1 = 0; N1 <= 4; N1++)
                //{
                //    //畫噪線
                //    oPoint1.X = oRnd.Next(Width);
                //    oPoint1.Y = oRnd.Next(Height);
                //    oPoint2.X = oRnd.Next(Width);
                //    oPoint2.Y = oRnd.Next(Height);
                //    oColor = oColors[oRnd.Next(oColors.Length)];
                //    oGraphics.DrawLine(new Pen(oColor), oPoint1, oPoint2);
                //}

                float spaceWith = 0, dotX = 0, dotY = 0;
                
                spaceWith = (Width - FontSize * _code.Length - 10) / _code.Length;

                for (N1 = 0; N1 <= _code.Length - 1; N1++)
                {
                    //畫驗證碼字串
                    sFontName = oFontNames[oRnd.Next(oFontNames.Length)];
                    oFont = new Font(sFontName, FontSize, FontStyle.Italic);
                    oColor = oColors[oRnd.Next(oColors.Length)];

                    dotY = (Height - oFont.Height) / 2 + 2;//中心下移2像素
                    dotX = Convert.ToSingle(N1) * FontSize + (N1 + 1) * spaceWith;

                    oGraphics.DrawString(_code[N1].ToString(), oFont, new SolidBrush(oColor), dotX, dotY);
                }

                for (int i = 0; i <= 30; i++)
                {
                    //畫噪點
                    int x = oRnd.Next(oBmp.Width);
                    int y = oRnd.Next(oBmp.Height);
                    Color clr = oColors[oRnd.Next(oColors.Length)];
                    oBmp.SetPixel(x, y, clr);
                }
                
                return oBmp;
            }
            finally
            {
                oGraphics.Dispose();
            }
        }
    }
}
