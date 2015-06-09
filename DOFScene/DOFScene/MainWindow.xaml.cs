#define SCREENSHOT
using SharpDX.Windows;
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
        float distance;

        const int SAMPLE_DISTANCE_NUM = 10;
	    const int OBJECT_DISTANCE_NUM = 10;
        float []focus_distance = new float[]{120, 200, 500, 800, 1000, 1600, 2000, 2500, 3000, 5000};
	    // object distances away from eye model.
	    float [][]object_distance = new float[SAMPLE_DISTANCE_NUM][]{
		    new float[]{50, 100, 120, 130, 150, 200, 300, 500, 1000, 5000},		// 120
		    new float[]{50, 100, 150, 180, 200, 250, 300, 500, 1000, 5000},
		    new float[]{100, 250, 350, 450, 500, 550, 600, 800, 1000, 5000},		// 500
		    new float[]{100, 250, 400, 500, 700, 800, 900, 1000, 1500, 5000},
		    new float[]{100, 300, 600, 800, 900, 1000, 1100, 1200, 2000, 5000},	// 1000
		    new float[]{200, 400, 800, 1200, 1400, 1600, 1700, 1800, 2000, 5000},
		    new float[]{200, 500, 1000, 1400, 1600, 1900, 2000, 2200, 2500, 5000},	// 2000
		    new float[]{200, 500, 800, 1200, 2000, 2400, 2500, 2700, 3000, 5000},
		    new float[]{200, 800, 1500, 2000, 2200, 2500, 2700, 3000, 3500, 5000},	// 3000
		    new float[]{200, 800, 1600, 2000, 2500, 3000, 3500, 4000, 4500, 5000}
	    };

        const int FOCUS_NUM = 6;
        int[] focus_x = new int[FOCUS_NUM] { 106, 186, 307, 505, 716, 766 };
        int[] focus_y = new int[FOCUS_NUM] { 169, 153, 132, 111, 90, 285 };
        RenderMode[] renderModes = new RenderMode[] { RenderMode.Result, RenderMode.SignedCOC, RenderMode.VisionResult, RenderMode.VisionXCoC };

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

            //displayWindow.setScreenshots(true);
            //int fi = 0;
            //int di = 0;
            //int mi = 0;
            //float startX = 95, startY = 187;
            //float endX = 897, endY = 247;
            //float curX = startX, curY = startY;
            //int steps = 30;
            //di = 0;
            //pupil = 4;
            //bool samplingFinished = false;
            //RenderLoop.Run(displayWindow.getForm(), () =>
            //{
            //    if (samplingFinished)
            //        return;
            //    renderMode = renderModes[mi];
            //    //renderMode = RenderMode.Pinhole;
            //    //focus = focus_distance[fi];
            //    //distance = object_distance[fi][di];
            //    distance = focus_distance[di];
            //    displayWindow.setFocusPoint(focus_x[fi], focus_y[fi]);
            //    fi++;
            //    //curX += (endX - startX) / steps;
            //    //curY += (endY - startY) / steps;
            //    redraw();

            //    if (fi >= FOCUS_NUM)
            //    {
            //        di++;
            //        fi = 0;
            //        //curX = startX;
            //        //curY = startY;
            //    }
            //    if (di >= SAMPLE_DISTANCE_NUM)
            //    {
            //        pupil += 2;
            //        di = 0;
            //    }
            //    if (pupil > 8)
            //    {
            //        mi++;
            //        pupil = 4;
            //    }
            //    if (mi > 3)
            //        samplingFinished = true;
            //    //if (di == OBJECT_DISTANCE_NUM)
            //    //{
            //    //    di = 0;
            //    //    fi++;
            //    //}
            //}
            //);
        }

        private void redraw()
        {
            float scale = distance / 0.7524f;
            displayWindow.Draw(renderMode, focus, pupil, scale);
        }

        private void RadioButton_Checked_1(object sender, RoutedEventArgs e)
        {
            renderMode = RenderMode.SignedCOC;
            redraw();
        }

        private void RadioButton_Checked_2(object sender, RoutedEventArgs e)
        {
            renderMode = RenderMode.Result;
            redraw();
        }

        private void RadioButton_Checked_3(object sender, RoutedEventArgs e)
        {
            renderMode = RenderMode.NearBuffer;
            redraw();
        }

        private void RadioButton_Checked_4(object sender, RoutedEventArgs e)
        {
            renderMode = RenderMode.Pinhole;
            redraw();
        }

        private void RadioButton_Checked_5(object sender, RoutedEventArgs e)
        {
            renderMode = RenderMode.Blurred;
            redraw();
        }

        private void RadioButton_Checked_6(object sender, RoutedEventArgs e)
        {
            renderMode = RenderMode.VisionParam;
            redraw();
        }

        private void Slider_ValueChanged_1(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            focus = (float)e.NewValue;
            if (focusValue != null)
                focusValue.Text = focus.ToString();
            redraw();
        }

        private void Slider_ValueChanged_2(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            pupil = (float)e.NewValue;
            if (pupilValue != null)
                pupilValue.Text = pupil.ToString();
            redraw();
        }

        private void RadioButton_Checked_7(object sender, RoutedEventArgs e)
        {
            renderMode = RenderMode.VisionResult;
            redraw();
        }

        private void RadioButton_Checked_8(object sender, RoutedEventArgs e)
        {
            renderMode = RenderMode.VisionXCoC;
            redraw();
        }

        private void RadioButton_Checked_9(object sender, RoutedEventArgs e)
        {
            renderMode = RenderMode.VisionYCoC;
            redraw();
        }

        private void Slider_ValueChanged_4(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            distance = (float)e.NewValue;
            if (distanceValue != null)
                distanceValue.Text = distance.ToString();
            redraw();
        }

        private void focusValue_TextChanged(object sender, TextChangedEventArgs e)
        {
            focus = (float)Double.Parse(focusValue.Text);
            redraw();
        }

        private void pupilValue_TextChanged(object sender, TextChangedEventArgs e)
        {
            pupil = (float)Double.Parse(pupilValue.Text);
            redraw();
        }

        private void distanceValue_TextChanged(object sender, TextChangedEventArgs e)
        {
            distance = (float)Double.Parse(distanceValue.Text);
            redraw();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            focusPosition.Content = "X: " + displayWindow.focusPoint.X + " Y: " + displayWindow.focusPoint.Y;
        }
    }
}
