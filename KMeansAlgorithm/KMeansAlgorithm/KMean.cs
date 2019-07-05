using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KMeansAlgorithm
{
    class KMean<TObject> 
    {
        static public readonly int MaxObjectsAmount = 100000;
        static public readonly int MinObjectsAmount = 1000;
        static public readonly byte MaxClassesAmount = 20;
        static public readonly byte MinClassesAmount = 2;

        int _objectsAmount;
        int _classesAmount;

        public int ObjectsAmount
        {
            get
            {
                return _objectsAmount;
            }
            private set
            {
                if (value >= MinObjectsAmount && value <= MaxObjectsAmount)
                {
                    _objectsAmount = value;
                }
            }
        }

        public int ClassesAmount
        {
            get
            {
                return _classesAmount;
            }
            private set
            {
                if (value >= MinClassesAmount && value <= MaxObjectsAmount)
                {
                    _classesAmount = value;
                }
            }
        }

        public KMean(int objectsAmount, int classesAmount)
        {
            this.ObjectsAmount = objectsAmount;
            this.ClassesAmount = classesAmount;
        }

        public int[] ChooseRandomKernels ()
        {
            int[] kernelsIndexes = new int[_classesAmount];
            Random random = new Random();
            int i = 0;
            while (i<_classesAmount)
            {
                int newKernel = random.Next(_objectsAmount);
                if (!kernelsIndexes.Contains(newKernel))
                {
                    kernelsIndexes[i++] = newKernel;
                }
            }
            return kernelsIndexes;
        }

        public Dictionary<TObject, int> DivideIntoClasses(TObject[] objects, int[] kernelsIndexes, Func<TObject, TObject, double> Distance)
        {
            Dictionary<TObject, int> division = new Dictionary<TObject, int>(objects.Count());
            if (objects.Count() == _objectsAmount && kernelsIndexes.Count() == _classesAmount)
            {
                Task<int>[] tasksArr = new Task<int>[_objectsAmount];
                for (int i = 0; i < _objectsAmount; i++)
                {
                    tasksArr[i] = new Task<int>((objNum) =>
                    {
                        double minDistance = double.MaxValue;
                        int classNum = 0;
                        for (int j = 0; j < _classesAmount; j++)
                        {
                            double distance = Distance(objects[(int)objNum], objects[kernelsIndexes[j]]);
                            if (distance < minDistance)
                            {
                                minDistance = distance;
                                classNum = j;
                            }
                        }
                        return classNum;
                    },i);
                    tasksArr[i].Start();                   
                }
                for(int i=0; i<_objectsAmount;i++)
                {
                    tasksArr[i].Wait();
                    division[objects[i]] = tasksArr[i].Result;
                }
            }
            return division;
        }

        private TObject FindNewClassKernel (List<TObject> classObjects, Func<TObject, TObject, double> Distance)
        { 
            if (classObjects.Count() > 0)
            {
                TObject kernel = classObjects[0];
                double minStandardDeviation = double.MaxValue;
                for (int tryKernelNum=0; tryKernelNum<classObjects.Count(); tryKernelNum++)
                {
                    double standardDeviation = 0;
                    for (int objectNum=0; objectNum<classObjects.Count() && standardDeviation<minStandardDeviation; objectNum++)
                    {
                        standardDeviation += Distance(classObjects[tryKernelNum], classObjects[objectNum]);
                    }
                    if (standardDeviation<minStandardDeviation)
                    {
                        minStandardDeviation = standardDeviation;
                        kernel = classObjects[tryKernelNum];
                    }
                }
                return kernel;
            }
            else
            {
                throw new ArgumentException("List is empty");
            }
        }

        public bool CheckandRechooseKernels (Dictionary<TObject, int> classesDictionary, TObject[] objects, ref int[] kernelsIndexes, Func<TObject, TObject, double> Distance)
        {
            bool isChanged = false;
            if (objects.Count() == _objectsAmount && kernelsIndexes.Count() == _classesAmount)
            {
                Task<TObject>[] tasksArr = new Task<TObject>[_classesAmount];
                for (int classNum = 0; classNum < _classesAmount; classNum++)
                {
                    List<TObject> classObjects = new List<TObject>();
                    for (int objectNum=0; objectNum<_objectsAmount; objectNum++)
                    {
                        if (classesDictionary[objects[objectNum]] == classNum)
                        {
                            classObjects.Add(objects[objectNum]);
                        }
                    }
                    tasksArr[classNum] = new Task<TObject>(() => { return FindNewClassKernel(classObjects, Distance); });
                    tasksArr[classNum].Start();
                }
                for (int classNum = 0; classNum < _classesAmount; classNum++)
                {
                    tasksArr[classNum].Wait();
                    TObject newKernel = tasksArr[classNum].Result;
                    if (!newKernel.Equals(objects[kernelsIndexes[classNum]]))
                    {
                        isChanged = true;
                        kernelsIndexes[classNum] = Array.IndexOf(objects, newKernel);
                    }
                }
            }
            return !isChanged;
        }
    }
}
