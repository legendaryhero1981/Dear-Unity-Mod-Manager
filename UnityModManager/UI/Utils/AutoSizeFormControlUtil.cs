using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace UnityModManagerNet.UI.Utils
{
    public sealed class AutoSizeFormControlUtil
    {
        private Stack<Control> controlsCache;
        private Dictionary<string, string> controlsInfo;//控件中心Left,Top,控件Width,控件Height,控件字体Size
        private double scaleX;//水平缩放比例
        private double scaleY;//垂直缩放比例
        private readonly double formOriginalWidth;//窗体原始宽度
        private readonly double formOriginalHeight;//窗体原始高度
        private readonly int captionHeight;
        private readonly int borderSize;
        private readonly int screenHeight;
        private readonly int screenWidth;
        private readonly Form _form;

        public AutoSizeFormControlUtil(Form form)
        {
            _form = form;
            formOriginalWidth = Convert.ToDouble(_form.ClientSize.Width);
            formOriginalHeight = Convert.ToDouble(_form.ClientSize.Height);
            captionHeight = SystemInformation.CaptionHeight;
            borderSize = SystemInformation.HorizontalResizeBorderThickness * 2;
            screenHeight = Screen.PrimaryScreen.Bounds.Height;
            screenWidth = Screen.PrimaryScreen.Bounds.Width;
            controlsCache = new Stack<Control>();
            controlsInfo = new Dictionary<string, string>();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="control">panel 控件</param>
        public void RefreshControlsInfo(Control control)
        {
            ClearCaches();
            _RefreshControlsInfo(control);
        }

        public void FormSizeChanged()
        {
            if (controlsInfo.Count > 0)//如果字典中有数据，即窗体改变
            {
                double width = Convert.ToDouble(_form.ClientSize.Width);//表示pannel控件
                double height = Convert.ToDouble(_form.ClientSize.Height);
                if (screenWidth == width)
                    width += borderSize;
                if (screenHeight - captionHeight == height)
                    height += borderSize + captionHeight;
                scaleX = width / formOriginalWidth;
                scaleY = height / formOriginalHeight;
                if (0 < scaleX * scaleY)
                    ControlsChange();
            }
        }

        private void _RefreshControlsInfo(Control control)
        {
            Stack<Control> controls = new Stack<Control>();
            foreach (Control item in control.Controls)
                controls.Push(item);
            while (0 < controls.Count)
            {
                Control parent = controls.Pop();
                foreach (Control child in parent.Controls)
                {
                    if (0 < child.Controls.Count)
                        controls.Push(child);
                    cacheControl(child);
                }
            }
        }

        private void cacheControl(Control control)
        {
            var name = control.Name.Trim();
            if (!string.IsNullOrEmpty(name) && DockStyle.None == control.Dock)
            {
                //添加信息：键值：控件名，内容：据左边距离，距顶部距离，控件宽度，控件高度，控件字体。
                controlsInfo[name] = (control.Left + control.Width / 2) + "," + (control.Top + control.Height / 2) + "," + control.Width + "," + control.Height + "," + control.Font.Size;
                controlsCache.Push(control);
            }
        }
        /// <summary>
        /// 改变控件大小
        /// </summary>
        private void ControlsChange()
        {
            double[] pos = new double[5];//pos数组保存当前控件中心Left,Top,控件Width,控件Height,控件字体Size
            foreach (Control item in controlsCache)//遍历控件
            {
                var name = item.Name.Trim();
                string[] strs = controlsInfo[name].Split(',');//从字典中查出的数据，以‘,’分割成字符串组
                for (int i = 0; i < 5; i++)
                    pos[i] = Convert.ToDouble(strs[i]);//添加到临时数组
                double itemWidth = pos[2] * scaleX;     //计算控件宽度，double类型
                double itemHeight = pos[3] * scaleY;    //计算控件高度
                item.Left = Convert.ToInt32(pos[0] * scaleX - itemWidth / 2);//计算控件距离左边距离
                item.Top = Convert.ToInt32(pos[1] * scaleY - itemHeight / 2);//计算控件距离顶部距离
                item.Width = Convert.ToInt32(itemWidth);//控件宽度，int类型
                item.Height = Convert.ToInt32(itemHeight);//控件高度
                var size = float.Parse((pos[4] * Math.Min(scaleX, scaleY)).ToString());
                if (size != item.Font.Size)
                    item.Font = new Font(item.Font.Name, size, item.Font.Style);//字体
            }
        }

        private void ClearCaches()
        {
            controlsCache.Clear();
            controlsInfo.Clear();
        }
    }
}
