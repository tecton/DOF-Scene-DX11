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

namespace DOFScene
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private DisplayWindow displayWindow = new DisplayWindow();

        RenderMode renderMode;
        float focus;
        float pupil;

        public MainWindow()
        {
            InitializeComponent();

            Loaded += (o, e) =>
            {
                var workingArea = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;
                var transform = PresentationSource.FromVisual(this).CompositionTarget.TransformFromDevice;
                var corner = transform.Transform(new Point(workingArea.Right, workingArea.Bottom));

                this.Left = corner.X - this.ActualWidth;
                this.Top = corner.Y - this.ActualHeight;
            };
            displayWindow.showWindow();
            //displayWindow.Draw(renderMode);
        }

        private void RadioButton_Checked_1(object sender, RoutedEventArgs e)
        {
            renderMode = RenderMode.SignedCOC;
            displayWindow.Draw(renderMode, focus, pupil);
        }

        private void RadioButton_Checked_2(object sender, RoutedEventArgs e)
        {
            renderMode = RenderMode.Result;
            displayWindow.Draw(renderMode, focus, pupil);
        }

        private void RadioButton_Checked_3(object sender, RoutedEventArgs e)
        {
            renderMode = RenderMode.NearBuffer;
            displayWindow.Draw(renderMode, focus, pupil);
        }

        private void RadioButton_Checked_4(object sender, RoutedEventArgs e)
        {
            renderMode = RenderMode.Pinhole;
            displayWindow.Draw(renderMode, focus, pupil);
        }

        private void RadioButton_Checked_5(object sender, RoutedEventArgs e)
        {
            renderMode = RenderMode.Blurred;
            displayWindow.Draw(renderMode, focus, pupil);
        }

        private void RadioButton_Checked_6(object sender, RoutedEventArgs e)
        {
            renderMode = RenderMode.VisionParam;
            displayWindow.Draw(renderMode, focus, pupil);
        }

        private void Slider_ValueChanged_1(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            focus = (float)e.NewValue;
            displayWindow.Draw(renderMode, focus, pupil);
        }

        private void Slider_ValueChanged_2(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            pupil = (float)e.NewValue;
            displayWindow.Draw(renderMode, focus, pupil);
        }

        private void RadioButton_Checked_7(object sender, RoutedEventArgs e)
        {
            renderMode = RenderMode.VisionResult;
            displayWindow.Draw(renderMode, focus, pupil);
        }

        private void RadioButton_Checked_8(object sender, RoutedEventArgs e)
        {
            renderMode = RenderMode.VisionXCoC;
            displayWindow.Draw(renderMode, focus, pupil);
        }

        private void RadioButton_Checked_9(object sender, RoutedEventArgs e)
        {
            renderMode = RenderMode.VisionYCoC;
            displayWindow.Draw(renderMode, focus, pupil);
        }


    }
}
