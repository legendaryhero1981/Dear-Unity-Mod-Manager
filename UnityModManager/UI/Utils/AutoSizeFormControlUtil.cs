using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace UnityModManagerNet.UI.Utils
{
    public sealed class AutoSizeFormControlUtil
    {
        private readonly Stack<Control> _controlsCache;
        private readonly Dictionary<string, string> _controlsInfo;//控件中心Left,Top,控件Width,控件Height,控件字体Size
        private double _scaleX;//水平缩放比例
        private double _scaleY;//垂直缩放比例
        private readonly double _formOriginalWidth;//窗体原始宽度
        private readonly double _formOriginalHeight;//窗体原始高度
        private readonly int _captionHeight;
        private readonly int _borderSize;
        private readonly int _screenHeight;
        private readonly int _screenWidth;
        private readonly Form _form;

        public AutoSizeFormControlUtil(Form form)
        {
            _form = form;
            _formOriginalWidth = Convert.ToDouble(_form.ClientSize.Width);
            _formOriginalHeight = Convert.ToDouble(_form.ClientSize.Height);
            _captionHeight = SystemInformation.CaptionHeight;
            _borderSize = SystemInformation.HorizontalResizeBorderThickness * 2;
            _screenHeight = Screen.PrimaryScreen.Bounds.Height;
            _screenWidth = Screen.PrimaryScreen.Bounds.Width;
            _controlsCache = new Stack<Control>();
            _controlsInfo = new Dictionary<string, string>();
        }
        /// <summary>
        /// <param name="control">panel 控件</param>
        /// </summary>
        public void RefreshControlsInfo(Control control)
        {
            ClearCaches();
            _RefreshControlsInfo(control);
        }

        public void FormSizeChanged()
        {
            if (_controlsInfo.Count <= 0) return;
            var width = Convert.ToDouble(_form.ClientSize.Width);//表示panel控件
            var height = Convert.ToDouble(_form.ClientSize.Height);
            if (_screenWidth == _form.ClientSize.Width)
                width += _borderSize;
            if (_screenHeight - _captionHeight == _form.ClientSize.Height)
                height += _borderSize + _captionHeight;
            _scaleX = width / _formOriginalWidth;
            _scaleY = height / _formOriginalHeight;
            if (0 < _scaleX * _scaleY)
                ControlsChange();
        }

        private void _RefreshControlsInfo(Control control)
        {
            var controls = new Stack<Control>();
            foreach (Control item in control.Controls)
                controls.Push(item);
            while (0 < controls.Count)
            {
                var parent = controls.Pop();
                foreach (Control child in parent.Controls)
                {
                    if (0 < child.Controls.Count)
                        controls.Push(child);
                    CacheControl(child);
                }
            }
        }

        private void CacheControl(Control control)
        {
            var name = control.Name.Trim();
            if (string.IsNullOrEmpty(name) || DockStyle.None != control.Dock) return;
            //添加信息：键值：控件名，内容：据左边距离，距顶部距离，控件宽度，控件高度，控件字体。
            _controlsInfo[name] = (control.Left + control.Width / 2) + "," + (control.Top + control.Height / 2) + "," + control.Width + "," + control.Height + "," + control.Font.Size;
            _controlsCache.Push(control);
        }
        /// <summary>
        /// 改变控件大小
        /// </summary>
        private void ControlsChange()
        {
            var pos = new double[5];//pos数组保存当前控件中心Left,Top,控件Width,控件Height,控件字体Size
            foreach (var item in _controlsCache)//遍历控件
            {
                var name = item.Name.Trim();
                var values = _controlsInfo[name].Split(',');//从字典中查出的数据，以‘,’分割成字符串组
                for (var i = 0; i < 5; i++)
                    pos[i] = Convert.ToDouble(values[i]);//添加到临时数组
                var itemWidth = pos[2] * _scaleX;     //计算控件宽度，double类型
                var itemHeight = pos[3] * _scaleY;    //计算控件高度
                item.Left = Convert.ToInt32(pos[0] * _scaleX - itemWidth / 2);//计算控件距离左边距离
                item.Top = Convert.ToInt32(pos[1] * _scaleY - itemHeight / 2);//计算控件距离顶部距离
                item.Width = Convert.ToInt32(itemWidth);//控件宽度，int类型
                item.Height = Convert.ToInt32(itemHeight);//控件高度
                var size = float.Parse((pos[4] * Math.Min(_scaleX, _scaleY)).ToString());
                if (Math.Abs(size - item.Font.Size) > 0)
                    item.Font = new Font(item.Font.Name, size, item.Font.Style);//字体
            }
        }

        private void ClearCaches()
        {
            _controlsCache.Clear();
            _controlsInfo.Clear();
        }
    }
}
