using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MaxMinAlgorithm
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly Color[] colorsArr = new Color[] { Colors.MediumSeaGreen, Colors.SandyBrown, Colors.SlateBlue, Colors.DeepPink, Colors.GreenYellow,
                                                  Colors.IndianRed, Colors.LightSkyBlue, Colors.Crimson, Colors.SpringGreen, Colors.LightPink,
                                                  Colors.CornflowerBlue, Colors.Gold, Colors.Maroon, Colors.MidnightBlue, Colors.Orchid,
                                                  Colors.Lime, Colors.Tan, Colors.Brown, Colors.MediumSlateBlue, Colors.Coral};
        private static readonly Color kernelColor = Colors.Black;
        private static readonly Color kernelBorderColor = Colors.Red;
        private static readonly Color pointsBorderColor = Color.FromRgb(50, 50, 50);
        private static readonly double kernelsRelativeSize = 0.015;



        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            int objects = (int)sliderObjectsAmount.Value;
            ThreadPool.QueueUserWorkItem((obj) => { StartAlgorithm((SynchronizationContext)obj, objects); }, SynchronizationContext.Current);
        }

        private void ChangeElementsState(bool isEnabled)
        {
            btnStart.IsEnabled = isEnabled;
            sliderObjectsAmount.IsEnabled = isEnabled;
            txtClassesAmount.IsEnabled = isEnabled;
            if (isEnabled)
            {
                this.ResizeMode = ResizeMode.CanResize;
            }
            else
            {
                this.ResizeMode = ResizeMode.NoResize;
                txtClassesAmount.Text = "0";
                DrawCanvas.Children.Clear();
            }
        }

        private void StartAlgorithm(SynchronizationContext context, int objectsAmount)
        {
            context.Send((x) =>
            {
                ChangeElementsState(isEnabled: false);
            }, null);

            Point[] objects = GenerateRandomPoints(objectsAmount);

            MaxMin<Point> maxMinAlgorithm = new MaxMin<Point>(objectsAmount);
            List<int> kernelIndexes = maxMinAlgorithm.ChooseTwoKernels(objects,DistanceBetweenPoints);
            Dictionary<Point, int> classesDivision = maxMinAlgorithm.DivideIntoClasses(objects, kernelIndexes, DistanceBetweenPoints);

            Task drawingTask = new Task((obj) => { AddPointsAsync(obj, classesDivision, objects, kernelIndexes); }, context);
            drawingTask.Start();

            while (maxMinAlgorithm.TryFindNewKernel(classesDivision, objects, ref kernelIndexes, DistanceBetweenPoints))
            {
                drawingTask.Wait();
                Thread.Sleep(1000);

                classesDivision = maxMinAlgorithm.DivideIntoClasses(objects, kernelIndexes, DistanceBetweenPoints);

                drawingTask = new Task((obj) => { RepaintPoints(obj, objects,classesDivision, kernelIndexes); }, context);
                drawingTask.Start();
            }
            context.Send((x) =>
            {
                ((Ellipse)DrawCanvas.Children[DrawCanvas.Children.Count - 1]).Fill = new SolidColorBrush(kernelColor);
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

        private void AddPointsAsync(object context, Dictionary<Point, int> classesDivision, Point[] points, List<int> kernelIndexes)
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
                for (int i = 0; i < 2; i++)
                {
                    AddPoint(points[kernelIndexes[i]].X, points[kernelIndexes[i]].Y, kernelsWidth, kernelColor, kernelBorderColor);
                }
            }, null);
        }

        private void RepaintPoints(object context, Point[] points, Dictionary<Point, int> classesDivision, List<int> kernelIndexes)
        {
            int classesAmount = kernelIndexes.Count();
            Point newKernel = points[kernelIndexes.Last()];
            
            List<int> newClassPointsIndexes = new List<int>();
            for (int i=0;i<points.Count();i++)
            {
                if (classesDivision[points[i]] == classesAmount-1)
                {
                    newClassPointsIndexes.Add(i);
                }
            }
            int classPointsAmount = newClassPointsIndexes.Count();
            
            double kernelsWidth = DrawCanvas.ActualWidth * kernelsRelativeSize;
            ((SynchronizationContext)context).Send((x) =>
            {
                for (int i = 0; i < classPointsAmount; i++)
                {
                    ((Ellipse)DrawCanvas.Children[newClassPointsIndexes[i]]).Fill = new SolidColorBrush(colorsArr[classesAmount-1]);      
                }
                ((Ellipse)DrawCanvas.Children[DrawCanvas.Children.Count - 1]).Fill = new SolidColorBrush(kernelColor);
                AddPoint(newKernel.X, newKernel.Y, kernelsWidth, Colors.Red, kernelBorderColor);
                txtClassesAmount.Text = (classesAmount).ToString();
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
