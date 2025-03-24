using EM.Maman.Models.LocalDbModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace EM.Maman.DriverClient.Controls
{
    /// <summary>
    /// Interaction logic for FingerControl.xaml
    /// </summary>
    public partial class FingerControl : UserControl
    {
        public static readonly DependencyProperty FingerProperty =
            DependencyProperty.Register("Finger", typeof(Finger), typeof(FingerControl), new PropertyMetadata(null));

        public static readonly DependencyProperty IsSideRightProperty =
            DependencyProperty.Register("IsSideRight", typeof(bool), typeof(FingerControl), new PropertyMetadata(false));

        public static readonly DependencyProperty CurrentLevelProperty =
            DependencyProperty.Register("CurrentLevel", typeof(int), typeof(FingerControl), new PropertyMetadata(0));

        public Finger Finger
        {
            get { return (Finger)GetValue(FingerProperty); }
            set { SetValue(FingerProperty, value); }
        }

        public bool IsSideRight
        {
            get { return (bool)GetValue(IsSideRightProperty); }
            set { SetValue(IsSideRightProperty, value); }
        }

        public int CurrentLevel
        {
            get { return (int)GetValue(CurrentLevelProperty); }
            set { SetValue(CurrentLevelProperty, value); }
        }

        public FingerControl()
        {
            InitializeComponent();
        }
    }
}
