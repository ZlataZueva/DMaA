using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

namespace KMeansAlgorithm
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly Color[] colorsArr = new Color[] { Colors.MediumSeaGreen,Colors.SandyBrown,  Colors.SlateBlue,Colors.DeepPink,       Colors.GreenYellow,
                                                                  Colors.IndianRed,     Colors.LightSkyBlue,Colors.Crimson,  Colors.SpringGreen,    Colors.LightPink,
                                                                  Colors.CornflowerBlue,Colors.Gold,        Colors.Maroon,   Colors.MidnightBlue,   Colors.Orchid,
                                                                  Colors.Lime,          Colors.Tan,         Colors.Brown,    Colors.MediumSlateBlue,Colors.Coral};
        private static readonly Color kernelColor = Colors.Black;
        private static readonly Color kernelBorderColor = Colors.Red;
        private static readonly Color pointsBorderColor = Color.FromRgb(50, 50, 50);
        private static readonly double kernelsRelativeSize = 0.015;

        public static int iterationsCounter = 0;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void cmdUp_Click(object sender, RoutedEventArgs e)
        {
            int current = int.Parse(txtNum.Text);
            if (current < KMean<Point>.MaxClassesAmount)
            {
                txtNum.Text = (++current).ToString();
            }
        }

        private void cmdDown_Click(object sender, RoutedEventArgs e)
        {
            int current = int.Parse(txtNum.Text);
            if (current > KMean<Point>.MinClassesAmount)
            {
                txtNum.Text = (--current).ToString();
            }
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            int classes = int.Parse(txtNum.Text);
            int objects = (int)sliderObjectsAmount.Value;
            ThreadPool.QueueUserWorkItem((obj) => { StartAlgorithm((SynchronizationContext)obj, objects, classes); }, SynchronizationContext.Current);
        }

        private void ChangeElementsState(bool isEnabled)
        {
            btnStart.IsEnabled = isEnabled;
            sliderObjectsAmount.IsEnabled = isEnabled;
            cmdDown.IsEnabled = isEnabled;
            cmdUp.IsEnabled = isEnabled;
            if (isEnabled)
            {
                lblIterationsCounter.BorderBrush = null;
                this.ResizeMode = ResizeMode.CanResize;
            }
            else
            {
                this.ResizeMode = ResizeMode.NoResize;
                lblIterationsCounter.BorderBrush = lblIterationsCounter.Foreground;
                lblIterationsCounter.Content = "0 iteration(s)";
                DrawCanvas.Children.Clear();
            }
        }

        private void StartAlgorithm(SynchronizationContext context, int objectsAmount, int classesAmount)
        {
            context.Send((x) =>
            {
                ChangeElementsState(isEnabled: false);
            }, null);

            Point[] objects = GenerateRandomPoints(objectsAmount);

            KMean<Point> kMeansAlgorithm = new KMean<Point>(objectsAmount, classesAmount);
            iterationsCounter = 0;
       /*1*/int[] kernelIndexes = kMeansAlgorithm.ChooseRandomKernels();
            Dictionary<Point, int> classesDivision = kMeansAlgorithm.DivideIntoClasses(objects, kernelIndexes, DistanceBetweenPoints);

            Task drawingTask = new Task((obj) => { AddPointsAsync(obj, classesDivision, objects, kernelIndexes); }, context);
            drawingTask.Start();

       /*2*/while (!kMeansAlgorithm.CheckandRechooseKernels(classesDivision, objects, ref kernelIndexes, DistanceBetweenPoints))
            {
       /*3*/    classesDivision = kMeansAlgorithm.DivideIntoClasses(objects, kernelIndexes, DistanceBetweenPoints);

                drawingTask.Wait();
                drawingTask = new Task((obj) => { RepaintPoints(obj, classesDivision, objects, kernelIndexes); }, context);
                drawingTask.Start();

            }

            context.Send((x) =>
            {
                ChangeElementsState(isEnabled: true);
            }, null);
        }

        private void AddPoint(double x, double y, double wh, Color pointColor, Color borderColor)
        {
            Ellipse point = new Ellipse();
            point.Width = point.Height = wh;
            point.Fill = new SolidColorBrush(pointColor);
            point.Stroke = new SolidColorBrush(borderColor);
            point.Margin = new Thickness(x - point.Width / 2, y - point.Height / 2, 0, 0);
            DrawCanvas.Children.Add(point);
        }

        private void AddPointsAsync(object context, Dictionary<Point, int> classesDivision, Point[] points, int[] kernelIndexes)
        {
            ((SynchronizationContext)context).Send((x) =>
            {
                DrawCanvas.Children.Clear();

                int pointsAmount = points.Count();
                double pointsWidth = Math.Sqrt(DrawCanvas.ActualWidth * DrawCanvas.ActualHeight / pointsAmount);
                for (int i = 0; i < pointsAmount; i++)
                {
                    AddPoint(points[i].X, points[i].Y, pointsWidth, colorsArr[classesDivision[points[i]]], pointsBorderColor);
                }
            }, null);
            ((SynchronizationContext)context).Send((x) =>
            {
                double kernelsWidth = DrawCanvas.ActualWidth * kernelsRelativeSize;
                for (int i = 0; i < kernelIndexes.Count(); i++)
                {
                    AddPoint(points[kernelIndexes[i]].X, points[kernelIndexes[i]].Y, kernelsWidth, kernelColor, kernelBorderColor);
                }

                lblIterationsCounter.Content = ++iterationsCounter + " iteration(s)";
            }, null);
        }

        private void RepaintPoints(object context, Dictionary<Point, int> classesDivision, Point[] points, int[] kernelIndexes)
        {
            int pointsAmount = points.Count();
            double kernelsWidth = DrawCanvas.ActualWidth * kernelsRelativeSize;
            ((SynchronizationContext)context).Send((x) =>
            {
                for (int i = 0; i < pointsAmount; i++)
                {
                    ((Ellipse)DrawCanvas.Children[i]).Fill = new SolidColorBrush(colorsArr[classesDivision[points[i]]]);
                }
                for (int i = 0; i < kernelIndexes.Count(); i++)
                {
                    ((Ellipse)DrawCanvas.Children[i + pointsAmount]).Margin = new Thickness(points[kernelIndexes[i]].X - kernelsWidth / 2, points[kernelIndexes[i]].Y - kernelsWidth / 2, 0, 0);
                }

                lblIterationsCounter.Content = ++iterationsCounter + " iteration(s)";
            }, null);
        }

        public double DistanceBetweenPoints(Point p1, Point p2)
        {
            return Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y));
        }

        private Point[] GenerateRandomPoints(int amount)
        {
            Point[] points_arr = new Point[amount];
            Random random = new Random();
            int i = 0;
            while (i < amount)
            {
                Point newPoint = new Point(random.Next((int)DrawCanvas.ActualWidth), random.Next((int)DrawCanvas.ActualHeight));
                if ((amount > 10000) || !points_arr.Contains(newPoint))
                {
                    points_arr[i++] = newPoint;
                }
            }
            return points_arr;
        }

        private void winMain_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawCanvas.Children.Clear();
        }
    }
}
