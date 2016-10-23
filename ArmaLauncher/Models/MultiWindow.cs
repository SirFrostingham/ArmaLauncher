using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ArmaLauncher.Models
{
    public class MultiWindow : BaseWindow
    {
        private readonly IList<Window> _windows = new List<Window>();

        public void Register(Window window)
        {
            _windows.Add(window);
        }

        protected override void BaseShow()
        {
            foreach (var w in _windows)
                w.Show();
        }
    }
}
