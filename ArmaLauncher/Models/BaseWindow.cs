using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ArmaLauncher.Models
{
    public class BaseWindow : Window
    {

        // Hide default implementation and call BaseShow instead
        public new void Show()
        {
            BaseShow();
        }

        protected virtual void BaseShow()
        {
            base.Show();
        }

        // ... abstract away any additional Window methods and properties required 
    }
}
